//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.VisualBasic;
//using Microsoft.CodeAnalysis.VisualBasic.Syntax;
//using WorkspaceLib.Models;

//namespace WorkspaceLib;

//public class VBTranspiler
//{
//    public SyntaxNode Transpile(WorkspaceDef def)
//    {
//        var codeBlock = SyntaxFactory.ModuleBlock();
//        foreach (BlockValue block in def.Blocks.Blocks)
//        {
//            var exp = Evaluate(block);
//            codeBlock = codeBlock.AddStatements(SyntaxFactory.ExpressionStatement(exp));
//        }
//        return codeBlock;
//    }

//    ExpressionSyntax Evaluate(BlockValue block)
//    {
//        switch (block.Type)
//        {
//            case "messagestatus":
//                return MessageStatus(block);
//            case "logic_operation":
//                return LogicOperation(block);
//            case "logic_compare":
//                return LogicCompare(block);
//            case "variables_get_dynamic":
//            case "variables_get":
//                return VariablesGet(block);
//            case "math_number":
//                return MathNumber(block);
//            default:
//                Console.WriteLine(block.Type);
//                return null;
//        }
//    }

//    ExpressionSyntax LogicOperation(BlockValue block)
//    {
//        switch (block.Fields["OP"].GetString())
//        {
//            case "AND":
//                var a = block.Inputs["A"]["block"];
//                var b = block.Inputs["B"]["block"];
//                return SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, Evaluate(a), Evaluate(b));
//            default:
//                Console.WriteLine(block.Type);
//                return null;
//        }
//    }

//    ExpressionSyntax LogicCompare(BlockValue block)
//    {
//        switch (block.Fields["OP"].GetString())
//        {
//            case "EQ":
//                var a = block.Inputs["A"]["block"];
//                var b = block.Inputs["B"]["block"];
//                return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, Evaluate(a), Evaluate(b));
//            default:
//                Console.WriteLine(block.Type);
//                return null;
//        }
//    }

//    ExpressionSyntax VariablesGet(BlockValue block)
//    {
//        var id = block.Fields["VAR"].GetProperty("id").GetString();
//        var idExp = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(id));
//        var xxx = idExp.ToFullString();
//        var invocationExp =
//            SyntaxFactory.InvocationExpression(
//                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("Get")),
//                SyntaxFactory.ArgumentList().AddArguments(
//                    SyntaxFactory.Argument(idExp)
//                )
//            );
//        return invocationExp;
//    }

//    ExpressionSyntax MathNumber(BlockValue block)
//    {
//        var num = block.Fields["NUM"].GetDecimal();

//        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.ParseToken(num.ToString()));
//    }


//    ExpressionSyntax MessageStatus(BlockValue block)
//    {
//        var power = block.Inputs["Power"]["block"];
//        var running = block.Inputs["Running"]["block"];

//        var invocationExp =
//            SyntaxFactory.InvocationExpression(
//                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("MessageStatus")),
//                SyntaxFactory.ArgumentList().AddArguments(
//                    SyntaxFactory.Argument(Evaluate(power)),
//                    SyntaxFactory.Argument(Evaluate(running))
//                )
//            );
//        return invocationExp;
//    }
//}
