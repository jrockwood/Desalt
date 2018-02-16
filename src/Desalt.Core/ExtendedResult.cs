﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedResult.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Represents the results from executing a process that can produce messages.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class ExtendedResult<T> : IExtendedResult<T>
    {
        //// ===========================================================================================================
        //// Constructors
        //// ===========================================================================================================

        public ExtendedResult(T result, IEnumerable<Diagnostic> messages = null)
        {
            Result = result;
            Messages = messages?.ToImmutableArray() ?? ImmutableArray<Diagnostic>.Empty;
        }

        //// ===========================================================================================================
        //// Properties
        //// ===========================================================================================================

        /// <summary>
        /// Gets the result of the operation.
        /// </summary>
        object IExtendedResult.Result => Result;

        /// <summary>
        /// Gets the result of the operation.
        /// </summary>
        public T Result { get; }

        /// <summary>
        /// Gets all of the messages in the order in which they were generated.
        /// </summary>
        public ImmutableArray<Diagnostic> Messages { get; }

        /// <summary>
        /// Gets the count of errors.
        /// </summary>
        public int ErrorCount => Messages.Count(m => m.Severity == DiagnosticSeverity.Error);

        /// <summary>
        /// Gets a value indicating if there are any errors.
        /// </summary>
        public bool HasErrors => Messages.Any(m => m.Severity == DiagnosticSeverity.Error);

        /// <summary>
        /// Gets a value indicating if there are any warnings.
        /// </summary>
        public bool HasWarnings => Messages.Any(m => m.Severity == DiagnosticSeverity.Warning);

        /// <summary>
        /// Gets a value indicating if the overall result is a success, meaning that there are no
        /// errors. Warnings are allowed.
        /// </summary>
        public bool Success => !HasErrors;

        private string DebuggerDisplay => Success ? "Success" : $"Error Count = {ErrorCount}";
    }
}
