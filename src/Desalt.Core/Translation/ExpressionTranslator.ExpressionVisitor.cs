// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ExpressionTranslator.Visitor.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Translation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Desalt.CompilerUtilities.Extensions;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.SymbolTables;
    using Desalt.Core.Utility;
    using Desalt.TypeScriptAst.Ast;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Factory = TypeScriptAst.Ast.TsAstFactory;

    internal partial class ExpressionTranslator
    {
        private sealed partial class ExpressionVisitor : BaseTranslationVisitor<ExpressionSyntax, ITsExpression>
        {
            //// =======================================================================================================
            //// Member Variables
            //// =======================================================================================================

            private bool _isVisitingSubExpression;

            //// =======================================================================================================
            //// Constructors
            //// =======================================================================================================

            public ExpressionVisitor(TranslationContext context)
                : base(context)
            {
            }

            //// =======================================================================================================
            //// Properties
            //// =======================================================================================================

            public IList<ITsStatementListItem> AdditionalStatements { get; } = new List<ITsStatementListItem>();

            //// =======================================================================================================
            //// Methods
            //// =======================================================================================================

            public ITsExpression VisitSubExpression(SyntaxNode node)
            {
                _isVisitingSubExpression = true;
                return Visit(node);
            }

            //// =======================================================================================================
            //// Literal Expressions
            //// =======================================================================================================

            /// <summary>
            /// Called when the visitor visits the 'this' expression.
            /// </summary>
            /// <returns>An <see cref="ITsThis"/>.</returns>
            public override ITsExpression VisitThisExpression(ThisExpressionSyntax node)
            {
                return Factory.This;
            }

            /// <summary>
            /// Called when the visitor visits a LiteralExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsExpression"/>.</returns>
            public override ITsExpression VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (node.Kind())
                {
                    case SyntaxKind.StringLiteralExpression:
                        // Use the raw text since C# strings are escaped the same as JavaScript strings.
                        string str = node.Token.Text;
                        bool isVerbatim = str.StartsWith("@", StringComparison.Ordinal);

                        // Trim the leading @ and surrounding quotes.
                        str = isVerbatim ? str.Substring(1) : str;
                        str = str.StartsWith("\"", StringComparison.Ordinal) ? str.Substring(1) : str;
                        str = str.EndsWith("\"", StringComparison.Ordinal) ? str.Substring(0, str.Length - 1) : str;

                        // For verbatim strings, we need to add the escape characters back in.
                        if (isVerbatim)
                        {
                            str = str.Replace(@"\", @"\\").Replace("\"\"", @"\""");
                        }

                        return Factory.String(str);

                    case SyntaxKind.CharacterLiteralExpression:
                        return Factory.String(node.Token.ValueText);

                    case SyntaxKind.NumericLiteralExpression:
                        return node.Token.Text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                            ? Factory.HexInteger(Convert.ToInt64(node.Token.Value))
                            : Factory.Number(Convert.ToDouble(node.Token.Value));

                    case SyntaxKind.TrueLiteralExpression:
                        return Factory.True;

                    case SyntaxKind.FalseLiteralExpression:
                        return Factory.False;

                    case SyntaxKind.NullLiteralExpression:
                        return Factory.Null;

                    default:
                        Diagnostics.Add(DiagnosticFactory.LiteralExpressionTranslationNotSupported(node));
                        return Factory.Null;
                }
            }

            //// =======================================================================================================
            //// Parenthesized, Cast, and TypeOf Expressions
            //// =======================================================================================================

            /// <summary>
            /// Called when the visitor visits a ParenthesizedExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsParenthesizedExpression"/>.</returns>
            public override ITsExpression VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
            {
                var expression = StartVisit(node.Expression);
                ITsParenthesizedExpression translated = Factory.ParenthesizedExpression(expression);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a CastExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsCastExpression"/>.</returns>
            public override ITsExpression VisitCastExpression(CastExpressionSyntax node)
            {
                ITsType castType = TypeTranslator.TranslateTypeSymbol(
                    Context,
                    Context.GetExpectedTypeSymbol(node.Type),
                    node.Type.GetLocation);

                var expression = Visit(node.Expression);
                bool isComplexExpression = expression is ITsAssignmentExpression;
                if (isComplexExpression)
                {
                    expression = expression.WithParentheses();
                }

                ITsCastExpression translated = Factory.Cast(castType, expression);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a TypeOfExpressionSyntax node.
            /// </summary>
            /// <remarks>An <see cref="ITsIdentifier"/>.</remarks>
            public override ITsExpression VisitTypeOfExpression(TypeOfExpressionSyntax node)
            {
                ITsType type = TypeTranslator.TranslateTypeSymbol(
                    Context,
                    Context.GetExpectedTypeSymbol(node.Type),
                    node.Type.GetLocation);

                ITsIdentifier translated = Factory.Identifier(type.EmitAsString());
                return translated;
            }

            //// =======================================================================================================
            //// Identifiers and Member Names
            //// =======================================================================================================

            /// <summary>
            /// Called when the visitor visits a PredefinedTypeSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsIdentifier"/>.</returns>
            public override ITsExpression VisitPredefinedType(PredefinedTypeSyntax node)
            {
                // Try to get the script name of the expression.
                ISymbol? symbol = SemanticModel.GetSymbolInfo(node).Symbol;

                // If there's no symbol then just return an identifier.
                string scriptName = node.Keyword.ValueText;
                if (symbol != null && ScriptSymbolTable.TryGetValue(symbol, out IScriptSymbol? scriptSymbol))
                {
                    scriptName = scriptSymbol.ComputedScriptName;
                }

                return Factory.Identifier(scriptName);
            }

            /// <summary>
            /// Called when the visitor visits a IdentifierNameSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsIdentifier"/>, <see cref="ITsMemberDotExpression"/>, or <see cref="IMemberAccessOmittedIdentifier"/>.</returns>
            public override ITsExpression VisitIdentifierName(IdentifierNameSyntax node)
            {
                // Try to get the script name of the expression.
                ISymbol? symbol = SemanticModel.GetSymbolInfo(node).Symbol;

                // If there's no symbol then just return an identifier.
                if (symbol == null)
                {
                    return Factory.Identifier(node.Identifier.Text);
                }

                if (ScriptSymbolTable.TryGetValue(symbol, out IScriptTypeSymbol? typeSymbol)
                    && typeSymbol.TreatMethodsAsGlobal)
                {
                    return Factory.MemberAccessOmittedIdentifier();  
                }

                ITsExpression translated = Context.TranslateIdentifierName(symbol, node);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a GenericNameSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsGenericTypeName"/>.</returns>
            public override ITsExpression VisitGenericName(GenericNameSyntax node)
            {
                var typeArguments = (from typeSyntax in node.TypeArgumentList.Arguments
                                     let typeSymbol = Context.GetExpectedTypeSymbol(typeSyntax)
                                     where typeSymbol != null
                                     select TypeTranslator.TranslateTypeSymbol(
                                         Context,
                                         typeSymbol,
                                         typeSyntax.GetLocation)).ToArray();

                ITsGenericTypeName translated = Factory.GenericTypeName(node.Identifier.Text, typeArguments);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a MemberAccessExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsExpression"/> which is usually, but not always, a <see cref="ITsMemberDotExpression"/>.</returns>
            public override ITsExpression VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                var leftSide = VisitSubExpression(node.Expression);

                ISymbol? symbol = SemanticModel.GetSymbolInfo(node).Symbol;

                IScriptTypeSymbol? typeSymbol = null;
                if (symbol != null)
                {
                    ScriptSymbolTable.TryGetValue(symbol, out typeSymbol);
                }

                if (typeSymbol != null && typeSymbol.TreatMethodsAsGlobal)
                {
                    // The right-hand side is a static class with a [GlobalMethods] attribute,
                    // which hoists class references up to the global namespace
                    // (omitting *all* other enclosing classes or namespaces),
                    // so we discard the whole expression.
                    return Factory.MemberAccessOmittedIdentifier();
                }

                // Get the script name - the symbol can be null if we're inside a dynamic scope since all
                // bets are off with the type checking.
                 string scriptName = symbol == null
                    ? node.Name.Identifier.Text
                    : ScriptSymbolTable.GetComputedScriptNameOrDefault(symbol, node.Name.Identifier.Text);

                // Is the left side a type or namespace that should be omitted here?
                if (leftSide is IMemberAccessOmittedIdentifier ||
                    (typeSymbol != null && typeSymbol.IgnoreNamespace))
                {
                    return Factory.Identifier(scriptName);
                }

                ITsMemberDotExpression translated = Factory.MemberDot(leftSide, scriptName);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a ElementAccessExpressionSyntax node.
            /// </summary>
            public override ITsExpression VisitElementAccessExpression(ElementAccessExpressionSyntax node)
            {
                ITsExpression leftSide = VisitSubExpression(node.Expression);
                ITsExpression bracketContents = TranslateBracketedArgumentList(node.ArgumentList);
                ITsMemberBracketExpression translation = Factory.MemberBracket(leftSide, bracketContents);
                return translation;
            }

            /// <summary>
            /// Called when the visitor visits a BracketedArgumentListSyntax node, of the form `[expression]`.
            /// </summary>
            private ITsExpression TranslateBracketedArgumentList(BracketedArgumentListSyntax node)
            {
                if (node.Arguments.Count > 1)
                {
                    Diagnostics.Add(DiagnosticFactory.ElementAccessWithMoreThanOneExpressionNotAllowed(node));
                }

                ITsArgument arg = TranslateArgument(node.Arguments[0]);
                return arg.Expression;
            }

            /// <summary>
            /// Called when the visitor visits a EqualsValueClauseSyntax node, which is an expression of the form `=
            /// expression`. It's used inside of a <see cref="VariableDeclaratorSyntax"/> when declaring variables.
            /// </summary>
            /// <returns>An <see cref="ITsExpression"/>.</returns>
            public override ITsExpression VisitEqualsValueClause(EqualsValueClauseSyntax node)
            {
                return VisitSubExpression(node.Value);
            }

            //// =======================================================================================================
            //// Array and Object Creation Expressions
            //// =======================================================================================================

            /// <summary>
            /// Called when the visitor visits a ArrayCreationExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsArrayLiteral"/>.</returns>
            public override ITsExpression VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
            {
                // Translate `new int[x]`
                if (node.Initializer == null)
                {
                    // TODO: Support multidimensional arrays `new int[x, y]` to `ss.multidimArray(0, x, y)`
                    if (node.Type.RankSpecifiers.Count > 1)
                    {
                        Diagnostics.Add(DiagnosticFactory.MultidimensionalArraysNotSupported(node.GetLocation()));
                        return Factory.Array();
                    }

                    var rankExpression = VisitSubExpression(node.Type.RankSpecifiers[0].Sizes[0]);

                    // Translate `new int[0]` to `[]`
                    if (rankExpression is ITsNumericLiteral literal && (int)literal.Value == 0)
                    {
                        return Factory.Array();
                    }

                    // Translate `new int[20]` to `new Array(20)`
                    ITsNewCallExpression newArrayCall = Factory.NewCall(
                        Factory.Identifier("Array"),
                        Factory.ArgumentList(Factory.Argument(rankExpression)));
                    return newArrayCall;
                }

                // Translate `new int[] { 1, 2, 3 }` to `[1, 2, 3]`
                ITsExpression[] arrayElements = node.Initializer.Expressions.Select(VisitSubExpression).ToArray();
                var translated = Factory.Array(arrayElements);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a ObjectCreationExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsNewCallExpression"/>.</returns>
            public override ITsExpression VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                var leftSide = VisitSubExpression(node.Type);

                // node.ArgumentList can be null in the case of the following pattern:
                // var x = new Thing { Prop = value; }
                ITsArgumentList arguments = Factory.ArgumentList();
                if (node.ArgumentList != null)
                {
                    arguments = TranslateArgumentList(node.ArgumentList);
                }

                if (node.Initializer != null)
                {
                    // TODO - need to support object creation of the form:
                    // var x = new Thing { Prop = value; }
                }

                // See if there's an [InlineCode] entry for the ctor invocation.
                if (SemanticModel.GetSymbolInfo(node).Symbol is IMethodSymbol ctorAsMethodSymbol &&
                    InlineCodeTranslator.TryTranslateMethodCall(
                        Context,
                        ctorAsMethodSymbol,
                        node.GetLocation(),
                        leftSide,
                        arguments,
                        out ITsExpression? translatedNode))
                {
                    return translatedNode;
                }

                if (JsDictionaryTranslator.TryTranslateObjectCreation(
                    Context,
                    node,
                    arguments,
                    out ITsObjectLiteral? translatedObjectLiteral))
                {
                    return translatedObjectLiteral;
                }

                ITsNewCallExpression translated = Factory.NewCall(leftSide, arguments);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a ImplicitArrayCreationExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsArrayLiteral"/>.</returns>
            public override ITsExpression VisitImplicitArrayCreationExpression(
                ImplicitArrayCreationExpressionSyntax node)
            {
                var elements = node.Initializer.Expressions.Select(Visit).ToArray();
                ITsArrayLiteral translated = Factory.Array(elements);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a DefaultExpressionSyntax node.
            /// </summary>
            /// <returns>
            /// An <see cref="ITsCallExpression"/>, since `default(T)` gets translated as a call to
            /// `ss.getDefaultValue(T)`.
            /// </returns>
            public override ITsExpression VisitDefaultExpression(DefaultExpressionSyntax node)
            {
                ITsType translatedType = TypeTranslator.TranslateTypeSymbol(
                    Context,
                    Context.GetExpectedTypeSymbol(node.Type),
                    node.Type.GetLocation);

                ITsCallExpression translated = Factory.Call(
                    Factory.MemberDot(Factory.Identifier("ss"), "getDefaultValue"),
                    Factory.ArgumentList(Factory.Argument(Factory.Identifier(translatedType.EmitAsString()))));
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a InitializerExpressionSyntax node.
            /// </summary>
            /// <returns></returns>
            public override ITsExpression VisitInitializerExpression(InitializerExpressionSyntax node)
            {
                // TODO: Support initializer expressions (#79)
                return VisitSubExpression(node.Expressions.First());
            }

            //// =======================================================================================================
            //// Assignments and Conditional Expressions
            //// =======================================================================================================

            /// <summary>
            /// Called when the visitor visits a AssignmentExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsAssignmentExpression"/>.</returns>
            public override ITsExpression VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                var leftSide = VisitSubExpression(node.Left);
                var rightSide = VisitSubExpression(node.Right);

                ITsAssignmentExpression translated = Factory.Assignment(
                    leftSide,
                    TranslateAssignmentOperator(node.OperatorToken),
                    rightSide);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a ConditionalExpressionSyntax node.
            /// </summary>
            /// <returns>An <see cref="ITsConditionalExpression"/>.</returns>
            public override ITsExpression VisitConditionalExpression(ConditionalExpressionSyntax node)
            {
                var condition = VisitSubExpression(node.Condition);
                var whenTrue = VisitSubExpression(node.WhenTrue);
                var whenFalse = VisitSubExpression(node.WhenFalse);

                ITsConditionalExpression translated = Factory.Conditional(condition, whenTrue, whenFalse);
                return translated;
            }

            private TsAssignmentOperator TranslateAssignmentOperator(SyntaxToken operatorToken)
            {
                TsAssignmentOperator? op = operatorToken.Kind() switch
                {
                    SyntaxKind.EqualsToken => TsAssignmentOperator.SimpleAssign,
                    SyntaxKind.AsteriskEqualsToken => TsAssignmentOperator.MultiplyAssign,
                    SyntaxKind.SlashEqualsToken => TsAssignmentOperator.DivideAssign,
                    SyntaxKind.PercentEqualsToken => TsAssignmentOperator.ModuloAssign,
                    SyntaxKind.PlusEqualsToken => TsAssignmentOperator.AddAssign,
                    SyntaxKind.MinusEqualsToken => TsAssignmentOperator.SubtractAssign,
                    SyntaxKind.LessThanLessThanEqualsToken => TsAssignmentOperator.LeftShiftAssign,
                    SyntaxKind.GreaterThanGreaterThanEqualsToken => TsAssignmentOperator.SignedRightShiftAssign,
                    SyntaxKind.AmpersandEqualsToken => TsAssignmentOperator.BitwiseAndAssign,
                    SyntaxKind.CaretEqualsToken => TsAssignmentOperator.BitwiseXorAssign,
                    SyntaxKind.BarEqualsToken => TsAssignmentOperator.BitwiseOrAssign,
                    _ => null,
                };

                if (op == null)
                {
                    Diagnostics.Add(DiagnosticFactory.OperatorKindNotSupported(operatorToken));
                }

                return op.GetValueOrDefault(TsAssignmentOperator.SimpleAssign);
            }

            //// =======================================================================================================
            //// Function and Method Expressions
            //// =======================================================================================================

            /// <summary>
            /// Called when the visitor visits a InvocationExpressionSyntax node of the form `expression.method(args)`.
            /// </summary>
            /// <returns>
            /// An <see cref="ITsCallExpression"/> or a <see cref="ITsExpression"/> if there is a
            /// [ScriptSkip] attribute.
            /// </returns>
            public override ITsExpression VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                ITsExpression leftSide = VisitSubExpression(node.Expression);
                ITsArgumentList arguments = TranslateArgumentList(node.ArgumentList);

                // Get the method symbol, which is either a constructor or "normal" method.
                if (!(SemanticModel.GetSymbolInfo(node).Symbol is IMethodSymbol methodSymbol))
                {
                    // For dynamic invocations, there isn't a symbol since the compiler can't tell what it is.
                    if (SemanticModel.GetTypeInfo(node).Type?.TypeKind != TypeKind.Dynamic)
                    {
                        Context.ReportInternalError("Isn't an invocation always a method symbol?", node);
                    }

                    return Factory.Call(leftSide, arguments);
                }

                // Try to adapt the method if it's an extension method (convert it from `x.Extension()` to
                // `ExtensionClass.Extension(x)`. This must be done first before translating [InlineCode] or
                // [ScriptSkip] methods.
                ExtensionMethodTranslator.TryAdaptMethodInvocation(
                    Context,
                    node,
                    ref methodSymbol,
                    ref leftSide,
                    ref arguments);

                // Check [ScriptSkip] before [InlineCode]. If a method is marked with both, [ScriptSkip] takes precedence
                // and there's no need to use [InlineCode].
                if (ScriptSkipTranslator.TryTranslateInvocationExpression(
                    Context,
                    node,
                    methodSymbol,
                    leftSide,
                    arguments,
                    out ITsExpression? translatedExpression))
                {
                    return translatedExpression;
                }

                // If the node's left side expression is a method or a constructor, then it will have already been
                // translated and the [InlineCode] would have already been applied - we shouldn't do it twice because it
                // will be wrong the second time.
                bool hasLeftSideAlreadyBeenTranslatedWithInlineCode = node.Expression.Kind()
                    .IsOneOf(SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);

                // See if there's an [InlineCode] entry for the method invocation.
                if (!hasLeftSideAlreadyBeenTranslatedWithInlineCode &&
                    InlineCodeTranslator.TryTranslateMethodCall(
                        Context,
                        methodSymbol,
                        node.Expression.GetLocation(),
                        leftSide,
                        arguments,
                        out ITsExpression? translatedNode))
                {
                    return translatedNode;
                }

                ITsCallExpression translatedCallExpression = Factory.Call(leftSide, arguments);
                return translatedCallExpression;
            }

            /// <summary>
            /// Called when the visitor visits a AnonymousMethodExpressionSyntax node, which is a node of the form
            /// <c><![CDATA[(int x, string y) => { statements }]]></c>.
            /// </summary>
            /// <returns>An <see cref="ITsArrowFunction"/>.</returns>
            public override ITsExpression VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
            {
                ITsCallSignature callSignature = FunctionTranslator.TranslateCallSignature(Context, node.ParameterList);
                var body = StatementTranslator.TranslateBlockStatement(Context, node.Block);
                ITsArrowFunction translated = Factory.ArrowFunction(callSignature, body.Statements.ToArray());
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a SimpleLambdaExpressionSyntax node, which is a node of the form
            /// <c><![CDATA[x => expression]]></c>.
            /// </summary>
            /// <returns>An <see cref="ITsArrowFunction"/>.</returns>
            public override ITsExpression VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                ITsIdentifier singleParameterName = Factory.Identifier(node.Parameter.Identifier.ValueText);
                var body = VisitSubExpression(node.Body);

                ITsArrowFunction translated = Factory.ArrowFunction(singleParameterName, body);
                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a ParenthesizedLambdaExpressionSyntax node, which is a node of the form
            /// <c><![CDATA[(x, y) => expression]]></c> or <c><![CDATA[(x, y) => { statements }]]></c>.
            /// </summary>
            /// <returns>An <see cref="ITsArrowFunction"/>.</returns>
            public override ITsExpression VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                ITsCallSignature callSignature = FunctionTranslator.TranslateCallSignature(Context, node.ParameterList);

                ITsArrowFunction translated;
                if (node.ExpressionBody != null)
                {
                    var bodyExpression = VisitSubExpression(node.ExpressionBody);
                    translated = Factory.ArrowFunction(callSignature, bodyExpression);
                }
                else if (node.Block != null)
                {
                    var bodyBlock = StatementTranslator.TranslateBlockStatement(Context, node.Block);
                    translated = Factory.ArrowFunction(callSignature, bodyBlock.Statements.ToArray());
                }
                else
                {
                    Context.ReportInternalError($"Unknown lambda expression body type: {node.Body}", node.Body);
                    translated = Factory.ArrowFunction(callSignature, Factory.Identifier("TranslationError"));
                }

                return translated;
            }

            /// <summary>
            /// Called when the visitor visits a ArrowExpressionClauseSyntax node, which is the right side of the arrow
            /// in a property or method body expression.
            /// </summary>
            /// <returns>An <see cref="ITsExpression"/>.</returns>
            public override ITsExpression VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
            {
                return VisitSubExpression(node.Expression);
            }

            //// =======================================================================================================
            //// Arguments and Parameters
            //// =======================================================================================================

            private ITsArgumentList TranslateArgumentList(ArgumentListSyntax node)
            {
                var translatedArguments = node.Arguments.Select(TranslateArgument).ToList();
                if (!TryTranslateParamsArgument(node, translatedArguments, out ITsArgumentList? translated))
                {
                    translated = Factory.ArgumentList(translatedArguments.ToArray());
                }

                return translated;
            }

            private ITsArgument TranslateArgument(ArgumentSyntax node)
            {
                ITsExpression expression = VisitSubExpression(node.Expression);
                ITsArgument argument = Factory.Argument(expression);
                return argument;
            }

            private bool TryTranslateParamsArgument(
                ArgumentListSyntax node,
                IReadOnlyList<ITsArgument> translatedArguments,
                [NotNullWhen(true)] out ITsArgumentList? translatedArgumentList)
            {
                SyntaxNode methodNode = node.Parent;
                var methodSymbol = SemanticModel.GetSymbolInfo(methodNode).Symbol as IMethodSymbol;
                bool isParamsArgument = methodSymbol?.Parameters.LastOrDefault()?.IsParams == true;

                // If the last argument isn't a `params` argument, we don't need to do any special processing, so exit early.
                if (methodSymbol == null || !isParamsArgument)
                {
                    translatedArgumentList = null;
                    return false;
                }

                // If this method implements an interface, we use the interface's [ExpandParams] (or lack of)
                // instead of the method's.
                IScriptMethodSymbol scriptMethodSymbol = Context.GetExpectedScriptSymbol<IScriptMethodSymbol>(
                    methodSymbol.TryFindInterfaceMethodOfImplementingMethod(out IMethodSymbol? interfaceMethodSymbol)
                        ? interfaceMethodSymbol
                        : methodSymbol,
                    methodNode);

                // Create a copy of the translated arguments, since we may modify it.
                var convertedArgs = new List<ITsArgument>(translatedArguments);

                // We need to convert the 'params' arguments into an array if one of the following are true:
                // * The method (or interface method) is marked with [InlineCode], which always requires an array even
                //   if the [ExpandParams] is set.
                // * The [ExpandParams] is not set on the method (or the interface method, which takes precedence).
                bool convertToArray = scriptMethodSymbol.InlineCode != null || !scriptMethodSymbol.ExpandParams;
                bool isLastArgumentAlreadyArray = translatedArguments.Last().Expression is ITsArrayLiteral;
                if (convertToArray && !isLastArgumentAlreadyArray)
                {
                    int indexOfParams = methodSymbol.Parameters.Length - 1;

                    // Take the translated arguments starting at the index of the params and convert them into an array.
                    ITsArrayLiteral array = Factory.Array(
                        translatedArguments.Skip(indexOfParams).Select(arg => Factory.ArrayElement(arg.Expression)).ToArray());

                    convertedArgs.RemoveRange(indexOfParams, array.Elements.Length);
                    convertedArgs.Add(Factory.Argument(array));
                }

                // In C#, you can pass in an array into a 'params' argument. If the [ExpandParams] has been set, then we
                // need to flatten the array into individual arguments.
                if (!convertToArray && isLastArgumentAlreadyArray)
                {
                    var lastArray = (ITsArrayLiteral)translatedArguments.Last().Expression;

                    // Remove the array from the converted args.
                    convertedArgs.RemoveAt(convertedArgs.Count - 1);

                    foreach (ITsArrayElement? arrayElement in lastArray.Elements)
                    {
                        if (arrayElement == null)
                        {
                            Context.ReportInternalError(
                                "There should never be an uninitialized array element since it can't happen in C#",
                                node);
                        }

                        convertedArgs.Add(Factory.Argument(arrayElement?.Expression ?? Factory.Null));
                    }
                }

                translatedArgumentList = Factory.ArgumentList(convertedArgs.ToArray());
                return true;
            }
        }
    }
}
