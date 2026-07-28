using Microsoft.JSInterop;

namespace FlowUILib;

public class FlowUIInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    string? name;
    string? elName;

    public FlowUIInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/FlowUILib/flowUIInterop.js").AsTask());
    }

    public async Task InitFlowAsync(string name, string elName, string? state = null, object? saveCallbackRef = null)
    {
        this.name = name;
        this.elName = elName;

        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("initFlow", this.name, this.elName, state, saveCallbackRef);
    }

    public async Task<string?> GetFlowJsonAsync()
    {
        if (name is null) return null;
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string?>("getFlowJson", name);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                if (name is not null)
                {
                    await module.InvokeVoidAsync("disposeFlow", name);
                }
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone (tab closed, navigated away, connection dropped) — there's
                // no client left to run the JS cleanup on, and the browser will tear down the page's JS
                // state on its own anyway.
            }
        }
    }
}
