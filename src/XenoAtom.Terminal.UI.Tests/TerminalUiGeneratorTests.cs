// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XenoAtom.Terminal.UI.SourceGen;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalUiGeneratorTests
{
    [TestMethod]
    public void Generates_Bindings_And_RoutedEvents()
    {
        const string source = """
                              using XenoAtom.Terminal.UI;
                              
                              namespace Demo;
                              
                              public partial class MyControl : Visual
                              {
                                  [Bindable]
                                  public partial int Count { get; set; }

                                  [Bindable]
                                  public partial Visual? Content { get; set; }

                                  private string _title;

                                  [Bindable]
                                  public string Title
                                  {
                                      get => _title;
                                      set => _title = value;
                                  }
                              
                                  [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
                                  protected virtual void OnPointerPressed(PointerPressedEventArgs e)
                                  {
                                  }
                              }
                              
                              public sealed class PointerPressedEventArgs : RoutedEventArgs
                              {
                              }
                              """;

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CreateCompilation(source, parseOptions);
        var generator = new TerminalUiGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        var compilationDiagnostics = outputCompilation.GetDiagnostics();
        var errors = generatorDiagnostics
            .Concat(compilationDiagnostics)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors.Select(x => x.ToString())));

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => s.SourceText.ToString())
            .ToList();

        Assert.IsTrue(generatedSources.Any(s => s.Contains("MyControl", StringComparison.Ordinal)), "Expected generated sources for MyControl.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("IBindings", StringComparison.Ordinal)), "Expected generated IBindings interface.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("BindingManager.Current.GetValue(this", StringComparison.Ordinal)), "Expected generated binding accessors.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("return global::XenoAtom.Terminal.UI.BindingManager.Current.GetValue(this, ref _content, __Content__BindingAccessor.Instance);", StringComparison.Ordinal)), "Expected generated Visual bindable getter tracking.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("AttachChild(updated)", StringComparison.Ordinal)), "Expected generated Visual bindable setter child attachment.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("NotifyValueChanged(this, __Content__BindingAccessor.Instance)", StringComparison.Ordinal)), "Expected generated Visual bindable setter notifications.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("public void BindCount(", StringComparison.Ordinal)), "Expected generated Bind* method for bidirectional bindings.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("RegisterBoundValue(this, __Count__BindingAccessor.Instance, binding", StringComparison.Ordinal)), "Expected generated binding synchronization registration.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("__ApplyBoundCount(binding.GetValue())", StringComparison.Ordinal)), "Expected generated bind attach to synchronize the local value immediately.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("__Title__BindingAccessor", StringComparison.Ordinal)), "Expected generated accessor for custom bindable property.");
        Assert.IsFalse(generatedSources.Any(s => s.Contains("private global::System.String _title", StringComparison.Ordinal)), "Did not expect generated backing field for custom bindable property.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("PointerPressedEvent", StringComparison.Ordinal)), "Expected generated routed event field.");
        Assert.IsTrue(generatedSources.Any(s => s.Contains("Count<T>(this T obj, global::XenoAtom.Terminal.UI.Binding<int>", StringComparison.Ordinal)), "Expected generated fluent overloads for Binding<T>.");
        Assert.IsFalse(generatedSources.Any(s => s.Contains("Count<T>(this T obj, global::XenoAtom.Terminal.UI.State<", StringComparison.Ordinal)), "Did not expect generated fluent overloads for State<T>.");
    }

    [TestMethod]
    public void Generates_Public_Accessor_Class_For_Bindable_Models()
    {
        const string source = """
                              using XenoAtom.Terminal.UI;
                              
                              namespace Demo;
                              
                              public partial class BaseRow
                              {
                                  [Bindable]
                                  public partial int Id { get; set; }
                              }
                              
                              public partial class DerivedRow : BaseRow
                              {
                                  [Bindable]
                                  public partial string Name { get; set; }
                              }
                              """;

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CreateCompilation(source, parseOptions);
        var generator = new TerminalUiGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        var compilationDiagnostics = outputCompilation.GetDiagnostics();
        var errors = generatorDiagnostics
            .Concat(compilationDiagnostics)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors.Select(x => x.ToString())));

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => s.SourceText.ToString())
            .ToList();

        Assert.IsTrue(
            generatedSources.Any(s => s.Contains("public class Accessor", StringComparison.Ordinal) &&
                                      s.Contains("public static global::XenoAtom.Terminal.UI.BindingAccessor<int> Id => __Id__BindingAccessor.Instance;", StringComparison.Ordinal)),
            "Expected generated model Accessor for BaseRow.");

        Assert.IsTrue(
            generatedSources.Any(s => s.Contains("public new class Accessor : global::Demo.BaseRow.Accessor", StringComparison.Ordinal) &&
                                      s.Contains("public static global::XenoAtom.Terminal.UI.BindingAccessor<string> Name => __Name__BindingAccessor.Instance;", StringComparison.Ordinal)),
            "Expected generated model Accessor for DerivedRow with correct inheritance.");
    }

    [TestMethod]
    public void Generates_New_Accessor_And_IBindings_For_Derived_Types_From_Referenced_Assemblies()
    {
        const string source = """
                              using XenoAtom.Terminal.UI;

                              namespace Demo;

                              public partial class DerivedVisual : Visual
                              {
                                  [Bindable]
                                  public partial int Count { get; set; }
                              }
                              """;

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CreateCompilation(source, parseOptions);
        var generator = new TerminalUiGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        var diagnostics = generatorDiagnostics.Concat(outputCompilation.GetDiagnostics()).ToArray();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors.Select(x => x.ToString())));

        var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToArray();
        Assert.IsFalse(
            warnings.Any(d => d.Id == "CS0108"),
            "Did not expect generated members to hide Visual.Accessor or Visual.IBindings without the new keyword.");

        var generatedSources = driver.GetRunResult().Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => s.SourceText.ToString())
            .ToList();

        Assert.IsTrue(
            generatedSources.Any(s => s.Contains("public new class Accessor : global::XenoAtom.Terminal.UI.Visual.Accessor", StringComparison.Ordinal)),
            "Expected derived generated Accessor to hide Visual.Accessor with the new keyword.");

        Assert.IsTrue(
            generatedSources.Any(s => s.Contains("public new interface IBindings : global::XenoAtom.Terminal.UI.Visual.IBindings", StringComparison.Ordinal)),
            "Expected derived generated IBindings to hide Visual.IBindings with the new keyword.");
    }

    private static CSharpCompilation CreateCompilation(string source, CSharpParseOptions parseOptions)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var references = new List<MetadataReference>();
        references.AddRange(GetTrustedPlatformAssembliesReferences());
        references.Add(MetadataReference.CreateFromFile(typeof(BindableAttribute).Assembly.Location));

        return CSharpCompilation.Create(
            "TerminalUiGeneratorTests.DynamicCompilation",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static IEnumerable<MetadataReference> GetTrustedPlatformAssembliesReferences()
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.IsNotNull(tpa, "TRUSTED_PLATFORM_ASSEMBLIES is required to build the Roslyn test compilation.");

        return tpa
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path));
    }
}
