﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsGrammarExtensions.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScript.Ast
{
    using System;
    using Desalt.Core.Ast;
    using Desalt.Core.Emit;
    using Desalt.TypeScript.Ast.Expressions;
    using Desalt.TypeScript.Ast.Statements;
    using Desalt.TypeScript.Ast.Types;

    /// <summary>
    /// Contains static extension methods for <see cref="IAstNode"/> objects.
    /// </summary>
    public static class TsGrammarExtensions
    {
        /// <summary>
        /// Creates a new <see cref="ITsParenthesizedType"/> wrapping the specified type.
        /// </summary>
        /// <param name="type">The type to wrap inside of parentheses.</param>
        /// <returns>A new <see cref="ITsParenthesizedType"/> wrapping the specified type.</returns>
        public static ITsParenthesizedType WithParentheses(this ITsType type)
        {
            return new TsParenthesizedType(type);
        }

        /// <summary>
        /// Converts a unary operator to its code representation.
        /// </summary>
        public static string ToCodeDisplay(this TsUnaryOperator unaryOperator)
        {
            switch (unaryOperator)
            {
                case TsUnaryOperator.Delete: return "delete";
                case TsUnaryOperator.Void: return "void";
                case TsUnaryOperator.Typeof: return "typeof";

                case TsUnaryOperator.PrefixIncrement:
                case TsUnaryOperator.PostfixIncrement:
                    return "++";

                case TsUnaryOperator.PrefixDecrement:
                case TsUnaryOperator.PostfixDecrement:
                    return "--";

                case TsUnaryOperator.Plus: return "+";
                case TsUnaryOperator.Minus: return "-";
                case TsUnaryOperator.BitwiseNot: return "~";
                case TsUnaryOperator.LogicalNot: return "!";

                default:
                    throw new ArgumentOutOfRangeException(nameof(unaryOperator), unaryOperator, message: null);
            }
        }

        /// <summary>
        /// Converts a binary operator to its code representation.
        /// </summary>
        public static string ToCodeDisplay(this TsBinaryOperator binaryOperator)
        {
            switch (binaryOperator)
            {
                case TsBinaryOperator.Multiply: return "*";
                case TsBinaryOperator.Divide: return "/";
                case TsBinaryOperator.Modulo: return "%";
                case TsBinaryOperator.Add: return "+";
                case TsBinaryOperator.Subtract: return "-";
                case TsBinaryOperator.LeftShift: return "<<";
                case TsBinaryOperator.SignedRightShift: return ">>";
                case TsBinaryOperator.UnsignedRightShift: return ">>>";
                case TsBinaryOperator.LessThan: return "<";
                case TsBinaryOperator.GreaterThan: return ">";
                case TsBinaryOperator.LessThanEqual: return "<=";
                case TsBinaryOperator.GreaterThanEqual: return ">=";
                case TsBinaryOperator.InstanceOf: return "instanceof";
                case TsBinaryOperator.In: return "in";
                case TsBinaryOperator.Equals: return "==";
                case TsBinaryOperator.NotEquals: return "!=";
                case TsBinaryOperator.StrictEquals: return "===";
                case TsBinaryOperator.StrictNotEquals: return "!==";
                case TsBinaryOperator.BitwiseAnd: return "&";
                case TsBinaryOperator.BitwiseXor: return "^";
                case TsBinaryOperator.BitwiseOr: return "|";
                case TsBinaryOperator.LogicalAnd: return "&&";
                case TsBinaryOperator.LogicalOr: return "||";
                default:
                    throw new ArgumentOutOfRangeException(nameof(binaryOperator), binaryOperator, null);
            }
        }

        /// <summary>
        /// Converts an assignment operator to its code representation.
        /// </summary>
        public static string ToCodeDisplay(this TsAssignmentOperator assignmentOperator)
        {
            switch (assignmentOperator)
            {
                case TsAssignmentOperator.SimpleAssign: return "=";
                case TsAssignmentOperator.MultiplyAssign: return "*=";
                case TsAssignmentOperator.DivideAssign: return "/=";
                case TsAssignmentOperator.ModuloAssign: return "%=";
                case TsAssignmentOperator.AddAssign: return "+=";
                case TsAssignmentOperator.SubtractAssign: return "-=";
                case TsAssignmentOperator.LeftShiftAssign: return "<<=";
                case TsAssignmentOperator.SignedRightShiftAssign: return ">>=";
                case TsAssignmentOperator.UnsignedRightShiftAssign: return ">>>=";
                case TsAssignmentOperator.BitwiseAndAssign: return "&=";
                case TsAssignmentOperator.BitwiseXorAssign: return "^=";
                case TsAssignmentOperator.BitwiseOrAssign: return "|=";
                default:
                    throw new ArgumentOutOfRangeException(nameof(assignmentOperator), assignmentOperator, message: null);
            }
        }

        /// <summary>
        /// Returns a ": type" type annotation if the type is not null.
        /// </summary>
        /// <param name="type">The type annotation to write.</param>
        public static string ToTypeAnnotationCodeDisplay(this ITsType type)
        {
            return type != null ? $": {type.CodeDisplay}" : string.Empty;
        }

        /// <summary>
        /// Writes out a ": type" type annotation if the type is not null.
        /// </summary>
        /// <param name="type">The type annotation to write.</param>
        /// <param name="emitter">The emitter to write to.</param>
        public static void EmitTypeAnnotation(this ITsType type, Emitter emitter)
        {
            if (type != null)
            {
                emitter.Write(": ");
                type.Emit(emitter);
            }
        }

        /// <summary>
        /// Returns a " = expression" assignment if the expression is not null.
        /// </summary>
        /// <param name="expression">The expression to assign.</param>
        public static string ToAssignmentCodeDisplay(this ITsExpression expression)
        {
            return expression != null ? $" = {expression.CodeDisplay}" : string.Empty;
        }

        /// <summary>
        /// Writes out a " = expression" assignment if the expression is not null.
        /// </summary>
        /// <param name="expression">The expression to assign.</param>
        /// <param name="emitter">The emitter to write to.</param>
        public static void EmitAssignment(this ITsExpression expression, Emitter emitter)
        {
            if (expression != null)
            {
                emitter.Write(" = ");
                expression.Emit(emitter);
            }
        }

        /// <summary>
        /// Creates a new statement from the expression.
        /// </summary>
        public static ITsExpressionStatement ToStatement(this ITsExpression expression) =>
            new TsExpressionStatement(expression);
    }
}
