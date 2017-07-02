﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsDestructuringVariableDeclaration.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScript.Ast.Statements
{
    using System;
    using Desalt.Core.Ast;
    using Desalt.Core.Emit;

    /// <summary>
    /// Represents a destructuring variable declaration of the form '{x, y} = foo' or '[x, y] = foo'.
    /// </summary>
    internal class TsDestructuringVariableDeclaration : AstNode<TsVisitor>, ITsDestructuringVariableDeclaration
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TsDestructuringVariableDeclaration(
            ITsBindingPattern bindingPattern,
            ITsExpression initializer,
            ITsType variableType = null)
        {
            BindingPattern = bindingPattern ?? throw new ArgumentNullException(nameof(bindingPattern));
            Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
            VariableType = variableType;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public ITsBindingPattern BindingPattern { get; }
        public ITsType VariableType { get; }
        public ITsExpression Initializer { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public override void Accept(TsVisitor visitor) => visitor.VisitDestructuringVariableDeclaration(this);

        public override string CodeDisplay =>
            $"{BindingPattern}{VariableType.ToTypeAnnotationCodeDisplay()}{Initializer.ToAssignmentCodeDisplay()}";

        public override void Emit(Emitter emitter)
        {
            BindingPattern.Emit(emitter);
            VariableType.EmitTypeAnnotation(emitter);
            Initializer.EmitAssignment(emitter);
        }
    }
}
