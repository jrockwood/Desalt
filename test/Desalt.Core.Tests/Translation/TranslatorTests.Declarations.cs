// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TranslatorTests.Declarations.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.Core.Tests.Translation
{
    using System.Linq;
    using System.Threading.Tasks;
    using Desalt.Core.Diagnostics;
    using Desalt.Core.Options;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;

    public partial class TranslatorTests
    {
        //// ===========================================================================================================
        //// Interface Declaration Tests
        //// ===========================================================================================================

        [Test]
        public async Task Bare_interface_declaration_without_accessibility_should_not_be_exported()
        {
            await AssertTranslation("interface ITest {}", "interface ITest {\n}\n");
        }

        [Test]
        public async Task Public_interface_declaration_should_be_exported()
        {
            await AssertTranslation("public interface ITest {}", "export interface ITest {\n}\n");
        }

        [Test]
        public async Task A_method_declaration_with_no_parameters_and_a_void_return_type_should_be_translated()
        {
            await AssertTranslation("interface ITest { void Do(); }", "interface ITest {\n  do(): void;\n}\n");
        }

        [Test]
        public async Task A_method_declaration_with_simple_parameters_and_a_void_return_type_should_be_translated()
        {
            await AssertTranslation(
                "interface ITest { void Do(string x, string y); }",
                "interface ITest {\n  do(x: string, y: string): void;\n}\n");
        }

        //// ===========================================================================================================
        //// Enum Declaration Tests
        //// ===========================================================================================================

        [Test]
        public async Task Bare_enum_declaration_without_accessibility_should_not_be_exported()
        {
            await AssertTranslation("enum LoggerLevel { All }", "enum LoggerLevel {\n  All,\n}\n");
        }

        [Test]
        public async Task Public_enum_declaration_should_be_exported()
        {
            await AssertTranslation("public enum LoggerLevel { All }", "export enum LoggerLevel {\n  All,\n}\n");
        }

        [Test]
        public async Task Enum_declarations_with_literal_values_should_be_translated()
        {
            await AssertTranslation(
                "enum LoggerLevel { All = 123 }",
                "enum LoggerLevel {\n  All = 123,\n}\n");
        }

        [Test]
        public async Task Enum_declarations_with_hex_values_should_be_translated_as_hex()
        {
            await AssertTranslation(
                "enum LoggerLevel { Hex = 0x100 }",
                "enum LoggerLevel {\n  Hex = 0x100,\n}\n");
        }

        [Test]
        public async Task Enum_declarations_should_respect_EnumRenameRule_LowerCaseFirstChar()
        {
            await AssertTranslation(
                "enum E { [ScriptName(\"uno\")] One, Two }",
                "enum E {\n  uno,\n  two,\n}\n",
                populateOptionsFunc: options =>
                    options.WithRenameRules(options.RenameRules.WithEnumRule(EnumRenameRule.LowerCaseFirstChar)));
        }

        [Test]
        public async Task Enum_declarations_with_NamedValues_should_be_a_const_enum()
        {
            await AssertTranslation(
                "[NamedValues] enum E { One, Two }",
                "const enum E {\n  One = 'one',\n  Two = 'two',\n}\n");
        }

        [Test]
        public async Task Enum_declarations_with_NamedValues_should_be_a_const_enum_even_when_a_value_has_been_defined()
        {
            await AssertTranslation(
                "[NamedValues] enum E { One = 1, Two = 2 }",
                "const enum E {\n  One = 'one',\n  Two = 'two',\n}\n");
        }

        [Test]
        public async Task
            Enum_declarations_with_NamedValues_should_use_script_name_for_the_value_and_not_the_declaration()
        {
            await AssertTranslation(
                "[NamedValues] enum E { [ScriptName(\"uno\")] One, Two }",
                "const enum E {\n  One = 'uno',\n  Two = 'two',\n}\n");
        }

        [Test]
        public async Task
            Enum_declarations_with_NamedValues_should_use_script_name_and_respect_EnumRenameRule_LowerCaseFirstChar()
        {
            await AssertTranslation(
                "[NamedValues] enum E { [ScriptName(\"uno\")] One, Two }",
                "const enum E {\n  one = 'uno',\n  two = 'two',\n}\n",
                populateOptionsFunc: options =>
                    options.WithRenameRules(options.RenameRules.WithEnumRule(EnumRenameRule.LowerCaseFirstChar)));
        }

        //// ===========================================================================================================
        //// Class Declaration Tests
        //// ===========================================================================================================

        [Test]
        public async Task Bare_class_declaration_without_accessibility_should_not_be_exported()
        {
            await AssertTranslation("class C { }", "class C {\n}\n");
        }

        [Test]
        public async Task Public_class_declaration_should_be_exported()
        {
            await AssertTranslation("public class C { }", "export class C {\n}\n");
        }

        [Test]
        public async Task Translate_class_declarations_with_class_heritage()
        {
            await AssertTranslation(
                "interface IA{} interface IC{} class A : IA{} class B : A{} class C : B, IA, IC{}",
                @"
interface IA {
}

interface IC {
}

class A implements IA {
}

class B extends A {
}

class C extends B implements IA, IC {
}
");
        }

        //// ===========================================================================================================
        //// Delegate Declaration Tests
        //// ===========================================================================================================

        [Test]
        public async Task Delegate_declarations_should_be_translated_as_a_type_alias()
        {
            await AssertTranslation(
                @"
delegate string MyFunc(int x, string y);
",
                @"
type MyFunc = (x: number, y: string) => string;
");
        }

        [Test]
        public async Task Public_delegate_declarations_should_be_translated_as_an_exported_type_alias()
        {
            await AssertTranslation(
                @"
public delegate string MyFunc(int x, string y);
",
                @"
export type MyFunc = (x: number, y: string) => string;
");
        }

        [Test]
        public async Task Using_a_delegate_as_a_type_should_use_the_delegate_name()
        {
            await AssertTranslation(
                @"
public delegate string MyFunc(int x, string y);
class C
{
    public void Method(MyFunc func) { }
}
",
                @"
export type MyFunc = (x: number, y: string) => string;

class C {
  public method(func: MyFunc): void { }
}
");
        }

        //// ===========================================================================================================
        //// Method Declaration Tests
        //// ===========================================================================================================

        [Test]
        public async Task Translate_should_accept_arrow_expression_clauses_for_the_method_body()
        {
            await AssertTranslation(
                @"
class A
{
    private int i;

    public int ReturnNum() => 100;
    public void NoReturn() => i++;
}
",
                @"
class A {
  private i: number;

  public returnNum(): number {
    return 100;
  }

  public noReturn(): void {
    this.i++;
  }
}
");
        }

        [Test]
        public async Task Translate_should_accept_arrow_expression_clauses_for_the_ctor_body()
        {
            await AssertTranslation(
                @"
class A
{
    private readonly int _x;
    public A(int x) => _x = x;
}
",
                @"
class A {
  private readonly _x: number;

  public constructor(x: number) {
    this._x = x;
  }
}
");
        }

        [Test]
        public async Task Translate_should_rename_overloaded_method_declarations()
        {
            await AssertTranslation(
                @"
class A
{
    public void Method()
    {
    }

    public void Method(int x)
    {
    }
}",
                @"
class A {
  public method(): void { }

  public method$1(x: number): void { }
}
");
        }

        [Test]
        public async Task Translate_should_skip_methods_and_ctors_marked_with_InlineCode_in_declarations()
        {
            await AssertTranslation(
                @"
class A
{
    [InlineCode(""new object()"")]
    public A() { }

    [InlineCode(""ss.contains()"")]
    public static bool Contains()
    {
        return true;
    }
}
",
                @"
class A {
}
");
        }

        [Test]
        public async Task Translate_should_skip_methods_but_not_ctors_marked_with_ScriptSkip_in_declarations()
        {
            await AssertTranslation(
                @"
class A
{
    [ScriptSkip]
    public A() { }

    [ScriptSkip]
    public void Method() { }
}",
                @"
class A {
  public constructor() { }
}
");
        }

        [Test]
        [Ignore("Not yet implemented")]
        public async Task Translate_should_declare_global_functions_for_GlobalMethods_classes_in_declarations()
        {
            await AssertTranslation(
                @"
namespace N {

    [GlobalMethods]
    static class A
    {
        public static void Method1() { }

        [PreserveName]
        static void Method2() {}

        [PreserveCase]
        static void Method3() {}

        [ScriptName(""Renamed"")]
        static void Method4() { }

        [InlineCode(""FooModule.foo({a})"")]
        static void Method5(int a) { }

        [ScriptSkip]
        static void Method6(int a) { }
    }
}",
// TODO: add cases with `export`
@"
function method1(): void { };

function method2(): void { };

function Method3(): void { };

function Renamed(): void { };
");
        }

        //// ===========================================================================================================
        //// Method Declarations with params and [ExpandParams]
        //// ===========================================================================================================

        [Test]
        public async Task Translate_should_convert_params_arguments_to_an_array()
        {
            await AssertTranslation(
                @"
interface I
{
    void NormalParams(params int[] args);
}

class A
{
    public void NormalParams(params int[] args) { }
}
",
                @"
interface I {
  normalParams(args: number[]): void;
}

class A {
  public normalParams(args: number[]): void { }
}
");
        }

        [Test]
        public async Task Delegate_declarations_with_ExpandParams_should_use_a_rest_parameter()
        {
            await AssertTranslation(
                @"
[ExpandParams]
delegate void D(params int[] args);
",
                @"
type D = (...args: number[]) => void;
");
        }

        [Test]
        public async Task Interface_declarations_with_ExpandParams_should_use_a_rest_parameter()
        {
            await AssertTranslation(
                @"
interface I
{
    [ExpandParams]
    void WithExpandParams(params int[] args);
}
",
                @"
interface I {
  withExpandParams(...args: number[]): void;
}
");
        }

        [Test]
        public async Task Class_declarations_with_ExpandParams_should_use_a_rest_parameter()
        {
            await AssertTranslation(
                @"
class A
{
    [ExpandParams]
    public A(params int[] args) { }

    [ExpandParams]
    public void WithExpandParams(params int[] args) { }
}
",
                @"
class A {
  public constructor(...args: number[]) { }

  public withExpandParams(...args: number[]): void { }
}
");
        }

        [Test]
        public async Task Interface_declarations_with_ExpandParams_should_also_override_a_class_method()
        {
            await AssertTranslation(
                @"
interface I
{
    [ExpandParams]
    void IFaceExpandParams(params int[] args);
}

class C : I
{
    public void IFaceExpandParams(params int[] args) { }
}
",
                @"
interface I {
  iFaceExpandParams(...args: number[]): void;
}

class C implements I {
  public iFaceExpandParams(...args: number[]): void { }
}
");
        }

        //// ===========================================================================================================
        //// Property Declaration Tests
        //// ===========================================================================================================

        [Test]
        public async Task Translate_should_accept_property_accessors_with_an_explicit_body()
        {
            await AssertTranslation(
                @"
class A
{
    private int _x;

    public int GetOnly
    {
        get { return _x; }
    }

    public int GetAndSet
    {
        get { return _x; }
        set { _x = value; }
    }
}",
                @"
class A {
  private _x: number;

  public get getOnly(): number {
    return this._x;
  }

  public get getAndSet(): number {
    return this._x;
  }

  public set getAndSet(value: number) {
    this._x = value;
  }
}
");
        }

        [Test]
        public async Task Translate_should_accept_arrow_expression_clauses_for_the_property_bodies()
        {
            await AssertTranslation(
                @"
class A
{
    public int PropGet => 100;
    public int PropGetAndSet
    {
        get => 200;
        set => value = value;
    }
}
",
                @"
class A {
  public get propGet(): number {
    return 100;
  }

  public get propGetAndSet(): number {
    return 200;
  }

  public set propGetAndSet(value: number) {
    value = value;
  }
}
");
        }

        [Test]
        public async Task Translate_should_accept_auto_generated_property_accessor_declarations()
        {
            await AssertTranslation(
                @"
class A
{
    public static int StaticProp { get; set; }
    public string GetAndSet { get; set; }
}",
                @"
class A {
  private static _$staticPropField: number;

  private _$getAndSetField: string;

  public static get staticProp(): number {
    return A._$staticPropField;
  }

  public static set staticProp(value: number) {
    A._$staticPropField = value;
  }

  public get getAndSet(): string {
    return this._$getAndSetField;
  }

  public set getAndSet(value: string) {
    this._$getAndSetField = value;
  }
}
");
        }

        [Test]
        public async Task Translate_should_accept_property_declarations_in_interfaces()
        {
            await AssertTranslation(
                @"
interface I
{
    int GetOnly { get; }
    int SetOnly { set; }
    int GetAndSet { get; set; }
}
",
                @"
interface I {
  readonly getOnly: number;
  setOnly: number;
  getAndSet: number;
}
");
        }

        [Test]
        public async Task
            Translate_should_choose_the_most_visible_accessor_when_differing_visibility_for_property_declarations()
        {
            await AssertTranslation(
                @"
class A
{
    public int PublicProtected { get; protected set; }
    public int PublicPrivate { get; private set; }
    protected int ProtectedPrivate { get; private set; }
    public int PrivatePublic { private get; set; }
    protected int PrivateProtected { private get; set; }
}
",
                @"
class A {
  private _$publicProtectedField: number;

  private _$publicPrivateField: number;

  private _$protectedPrivateField: number;

  private _$privatePublicField: number;

  private _$privateProtectedField: number;

  public get publicProtected(): number {
    return this._$publicProtectedField;
  }

  public set publicProtected(value: number) {
    this._$publicProtectedField = value;
  }

  public get publicPrivate(): number {
    return this._$publicPrivateField;
  }

  public set publicPrivate(value: number) {
    this._$publicPrivateField = value;
  }

  protected get protectedPrivate(): number {
    return this._$protectedPrivateField;
  }

  protected set protectedPrivate(value: number) {
    this._$protectedPrivateField = value;
  }

  public get privatePublic(): number {
    return this._$privatePublicField;
  }

  public set privatePublic(value: number) {
    this._$privatePublicField = value;
  }

  protected get privateProtected(): number {
    return this._$privateProtectedField;
  }

  protected set privateProtected(value: number) {
    this._$privateProtectedField = value;
  }
}
",
                diagnosticsAssertionAction: diagnostics =>
                {
                    diagnostics.Select(x => x.Id)
                        .Should()
                        .OnlyContain(
                            id => id ==
                                DiagnosticFactory
                                    .GetterAndSetterAccessorsDoNotAgreeInVisibility("nothing", Location.None)
                                    .Id);

                    var propertyNamesFromErrors = from d in diagnostics
                                                  let message = d.ToString()
                                                  let tickIndex = message.IndexOf('\'')
                                                  let propertyName = message.Substring(
                                                      tickIndex + 1,
                                                      message.LastIndexOf('\'') - tickIndex - 1)
                                                  select propertyName;

                    propertyNamesFromErrors.Should()
                        .HaveCount(5)
                        .And.ContainInOrder(
                            "PublicProtected",
                            "PublicPrivate",
                            "ProtectedPrivate",
                            "PrivatePublic",
                            "PrivateProtected");
                });
        }

        [Test]
        public async Task Translate_should_not_emit_declarations_for_IntrinsicProperty_properties()
        {
            await AssertTranslation(
                @"
interface I
{
    [IntrinsicProperty]
    int Prop { get; set; }
}

class A
{
    [IntrinsicProperty]
    private int Prop { get; set; }
}
",
                @"
interface I {
}

class A {
}
");
        }

        //// ===========================================================================================================
        //// Overloaded Operator Tests
        //// ===========================================================================================================

        [Test]
        public async Task Translate_unary_operator_declarations()
        {
            await AssertTranslation(
                @"
sealed class JsNumber
{
    // Unary operators
    public static JsNumber operator +(JsNumber x) => new JsNumber();
    public static JsNumber operator -(JsNumber x) => new JsNumber();
    public static JsNumber operator !(JsNumber x) => new JsNumber();
    public static JsNumber operator ~(JsNumber x) => new JsNumber();
    public static JsNumber operator ++(JsNumber x) => new JsNumber();
    public static JsNumber operator --(JsNumber x) => new JsNumber();
    public static bool operator true(JsNumber x) => true;
    public static bool operator false(JsNumber x) => false;
}",
                @"
class JsNumber {
  public static op_UnaryPlus(x: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_UnaryNegation(x: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_LogicalNot(x: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_OnesComplement(x: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_Increment(x: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_Decrement(x: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_True(x: JsNumber): boolean {
    return true;
  }

  public static op_False(x: JsNumber): boolean {
    return false;
  }
}
");
        }

        [Test]
        public async Task Translate_binary_operator_declarations()
        {
            await AssertTranslation(
                @"
sealed class JsNumber
{
    // Binary operators
    public static JsNumber operator +(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator -(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator *(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator /(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator %(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator &(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator |(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator ^(JsNumber x, JsNumber y) => new JsNumber();
    public static JsNumber operator <<(JsNumber x, int y) => new JsNumber();
    public static JsNumber operator >>(JsNumber x, int y) => new JsNumber();
}",
                @"
class JsNumber {
  public static op_Addition(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_Subtraction(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_Multiply(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_Division(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_Modulus(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_BitwiseAnd(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_BitwiseOr(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_ExclusiveOr(x: JsNumber, y: JsNumber): JsNumber {
    return new JsNumber();
  }

  public static op_LeftShift(x: JsNumber, y: number): JsNumber {
    return new JsNumber();
  }

  public static op_RightShift(x: JsNumber, y: number): JsNumber {
    return new JsNumber();
  }
}
");
        }

        [Test]
        public async Task Translate_comparison_operator_declarations()
        {
            await AssertTranslation(
                @"
sealed class JsNumber
{
    // Comparison operators
    public static bool operator ==(JsNumber x, JsNumber y) => false;
    public static bool operator !=(JsNumber x, JsNumber y) => false;
    public static bool operator <(JsNumber x, JsNumber y) => false;
    public static bool operator <=(JsNumber x, JsNumber y) => false;
    public static bool operator >(JsNumber x, JsNumber y) => false;
    public static bool operator >=(JsNumber x, JsNumber y) => false;
}",
                @"
class JsNumber {
  public static op_Equality(x: JsNumber, y: JsNumber): boolean {
    return false;
  }

  public static op_Inequality(x: JsNumber, y: JsNumber): boolean {
    return false;
  }

  public static op_LessThan(x: JsNumber, y: JsNumber): boolean {
    return false;
  }

  public static op_LessThanOrEqual(x: JsNumber, y: JsNumber): boolean {
    return false;
  }

  public static op_GreaterThan(x: JsNumber, y: JsNumber): boolean {
    return false;
  }

  public static op_GreaterThanOrEqual(x: JsNumber, y: JsNumber): boolean {
    return false;
  }
}
");
        }

        [Test]
        public async Task Translate_conversion_operator_declarations()
        {
            await AssertTranslation(
                @"
sealed class JsNumber
{
    // Explicit and Implicit conversion operators
    public static explicit operator int(JsNumber x) => 0;
    public static implicit operator double(JsNumber x) => 0.0;
}",
                @"
class JsNumber {
  public static op_Explicit(x: JsNumber): number {
    return 0;
  }

  public static op_Implicit(x: JsNumber): number {
    return 0;
  }
}
");
        }

        [Test]
        public async Task Translate_should_skip_operator_declarations_marked_with_InlineCode()
        {
            await AssertTranslation(
                @"
sealed class JsNumber
{
    [InlineCode(""{x} + {y}"")]
    public static JsNumber operator +(JsNumber x, JsNumber y) { return new JsNumber(); }

    [InlineCode(""{x}"")]
    public static implicit operator double(JsNumber x) { return 0; }
}",
                @"
class JsNumber {
}
");
        }
    }
}
