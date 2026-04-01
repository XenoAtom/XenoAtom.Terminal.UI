// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

#pragma warning disable RS2000 // New analyzer diagnostics are tracked out-of-band for this repo.

namespace XenoAtom.Terminal.UI.SourceGen;

[Generator(LanguageNames.CSharp)]
public sealed partial class TerminalUiGenerator : IIncrementalGenerator
{
    private const string BindableAttributeMetadataName = "XenoAtom.Terminal.UI.BindableAttribute";
    private const string RoutedEventAttributeMetadataName = "XenoAtom.Terminal.UI.RoutedEventAttribute";
    private const string FluentAttributeMetadataName = "XenoAtom.Terminal.UI.FluentAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var bindableProperties = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BindableAttributeMetadataName,
                static (node, _) => node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, ct) => BindablePropertyInfo.TryCreate(ctx, ct));

        context.RegisterSourceOutput(
            bindableProperties.Collect(),
            static (spc, items) => BindableEmitter.Emit(spc, items));

        var fluentProperties = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                FluentAttributeMetadataName,
                static (node, _) => node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, ct) => FluentPropertyInfo.TryCreate(ctx, ct));

        context.RegisterSourceOutput(
            fluentProperties.Collect(),
            static (spc, items) => FluentEmitter.Emit(spc, items));

        var routedEventMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                RoutedEventAttributeMetadataName,
                static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, ct) => RoutedEventMethodInfo.TryCreate(ctx, ct));

        context.RegisterSourceOutput(
            routedEventMethods.Collect(),
            static (spc, items) => RoutedEventEmitter.Emit(spc, items));
    }

    private static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor TypeMustBePartial = new(
            id: "XATUI001",
            title: "Containing type must be partial",
            messageFormat: "The containing type '{0}' must be declared partial to use this attribute",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor BindablePropertyMustBePartialDeclaration = new(
            id: "XATUI002",
            title: "Bindable property must be a partial declaration",
            messageFormat: "The bindable property '{0}' must be declared as a partial auto-property declaration (no bodies)",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor BindablePropertyMissingPartialWarning = new(
            id: "XATUI008",
            title: "Bindable property should be partial",
            messageFormat: "The bindable property '{0}' is an auto-property without the 'partial' keyword; binding tracking will not be generated",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RoutedEventMethodMustHaveSingleArg = new(
            id: "XATUI003",
            title: "Routed event method signature is invalid",
            messageFormat: "The routed event method '{0}' must return void and take exactly one parameter",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor BindablePropertyMustHaveGetterAndSetter = new(
            id: "XATUI004",
            title: "Bindable property must have getter and setter",
            messageFormat: "The bindable property '{0}' must have both a getter and a setter",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor BindableListPropertyMustBeGetOnly = new(
            id: "XATUI005",
            title: "Bindable list property must be get-only",
            messageFormat: "The bindable list property '{0}' must be declared as a non-partial get-only property (no setter)",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FluentPropertyMustBeSettable = new(
            id: "XATUI006",
            title: "Fluent property must be settable",
            messageFormat: "The fluent property '{0}' must have a setter (not init-only)",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CannotGenerateAccessorType = new(
            id: "XATUI007",
            title: "Cannot generate Accessor type",
            messageFormat: "The type '{0}' already declares a member named 'Accessor'; the source generator will not generate the nested 'Accessor' type",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }

    private static (bool IsDelegatorProperty, string? DelegatorDelegateTypeFullyQualified) TryGetDelegatorDelegateType(
        Compilation compilation,
        ITypeSymbol propertyType,
        SymbolDisplayFormat format)
    {
        var delegatorType = compilation.GetTypeByMetadataName("XenoAtom.Terminal.UI.Delegator`1");
        if (delegatorType is null)
        {
            return (false, null);
        }

        if (propertyType is not INamedTypeSymbol namedType)
        {
            return (false, null);
        }

        if (!SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, delegatorType))
        {
            return (false, null);
        }

        if (namedType.TypeArguments.Length != 1)
        {
            return (false, null);
        }

        var delegateType = namedType.TypeArguments[0];
        var delegateTypeFullyQualified = delegateType.ToDisplayString(format);

        // A Delegator<TDelegate> can always be empty, so the fluent API should allow passing null to clear it.
        if (delegateType.IsReferenceType && delegateType.NullableAnnotation != NullableAnnotation.Annotated &&
            !delegateTypeFullyQualified.EndsWith("?", StringComparison.Ordinal))
        {
            delegateTypeFullyQualified += "?";
        }

        return (true, delegateTypeFullyQualified);
    }

    private static bool IsPubliclyVisible(INamedTypeSymbol typeSymbol)
    {
        for (INamedTypeSymbol? current = typeSymbol; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    private sealed record BindablePropertyInfo(
        INamedTypeSymbol ContainingType,
        string Namespace,
        string ContainingTypeDisplayName,
        string PropertyName,
        string PropertyTypeFullyQualified,
        string? PropertyDocumentationXml,
        bool IsDelegateProperty,
        bool IsDelegatorProperty,
        string? DelegatorDelegateTypeFullyQualified,
        bool IsBindableListFluentOnlyProperty,
        bool CanGenerateFluentExtensions,
        string? BindableListElementTypeFullyQualified,
        bool IncludeInBindings,
        bool IsParentVisual,
        bool NoVisualAttach,
        bool IsVisualChildProperty,
        bool GenerateImplementation,
        string PropertyModifiers,
        string GetAccessorModifier,
        string SetAccessorModifier,
        string SetAccessorKeyword,
        bool SetPrivateOrInternal,
        string BackingFieldName,
        string AccessorClassName)
    {
        public static BindablePropertyResult TryCreate(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.TargetNode is not PropertyDeclarationSyntax propertySyntax)
            {
                return new BindablePropertyResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            if (context.TargetSymbol is not IPropertySymbol propertySymbol)
            {
                return new BindablePropertyResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            var containingType = propertySymbol.ContainingType;
            if (containingType is null)
            {
                return new BindablePropertyResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            if (!IsPartial(containingType))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, propertySyntax.Identifier.GetLocation(), containingType.ToDisplayString()));
                return new BindablePropertyResult(null, diagnostics.ToImmutable());
            }

            var generateImplementation = propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

            var typeFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            var propertyTypeFullyQualified = propertySymbol.Type.ToDisplayString(typeFormat);
            var isDelegateProperty = propertySymbol.Type.TypeKind == TypeKind.Delegate;
            var (isDelegatorProperty, delegatorDelegateTypeFullyQualified) = TryGetDelegatorDelegateType(context.SemanticModel.Compilation, propertySymbol.Type, typeFormat);
            var (isBindableListProperty, bindableListElementTypeFullyQualified) = TryGetBindableListElementType(context.SemanticModel.Compilation, propertySymbol.Type, typeFormat);
            var propertyDocumentationXml = propertySymbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);

            var isBindableListFluentOnlyProperty = false;
            var includeInBindings = true;

            if (isBindableListProperty)
            {
                // BindableList/VisualList properties participate in binding via the list itself (reads/writes),
                // so we only generate fluent extensions for them. They must be declared as get-only (no setter)
                // and non-partial (no codegen-backed implementation).
                if (generateImplementation || propertySymbol.SetMethod is not null)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.BindableListPropertyMustBeGetOnly, propertySyntax.Identifier.GetLocation(), propertySymbol.Name));
                    return new BindablePropertyResult(null, diagnostics.ToImmutable());
                }

                if (propertySymbol.GetMethod is null)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.BindablePropertyMustHaveGetterAndSetter, propertySyntax.Identifier.GetLocation(), propertySymbol.Name));
                    return new BindablePropertyResult(null, diagnostics.ToImmutable());
                }

                isBindableListFluentOnlyProperty = true;
                includeInBindings = false;
            }
            else
            {
                if (generateImplementation)
                {
                    if (!IsPartialAutoPropertyDeclaration(propertySyntax))
                    {
                        diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.BindablePropertyMustBePartialDeclaration, propertySyntax.Identifier.GetLocation(), propertySymbol.Name));
                        return new BindablePropertyResult(null, diagnostics.ToImmutable());
                    }
                }
                else
                {
                    if (propertySymbol.GetMethod is null || propertySymbol.SetMethod is null)
                    {
                        diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.BindablePropertyMustHaveGetterAndSetter, propertySyntax.Identifier.GetLocation(), propertySymbol.Name));
                        return new BindablePropertyResult(null, diagnostics.ToImmutable());
                    }

                    // A non-partial auto-property means we cannot generate read/write tracking. This usually indicates a missing 'partial' keyword.
                    if (IsAutoPropertyDeclaration(propertySyntax))
                    {
                        diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.BindablePropertyMissingPartialWarning, propertySyntax.Identifier.GetLocation(), propertySymbol.Name));
                    }
                }
            }

            bool privateOrInternalSet = propertySymbol.SetMethod is not null && (propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Private || propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Internal);

            if (propertySymbol.IsStatic)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.BindablePropertyMustBePartialDeclaration, propertySyntax.Identifier.GetLocation(), propertySymbol.Name));
                return new BindablePropertyResult(null, diagnostics.ToImmutable());
            }

            var ns = containingType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "global::";
            if (ns.StartsWith("global::", StringComparison.Ordinal))
            {
                ns = ns.Substring("global::".Length);
            }

            var containingTypeDisplayName = containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var noVisualAttach = GetNoVisualAttach(context.SemanticModel.Compilation, propertySymbol);
            var isVisualChildProperty = !noVisualAttach && ComputeIsVisualChildProperty(context.SemanticModel.Compilation, containingType, propertySymbol.Type);
            var isVisual = InheritsFromVisual(context.SemanticModel.Compilation, containingType);

            // Fluent extension methods live in a separate static class and can only assign bindable properties that have
            // a public, non-init setter. Read-only BindableList/VisualList properties are a special case handled via
            // list-specific fluent APIs.
            var hasPublicAssignableSetter =
                isBindableListFluentOnlyProperty ||
                (propertySymbol.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false });

            var canGenerateFluentExtensions =
                IsPubliclyVisible(containingType) &&
                propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                hasPublicAssignableSetter;

            var propertyName = propertySymbol.Name;
            var backingFieldName = "_" + ToLowerCamel(propertyName);
            var accessorClassName = $"__{propertyName}__BindingAccessor";

            var modifiers = string.Join(" ", propertySyntax.Modifiers.Select(m => m.Text));

            var getAccessorModifier = ComputeAccessorModifier(propertySymbol, propertySymbol.GetMethod);
            var setAccessorModifier = ComputeAccessorModifier(propertySymbol, propertySymbol.SetMethod);
            var setAccessorKeyword = propertySymbol.SetMethod?.IsInitOnly == true ? "init" : "set";

            return new BindablePropertyResult(new BindablePropertyInfo(
                ContainingType: containingType,
                Namespace: ns,
                ContainingTypeDisplayName: containingTypeDisplayName,
                PropertyName: propertyName,
                PropertyTypeFullyQualified: propertyTypeFullyQualified,
                PropertyDocumentationXml: string.IsNullOrWhiteSpace(propertyDocumentationXml) ? null : propertyDocumentationXml,
                IsDelegateProperty: isDelegateProperty,
                IsDelegatorProperty: isDelegatorProperty,
                DelegatorDelegateTypeFullyQualified: delegatorDelegateTypeFullyQualified,
                IsBindableListFluentOnlyProperty: isBindableListFluentOnlyProperty,
                CanGenerateFluentExtensions: canGenerateFluentExtensions,
                BindableListElementTypeFullyQualified: bindableListElementTypeFullyQualified,
                IncludeInBindings: includeInBindings,
                IsParentVisual: isVisual,
                NoVisualAttach: noVisualAttach,
                IsVisualChildProperty: isVisualChildProperty,
                GenerateImplementation: generateImplementation,
                PropertyModifiers: modifiers,
                GetAccessorModifier: getAccessorModifier,
                SetAccessorModifier: setAccessorModifier,
                SetAccessorKeyword: setAccessorKeyword,
                SetPrivateOrInternal: privateOrInternalSet,
                BackingFieldName: backingFieldName,
                AccessorClassName: accessorClassName), diagnostics.ToImmutable());
        }

        private static (bool IsBindableListProperty, string? ElementTypeFullyQualified) TryGetBindableListElementType(
            Compilation compilation,
            ITypeSymbol propertyType,
            SymbolDisplayFormat format)
        {
            var bindableListType = compilation.GetTypeByMetadataName("XenoAtom.Terminal.UI.Collections.BindableList`1");
            if (bindableListType is null)
            {
                return (false, null);
            }

            if (propertyType is not INamedTypeSymbol named)
            {
                return (false, null);
            }

            for (var current = named; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, bindableListType))
                {
                    if (current.TypeArguments.Length != 1)
                    {
                        return (false, null);
                    }

                    var elementType = current.TypeArguments[0];
                    return (true, elementType.ToDisplayString(format));
                }
            }

            return (false, null);
        }

        private static string ComputeAccessorModifier(IPropertySymbol property, IMethodSymbol? accessor)
        {
            if (accessor is null)
            {
                return string.Empty;
            }

            var propertyAccessibility = property.DeclaredAccessibility;
            var accessorAccessibility = accessor.DeclaredAccessibility;

            if (propertyAccessibility == accessorAccessibility)
            {
                return string.Empty;
            }

            return accessorAccessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Public => "public",
                _ => string.Empty,
            };
        }

        
        private static bool ComputeIsVisualChildProperty(Compilation compilation, INamedTypeSymbol containingType, ITypeSymbol propertyType)
        {
            var visualType = compilation.GetTypeByMetadataName("XenoAtom.Terminal.UI.Visual");
            if (visualType is null)
            {
                return false;
            }

            return IsOrInheritsFrom(containingType, visualType) && IsOrInheritsFrom(propertyType, visualType);
        }

        private static bool GetNoVisualAttach(Compilation compilation, IPropertySymbol propertySymbol)
        {
            var bindableAttribute = compilation.GetTypeByMetadataName("XenoAtom.Terminal.UI.BindableAttribute");
            if (bindableAttribute is null)
            {
                return false;
            }

            foreach (var attribute in propertySymbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, bindableAttribute))
                {
                    continue;
                }

                foreach (var named in attribute.NamedArguments)
                {
                    if (!string.Equals(named.Key, "NoVisualAttach", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    return named.Value.Value is bool b && b;
                }

                return false;
            }

            return false;
        }


        public static bool IsPartial(INamedTypeSymbol typeSymbol)
        {
            foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl)
                {
                    if (typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsPartialAutoPropertyDeclaration(PropertyDeclarationSyntax propertySyntax)
        {
            if (!propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return false;
            }

            return IsAutoPropertyDeclaration(propertySyntax);
        }

        private static bool IsAutoPropertyDeclaration(PropertyDeclarationSyntax propertySyntax)
        {
            if (propertySyntax.ExpressionBody is not null)
            {
                return false;
            }

            if (propertySyntax.Initializer is not null)
            {
                return false;
            }

            if (propertySyntax.AccessorList is null)
            {
                return false;
            }

            foreach (var accessor in propertySyntax.AccessorList.Accessors)
            {
                if (accessor.Body is not null || accessor.ExpressionBody is not null)
                {
                    return false;
                }

                if (!accessor.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static bool InheritsFromVisual(Compilation compilation, INamedTypeSymbol type)
    {
        var visualType = compilation.GetTypeByMetadataName("XenoAtom.Terminal.UI.Visual");
        if (visualType is null)
        {
            return false;
        }
        return IsOrInheritsFrom(type, visualType);
    }

    private static bool IsOrInheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        if (SymbolEqualityComparer.Default.Equals(type, baseType))
        {
            return true;
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        for (var current = named.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }


    private sealed record BindablePropertyResult(BindablePropertyInfo? Info, ImmutableArray<Diagnostic> Diagnostics);

    private sealed record FluentPropertyResult(FluentPropertyInfo? Info, ImmutableArray<Diagnostic> Diagnostics);

    private static class BindableEmitter
    {
        public static void Emit(SourceProductionContext context, ImmutableArray<BindablePropertyResult> items)
        {
            if (items.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var item in items)
            {
                foreach (var diagnostic in item.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            var infos = items
                .Select(static item => item.Info)
                .Where(static info => info is not null)
                .Select(static info => info!)
                .ToList();

            var grouped = infos
                .GroupBy(static item => item.ContainingType, (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
                .ToList();

            var typesWithBindings = new HashSet<INamedTypeSymbol>((IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default);
            foreach (var group in grouped)
            {
                if (group.Key is not null && group.Any(static p => p.IncludeInBindings))
                {
                    typesWithBindings.Add(group.Key);
                }
            }

            var typesWithAccessors = new HashSet<INamedTypeSymbol>((IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default);
            foreach (var group in grouped)
            {
                var containingType = group.Key;
                if (containingType is null || !group.Any(static p => p.IncludeInBindings))
                {
                    continue;
                }

                // The generated nested type is named "Accessor". Skip generation when it would conflict with
                // an existing member (e.g. a property named Accessor).
                if (containingType.GetMembers("Accessor").Length != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.CannotGenerateAccessorType, containingType.Locations.FirstOrDefault(), containingType.ToDisplayString()));
                    continue;
                }

                typesWithAccessors.Add(containingType);
            }

            foreach (var group in grouped)
            {
                var containingType = group.Key;
                if (containingType is null)
                {
                    continue;
                }

                var ordered = group.OrderBy(static item => item.PropertyName, StringComparer.Ordinal).ToList();
                if (ordered.Count == 0)
                {
                    continue;
                }

                var orderedForBindings = ordered.Where(static p => p.IncludeInBindings).ToList();
                if (orderedForBindings.Count > 0)
                {
                    var hintName = $"{SanitizeFileName(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.Bindings.g.cs";
                    var source = GenerateBindingsSource(containingType, orderedForBindings, typesWithBindings, typesWithAccessors);
                    context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
                }

                // Only public types surface fluent extensions. Non-public bindable properties still get their binding implementation.
                if (IsPubliclyVisible(containingType))
                {
                    var fluentHintName = $"{SanitizeFileName(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.Fluent.g.cs";
                    var fluentSource = GenerateFluentExtensionsSource(containingType, ordered);
                    context.AddSource(fluentHintName, SourceText.From(fluentSource, Encoding.UTF8));
                }
            }
        }

        private static string GenerateFluentExtensionsSource(INamedTypeSymbol containingType, List<BindablePropertyInfo> properties)
        {
            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");

            var ns = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append("namespace ").Append(ns).AppendLine();
                sb.AppendLine("{");
            }

            var indent = new string(' ', string.IsNullOrEmpty(ns) ? 0 : 4);
            var extensionClassName = GetExtensionClassName(containingType);

            string receiverTypeXml = DocumentationCommentId.CreateDeclarationId(containingType)
                ?? containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            sb.Append(indent).AppendLine("/// <summary>");
            sb.Append(indent).Append("/// Fluent extension methods for configuring instances of <see cref=\"")
                .Append(receiverTypeXml)
                .AppendLine("\"/>.");
            sb.Append(indent).AppendLine("/// </summary>");
            sb.Append(indent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
            sb.Append(indent).Append("public static partial class ").Append(extensionClassName).AppendLine();
            sb.Append(indent).AppendLine("{");

            var methodIndent = indent + "    ";
            var receiverType = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            // Use a generic receiver only for non-generic, non-sealed classes (so derived types can fluently chain).
            // For generic controls (e.g. Slider<T>), we must emit the containing type parameters and constraints
            // on each extension method so it can compile and preserve type safety.
            var canUseGeneric = containingType.TypeKind == TypeKind.Class && !containingType.IsSealed && containingType.TypeParameters.Length == 0;
            var needsTypeParameters = containingType.TypeParameters.Length > 0;

            if (properties.Any(static p => p.IncludeInBindings))
            {
                sb.Append(methodIndent).AppendLine("/// <summary>");
                sb.Append(methodIndent).AppendLine("/// Gets a typed view over this instance bindable properties as <see cref=\"global::XenoAtom.Terminal.UI.Binding{T}\"/> values.");
                sb.Append(methodIndent).AppendLine("/// </summary>");

                if (needsTypeParameters)
                {
                    sb.Append(methodIndent).Append("extension");
                    AppendTypeParameters(sb, containingType);
                    sb.Append('(').Append(receiverType).Append(" obj)");
                    AppendTypeParameterConstraints(sb, containingType, methodIndent);
                }
                else
                {
                    sb.Append(methodIndent).Append("extension(").Append(receiverType).AppendLine(" obj)");
                }

                sb.Append(methodIndent).AppendLine("{");
                sb.Append(methodIndent).AppendLine("    /// <summary>");
                sb.Append(methodIndent).AppendLine("    /// Gets access to bindable properties of this instance.");
                sb.Append(methodIndent).AppendLine("    /// </summary>");
                sb.Append(methodIndent).AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(methodIndent).Append("    public ").Append(receiverType).Append(".IBindings Bind => (").Append(receiverType).AppendLine(".IBindings)obj;");
                sb.Append(methodIndent).AppendLine("}");
                sb.AppendLine();
            }

            foreach (var p in properties)
            {
                if (!p.CanGenerateFluentExtensions)
                {
                    continue;
                }

                var propName = p.PropertyName;
                var argName = ToLowerCamel(propName);
                var argType = p.PropertyTypeFullyQualified;
                var setterArgType = p.IsDelegatorProperty && p.DelegatorDelegateTypeFullyQualified is { } delegatorArgType
                    ? delegatorArgType
                    : argType;

                if (p.SetPrivateOrInternal)
                {
                    continue;
                }

                if (p.IsBindableListFluentOnlyProperty)
                {
                    var elementType = p.BindableListElementTypeFullyQualified ?? "global::System.Object";

                    sb.Append(methodIndent).AppendLine("/// <summary>");
                    sb.Append(methodIndent).Append("/// Replaces the contents of <see cref=\"").Append(receiverTypeXml).Append('.').Append(EscapeIdentifier(propName)).AppendLine("\"/> and returns the same instance.");
                    sb.Append(methodIndent).AppendLine("/// </summary>");
                    sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                    sb.Append(methodIndent).Append("/// <param name=\"").Append(argName).AppendLine("\">The items to populate the list with, or <c>null</c> to clear.</param>");
                    sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                    sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                    if (canUseGeneric)
                    {
                        sb.Append(methodIndent).Append("public static T ").Append(EscapeIdentifier(propName)).Append("<T>(this T obj, global::System.Collections.Generic.IEnumerable<").Append(elementType).Append(">? ").Append(EscapeIdentifier(argName))
                            .Append(") where T : ").Append(receiverType).AppendLine();
                        sb.Append(methodIndent).AppendLine("{");
                        sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                        sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).AppendLine(".Clear();");
                        sb.Append(methodIndent).Append("    if (").Append(EscapeIdentifier(argName)).AppendLine(" is null) return obj;");
                        sb.Append(methodIndent).Append("    foreach (var item in ").Append(EscapeIdentifier(argName)).AppendLine(")");
                        sb.Append(methodIndent).Append("        obj.").Append(EscapeIdentifier(propName)).AppendLine(".Add(item);");
                        sb.Append(methodIndent).AppendLine("    return obj;");
                        sb.Append(methodIndent).AppendLine("}");
                    }
                    else
                    {
                        sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(EscapeIdentifier(propName));
                        if (needsTypeParameters)
                        {
                            AppendTypeParameters(sb, containingType);
                        }
                        sb.Append("(this ").Append(receiverType).Append(" obj, global::System.Collections.Generic.IEnumerable<").Append(elementType).Append(">? ").Append(EscapeIdentifier(argName)).Append(')');
                        if (needsTypeParameters)
                        {
                            AppendTypeParameterConstraints(sb, containingType, methodIndent);
                        }
                        else
                        {
                            sb.AppendLine();
                        }
                        sb.Append(methodIndent).AppendLine("{");
                        sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                        sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).AppendLine(".Clear();");
                        sb.Append(methodIndent).Append("    if (").Append(EscapeIdentifier(argName)).AppendLine(" is null) return obj;");
                        sb.Append(methodIndent).Append("    foreach (var item in ").Append(EscapeIdentifier(argName)).AppendLine(")");
                        sb.Append(methodIndent).Append("        obj.").Append(EscapeIdentifier(propName)).AppendLine(".Add(item);");
                        sb.Append(methodIndent).AppendLine("    return obj;");
                        sb.Append(methodIndent).AppendLine("}");
                    }
                    sb.AppendLine();

                    sb.Append(methodIndent).AppendLine("/// <summary>");
                    sb.Append(methodIndent).Append("/// Replaces the contents of <see cref=\"").Append(receiverTypeXml).Append('.').Append(EscapeIdentifier(propName)).AppendLine("\"/> and returns the same instance.");
                    sb.Append(methodIndent).AppendLine("/// </summary>");
                    sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                    sb.Append(methodIndent).Append("/// <param name=\"").Append(argName).AppendLine("\">The items to populate the list with.</param>");
                    sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                    sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                    if (canUseGeneric)
                    {
                        sb.Append(methodIndent).Append("public static T ").Append(EscapeIdentifier(propName)).Append("<T>(this T obj, params global::System.ReadOnlySpan<").Append(elementType).Append("> ").Append(EscapeIdentifier(argName))
                            .Append(") where T : ").Append(receiverType).AppendLine();
                        sb.Append(methodIndent).AppendLine("{");
                        sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                        sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).AppendLine(".Clear();");
                        sb.Append(methodIndent).Append("    for (var i = 0; i < ").Append(EscapeIdentifier(argName)).AppendLine(".Length; i++)");
                        sb.Append(methodIndent).Append("        obj.").Append(EscapeIdentifier(propName)).Append(".Add(").Append(EscapeIdentifier(argName)).AppendLine("[i]);");
                        sb.Append(methodIndent).AppendLine("    return obj;");
                        sb.Append(methodIndent).AppendLine("}");
                    }
                    else
                    {
                        sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(EscapeIdentifier(propName));
                        if (needsTypeParameters)
                        {
                            AppendTypeParameters(sb, containingType);
                        }
                        sb.Append("(this ").Append(receiverType).Append(" obj, params global::System.ReadOnlySpan<").Append(elementType).Append("> ").Append(EscapeIdentifier(argName)).Append(')');
                        if (needsTypeParameters)
                        {
                            AppendTypeParameterConstraints(sb, containingType, methodIndent);
                        }
                        else
                        {
                            sb.AppendLine();
                        }
                        sb.Append(methodIndent).AppendLine("{");
                        sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                        sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).AppendLine(".Clear();");
                        sb.Append(methodIndent).Append("    for (var i = 0; i < ").Append(EscapeIdentifier(argName)).AppendLine(".Length; i++)");
                        sb.Append(methodIndent).Append("        obj.").Append(EscapeIdentifier(propName)).Append(".Add(").Append(EscapeIdentifier(argName)).AppendLine("[i]);");
                        sb.Append(methodIndent).AppendLine("    return obj;");
                        sb.Append(methodIndent).AppendLine("}");
                    }

                    sb.AppendLine();
                    continue;
                }

                AppendFluentPropertyXmlDocumentation(sb, methodIndent, receiverTypeXml, propName, p.PropertyDocumentationXml);
                sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                sb.Append(methodIndent).Append("/// <param name=\"").Append(argName).AppendLine("\">The value to set.</param>");
                sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                if (canUseGeneric)
                {
                    sb.Append(methodIndent).Append("public static T ").Append(EscapeIdentifier(propName)).Append("<T>(this T obj, ").Append(setterArgType).Append(' ').Append(EscapeIdentifier(argName))
                        .Append(") where T : ").Append(receiverType).AppendLine();
                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).Append(" = ").Append(EscapeIdentifier(argName)).AppendLine(";");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }
                else
                {
                    sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(EscapeIdentifier(propName));
                    if (needsTypeParameters)
                    {
                        AppendTypeParameters(sb, containingType);
                    }
                    sb.Append("(this ").Append(receiverType).Append(" obj, ").Append(setterArgType).Append(' ').Append(EscapeIdentifier(argName)).Append(')');
                    if (needsTypeParameters)
                    {
                        AppendTypeParameterConstraints(sb, containingType, methodIndent);
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).Append(" = ").Append(EscapeIdentifier(argName)).AppendLine(";");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }
                sb.AppendLine();

                if (p.GenerateImplementation)
                {
                    sb.Append(methodIndent).AppendLine("/// <summary>");
                    sb.Append(methodIndent).Append("/// Binds <see cref=\"").Append(receiverTypeXml).Append('.').Append(EscapeIdentifier(propName)).AppendLine("\"/> to a binding and returns the same instance.");
                    sb.Append(methodIndent).AppendLine("/// </summary>");
                    sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                    sb.Append(methodIndent).Append("/// <param name=\"").Append(EscapeIdentifier(argName)).AppendLine("\">The binding to attach.</param>");
                    sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                    sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                    if (canUseGeneric)
                    {
                        sb.Append(methodIndent).Append("public static T ").Append(EscapeIdentifier(propName)).Append("<T>(this T obj, global::XenoAtom.Terminal.UI.Binding<").Append(argType).Append("> ").Append(EscapeIdentifier(argName))
                            .Append(") where T : ").Append(receiverType).AppendLine();
                        sb.Append(methodIndent).AppendLine("{");
                        sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                        sb.Append(methodIndent).Append("    obj.Bind").Append(propName).Append("(").Append(EscapeIdentifier(argName)).AppendLine(");");
                        sb.Append(methodIndent).AppendLine("    return obj;");
                        sb.Append(methodIndent).AppendLine("}");
                    }
                    else
                    {
                        sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(EscapeIdentifier(propName));
                        if (needsTypeParameters)
                        {
                            AppendTypeParameters(sb, containingType);
                        }
                        sb.Append("(this ").Append(receiverType).Append(" obj, global::XenoAtom.Terminal.UI.Binding<").Append(argType).Append("> ").Append(EscapeIdentifier(argName)).Append(')');
                        if (needsTypeParameters)
                        {
                            AppendTypeParameterConstraints(sb, containingType, methodIndent);
                        }
                        else
                        {
                            sb.AppendLine();
                        }
                        sb.Append(methodIndent).AppendLine("{");
                        sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                        sb.Append(methodIndent).Append("    obj.Bind").Append(propName).Append("(").Append(EscapeIdentifier(argName)).AppendLine(");");
                        sb.Append(methodIndent).AppendLine("    return obj;");
                        sb.Append(methodIndent).AppendLine("}");
                    }
                    sb.AppendLine();
                }

                if (p.IsParentVisual)
                {
                    // If the property itself is a delegate (e.g. Func<...>), avoid generating an overload taking
                    // Func<PropertyType>. It would compete with the natural "set the delegate" overload and complicate
                    // method resolution for lambdas.
                    if (!p.IsDelegateProperty)
                    {
                        sb.Append(methodIndent).AppendLine("/// <summary>");
                        sb.Append(methodIndent).Append("/// Configures <see cref=\"").Append(receiverTypeXml).Append('.').Append(EscapeIdentifier(propName)).AppendLine("\"/> from a computed value and returns the same instance.");
                        sb.Append(methodIndent).AppendLine("/// </summary>");
                        sb.Append(methodIndent).AppendLine("/// <remarks>");
                        sb.Append(methodIndent).AppendLine("/// The delegate is evaluated as needed; accessed bindings are tracked so only affected visuals are refreshed.");
                        sb.Append(methodIndent).AppendLine("/// </remarks>");
                        sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                        sb.Append(methodIndent).Append("/// <param name=\"").Append(EscapeIdentifier(argName)).AppendLine("\">A delegate that computes the current value.</param>");
                        sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                        sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                        if (canUseGeneric)
                        {
                            sb.Append(methodIndent).Append("public static T ").Append(EscapeIdentifier(propName)).Append("<T>(this T obj, global::System.Func<").Append(argType).Append("> ").Append(EscapeIdentifier(argName))
                                .Append(") where T : ").Append(receiverType).AppendLine();
                            if (p.GenerateImplementation && !p.IsVisualChildProperty)
                            {
                                sb.Append(methodIndent).AppendLine("{");
                                sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                                sb.Append(methodIndent).Append("    var state = new global::XenoAtom.Terminal.UI.FuncState<").Append(argType).Append(">(").Append(EscapeIdentifier(argName)).AppendLine(");");
                                sb.Append(methodIndent).Append("    obj.Bind").Append(propName).Append("(((")
                                    .Append("global::XenoAtom.Terminal.UI.FuncState<").Append(argType).Append(">.IBindings)state).Value").AppendLine(");");
                                sb.Append(methodIndent).AppendLine("    return obj;");
                                sb.Append(methodIndent).AppendLine("}");
                            }
                            else
                            {
                                sb.Append(methodIndent).Append("    => global::XenoAtom.Terminal.UI.VisualExtensions.Update(obj, x => x.").Append(EscapeIdentifier(propName)).Append(" = ").Append(EscapeIdentifier(argName)).AppendLine("());");
                            }
                        }
                        else
                        {
                            sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(EscapeIdentifier(propName));
                            if (needsTypeParameters)
                            {
                                AppendTypeParameters(sb, containingType);
                            }
                            sb.Append("(this ").Append(receiverType).Append(" obj, global::System.Func<").Append(argType)
                                .Append("> ").Append(EscapeIdentifier(argName)).Append(')');
                            if (needsTypeParameters)
                            {
                                AppendTypeParameterConstraints(sb, containingType, methodIndent);
                            }
                            else
                            {
                                sb.AppendLine();
                            }
                            if (p.GenerateImplementation && !p.IsVisualChildProperty)
                            {
                                sb.Append(methodIndent).AppendLine("{");
                                sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                                sb.Append(methodIndent).Append("    var state = new global::XenoAtom.Terminal.UI.FuncState<").Append(argType).Append(">(").Append(EscapeIdentifier(argName)).AppendLine(");");
                                sb.Append(methodIndent).Append("    obj.Bind").Append(propName).Append("(((")
                                    .Append("global::XenoAtom.Terminal.UI.FuncState<").Append(argType).Append(">.IBindings)state).Value").AppendLine(");");
                                sb.Append(methodIndent).AppendLine("    return obj;");
                                sb.Append(methodIndent).AppendLine("}");
                            }
                            else
                            {
                                sb.Append(methodIndent).Append("    => global::XenoAtom.Terminal.UI.VisualExtensions.Update(obj, x => x.").Append(EscapeIdentifier(propName)).Append(" = ").Append(EscapeIdentifier(argName)).AppendLine("());");
                            }
                        }

                        sb.AppendLine();
                    }

                }
            }

            sb.Append(indent).AppendLine("}");

            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static void AppendFluentPropertyXmlDocumentation(
            StringBuilder sb,
            string indent,
            string receiverTypeXml,
            string propertyName,
            string? propertyDocumentationXml)
        {
            var fallbackSummary = $"Sets <see cref=\"{receiverTypeXml}.{EscapeIdentifier(propertyName)}\"/> and returns the same instance.";

            if (string.IsNullOrWhiteSpace(propertyDocumentationXml))
            {
                sb.Append(indent).AppendLine("/// <summary>");
                sb.Append(indent).Append("/// ").AppendLine(fallbackSummary);
                sb.Append(indent).AppendLine("/// </summary>");
                return;
            }

            try
            {
                var doc = XDocument.Parse(propertyDocumentationXml, LoadOptions.PreserveWhitespace);
                var member = doc.Root;
                if (member is null)
                {
                    throw new InvalidOperationException("Missing root element.");
                }

                var summaryElement = member.Element("summary");
                var remarksElement = member.Element("remarks");

                sb.Append(indent).AppendLine("/// <summary>");
                var summaryInnerXml = summaryElement is null ? string.Empty : GetInnerXml(summaryElement);
                if (string.IsNullOrWhiteSpace(summaryInnerXml))
                {
                    sb.Append(indent).Append("/// ").AppendLine(fallbackSummary);
                }
                else
                {
                    foreach (var line in FormatXmlDocText(TransformPropertySummaryToSetterSummary(summaryInnerXml)))
                    {
                        sb.Append(indent).Append("/// ").AppendLine(line);
                    }
                }
                sb.Append(indent).AppendLine("/// </summary>");

                if (remarksElement is not null)
                {
                    var remarksInnerXml = GetInnerXml(remarksElement);
                    if (!string.IsNullOrWhiteSpace(remarksInnerXml))
                    {
                        sb.Append(indent).AppendLine("/// <remarks>");
                        foreach (var line in FormatXmlDocText(remarksInnerXml))
                        {
                            sb.Append(indent).Append("/// ").AppendLine(line);
                        }
                        sb.Append(indent).AppendLine("/// </remarks>");
                    }
                }
            }
            catch
            {
                sb.Append(indent).AppendLine("/// <summary>");
                sb.Append(indent).Append("/// ").AppendLine(fallbackSummary);
                sb.Append(indent).AppendLine("/// </summary>");
            }
        }

        private static IEnumerable<string> FormatXmlDocText(string xmlText)
        {
            var normalized = xmlText.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n');
            var start = 0;
            while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
            {
                start++;
            }
            for (var i = start; i < lines.Length; i++)
            {
                yield return lines[i].TrimEnd();
            }
        }

        private static string TransformPropertySummaryToSetterSummary(string summaryInnerXml)
        {
            // Most properties follow the "Gets or sets ..." pattern; translate it for a setter fluent API.
            var text = summaryInnerXml.TrimStart();
            return text.StartsWith("Gets or sets", StringComparison.Ordinal)
                ? "Sets" + text.Substring("Gets or sets".Length)
                : summaryInnerXml;
        }

        private static string GetInnerXml(XElement element) => string.Concat(element.Nodes());

        private static string GetExtensionClassName(INamedTypeSymbol type)
        {
            var containingTypes = GetContainingTypes(type);
            var sb = new StringBuilder();
            for (var i = 0; i < containingTypes.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append('_');
                }
                AppendExtensionTypeName(sb, containingTypes[i]);
            }
            sb.Append("Extensions");
            return sb.ToString();
        }

        private static void AppendExtensionTypeName(StringBuilder sb, INamedTypeSymbol type)
        {
            sb.Append(type.Name);
            if (type.TypeParameters.Length == 0)
            {
                return;
            }

            sb.Append('_');
            for (var i = 0; i < type.TypeParameters.Length; i++)
            {
                if (i > 0) sb.Append('_');
                sb.Append(type.TypeParameters[i].Name);
            }
        }

        private static string EscapeIdentifier(string name)
        {
            return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
                ? "@" + name
                : name;
        }

        private static string GenerateBindingsSource(
            INamedTypeSymbol containingType,
            List<BindablePropertyInfo> properties,
            HashSet<INamedTypeSymbol> typesWithBindings,
            HashSet<INamedTypeSymbol> typesWithAccessors)
        {
            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");

            var ns = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append("namespace ").Append(ns).AppendLine();
                sb.AppendLine("{");
            }

            var containingTypes = GetContainingTypes(containingType);
            for (var i = 0; i < containingTypes.Count; i++)
            {
                var type = containingTypes[i];
                var indent = new string(' ', (string.IsNullOrEmpty(ns) ? 0 : 4) + (i * 4));

                sb.Append(indent);
                sb.Append("partial ").Append(GetTypeKeyword(type)).Append(' ').Append(type.Name);
                AppendTypeParameters(sb, type);
                if (i == containingTypes.Count - 1)
                {
                    sb.Append(" : ").Append(type.Name);
                    AppendTypeParameters(sb, type);
                    sb.Append(".IBindings");
                }
                sb.AppendLine();
                sb.Append(indent).AppendLine("{");
            }

            var baseIndent = new string(' ', (string.IsNullOrEmpty(ns) ? 0 : 4) + (containingTypes.Count * 4));

            var baseBindings = FindBaseBindings(containingType, typesWithBindings);
            var baseAccessor = FindBaseAccessor(containingType, typesWithAccessors);

            // Bind property is provided as a C# 14 extension member in the generated fluent extensions.
            // Backing fields
            foreach (var p in properties)
            {
                if (!p.GenerateImplementation)
                {
                    continue;
                }

                sb.Append(baseIndent).AppendLine("[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]");
                sb.Append(baseIndent).Append("private ").Append(p.PropertyTypeFullyQualified).Append(' ').Append(p.BackingFieldName).AppendLine(" = default!;");
                sb.AppendLine();

                sb.Append(baseIndent).AppendLine("[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]");
                sb.Append(baseIndent).Append("private global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append("> ").Append(p.BackingFieldName).AppendLine("Bound;");
                sb.AppendLine();
            }

            // Generated property implementations + accessors + explicit interface impl
            foreach (var p in properties)
            {
                if (p.GenerateImplementation)
                {
                    sb.Append(baseIndent).Append("partial void On").Append(p.PropertyName).Append("Changing(ref ").Append(p.PropertyTypeFullyQualified).AppendLine(" value);");
                    sb.Append(baseIndent).Append("partial void On").Append(p.PropertyName).Append("Changed(").Append(p.PropertyTypeFullyQualified).AppendLine(" value);");
                    sb.AppendLine();

                    sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                    sb.Append(baseIndent).Append("private void __ApplyBound").Append(p.PropertyName).Append("(").Append(p.PropertyTypeFullyQualified).AppendLine(" value)");
                    sb.Append(baseIndent).AppendLine("{");
                    sb.Append(baseIndent).Append("    var updated = value;").AppendLine();
                    sb.Append(baseIndent).Append("    On").Append(p.PropertyName).AppendLine("Changing(ref updated);");
                    sb.AppendLine();
                    if (p.IsVisualChildProperty)
                    {
                        sb.Append(baseIndent).Append("    if (global::System.Object.ReferenceEquals(").Append(p.BackingFieldName).AppendLine(", updated))");
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).AppendLine("        return;");
                        sb.Append(baseIndent).AppendLine("    }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("    if (").Append(p.BackingFieldName).AppendLine(" is not null)");
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).AppendLine("        DetachChild(" + p.BackingFieldName + ");");
                        sb.Append(baseIndent).AppendLine("    }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("    ").Append(p.BackingFieldName).AppendLine(" = updated;");
                        sb.Append(baseIndent).AppendLine("    if (updated is not null)");
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).AppendLine("        AttachChild(updated);");
                        sb.Append(baseIndent).AppendLine("    }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("    global::XenoAtom.Terminal.UI.BindingManager.Current.NotifyValueChanged(this, ").Append(p.AccessorClassName).AppendLine(".Instance);");
                        sb.Append(baseIndent).Append("    On").Append(p.PropertyName).AppendLine("Changed(updated);");
                    }
                    else
                    {
                        sb.Append(baseIndent).Append("    if (global::XenoAtom.Terminal.UI.BindingManager.Current.SetValue(this, ref ").Append(p.BackingFieldName).Append(", updated, ").Append(p.AccessorClassName).AppendLine(".Instance))");
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).Append("        On").Append(p.PropertyName).AppendLine("Changed(updated);");
                        sb.Append(baseIndent).AppendLine("    }");
                    }
                    sb.Append(baseIndent).AppendLine("}");
                    sb.AppendLine();

                    sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                    sb.Append(baseIndent).Append(p.PropertyModifiers).Append(' ').Append(p.PropertyTypeFullyQualified).Append(' ').Append(p.PropertyName).AppendLine();
                    sb.Append(baseIndent).AppendLine("{");
                    if (p.IsVisualChildProperty)
                    {
                        var boundFieldName = p.BackingFieldName + "Bound";
                        sb.Append(baseIndent).Append("    ").Append(string.IsNullOrEmpty(p.GetAccessorModifier) ? string.Empty : p.GetAccessorModifier + " ").Append("get").AppendLine();
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).Append("        if (!").Append(boundFieldName).AppendLine(".IsEmpty && ").Append(boundFieldName).AppendLine(".Owner is global::XenoAtom.Terminal.UI.IPullBindingSource)");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).Append("            __ApplyBound").Append(p.PropertyName).Append("(").Append(boundFieldName).AppendLine(".GetValue());");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        return global::XenoAtom.Terminal.UI.BindingManager.Current.GetValue(this, ref ").Append(p.BackingFieldName).Append(", ").Append(p.AccessorClassName).AppendLine(".Instance);");
                        sb.Append(baseIndent).AppendLine("    }");
                        sb.Append(baseIndent).Append("    ").Append(string.IsNullOrEmpty(p.SetAccessorModifier) ? string.Empty : p.SetAccessorModifier + " ").Append(p.SetAccessorKeyword).AppendLine();
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).Append("        if (!").Append(boundFieldName).AppendLine(".IsEmpty)");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).Append("            var boundUpdated = value;").AppendLine();
                        sb.Append(baseIndent).Append("            On").Append(p.PropertyName).AppendLine("Changing(ref boundUpdated);");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("            ").Append(boundFieldName).AppendLine(".SetValue(boundUpdated);");
                        sb.AppendLine();
                        sb.Append(baseIndent).AppendLine("            return;");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        var updated = value;").AppendLine();
                        sb.Append(baseIndent).Append("        On").Append(p.PropertyName).AppendLine("Changing(ref updated);");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        if (global::System.Object.ReferenceEquals(").Append(p.BackingFieldName).AppendLine(", updated))");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).AppendLine("            return;");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        if (").Append(p.BackingFieldName).AppendLine(" is not null)");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).AppendLine("            DetachChild(" + p.BackingFieldName + ");");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        ").Append(p.BackingFieldName).AppendLine(" = updated;");
                        sb.Append(baseIndent).AppendLine("        if (updated is not null)");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).AppendLine("            AttachChild(updated);");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        global::XenoAtom.Terminal.UI.BindingManager.Current.NotifyValueChanged(this, ").Append(p.AccessorClassName).AppendLine(".Instance);");
                        sb.Append(baseIndent).Append("        On").Append(p.PropertyName).AppendLine("Changed(updated);");
                        sb.Append(baseIndent).AppendLine("    }");
                    }
                    else
                    {
                        var boundFieldName = p.BackingFieldName + "Bound";
                        sb.Append(baseIndent).Append("    ").Append(string.IsNullOrEmpty(p.GetAccessorModifier) ? string.Empty : p.GetAccessorModifier + " ").AppendLine("get");
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).Append("        if (!").Append(boundFieldName).AppendLine(".IsEmpty && ").Append(boundFieldName).AppendLine(".Owner is global::XenoAtom.Terminal.UI.IPullBindingSource)");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).Append("            __ApplyBound").Append(p.PropertyName).Append("(").Append(boundFieldName).AppendLine(".GetValue());");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        return global::XenoAtom.Terminal.UI.BindingManager.Current.GetValue(this, ref ").Append(p.BackingFieldName).Append(", ").Append(p.AccessorClassName).AppendLine(".Instance);");
                        sb.Append(baseIndent).AppendLine("    }");
                        sb.Append(baseIndent).Append("    ").Append(string.IsNullOrEmpty(p.SetAccessorModifier) ? string.Empty : p.SetAccessorModifier + " ").Append(p.SetAccessorKeyword).AppendLine();
                        sb.Append(baseIndent).AppendLine("    {");
                        sb.Append(baseIndent).Append("        if (!").Append(boundFieldName).AppendLine(".IsEmpty)");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).Append("            var boundUpdated = value;").AppendLine();
                        sb.Append(baseIndent).Append("            On").Append(p.PropertyName).AppendLine("Changing(ref boundUpdated);");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("            ").Append(boundFieldName).AppendLine(".SetValue(boundUpdated);");
                        sb.AppendLine();
                        sb.Append(baseIndent).AppendLine("            return;");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.AppendLine();
                        sb.Append(baseIndent).Append("        var updated = value;").AppendLine();
                        sb.Append(baseIndent).Append("        On").Append(p.PropertyName).AppendLine("Changing(ref updated);");
                        sb.Append(baseIndent).Append("        if (global::XenoAtom.Terminal.UI.BindingManager.Current.SetValue(this, ref ").Append(p.BackingFieldName).Append(", updated, ").Append(p.AccessorClassName).AppendLine(".Instance))");
                        sb.Append(baseIndent).AppendLine("        {");
                        sb.Append(baseIndent).Append("            On").Append(p.PropertyName).AppendLine("Changed(updated);");
                        sb.Append(baseIndent).AppendLine("        }");
                        sb.Append(baseIndent).AppendLine("    }");
                    }
                    sb.Append(baseIndent).AppendLine("}");
                    sb.AppendLine();

                    sb.Append(baseIndent).AppendLine("/// <summary>");
                    sb.Append(baseIndent).Append("/// Binds <c>").Append(p.PropertyName).AppendLine("</c> to a binding.");
                    sb.Append(baseIndent).AppendLine("/// </summary>");
                    sb.Append(baseIndent).AppendLine("/// <remarks>");
                    sb.Append(baseIndent).AppendLine("/// When a binding is attached, sets to the property are forwarded to the binding. This method is hidden from IntelliSense.");
                    sb.Append(baseIndent).AppendLine("/// </remarks>");
                    sb.Append(baseIndent).AppendLine("/// <param name=\"binding\">The binding to attach.</param>");
                    sb.Append(baseIndent).AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
                    sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                    sb.Append(baseIndent).Append("public void Bind").Append(p.PropertyName).Append("(global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append("> binding)").AppendLine();
                    sb.Append(baseIndent).AppendLine("{");
                    sb.Append(baseIndent).Append("    if (global::System.Object.ReferenceEquals(").Append(p.BackingFieldName).Append("Bound.Owner, binding.Owner) && global::System.Object.ReferenceEquals(").Append(p.BackingFieldName).Append("Bound.Accessor, binding.Accessor))").AppendLine();
                    sb.Append(baseIndent).AppendLine("    {");
                    sb.Append(baseIndent).AppendLine("        return;");
                    sb.Append(baseIndent).AppendLine("    }");
                    sb.AppendLine();
                    sb.Append(baseIndent).Append("    if (!").Append(p.BackingFieldName).AppendLine("Bound.IsEmpty)");
                    sb.Append(baseIndent).AppendLine("    {");
                    sb.Append(baseIndent).Append("        global::XenoAtom.Terminal.UI.BindingManager.Current.UnregisterBoundValue(this, ").Append(p.AccessorClassName).Append(".Instance, ").Append(p.BackingFieldName).AppendLine("Bound);");
                    sb.Append(baseIndent).AppendLine("    }");
                    sb.AppendLine();
                    sb.Append(baseIndent).Append("    ").Append(p.BackingFieldName).Append("Bound = binding;").AppendLine();
                    sb.Append(baseIndent).Append("    if (binding.IsEmpty)").AppendLine();
                    sb.Append(baseIndent).AppendLine("    {");
                    sb.Append(baseIndent).AppendLine("        return;");
                    sb.Append(baseIndent).AppendLine("    }");
                    sb.AppendLine();
                    sb.Append(baseIndent).Append("    if (binding.Owner is not global::XenoAtom.Terminal.UI.IPullBindingSource)").AppendLine();
                    sb.Append(baseIndent).AppendLine("    {");
                    sb.Append(baseIndent).Append("        global::XenoAtom.Terminal.UI.BindingManager.Current.RegisterBoundValue(this, ").Append(p.AccessorClassName).Append(".Instance, binding, static (owner, value) => ((").Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(")owner).__ApplyBound").Append(p.PropertyName).AppendLine("(value));");
                    sb.Append(baseIndent).AppendLine("    }");
                    sb.AppendLine();
                    sb.Append(baseIndent).AppendLine("    using (global::XenoAtom.Terminal.UI.BindingManager.Current.SuppressReadTracking())");
                    sb.Append(baseIndent).AppendLine("    {");
                    sb.Append(baseIndent).Append("        __ApplyBound").Append(p.PropertyName).AppendLine("(binding.GetValue());");
                    sb.Append(baseIndent).AppendLine("    }");
                    sb.Append(baseIndent).AppendLine("}");
                    sb.AppendLine();
                }

                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append("global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append("> IBindings.").Append(p.PropertyName)
                    .Append(" => ");

                if (p.GenerateImplementation)
                {
                    var boundFieldNameForBindings = p.BackingFieldName + "Bound";
                    sb.Append(boundFieldNameForBindings).Append(".IsEmpty ? new global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append(">(this, ").Append(p.AccessorClassName).Append(".Instance) : ").Append(boundFieldNameForBindings).AppendLine(";");
                }
                else
                {
                    sb.Append("new global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append(">(this, ").Append(p.AccessorClassName).AppendLine(".Instance);");
                }
                sb.AppendLine();

                sb.Append(baseIndent).Append("private sealed class ").Append(p.AccessorClassName).Append(" : global::XenoAtom.Terminal.UI.BindingAccessor<").Append(p.PropertyTypeFullyQualified).AppendLine(">");
                sb.Append(baseIndent).AppendLine("{");
                sb.Append(baseIndent).Append("    public static ").Append(p.AccessorClassName).Append(" Instance { get; } = new ").Append(p.AccessorClassName).AppendLine("();");
                sb.AppendLine();
                sb.Append(baseIndent).Append("    private ").Append(p.AccessorClassName).Append("() : base(nameof(").Append(p.PropertyName).AppendLine("), StaticGetter, StaticSetter) { }");
                sb.AppendLine();
                sb.Append(baseIndent).Append("    private static ").Append(p.PropertyTypeFullyQualified).Append(" StaticGetter(object instance) => ((")
                    .Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(")instance).").Append(p.PropertyName).AppendLine(";");
                sb.Append(baseIndent).Append("    private static void StaticSetter(object instance, ").Append(p.PropertyTypeFullyQualified).Append(" value) => ((")
                    .Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(")instance).").Append(p.PropertyName).AppendLine(" = value;");
                sb.Append(baseIndent).AppendLine("}");
                sb.AppendLine();
            }

            if (typesWithAccessors.Contains(containingType))
            {
                // Public accessors for use outside of binding contexts (e.g. DataGrid column definitions).
                sb.Append(baseIndent).AppendLine("/// <summary>");
                sb.Append(baseIndent).AppendLine("/// Provides <see cref=\"global::XenoAtom.Terminal.UI.BindingAccessor{T}\"/> instances for bindable properties of this type.");
                sb.Append(baseIndent).AppendLine("/// </summary>");
                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append(string.IsNullOrEmpty(baseAccessor) ? "public class Accessor" : "public new class Accessor : ").Append(baseAccessor).AppendLine();
                sb.Append(baseIndent).AppendLine("{");
                sb.Append(baseIndent).AppendLine("    /// <summary>");
                sb.Append(baseIndent).AppendLine("    /// Initializes a new instance of the <see cref=\"Accessor\"/> class.");
                sb.Append(baseIndent).AppendLine("    /// </summary>");
                sb.Append(baseIndent).AppendLine("    protected Accessor() { }");
                sb.AppendLine();
                foreach (var p in properties)
                {
                    sb.Append(baseIndent).AppendLine("    /// <summary>");
                    sb.Append(baseIndent).Append("    /// Gets an accessor representing <c>").Append(p.PropertyName).AppendLine("</c>.");
                    sb.Append(baseIndent).AppendLine("    /// </summary>");
                    sb.Append(baseIndent).Append("    public static global::XenoAtom.Terminal.UI.BindingAccessor<").Append(p.PropertyTypeFullyQualified).Append("> ").Append(p.PropertyName).Append(" => ").Append(p.AccessorClassName).AppendLine(".Instance;");
                    sb.AppendLine();
                }
                sb.Append(baseIndent).AppendLine("}");
                sb.AppendLine();
            }

            // IBindings interface
            sb.Append(baseIndent).AppendLine("/// <summary>");
            sb.Append(baseIndent).AppendLine("/// Exposes bindable properties of this type as <see cref=\"global::XenoAtom.Terminal.UI.Binding{T}\"/> values.");
            sb.Append(baseIndent).AppendLine("/// </summary>");
            sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
            sb.Append(baseIndent).Append(string.IsNullOrEmpty(baseBindings) ? "public interface IBindings" : "public new interface IBindings");
            if (!string.IsNullOrEmpty(baseBindings))
            {
                sb.Append(" : ").Append(baseBindings);
            }
            sb.AppendLine();
            sb.Append(baseIndent).AppendLine("{");
            foreach (var p in properties)
            {
                sb.Append(baseIndent).AppendLine("    /// <summary>");
                sb.Append(baseIndent).Append("    /// Gets a binding representing <c>").Append(p.PropertyName).AppendLine("</c> on this instance.");
                sb.Append(baseIndent).AppendLine("    /// </summary>");
                sb.Append(baseIndent).Append("    global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append("> ").Append(p.PropertyName).AppendLine(" { get; }");
            }
            sb.Append(baseIndent).AppendLine("}");

            // Close type braces
            for (var i = containingTypes.Count - 1; i >= 0; i--)
            {
                var indent = new string(' ', (string.IsNullOrEmpty(ns) ? 0 : 4) + (i * 4));
                sb.Append(indent).AppendLine("}");
            }

            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string FindBaseBindings(INamedTypeSymbol containingType, HashSet<INamedTypeSymbol> typesWithBindings)
        {
            var baseType = containingType.BaseType;
            while (baseType is not null)
            {
                if (typesWithBindings.Contains(baseType) || HasNestedType(baseType, "IBindings"))
                {
                    return $"{baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.IBindings";
                }
                baseType = baseType.BaseType;
            }

            return string.Empty;
        }

        private static string FindBaseAccessor(INamedTypeSymbol containingType, HashSet<INamedTypeSymbol> typesWithAccessors)
        {
            var baseType = containingType.BaseType;
            while (baseType is not null)
            {
                if (typesWithAccessors.Contains(baseType) || HasNestedType(baseType, "Accessor"))
                {
                    return $"{baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Accessor";
                }
                baseType = baseType.BaseType;
            }

            return string.Empty;
        }

        private static bool HasNestedType(INamedTypeSymbol typeSymbol, string nestedTypeName)
        {
            return typeSymbol.GetTypeMembers(nestedTypeName).Length != 0;
        }

        internal static List<INamedTypeSymbol> GetContainingTypes(INamedTypeSymbol type)
        {
            var list = new List<INamedTypeSymbol>();
            var current = type;
            while (current is not null)
            {
                list.Add(current);
                current = current.ContainingType;
            }
            list.Reverse();
            return list;
        }

        internal static string GetTypeKeyword(INamedTypeSymbol type)
        {
            if (type.IsRecord)
            {
                return type.TypeKind == TypeKind.Struct ? "record struct" : "record";
            }

            return type.TypeKind switch
            {
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
                _ => "class",
            };
        }

        internal static void AppendTypeParameters(StringBuilder sb, INamedTypeSymbol type)
        {
            if (type.TypeParameters.Length == 0)
            {
                return;
            }

            sb.Append('<');
            for (var i = 0; i < type.TypeParameters.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(type.TypeParameters[i].Name);
            }
            sb.Append('>');
        }

        internal static void AppendTypeParameterConstraints(StringBuilder sb, INamedTypeSymbol type, string indent)
        {
            var format = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            var wroteAny = false;

            foreach (var tp in type.TypeParameters)
            {
                var first = true;

                void AppendConstraint(string value)
                {
                    if (first)
                    {
                        if (!wroteAny)
                        {
                            sb.Append(' ');
                        }
                        else
                        {
                            sb.AppendLine();
                            sb.Append(indent);
                        }

                        sb.Append("where ").Append(tp.Name).Append(" : ");
                        first = false;
                        wroteAny = true;
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append(value);
                }

                if (tp.HasNotNullConstraint)
                {
                    AppendConstraint("notnull");
                }

                if (tp.HasUnmanagedTypeConstraint)
                {
                    AppendConstraint("unmanaged");
                }
                else if (tp.HasValueTypeConstraint)
                {
                    AppendConstraint("struct");
                }
                else if (tp.HasReferenceTypeConstraint)
                {
                    AppendConstraint(tp.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
                }

                foreach (var c in tp.ConstraintTypes)
                {
                    AppendConstraint(c.ToDisplayString(format));
                }

                if (tp.HasConstructorConstraint)
                {
                    AppendConstraint("new()");
                }
            }

            sb.AppendLine();
        }

        internal static string SanitizeFileName(string fullTypeName)
        {
            if (fullTypeName.StartsWith("global::", StringComparison.Ordinal))
            {
                fullTypeName = fullTypeName.Substring("global::".Length);
            }

            var sb = new StringBuilder(fullTypeName.Length);
            foreach (var ch in fullTypeName)
            {
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }
            return sb.ToString();
        }
    }

    private sealed record FluentPropertyInfo(
        INamedTypeSymbol ContainingType,
        string Namespace,
        string PropertyName,
        string MethodName,
        string MethodArgumentTypeFullyQualified,
        bool IsDelegateProperty,
        bool IsDelegatorProperty,
        bool CanGenerateFluentExtensions)
    {
        public static FluentPropertyResult TryCreate(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.TargetNode is not PropertyDeclarationSyntax propertySyntax)
            {
                return new FluentPropertyResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            if (context.TargetSymbol is not IPropertySymbol propertySymbol)
            {
                return new FluentPropertyResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            var containingType = propertySymbol.ContainingType;
            if (containingType is null)
            {
                return new FluentPropertyResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            var setMethod = propertySymbol.SetMethod;
            if (setMethod is null || setMethod.IsInitOnly)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.FluentPropertyMustBeSettable, propertySyntax.Identifier.GetLocation(), propertySymbol.ToDisplayString()));
                return new FluentPropertyResult(null, diagnostics.ToImmutable());
            }

            string? configuredMethodName = null;
            foreach (var attribute in propertySymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::XenoAtom.Terminal.UI.FluentAttribute")
                {
                    if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        configuredMethodName = s;
                    }
                    break;
                }
            }

            var format = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            var compilation = context.SemanticModel.Compilation;
            var (isDelegatorProperty, delegatorDelegateTypeFullyQualified) =
                TryGetDelegatorDelegateType(compilation, propertySymbol.Type, format);

            var isRawDelegateProperty = propertySymbol.Type.TypeKind == TypeKind.Delegate;
            var isDelegateProperty = isRawDelegateProperty || isDelegatorProperty;

            var propertyName = propertySymbol.Name;
            string resolvedMethodName = configuredMethodName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resolvedMethodName))
            {
                // Raw delegate properties are invocable (obj.Prop(...)), which prevents fluent extension methods
                // of the same name from being used. For those we prefix with "With" by default.
                // Delegator<TDelegate> properties are not invocable, so we can keep the property name.
                resolvedMethodName = isRawDelegateProperty ? $"With{propertyName}" : propertyName;
            }

            var ns = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;

            var info = new FluentPropertyInfo(
                ContainingType: containingType,
                Namespace: ns,
                PropertyName: propertyName,
                MethodName: resolvedMethodName,
                MethodArgumentTypeFullyQualified: isDelegatorProperty ? delegatorDelegateTypeFullyQualified! : propertySymbol.Type.ToDisplayString(format),
                IsDelegateProperty: isDelegateProperty,
                IsDelegatorProperty: isDelegatorProperty,
                CanGenerateFluentExtensions: IsPubliclyVisible(containingType) && propertySymbol.DeclaredAccessibility == Accessibility.Public);

            return new FluentPropertyResult(info, diagnostics.ToImmutable());
        }
    }

    private static class FluentEmitter
    {
        public static void Emit(SourceProductionContext context, ImmutableArray<FluentPropertyResult> items)
        {
            if (items.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var item in items)
            {
                foreach (var diagnostic in item.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            var infos = items
                .Select(static item => item.Info)
                .Where(static info => info is not null)
                .Select(static info => info!)
                .Where(static info => info.CanGenerateFluentExtensions)
                .ToList();

            var grouped = infos
                .GroupBy(static item => item.ContainingType, (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
                .ToList();

            foreach (var group in grouped)
            {
                var containingType = group.Key;
                if (containingType is null)
                {
                    continue;
                }

                var ordered = group.OrderBy(static item => item.MethodName, StringComparer.Ordinal).ToList();
                if (ordered.Count == 0)
                {
                    continue;
                }

                var hintName = $"{SanitizeFileName(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.FluentOnly.g.cs";
                var source = GenerateFluentOnlyExtensionsSource(containingType, ordered);
                context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            }
        }

        private static string GenerateFluentOnlyExtensionsSource(INamedTypeSymbol containingType, List<FluentPropertyInfo> properties)
        {
            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");

            var ns = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append("namespace ").Append(ns).AppendLine();
                sb.AppendLine("{");
            }

            var indent = new string(' ', string.IsNullOrEmpty(ns) ? 0 : 4);
            var extensionClassName = GetExtensionClassName(containingType);

            string receiverTypeXml = DocumentationCommentId.CreateDeclarationId(containingType)
                ?? containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            sb.Append(indent).AppendLine("/// <summary>");
            sb.Append(indent).Append("/// Fluent extension methods for configuring instances of <see cref=\"")
                .Append(receiverTypeXml)
                .AppendLine("\"/>.");
            sb.Append(indent).AppendLine("/// </summary>");
            sb.Append(indent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
            sb.Append(indent).Append("public static partial class ").Append(extensionClassName).AppendLine();
            sb.Append(indent).AppendLine("{");

            var methodIndent = indent + "    ";
            var receiverType = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Use a generic receiver for non-sealed classes to enable fluent chaining on derived types.
            var canUseGenericReceiver = containingType.TypeKind == TypeKind.Class && !containingType.IsSealed;
            var needsTypeParameters = containingType.TypeParameters.Length > 0;

            foreach (var p in properties)
            {
                var propName = p.PropertyName;
                var methodName = p.MethodName;
                var argName = ToLowerCamel(methodName);
                var argType = p.MethodArgumentTypeFullyQualified;

                sb.Append(methodIndent).AppendLine("/// <summary>");
                sb.Append(methodIndent).Append("/// Sets <see cref=\"").Append(receiverTypeXml).Append('.').Append(EscapeIdentifier(propName)).AppendLine("\"/> and returns the same instance.");
                sb.Append(methodIndent).AppendLine("/// </summary>");
                sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                sb.Append(methodIndent).Append("/// <param name=\"").Append(argName).AppendLine("\">The value to set.</param>");
                sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");

                if (canUseGenericReceiver)
                {
                    if (needsTypeParameters)
                    {
                        sb.Append(methodIndent).Append("public static TObj ").Append(EscapeIdentifier(methodName)).Append("<TObj");
                        for (var i = 0; i < containingType.TypeParameters.Length; i++)
                        {
                            sb.Append(", ").Append(containingType.TypeParameters[i].Name);
                        }
                        sb.Append(">(this TObj obj, ").Append(argType).Append(' ').Append(EscapeIdentifier(argName)).Append(')');

                        sb.Append(" where TObj : ").Append(receiverType);
                        AppendTypeParameterConstraints(sb, containingType, methodIndent);
                    }
                    else
                    {
                        sb.Append(methodIndent).Append("public static T ").Append(EscapeIdentifier(methodName)).Append("<T>(this T obj, ").Append(argType).Append(' ').Append(EscapeIdentifier(argName))
                            .Append(") where T : ").Append(receiverType).AppendLine();
                    }

                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).Append(" = ").Append(EscapeIdentifier(argName)).AppendLine(";");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }
                else
                {
                    sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(EscapeIdentifier(methodName));
                    if (needsTypeParameters)
                    {
                        AppendTypeParameters(sb, containingType);
                    }
                    sb.Append("(this ").Append(receiverType).Append(" obj, ").Append(argType).Append(' ').Append(EscapeIdentifier(argName)).Append(')');
                    if (needsTypeParameters)
                    {
                        AppendTypeParameterConstraints(sb, containingType, methodIndent);
                    }
                    else
                    {
                        sb.AppendLine();
                    }

                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).Append("    obj.").Append(EscapeIdentifier(propName)).Append(" = ").Append(EscapeIdentifier(argName)).AppendLine(";");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }

                sb.AppendLine();
            }

            sb.Append(indent).AppendLine("}");

            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GetExtensionClassName(INamedTypeSymbol type)
        {
            var containingTypes = BindableEmitter.GetContainingTypes(type);
            var sb = new StringBuilder();
            for (var i = 0; i < containingTypes.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append('_');
                }
                sb.Append(containingTypes[i].Name);
                if (containingTypes[i].TypeParameters.Length == 0)
                {
                    continue;
                }

                sb.Append('_');
                for (var j = 0; j < containingTypes[i].TypeParameters.Length; j++)
                {
                    if (j > 0) sb.Append('_');
                    sb.Append(containingTypes[i].TypeParameters[j].Name);
                }
            }
            sb.Append("Extensions");
            return sb.ToString();
        }

        private static string EscapeIdentifier(string identifier)
        {
            return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
        }

        private static string ToLowerCamel(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var first = name[0];
            if (char.IsLower(first))
            {
                return name;
            }

            return char.ToLowerInvariant(first) + name.Substring(1);
        }

        private static void AppendTypeParameters(StringBuilder sb, INamedTypeSymbol type)
        {
            if (type.TypeParameters.Length == 0)
            {
                return;
            }

            sb.Append('<');
            for (var i = 0; i < type.TypeParameters.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(type.TypeParameters[i].Name);
            }
            sb.Append('>');
        }

        private static void AppendTypeParameterConstraints(StringBuilder sb, INamedTypeSymbol type, string indent)
        {
            var format = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            var wroteAny = false;

            foreach (var tp in type.TypeParameters)
            {
                var first = true;

                void AppendConstraint(string value)
                {
                    if (first)
                    {
                        if (!wroteAny)
                        {
                            sb.Append(' ');
                        }
                        else
                        {
                            sb.AppendLine();
                            sb.Append(indent);
                        }

                        sb.Append("where ").Append(tp.Name).Append(" : ");
                        first = false;
                        wroteAny = true;
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append(value);
                }

                if (tp.HasNotNullConstraint)
                {
                    AppendConstraint("notnull");
                }

                if (tp.HasUnmanagedTypeConstraint)
                {
                    AppendConstraint("unmanaged");
                }
                else if (tp.HasValueTypeConstraint)
                {
                    AppendConstraint("struct");
                }
                else if (tp.HasReferenceTypeConstraint)
                {
                    AppendConstraint(tp.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
                }

                foreach (var c in tp.ConstraintTypes)
                {
                    AppendConstraint(c.ToDisplayString(format));
                }

                if (tp.HasConstructorConstraint)
                {
                    AppendConstraint("new()");
                }
            }

            sb.AppendLine();
        }

        private static string SanitizeFileName(string fullTypeName)
        {
            if (fullTypeName.StartsWith("global::", StringComparison.Ordinal))
            {
                fullTypeName = fullTypeName.Substring("global::".Length);
            }

            var sb = new StringBuilder(fullTypeName.Length);
            foreach (var ch in fullTypeName)
            {
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }
            return sb.ToString();
        }
    }

    private sealed record RoutedEventMethodInfo(
        INamedTypeSymbol ContainingType,
        string MethodName,
        string EventName,
        string EventArgsTypeFullyQualified,
        string RoutingStrategyExpression)
    {
        public static RoutedEventMethodResult TryCreate(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.TargetNode is not MethodDeclarationSyntax methodSyntax)
            {
                return new RoutedEventMethodResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            if (context.TargetSymbol is not IMethodSymbol methodSymbol)
            {
                return new RoutedEventMethodResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            var containingType = methodSymbol.ContainingType;
            if (containingType is null)
            {
                return new RoutedEventMethodResult(null, ImmutableArray<Diagnostic>.Empty);
            }

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            if (!BindablePropertyInfo.IsPartial(containingType))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, methodSyntax.Identifier.GetLocation(), containingType.ToDisplayString()));
                return new RoutedEventMethodResult(null, diagnostics.ToImmutable());
            }

            if (methodSymbol.ReturnsVoid == false || methodSymbol.Parameters.Length != 1)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.RoutedEventMethodMustHaveSingleArg, methodSyntax.Identifier.GetLocation(), methodSymbol.Name));
                return new RoutedEventMethodResult(null, diagnostics.ToImmutable());
            }

            var eventArgsType = methodSymbol.Parameters[0].Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                    SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            var methodName = methodSymbol.Name;
            var eventName = methodName.StartsWith("On", StringComparison.Ordinal) && methodName.Length > 2 ? methodName.Substring(2) : methodName;

            // Read routing strategy from the attribute, defaulting to Bubble.
            var routingStrategyExpression = "global::XenoAtom.Terminal.UI.RoutingStrategy.Bubble";
            foreach (var attr in methodSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + RoutedEventAttributeMetadataName)
                {
                    if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is not null)
                    {
                        var raw = attr.ConstructorArguments[0].Value;
                        routingStrategyExpression = string.Format(CultureInfo.InvariantCulture, "(global::XenoAtom.Terminal.UI.RoutingStrategy){0}", raw);
                    }
                    break;
                }
            }

            return new RoutedEventMethodResult(
                new RoutedEventMethodInfo(containingType, methodName, eventName, eventArgsType, routingStrategyExpression),
                diagnostics.ToImmutable());
        }
    }

    private sealed record RoutedEventMethodResult(RoutedEventMethodInfo? Info, ImmutableArray<Diagnostic> Diagnostics);

    private static class RoutedEventEmitter
    {
        public static void Emit(SourceProductionContext context, ImmutableArray<RoutedEventMethodResult> items)
        {
            if (items.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var item in items)
            {
                foreach (var diagnostic in item.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            var grouped = items
                .Select(static item => item.Info)
                .Where(static info => info is not null)
                .Select(static info => info!)
                .GroupBy(static item => item.ContainingType, (IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default);

            foreach (var group in grouped)
            {
                var containingType = group.Key;
                if (containingType is null)
                {
                    continue;
                }

                var ordered = group.OrderBy(static item => item.EventName, StringComparer.Ordinal).ToList();
                if (ordered.Count == 0)
                {
                    continue;
                }

                var hintName = $"{SanitizeFileName(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.RoutedEvents.g.cs";
                var source = GenerateRoutedEventsSource(containingType, ordered);
                context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));

                var fluentHintName = $"{SanitizeFileName(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.RoutedEvents.Fluent.g.cs";
                var fluentSource = GenerateRoutedEventFluentExtensionsSource(containingType, ordered);
                context.AddSource(fluentHintName, SourceText.From(fluentSource, Encoding.UTF8));
            }
        }

        private static string GenerateRoutedEventsSource(INamedTypeSymbol containingType, List<RoutedEventMethodInfo> events)
        {
            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");

            var ns = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append("namespace ").Append(ns).AppendLine();
                sb.AppendLine("{");
            }

            var containingTypes = BindableEmitter.GetContainingTypes(containingType);
            for (var i = 0; i < containingTypes.Count; i++)
            {
                var type = containingTypes[i];
                var indent = new string(' ', (string.IsNullOrEmpty(ns) ? 0 : 4) + (i * 4));

                sb.Append(indent);
                sb.Append("partial ").Append(BindableEmitter.GetTypeKeyword(type)).Append(' ').Append(type.Name);
                BindableEmitter.AppendTypeParameters(sb, type);
                sb.AppendLine();
                sb.Append(indent).AppendLine("{");
            }

            var baseIndent = new string(' ', (string.IsNullOrEmpty(ns) ? 0 : 4) + (containingTypes.Count * 4));

            foreach (var ev in events)
            {
                var routedHandlerName = ev.EventName + "Routed";

                sb.Append(baseIndent).AppendLine("/// <summary>");
                sb.Append(baseIndent).Append("/// Identifies the <c>").Append(ev.EventName).AppendLine("</c> routed event.");
                sb.Append(baseIndent).AppendLine("/// </summary>");
                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append("public static readonly global::XenoAtom.Terminal.UI.RoutedEvent<").Append(ev.EventArgsTypeFullyQualified).Append("> ")
                    .Append(ev.EventName).Append("Event = global::XenoAtom.Terminal.UI.RoutedEvent.Register<")
                    .Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(", ").Append(ev.EventArgsTypeFullyQualified).Append(">(")
                    .Append('"').Append(ev.EventName).Append("\", ")
                    .Append("static (sender, args) => (sender as ").Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(")?.").Append(ev.MethodName).Append("(args), ")
                    .Append(ev.RoutingStrategyExpression)
                    .AppendLine(");");
                sb.AppendLine();

                sb.Append(baseIndent).AppendLine("/// <summary>");
                sb.Append(baseIndent).Append("/// Occurs when the <c>").Append(ev.EventName).AppendLine("</c> routed event is raised on this instance.");
                sb.Append(baseIndent).AppendLine("/// </summary>");
                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append("public event global::System.EventHandler<").Append(ev.EventArgsTypeFullyQualified).Append("> ").Append(routedHandlerName).AppendLine();
                sb.Append(baseIndent).AppendLine("{");
                sb.Append(baseIndent).Append("    add => AddHandler(").Append(ev.EventName).AppendLine("Event, value);");
                sb.Append(baseIndent).Append("    remove => RemoveHandler(").Append(ev.EventName).AppendLine("Event, value);");
                sb.Append(baseIndent).AppendLine("}");
                sb.AppendLine();
            }

            for (var i = containingTypes.Count - 1; i >= 0; i--)
            {
                var indent = new string(' ', (string.IsNullOrEmpty(ns) ? 0 : 4) + (i * 4));
                sb.Append(indent).AppendLine("}");
            }

            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GenerateRoutedEventFluentExtensionsSource(INamedTypeSymbol containingType, List<RoutedEventMethodInfo> events)
        {
            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");

            var ns = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append("namespace ").Append(ns).AppendLine();
                sb.AppendLine("{");
            }

            var indent = new string(' ', string.IsNullOrEmpty(ns) ? 0 : 4);
            var extensionClassName = GetExtensionClassName(containingType);
            var receiverTypeXml = DocumentationCommentId.CreateDeclarationId(containingType)
                ?? containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            sb.Append(indent).AppendLine("/// <summary>");
            sb.Append(indent).Append("/// Fluent extension methods for registering routed event handlers on <see cref=\"")
                .Append(receiverTypeXml)
                .AppendLine("\"/>.");
            sb.Append(indent).AppendLine("/// </summary>");
            sb.Append(indent).Append("public static partial class ").Append(extensionClassName).AppendLine();
            sb.Append(indent).AppendLine("{");

            var methodIndent = indent + "    ";
            var receiverType = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var canUseGeneric = containingType.TypeKind == TypeKind.Class && !containingType.IsSealed && containingType.TypeParameters.Length == 0;
            var needsTypeParameters = containingType.TypeParameters.Length > 0;

            foreach (var ev in events)
            {
                var eventName = ev.EventName;
                var argsType = ev.EventArgsTypeFullyQualified;
                var routedHandlerName = eventName + "Routed";
                var escapedRoutedHandlerName = EscapeIdentifier(routedHandlerName);
                var escapedEventName = EscapeIdentifier(eventName);

                sb.Append(methodIndent).AppendLine("/// <summary>");
                sb.Append(methodIndent).Append("/// Adds a handler for the <c>").Append(escapedEventName).AppendLine("</c> routed event and returns the same instance.");
                sb.Append(methodIndent).AppendLine("/// </summary>");
                sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                sb.Append(methodIndent).AppendLine("/// <param name=\"handler\">The event handler to add.</param>");
                sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");

                if (canUseGeneric)
                {
                    sb.Append(methodIndent).Append("public static T ").Append(escapedEventName).Append("<T>(this T obj, global::System.EventHandler<").Append(argsType).Append("> handler) where T : ").Append(receiverType).AppendLine();
                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(handler);");
                    sb.Append(methodIndent).Append("    obj.").Append(escapedRoutedHandlerName).AppendLine(" += handler;");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }
                else
                {
                    sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(escapedEventName);
                    if (needsTypeParameters)
                    {
                        BindableEmitter.AppendTypeParameters(sb, containingType);
                    }
                    sb.Append("(this ").Append(receiverType).Append(" obj, global::System.EventHandler<").Append(argsType).Append("> handler)");
                    if (needsTypeParameters)
                    {
                        BindableEmitter.AppendTypeParameterConstraints(sb, containingType, methodIndent);
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(handler);");
                    sb.Append(methodIndent).Append("    obj.").Append(escapedRoutedHandlerName).AppendLine(" += handler;");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }
                sb.AppendLine();

                sb.Append(methodIndent).AppendLine("/// <summary>");
                sb.Append(methodIndent).Append("/// Adds a handler for the <c>").Append(escapedEventName).AppendLine("</c> routed event and returns the same instance.");
                sb.Append(methodIndent).AppendLine("/// </summary>");
                sb.Append(methodIndent).AppendLine("/// <param name=\"obj\">The instance to configure.</param>");
                sb.Append(methodIndent).AppendLine("/// <param name=\"handler\">A callback invoked when the event is raised.</param>");
                sb.Append(methodIndent).AppendLine("/// <returns>The same instance for chaining.</returns>");
                sb.Append(methodIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                if (canUseGeneric)
                {
                    sb.Append(methodIndent).Append("public static T ").Append(escapedEventName).Append("<T>(this T obj, global::System.Action handler) where T : ").Append(receiverType).AppendLine();
                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(handler);");
                    sb.Append(methodIndent).Append("    obj.").Append(escapedRoutedHandlerName).AppendLine(" += (_, __) => handler();");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }
                else
                {
                    sb.Append(methodIndent).Append("public static ").Append(receiverType).Append(' ').Append(escapedEventName);
                    if (needsTypeParameters)
                    {
                        BindableEmitter.AppendTypeParameters(sb, containingType);
                    }
                    sb.Append("(this ").Append(receiverType).Append(" obj, global::System.Action handler)");
                    if (needsTypeParameters)
                    {
                        BindableEmitter.AppendTypeParameterConstraints(sb, containingType, methodIndent);
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                    sb.Append(methodIndent).AppendLine("{");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(obj);");
                    sb.Append(methodIndent).AppendLine("    global::System.ArgumentNullException.ThrowIfNull(handler);");
                    sb.Append(methodIndent).Append("    obj.").Append(escapedRoutedHandlerName).AppendLine(" += (_, __) => handler();");
                    sb.Append(methodIndent).AppendLine("    return obj;");
                    sb.Append(methodIndent).AppendLine("}");
                }
                sb.AppendLine();
            }

            sb.Append(indent).AppendLine("}");

            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GetExtensionClassName(INamedTypeSymbol type)
        {
            var containingTypes = BindableEmitter.GetContainingTypes(type);
            var sb = new StringBuilder();
            for (var i = 0; i < containingTypes.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append('_');
                }
                sb.Append(containingTypes[i].Name);
                if (containingTypes[i].TypeParameters.Length == 0)
                {
                    continue;
                }

                sb.Append('_');
                for (var j = 0; j < containingTypes[i].TypeParameters.Length; j++)
                {
                    if (j > 0) sb.Append('_');
                    sb.Append(containingTypes[i].TypeParameters[j].Name);
                }
            }
            sb.Append("Extensions");
            return sb.ToString();
        }

        private static string EscapeIdentifier(string name)
        {
            return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
                ? "@" + name
                : name;
        }

        private static string SanitizeFileName(string fullTypeName) => BindableEmitter.SanitizeFileName(fullTypeName);
    }

    private static string ToLowerCamel(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (name.Length == 1)
        {
            return name.ToLowerInvariant();
        }

        return char.ToLower(name[0], CultureInfo.InvariantCulture) + name.Substring(1);
    }
}
