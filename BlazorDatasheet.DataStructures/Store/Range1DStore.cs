using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// Contains non-overlapping data in ranges
/// </summary>
public class Range1DStore<T> : ISparseSource
{
    protected readonly MergeableIntervalStore<OverwritingValue<T>> Intervals;
    private readonly T? _defaultIfNotFound;

    /// <summary>
    /// The minimum range of all intervals
    /// </summary>
    public int Start => Intervals.Start;

    /// <summary>
    /// The maximum range of all intervals
    /// </summary>
    public int End => Intervals.End;

    public Range1DStore(T? defaultIfNotFound)
    {
        _defaultIfNotFound = defaultIfNotFound;
        Intervals = new MergeableIntervalStore<OverwritingValue<T>>(new OverwritingValue<T>(defaultIfNotFound));
    }

    /// <summary>
    /// Assigns the range to the value given. Returns any ranges that were modified when it was set.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual MergeableIntervalStoreRestoreData<OverwritingValue<T>> Set(int start, int end, T value)
    {
        return Intervals.Add(start, end, new OverwritingValue<T>(value));
    }

    /// <summary>
    /// Sets a range of length = 1 to the value given
    /// </summary>
    /// <param name="start"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public MergeableIntervalStoreRestoreData<OverwritingValue<T>> Set(int start, T value)
    {
        return Set(start, start, value);
    }

    /// <summary>
    /// Removes the intervals between and including <paramref name="start"/> amd <paramref name="end"/>
    /// and shifts the remaining values to the left.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public virtual MergeableIntervalStoreRestoreData<OverwritingValue<T>>
        Delete(int start, int end)
    {
        var restoreData = Intervals.Clear(start, end);
        restoreData.Merge(Intervals.ShiftLeft(start, (end - start) + 1));
        return restoreData;
    }

    public List<(int start, int end, T? value)> GetOverlapping(int start, int end)
    {
        return Intervals.GetIntervals(start, end)
            .Select(x => (x.Start, x.End, x.Data.Value))
            .ToList();
    }

    /// <summary>
    /// Inserts empty values into the store and shifts to the right
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    public virtual MergeableIntervalStoreRestoreData<OverwritingValue<T>> InsertAt(int start, int n)
    {
        return Intervals.ShiftRight(start, n);
    }

    public T? Get(int position)
    {
        var val = Intervals.Get(position);
        if (val == null)
            return _defaultIfNotFound;
        return val.Value;
    }

    /// <summary>
    /// Returns the interval after the given position
    /// </summary>
    /// <param name="position"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Interval? GetNext(int position, int direction = 1)
    {
        var interval = Intervals.GetNext(position, direction);
        if (interval == null)
            return null;

        return new Interval(interval.Start, interval.End);
    }

    public List<(Interval interval, T? data)> GetAllIntervalData()
    {
        return Intervals.GetAllIntervals().Select(x => (new Interval(x.Start, x.End), x.Data.Value)).ToList();
    }

    public List<Interval> GetAllIntervals()
    {
        return Intervals
            .GetAllIntervals()
            .Select(x => new Interval(x.Start, x.End))
            .ToList();
    }

    /// <summary>
    /// Removes the data between the given positions but does not shift the remaining data
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public MergeableIntervalStoreRestoreData<OverwritingValue<T>> Clear(int start, int end)
    {
        return Intervals.Clear(start, end);
    }

    public void Clear()
    {
        this.Intervals.Clear();
    }

    public void Restore(MergeableIntervalStoreRestoreData<OverwritingValue<T>> restoreData)
    {
        Intervals.Restore(restoreData);
    }

    public int GetNextNonEmptyIndex(int index)
    {
        var interval = Intervals.GetNextNonEmptyIndex(index);
        return interval;
    }

    public bool Any() => Intervals.Any();
}

public class OverwritingValue<R> : IMergeable<OverwritingValue<R>>, IEquatable<OverwritingValue<R>>
{
    public R? Value { get; private set; }

    public OverwritingValue(R? value)
    {
        Value = value;
    }

    public void Merge(OverwritingValue<R> item)
    {
        Value = item.Value;
    }

    public OverwritingValue<R> Clone()
    {
        return new OverwritingValue<R>(Value);
    }

    public bool Equals(OverwritingValue<R>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<R?>.Default.Equals(Value, other.Value);
    }
}