// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsAstFactory.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScriptAst.Ast
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>
    /// Provides factory methods for creating TypeScript AST nodes.
    /// </summary>
    public static partial class TsAstFactory
    {
        //// ===========================================================================================================
        //// Singleton Properties
        //// ===========================================================================================================

        public static readonly ITsThisType ThisType = new TsThisType();

        public static readonly ITsType AnyType = new TsPredefinedType(TsPredefinedKind.Any);
        public static readonly ITsType BooleanType = new TsPredefinedType(TsPredefinedKind.Boolean);
        public static readonly ITsType NumberType = new TsPredefinedType(TsPredefinedKind.Number);
        public static readonly ITsType StringType = new TsPredefinedType(TsPredefinedKind.String);
        public static readonly ITsType SymbolType = new TsPredefinedType(TsPredefinedKind.Symbol);
        public static readonly ITsType VoidType = new TsPredefinedType(TsPredefinedKind.Void);
        public static readonly ITsType NullType = new TsPredefinedType(TsPredefinedKind.Null);
        public static readonly ITsType UndefinedType = new TsPredefinedType(TsPredefinedKind.Undefined);

        //// ===========================================================================================================
        //// Identifiers
        //// ===========================================================================================================

        public static ITsIdentifier Identifier(string name)
        {
            return new TsIdentifier(name);
        }

        /// <summary>
        /// Creates a qualified name, which has dots between identifiers. For example, 'ns.type.method'.
        /// </summary>
        /// <param name="dottedName">A full name with or without dots that will be parsed.</param>
        /// <returns>An <see cref="ITsQualifiedName"/>.</returns>
        public static ITsQualifiedName QualifiedName(string dottedName)
        {
            string[] parts = dottedName?.Split('.') ?? throw new ArgumentNullException(nameof(dottedName));
            if (parts.Length > 1)
            {
                return QualifiedName(parts.ToArray());
            }

            return new TsQualifiedName(ImmutableArray<ITsIdentifier>.Empty, new TsIdentifier(parts[0]));
        }

        /// <summary>
        /// Creates a qualified name, which has dots between identifiers. For example, 'ns.type.method'.
        /// </summary>
        /// <param name="identifiers">The parts of the full name.</param>
        /// <returns>An <see cref="ITsQualifiedName"/>.</returns>
        public static ITsQualifiedName QualifiedName(params ITsIdentifier[] identifiers)
        {
            return new TsQualifiedName(identifiers.Take(identifiers.Length - 1).ToImmutableArray(), identifiers.Last());
        }

        /// <summary>
        /// Creates a qualified name, which has dots between identifiers. For example, 'ns.type.method'.
        /// </summary>
        /// <param name="names">The parts of the full name.</param>
        /// <returns>An <see cref="ITsQualifiedName"/>.</returns>
        public static ITsQualifiedName QualifiedName(params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                throw new ArgumentException("Empty names array", nameof(names));
            }

            var right = new TsIdentifier(names.Last());
            var left = names.Take(names.Length - 1)
                .Select(name => new TsIdentifier(name))
                .Cast<ITsIdentifier>()
                .ToImmutableArray();

            return new TsQualifiedName(left, right);
        }

        /// <summary>
        /// Creates a qualified name with type arguments. For example, 'ns.type.method&lt;T1, T2&gt;'.
        /// </summary>
        /// <param name="dottedName">A full name with or without dots that will be parsed.</param>
        /// <param name="typeArguments">An optional list of type arguments.</param>
        /// <returns>An <see cref="ITsGenericTypeName"/>.</returns>
        public static ITsGenericTypeName GenericTypeName(string dottedName, params ITsType[] typeArguments)
        {
            ITsQualifiedName qualifiedName = QualifiedName(dottedName);
            return new TsGenericTypeName(qualifiedName.Left, qualifiedName.Right, typeArguments.ToImmutableArray());
        }

        public static ITsExpression MemberAccessOmittedIdentifier()
        {
            // what about trivia?
            return new MemberAccessOmittedIdentifier();
        }
    }
}
