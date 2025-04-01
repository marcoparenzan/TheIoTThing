using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace RoslynLib;

public class CSharpExecutive<TReturn>
{
    private CSharpExecutive() { }

    InteractiveAssemblyLoader loader;
    Script<TReturn> script;
    object ctx;

    public static CSharpExecutive<TReturn> New(string scriptText, object ctx)
    {
        var exec = new CSharpExecutive<TReturn>();

        exec.ctx = ctx;

        exec.loader = new InteractiveAssemblyLoader();
        exec.script = CSharpScript.Create<TReturn>(
            scriptText, 
            ScriptOptions.Default
                .AddReferences(ctx.GetType().Assembly)
                .AddImports("System")
                .WithEmitDebugInformation(true), 
            ctx.GetType(),
            exec.loader
        );

        exec.script.Compile();

        return exec;
    }

    public async Task<TReturn> RunAsync()
    {
        var result = await this.script.RunAsync(this.ctx);
        return result.ReturnValue;
    }
}
