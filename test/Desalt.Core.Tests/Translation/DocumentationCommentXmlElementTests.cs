﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentationCommentXmlElementTests.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Tests.Translation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Desalt.CompilerUtilities;
    using FluentAssertions;
    using NUnit.Framework;
    using XmlElem = Desalt.Core.Translation.DocumentationCommentXmlElement;

    public class DocumentationCommentXmlElementTests
    {
        private static void VerifyElement(
            string text,
            string expectedElementName,
            string expectedContent = "",
            params (string name, string value)[] expectedAttributes)
        {
            using var reader = new PeekingTextReader(text);
            var actual = XmlElem.Parse(reader) ?? throw new InvalidOperationException("Parse failed");
            actual.ElementName.Should().Be(expectedElementName);
            actual.Content.Should().Be(expectedContent);

            actual.Attributes.Select(pair => (pair.Key, pair.Value)).Should().BeEquivalentTo(expectedAttributes);
        }

        [Test]
        public void Create_should_throw_on_blank_element_name()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => XmlElem.Create(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("elementName");
        }

        [Test]
        public void Create_should_convert_a_null_attribute_list_to_an_empty_dictionary()
        {
            XmlElem.Create("element").Attributes.Should().BeEmpty();
        }

        [Test]
        public void Create_should_convert_a_null_content_value_to_an_empty_string()
        {
            XmlElem.Create("element").Content.Should().BeSameAs(string.Empty);
        }

        [Test]
        public void Parse_should_throw_an_exception_if_the_reader_is_not_positioned_at_a_lt_character()
        {
            using var reader = new PeekingTextReader("not valid");

            // ReSharper disable once AccessToDisposedClosure
            Action action = () => XmlElem.Parse(reader);
            action.Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void Parse_should_get_the_element_name_for_an_empty_element()
        {
            VerifyElement("<c/>", "c");
            VerifyElement("<c  />", "c");
        }

        [Test]
        public void Parse_should_get_the_content_of_an_element()
        {
            VerifyElement("<a>content</a>", "a", "content");
            VerifyElement("<a  >content</a  >", "a", "content");
        }

        [Test]
        public void Parse_should_get_the_attributes_in_an_empty_element()
        {
            VerifyElement("<a one=\"1\" two=\"2\"/>", "a", "", ("one", "1"), ("two", "2"));
            VerifyElement("<a one = \"1\"  two = \"2\"  />", "a", "", ("one", "1"), ("two", "2"));
        }

        [Test]
        public void Parse_should_get_the_attributes_in_an_element()
        {
            VerifyElement("<a one=\"1\" two=\"2\">content</a>", "a", "content", ("one", "1"), ("two", "2"));
            VerifyElement("<a one = \"1\"  two = \"2\"  >content</a>", "a", "content", ("one", "1"), ("two", "2"));
        }

        [Test]
        public void ToString_should_just_show_the_element_name_if_there_is_no_content_or_attributes()
        {
            XmlElem.Create("element").ToString().Should().Be("<element/>");
        }

        [Test]
        public void ToString_should_include_the_contents_if_present()
        {
            XmlElem.Create("element", content: "content").ToString().Should().Be("<element>content</element>");
        }

        [Test]
        public void ToString_should_include_any_attributes_in_an_empty_element_in_sorted_order()
        {
            XmlElem.Create(
                    "a",
                    new Dictionary<string, string>
                    {
                        ["x"] = "y",
                        ["b"] = "c",
                        ["d"] = "e"
                    })
                .ToString()
                .Should()
                .Be("<a b=\"c\" d=\"e\" x=\"y\"/>");
        }

        [Test]
        public void ToString_should_include_any_attributes_in_an_element_in_sorted_order()
        {
            XmlElem.Create(
                    "a",
                    new Dictionary<string, string>
                    {
                        ["x"] = "y",
                        ["b"] = "c",
                        ["d"] = "e"
                    },
                    "content")
                .ToString()
                .Should()
                .Be("<a b=\"c\" d=\"e\" x=\"y\">content</a>");
        }
    }
}
