using System.Collections.Generic;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// Tracks index offsets resulting from insert and delete operations.
/// The offsets are stored as a difference list so the cumulative
/// offset at an index can be calculated quickly.
/// </summary>
internal class OffsetManager
{
    private readonly SortedDictionary<int, int> _diff = new();

    private int GetCumulative(int index)
    {
        int offset = 0;
        foreach (var kv in _diff)
        {
            if (kv.Key > index)
                break;
            offset += kv.Value;
        }
        return offset;
    }

    public int ToPhysical(int logical)
    {
        return logical - GetCumulative(logical);
    }

    public bool IsInserted(int logical)
    {
        var prev = GetCumulative(logical - 1);
        var curr = GetCumulative(logical);
        return curr > prev;
    }

    public void Insert(int index, int count)
    {
        if (count == 0)
            return;
        if (_diff.ContainsKey(index))
            _diff[index] += count;
        else
            _diff.Add(index, count);
    }

    public void Remove(int index, int count)
    {
        if (count == 0)
            return;
        if (_diff.ContainsKey(index))
            _diff[index] -= count;
        else
            _diff.Add(index, -count);
    }
}
