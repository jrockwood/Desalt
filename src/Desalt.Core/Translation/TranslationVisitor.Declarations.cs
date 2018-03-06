﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TranslationVisitor.Declarations.cs" company="Justin Rockwood">
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

    internal sealed partial class TranslationVisitor
    {
        /// <summary>
        /// Called when the visitor visits a NamespaceDeclarationSyntax node.
        /// </summary>
        /// <returns>A list of <see cref="ITsImplementationModuleElement"/> elements.</returns>
        public override IEnumerable<IAstNode> VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            return node.Members.SelectMany(Visit).Cast<ITsImplementationModuleElement>();
        }

        /// <summary>
        /// Called when the visitor visits a InterfaceDeclarationSyntax node.
        /// </summary>
        /// <returns>
        /// An <see cref="ITsImplementationModuleElement"/>, which is either an <see
        /// cref="ITsInterfaceDeclaration"/> or an <see cref="ITsExportImplementationElement"/>.
        /// </returns>
        public override IEnumerable<IAstNode> VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            ITsIdentifier interfaceName = TranslateIdentifier(node);

            // translate the interface body
            var translatedMembers = node.Members.SelectMany(Visit).Cast<ITsTypeMember>();
            ITsObjectType body = Factory.ObjectType(translatedMembers.ToArray());

            // translate the generic type parameters
            ITsTypeParameters typeParameters = Factory.TypeParameters();

            // translate the extends clause
            IEnumerable<ITsTypeReference> extendsClause = Enumerable.Empty<ITsTypeReference>();

            // create the interface declaration
            ITsInterfaceDeclaration interfaceDeclaration = Factory.InterfaceDeclaration(
                interfaceName,
                body,
                typeParameters,
                extendsClause);

            // export if necessary and add documentation comments
            ITsImplementationModuleElement final =
                ExportAndAddDocumentationCommentIfNecessary(node, interfaceDeclaration);
            return final.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a EnumDeclarationSyntax node.
        /// </summary>
        /// <returns>
        /// An <see cref="ITsImplementationModuleElement"/>, which is either an <see
        /// cref="ITsEnumDeclaration"/> or an <see cref="ITsExportImplementationElement"/>.
        /// </returns>
        public override IEnumerable<IAstNode> VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            ITsIdentifier enumName = TranslateIdentifier(node);

            // translate the enum body
            var enumMembers = node.Members.SelectMany(Visit).Cast<ITsEnumMember>();
            ITsEnumDeclaration enumDeclaration = Factory.EnumDeclaration(enumName, enumMembers);

            // export if necessary and add documentation comments
            ITsImplementationModuleElement final = ExportAndAddDocumentationCommentIfNecessary(node, enumDeclaration);
            return final.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a EnumMemberDeclarationSyntax node.
        /// </summary>
        public override IEnumerable<IAstNode> VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            ITsIdentifier scriptName = TranslateIdentifier(node);
            ITsExpression value = null;
            if (node.EqualsValue != null)
            {
                value = Visit(node.EqualsValue.Value).Cast<ITsExpression>().Single();
            }

            ITsEnumMember translatedMember = Factory.EnumMember(scriptName, value);
            return translatedMember.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a ClassDeclarationSyntax node.
        /// </summary>
        /// <returns>
        /// An <see cref="ITsImplementationModuleElement"/>, which is either an <see
        /// cref="ITsClassElement"/> or an <see cref="ITsExportImplementationElement"/>.
        /// </returns>
        public override IEnumerable<IAstNode> VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ITsIdentifier className = TranslateIdentifier(node);

            // translate the generic type parameters
            ITsTypeParameters typeParameters = Factory.TypeParameters();

            // translate the class heritage
            ITsClassHeritage heritage = Factory.ClassHeritage(implementsTypes: null);

            // translate the interface body
            var classBody = node.Members.SelectMany(Visit).Cast<ITsClassElement>();

            ITsClassDeclaration classDeclaration = Factory.ClassDeclaration(
                className,
                typeParameters,
                heritage,
                classBody);

            // export if necessary and add documentation comments
            ITsImplementationModuleElement final = ExportAndAddDocumentationCommentIfNecessary(node, classDeclaration);
            return final.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a FieldDeclarationSyntax node.
        /// </summary>
        public override IEnumerable<IAstNode> VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var fieldDeclarations = new List<ITsVariableMemberDeclaration>();
            foreach (VariableDeclaratorSyntax variableDeclaration in node.Declaration.Variables)
            {
                var variableName = TranslateIdentifier(variableDeclaration);
                ISymbol symbol = _context.SemanticModel.GetDeclaredSymbol(variableDeclaration);
                TsAccessibilityModifier accessibilityModifier =
                    GetAccessibilityModifier(symbol, variableDeclaration.GetLocation);

                var typeAnnotation = TypeTranslator.TranslateSymbol(
                    node.Declaration.Type.GetTypeSymbol(_context.SemanticModel),
                    _typesToImport);

                ITsVariableMemberDeclaration fieldDeclaration = Factory.VariableMemberDeclaration(
                    variableName,
                    accessibilityModifier,
                    symbol.IsStatic,
                    typeAnnotation);
                fieldDeclarations.Add(fieldDeclaration);
            }

            return fieldDeclarations;
        }

        /// <summary>
        /// Called when the visitor visits a MethodDeclarationSyntax node.
        /// </summary>
        /// <returns>
        /// An <see cref="ITsMethodSignature"/> if we're within an interface declaration; otherwise
        /// an <see cref="ITsFunctionMemberDeclaration"/>.
        /// </returns>
        public override IEnumerable<IAstNode> VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            ITsIdentifier functionName = TranslateIdentifier(node);

            // create the call signature
            ITsTypeParameters typeParameters = Factory.TypeParameters();
            var parameters = (ITsParameterList)Visit(node.ParameterList).Single();
            ITsType returnType = TypeTranslator.TranslateSymbol(
                node.ReturnType.GetTypeSymbol(_context.SemanticModel),
                _typesToImport);

            ITsCallSignature callSignature = Factory.CallSignature(typeParameters, parameters, returnType);

            // if we're defining an interface, then we need to return a ITsMethodSignature
            if (_context.SemanticModel.GetDeclaredSymbol(node).ContainingType.IsInterfaceType())
            {
                ITsMethodSignature methodSignature = Factory.MethodSignature(
                    functionName,
                    isOptional: false,
                    callSignature: callSignature);
                methodSignature = AddDocumentationCommentIfNecessary(node, methodSignature);
                return methodSignature.ToSingleEnumerable();
            }

            // we're within a class, so return a method declaration
            TsAccessibilityModifier accessibilityModifier = GetAccessibilityModifier(node);
            ITsFunctionMemberDeclaration methodDeclaration = Factory.FunctionMemberDeclaration(
                functionName,
                callSignature,
                accessibilityModifier,
                functionBody: null);

            methodDeclaration = AddDocumentationCommentIfNecessary(node, methodDeclaration);
            return methodDeclaration.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a ConstructorDeclarationSyntax node.
        /// </summary>
        /// <returns>An <see cref="ITsConstructorDeclaration"/>.</returns>
        public override IEnumerable<IAstNode> VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            TsAccessibilityModifier accessibilityModifier = GetAccessibilityModifier(node);
            var parameterList = Factory.ParameterList();

            ITsConstructorDeclaration constructorDeclaration = Factory.ConstructorDeclaration(
                accessibilityModifier,
                parameterList,
                functionBody: null);

            constructorDeclaration = AddDocumentationCommentIfNecessary(node, constructorDeclaration);
            return constructorDeclaration.ToSingleEnumerable();
        }

        /// <summary>
        /// Called when the visitor visits a PropertyDeclarationSyntax node.
        /// </summary>
        public override IEnumerable<IAstNode> VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            ITsIdentifier propertyName = TranslateIdentifier(node);
            ITypeSymbol typeSymbol = node.Type.GetTypeSymbol(_context.SemanticModel);
            var propertyType = TypeTranslator.TranslateSymbol(typeSymbol, _typesToImport);

            foreach (AccessorDeclarationSyntax accessor in node.AccessorList.Accessors)
            {
                TsAccessibilityModifier accessibilityModifier = GetAccessibilityModifier(accessor);

                switch (accessor.Kind())
                {
                    case SyntaxKind.GetAccessorDeclaration:
                        ITsGetAccessor getAccessor = Factory.GetAccessor(
                            propertyName,
                            propertyType,
                            functionBody: null);
                        ITsGetAccessorMemberDeclaration getAccessorDeclaration =
                            Factory.GetAccessorMemberDeclaration(getAccessor, accessibilityModifier);
                        yield return getAccessorDeclaration;
                        break;

                    case SyntaxKind.SetAccessorDeclaration:
                        ITsSetAccessor setAccessor = Factory.SetAccessor(
                            propertyName,
                            Factory.Identifier("value"),
                            propertyType,
                            functionBody: null);
                        ITsSetAccessorMemberDeclaration setAccessorDeclaration =
                            Factory.SetAccessorMemberDeclaration(setAccessor, accessibilityModifier);
                        yield return setAccessorDeclaration;
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown accessor kind '{accessor.Kind()}'");
                }
            }
        }

        private TsAccessibilityModifier GetAccessibilityModifier(SyntaxNode node)
        {
            ISymbol symbol = _context.SemanticModel.GetDeclaredSymbol(node);
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

        /// <summary>
        /// Converts the translated declaration to an exported declaration if the C# declaration is public.
        /// </summary>
        /// <param name="node">The C# syntax node to inspect.</param>
        /// <param name="translatedDeclaration">The TypeScript declaration to conditionally export.</param>
        /// <returns>
        /// If the type does not need to be exported, <paramref name="translatedDeclaration"/> is
        /// returned; otherwise a wrapped exported <see cref="ITsExportImplementationElement"/> is returned.
        /// </returns>
        private ITsImplementationModuleElement ExportIfNecessary(
            BaseTypeDeclarationSyntax node,
            ITsImplementationElement translatedDeclaration)
        {
            // determine if this declaration should be exported
            INamedTypeSymbol symbol = _context.SemanticModel.GetDeclaredSymbol(node);
            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return translatedDeclaration;
            }

            ITsExportImplementationElement exportedInterfaceDeclaration =
                Factory.ExportImplementationElement(translatedDeclaration);

            exportedInterfaceDeclaration = AddDocumentationCommentIfNecessary(node, exportedInterfaceDeclaration);
            return exportedInterfaceDeclaration;
        }

        private ITsImplementationModuleElement ExportAndAddDocumentationCommentIfNecessary(
            BaseTypeDeclarationSyntax node,
            ITsImplementationElement translatedDeclaration)
        {
            ITsImplementationModuleElement exported = ExportIfNecessary(node, translatedDeclaration);
            return AddDocumentationCommentIfNecessary(node, exported);
        }
    }
}
