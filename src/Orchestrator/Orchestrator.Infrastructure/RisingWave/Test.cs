using Npgsql;

namespace Orchestrator.Infrastructure.RisingWave;

public sealed class RwCursorOptions<T>
{
    public required Func<NpgsqlConnection> ConnectionFactory { get; init; }
    public required string SubscriptionName { get; init; }   // e.g. schema.sub_name OR just sub_name if search_path set
    public string CursorName { get; init; } = "rw_cur";
    public string? SearchPath { get; init; }                 // e.g. "orchestration"
    public string ProgressTable { get; init; } = "subscription_progress";
    public string FetchTimeout { get; init; } = "30s";       // returns no rows on timeout
    public required Func<NpgsqlDataReader, T> Map { get; init; }
    public required Func<T, long> GetProgress { get; init; } // must return the rw_timestamp (monotonic)
}

public sealed class RwCursorEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly RwCursorOptions<T> _opt;
    private readonly CancellationToken _ct;
    private NpgsqlConnection? _conn;
    private bool _declared;
    private long? _lastProgress; // persisted checkpoint
    private long? _pendingProgress; // for Current, to ack later
    private bool _disposed;

    public RwCursorEnumerator(RwCursorOptions<T> opt, CancellationToken ct = default)
    {
        _opt = opt;
        _ct = ct;
    }

    public T Current { get; private set; } = default!;

    public async ValueTask<bool> MoveNextAsync()
    {
        _ct.ThrowIfCancellationRequested();

        // Auto-ack previous item (if any)
        if (_pendingProgress is long pPrev)
        {
            await CommitProgressAsync(pPrev);
            _pendingProgress = null;
        }

        // Ensure connection + cursor
        if (_conn is not {State: System.Data.ConnectionState.Open})
            await OpenAndInitAsync();
        if (!_declared)
            await DeclareCursorAsync();

        // Fetch until we get a row (timeout just returns empty)
        while (true)
        {
            await using var cmd = _conn!.CreateCommand();
            cmd.CommandText = $"""
                                   FETCH 1 FROM {_opt.CursorName}
                                   WITH (timeout = '{_opt.FetchTimeout}');
                               """;
            using var r = await cmd.ExecuteReaderAsync(_ct);

            if (await r.ReadAsync(_ct))
            {
                Current = _opt.Map(r);
                _pendingProgress = _opt.GetProgress(Current); // will be acked on next MoveNext or Dispose
                return true;
            }

            // No rows within timeout ⇒ loop and try again (acts like a blocking enumerator)
            _ct.ThrowIfCancellationRequested();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            // Auto-ack last yielded item (if caller never asked for next)
            if (_pendingProgress is long p)
            {
                try { await CommitProgressAsync(p); }
                catch
                {
                    /* best-effort */
                }
                _pendingProgress = null;
            }

            if (_conn is {State: System.Data.ConnectionState.Open} && _declared)
            {
                try
                {
                    using var close = _conn.CreateCommand();
                    close.CommandText = $"CLOSE {_opt.CursorName};";
                    await close.ExecuteNonQueryAsync(_ct);
                }
                catch
                {
                    /* ignore */
                }
            }
        }
        finally
        {
            if (_conn != null)
            {
                try { await _conn.CloseAsync(); }
                catch { }
                await _conn.DisposeAsync();
                _conn = null;
            }
        }
    }

    // ---------------- internals ----------------

    private async Task OpenAndInitAsync()
    {
        if (_conn != null)
        {
            try { await _conn.DisposeAsync(); }
            catch { }
            _conn = null;
        }

        _conn = _opt.ConnectionFactory();
        await _conn.OpenAsync(_ct);

        if (!string.IsNullOrWhiteSpace(_opt.SearchPath))
        {
            using var sp = _conn.CreateCommand();
            sp.CommandText = $"SET search_path TO {_opt.SearchPath};";
            await sp.ExecuteNonQueryAsync(_ct);
        }

        // progress table
        using (var ctCmd = _conn.CreateCommand())
        {
            ctCmd.CommandText = $"""
                                     CREATE TABLE IF NOT EXISTS {_opt.ProgressTable}(
                                       sub_name TEXT PRIMARY KEY,
                                       progress BIGINT
                                     );
                                 """;
            await ctCmd.ExecuteNonQueryAsync(_ct);
        }

        // load checkpoint
        using (var lp = _conn.CreateCommand())
        {
            lp.CommandText = $"""SELECT progress FROM {_opt.ProgressTable} WHERE sub_name=@n;""";
            lp.Parameters.AddWithValue("n", _opt.SubscriptionName);
            var o = await lp.ExecuteScalarAsync(_ct);
            _lastProgress = o is long l ? l : null;
        }

        _declared = false;
    }

    private async Task DeclareCursorAsync()
    {
        await using var cmd = _conn!.CreateCommand();
        cmd.CommandText = _lastProgress is long p
            ? $"""
               DECLARE {_opt.CursorName}
               SUBSCRIPTION CURSOR
               FOR {_opt.SubscriptionName} SINCE {p};
               """
            : $"""
               DECLARE {_opt.CursorName}
               SUBSCRIPTION CURSOR
               FOR {_opt.SubscriptionName};
               """;
        await cmd.ExecuteNonQueryAsync(_ct);
        _declared = true;
    }

    private async Task CommitProgressAsync(long p)
    {
        if (_lastProgress is long lp && p <= lp) return; // monotonic guard
        using var cmd = _conn!.CreateCommand();
        cmd.CommandText = $"""
                               INSERT INTO {_opt.ProgressTable}(sub_name, progress)
                               VALUES (@n, @p)
                               ON CONFLICT (sub_name) DO UPDATE SET progress = EXCLUDED.progress;
                               FLUSH;
                           """;
        cmd.Parameters.AddWithValue("n", _opt.SubscriptionName);
        cmd.Parameters.AddWithValue("p", p);
        await cmd.ExecuteNonQueryAsync(_ct);
        _lastProgress = p;
    }
}