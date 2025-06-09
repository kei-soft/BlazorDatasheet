﻿using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class UnhideCommand : BaseCommand, IUndoableCommand
{
    private List<Interval> _intervals;
    private readonly Axis _axis;

    private RowColInfoRestoreData _restoreData = null!;

    public UnhideCommand(int start, int end, Axis axis)
    {
        _intervals = [new(start, end)];
        _axis = axis;
    }

    public UnhideCommand(List<Interval> intervals, Axis axis)
    {
        _intervals = intervals;
        _axis = axis;
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).UnhideImpl(_intervals);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(0, int.MaxValue)
            : new RowRegion(0, int.MaxValue);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}