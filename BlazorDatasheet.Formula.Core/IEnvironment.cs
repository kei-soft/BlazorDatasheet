﻿using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public interface IEnvironment : IFunctionProvider
{
    CellValue GetCellValue(int row, int col, string sheetName);

    /// <summary>
    /// Return a cell formula at <paramref name="row"/>, <paramref name="col"/> if it exists.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    CellFormula? GetFormula(int row, int col, string sheetName);

    public CellValue[][] GetRangeValues(Reference reference);
    bool VariableExists(string variableIdentifier);
    CellValue GetVariable(string variableIdentifier);
    void SetVariable(string name, CellValue value);
    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference);
    void SetCellValue(int row, int col, string sheetName, CellValue value);
    void ClearVariable(string varName);
    IEnumerable<string> GetVariableNames();
}