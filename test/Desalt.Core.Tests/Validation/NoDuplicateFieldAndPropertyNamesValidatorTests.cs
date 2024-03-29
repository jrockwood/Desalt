﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="NoDuplicateFieldAndPropertyNamesValidatorTests.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Tests.Validation
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Desalt.CompilerUtilities.Extensions;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.SymbolTables;
    using Desalt.Core.Tests.TestUtility;
    using Desalt.Core.Translation;
    using Desalt.Core.Validation;
    using FluentAssertions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public class NoDuplicateFieldAndPropertyNamesValidatorTests
    {
        [Test]
        public async Task Having_a_duplicate_field_and_property_name_in_a_class_should_log_a_diagnostic()
        {
            const string code = @"
public class C
{
    private string name;

    public string Name
    {
        get { return this.name; }
        set { this.name = value; }
    }
}
";

            using TempProject tempProject = await TempProject.CreateAsync(code);
            DocumentTranslationContextWithSymbolTables context =
                await tempProject.CreateContextWithSymbolTablesForFileAsync(
                    "File.cs",
                    discoveryKind: SymbolDiscoveryKind.OnlyDocumentTypes);

            var validator = new NoDuplicateFieldAndPropertyNamesValidator();
            IExtendedResult<bool> result = validator.Validate(context.ToSingleEnumerable().ToImmutableArray());

            VariableDeclaratorSyntax fieldDeclaration = context.RootSyntax.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .Single()
                .Declaration.Variables.First();

            result.Diagnostics.Select(d => d.ToString())
                .Should()
                .HaveCount(1)
                .And.BeEquivalentTo(
                    DiagnosticFactory.ClassWithDuplicateFieldAndPropertyName(
                            "C",
                            "name",
                            fieldDeclaration.GetLocation())
                        .ToString());
        }

        [Test]
        public async Task Having_a_duplicate_field_and_property_but_with_a_ScriptName_should_prevent_a_diagnostic()
        {
            const string code = @"
using System.Runtime.CompilerServices;

public class C
{
    private string name;

    [ScriptName(""differentName"")]
    public string Name
    {
        get { return this.name; }
        set { this.name = value; }
    }
}
";

            using TempProject tempProject = await TempProject.CreateAsync(code);
            DocumentTranslationContextWithSymbolTables context =
                await tempProject.CreateContextWithSymbolTablesForFileAsync(
                    "File.cs",
                    discoveryKind: SymbolDiscoveryKind.OnlyDocumentTypes);

            var validator = new NoDuplicateFieldAndPropertyNamesValidator();
            IExtendedResult<bool> result = validator.Validate(context.ToSingleEnumerable().ToImmutableArray());

            result.Diagnostics.Should().BeEmpty();
        }
    }
}
