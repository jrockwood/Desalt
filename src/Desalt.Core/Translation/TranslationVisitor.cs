// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TranslationVisitor.cs" company="Justin Rockwood">
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
    using System.Threading;
    using Desalt.CompilerUtilities.Extensions;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.Options;
    using Desalt.Core.SymbolTables;
    using Desalt.Core.Utility;
    using Desalt.TypeScriptAst.Ast;
    using Desalt.TypeScriptAst.Ast.Types;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Factory = TypeScriptAst.Ast.TsAstFactory;

    /// <summary>
    /// Delegate for a function that translates an identifier name represented by the symbol, taking into account static
    /// vs. instance references.
    /// </summary>
    /// <param name="symbol">The symbol to translate.</param>
    /// <param name="node">The start of the syntax node where this symbol was located.</param>
    /// <param name="forcedScriptName">If present, this name will be used instead of looking it up in the symbol table.</param>
    /// <returns>An <see cref="ITsIdentifier"/> or <see cref="ITsMemberDotExpression"/>.</returns>
    internal delegate ITsExpression TranslateIdentifierFunc(
        ISymbol symbol,
        SyntaxNode node,
        string? forcedScriptName = null);

    internal delegate T TranslationVisitFunc<out T>(SyntaxNode node)
        where T : ITsAstNode;

    /// <summary>
    /// Visits a C# syntax tree, translating from a C# AST into a TypeScript AST.
    /// </summary>
    internal sealed partial class TranslationVisitor : CSharpSyntaxVisitor<IEnumerable<ITsAstNode>>
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        private static readonly ITsIdentifier s_staticCtorName = Factory.Identifier("__ctor");

        private readonly ICollection<Diagnostic> _diagnostics;
        private readonly CancellationToken _cancellationToken;
        private readonly SemanticModel _semanticModel;
        private readonly ScriptSymbolTable _scriptSymbolTable;
        private readonly RenameRules _renameRules;

        private readonly ExtensionMethodTranslator _extensionMethodTranslator;
        private readonly InlineCodeTranslator _inlineCodeTranslator;
        private readonly ScriptSkipTranslator _scriptSkipTranslator;
        private readonly TypeTranslator _typeTranslator;
        private readonly AlternateSignatureTranslator _alternateSignatureTranslator;
        private readonly UserDefinedOperatorTranslator _userDefinedOperatorTranslator;

        private readonly ISet<ITypeSymbol> _typesToImport = new HashSet<ITypeSymbol>();
        private readonly TemporaryVariableAllocator _temporaryVariableAllocator = new TemporaryVariableAllocator();

        /// <summary>
        /// Keeps track of the auto-generated property names, keyed by the property symbol and containing the property name.
        /// </summary>
        private readonly IDictionary<IPropertySymbol, ITsIdentifier> _autoGeneratedPropertyNames =
            new Dictionary<IPropertySymbol, ITsIdentifier>(SymbolEqualityComparer.Default);

        /// <summary>
        /// Keeps track of additional variable declarations that need to happen in the class as a result of auto properties.
        /// </summary>
        private readonly ICollection<ITsVariableMemberDeclaration> _autoGeneratedClassVariableDeclarations =
            new List<ITsVariableMemberDeclaration>();

        /// <summary>
        /// Keeps track of any additional statements that need to be included before translating the current statement.
        /// This is used for some expressions that require temporary variable declaration and assignment (for example, a
        /// postfix increment/decrement that represents an operator overload.
        /// </summary>
        private readonly ICollection<ITsStatementListItem> _additionalStatementsNeededBeforeCurrentStatement =
            new List<ITsStatementListItem>();

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationVisitor"/> class.
        /// </summary>
        /// <param name="context">The context for the document translation.</param>
        /// <param name="diagnostics">
        /// An optional diagnostic collection to use for adding errors. This should normally not be used since it could
        /// make this class not thread safe if access to the collection is not guarded with thread locking mechanisms.
        /// No locking is done within this class. This is used mainly for unit tests.
        /// </param>
        /// <param name="cancellationToken">An optional token to control canceling translation.</param>
        public TranslationVisitor(
            DocumentTranslationContextWithSymbolTables context,
            ICollection<Diagnostic>? diagnostics = null,
            CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;

            _semanticModel = context.SemanticModel;
            _scriptSymbolTable = context.ScriptSymbolTable;
            _renameRules = context.Options.RenameRules;

            _extensionMethodTranslator =
                new ExtensionMethodTranslator(_semanticModel, _scriptSymbolTable);
            _inlineCodeTranslator = new InlineCodeTranslator(_semanticModel, _scriptSymbolTable);
            _scriptSkipTranslator = new ScriptSkipTranslator(_semanticModel, _scriptSymbolTable);

            _typeTranslator = new TypeTranslator(_scriptSymbolTable);

            _alternateSignatureTranslator = new AlternateSignatureTranslator(
                context.AlternateSignatureSymbolTable,
                _typeTranslator);

            if (diagnostics == null)
            {
                var diagnosticList = DiagnosticList.Create(context.Options);
                _diagnostics = diagnosticList;
#if DEBUG

                // Throwing an exception lets us fail fast and see the problem in the unit test failure window.
                diagnosticList.ThrowOnErrors = true;
#endif
            }
            else
            {
                _diagnostics = diagnostics;
            }

            _userDefinedOperatorTranslator = new UserDefinedOperatorTranslator(
                _semanticModel,
                _scriptSymbolTable,
                _renameRules,
                TranslateIdentifierName,
                VisitSingleOfType<ITsExpression>,
                _temporaryVariableAllocator,
                _diagnostics);
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics.AsEnumerable();

        public IEnumerable<ITypeSymbol> TypesToImport => _typesToImport.AsEnumerable();

        //// ===========================================================================================================
        //// Visit Methods
        //// ===========================================================================================================

        public override IEnumerable<ITsAstNode> DefaultVisit(SyntaxNode node)
        {
            Diagnostic diagnostic = DiagnosticFactory.TranslationNotSupported(node);
            _diagnostics.Add(diagnostic);
            return Enumerable.Empty<ITsAstNode>();
        }

        /// <summary>
        /// Visits a node where it is known that a single value of the specified type will be returned.
        /// </summary>
        /// <typeparam name="T">The type of the translated <see cref="ITsAstNode"/>.</typeparam>
        /// <param name="node">The syntax node to translate.</param>
        /// <returns>A single <see cref="ITsAstNode"/>.</returns>
        private T VisitSingleOfType<T>(SyntaxNode node)
            where T : ITsAstNode
        {
            return (T)Visit(node).Single();
        }

        /// <summary>
        /// Shortcut of <see cref="VisitSingleOfType{ITsExpression}"/>.
        /// </summary>
        private ITsExpression VisitExpression(SyntaxNode node)
        {
            return VisitSingleOfType<ITsExpression>(node);
        }

        /// <summary>
        /// Shortcut of <see cref="VisitSingleOfType{ITsStatement}"/>.
        /// </summary>
        private ITsStatement VisitStatement(SyntaxNode node)
        {
            return VisitSingleOfType<ITsStatement>(node);
        }

        /// <summary>
        /// Visits a series of nodes that should all get translated similarly and returns a list of the translated <see cref="ITsAstNode"/>.
        /// </summary>
        /// <typeparam name="T">The type of the translated <see cref="ITsAstNode"/>.</typeparam>
        /// <param name="nodes">The syntax node list to translate.</param>
        /// <returns>A list of translated <see cref="ITsAstNode"/>.</returns>
        private List<T> VisitMultipleOfType<T>(IEnumerable<SyntaxNode> nodes)
            where T : ITsAstNode
        {
            return nodes.SelectMany(Visit).Cast<T>().ToList();
        }

        /// <summary>
        /// Visits a node that gets translated to multiple <see cref="ITsAstNode"/> of the same type. For example,
        /// TypeArgumentListSyntax or other nodes that inherently represent a list.
        /// </summary>
        /// <typeparam name="T">The type of the translated <see cref="ITsAstNode"/>.</typeparam>
        /// <param name="node">The syntax node to translate.</param>
        /// <returns>A list of translated <see cref="ITsAstNode"/>.</returns>
        private List<T> VisitMultipleOfType<T>(SyntaxNode node) where T : ITsAstNode
        {
            return Visit(node).Cast<T>().ToList();
        }

        /// <summary>
        /// Creates a new InternalError diagnostic, adds it to the diagnostics list, and then throws an exception so we
        /// can get a stack trace in debug mode and returns an empty enumerable.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="node">The node where the error occurs.</param>
        /// <returns>An empty <see cref="IEnumerable{ITsAstNode}"/>.</returns>
        [DoesNotReturn]
        private void ReportInternalError(string message, SyntaxNode node)
        {
            var diagnostic = DiagnosticFactory.InternalError(message, node.GetLocation());
            _diagnostics.Add(diagnostic);
            throw new Exception(diagnostic.ToString());
        }

        /// <summary>
        /// Gets an expected symbol from the semantic model and calls <see cref="ReportInternalError"/> if the
        /// symbol is not found.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/> from which to get a symbol.</param>
        /// <returns>The symbol associated with the syntax node.</returns>
        private ISymbol GetExpectedSymbol(SyntaxNode node)
        {
            ISymbol? symbol = _semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol == null)
            {
                ReportInternalError($"Node '{node}' should have an expected symbol.", node);
            }

            return symbol;
        }

        /// <summary>
        /// Gets an expected symbol from the semantic model and calls <see cref="ReportInternalError"/> if the
        /// symbol is not found.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/> from which to get a symbol.</param>
        /// <returns>The symbol associated with the syntax node.</returns>
        private TSymbol GetExpectedDeclaredSymbol<TSymbol>(SyntaxNode node) where TSymbol : class, ISymbol
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node) as TSymbol;
            if (symbol == null)
            {
                ReportInternalError($"Node '{node}' should have an expected declared symbol.", node);
            }

            return symbol;
        }

        /// <summary>
        /// Gets an expected symbol and associated <see cref="IScriptSymbol"/> and calls <see
        /// cref="ReportInternalError"/> if either is not found.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/> from which to get a symbol.</param>
        /// <returns>The symbol and script symbol associated with the syntax node.</returns>
        private (ISymbol symbol, IScriptSymbol scriptSymbol) GetExpectedScriptSymbol(SyntaxNode node)
        {
            ISymbol symbol = GetExpectedSymbol(node);

            if (!_scriptSymbolTable.TryGetValue(symbol, out IScriptSymbol? scriptSymbol))
            {
                ReportInternalError($"Node should have been added to the ScriptSymbolTable: {node}", node);
            }

            return (symbol, scriptSymbol);
        }

        /// <summary>
        /// Gets an expected symbol and associated <see cref="IScriptSymbol"/> and calls <see
        /// cref="ReportInternalError"/> if either is not found.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/> from which to get a symbol.</param>
        /// <returns>The symbol and script symbol associated with the syntax node.</returns>
        private (TSymbol symbol, TScriptSymbol scriptSymbol) GetExpectedDeclaredScriptSymbol<TSymbol, TScriptSymbol>(
            SyntaxNode node)
            where TSymbol : class, ISymbol
            where TScriptSymbol : class, IScriptSymbol
        {
            TSymbol symbol = GetExpectedDeclaredSymbol<TSymbol>(node);

            if (!_scriptSymbolTable.TryGetValue(symbol, out TScriptSymbol? scriptSymbol))
            {
                ReportInternalError($"Node should have been added to the ScriptSymbolTable: {node}", node);
            }

            return (symbol, scriptSymbol);
        }

        /// <summary>
        /// Gets an expected symbol and associated <see cref="IScriptSymbol"/> and calls <see
        /// cref="ReportInternalError"/> if either is not found.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/> from which to get a symbol.</param>
        /// <returns>The symbol and script symbol associated with the syntax node.</returns>
        private TScriptSymbol GetExpectedDeclaredScriptSymbol<TScriptSymbol>(SyntaxNode node)
            where TScriptSymbol : class, IScriptSymbol
        {
            return GetExpectedDeclaredScriptSymbol<ISymbol, TScriptSymbol>(node).scriptSymbol;
        }

        /// <summary>
        /// Called when the visitor visits a CompilationUnitSyntax node.
        /// </summary>
        /// <returns>An <see cref="ITsImplementationModule"/>.</returns>
        public override IEnumerable<ITsAstNode> VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var elements = node.Members.SelectMany(Visit).Cast<ITsImplementationModuleElement>();
            ITsImplementationModule implementationScript = Factory.ImplementationModule(elements.ToArray());

            return implementationScript.ToSingleEnumerable();
        }

        /// <summary>
        /// Translates an identifier used in a declaration (class, interface, method, etc.) by
        /// looking up the symbol and the associated script name.
        /// </summary>
        /// <param name="node">The node to translate.</param>
        /// <returns>An <see cref="ITsIdentifier"/>.</returns>
        private ITsIdentifier TranslateDeclarationIdentifier(MemberDeclarationSyntax node)
        {
            var scriptSymbol = GetExpectedDeclaredScriptSymbol<IScriptSymbol>(node);
            return Factory.Identifier(scriptSymbol.ComputedScriptName);
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
        private T AddDocumentationComment<T>(T translatedNode, SyntaxNode node, SyntaxNode? symbolNode = null)
            where T : ITsAstNode
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

            DocumentationComment? documentationComment = symbol.GetDocumentationComment();
            if (documentationComment == null)
            {
                return translatedNode;
            }

            var result = DocumentationCommentTranslator.Translate(documentationComment);
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
            ITsImplementationModuleElement exportedDeclaration = ExportIfNeeded(translatedDeclaration, node);
            ITsImplementationModuleElement withDocComment = AddDocumentationComment(exportedDeclaration, node);
            return withDocComment;
        }

        /// <summary>
        /// Translates a C# call signature into a TypeScript equivalent.
        /// </summary>
        /// <param name="parameterListNode">The C# parameter list.</param>
        /// <param name="typeParameterListNode">The C# type parameter list.</param>
        /// <param name="returnTypeNode">The C# return type.</param>
        /// <param name="methodSymbol">The method symbol, which is used for [AlternateSignature] methods.</param>
        /// <returns></returns>
        private ITsCallSignature TranslateCallSignature(
            ParameterListSyntax? parameterListNode,
            TypeParameterListSyntax? typeParameterListNode = null,
            TypeSyntax? returnTypeNode = null,
            IMethodSymbol? methodSymbol = null)
        {
            ITsTypeParameters typeParameters = typeParameterListNode == null
                ? Factory.TypeParameters()
                : (ITsTypeParameters)Visit(typeParameterListNode).Single();

            ITsParameterList parameters = parameterListNode == null
                ? Factory.ParameterList()
                : (ITsParameterList)Visit(parameterListNode).Single();

            ITsType? returnType = null;
            ITypeSymbol? returnTypeSymbol = returnTypeNode?.GetTypeSymbol(_semanticModel);
            if (returnTypeNode != null && returnTypeSymbol != null)
            {
                returnType = _typeTranslator.TranslateSymbol(
                    returnTypeSymbol,
                    _typesToImport,
                    _diagnostics,
                    returnTypeNode.GetLocation);
            }

            ITsCallSignature callSignature = Factory.CallSignature(typeParameters, parameters, returnType);

            // See if the parameter list should be adjusted to accomodate [AlternateSignature] methods.
            if (methodSymbol != null)
            {
                bool adjustedParameters = _alternateSignatureTranslator.TryAdjustParameterListTypes(
                    methodSymbol,
                    callSignature.Parameters,
                    out ITsParameterList translatedParameterList,
                    out IEnumerable<Diagnostic> diagnostics);

                _diagnostics.AddRange(diagnostics);

                if (adjustedParameters)
                {
                    callSignature = callSignature.WithParameters(translatedParameterList);
                }
            }

            return callSignature;
        }

        private TsAccessibilityModifier GetAccessibilityModifier(SyntaxNode node)
        {
            ISymbol symbol = _semanticModel.GetDeclaredSymbol(node);
            return GetAccessibilityModifier(symbol, node.GetLocation);
        }

        private TsAccessibilityModifier GetAccessibilityModifier(ISymbol symbol, Func<Location> getLocationFunc)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Private:
                    return TsAccessibilityModifier.Private;

                case Accessibility.Protected:
                    return TsAccessibilityModifier.Protected;

                case Accessibility.Public:
                    return TsAccessibilityModifier.Public;

                case Accessibility.NotApplicable:
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                    _diagnostics.Add(
                        DiagnosticFactory.UnsupportedAccessibility(
                            symbol.DeclaredAccessibility.ToString(),
                            "public",
                            getLocationFunc()));
                    return TsAccessibilityModifier.Public;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
