using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public class SinFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition("x",
                ParameterType.Number,
                ParameterRequirement.Required,
                isRepeating: false)
        };
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var val = args[0];
        return CellValue.Number(System.Math.Sin((double)val.Data!));
    }

    public bool AcceptsErrors => false;
    public bool IsVolatile => false;
}