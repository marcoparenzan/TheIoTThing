using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkspaceLib.Models;

namespace WorkspaceLib;

public class CSharpTranspiler
{
    public BlockSyntax Transpile(WorkspaceDef def)
    {
        var codeBlock = SyntaxFactory.Block();
        foreach (BlockValue block in def.Blocks.Blocks)
        {
            var exp = Evaluate(block, def.Variables.ToArray());
            codeBlock = codeBlock.AddStatements(SyntaxFactory.ExpressionStatement(exp));
        }
        return codeBlock;
    }

    ExpressionSyntax Evaluate(BlockValue block, VariableDef[] vars)
    {
        switch (block.Type)
        {
            case "messagestatus":
                return MessageStatus(block, vars);
            case "logic_operation":
                return LogicOperation(block, vars);
            case "logic_compare":
                return LogicCompare(block, vars);
            case "variables_get_dynamic":
            case "variables_get":
                return VariablesGet(block, vars);
            case "math_number":
                return MathNumber(block, vars);
            default:
                Console.WriteLine(block.Type);
                return null;
        }
    }

    ExpressionSyntax LogicOperation(BlockValue block, VariableDef[] vars)
    {
        switch (block.Fields["OP"].GetString())
        {
            case "AND":
                var a = block.Inputs["A"]["block"];
                var b = block.Inputs["B"]["block"];
                return SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, Evaluate(a, vars), Evaluate(b, vars));
            default:
                Console.WriteLine(block.Type);
                return null;
        }
    }

    ExpressionSyntax LogicCompare(BlockValue block, VariableDef[] vars)
    {
        switch (block.Fields["OP"].GetString())
        {
            case "EQ":
                {
                    var a = block.Inputs["A"]["block"];
                    var b = block.Inputs["B"]["block"];
                    return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, Evaluate(a, vars), Evaluate(b, vars));
                }
            case "GT":
                {
                    var a = block.Inputs["A"]["block"];
                    var b = block.Inputs["B"]["block"];
                    return SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanExpression, Evaluate(a, vars), Evaluate(b, vars));
                }
            default:
                Console.WriteLine(block.Type);
                return null;
        }
    }

    ExpressionSyntax VariablesGet(BlockValue block, VariableDef[] vars)
    {
        var id = block.Fields["VAR"].GetProperty("id").GetString();
        var name = vars.Single(xx => xx.Id == id).Name;
        var nameExp = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name));
        var xxx = nameExp.ToFullString();
        //var invocationExp =
        //    SyntaxFactory.InvocationExpression(
        //        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("Numeric")),
        //        SyntaxFactory.ArgumentList().AddArguments(
        //            SyntaxFactory.Argument(nameExp)
        //        )
        //    );
        var invocationExp =
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("Numeric"),
                SyntaxFactory.ArgumentList().AddArguments(
                    SyntaxFactory.Argument(nameExp)
                )
            );
        return invocationExp;
    }

    ExpressionSyntax MathNumber(BlockValue block, VariableDef[] vars)
    {
        try
        {
            var ff = -0.4;
            var qq = ff > -0.5;
            var num = block.Fields["NUM"].GetDecimal();
            if (num >= 0)
            {
                var token = SyntaxFactory.ParseToken((num).ToString().Replace(',', '.'));
                var exp1 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, token);
                var exp = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryPlusExpression, exp1);
                return exp;
            }
            else
            {
                var token = SyntaxFactory.ParseToken((-num).ToString().Replace(',', '.'));
                var exp1 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, token);
                var exp = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, exp1);
                return exp;
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }


    ExpressionSyntax MessageStatus(BlockValue block, VariableDef[] vars)
    {
        var power = block.Inputs["Power"]["block"];
        var running = block.Inputs["Running"]["block"];

        //var invocationExp =
        //    SyntaxFactory.InvocationExpression(
        //        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("MessageStatus")),
        //        SyntaxFactory.ArgumentList().AddArguments(
        //            SyntaxFactory.Argument(Evaluate(power, vars)),
        //            SyntaxFactory.Argument(Evaluate(running, vars))
        //        )
        //    );
        var invocationExp =
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName("MessageStatus"),
                SyntaxFactory.ArgumentList().AddArguments(
                    SyntaxFactory.Argument(Evaluate(power, vars)),
                    SyntaxFactory.Argument(Evaluate(running, vars))
                )
            );
        return invocationExp;
    }
}
