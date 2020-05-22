﻿
// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsTriviaNodes.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// <auto-generated/>
// ---------------------------------------------------------------------------------------------------------------------

// DO NOT HAND-MODIFY. This is auto-generated code from the template file 'TsTriviaNodes.tt'.
// ReSharper disable ArrangeMethodOrOperatorBody
// ReSharper disable CheckNamespace
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable RedundantUsingDirective
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

#nullable enable

namespace Desalt.TypeScriptAst.Ast
{
    using System;
    using System.Collections.Immutable;
    using Desalt.TypeScriptAst.Emit;

    //// ===============================================================================================================
    //// WhitespaceTrivia
    //// ===============================================================================================================

    /// <summary>
    /// Represents whitespace that can appear before or after another <see cref="ITsAstNode" />.
    /// </summary>
    public interface ITsWhitespaceTrivia : ITsAstTriviaNode
    {
        string Text { get; }
        bool IsNewline { get; }
    }

    /// <summary>
    /// Represents whitespace that can appear before or after another <see cref="ITsAstNode" />.
    /// </summary>
    internal partial class TsWhitespaceTrivia : TsAstTriviaNode, ITsWhitespaceTrivia
    {
        public TsWhitespaceTrivia(string text, bool isNewline, bool preserveSpacing)
            : base(preserveSpacing)
        {
            Text = text;
            IsNewline = isNewline;
        }

        public string Text { get; }
        public bool IsNewline { get; }

        public override void Emit(Emitter emitter) => TsAstEmitter.EmitWhitespaceTrivia(emitter, this);
    }

    public static class WhitespaceTriviaExtensions
    {
        public static ITsWhitespaceTrivia WithText(this ITsWhitespaceTrivia node, string value) =>
            node.Text == value ? node : new TsWhitespaceTrivia(value, node.IsNewline, node.PreserveSpacing);

        public static ITsWhitespaceTrivia WithIsNewline(this ITsWhitespaceTrivia node, bool value) =>
            node.IsNewline == value ? node : new TsWhitespaceTrivia(node.Text, value, node.PreserveSpacing);
    }

    //// ===============================================================================================================
    //// MultiLineComment
    //// ===============================================================================================================

    /// <summary>
    /// Represents a TypeScript multi-line comment of the form '/* lines */'.
    /// </summary>
    public interface ITsMultiLineComment : ITsAstTriviaNode
    {
        /// <summary>
        /// Indicates whether the comment should start with '/**' (JsDoc) or '/*'.
        /// </summary>
        bool IsJsDoc { get; }
        ImmutableArray<string> Lines { get; }
    }

    /// <summary>
    /// Represents a TypeScript multi-line comment of the form '/* lines */'.
    /// </summary>
    internal partial class TsMultiLineComment : TsAstTriviaNode, ITsMultiLineComment
    {
        public TsMultiLineComment(bool isJsDoc, ImmutableArray<string> lines, bool preserveSpacing)
            : base(preserveSpacing)
        {
            IsJsDoc = isJsDoc;
            Lines = lines;
        }

        /// <summary>
        /// Indicates whether the comment should start with '/**' (JsDoc) or '/*'.
        /// </summary>
        public bool IsJsDoc { get; }
        public ImmutableArray<string> Lines { get; }

        public override void Emit(Emitter emitter) => TsAstEmitter.EmitMultiLineComment(emitter, this);
    }

    public static class MultiLineCommentExtensions
    {
        public static ITsMultiLineComment WithIsJsDoc(this ITsMultiLineComment node, bool value) =>
            node.IsJsDoc == value ? node : new TsMultiLineComment(value, node.Lines, node.PreserveSpacing);

        public static ITsMultiLineComment WithLines(this ITsMultiLineComment node, ImmutableArray<string> value) =>
            node.Lines == value ? node : new TsMultiLineComment(node.IsJsDoc, value, node.PreserveSpacing);
    }

    //// ===============================================================================================================
    //// SingleLineComment
    //// ===============================================================================================================

    /// <summary>
    /// Represents a TypeScript single-line comment of the form '// comment'.
    /// </summary>
    public interface ITsSingleLineComment : ITsAstTriviaNode
    {
        string Text { get; }
        bool OmitNewLineAtEnd { get; }
    }

    /// <summary>
    /// Represents a TypeScript single-line comment of the form '// comment'.
    /// </summary>
    internal partial class TsSingleLineComment : TsAstTriviaNode, ITsSingleLineComment
    {
        public TsSingleLineComment(string text, bool omitNewLineAtEnd, bool preserveSpacing)
            : base(preserveSpacing)
        {
            Text = text;
            OmitNewLineAtEnd = omitNewLineAtEnd;
        }

        public string Text { get; }
        public bool OmitNewLineAtEnd { get; }

        public override void Emit(Emitter emitter) => TsAstEmitter.EmitSingleLineComment(emitter, this);
    }

    public static class SingleLineCommentExtensions
    {
        public static ITsSingleLineComment WithText(this ITsSingleLineComment node, string value) =>
            node.Text == value ? node : new TsSingleLineComment(value, node.OmitNewLineAtEnd, node.PreserveSpacing);

        public static ITsSingleLineComment WithOmitNewLineAtEnd(this ITsSingleLineComment node, bool value) =>
            node.OmitNewLineAtEnd == value ? node : new TsSingleLineComment(node.Text, value, node.PreserveSpacing);
    }
}