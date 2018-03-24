﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TranslationVisitor.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Translation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.Extensions;
    using Desalt.Core.TypeScript.Ast;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Factory = Desalt.Core.TypeScript.Ast.TsAstFactory;

    /// <summary>
    /// Visits a C# syntax tree, translating from a C# AST into a TypeScript AST.
    /// </summary>
    internal sealed partial class TranslationVisitor : CSharpSyntaxVisitor<IEnumerable<IAstNode>>
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        private readonly DiagnosticList _diagnostics;
        private readonly DocumentTranslationContextWithSymbolTables _context;
        private readonly SemanticModel _semanticModel;
        private readonly ISet<ISymbol> _typesToImport = new HashSet<ISymbol>(SymbolTable.KeyComparer);

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TranslationVisitor(DocumentTranslationContextWithSymbolTables context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _semanticModel = context.SemanticModel;
            _diagnostics = DiagnosticList.Create(context.Options);
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics.AsEnumerable();

        public IEnumerable<ISymbol> TypesToImport => _typesToImport.AsEnumerable();

        //// ===========================================================================================================
        //// Visit Methods
        //// ===========================================================================================================

        public override IEnumerable<IAstNode> DefaultVisit(SyntaxNode node)
        {
            var diagnostic = DiagnosticFactory.TranslationNotSupported(node);
            _diagnostics.Add(diagnostic);

#if DEBUG

            // throwing an exception lets us fail fast and see the problem in the unit test failure window
            throw new Exception(diagnostic.ToString());
#else
            return Enumerable.Empty<IAstNode>();
#endif
        }

        /// <summary>
        /// Called when the visitor visits a CompilationUnitSyntax node.
        /// </summary>
        /// <returns>An <see cref="ITsImplementationModule"/>.</returns>
        public override IEnumerable<IAstNode> VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var elements = node.Members.SelectMany(Visit).Cast<ITsImplementationModuleElement>();
            ITsImplementationModule implementationScript = Factory.ImplementationModule(elements.ToArray());

            return implementationScript.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a ParameterListSyntax node.
        /// </summary>
        /// <returns>A <see cref="ITsParameterList"/>.</returns>
        public override IEnumerable<IAstNode> VisitParameterList(ParameterListSyntax node)
        {
            var requiredParameters = new List<ITsRequiredParameter>();
            var optionalParameters = new List<ITsOptionalParameter>();
            ITsRestParameter restParameter = null;

            foreach (ParameterSyntax parameterNode in node.Parameters)
            {
                IAstNode parameter = Visit(parameterNode).Single();
                if (parameter is ITsRequiredParameter requiredParameter)
                {
                    requiredParameters.Add(requiredParameter);
                }
                else
                {
                    optionalParameters.Add((ITsOptionalParameter)parameter);
                }
            }

            // ReSharper disable once ExpressionIsAlwaysNull
            ITsParameterList parameterList = Factory.ParameterList(
                requiredParameters,
                optionalParameters,
                restParameter);
            return parameterList.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a ParameterSyntax node.
        /// </summary>
        /// <returns>Either a <see cref="ITsBoundRequiredParameter"/> or a <see cref="ITsBoundOptionalParameter"/>.</returns>
        public override IEnumerable<IAstNode> VisitParameter(ParameterSyntax node)
        {
            ITsIdentifier parameterName = Factory.Identifier(node.Identifier.Text);
            ITypeSymbol parameterTypeSymbol = node.Type.GetTypeSymbol(_semanticModel);
            ITsType parameterType = TypeTranslator.TranslateSymbol(parameterTypeSymbol, _typesToImport);
            IAstNode parameter;

            if (node.Default == null)
            {
                parameter = Factory.BoundRequiredParameter(parameterName, parameterType);
            }
            else
            {
                var initializer = (ITsExpression)Visit(node.Default).Single();
                parameter = Factory.BoundOptionalParameter(parameterName, initializer, parameterType);
            }

            return parameter.ToSingleEnumerable();
        }

        private ITsIdentifier TranslateIdentifier(SyntaxNode node)
        {
            ISymbol symbol = _semanticModel.GetDeclaredSymbol(node);
            string scriptName = _context.ScriptNameSymbolTable[symbol];
            return Factory.Identifier(scriptName);
        }

        /// <summary>
        /// Translates the C# XML documentation comment into a JSDoc comment if there is a
        /// documentation comment on the specified node.
        /// </summary>
        /// <typeparam name="T">The type of the translated node.</typeparam>
        /// <param name="translatedNode">The already-translated TypeScript AST node.</param>
        /// <param name="node">The C# syntax node to get documentation comments from.</param>
        /// <param name="symbolNode">
        /// The C# syntax node to use for retrieving the symbol. If not supplied <paramref
        /// name="node"/> is used.
        /// </param>
        /// <returns>
        /// If there are documentation comments, a new TypeScript AST node with the translated JsDoc
        /// comments prepended. If there are no documentation comments, the same node is returned.
        /// </returns>
        private T AddDocumentationComment<T>(T translatedNode, SyntaxNode node, SyntaxNode symbolNode = null)
            where T : IAstNode
        {
            if (!node.HasStructuredTrivia)
            {
                return translatedNode;
            }

            ISymbol symbol = _semanticModel.GetDeclaredSymbol(symbolNode ?? node);
            if (symbol == null)
            {
                return translatedNode;
            }

            DocumentationComment documentationComment = symbol.GetDocumentationComment();
            var result = DocumentationCommentTranslator.Translate(documentationComment, _context.Options);
            _diagnostics.AddRange(result.Diagnostics);

            return translatedNode.WithLeadingTrivia(result.Result);
        }

        /// <summary>
        /// Converts the translated declaration to an exported declaration if the C# declaration is public.
        /// </summary>
        /// <param name="translatedDeclaration">The TypeScript declaration to conditionally export.</param>
        /// <param name="node">The C# syntax node to inspect.</param>
        /// <returns>
        /// If the type does not need to be exported, <paramref name="translatedDeclaration"/> is
        /// returned; otherwise a wrapped exported <see cref="ITsExportImplementationElement"/> is returned.
        /// </returns>
        private ITsImplementationModuleElement ExportIfNeeded(
            ITsImplementationElement translatedDeclaration,
            BaseTypeDeclarationSyntax node)
        {
            // determine if this declaration should be exported
            INamedTypeSymbol symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return translatedDeclaration;
            }

            ITsExportImplementationElement exportedInterfaceDeclaration =
                Factory.ExportImplementationElement(translatedDeclaration);
            return exportedInterfaceDeclaration;
        }

        /// <summary>
        /// Calls <see cref="ExportIfNeeded"/> followed by <see cref="AddDocumentationComment{T}"/>.
        /// </summary>
        /// <param name="translatedDeclaration">The TypeScript declaration to conditionally export.</param>
        /// <param name="node">The C# syntax node to inspect.</param>
        /// <returns>
        /// If the type does not need to be exported, <paramref name="translatedDeclaration"/> is
        /// returned; otherwise a wrapped exported <see cref="ITsExportImplementationElement"/> is
        /// returned. Whichever element is returned, it includes any documentation comment.
        /// </returns>
        private ITsImplementationModuleElement ExportAndAddDocComment(
            ITsImplementationElement translatedDeclaration,
            BaseTypeDeclarationSyntax node)
        {
            var exportedDeclaration = ExportIfNeeded(translatedDeclaration, node);
            var withDocComment = AddDocumentationComment(exportedDeclaration, node);
            return withDocComment;
        }
    }
}
