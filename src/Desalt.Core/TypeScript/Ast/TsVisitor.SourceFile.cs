// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsVisitor.SourceFile.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.TypeScript.Ast
{
    public abstract partial class TsVisitor
    {
        /// <summary>
        /// Visits a TypeScript implementation source file (extension '.ts'), containing statements
        /// and declarations.
        /// </summary>
        public virtual void VisitImplementationScript(ITsImplementationScript node) => Visit(node);

        /// <summary>
        /// Visits a TypeScript implementation source file (extension '.ts'), containing exported
        /// statements and declarations.
        /// </summary>
        public virtual void VisitImplementationModule(ITsImplementationModule node) => Visit(node);
    }
}
