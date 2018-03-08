﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentationCommentTranslatorTests.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Tests.Translation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Desalt.Core.Emit;
    using Desalt.Core.Extensions;
    using Desalt.Core.Translation;
    using Desalt.Core.TypeScript.Ast;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DocumentationCommentTranslatorTests
    {
        private static void AssertTranslation(string csharpComment, params string[] expectedJsDocLines)
        {
            // parse the C# code and get the root syntax node
            string csharpCode =
                $"using System; class Foo {{ {csharpComment}\npublic int Bar<T>(string p1, double p2) {{ }} }}";
            var syntaxTree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(csharpCode);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

            // compile it and get a semantic model
            CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly")
                .AddSyntaxTrees(syntaxTree)
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IEnumerable<int>).Assembly.Location));

            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

            // find the type symbol for the class member
            var methodDeclaration = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);

            // get the documentation comment
            DocumentationComment docComment = methodSymbol.GetDocumentationComment();
            docComment.Should().NotBeSameAs(DocumentationComment.Empty);

            // translate the documentation comment
            ITsMultiLineComment jsdocComment = DocumentationCommentTranslator.Translate(docComment);
            using (var stream = new MemoryStream())
            using (var emitter = new Emitter(stream, options: EmitOptions.UnixSpaces))
            {
                jsdocComment.Emit(emitter);
                string actualJsDoc = stream.ReadAllText(emitter.Encoding);
                string expectedJsDoc =
                    "/**\n" + string.Join("\n", expectedJsDocLines.Select(x => $" * {x}")) + "\n */\n";
                actualJsDoc.Should().Be(expectedJsDoc);
            }
        }

        [TestMethod]
        public void Translate_should_convert_a_summary_section_to_the_JSDoc_header()
        {
            AssertTranslation("///<summary>Test</summary>", "Test");
        }

        [TestMethod]
        public void Translate_should_convert_a_remarks_section()
        {
            AssertTranslation("///<remarks>Remarks</remarks>", "Remarks");
        }

        [TestMethod]
        public void Translate_should_convert_an_example_section()
        {
            AssertTranslation("///<example>Example</example>", "@example Example");
        }

        [TestMethod]
        public void Translate_should_convert_type_param_tags()
        {
            AssertTranslation(
                "///<typeparam name=\"T\">This is T\n///With two lines</typeparam>",
                "typeparam T - This is T",
                "With two lines");
        }

        [TestMethod]
        public void Translate_should_convert_param_tags()
        {
            AssertTranslation(
                "///<param name=\"p2\">This is p2\n///With two lines</param><param name=\"p1\">This is p1</param>",
                "@param p2 - This is p2",
                "With two lines",
                "@param p1 - This is p1");
        }

        [TestMethod]
        public void Translate_should_convert_the_returns_tag()
        {
            AssertTranslation("///<returns>A value</returns>", "@returns A value");
        }

        [TestMethod]
        public void Translate_should_convert_exception_tags()
        {
            AssertTranslation(
                "///<exception cref=\"ArgumentNullException\">p1 is null</exception>\n" +
                "///<exception cref=\"ArgumentNullException\">p2 is null</exception>\n" +
                "///<exception cref=\"InvalidOperationException\">Something is wrong</exception>",
                "@throws {ArgumentNullException} p1 is null",
                "@throws {ArgumentNullException} p2 is null",
                "@throws {InvalidOperationException} Something is wrong");
        }

        [TestMethod]
        public void Translate_should_convert_see_langword_to_markdown_backticks()
        {
            AssertTranslation("///<summary><see langword=\"null\"/></summary>", "`null`");
        }

        [TestMethod]
        public void Translate_should_convert_c_elements_to_markdown_backticks()
        {
            AssertTranslation("///<summary><c>some code</c></summary>", "`some code`");
        }

        [TestMethod]
        public void Translate_should_convert_see_references()
        {
            AssertTranslation(@"///<summary><see cref=""Console""/></summary>", "@see Console");
            AssertTranslation(@"///<summary><see cref=""IEnumerable""/></summary>", "@see IEnumerable");
        }

        [TestMethod]
        public void Translate_should_convert_a_seealso_reference_to_a_JSDoc_see_reference()
        {
            AssertTranslation(@"///<summary><seealso cref=""Console""/></summary>", "@see Console");
            AssertTranslation(@"///<summary><seealso cref=""IEnumerable""/></summary>", "@see IEnumerable");
        }

        [TestMethod]
        public void Translate_should_write_sections_in_the_correct_order()
        {
            const string csharpComment = @"
/// <remarks>Remarks</remarks>
/// <summary>Summary</summary>
/// <exception cref=""Exception"">Error 1</exception>
/// <param name=""p2"">P2</param>
/// <returns>Returns</returns>
/// <example>Example</example>
/// <exception cref=""Exception"">Error 2</exception>
/// <param name=""p1"">P1</param>";

            string[] jsDocLines =
            {
                "Summary",
                "Remarks",
                "@example Example",
                "@param p2 - P2",
                "@param p1 - P1",
                "@returns Returns",
                "@throws {Exception} Error 1",
                "@throws {Exception} Error 2"
            };

            AssertTranslation(csharpComment, jsDocLines);
        }
    }
}