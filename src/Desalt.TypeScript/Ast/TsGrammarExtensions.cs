﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsGrammarExtensions.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScript.Ast
{
    using System;
    using Desalt.Core.Utility;
    using Desalt.TypeScript.Ast.Expressions;
    using Desalt.TypeScript.Ast.Types;

    /// <summary>
    /// Contains static extension methods for <see cref="ITsAstNode"/> objects.
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
        /// Writes out a ": type" type annotation if the type is not null.
        /// </summary>
        /// <param name="type">The type annotation to write.</param>
        public static string ToTypeAnnotationCodeDisplay(this ITsType type)
        {
            return type != null ? $": {type.ToCodeDisplay()}" : string.Empty;
        }

        public static void WriteTypeAnnotation(this ITsType type, IndentedTextWriter writer)
        {
            if (type != null)
            {
                writer.Write(": ");
                type.WriteFullCodeDisplay(writer);
            }
        }
    }
}
