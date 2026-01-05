// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace XenoAtom.Terminal.UI.SourceGen;

[Generator(LanguageNames.CSharp)]
public sealed partial class TerminalUiGenerator : IIncrementalGenerator
{
    private const string BindableAttributeMetadataName = "XenoAtom.Terminal.UI.BindableAttribute";
    private const string RoutedEventAttributeMetadataName = "XenoAtom.Terminal.UI.RoutedEventAttribute";

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

        public static readonly DiagnosticDescriptor RoutedEventMethodMustHaveSingleArg = new(
            id: "XATUI003",
            title: "Routed event method signature is invalid",
            messageFormat: "The routed event method '{0}' must return void and take exactly one parameter",
            category: "XenoAtom.Terminal.UI.SourceGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }

    private sealed record BindablePropertyInfo(
        INamedTypeSymbol ContainingType,
        string Namespace,
        string ContainingTypeDisplayName,
        string PropertyName,
        string PropertyTypeFullyQualified,
        string PropertyModifiers,
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

            if (!IsPartialAutoPropertyDeclaration(propertySyntax))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.BindablePropertyMustBePartialDeclaration, propertySyntax.Identifier.GetLocation(), propertySymbol.Name));
                return new BindablePropertyResult(null, diagnostics.ToImmutable());
            }

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
            var propertyTypeFullyQualified = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var propertyName = propertySymbol.Name;
            var backingFieldName = "_" + ToLowerCamel(propertyName);
            var accessorClassName = $"__{propertyName}__BindingAccessor";

            var modifiers = string.Join(" ", propertySyntax.Modifiers.Select(m => m.Text));

            return new BindablePropertyResult(new BindablePropertyInfo(
                ContainingType: containingType,
                Namespace: ns,
                ContainingTypeDisplayName: containingTypeDisplayName,
                PropertyName: propertyName,
                PropertyTypeFullyQualified: propertyTypeFullyQualified,
                PropertyModifiers: modifiers,
                BackingFieldName: backingFieldName,
                AccessorClassName: accessorClassName), diagnostics.ToImmutable());
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

    private sealed record BindablePropertyResult(BindablePropertyInfo? Info, ImmutableArray<Diagnostic> Diagnostics);

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

                var ordered = group.OrderBy(static item => item.PropertyName, StringComparer.Ordinal).ToList();
                if (ordered.Count == 0)
                {
                    continue;
                }

                var hintName = $"{SanitizeFileName(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.Bindings.g.cs";
                var source = GenerateBindingsSource(containingType, ordered);
                context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            }
        }

        private static string GenerateBindingsSource(INamedTypeSymbol containingType, List<BindablePropertyInfo> properties)
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
                    sb.Append(" : ").Append(type.Name).Append(".IBindings");
                }
                sb.AppendLine();
                sb.Append(indent).AppendLine("{");
            }

            var baseIndent = new string(' ', (string.IsNullOrEmpty(ns) ? 0 : 4) + (containingTypes.Count * 4));

            // @ref property
            sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
            sb.Append(baseIndent).AppendLine("public IBindings @ref => this;");
            sb.AppendLine();

            // Backing fields
            foreach (var p in properties)
            {
                sb.Append(baseIndent).AppendLine("[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]");
                sb.Append(baseIndent).Append("private ").Append(p.PropertyTypeFullyQualified).Append(' ').Append(p.BackingFieldName).AppendLine(";");
                sb.AppendLine();
            }

            // Generated property implementations + accessors + explicit interface impl
            foreach (var p in properties)
            {
                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append(p.PropertyModifiers).Append(' ').Append(p.PropertyTypeFullyQualified).Append(' ').Append(p.PropertyName).AppendLine();
                sb.Append(baseIndent).AppendLine("{");
                sb.Append(baseIndent).Append("    get => global::XenoAtom.Terminal.UI.BindingManager.Current.GetValue(ref ").Append(p.BackingFieldName).Append(", ").Append(p.AccessorClassName).AppendLine(".Instance);");
                sb.Append(baseIndent).Append("    set => global::XenoAtom.Terminal.UI.BindingManager.Current.SetValue(ref ").Append(p.BackingFieldName).Append(", value, ").Append(p.AccessorClassName).AppendLine(".Instance);");
                sb.Append(baseIndent).AppendLine("}");
                sb.AppendLine();

                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append("global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append("> IBindings.").Append(p.PropertyName)
                    .Append(" => new global::XenoAtom.Terminal.UI.Binding<").Append(p.PropertyTypeFullyQualified).Append(">(this, ").Append(p.AccessorClassName).AppendLine(".Instance);");
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

            // IBindings interface
            var baseBindings = FindBaseBindings(containingType);
            sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
            sb.Append(baseIndent).Append("public interface IBindings : ").Append(baseBindings).AppendLine();
            sb.Append(baseIndent).AppendLine("{");
            foreach (var p in properties)
            {
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

        private static string FindBaseBindings(INamedTypeSymbol containingType)
        {
            var baseType = containingType.BaseType;
            while (baseType is not null)
            {
                foreach (var nested in baseType.GetTypeMembers("IBindings"))
                {
                    if (nested.TypeKind == TypeKind.Interface)
                    {
                        return $"{baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.IBindings";
                    }
                }
                baseType = baseType.BaseType;
            }

            return "global::XenoAtom.Terminal.UI.BindableObject.IBindings";
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

            var eventArgsType = methodSymbol.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append("public static readonly global::XenoAtom.Terminal.UI.RoutedEvent<").Append(ev.EventArgsTypeFullyQualified).Append("> ")
                    .Append(ev.EventName).Append("Event = global::XenoAtom.Terminal.UI.RoutedEvent.Register<")
                    .Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(", ").Append(ev.EventArgsTypeFullyQualified).Append(">(")
                    .Append("nameof(").Append(ev.EventName).Append("), ")
                    .Append("static (sender, args) => (sender as ").Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(")?.").Append(ev.MethodName).Append("(args), ")
                    .Append(ev.RoutingStrategyExpression)
                    .AppendLine(");");
                sb.AppendLine();

                sb.Append(baseIndent).AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"XenoAtom.Terminal.UI.SourceGen\", \"0.1.0\")]");
                sb.Append(baseIndent).Append("public event global::System.EventHandler<").Append(ev.EventArgsTypeFullyQualified).Append("> ").Append(ev.EventName).AppendLine();
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
