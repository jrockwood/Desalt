﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlFragmentParserTests.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Tests.Utility
{
    using System;
    using System.Xml;
    using Desalt.Core.Utility;
    using FluentAssertions;
    using NUnit.Framework;

    public class XmlFragmentParserTests
    {
        [Test]
        public void ParseFragment_should_work_on_simple_text()
        {
            int callCount = 0;

            void ParseCallback(XmlReader reader, object arg)
            {
                callCount++;
                reader.NodeType.Should().Be(XmlNodeType.Text);
                reader.Value.Should().Be("text");
                reader.Read();
            }

            var parser = new XmlFragmentParser();
            parser.ParseFragment("text", ParseCallback, new object());
            callCount.Should().Be(1);
        }

        [Test]
        public void ParseFragment_should_work_on_a_single_element_with_no_attributes_and_a_value()
        {
            int callCount = 0;

            void ParseCallback(XmlReader reader, object arg)
            {
                callCount++;
                reader.NodeType.Should().Be(XmlNodeType.Element);
                reader.LocalName.Should().Be("summary");
                reader.ReadInnerXml().Should().Be("Value");
            }

            var parser = new XmlFragmentParser();
            parser.ParseFragment("<summary>Value</summary>", ParseCallback, new object());
            callCount.Should().Be(1);
        }

        [Test]
        public void ParseFragment_should_work_on_an_element_with_attributes()
        {
            int callCount = 0;

            void ParseCallback(XmlReader reader, object arg)
            {
                callCount++;
                reader.NodeType.Should().Be(XmlNodeType.Element);
                reader.LocalName.Should().Be("param");
                reader.GetAttribute("name").Should().Be("arg");
                reader.ReadInnerXml().Should().Be("value");
            }

            var parser = new XmlFragmentParser();
            parser.ParseFragment(@"<param name=""arg"">value</param>", ParseCallback, new object());
            callCount.Should().Be(1);
        }

        [Test]
        public void ParseFragment_should_rethrow_any_errors()
        {
            void ParseCallback(XmlReader reader, object arg) => reader.ReadInnerXml();

            Action action = () => new XmlFragmentParser().ParseFragment(@"<invalidTag>a</tag>", ParseCallback, new object());
            action.Should().Throw<XmlException>();
        }
    }
}
