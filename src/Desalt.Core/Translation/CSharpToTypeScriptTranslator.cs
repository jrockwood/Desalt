﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="CSharpToTypeScriptTranslator.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Translation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Desalt.Core.Pipeline;
    using Desalt.Core.TypeScript.Ast;
    using Factory = Desalt.Core.TypeScript.Ast.TsAstFactory;

    /// <summary>
    /// Converts a CSharp syntax tree into a TypeScript syntax tree.
    /// </summary>
    internal class CSharpToTypeScriptTranslator
    {
        //// ===========================================================================================================
        //// Methods
        //// ===========================================================================================================

        public IExtendedResult<ITsImplementationModule> TranslateDocument(
            DocumentTranslationContextWithSymbolTables context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var visitor = new TranslationVisitor(context, cancellationToken);
            var implementationModule = (ITsImplementationModule)visitor.Visit(context.RootSyntax).Single();

            IExtendedResult<IEnumerable<ITsImportDeclaration>> addImportsResult = ImportsTranslator.TranslateImports(
                context,
                visitor.TypesToImport);
            IEnumerable<ITsImportDeclaration> importDeclarations = addImportsResult.Result;

            // insert the imports at the top of the translated file
            ITsImplementationModuleElement[] newElements =
                implementationModule.Elements.InsertRange(0, importDeclarations).ToArray();
            ITsImplementationModule moduleWithImports = Factory.ImplementationModule(newElements);

            return new ExtendedResult<ITsImplementationModule>(
                moduleWithImports,
                visitor.Diagnostics.Concat(addImportsResult.Diagnostics));
        }
    }
}
