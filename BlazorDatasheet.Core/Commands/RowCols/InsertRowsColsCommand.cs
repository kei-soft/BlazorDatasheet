﻿using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands.RowCols;

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowsColsCommand : BaseCommand, IUndoableCommand
{
    private readonly int _index;
    private readonly int _count;
    private readonly Axis _axis;

    private RegionRestoreData<int> _validatorRestoreData = null!;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData = null!;
    private CellStoreRestoreData _cellStoreRestoreData = null!;
    private RowColInfoRestoreData _rowColInfoRestoreData = null!;
    private MergeableIntervalStoreRestoreData<OverwritingValue<List<IFilter>?>> _filterRestoreData = null!;

    /// <summary>
    /// Command for inserting a row into the sheet.
    /// </summary>
    /// <param name="index">The index that the row/column will be inserted at.</param>
    /// <param name="count">The number to insert</param>
    /// <param name="axis">Which axis to insert into the sheet</param>
    public InsertRowsColsCommand(int index, int count, Axis axis)
    {
        _index = index;
        _count = count;
        _axis = axis;
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.Add(_axis, _count);
        _validatorRestoreData = sheet.Validators.Store.InsertRowColAt(_index, _count, _axis);
        _cellStoreRestoreData = sheet.Cells.InsertRowColAt(_index, _count, _axis);
        _cfRestoreData = sheet.ConditionalFormats.InsertRowColAt(_index, _count, _axis);
        _rowColInfoRestoreData = sheet.GetRowColStore(_axis).InsertImpl(_index, _count);

        if (_axis == Axis.Col)
        {
            _filterRestoreData = sheet.Columns.Filters.Store.InsertAt(_index, _count);
        }

        sheet.ScreenUpdating = true;
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.Remove(_axis, _count);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.GetRowColStore(_axis).Restore(_rowColInfoRestoreData);
        sheet.GetRowColStore(_axis).EmitRemoved(_index, _count);
        if (_axis == Axis.Col)
            sheet.Columns.Filters.Store.Restore(_filterRestoreData);

        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(_index, sheet.NumCols)
            : new RowRegion(_index, sheet.NumRows);
        sheet.MarkDirty(dirtyRegion);
        sheet.ScreenUpdating = true;
        return true;
    }
}