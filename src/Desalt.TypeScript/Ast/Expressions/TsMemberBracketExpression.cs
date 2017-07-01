﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsMemberBracketExpression.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScript.Ast.Expressions
{
    using System;
    using Desalt.Core.Ast;
    using Desalt.Core.Emit;

    /// <summary>
    /// Represents a member expression of the form 'expression[expression]'.
    /// </summary>
    internal class TsMemberBracketExpression : AstNode<TsVisitor>, ITsMemberBracketExpression
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TsMemberBracketExpression(ITsExpression leftSide, ITsExpression bracketContents)
        {
            LeftSide = leftSide ?? throw new ArgumentNullException(nameof(leftSide));
            BracketContents = bracketContents ?? throw new ArgumentNullException(nameof(bracketContents));
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public ITsExpression LeftSide { get; }
        public ITsExpression BracketContents { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public override void Accept(TsVisitor visitor) => visitor.VisitMemberBracketExpression(this);

        public override string CodeDisplay => $"{LeftSide}[{BracketContents}]";

        public override void Emit(Emitter emitter)
        {
            LeftSide.Emit(emitter);
            emitter.Write("[");
            BracketContents.Emit(emitter);
            emitter.Write("]");
        }
    }
}
