using TheFlowThing.Abstractions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheFlowThing.Steps;

public class ApiOutputStepDef : StepDef<ApiOutputPropertiesDef>
{
    public override ApiOutputStepState CreateState()
    {
        return new ApiOutputStepState(this)
        {
        };
    }
}

public class ApiOutputStepState(ApiOutputStepDef def) : StepState
{
    static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    public override async Task<Dictionary<string, object>> AdvanceAsync(QuantumOfTime qot, Dictionary<string, object> inputValues)
    {
        var value = inputValues.Values.FirstOrDefault();
        var method = string.IsNullOrWhiteSpace(def.Properties.Method) ? "POST" : def.Properties.Method.ToUpperInvariant();

        using var request = new HttpRequestMessage(new HttpMethod(method), def.Properties.Url);
        if (method is not ("GET" or "HEAD"))
        {
            request.Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
        }

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return new Dictionary<string, object>();
    }

    public override StepDef Def => def;
}

public class ApiOutputPropertiesDef : StepPropertiesDef
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; }
}
