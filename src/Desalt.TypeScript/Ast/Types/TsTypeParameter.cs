﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsTypeParameter.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScript.Ast.Types
{
    using System;
    using Desalt.Core.Ast;
    using Desalt.Core.Utility;

    /// <summary>
    /// Represents a TypeScript type parameter, for example &lt;MyType extends MyBase&gt;.
    /// </summary>
    internal class TsTypeParameter : AstNode, ITsTypeParameter
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TsTypeParameter(ITsIdentifier typeName, ITsType constraint = null)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            Constraint = constraint;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public ITsIdentifier TypeName { get; }
        public ITsType Constraint { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public void Accept(TsVisitor visitor) => visitor.VisitTypeParameter(this);

        public T Accept<T>(TsVisitor<T> visitor) => visitor.VisitTypeParameter(this);

        public override string ToCodeDisplay() =>
            TypeName.ToCodeDisplay() + (Constraint != null ? $" extends {Constraint}" : "");

        public override void WriteFullCodeDisplay(IndentedTextWriter writer)
        {
            TypeName.WriteFullCodeDisplay(writer);

            if (Constraint != null)
            {
                writer.Write(" extends ");
                Constraint.WriteFullCodeDisplay(writer);
            }
        }
    }
}