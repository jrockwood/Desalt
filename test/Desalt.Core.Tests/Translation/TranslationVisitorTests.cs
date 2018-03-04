﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TranslationVisitorTests.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Tests.Translation
{
    using System.Linq;
    using Desalt.Core.Emit;
    using Desalt.Core.Translation;
    using Desalt.Core.TypeScript.Ast;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TranslationVisitorTests
    {
        private static void AssertTranslation(string csharpCode, string expectedTypeScriptCode)
        {
            var syntaxTree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(csharpCode);
            CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly").AddSyntaxTrees(syntaxTree);
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            var visitor = new TranslationVisitor(new CompilerOptions("out"), semanticModel);

            CompilationUnitSyntax compilationUnit = syntaxTree.GetCompilationUnitRoot();
            IAstNode result = visitor.Visit(compilationUnit).Single();

            visitor.Diagnostics.Should().BeEmpty();

            // rather than try to implement equality tests for all IAstNodes, just emit both and compare the strings
            result.EmitAsString(emitOptions: EmitOptions.UnixSpaces).Should().Be(expectedTypeScriptCode);
        }

        [TestClass]
        public class InterfaceDeclarationTests
        {
            [TestMethod]
            public void Bare_interface_declaration_without_accessibility_should_not_be_exported()
            {
                AssertTranslation("interface ITest {}", "interface ITest {\n}\n");
            }

            [TestMethod]
            public void Public_interface_declaration_should_be_exported()
            {
                AssertTranslation("public interface ITest {}", "export interface ITest {\n}\n");
            }

            [TestMethod]
            public void A_method_declaration_with_no_parameters_and_a_void_return_type_should_be_translated()
            {
                AssertTranslation("interface ITest { void Do(); }", "interface ITest {\n  Do(): void;\n}\n");
            }

            [TestMethod]
            public void A_method_declaration_with_simple_parameters_and_a_void_return_type_should_be_translated()
            {
                AssertTranslation(
                    "interface ITest { void Do(string x, string y); }",
                    "interface ITest {\n  Do(x: string, y: string): void;\n}\n");
            }
        }
    }
}