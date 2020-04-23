﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionMethodTranslator.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Translation
{
    using System;
    using System.Linq;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.SymbolTables;
    using Desalt.TypeScriptAst.Ast;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Recognizes and adapts extension method invocations to a standard TypeScript/JavaScript static method invocation.
    /// </summary>
    internal class ExtensionMethodTranslator
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        private readonly SemanticModel _semanticModel;
        private readonly ScriptSymbolTable _scriptSymbolTable;

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        /// <summary>
        /// Creates a new instance of a <see cref="ScriptSkipTranslator"/> from the specified
        /// semantic model and symbol tables.
        /// </summary>
        /// <param name="semanticModel">The semantic model to use.</param>
        /// <param name="scriptSymbolTable"> A symbol table containing script names given a symbol.</param>
        public ExtensionMethodTranslator(SemanticModel semanticModel, ScriptSymbolTable scriptSymbolTable)
        {
            _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

            _scriptSymbolTable =
                scriptSymbolTable ?? throw new ArgumentNullException(nameof(scriptSymbolTable));
        }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        /// <summary>
        /// Tries to adapt an already-translated method invocation expression that may contain an extension method from
        /// `x.Extension()` to `ExtensionClass.Extension(x)`.
        /// </summary>
        /// <param name="node">The <see cref="InvocationExpressionSyntax"/> node to translate.</param>
        /// <param name="methodSymbol">
        /// The method symbol of the invocation. This will be changed to the non-reduced method symbol if the adaptation occurred.
        /// </param>
        /// <param name="translatedLeftSide">
        /// The already-translated left side of the invocation expression. This will be changed to
        /// `ExtensionClass.Extension` if the adaptation occurred.
        /// </param>
        /// <param name="translatedArgumentList">
        /// The already-translated argument list for the method invocation. This will be changed to have the former left
        /// side of the expression as the first argument if the adaptation occurred.
        /// </param>
        /// <param name="translateIdentifierNameFunc">
        /// The function to call in order to translate the identifier needed to invoke the message, which is the name of
        /// the containing class of the extension method.
        /// </param>
        /// <param name="error">Contains any potential error while attempting to adapt the invocation expression.</param>
        /// <returns>
        /// True if the node was adapted (it was an extension method invoked as `x.Extension()`); false if the node was
        /// not adapted or if there was an error.
        /// </returns>
        public bool TryAdaptMethodInvocation(
            InvocationExpressionSyntax node,
            ref IMethodSymbol methodSymbol,
            ref ITsExpression translatedLeftSide,
            ref ITsArgumentList translatedArgumentList,
            TranslateIdentifierFunc translateIdentifierNameFunc,
            out Diagnostic? error)
        {
            // See if this is an extension method invoked as `receiver.Extension()` and change the call signature so
            // that the left side is the first argument to the static method.
            if (!methodSymbol.IsExtensionMethod || methodSymbol.ReducedFrom == null)
            {
                error = null;
                return false;
            }

            if (!(translatedLeftSide is ITsMemberDotExpression memberDotExpression))
            {
                error = DiagnosticFactory.InternalError(
                    "Translating an extension method that doesn't start with a member dot expression is " +
                    "currently not supported, since I couldn't think of a way this could be.",
                    node.GetLocation());
                return false;
            }

            // Get the non-reduced form of the method symbol. For example, if `static void Extension(this string s)`
            // is the original method, an invocation of the form `s.Extension()` would have the symbol
            // `System.String.Extension()`
            methodSymbol = methodSymbol.ReducedFrom;

            // Translate the name of the reduced type, which is the new left side of the invocation:
            // `x.Extension()` -> `ExtensionClass.Extension(x)`.
            translatedLeftSide = translateIdentifierNameFunc(methodSymbol, node);

            // Take the left side of the expression and instead make it the first argument to the static
            // method invocation: `x.Extension()` -> `ExtensionClass.Extension(x)`.
            translatedArgumentList = TsAstFactory.ArgumentList(
                translatedArgumentList.TypeArguments,
                translatedArgumentList.Arguments.Insert(0, TsAstFactory.Argument(memberDotExpression.LeftSide)).ToArray());

            error = null;
            return true;
        }
    }
}
