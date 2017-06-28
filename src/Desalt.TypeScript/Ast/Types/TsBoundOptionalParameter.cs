﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsBoundOptionalParameter.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScript.Ast.Types
{
    using System;
    using Desalt.Core.Ast;
    using Desalt.Core.Emit;

    /// <summary>
    /// Represents a bound optional parameter in a function.
    /// </summary>
    internal class TsBoundOptionalParameter : AstNode, ITsBoundOptionalParameter
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public TsBoundOptionalParameter(
            ITsBindingIdentifierOrPattern parameterName,
            ITsAssignmentExpression initializer,
            ITsType parameterType = null,
            TsAccessibilityModifier? modifier = null)
        {
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
            Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
            ParameterType = parameterType;
            Modifier = modifier;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        public TsAccessibilityModifier? Modifier { get; }
        public ITsBindingIdentifierOrPattern ParameterName { get; }
        public ITsType ParameterType { get; }
        public ITsAssignmentExpression Initializer { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public void Accept(TsVisitor visitor) => visitor.VisitBoundOptionalParameter(this);

        public T Accept<T>(TsVisitor<T> visitor) => visitor.VisitBoundOptionalParameter(this);

        public override string CodeDisplay
        {
            get
            {
                string display = string.Empty;
                if (Modifier.HasValue)
                {
                    display = $"{Modifier.Value.ToString().ToLowerInvariant()} ";
                }

                display += $"{ParameterName}${ParameterType.ToTypeAnnotationCodeDisplay()} = {Initializer}";

                return display;
            }
        }

        public override void Emit(Emitter emitter)
        {
            if (Modifier.HasValue)
            {
                emitter.Write($"{Modifier.Value.ToString().ToLowerInvariant()} ");
            }

            ParameterName.Emit(emitter);
            ParameterType.WriteTypeAnnotation(emitter);

            emitter.Write(" = ");
            Initializer.Emit(emitter);
        }
    }
}
