using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazorComponentToCSharp;

public class BlazorComponentToCSharpEngine(
    AnalyzerConfigOptionsProvider options,
    ParseOptions parseOptions
)
{
    private readonly AnalyzerConfigOptionsProvider analyzerConfigOptions
        = options ?? throw new ArgumentNullException(nameof(options));

    private readonly ParseOptions parseOptions
        = parseOptions ?? throw new ArgumentNullException(nameof(parseOptions));

    public string BlazorComponentToCSharp(
        AdditionalText additionalText
    )
    {
        var (projectItem, _) = ComputeProjectItems(additionalText, analyzerConfigOptions);

        var (razorOptions, _) = ComputeRazorSourceGeneratorOptions(analyzerConfigOptions, parseOptions);

        var generator = GetGenerationProjectEngine(
            projectItem!,
            Enumerable.Empty<SourceGeneratorProjectItem>(),
            razorOptions!
        );


        var codeGen = generator.Process(projectItem!);

        var result = codeGen.GetCSharpDocument().GeneratedCode;

        return result;
    }

    private static (SourceGeneratorProjectItem?, Diagnostic?) ComputeProjectItems((AdditionalText, AnalyzerConfigOptionsProvider) pair, CancellationToken ct)
    {
        var (additionalText, globalOptions) = pair;
        return ComputeProjectItems(additionalText, globalOptions);
    }

    private static (SourceGeneratorProjectItem?, Diagnostic?) ComputeProjectItems(AdditionalText additionalText, AnalyzerConfigOptionsProvider globalOptions)
    {
        var options = globalOptions.GetOptions(additionalText);
        return ComputeProjectItems(additionalText, options);
    }

    private static (SourceGeneratorProjectItem?, Diagnostic?) ComputeProjectItems(AdditionalText additionalText, AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue("build_metadata.AdditionalFiles.TargetPath", out var encodedRelativePath) ||
                    string.IsNullOrWhiteSpace(encodedRelativePath))
        {
            var diagnostic = Diagnostic.Create(
                RazorDiagnostics.TargetPathNotProvided,
                Location.None,
                additionalText.Path);
            return (null, diagnostic);
        }

        options.TryGetValue("build_metadata.AdditionalFiles.CssScope", out var cssScope);
        var relativePath = Encoding.UTF8.GetString(Convert.FromBase64String(encodedRelativePath));

        var projectItem = new SourceGeneratorProjectItem(
            basePath: "/",
            filePath: '/' + relativePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace("//", "/"),
            relativePhysicalPath: relativePath,
            fileKind: additionalText.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ? FileKinds.Component : FileKinds.Legacy,
            additionalText: additionalText,
            cssScope: cssScope);
        return (projectItem, null);
    }

    private static (RazorSourceGenerationOptions?, Diagnostic?) ComputeRazorSourceGeneratorOptions((AnalyzerConfigOptionsProvider, ParseOptions) pair, CancellationToken ct)
    {
        //Log.ComputeRazorSourceGeneratorOptions();

        var (options, parseOptions) = pair;

        return ComputeRazorSourceGeneratorOptions(options, parseOptions);
    }

    private static (RazorSourceGenerationOptions?, Diagnostic?) ComputeRazorSourceGeneratorOptions(AnalyzerConfigOptionsProvider options, ParseOptions parseOptions)
    {
        var globalOptions = options.GlobalOptions;
        return ComputeRazorSourceGeneratorOptions(globalOptions, parseOptions);
    }

    private static (RazorSourceGenerationOptions?, Diagnostic?) ComputeRazorSourceGeneratorOptions(AnalyzerConfigOptions globalOptions, ParseOptions parseOptions)
    {
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

    //TODO: Imports
    //private void GetImportFiles(IEnumerable<Add>)
    //{
    //    var importFiles = sourceItems.Where(static file =>
    //    {
    //        var path = file.FilePath;
    //        if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
    //        {
    //            var fileName = Path.GetFileNameWithoutExtension(path);
    //            return string.Equals(fileName, "_Imports", StringComparison.OrdinalIgnoreCase);
    //        }
    //        else if (path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
    //        {
    //            var fileName = Path.GetFileNameWithoutExtension(path);
    //            return string.Equals(fileName, "_ViewImports", StringComparison.OrdinalIgnoreCase);
    //        }

    //        return false;
    //    });
    //}

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
