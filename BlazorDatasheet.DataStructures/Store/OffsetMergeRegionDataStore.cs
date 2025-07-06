using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// Merge region data store that applies row/column offsets when rows or columns are inserted or removed.
/// </summary>
public class OffsetMergeRegionDataStore<T> : MergeRegionDataStore<T> where T : IMergeable<T>, IEquatable<T>
{
    private readonly OffsetManager _rowOffsets = new();
    private readonly OffsetManager _colOffsets = new();

    public OffsetMergeRegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true) : base(minArea, expandWhenInsertAfter)
    {
    }

    private int PhysicalRow(int logicalRow) => _rowOffsets.ToPhysical(logicalRow);
    private int PhysicalCol(int logicalCol) => _colOffsets.ToPhysical(logicalCol);

    private bool RowInserted(int row) => _rowOffsets.IsInserted(row);
    private bool ColInserted(int col) => _colOffsets.IsInserted(col);

    public new bool Contains(int row, int col)
    {
        if (RowInserted(row) || ColInserted(col))
            return false;
        return base.Contains(PhysicalRow(row), PhysicalCol(col));
    }

    public new IEnumerable<DataRegion<T>> GetDataRegions(int row, int col)
    {
        if (RowInserted(row) || ColInserted(col))
            return Enumerable.Empty<DataRegion<T>>();
        return base.GetDataRegions(PhysicalRow(row), PhysicalCol(col));
    }

    public new IEnumerable<DataRegion<T>> GetDataRegions(IRegion region)
    {
        var phys = new Region(
            PhysicalRow(region.Top), PhysicalRow(region.Bottom),
            PhysicalCol(region.Left), PhysicalCol(region.Right));
        return base.GetDataRegions(phys);
    }

    public new RegionRestoreData<T> Add(IRegion region, T data)
    {
        var phys = new Region(
            PhysicalRow(region.Top), PhysicalRow(region.Bottom),
            PhysicalCol(region.Left), PhysicalCol(region.Right));
        return base.Add(phys, data);
    }

    public override RegionRestoreData<T> Set(int row, int col, T value)
    {
        if (RowInserted(row) || ColInserted(col))
            return new RegionRestoreData<T>();
        return base.Set(PhysicalRow(row), PhysicalCol(col), value);
    }

    public new RegionRestoreData<T> InsertRowColAt(int index, int count, Axis axis)
    {
        if (axis == Axis.Row)
            _rowOffsets.Insert(index, count);
        else
            _colOffsets.Insert(index, count);
        return new RegionRestoreData<T>()
        {
            Shifts = [new(axis, index, count, null)]
        };
    }

    public new RegionRestoreData<T> RemoveRowColAt(int index, int count, Axis axis)
    {
        if (axis == Axis.Row)
            _rowOffsets.Remove(index, count);
        else
            _colOffsets.Remove(index, count);
        return new RegionRestoreData<T>()
        {
            Shifts = [new(axis, index, -count, null)]
        };
    }
}
