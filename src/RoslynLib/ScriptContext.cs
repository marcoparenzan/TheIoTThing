namespace RoslynLib;

public class ScriptContext : Dictionary<string, object>
{
    List<VariableDef> variables = new();

    public void MessageStatus(bool power, bool running)
    {
        Console.WriteLine($"Power: {power}, Running: {running}");
    }

    public ScriptContext Numeric(string name, string nodeId)
    {
        variables.Add(new(name, nodeId, "numeric"));
        return this;
    }

    public void Variables(Action<string, string> action)
    {
        foreach (var node in variables)
        {
            action(node.Name, node.NodeId);
        }
    }

    public void Set<T>(string name, T value)
    {
        this[name] = value;
    }

    public double Numeric(string name) => Convert.ToDouble(this[name]);

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}
