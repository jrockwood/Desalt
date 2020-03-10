﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsEmptyStatement.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScriptAst.Ast.Statements
{
    using Desalt.TypeScriptAst.Emit;

    /// <summary>
    /// Represents an empty statement.
    /// </summary>
    internal class TsEmptyStatement : TsAstNode, ITsEmptyStatement
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        public static readonly TsEmptyStatement Instance = new TsEmptyStatement();

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        private TsEmptyStatement()
        {
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public override void Accept(TsVisitor visitor) => visitor.VisitEmptyStatement(this);

        public override string CodeDisplay => ";";

        protected override void EmitInternal(Emitter emitter) => emitter.WriteLine(";");
    }
}