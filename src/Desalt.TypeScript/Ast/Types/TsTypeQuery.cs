﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsTypeQuery.cs" company="Justin Rockwood">
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
    /// Represents a 'typeof' query.
    /// </summary>
    internal class TsTypeQuery : AstNode, ITsTypeQuery
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TsTypeQuery(ITsTypeQueryExpression query)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public ITsTypeQueryExpression Query { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public void Accept(TsVisitor visitor) => visitor.VisitTypeQuery(this);

        public T Accept<T>(TsVisitor<T> visitor) => visitor.VisitTypeQuery(this);

        public override string ToCodeDisplay() => $"typeof {Query}";

        public override void WriteFullCodeDisplay(IndentedTextWriter writer)
        {
            writer.Write("typeof ");
            Query.WriteFullCodeDisplay(writer);
        }
    }
}
