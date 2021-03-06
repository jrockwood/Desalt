// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ScriptSymbolTableTests.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Tests.SymbolTables
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Desalt.CompilerUtilities.Extensions;
    using Desalt.Core.Options;
    using Desalt.Core.SymbolTables;
    using Desalt.Core.Tests.TestUtility;
    using Desalt.Core.Translation;
    using Desalt.Core.Utility;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public class ScriptSymbolTableTests
    {
        private static IScriptNamer CreateFakeScriptNamer()
        {
            var fakeScriptNamer = new FakeScriptNamer("ComputedScriptName");
            return fakeScriptNamer;
        }

        private static async Task AssertDocumentEntriesInSymbolTable(
            string code,
            params string[] expectedEntries)
        {
            using TempProject tempProject = await TempProject.CreateAsync(code);
            DocumentTranslationContext context = await tempProject.CreateContextForFileAsync();
            var contexts = context.ToSingleEnumerable().ToImmutableArray();

            var symbolTable = ScriptSymbolTable.Create(
                contexts,
                CreateFakeScriptNamer(),
                SymbolDiscoveryKind.OnlyDocumentTypes);

            symbolTable.DocumentSymbols
                .Select(pair => pair.Key.ToHashDisplay())
                .Should()
                .BeEquivalentTo(expectedEntries);
        }

        [Test]
        public async Task Create_should_find_all_of_the_types_and_members_in_the_document()
        {
            const string code = @"
using System;
using System.Runtime.CompilerServices;

[BindThisToFirstParameter]
delegate void D();

class C
{
    public string Field;
    static C() {} // skipped
    public C(int x) {}
    public string Prop { get; }
    public void Method() {}
}

interface I
{
    string Prop { get; }
    void Method();
}

struct S
{
    public S(int field) { Field = field; Prop = ""Hi""; }
    public int Field;
    public string Prop { get; }
    public void Method() {}
}";

            await AssertDocumentEntriesInSymbolTable(
                code,
                "D",
                "C",
                "C.Field",
                "C.C(int x)",
                "C.Prop",
                "C.Method()",
                "I",
                "I.Prop",
                "I.Method()",
                "S",
                "S.S(int field)",
                "S.Field",
                "S.Prop",
                "S.Method()");
        }

        [Test]
        public async Task Create_should_find_all_of_the_types_and_members_in_external_references()
        {
            const string code = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    public void Method()
    {
        Script.IsValue(null);
    }
}
";

            using TempProject tempProject = await TempProject.CreateAsync(code);
            DocumentTranslationContext context = await tempProject.CreateContextForFileAsync();
            var contexts = context.ToSingleEnumerable().ToImmutableArray();

            var symbolTable = ScriptSymbolTable.Create(contexts, CreateFakeScriptNamer());

            // check a directly-reference symbol
            InvocationExpressionSyntax invocationExpressionSyntax =
                context.RootSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            ISymbol scriptIsValueSymbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol!;
            symbolTable.DirectlyReferencedExternalSymbols.Should().ContainKey(scriptIsValueSymbol);

            // check an implicitly referenced symbol
            INamedTypeSymbol stringBuilderSymbol =
                scriptIsValueSymbol.ContainingAssembly.GetTypeByMetadataName("System.Text.StringBuilder")!;
            symbolTable.IndirectlyReferencedExternalSymbols.Should().ContainKey(stringBuilderSymbol);
        }

        [Test]
        public async Task All_attributes_should_be_correctly_read_from_all_references()
        {
            const string code = @"
using System;
using System.Runtime.CompilerServices;

[Imported]
class C
{
    public void Method()
    {
        Script.IsNull(null);
    }
}
";

            using TempProject tempProject = await TempProject.CreateAsync(code);
            DocumentTranslationContext context = await tempProject.CreateContextForFileAsync();
            var contexts = context.ToSingleEnumerable().ToImmutableArray();

            var symbolTable = ScriptSymbolTable.Create(contexts, CreateFakeScriptNamer());

            // make sure we read the [Imported] from class C
            ClassDeclarationSyntax classDeclarationSyntax =
                context.RootSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            ISymbol classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            symbolTable.TryGetValue(classSymbol, out IScriptTypeSymbol? scriptClassSymbol).Should().BeTrue();
            scriptClassSymbol!.Imported.Should().BeTrue();

            // make sure we read the [InlineCode] from Script.IsNull
            InvocationExpressionSyntax invocationExpressionSyntax =
                context.RootSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            ISymbol scriptIsNullSymbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol!;

            symbolTable.TryGetValue(scriptIsNullSymbol, out IScriptMethodSymbol? scriptIsNullScriptSymbol)
                .Should()
                .BeTrue();

            scriptIsNullScriptSymbol!.InlineCode.Should().NotBeNullOrWhiteSpace();

            // make sure we read the [IgnoreNamespace] from System.Boolean
            ISymbol boolSymbol = scriptIsNullSymbol.ContainingAssembly.GetTypeByMetadataName("System.Boolean")!;

            symbolTable.TryGetValue(boolSymbol, out IScriptTypeSymbol? scriptBooleanSymbol).Should().BeTrue();
            scriptBooleanSymbol!.IgnoreNamespace.Should().BeTrue();
        }

        [Test]
        public async Task TryGetValue_should_use_the_overrides_if_present()
        {
            const string code = @"
using System;
using System.Runtime.CompilerServices;

class C
{
    public void Method() {}
}
";

            using TempProject tempProject = await TempProject.CreateAsync(code);
            var overrides = new SymbolTableOverrides(
                new KeyValuePair<string, SymbolTableOverride>(
                    "C.Method()",
                    new SymbolTableOverride(inlineCode: "OVERRIDE")));
            CompilerOptions options = tempProject.Options.WithSymbolTableOverrides(overrides);

            DocumentTranslationContext context = await tempProject.CreateContextForFileAsync(options: options);
            var contexts = context.ToSingleEnumerable().ToImmutableArray();

            var symbolTable = ScriptSymbolTable.Create(
                contexts,
                CreateFakeScriptNamer(),
                SymbolDiscoveryKind.OnlyDocumentTypes);

            // get the C.Method() symbol
            ISymbol methodSymbol = context.SemanticModel.Compilation.Assembly.GetTypeByMetadataName("C")!
                .GetMembers("Method")
                .Single();

            symbolTable.Get<IScriptMethodSymbol>(methodSymbol).InlineCode.Should().Be("OVERRIDE");
        }

        //// ===========================================================================================================
        //// Classes
        //// ===========================================================================================================

        private sealed class FakeScriptNamer : IScriptNamer
        {
            public FakeScriptNamer(string scriptNameForAllSymbols)
            {
                ScriptNameForAllSymbols = scriptNameForAllSymbols;
            }

            private string ScriptNameForAllSymbols { get; }

            /// <summary>
            /// Determines the name a symbol should have in the generated script.
            /// </summary>
            /// <param name="symbol">The symbol for which to discover the script name.</param>
            /// <returns>The name the specified symbol should have in the generated script.</returns>
            public string DetermineScriptNameForSymbol(ISymbol symbol)
            {
                return ScriptNameForAllSymbols;
            }
        }
    }
}
