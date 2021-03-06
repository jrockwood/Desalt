// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="AlternateSignatureMethodGroup.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.SymbolTables
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Desalt.CompilerUtilities.Extensions;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.Pipeline;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Represents a group of methods that all share a set of signatures via an [AlternateSignature] attribute.
    /// </summary>
    internal sealed class AlternateSignatureMethodGroup
    {
        //// ===========================================================================================================
        //// Member Variables
        //// ===========================================================================================================

        private readonly Lazy<ImmutableArray<ImmutableArray<ITypeSymbol>>> _parameterTypeUnions;

        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        private AlternateSignatureMethodGroup(
            IMethodSymbol implementingMethod,
            ImmutableArray<IMethodSymbol> alternateSignatureMethods)
        {
            ImplementingMethod = implementingMethod ?? throw new ArgumentNullException(nameof(implementingMethod));
            AlternateSignatureMethods = alternateSignatureMethods;
            _parameterTypeUnions = new Lazy<ImmutableArray<ImmutableArray<ITypeSymbol>>>(
                GatherTypesForParameters,
                isThreadSafe: true);

            int implementingParamCount = implementingMethod.Parameters.Length;
            var paramCounts = alternateSignatureMethods.Select(methodSymbol => methodSymbol.Parameters.Length)
                .ToImmutableArray();

            MinParameterCount = Math.Min(implementingParamCount, paramCounts.Min());
            MaxParameterCount = Math.Max(implementingParamCount, paramCounts.Max());

            if (implementingParamCount == MaxParameterCount)
            {
                MethodWithMaxParams = implementingMethod;
            }
            else
            {
                MethodWithMaxParams = alternateSignatureMethods.First(
                    methodSymbol => methodSymbol.Parameters.Length == MaxParameterCount);
            }
        }

        /// <summary>
        /// Creates a new <see cref="AlternateSignatureMethodGroup"/> from the specified method symbols.
        /// </summary>
        /// <param name="methodSymbols">The methods that all share the same [AlternateSignature] group.</param>
        public static IExtendedResult<AlternateSignatureMethodGroup> Create(IEnumerable<IMethodSymbol> methodSymbols)
        {
            var diagnostics = new List<Diagnostic>();
            var methodSymbolsArr = methodSymbols.ToImmutableArray();

            var implementingMethods = methodSymbolsArr.Where(
                    methodSymbol => !methodSymbol.GetFlagAttribute(SaltarelleAttributeName.AlternateSignature))
                .ToImmutableArray();

            // we don't support multiple overloads mixed with [AlternateSignature], which would mean
            // that we'd have to implement a way to figure out which [AlternateSignatures] go with
            // which methods based on the parameters and return type - hard!
            if (implementingMethods.Length != 1)
            {
                // we should not hit this case because the Saltarelle compiler will generate an error
                // if there's not exactly one implementing method
                diagnostics.Add(
                    DiagnosticFactory.InternalError(
                        "The Saltarelle compiler should enforce that there is exactly one implementing methods of the " +
                        $"[AlternateSignature] method group for '{methodSymbolsArr[0].Name}'",
                        methodSymbolsArr[0].DeclaringSyntaxReferences[0].GetSyntax().GetLocation()));
            }

            IMethodSymbol implementingMethod = implementingMethods[0];
            var alternateSignatureMethods = methodSymbolsArr.Except(implementingMethods).ToImmutableArray();
            var group = new AlternateSignatureMethodGroup(implementingMethod, alternateSignatureMethods);

            return new ExtendedResult<AlternateSignatureMethodGroup>(group, diagnostics);
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        /// <summary>
        /// Gets the <see cref="IMethodSymbol"/> that implements the method group.
        /// </summary>
        public IMethodSymbol ImplementingMethod { get; }

        /// <summary>
        /// Gets an array of <see cref="IMethodSymbol"/> objects comprising the group of methods
        /// decorated with the [AlternateSignature] attribute.
        /// </summary>
        public ImmutableArray<IMethodSymbol> AlternateSignatureMethods { get; }

        /// <summary>
        /// Gets the <see cref="IMethodSymbol"/> that has the most parameters. If there is more than
        /// one that have the same number of parameters, an indeterminate one is returned.
        /// </summary>
        public IMethodSymbol MethodWithMaxParams { get; }

        /// <summary>
        /// Gets the minimum number of parameters across the methods in the group.
        /// </summary>
        public int MinParameterCount { get; }

        /// <summary>
        /// Gets the maximum number of parameters across the methods in the group.
        /// </summary>
        public int MaxParameterCount { get; }

        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        /// <summary>
        /// Gets all of the valid types for the specified parameter across all of the methods in the group.
        /// </summary>
        /// <param name="index">The index of the parameter to retrieve.</param>
        public ImmutableArray<ITypeSymbol> TypesForParameter(int index)
        {
            return _parameterTypeUnions.Value[index];
        }

        private ImmutableArray<ImmutableArray<ITypeSymbol>> GatherTypesForParameters()
        {
            var typesForParameters = ImmutableArray.Create(
                Enumerable.Range(1, MaxParameterCount).Select(_ => new List<ITypeSymbol>()).ToArray());

            var allMethods = ImplementingMethod.ToSingleEnumerable().Concat(AlternateSignatureMethods);

            foreach (IMethodSymbol methodSymbol in allMethods)
            {
                for (int i = 0; i < methodSymbol.Parameters.Length; i++)
                {
                    ITypeSymbol parameterType = methodSymbol.Parameters[i].Type;

                    List<ITypeSymbol> typesForParameter = typesForParameters[i];
                    if (!typesForParameter.Contains(parameterType))
                    {
                        typesForParameter.Add(parameterType);
                    }
                }
            }

            return typesForParameters.Select(set => set.ToImmutableArray()).ToImmutableArray();
        }
    }
}
