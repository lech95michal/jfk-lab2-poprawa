namespace Synthesis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class Rewriter : CSharpSyntaxRewriter
    {
        List<SyntaxKind> kindList = new List<SyntaxKind>()
            {SyntaxKind.ByteKeyword,
             SyntaxKind.DecimalKeyword,
             SyntaxKind.DoubleKeyword,
             SyntaxKind.FloatKeyword,
             SyntaxKind.IntKeyword,
             SyntaxKind.LongKeyword,
             SyntaxKind.SByteKeyword,
             SyntaxKind.ShortKeyword,
             SyntaxKind.UIntKeyword,
             SyntaxKind.ULongKeyword,
             SyntaxKind.UShortKeyword,
            };

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var varDec = node.ChildNodes().OfType<VariableDeclarationSyntax>().FirstOrDefault();
            PredefinedTypeSyntax predefType = null;

            try
            {
                predefType = varDec.ChildNodes().OfType<PredefinedTypeSyntax>().Last();
            } catch(InvalidOperationException)
            {
                return base.VisitLocalDeclarationStatement(node).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            }

            var trivia = predefType.GetLeadingTrivia();

            var predefTypeKind = predefType.ChildTokens().FirstOrDefault().Kind();
            if(kindList.Contains(predefTypeKind))
                Console.WriteLine("Zawiera");

            var expr = varDec.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault()
                .ChildNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault()
                .ChildNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();

            var decl = varDec.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();

            var exprName = expr.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

            var newRight = SyntaxFactory.TriviaList();

            if (kindList.Contains(predefTypeKind) && exprName.ToString().Equals("Range"))
            {
                var argList = expr.ChildNodes().OfType<ArgumentListSyntax>().FirstOrDefault();

                var argListCount = argList.ChildNodes().OfType<ArgumentSyntax>().Count();
                if (argListCount == 2)
                {
                    var arg1 = argList.ChildNodes().OfType<ArgumentSyntax>().FirstOrDefault();
                    var arg2 = argList.ChildNodes().OfType<ArgumentSyntax>().Last();

                    var right = new SyntaxNodeOrToken[5];

                    Console.WriteLine(arg1 + " : " + arg2);
                    int count = 0;

                    try
                    {
                        ExpandSnippet(arg1, arg2, ref right, ref count);

                        var newFieldDecl = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ArrayType(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(predefTypeKind)))
                .WithRankSpecifiers(
                    SyntaxFactory.SingletonList<ArrayRankSpecifierSyntax>(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression())))))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(decl.Identifier.ToString()))
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ImplicitArrayCreationExpression(
                                SyntaxFactory.InitializerExpression(
                                    SyntaxKind.ArrayInitializerExpression,
                                    SyntaxFactory.SeparatedList<ExpressionSyntax>(
                                        right))))))))
                                        .NormalizeWhitespace();

                        node = node.ReplaceNode(node, newFieldDecl);
                    }
                    catch (FormatException)
                    {
                        return base.VisitLocalDeclarationStatement(node).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                    }
                }
            }

            return base.VisitLocalDeclarationStatement(node).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed).WithLeadingTrivia(SyntaxFactory.ElasticWhitespace(trivia.ToString()));
        }

        private static void ExpandSnippet(ArgumentSyntax arg1, ArgumentSyntax arg2, ref SyntaxNodeOrToken[] right, ref int count)
        {
            if (int.Parse(arg1.ToString()) < int.Parse(arg2.ToString()))
            {
                right = new SyntaxNodeOrToken[(int.Parse(arg2.ToString()) - int.Parse(arg1.ToString()) + 1) * 2 - 1];
                for (int i = int.Parse(arg1.ToString()); i <= int.Parse(arg2.ToString()); i++)
                {
                    var val = SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(i));

                    right[count] = val;
                    count++;
                    if (i < int.Parse(arg2.ToString()))
                    {
                        right[count] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                        count++;
                    }
                }
            }
            else if (int.Parse(arg1.ToString()) > int.Parse(arg2.ToString()))
            {
                right = new SyntaxNodeOrToken[(int.Parse(arg1.ToString()) - int.Parse(arg2.ToString()) + 1) * 2 - 1];
                for (int i = int.Parse(arg1.ToString()); i >= int.Parse(arg2.ToString()); i--)
                {
                    var val = SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(i));

                    right[count] = val;
                    count++;
                    if (i > int.Parse(arg2.ToString()))
                    {
                        right[count] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                        count++;
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                Console.Write(right[i]);
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var varDec = node.ChildNodes().OfType<VariableDeclarationSyntax>().FirstOrDefault();
            
            PredefinedTypeSyntax predefType = null;

            try
            {
                predefType = varDec.ChildNodes().OfType<PredefinedTypeSyntax>().Last();
            }
            catch (InvalidOperationException)
            {
                return base.VisitFieldDeclaration(node).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            }

            var trivia = predefType.GetLeadingTrivia();

            var predefTypeKind = predefType.ChildTokens().FirstOrDefault().Kind();
            if (kindList.Contains(predefTypeKind))
                Console.WriteLine("Zawiera");
            
            var expr = varDec.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault()
                .ChildNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault()
                .ChildNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();

            var decl = varDec.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();

            var exprName = expr.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

            var newRight = SyntaxFactory.TriviaList();

            if (kindList.Contains(predefTypeKind) && exprName.ToString().Equals("Range"))
            {
                var argList = expr.ChildNodes().OfType<ArgumentListSyntax>().FirstOrDefault();

                var argListCount = argList.ChildNodes().OfType<ArgumentSyntax>().Count();
                if (argListCount == 2)
                {
                    var arg1 = argList.ChildNodes().OfType<ArgumentSyntax>().FirstOrDefault();
                    var arg2 = argList.ChildNodes().OfType<ArgumentSyntax>().Last();

                    var right = new SyntaxNodeOrToken[5];
                    
                    Console.WriteLine(arg1 + " : " + arg2);
                    int count = 0;

                    try
                    {
                        ExpandSnippet(arg1, arg2, ref right, ref count);

                        var newFieldDecl = SyntaxFactory.FieldDeclaration(
        SyntaxFactory.VariableDeclaration(
            SyntaxFactory.ArrayType(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(predefTypeKind)))
            .WithRankSpecifiers(
                SyntaxFactory.SingletonList<ArrayRankSpecifierSyntax>(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
        .WithVariables(
            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                SyntaxFactory.VariableDeclarator(
                    SyntaxFactory.Identifier(decl.Identifier.ToString()))
                .WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ImplicitArrayCreationExpression(
                            SyntaxFactory.InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                                    right)))))))).NormalizeWhitespace();
            
                  
                    node = node.ReplaceNode(node, newFieldDecl);
                    }
                    catch (FormatException)
                    {
                        return base.VisitFieldDeclaration(node).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                    }
                }
            }
            return base.VisitFieldDeclaration(node).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed).WithLeadingTrivia(SyntaxFactory.ElasticWhitespace(trivia.ToString()));
        }
    }
}
