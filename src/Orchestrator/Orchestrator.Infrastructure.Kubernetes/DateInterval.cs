using System.Collections;

namespace Orchestrator.Infrastructure.Kubernetes;

public class DateInterval : IEnumerable<DateOnly>
{
    private readonly DateOnly _start;
    private readonly DateOnly _end;
    private readonly DateIntervalType _intervalType;

    public DateInterval(DateOnly start, DateOnly end, DateIntervalType intervalType)
    {
        switch (intervalType)
        {
            case DateIntervalType.Weekly:
            {
                if (start.DayOfWeek != end.DayOfWeek)
                    throw new InvalidOperationException();
                break;
            }
            case DateIntervalType.None:
            default:
                throw new ArgumentOutOfRangeException();
        }

        _start = start;
        _end = end;
        _intervalType = intervalType;

    }

    public IEnumerator<DateOnly> GetEnumerator()
    {
        var currentDate = _start;

        do
        {
            yield return currentDate;

            var daysToAdd = _intervalType switch
            {
                DateIntervalType.Weekly => 7,
                DateIntervalType.None => throw new ArgumentOutOfRangeException(),
                _ => throw new ArgumentOutOfRangeException()
            };

            currentDate = currentDate.AddDays(daysToAdd);

        } while (currentDate <= _end);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public enum DateIntervalType
{
    None,
    Weekly
}