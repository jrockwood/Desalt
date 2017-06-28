﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Es5FunctionExpression.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.JavaScript.Ast.Expressions
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Desalt.Core.Ast;
    using Desalt.Core.Utility;

    /// <summary>
    /// Represents a function expression of the form 'function name?(parameters) { body }'.
    /// </summary>
    public sealed class Es5FunctionExpression : Es5AstNode, IEs5Expression
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        internal Es5FunctionExpression(
            string functionName,
            IEnumerable<Es5Identifier> parameters,
            IEnumerable<IEs5SourceElement> functionBody)
        {
            FunctionName = functionName;
            Parameters = parameters?.ToImmutableArray() ?? ImmutableArray<Es5Identifier>.Empty;
            FunctionBody = functionBody?.ToImmutableArray() ?? ImmutableArray<IEs5SourceElement>.Empty;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public string FunctionName { get; }
        public ImmutableArray<Es5Identifier> Parameters { get; }
        public ImmutableArray<IEs5SourceElement> FunctionBody { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public override void Accept(Es5Visitor visitor)
        {
            visitor.VisitFunctionExpression(this);
        }

        public override T Accept<T>(Es5Visitor<T> visitor)
        {
            return visitor.VisitFunctionExpression(this);
        }

        public override string CodeDisplay => $"function {FunctionName}({Parameters.ToElidedList()}) {{...}}";

        public override void WriteFullCodeDisplay(IndentedTextWriter writer)
        {
            writer.Write("function");
            if (FunctionName != null)
            {
                writer.Write($" {FunctionName}");
            }
            WriteParameterList(writer, Parameters);
            writer.Write(" ");
            WriteBlock(writer, FunctionBody);
        }
    }
}
