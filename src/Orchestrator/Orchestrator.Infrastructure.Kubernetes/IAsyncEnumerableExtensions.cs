using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Orchestrator.Infrastructure.Kubernetes;

public static class IAsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> AsAsyncEnumerableSafe<T>(
        this IAsyncEnumerable<T> asyncEnumerable,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerLineNumber] int? callerLineNumber = null,
        [CallerFilePath] string? callerFilePath = null
    )
    {
        await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);

        bool iterate;
        
        do
        {
            iterate = await Iterate(
                enumerator,
                cancellationToken,
                callerMemberName,
                callerLineNumber,
                callerFilePath
            );

            if (iterate && !cancellationToken.IsCancellationRequested)
            {
                yield return enumerator.Current;
            }
        }
        while (iterate && !cancellationToken.IsCancellationRequested);
    }
    
    private async static ValueTask<bool> Iterate<T>(
        IAsyncEnumerator<T> enumerator,
        CancellationToken cancellationToken,
        string? callerMemberName,
        int? callerLineNumber,
        string? callerFilePath
    )
    {
        try
        {
            return await enumerator.MoveNextAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception e)
        {
            Trace.WriteLine($"{callerMemberName}@{callerFilePath}:{callerLineNumber} {e.Message}");
            return true;
        }
    }
}