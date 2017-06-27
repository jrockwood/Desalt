// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TsVisitor.Types.cs" company="Justin Rockwood">
//   Copyright (c) Justin Rockwood. All Rights Reserved. Licensed under the Apache License, Version 2.0. See
//   LICENSE.txt in the project root for license information.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Desalt.TypeScript.Ast
{
    public abstract partial class TsVisitor
    {
        /// <summary>
        /// Visits a type surrounded in parentheses.
        /// </summary>
        public virtual void VisitParenthesizedType(ITsParenthesizedType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a predefined type, which is one of any, number, boolean, string, symbol, or void.
        /// </summary>
        public virtual void VisitPredefinedType(ITsPredefinedType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript type reference.
        /// </summary>
        public virtual void VisitTypeReference(ITsTypeReference node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript object type.
        /// </summary>
        public virtual void VisitObjectType(ITsObjectType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript array type.
        /// </summary>
        public virtual void VisitArrayType(ITsArrayType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript tuple type.
        /// </summary>
        public virtual void VisitTupleType(ITsTupleType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript function type.
        /// </summary>
        public virtual void VisitFunctionType(ITsFunctionType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript constructor type.
        /// </summary>
        public virtual void VisitConstructorType(ITsFunctionType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a typeof query.
        /// </summary>
        public virtual void VisitTypeQuery(ITsTypeQuery node) => DefaultVisit(node);

        /// <summary>
        /// Visits a 'this' type.
        /// </summary>
        public virtual void VisitThisType(ITsThisType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a property signature.
        /// </summary>
        public virtual void VisitPropertySignature(ITsPropertySignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a a call signature of the form '&gt;T&lt;(parameter: type): type'..
        /// </summary>
        public virtual void VisitCallSignature(ITsCallSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a parameter list of the form '(parameter: type)'..
        /// </summary>
        public virtual void VisitParameterList(ITsParameterList node) => DefaultVisit(node);

        /// <summary>
        /// Visits a type parameter of the form &lt;MyType extends MyBase&gt;.
        /// </summary>
        public virtual void VisitTypeParameter(ITsTypeParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a bound required parameter in a parameter list for a function.
        /// </summary>
        public virtual void VisitBoundRequiredParameter(ITsBoundRequiredParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a required function parameter in the form <c>parameterName: 'stringLiteral'</c>.
        /// </summary>
        public virtual void VisitStringRequiredParameter(ITsStringRequiredParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a a bound optional parameter in a function.
        /// </summary>
        public virtual void VisitBoundOptionalParameter(ITsBoundOptionalParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a required function parameter in the form <c>parameterName: 'stringLiteral'</c>.
        /// </summary>
        public virtual void VisitStringOptionalParameter(ITsStringOptionalParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a a function parameter of the form '... parameterName: type'.
        /// </summary>
        public virtual void VisitRestParameter(ITsRestParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a constructor method signature of the form 'new &lt;T&gt;(parameter: type): type'.
        /// </summary>
        public virtual void VisitConstructSignature(ITsConstructSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits an index signature of the form '[parameterName: string|number]: type'.
        /// </summary>
        public virtual void VisitIndexSignature(ITsIndexSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a method signature, which is a shorthand for declaring a property of a function type.
        /// </summary>
        public virtual void VisitMethodSignature(ITsMethodSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a type alias of the form 'type alias&lt;T&gt; = type'.
        /// </summary>
        public virtual void VisitTypeAliasDeclaration(ITsTypeAliasDeclaration node) => DefaultVisit(node);
    }

    public abstract partial class TsVisitor<TResult>
    {
        /// <summary>
        /// Visits a type surrounded in parentheses.
        /// </summary>
        public virtual TResult VisitParenthesizedType(ITsParenthesizedType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a predefined type, which is one of any, number, boolean, string, symbol, or void.
        /// </summary>
        public virtual TResult VisitPredefinedType(ITsPredefinedType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript type reference.
        /// </summary>
        public virtual TResult VisitTypeReference(ITsTypeReference node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript object type.
        /// </summary>
        public virtual TResult VisitObjectType(ITsObjectType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript array type.
        /// </summary>
        public virtual TResult VisitArrayType(ITsArrayType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript tuple type.
        /// </summary>
        public virtual TResult VisitTupleType(ITsTupleType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript function type.
        /// </summary>
        public virtual TResult VisitFunctionType(ITsFunctionType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a TypeScript constructor type.
        /// </summary>
        public virtual TResult VisitConstructorType(ITsFunctionType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a typeof query.
        /// </summary>
        public virtual TResult VisitTypeQuery(ITsTypeQuery node) => DefaultVisit(node);

        /// <summary>
        /// Visits a 'this' type.
        /// </summary>
        public virtual TResult VisitThisType(ITsThisType node) => DefaultVisit(node);

        /// <summary>
        /// Visits a property signature.
        /// </summary>
        public virtual TResult VisitPropertySignature(ITsPropertySignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a a call signature of the form '&gt;T&lt;(parameter: type): type'..
        /// </summary>
        public virtual TResult VisitCallSignature(ITsCallSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a parameter list of the form '(parameter: type)'..
        /// </summary>
        public virtual TResult VisitParameterList(ITsParameterList node) => DefaultVisit(node);

        /// <summary>
        /// Visits a type parameter of the form &lt;MyType extends MyBase&gt;.
        /// </summary>
        public virtual TResult VisitTypeParameter(ITsTypeParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a bound required parameter in a parameter list for a function.
        /// </summary>
        public virtual TResult VisitBoundRequiredParameter(ITsBoundRequiredParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a required function parameter in the form <c>parameterName: 'stringLiteral'</c>.
        /// </summary>
        public virtual TResult VisitStringRequiredParameter(ITsStringRequiredParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a a bound optional parameter in a function.
        /// </summary>
        public virtual TResult VisitBoundOptionalParameter(ITsBoundOptionalParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a required function parameter in the form <c>parameterName: 'stringLiteral'</c>.
        /// </summary>
        public virtual TResult VisitStringOptionalParameter(ITsStringOptionalParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a a function parameter of the form '... parameterName: type'.
        /// </summary>
        public virtual TResult VisitRestParameter(ITsRestParameter node) => DefaultVisit(node);

        /// <summary>
        /// Visits a constructor method signature of the form 'new &lt;T&gt;(parameter: type): type'.
        /// </summary>
        public virtual TResult VisitConstructSignature(ITsConstructSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits an index signature of the form '[parameterName: string|number]: type'.
        /// </summary>
        public virtual TResult VisitIndexSignature(ITsIndexSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a method signature, which is a shorthand for declaring a property of a function type.
        /// </summary>
        public virtual TResult VisitMethodSignature(ITsMethodSignature node) => DefaultVisit(node);

        /// <summary>
        /// Visits a type alias of the form 'type alias&lt;T&gt; = type'.
        /// </summary>
        public virtual TResult VisitTypeAliasDeclaration(ITsTypeAliasDeclaration node) => DefaultVisit(node);
    }
}