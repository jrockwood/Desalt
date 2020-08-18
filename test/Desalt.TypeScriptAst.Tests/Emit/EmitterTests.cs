// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="EmitterTests.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScriptAst.Tests.Emit
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Desalt.CompilerUtilities.Extensions;
    using Desalt.TypeScriptAst.Ast;
    using Desalt.TypeScriptAst.Emit;
    using FluentAssertions;
    using NUnit.Framework;

    public class EmitterTests
    {
        private static readonly EmitOptions s_testOptions = EmitOptions.UnixTabs;

        private static readonly ITsAstNode[] s_statements =
        {
            new Identifier("One"),
            new Identifier("Two"),
            new Identifier("Three")
        };

        private sealed class Identifier : ITsAstNode
        {
            public Identifier(string name)
            {
                Name = name;
            }

            private string Name { get; }

            public string CodeDisplay => Name;

            public ImmutableArray<ITsAstTriviaNode> LeadingTrivia { get; } = ImmutableArray<ITsAstTriviaNode>.Empty;
            public ImmutableArray<ITsAstTriviaNode> TrailingTrivia { get; } = ImmutableArray<ITsAstTriviaNode>.Empty;

            public void Accept(TsVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public void Emit(Emitter emitter)
            {
                emitter.Write(Name);
            }

            public string EmitAsString(EmitOptions? options = null)
            {
                return Name;
            }

            public ITsAstNode ShallowCopy(
                ImmutableArray<ITsAstTriviaNode> leadingTrivia,
                ImmutableArray<ITsAstTriviaNode> trailingTrivia)
            {
                throw new NotImplementedException();
            }

            public bool Equals(ITsAstNode? other)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void Ctor_should_throw_on_null_args()
        {
            // ReSharper disable ObjectCreationAsStatement
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => new Emitter(outputStream: null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("outputStream");
            // ReSharper restore ObjectCreationAsStatement
        }

        [Test]
        public void Ctor_should_store_the_encoding_parameter_in_the_property()
        {
            var emitter = new Emitter(new MemoryStream(), Encoding.ASCII);
            emitter.Encoding.Should().BeSameAs(Encoding.ASCII);
        }

        [Test]
        public void Ctor_should_use_UTF8_no_BOM_encoding_if_not_supplied()
        {
            var emitter = new Emitter(new MemoryStream());
            emitter.Encoding.EncodingName.Should().Be(Encoding.UTF8.EncodingName);
            emitter.Encoding.GetPreamble().Should().BeEmpty();
        }

        [Test]
        public void Ctor_should_store_the_options_parameter_in_the_property()
        {
            EmitOptions options = EmitOptions.Default.WithIndentationPrefix("\v");
            var emitter = new Emitter(new MemoryStream(), options: options);
            emitter.Options.Should().BeSameAs(options);
        }

        [Test]
        public void WriteBlock_should_throw_on_null_args()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => emitter.WriteBlock(items: null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("items");
        }

        [Test]
        public void WriteBlock_should_surround_the_body_with_braces_and_indent()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream, options: s_testOptions);
            emitter.WriteBlock(new[] { new Identifier("body") });
            stream.ReadAllText().Should().Be("{\n\tbody\n}");
        }

        [Test]
        public void WriteBlock_should_add_newlines_between_empty_block_braces_if_the_options_specify_it()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream, options: s_testOptions);
            emitter.WriteBlock(ImmutableArray<ITsAstNode>.Empty);
            stream.ReadAllText().Should().Be("{ }");
        }

        [Test]
        public void WriteBlock_should_add_a_space_between_empty_block_braces_if_the_options_specify_it()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream, options: s_testOptions);
            emitter.WriteBlock(ImmutableArray<ITsAstNode>.Empty);
            stream.ReadAllText().Should().Be("{ }");
        }

        [Test]
        public void WriteBlock_should_write_all_of_the_statements()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream, options: s_testOptions);
            emitter.WriteBlock(s_statements);
            stream.ReadAllText().Should().Be("{\n\tOne\n\tTwo\n\tThree\n}");
        }

        [Test]
        public void WriteCommaNewlineSeparatedBlock_should_throw_on_null_args()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => emitter.WriteCommaNewlineSeparatedBlock(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("items");
        }

        [Test]
        public void WriteCommaNewlineSeparatedBlock_should_add_commas_and_newlines_between_elements()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream, options: s_testOptions);
            emitter.WriteCommaNewlineSeparatedBlock(s_statements);
            stream.ReadAllText().Should().Be("{\n\tOne,\n\tTwo,\n\tThree\n}");
        }

        [Test]
        public void WriteParameterList_should_throw_on_null_args()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => emitter.WriteParameterList(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("items");
        }

        [Test]
        public void WriteParameterList_should_write_all_of_the_parameters()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream, options: s_testOptions);
            emitter.WriteParameterList(s_statements);
            stream.ReadAllText().Should().Be("(One, Two, Three)");
        }

        [Test]
        public void WriteItems_should_throw_on_null_args()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => emitter.WriteList(null, true);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("items");
        }

        [Test]
        public void WriteItems_should_add_delimiters_between_elements()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream);
            emitter.WriteList(s_statements, indent: false, itemDelimiter: "-");
            stream.ReadAllText().Should().Be("One-Two-Three");
        }

        [Test]
        public void WriteList_should_not_add_a_delimiter_when_there_is_only_one_element()
        {
            using var stream = new MemoryStream();
            var emitter = new Emitter(stream);
            emitter.WriteList(s_statements.Take(1).ToImmutableArray(), indent: false, itemDelimiter: "-");
            stream.ReadAllText().Should().Be("One");
        }
    }
}
