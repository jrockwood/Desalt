﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsEmitTests.Types.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScriptAst.Tests.Ast
{
    using System;
    using Desalt.CompilerUtilities.Extensions;
    using Desalt.TypeScriptAst.Ast;
    using NUnit.Framework;
    using Factory = TypeScriptAst.Ast.TsAstFactory;

    public partial class TsEmitTests
    {
        [Test]
        public void Emit_type_parameter_with_no_constraint()
        {
            VerifyOutput(Factory.TypeParameter(s_T), "T");
        }

        [Test]
        public void Emit_type_parameter_with_constraint()
        {
            VerifyOutput(Factory.TypeParameter(s_T, s_MyTypeRef), "T extends MyType");
        }

        [Test]
        public void Emit_parenthesized_type()
        {
            VerifyOutput(Factory.SymbolType.WithParentheses(), "(symbol)");
        }

        [Test]
        public void Emit_simple_type_reference_with_no_type_arguments()
        {
            VerifyOutput(s_TRef, "T");
        }

        [Test]
        public void Emit_qualified_name_type_reference_with_no_type_arguments()
        {
            VerifyOutput(Factory.TypeReference(Factory.QualifiedName("Ns.Class")), "Ns.Class");
        }

        [Test]
        public void Emit_type_reference_with_type_arguments()
        {
            VerifyOutput(
                Factory.TypeReference(
                    Factory.Identifier("Sub"),
                    Factory.TypeReference(
                        Factory.Identifier("T1"), Factory.TypeReference(Factory.Identifier("T2")))),
                "Sub<T1<T2>>");
        }

        [Test]
        public void Emit_object_property_signatures()
        {
            VerifyOutput(Factory.PropertySignature(s_x), "x");
            VerifyOutput(Factory.PropertySignature(s_x, isReadOnly: true), "readonly x");
            VerifyOutput(Factory.PropertySignature(s_x, isOptional: true), "x?");
            VerifyOutput(Factory.PropertySignature(s_x, isReadOnly: true, isOptional: true), "readonly x?");
            VerifyOutput(Factory.PropertySignature(s_x, isOptional: true, propertyType: Factory.StringType), "x?: string");

            VerifyOutput(
                Factory.PropertySignature(s_x, isReadOnly: true, isOptional: false, propertyType: Factory.StringType),
                "readonly x: string");

            VerifyOutput(Factory.PropertySignature(s_x, isOptional: true, propertyType: Factory.StringType), "x?: string");
            VerifyOutput(
                Factory.PropertySignature(s_x, isReadOnly: true, isOptional: false, propertyType: Factory.StringType),
                "readonly x: string");
        }

        [Test]
        public void Emit_full_call_signature()
        {
            VerifyOutput(
                Factory.CallSignature(
                    Factory.TypeParameters(Factory.TypeParameter(s_T, s_MyTypeRef)),
                    Factory.ParameterList(
                        Factory.BoundRequiredParameter(s_x, s_TRef, TsAccessibilityModifier.Private)),
                    Factory.AnyType),
                "<T extends MyType>(private x: T): any");
        }

        [Test]
        public void Emit_call_signature_with_no_type_parameters()
        {
            VerifyOutput(
                Factory.CallSignature(
                    Factory.ParameterList(
                        Factory.BoundRequiredParameter(s_x, s_TRef, TsAccessibilityModifier.Protected)),
                    Factory.AnyType),
                "(protected x: T): any");
        }

        [Test]
        public void Emit_call_signature_with_no_type_parameters_or_return_type()
        {
            VerifyOutput(
                Factory.CallSignature(
                    Factory.ParameterList(
                        Factory.BoundRequiredParameter(s_x, s_TRef))),
                "(x: T)");
        }

        [Test]
        public void Emit_full_ctor_signature()
        {
            VerifyOutput(
                Factory.ConstructSignature(
                    Factory.TypeParameters(Factory.TypeParameter(s_T, s_MyTypeRef)),
                    Factory.ParameterList(
                        Factory.BoundRequiredParameter(s_x, s_TRef, TsAccessibilityModifier.Private)),
                    Factory.AnyType),
                "new <T extends MyType>(private x: T): any");
        }

        [Test]
        public void Emit_ctor_signature_with_no_type_parameters()
        {
            VerifyOutput(
                Factory.ConstructSignature(
                    Factory.ParameterList(
                        Factory.BoundRequiredParameter(s_x, s_TRef, TsAccessibilityModifier.Public)),
                    Factory.AnyType),
                "new (public x: T): any");
        }

        [Test]
        public void Emit_ctor_signature_with_no_type_parameters_or_return_type()
        {
            VerifyOutput(
                Factory.ConstructSignature(
                    Factory.ParameterList(
                        Factory.BoundRequiredParameter(s_x, s_TRef))),
                "new (x: T)");
        }

        [Test]
        public void Emit_index_signatures()
        {
            VerifyOutput(
                Factory.IndexSignature(Factory.Identifier("key"), isParameterNumberType: false, returnType: s_MyTypeRef),
                "[key: string]: MyType");

            VerifyOutput(
                Factory.IndexSignature(Factory.Identifier("key"), isParameterNumberType: true, returnType: s_MyTypeRef),
                "[key: number]: MyType");
        }

        [Test]
        public void Emit_method_signature()
        {
            VerifyOutput(
                Factory.MethodSignature(s_x, isOptional: false, callSignature: Factory.CallSignature()),
                "x()");

            VerifyOutput(
                Factory.MethodSignature(
                    s_x,
                    isOptional: true,
                    callSignature: Factory.CallSignature(
                        Factory.ParameterList(Factory.BoundRequiredParameter(s_y)))),
                "x?(y)");
        }

        [Test]
        public void Emit_object_type()
        {
            VerifyOutput(Factory.ObjectType(), "{}");

            VerifyOutput(
                Factory.ObjectType(
                    Factory.PropertySignature(s_x, propertyType: Factory.StringType),
                    Factory.CallSignature(
                        Factory.ParameterList(Factory.BoundRequiredParameter(s_z), Factory.BoundRequiredParameter(s_p)),
                        Factory.BooleanType),
                    Factory.ConstructSignature(
                        Factory.TypeParameters(Factory.TypeParameter(s_T, s_MyTypeRef)),
                        Factory.ParameterList(Factory.BoundRequiredParameter(Factory.Identifier("arg"), s_TRef))),
                    Factory.IndexSignature(Factory.Identifier("k"), false, Factory.AnyType),
                    Factory.MethodSignature(s_z, true, Factory.CallSignature(Factory.ParameterList(), Factory.VoidType))),
                @"{
  x: string;
  (z, p): boolean;
  new <T extends MyType>(arg: T);
  [k: string]: any;
  z?(): void;
}".Replace("\r\n", "\n"));
        }

        [Test]
        public void Emit_array_type()
        {
            VerifyOutput(Factory.ArrayType(Factory.StringType), "string[]");
        }

        [Test]
        public void Emit_array_of_functions_type_using_Array_of_T_instead_of_brackets()
        {
            VerifyOutput(
                Factory.ArrayType(
                    Factory.FunctionType(
                        Factory.TypeParameters(Factory.TypeParameter(s_T, s_MyTypeRef)),
                        Factory.ParameterList(
                            Factory.BoundRequiredParameter(s_x, s_TRef),
                            Factory.BoundRequiredParameter(s_y, Factory.StringType)),
                        Factory.BooleanType)),
                "Array<<T extends MyType>(x: T, y: string) => boolean>");
        }

        [Test]
        public void Emit_tuple_type()
        {
            VerifyOutput(
                Factory.TupleType(Factory.BooleanType, Factory.StringType),
                "[boolean, string]");
        }

        [Test]
        public void Emit_union_types()
        {
            VerifyOutput(
                Factory.UnionType(s_TRef, Factory.StringType, Factory.ArrayType(Factory.NumberType)),
                "T | string | number[]");
        }

        [Test]
        public void Emit_intersection_types()
        {
            VerifyOutput(
                Factory.IntersectionType(s_TRef, Factory.StringType, Factory.ArrayType(Factory.NumberType)),
                "T & string & number[]");
        }

        [Test]
        public void Emit_function_type()
        {
            VerifyOutput(
                Factory.FunctionType(
                    Factory.TypeParameters(Factory.TypeParameter(s_T)),
                    Factory.ParameterList(Factory.BoundRequiredParameter(s_x), Factory.BoundRequiredParameter(s_y)),
                    Factory.StringType),
                "<T>(x, y) => string");

            VerifyOutput(
                Factory.FunctionType(
                    Factory.ParameterList(Factory.BoundRequiredParameter(s_x), Factory.BoundRequiredParameter(s_y)),
                    Factory.StringType),
                "(x, y) => string");
        }

        [Test]
        public void Emit_ctor_type()
        {
            VerifyOutput(
                Factory.ConstructorType(
                    Factory.TypeParameters(Factory.TypeParameter(s_T)),
                    Factory.ParameterList(Factory.BoundRequiredParameter(s_x), Factory.BoundRequiredParameter(s_y)),
                    Factory.StringType),
                "new <T>(x, y) => string");

            VerifyOutput(
                Factory.ConstructorType(
                    Factory.ParameterList(Factory.BoundRequiredParameter(s_x), Factory.BoundRequiredParameter(s_y)),
                    Factory.StringType),
                "new (x, y) => string");
        }

        [Test]
        public void Emit_type_query()
        {
            VerifyOutput(Factory.TypeQuery(Factory.QualifiedName("a.b.c")), "typeof a.b.c");
        }

        [Test]
        public void Emit_this_type()
        {
            VerifyOutput(Factory.ThisType, "this");
        }

        [Test]
        public void Emit_parameter_list_with_only_required_parameters()
        {
            VerifyOutput(
                Factory.ParameterList(
                    Factory.BoundRequiredParameter(s_x),
                    Factory.StringRequiredParameter(s_y, Factory.String("value")),
                    Factory.StringRequiredParameter(s_z, Factory.String("hello", StringLiteralQuoteKind.DoubleQuote))),
                "x, y: 'value', z: \"hello\"");
        }

        [Test]
        public void Emit_parameter_list_with_only_optional_parameters()
        {
            VerifyOutput(
                Factory.ParameterList(
                    requiredParameters: Array.Empty<ITsRequiredParameter>(),
                    optionalParameters: new ITsOptionalParameter[]
                    {
                        Factory.StringOptionalParameter(s_y, Factory.String("value")),
                        Factory.BoundOptionalParameter(s_z, Factory.NumberType, Factory.Zero),
                        Factory.BoundOptionalParameter(s_p, Factory.BooleanType),
                        Factory.BoundOptionalParameter(Factory.Identifier("noType")),
                    }),
                "y?: 'value', z: number = 0, p?: boolean, noType?");
        }

        [Test]
        public void Emit_parameter_list_with_only_rest_parameter()
        {
            VerifyOutput(
                Factory.ParameterList(
                    requiredParameters: Array.Empty<ITsRequiredParameter>(),
                    restParameter: Factory.RestParameter(s_z, s_MyTypeRef)),
                "...z: MyType");
        }

        [Test]
        public void Emit_parameter_list_with_required_and_optional_parameters()
        {
            VerifyOutput(
                Factory.ParameterList(
                    requiredParameters: Factory.BoundRequiredParameter(s_x).ToSafeArray(),
                    optionalParameters: new ITsOptionalParameter[]
                    {
                        Factory.StringOptionalParameter(s_y, Factory.String("value")),
                        Factory.BoundOptionalParameter(s_z, Factory.NumberType, Factory.Zero),
                        Factory.BoundOptionalParameter(s_p, Factory.BooleanType),
                        Factory.BoundOptionalParameter(Factory.Identifier("noType")),
                    }),
                "x, y?: 'value', z: number = 0, p?: boolean, noType?");
        }

        [Test]
        public void Emit_parameter_list_with_required_and_rest_parameters()
        {
            VerifyOutput(
                Factory.ParameterList(
                    requiredParameters: Factory.BoundRequiredParameter(s_x).ToSafeArray(),
                    restParameter: Factory.RestParameter(s_z, s_MyTypeRef)),
                "x, ...z: MyType");
        }

        [Test]
        public void Emit_parameter_list_with_optional_and_rest_parameters()
        {
            VerifyOutput(
                Factory.ParameterList(
                    requiredParameters: Array.Empty<ITsRequiredParameter>(),
                    optionalParameters: Factory.BoundOptionalParameter(s_y, Factory.BooleanType, Factory.False)
                        .ToSafeArray(),
                    restParameter: Factory.RestParameter(s_z, s_MyTypeRef)),
                "y: boolean = false, ...z: MyType");
        }

        [Test]
        public void Emit_parameter_list_with_required_optional_and_rest_parameters()
        {
            VerifyOutput(
                Factory.ParameterList(
                    requiredParameters: Factory.BoundRequiredParameter(s_x).ToSafeArray(),
                    optionalParameters: Factory.BoundOptionalParameter(s_y, Factory.BooleanType, Factory.False)
                        .ToSafeArray(),
                    restParameter: Factory.RestParameter(s_z, s_MyTypeRef)),
                "x, y: boolean = false, ...z: MyType");
        }

        [Test]
        public void Emit_type_alias_declaration()
        {
            VerifyOutput(
                Factory.TypeAliasDeclaration(
                    Factory.Identifier("AnotherT"),
                    Factory.TypeParameter(s_T),
                    Factory.TypeReference(Factory.Identifier("MrT"), Factory.TypeReference(Factory.Identifier("IceT")))),
                "type AnotherT<T> = MrT<IceT>;\n");
        }
    }
}
