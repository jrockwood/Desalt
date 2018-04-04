﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsTokenCode.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.TypeScript.Ast.Parsing
{
    /// <summary>
    /// Enumerates the different types of TypeScript tokens.
    /// </summary>
    internal enum TsTokenCode
    {
        Unknown = 0,
        Identifier,

        /* The following keywords are reserved and cannot be used as an Identifier: */
        Break,
        Case,
        Catch,
        Class,
        Const,
        Continue,
        Debugger,
        Default,
        Delete,
        Do,
        Else,
        Enum,
        Export,
        Extends,
        False,
        Finally,
        For,
        Function,
        If,
        Import,
        In,
        Instanceof,
        New,
        Null,
        Return,
        Super,
        Switch,
        This,
        Throw,
        True,
        Try,
        Typeof,
        Var,
        Void,
        While,
        With,

        /* The following keywords cannot be used as identifiers in strict mode code, but are otherwise not restricted:*/
        Implements,
        Interface,
        Let,
        Package,
        Private,
        Protected,
        Public,
        Static,
        Yield,

        /* The following keywords cannot be used as user defined type names, but are otherwise not restricted: */
        Any,
        Boolean,
        Number,
        String,
        Symbol,

        /* The following keywords have special meaning in certain contexts, but are valid identifiers: */
        Abstract,
        As,
        Async,
        Await,
        Constructor,
        Declare,
        From,
        Get,
        Is,
        Module,
        Namespace,
        Of,
        Require,
        Set,
        Type,
    }
}
