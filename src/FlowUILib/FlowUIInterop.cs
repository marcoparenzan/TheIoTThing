using Microsoft.JSInterop;

namespace FlowUILib;

public class FlowUIInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    string name;
    string elName;

    public FlowUIInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/FlowUILib/flowUIInterop.js").AsTask());
    }

    public async Task InitFlowAsync(string name, string elName)
    {
        this.name = name;
        this.elName = elName;

        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("initFlow", this.name, this.elName);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
