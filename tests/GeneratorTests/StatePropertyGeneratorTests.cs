using System;
using System.Linq;
using LiveChartsGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GeneratorTests;

// Drives the LiveCharts source generator over a small snippet and inspects its output. Pins the
// [StateProperty] contract: it registers a PropertyDefinition with NO motion (so a visual state can
// target it) and leaves the property's accessors alone — while [MotionProperty] still emits its
// motion backing. Asserting on the generated text keeps this isolated from the rest of the library.
[TestClass]
public class StatePropertyGeneratorTests
{
    private const string Source = @"
using LiveChartsCore.Generators;
using LiveChartsCore.Painting;

namespace TestNs;

public partial class MyVisual : LiveChartsCore.Drawing.Animatable
{
    [StateProperty]
    public Paint? Fill { get; set; }

    [MotionProperty]
    public partial float Opacity { get; set; }
}
";

    private static CSharpCompilation BuildCompilation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            Source, new CSharpParseOptions(LanguageVersion.Preview));

        // the framework reference set, plus LiveChartsCore explicitly (so the snippet's attributes
        // and base types resolve and ForAttributeWithMetadataName fires).
        var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(System.IO.Path.PathSeparator);

        var paths = tpa
            .Append(typeof(LiveChartsCore.Drawing.Animatable).Assembly.Location)
            .Append(typeof(LiveChartsCore.Generators.StatePropertyAttribute).Assembly.Location)
            .Append(typeof(LiveChartsCore.Painting.Paint).Assembly.Location)
            .Where(p => !string.IsNullOrEmpty(p) && System.IO.File.Exists(p))
            .Distinct();

        return CSharpCompilation.Create(
            "GeneratorTestAssembly",
            [syntaxTree],
            paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static System.Collections.Immutable.ImmutableArray<GeneratedSourceResult> RunGenerator()
    {
        var driver = CSharpGeneratorDriver.Create(new XamlFriendlyObjectsGenerator());
        var result = driver.RunGenerators(BuildCompilation()).GetRunResult();
        return result.Results.Single().GeneratedSources;
    }

    private static string Source_(System.Collections.Immutable.ImmutableArray<GeneratedSourceResult> sources, string hintContains) =>
        sources.Single(s => s.HintName.Contains(hintContains)).SourceText.ToString();

    [TestMethod]
    public void StateProperty_EmitsMotionlessDefinition_WithNoMotionBacking()
    {
        var sources = RunGenerator();
        var fill = Source_(sources, "MyVisual.Fill");

        // a PropertyDefinition is emitted (the nullable reference type is sanitized to Paint)...
        StringAssert.Contains(fill, "FillProperty = new(");
        StringAssert.Contains(fill, "typeof(LiveChartsCore.Painting.Paint)");
        // ...with no motion (the getter for the motion returns null)...
        StringAssert.Contains(fill, "null);");
        // ...and crucially no motion backing field is generated.
        Assert.IsFalse(
            fill.Contains("MotionProperty"),
            "a [StateProperty] must not generate a motion backing field.");
    }

    [TestMethod]
    public void StateProperty_IsRegisteredInThePropertyDefinitions()
    {
        var sources = RunGenerator();
        var classFile = Source_(sources, "MyVisual.g.cs");

        StringAssert.Contains(classFile, "[\"Fill\"] = FillProperty");
    }

    [TestMethod]
    public void MotionProperty_StillGeneratesItsMotionBacking_AlongsideStateProperties()
    {
        var sources = RunGenerator();
        var opacity = Source_(sources, "MyVisual.Opacity");
        var classFile = Source_(sources, "MyVisual.g.cs");

        // the [MotionProperty] in the same type is unaffected by the combined grouping.
        StringAssert.Contains(opacity, "_OpacityMotionProperty");
        StringAssert.Contains(opacity, "OpacityProperty = new(");
        StringAssert.Contains(classFile, "[\"Opacity\"] = OpacityProperty");
    }
}
