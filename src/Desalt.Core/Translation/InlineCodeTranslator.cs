// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="InlineCodeTranslator.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Translation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using Desalt.CompilerUtilities;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.SymbolTables;
    using Desalt.Core.Utility;
    using Desalt.TypeScriptAst.Ast;
    using Desalt.TypeScriptAst.Parsing;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Parses and translates [InlineCode] attribute contents.
    /// </summary>
    internal static class InlineCodeTranslator
    {
        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        /// <summary>
        /// Attempts to translate the method call by using the specified [InlineCode]. A method call can be either a
        /// constructor, regular method, or a property get/set method. If the inline code cannot be parsed, <see
        /// langword="null"/> is returned.
        /// </summary>
        /// <param name="context">The <see cref="TranslationContext"/> to use.</param>
        /// <param name="methodSymbol">The symbol of the method to translate.</param>
        /// <param name="methodExpressionLocation">The location of the method expression to translate.</param>
        /// <param name="translatedLeftSide">
        /// The translated left side of the method call. Used for {this} parameter substitution.
        /// </param>
        /// <param name="translatedArgumentList">The translated argument list associated with this method.</param>
        /// <param name="translatedNode">
        /// The translated TypeScript code or null if no translation is possible (an error condition).
        /// </param>
        /// <returns>True if the translation happened or false if no translation is possible (an error condition).</returns>
        public static bool TryTranslateMethodCall(
            TranslationContext context,
            IMethodSymbol methodSymbol,
            Location methodExpressionLocation,
            ITsExpression translatedLeftSide,
            ITsArgumentList translatedArgumentList,
            [NotNullWhen(true)] out ITsExpression? translatedNode)
        {
            // See if there's an [InlineCode] entry for the method invocation.
            if (!context.ScriptSymbolTable.TryGetValue(methodSymbol, out IScriptMethodSymbol? methodScriptSymbol) ||
                methodScriptSymbol.InlineCode == null)
            {
                translatedNode = null;
                return false;
            }

            var inlineCodeContext = new InlineCodeContext(
                context,
                methodScriptSymbol.InlineCode,
                methodExpressionLocation,
                methodSymbol,
                translatedLeftSide,
                translatedArgumentList);

            translatedNode = Translate(inlineCodeContext);
            return translatedNode != null;
        }

        private static ITsExpression Translate(InlineCodeContext inlineCodeContext)
        {
            string replacedInlineCode = ReplaceParameters(inlineCodeContext);

            try
            {
                ITsExpression parsedExpression = TsParser.ParseExpression(replacedInlineCode);
                return parsedExpression;
            }
            catch (TsParserException e)
            {
                inlineCodeContext.AddParseError(e.Message);
                return inlineCodeContext.TranslatedLeftSide;
            }
        }

        private static string ReplaceParameters(InlineCodeContext inlineCodeContext)
        {
            var builder = new StringBuilder();

            using (var reader = new PeekingTextReader(inlineCodeContext.InlineCode))
            {
                // Define a local helper function to read an expected character.
                void Read(char expected)
                {
                    int read = reader.Read();
                    if (read != expected)
                    {
                        inlineCodeContext.AddParseError($"Expected to read '{expected}'.");
                    }
                }

                while (!reader.IsAtEnd)
                {
                    // Read all of the text until we hit the start of a parameter.
                    builder.Append(reader.ReadUntil('{'));

                    // Check for an escaped brace.
                    if (reader.Peek(2) == "{{")
                    {
                        reader.Read(2);
                        builder.Append('{');
                    }
                    else if (!reader.IsAtEnd)
                    {
                        Read('{');
                        string? parameterName = reader.ReadUntil('}');

                        // Check for an escaped brace.
                        while (reader.Peek(2) == "}}")
                        {
                            parameterName += "}" + reader.ReadUntil('}');
                        }

                        Read('}');

                        if (parameterName == null)
                        {
                            throw new InvalidOperationException(
                                "The Saltarelle compiler should have caught the invalid parameter syntax.");
                        }

                        string replacedValue = ReplaceParameter(parameterName, inlineCodeContext);
                        builder.Append(replacedValue);
                    }
                }
            }

            return builder.ToString();
        }

        private static string ReplaceParameter(string parameterName, InlineCodeContext inlineCodeContext)
        {
            if (parameterName[0] == '$')
            {
                return FindScriptNameOfType(parameterName.Substring(1), inlineCodeContext);
            }

            // A parameter of the form '*rest' means to expand the parameter array.
            if (parameterName[0] == '*')
            {
                return ExpandParams(parameterName.Substring(1), inlineCodeContext);
            }

            if (parameterName == "this")
            {
                // Get the expression that should be substituted for the 'this' instance, which is
                // everything to the left of a member.dot expression.
                switch (inlineCodeContext.TranslatedLeftSide)
                {
                    case ITsMemberDotExpression memberDotExpression:
                        return memberDotExpression.LeftSide.EmitAsString();

                    default:
                        return inlineCodeContext.TranslatedLeftSide.EmitAsString();
                }
            }

            // Find the translated parameter and use it for substitution.
            int index = TryFindIndexOfParameter(parameterName, inlineCodeContext, out Diagnostic? diagnostic);
            if (diagnostic != null)
            {
                inlineCodeContext.TranslationContext.Diagnostics.Add(diagnostic);
            }

            if (index < 0)
            {
                return parameterName;
            }

            ITsArgument translatedArgument = inlineCodeContext.TranslatedArgumentList.Arguments[index];
            return translatedArgument.EmitAsString();
        }

        private static string FindScriptNameOfType(string fullTypeName, InlineCodeContext inlineCodeContext)
        {
            // Try to resolve the type.
            TypeSyntax typeSyntax = SyntaxFactory.ParseTypeName(fullTypeName);

            if (typeSyntax == null)
            {
                inlineCodeContext.AddParseError($"Cannot parse '{fullTypeName}' as a type name");
                return fullTypeName;
            }

            ITypeSymbol? typeSymbol = inlineCodeContext.TranslationContext.SemanticModel.GetSpeculativeTypeInfo(
                    inlineCodeContext.MethodExpressionLocation.SourceSpan.Start,
                    typeSyntax,
                    SpeculativeBindingOption.BindAsTypeOrNamespace)
                .Type;

            if (typeSymbol == null || typeSymbol is IErrorTypeSymbol)
            {
                inlineCodeContext.AddParseError($"Cannot resolve '{fullTypeName}' to a single type symbol");
                return fullTypeName;
            }

            if (inlineCodeContext.TranslationContext.ScriptSymbolTable.TryGetValue(
                typeSymbol,
                out IScriptSymbol? scriptSymbol))
            {
                return scriptSymbol.ComputedScriptName;
            }

            inlineCodeContext.AddParseError($"Cannot find '{typeSymbol}' in the ScriptName symbol table");
            return fullTypeName;
        }

        private static string ExpandParams(string parameterName, InlineCodeContext inlineCodeContext)
        {
            // Find the index of the translated param.
            int index = TryFindIndexOfParameter(parameterName, inlineCodeContext, out _);

            // If there are no more parameters, then there's nothing to expand.
            if (index < 0)
            {
                return string.Empty;
            }

            // A parameter of the form '*rest' means to expand the parameter array.
            if (!inlineCodeContext.MethodSymbol.Parameters[index].IsParams)
            {
                inlineCodeContext.AddParseError($"Parameter '{parameterName}' is not a 'params' parameter.");
                return parameterName;
            }

            if (!(inlineCodeContext.TranslatedArgumentList.Arguments[^1].Expression is ITsArrayLiteral translatedArray))
            {
                inlineCodeContext.AddParseError($"Parameter '{parameterName}' does not correspond to an array.");
                return parameterName;
            }

            var builder = new StringBuilder();
            foreach (ITsArrayElement? translatedValue in translatedArray.Elements)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                if (translatedValue != null)
                {
                    builder.Append(translatedValue.Expression.EmitAsString());
                }
            }

            return builder.ToString();
        }

        private static int TryFindIndexOfParameter(
            string parameterName,
            InlineCodeContext inlineCodeContext,
            out Diagnostic? diagnostic)
        {
            // Find the position of the parameter in the parameter list.
            IParameterSymbol foundParameter =
                inlineCodeContext.MethodSymbol.Parameters.FirstOrDefault(parameter => parameter.Name == parameterName);
            if (foundParameter == null)
            {
                diagnostic = inlineCodeContext.CreateParseError($"Cannot find parameter '{parameterName}' in the method");
                return -1;
            }

            int index = inlineCodeContext.MethodSymbol.Parameters.IndexOf(foundParameter);

            // Find the translated parameter and use it for substitution.
            if (index >= inlineCodeContext.TranslatedArgumentList.Arguments.Length)
            {
                diagnostic = inlineCodeContext.CreateParseError(
                    $"Cannot find parameter '{parameterName}' in the translated argument list '{inlineCodeContext.TranslatedArgumentList.EmitAsString()}'");
                return -1;
            }

            diagnostic = null;
            return index;
        }

        //// ===========================================================================================================
        //// Classes
        //// ===========================================================================================================

        private sealed class InlineCodeContext
        {
            public InlineCodeContext(
                TranslationContext translationContext,
                string inlineCode,
                Location methodExpressionLocation,
                IMethodSymbol methodSymbol,
                ITsExpression translatedLeftSide,
                ITsArgumentList translatedArgumentList)
            {
                TranslationContext = translationContext;
                InlineCode = inlineCode;
                MethodExpressionLocation = methodExpressionLocation;

                MethodSymbol = methodSymbol;
                TranslatedLeftSide = translatedLeftSide;
                TranslatedArgumentList = translatedArgumentList;
            }

            public TranslationContext TranslationContext { get; }
            public string InlineCode { get; }
            public Location MethodExpressionLocation { get; }
            public IMethodSymbol MethodSymbol { get; }
            public ITsExpression TranslatedLeftSide { get; }
            public ITsArgumentList TranslatedArgumentList { get; }

            public void AddParseError(string message)
            {
                TranslationContext.Diagnostics.Add(CreateParseError(message));
            }

            public Diagnostic CreateParseError(string message)
            {
                return DiagnosticFactory.InlineCodeParsingError(
                    InlineCode,
                    MethodSymbol.ToHashDisplay(),
                    message,
                    MethodExpressionLocation);
            }
        }
    }
}
