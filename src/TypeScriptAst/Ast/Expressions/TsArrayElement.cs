﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsArrayElement.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace TypeScriptAst.Ast.Expressions
{
    using System;
    using TypeScriptAst.Emit;

    /// <summary>
    /// Represents an element in an array.
    /// </summary>
    internal class TsArrayElement : TsAstNode, ITsArrayElement
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TsArrayElement(ITsExpression element, bool isSpreadElement = false)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            IsSpreadElement = isSpreadElement;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public ITsExpression Element { get; }

        /// <summary>
        /// Indicates whether the <see cref="ITsArrayElement.Element"/> is preceded by a spread operator '...'.
        /// </summary>
        public bool IsSpreadElement { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public override void Accept(TsVisitor visitor) => visitor.VisitArrayElement(this);

        public override string CodeDisplay => (IsSpreadElement ? "... " : "") + Element.CodeDisplay;

        protected override void EmitInternal(Emitter emitter)
        {
            if (IsSpreadElement)
            {
                emitter.Write("... ");
            }

            Element.Emit(emitter);
        }
    }
}
