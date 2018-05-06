﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="InlineCodeTranslator.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Translation
{
    using System;
    using System.Linq;
    using System.Text;
    using Desalt.Core.TypeScript.Ast;
    using Desalt.Core.TypeScript.Parsing;
    using Desalt.Core.Utility;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Parses and translates [InlineCode] attribute contents.
    /// </summary>
    internal class InlineCodeTranslator
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        private readonly SemanticModel _semanticModel;
        private readonly InlineCodeSymbolTable _inlineCodeSymbolTable;
        private readonly ScriptNameSymbolTable _scriptNameSymbolTable;

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        /// <summary>
        /// Creates a new instance of a <see cref="InlineCodeTranslator"/> from the specified
        /// semantic model and symbol tables.
        /// </summary>
        /// <param name="semanticModel">The semantic model to use.</param>
        /// <param name="inlineCodeSymbolTable">
        /// A symbol table containing [InlineCode] attributes for various symbols.
        /// </param>
        /// <param name="scriptNameSymbolTable">
        /// A symbol table containing script names given a symbol. Used for {$Namespace.Type}
        /// parameter substitutions.
        /// </param>
        public InlineCodeTranslator(
            SemanticModel semanticModel,
            InlineCodeSymbolTable inlineCodeSymbolTable,
            ScriptNameSymbolTable scriptNameSymbolTable)
        {
            _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

            _inlineCodeSymbolTable =
                inlineCodeSymbolTable ?? throw new ArgumentNullException(nameof(inlineCodeSymbolTable));

            _scriptNameSymbolTable =
                scriptNameSymbolTable ?? throw new ArgumentNullException(nameof(scriptNameSymbolTable));
        }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        /// <summary>
        /// Attempts to translate the method call by using the specified [InlineCode]. A method call
        /// can be either a constructor, regular method, or a property get/set method. If the inline
        /// code cannot be parsed, <see langword="null"/> is returned.
        /// </summary>
        /// <param name="methodExpressionSyntax">The method expression to translate.</param>
        /// <param name="translatedLeftSide">
        /// The translated left side of the method call. Used for {this} parameter substitution.
        /// </param>
        /// <param name="translatedArgumentList">
        /// The translated argument list associated with this method.
        /// </param>
        /// <param name="translatedNode">
        /// The translated TypeScript code or null if no translation is possible (an error condition).
        /// </param>
        /// <returns>
        /// True if the translation happend or false if no translation is possible (an error condition).
        /// </returns>
        public bool TryTranslate(
            ExpressionSyntax methodExpressionSyntax,
            ITsExpression translatedLeftSide,
            ITsArgumentList translatedArgumentList,
            out IAstNode translatedNode)
        {
            // see if there's an [InlineCode] entry for the method invocation
            if (_semanticModel.GetSymbolInfo(methodExpressionSyntax).Symbol is IMethodSymbol methodSymbol && _inlineCodeSymbolTable.TryGetValue(methodSymbol, out string inlineCode))
            {
                var context = new Context(
                    inlineCode,
                    methodExpressionSyntax,
                    methodSymbol,
                    translatedLeftSide,
                    translatedArgumentList);

                translatedNode = Translate(context);
                return true;
            }

            translatedNode = null;
            return false;
        }

        private IAstNode Translate(Context context)
        {
            string replacedInlineCode = ReplaceParameters(context);
            ITsExpression parsedExpression = TsParser.ParseExpression(replacedInlineCode);
            return parsedExpression;
        }

        private string ReplaceParameters(Context context)
        {
            var builder = new StringBuilder();

            using (var reader = new PeekingTextReader(context.InlineCode))
            {
                // define a local helper function to read an expected character
                void Read(char expected)
                {
                    int read = reader.Read();
                    if (read != expected)
                    {
                        throw context.CreateParseException($"Expected to read '{expected}'.");
                    }

                    reader.SkipWhitespace();
                }

                while (!reader.IsAtEnd)
                {
                    // read all of the text until we hit the start of a parameter
                    builder.Append(reader.ReadUntil('{'));

                    // check for an escaped brace
                    if (reader.Peek(2) == "{{")
                    {
                        reader.Read(2);
                        builder.Append('{');
                    }
                    else if (!reader.IsAtEnd)
                    {
                        Read('{');
                        string parameterName = reader.ReadUntil('}');

                        // check for an escaped brace
                        while (reader.Peek(2) == "}}")
                        {
                            parameterName += "}" + reader.ReadUntil('}');
                        }

                        Read('}');

                        string replacedValue = ReplaceParameter(parameterName, context);
                        builder.Append(replacedValue);
                    }
                }
            }

            return builder.ToString();
        }

        private string ReplaceParameter(string parameterName, Context context)
        {
            if (parameterName[0] == '$')
            {
                return FindScriptNameOfType(parameterName.Substring(1), context);
            }

            // a parameter of the form '*rest' means to expand the parameter array
            if (parameterName[0] == '*')
            {
                return ExpandParams(parameterName.Substring(1), context);
            }

            if (parameterName == "this")
            {
                // get the expression that should be substituted for the 'this' instance, which is
                // everything to the left of a member.dot expression
                switch (context.TranslatedLeftSide)
                {
                    case ITsMemberDotExpression memberDotExpression:
                        return memberDotExpression.LeftSide.EmitAsString();

                    default:
                        return context.TranslatedLeftSide.EmitAsString();
                }
            }

            // find the translated parameter and use it for substitution
            int index = FindIndexOfParameter(parameterName, context);
            var translatedArgument = context.TranslatedArgumentList.Arguments[index];

            return translatedArgument.EmitAsString();
        }

        private string FindScriptNameOfType(string fullTypeName, Context context)
        {
            // try to resolve the type
            TypeSyntax typeSyntax = SyntaxFactory.ParseTypeName(fullTypeName);

            if (typeSyntax == null)
            {
                throw context.CreateParseException($"Cannot parse '{fullTypeName}' as a type name");
            }

            ITypeSymbol typeSymbol = _semanticModel.GetSpeculativeTypeInfo(
                    context.MethodExpressionSyntax.SpanStart,
                    typeSyntax,
                    SpeculativeBindingOption.BindAsTypeOrNamespace)
                .Type;

            if (typeSymbol == null || typeSymbol is IErrorTypeSymbol)
            {
                throw context.CreateParseException($"Cannot resolve '{fullTypeName}' to a single type symbol");
            }

            if (_scriptNameSymbolTable.TryGetValue(typeSymbol, out string scriptName))
            {
                return scriptName;
            }

            throw context.CreateParseException($"Cannot find '{typeSymbol}' in the ScriptName symbol table");
        }

        private static string ExpandParams(string parameterName, Context context)
        {
            // find the index of the translated param
            int index = FindIndexOfParameter(parameterName, context);

            // a parameter of the form '*rest' means to expand the parameter array
            if (!context.MethodSymbol.Parameters[index].IsParams)
            {
                throw context.CreateParseException($"Parameter '{parameterName}' is not a 'params' parameter.");
            }

            var builder = new StringBuilder();
            foreach (ITsArgument translatedValue in context.TranslatedArgumentList.Arguments.Skip(index))
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(translatedValue.Argument.EmitAsString());
            }

            return builder.ToString();
        }

        private static int FindIndexOfParameter(string parameterName, Context context)
        {
            // find the position of the parameter in the parameter list
            IParameterSymbol foundParameter =
                context.MethodSymbol.Parameters.FirstOrDefault(parameter => parameter.Name == parameterName);
            if (foundParameter == null)
            {
                throw context.CreateParseException($"Cannot find parameter '{parameterName}' in the method");
            }

            int index = context.MethodSymbol.Parameters.IndexOf(foundParameter);

            // find the translated parameter and use it for substitution
            if (index >= context.TranslatedArgumentList.Arguments.Length)
            {
                throw context.CreateParseException(
                    $"Cannot find parameter '{parameterName}' in the translated argument list '{context.TranslatedArgumentList.EmitAsString()}'");
            }

            return index;
        }

        //// ===========================================================================================================
        //// Classes
        //// ===========================================================================================================

        private sealed class Context
        {
            public Context(
                string inlineCode,
                ExpressionSyntax methodExpressionSyntax,
                IMethodSymbol methodSymbol,
                ITsExpression translatedLeftSide,
                ITsArgumentList translatedArgumentList)
            {
                InlineCode = inlineCode ?? throw new ArgumentNullException(nameof(inlineCode));
                MethodExpressionSyntax = methodExpressionSyntax ??
                    throw new ArgumentNullException(nameof(methodExpressionSyntax));

                MethodSymbol = methodSymbol ?? throw new ArgumentNullException(nameof(methodSymbol));
                TranslatedLeftSide = translatedLeftSide ?? throw new ArgumentNullException(nameof(translatedLeftSide));
                TranslatedArgumentList = translatedArgumentList ??
                    throw new ArgumentNullException(nameof(translatedArgumentList));
            }

            public string InlineCode { get; }
            public ExpressionSyntax MethodExpressionSyntax { get; }
            public IMethodSymbol MethodSymbol { get; }
            public ITsExpression TranslatedLeftSide { get; }
            public ITsArgumentList TranslatedArgumentList { get; }

            public Exception CreateParseException(string message)
            {
                return new InvalidOperationException(
                    $"Error parsing inline code '{InlineCode}' for '{SymbolTableUtils.KeyFromSymbol(MethodSymbol)}': {message}");
            }
        }
    }
}