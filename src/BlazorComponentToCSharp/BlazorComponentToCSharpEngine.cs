using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace BlazorComponentToCSharp;

public class BlazorComponentToCSharpEngine
{
    public static string BlazorComponentToCSharp(string filePath, string fileContent)
    {

        return "";
    }

    private static (RazorSourceGenerationOptions?, Diagnostic?) ComputeRazorSourceGeneratorOptions((AnalyzerConfigOptionsProvider, ParseOptions) pair, CancellationToken ct)
    {
        //Log.ComputeRazorSourceGeneratorOptions();

        var (options, parseOptions) = pair;
        var globalOptions = options.GlobalOptions;

        globalOptions.TryGetValue("build_property.RazorConfiguration", out var configurationName);
        globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        globalOptions.TryGetValue("build_property.SupportLocalizedComponentNames", out var supportLocalizedComponentNames);
        globalOptions.TryGetValue("build_property.GenerateRazorMetadataSourceChecksumAttributes", out var generateMetadataSourceChecksumAttributes);

        var razorLanguageVersion = RazorLanguageVersion.Latest;
        Diagnostic? diagnostic = null;
        if (!globalOptions.TryGetValue("build_property.RazorLangVersion", out var razorLanguageVersionString) ||
            !RazorLanguageVersion.TryParse(razorLanguageVersionString, out razorLanguageVersion))
        {
            diagnostic = Diagnostic.Create(
                RazorDiagnostics.InvalidRazorLangVersionDescriptor,
                Location.None,
                razorLanguageVersionString);
        }

        var razorConfiguration = RazorConfiguration.Create(razorLanguageVersion, configurationName ?? "default", System.Linq.Enumerable.Empty<RazorExtension>(), true);

        var razorSourceGenerationOptions = new RazorSourceGenerationOptions()
        {
            Configuration = razorConfiguration,
            GenerateMetadataSourceChecksumAttributes = generateMetadataSourceChecksumAttributes == "true",
            RootNamespace = rootNamespace ?? "ASP",
            SupportLocalizedComponentNames = supportLocalizedComponentNames == "true",
            CSharpLanguageVersion = ((CSharpParseOptions)parseOptions).LanguageVersion,
        };

        return (razorSourceGenerationOptions, diagnostic);
    }

    private static SourceGeneratorProjectEngine GetGenerationProjectEngine(
           SourceGeneratorProjectItem item,
           IEnumerable<SourceGeneratorProjectItem> imports,
           RazorSourceGenerationOptions razorSourceGeneratorOptions)
    {
        var fileSystem = new VirtualRazorProjectFileSystem();
        fileSystem.Add(item);
        foreach (var import in imports)
        {
            fileSystem.Add(import);
        }

        var projectEngine = (DefaultRazorProjectEngine)RazorProjectEngine.Create(razorSourceGeneratorOptions.Configuration, fileSystem, b =>
        {
            b.Features.Add(new DefaultTypeNameFeature());
            b.SetRootNamespace(razorSourceGeneratorOptions.RootNamespace);

            b.Features.Add(new ConfigureRazorCodeGenerationOptions(options =>
            {
                options.SuppressMetadataSourceChecksumAttributes = !razorSourceGeneratorOptions.GenerateMetadataSourceChecksumAttributes;
                options.SupportLocalizedComponentNames = razorSourceGeneratorOptions.SupportLocalizedComponentNames;
            }));

            CompilerFeatures.Register(b);
            RazorExtensions.Register(b);

            b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpLanguageVersion);
        });

        return new SourceGeneratorProjectEngine(projectEngine);
    }
}
