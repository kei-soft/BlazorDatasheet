using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Formatting;

public class SetTypeCommand : BaseCommand, IUndoableCommand
{
    private readonly IRegion _region;
    private readonly string _type;
    private CellStoreRestoreData _restoreData = null!;

    public SetTypeCommand(int row, int col, string type) : this(new Region(row, row, col, col), type)
    {
    }

    public SetTypeCommand(IRegion region, string type)
    {
        _region = region;
        _type = type;
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.SetCellTypeImpl(_region, _type);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}