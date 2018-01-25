﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsReturnStatement.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.TypeScript.Ast.Statements
{
    using Desalt.Core.Ast;
    using Desalt.Core.Emit;

    /// <summary>
    /// Represents a 'return' statement.
    /// </summary>
    internal class TsReturnStatement : AstNode<TsVisitor>, ITsReturnStatement
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TsReturnStatement(ITsExpression expression = null)
        {
            Expression = expression;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public ITsExpression Expression { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public override void Accept(TsVisitor visitor) => visitor.VisitReturnStatement(this);

        public override string CodeDisplay => "return" + (Expression != null ? $" {Expression}" : "") + ";";

        public override void Emit(Emitter emitter)
        {
            emitter.Write("return");

            if (Expression != null)
            {
                emitter.Write(" ");
                Expression.Emit(emitter);
            }

            emitter.WriteLine(";");
        }
    }
}
