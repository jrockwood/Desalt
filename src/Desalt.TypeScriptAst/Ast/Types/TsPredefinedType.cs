// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsPredefinedType.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScriptAst.Ast.Types
{
    using System;
    using Desalt.TypeScriptAst.Emit;

    /// <summary>
    /// Represents one of the predefined types: any, number, boolean, string, symbol, void.
    /// </summary>
    internal class TsPredefinedType : TsAstNode, ITsType
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        public static readonly TsPredefinedType Any = new TsPredefinedType("any");
        public static readonly TsPredefinedType Boolean = new TsPredefinedType("boolean");
        public static readonly TsPredefinedType Null = new TsPredefinedType("null");
        public static readonly TsPredefinedType Number = new TsPredefinedType("number");
        public static readonly TsPredefinedType String = new TsPredefinedType("string");
        public static readonly TsPredefinedType Symbol = new TsPredefinedType("symbol");
        public static readonly TsPredefinedType Void = new TsPredefinedType("void");

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        private TsPredefinedType(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public string Name { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public override void Accept(TsVisitor visitor)
        {
            visitor.VisitPredefinedType(this);
        }

        public override string CodeDisplay => Name;

        protected override void EmitContent(Emitter emitter)
        {
            emitter.Write(Name);
        }
    }
}
