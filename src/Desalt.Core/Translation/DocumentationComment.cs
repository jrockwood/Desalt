﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentationComment.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Translation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml;
    using Desalt.Core.Utility;
    using Microsoft.CodeAnalysis;
    using XmlNames = DocumentationCommentXmlNames;

    /// <summary>
    /// A documentation comment derived from either source text or metadata.
    /// </summary>
    /// <remarks>Modified from the Roslyn source code at <see href="http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Shared/Utilities/DocumentationComment.cs"/></remarks>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    internal sealed class DocumentationComment
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        /// <summary>
        /// Used for <see cref="CommentBuilder.TrimEachLine"/> method, to prevent new allocation of string
        /// </summary>
        private static readonly string[] s_newLineAsStringArray = { "\n" };

        private readonly Dictionary<string, string> _parameterTexts = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _typeParameterTexts = new Dictionary<string, string>();

        private readonly Dictionary<string, ImmutableArray<string>> _exceptionTexts =
            new Dictionary<string, ImmutableArray<string>>();

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        private DocumentationComment(string fullXmlFragment, ISymbol symbol)
        {
            FullXmlFragment = fullXmlFragment ?? throw new ArgumentNullException(nameof(fullXmlFragment));
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));

            ParameterNames = ImmutableArray<string>.Empty;
            TypeParameterNames = ImmutableArray<string>.Empty;
            ExceptionTypes = ImmutableArray<string>.Empty;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        /// <summary>
        /// True if an error occurred when parsing.
        /// </summary>
        public bool HadXmlParseError { get; private set; }

        /// <summary>
        /// The full XML text of this tag.
        /// </summary>
        public string FullXmlFragment { get; }

        /// <summary>
        /// The text in the &lt;example&gt; tag. Null if no tag existed.
        /// </summary>
        public string? ExampleText { get; private set; }

        /// <summary>
        /// The text in the &lt;summary&gt; tag. Null if no tag existed.
        /// </summary>
        public string? SummaryText { get; private set; }

        /// <summary>
        /// The text in the &lt;returns&gt; tag. Null if no tag existed.
        /// </summary>
        public string? ReturnsText { get; private set; }

        /// <summary>
        /// The text in the &lt;remarks&gt; tag. Null if no tag existed.
        /// </summary>
        public string? RemarksText { get; private set; }

        /// <summary>
        /// The names of items in &lt;param&gt; tags.
        /// </summary>
        public ImmutableArray<string> ParameterNames { get; private set; }

        /// <summary>
        /// The names of items in &lt;typeparam&gt; tags.
        /// </summary>
        public ImmutableArray<string> TypeParameterNames { get; private set; }

        /// <summary>
        /// The types of items in &lt;exception&gt; tags.
        /// </summary>
        public ImmutableArray<string> ExceptionTypes { get; private set; }

        /// <summary>
        /// The C# symbol associated with this documentation comment.
        /// </summary>
        public ISymbol Symbol { get; }

        // ReSharper disable once CommentTypo
        /// <summary>
        /// The item named in the &lt;completionlist&gt; tag's cref attribute.
        /// Null if the tag or cref attribute didn't exist.
        /// </summary>
        public string? CompletionListCref { get; private set; }

        private string DebuggerDisplay
        {
            // ReSharper disable once UnusedMember.Local
            get
            {
                if (SummaryText != null)
                {
                    return $"<summary>{SummaryText}</summary>";
                }

                if (RemarksText != null)
                {
                    return $"<remarks>{RemarksText}</remarks>";
                }

                return FullXmlFragment;
            }
        }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        /// <summary>
        /// Parses and constructs a <see cref="DocumentationComment"/> from the given fragment of XML.
        /// </summary>
        /// <param name="xml">The fragment of XML to parse.</param>
        /// <param name="symbol">The symbol associated with the fragment.</param>
        /// <returns>A DocumentationComment instance.</returns>
        public static DocumentationComment FromXmlFragment(string xml, ISymbol symbol)
        {
            DocumentationComment result = CommentBuilder.Parse(xml, symbol);
            return result;
        }

        /// <summary>
        /// Returns the text for a given parameter, or null if no documentation was given for the parameter.
        /// </summary>
        public string? GetParameterText(string parameterName)
        {
            _parameterTexts.TryGetValue(parameterName, out string? text);
            return text;
        }

        /// <summary>
        /// Returns the text for a given type parameter, or null if no documentation was given for the type parameter.
        /// </summary>
        public string? GetTypeParameterText(string typeParameterName)
        {
            _typeParameterTexts.TryGetValue(typeParameterName, out string? text);
            return text;
        }

        /// <summary>
        /// Returns the texts for a given exception, or an empty <see cref="ImmutableArray"/> if no
        /// documentation was given for the exception.
        /// </summary>
        public ImmutableArray<string> GetExceptionTexts(string exceptionName)
        {
            _exceptionTexts.TryGetValue(exceptionName, out ImmutableArray<string> texts);

            if (texts.IsDefault)
            {
                // If the exception wasn't found, TryGetValue will set "texts" to a default value.
                // To be friendly, we want to return an empty array rather than a null array.
                texts = ImmutableArray.Create<string>();
            }

            return texts;
        }

        //// ===========================================================================================================
        //// Classes
        //// ===========================================================================================================

        /// <summary>
        /// Helper class for parsing XML doc comments. Encapsulates the state required during parsing.
        /// </summary>
        private sealed class CommentBuilder
        {
            private readonly DocumentationComment _comment;
            private ImmutableArray<string>.Builder? _parameterNamesBuilder;
            private ImmutableArray<string>.Builder? _typeParameterNamesBuilder;
            private ImmutableArray<string>.Builder? _exceptionTypesBuilder;
            private Dictionary<string, ImmutableArray<string>.Builder>? _exceptionTextBuilders;

            /// <summary>
            /// Parse and construct a <see cref="DocumentationComment" /> from the given fragment of XML.
            /// </summary>
            /// <param name="xml">The fragment of XML to parse.</param>
            /// <param name="symbol">The symbol associated with the fragment.</param>
            /// <returns>A DocumentationComment instance.</returns>
            public static DocumentationComment Parse(string xml, ISymbol symbol)
            {
                try
                {
                    return new CommentBuilder(xml, symbol).ParseInternal(xml);
                }
                catch (Exception)
                {
                    // It would be nice if we only had to catch XmlException to handle invalid XML
                    // while parsing doc comments. Unfortunately, other exceptions can also occur,
                    // so we just catch them all. See Dev12 Bug 612456 for an example.
                    return new DocumentationComment(xml, symbol)
                    {
                        HadXmlParseError = true
                    };
                }
            }

            private CommentBuilder(string xml, ISymbol symbol)
            {
                _comment = new DocumentationComment(xml, symbol);
            }

            private DocumentationComment ParseInternal(string xml)
            {
                var fragmentParser = new XmlFragmentParser();
                fragmentParser.ParseFragment(xml, ParseCallback, this);

                if (_exceptionTextBuilders != null)
                {
                    foreach (KeyValuePair<string, ImmutableArray<string>.Builder> typeAndBuilderPair in _exceptionTextBuilders)
                    {
                        _comment._exceptionTexts.Add(typeAndBuilderPair.Key, typeAndBuilderPair.Value.ToImmutable());
                    }
                }

                _comment.ParameterNames = _parameterNamesBuilder?.ToImmutable() ?? ImmutableArray<string>.Empty;
                _comment.TypeParameterNames = _typeParameterNamesBuilder?.ToImmutable() ?? ImmutableArray<string>.Empty;
                _comment.ExceptionTypes = _exceptionTypesBuilder?.ToImmutable() ?? ImmutableArray<string>.Empty;

                return _comment;
            }

            private static void ParseCallback(XmlReader reader, CommentBuilder builder)
            {
                builder.ParseCallback(reader);
            }

            private static string TrimEachLine(string text)
            {
                return string.Join(
                    Environment.NewLine,
                    text.Split(s_newLineAsStringArray, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()));
            }

            private void ParseCallback(XmlReader reader)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    string localName = reader.LocalName;
                    if (XmlNames.ElementEquals(localName, XmlNames.ExampleElementName) && _comment.ExampleText == null)
                    {
                        _comment.ExampleText = TrimEachLine(reader.ReadInnerXml());
                    }
                    else if (XmlNames.ElementEquals(localName, XmlNames.SummaryElementName) &&
                        _comment.SummaryText == null)
                    {
                        _comment.SummaryText = TrimEachLine(reader.ReadInnerXml());
                    }
                    else if (XmlNames.ElementEquals(localName, XmlNames.ReturnsElementName) &&
                        _comment.ReturnsText == null)
                    {
                        _comment.ReturnsText = TrimEachLine(reader.ReadInnerXml());
                    }
                    else if (XmlNames.ElementEquals(localName, XmlNames.RemarksElementName) &&
                        _comment.RemarksText == null)
                    {
                        _comment.RemarksText = TrimEachLine(reader.ReadInnerXml());
                    }
                    else if (XmlNames.ElementEquals(localName, XmlNames.ParameterElementName))
                    {
                        string name = reader.GetAttribute(XmlNames.NameAttributeName);
                        string paramText = reader.ReadInnerXml();

                        if (!string.IsNullOrWhiteSpace(name) && !_comment._parameterTexts.ContainsKey(name))
                        {
                            (_parameterNamesBuilder ??= ImmutableArray.CreateBuilder<string>()).Add(name);
                            _comment._parameterTexts.Add(name, TrimEachLine(paramText));
                        }
                    }
                    else if (XmlNames.ElementEquals(localName, XmlNames.TypeParameterElementName))
                    {
                        string name = reader.GetAttribute(XmlNames.NameAttributeName);
                        string typeParamText = reader.ReadInnerXml();

                        if (!string.IsNullOrWhiteSpace(name) && !_comment._typeParameterTexts.ContainsKey(name))
                        {
                            (_typeParameterNamesBuilder ??= ImmutableArray.CreateBuilder<string>()).Add(name);
                            _comment._typeParameterTexts.Add(name, TrimEachLine(typeParamText));
                        }
                    }
                    else if (XmlNames.ElementEquals(localName, XmlNames.ExceptionElementName))
                    {
                        string type = reader.GetAttribute(XmlNames.CrefAttributeName);
                        string exceptionText = reader.ReadInnerXml();

                        if (!string.IsNullOrWhiteSpace(type))
                        {
                            if (_exceptionTextBuilders == null || !_exceptionTextBuilders.ContainsKey(type))
                            {
                                (_exceptionTypesBuilder ??= ImmutableArray.CreateBuilder<string>()).Add(type);
                                (_exceptionTextBuilders ??= new Dictionary<string, ImmutableArray<string>.Builder>())
                                    .Add(type, ImmutableArray.CreateBuilder<string>());
                            }

                            _exceptionTextBuilders[type].Add(exceptionText);
                        }
                    }
                    else if (XmlNames.ElementEquals(localName, XmlNames.CompletionListElementName))
                    {
                        string cref = reader.GetAttribute(XmlNames.CrefAttributeName);
                        if (!string.IsNullOrWhiteSpace(cref))
                        {
                            _comment.CompletionListCref = cref;
                        }

                        reader.ReadInnerXml();
                    }
                    else
                    {
                        // This is an element we don't handle. Skip it.
                        reader.Read();
                    }
                }
                else
                {
                    // We came across something that isn't a start element, like a block of text.
                    // Skip it.
                    reader.Read();
                }
            }
        }
    }
}
