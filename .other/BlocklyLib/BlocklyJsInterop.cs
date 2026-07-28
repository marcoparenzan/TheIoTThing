using Microsoft.JSInterop;

namespace BlocklyLib;

public class BlocklyJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    string name;
    string elName;

    public BlocklyJsInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlocklyLib/blocklyJsInterop.js").AsTask());
    }

    public async Task CreateVariableAsync(string name, string varName)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("createVariable", this.name, varName);
    }

    public async Task InitWorkspaceAsync(string name, string elName)
    {
        this.name = name;
        this.elName = elName;

        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("initWorkspace", this.name, this.elName);
    }

    public async Task LoadWorkspaceAsync(string state)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("loadWorkspace", this.name, state);
    }

    public async Task<string> SaveWorkspaceAsync()
    {
        var module = await moduleTask.Value;
        var state = await module.InvokeAsync<string>("saveWorkspace", this.name);
        return state;
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
