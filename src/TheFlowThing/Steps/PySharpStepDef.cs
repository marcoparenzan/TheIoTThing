using TheFlowThing.Abstractions;
using PySharpLib;
using PySharpLib.Runtime;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class PySharpStepDef : StepDef<PySharpPropertiesDef>
{
    public override PySharpStepState CreateState()
    {
        return new PySharpStepState(this)
        {
        };
    }
}

public class PySharpStepState(PySharpStepDef def) : StepState
{
    public override Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        // Same inputs/outputs dict convention as node-script (see NodeScriptStepDef): connector ids
        // aren't guaranteed to be valid, non-reserved identifiers (`in` is a Python keyword too), so
        // the code reads inputs['id'] and writes outputs['id'] instead of bare variables.
        var script = new StringBuilder()
            .Append("inputs = ").AppendLine(ToPyDictLiteral(inputValues))
            .AppendLine("outputs = {}")
            .AppendLine(def.Properties.Code ?? "")
            .ToString();

        var engine = new PyEngine(TextWriter.Null);
        var module = engine.Run(script, "flow");

        var result = new Dictionary<string, object>();
        var outputs = module.Dict.TryGet("outputs", out var outputsObj) ? outputsObj as PyDict : null;
        foreach (var output in def.Outputs)
        {
            result[output.Id] = outputs is not null && outputs.TryGet(output.Id, out var value) ? FromPyValue(value) : null!;
        }
        return Task.FromResult(result);
    }

    public override StepDef Def => def;

    static string ToPyDictLiteral(Dictionary<string, object> values)
    {
        var sb = new StringBuilder("{");
        var first = true;
        foreach (var (key, value) in values)
        {
            if (!first) sb.Append(", ");
            first = false;
            sb.Append(JsonSerializer.Serialize(key)).Append(": ").Append(ToPyLiteral(value));
        }
        return sb.Append('}').ToString();
    }

    // JSON literal syntax for strings/numbers is also valid Python literal syntax; only booleans and
    // null need translating (True/False/None instead of true/false/null).
    static string ToPyLiteral(object? value) => value switch
    {
        null => "None",
        bool b => b ? "True" : "False",
        _ => JsonSerializer.Serialize(value)
    };

    // Composite Python values (list/dict/tuple/...) are passed through as raw PySharpLib runtime
    // objects rather than converted back to CLR/JSON — only the scalar cases a pipe value realistically
    // needs are handled here.
    static object? FromPyValue(object? value) => value switch
    {
        PyNone => null,
        BigInteger bi when bi >= long.MinValue && bi <= long.MaxValue => (long)bi,
        BigInteger bi => (double)bi,
        _ => value
    };
}

public class PySharpPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("code")]
    public string Code { get; set; }
}
