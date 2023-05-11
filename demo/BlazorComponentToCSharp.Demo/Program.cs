using BlazorComponentToCSharp;
using BlazorComponentDemoLibrary;

using Spectre.Console;

using XpoRecords.Demo.Helpers;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

MSBuildLocator.RegisterDefaults();

AnsiConsole.WriteLine("Hello, World!");

var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
var solutionPath = $"../{nameof(BlazorComponentDemoLibrary)}/{nameof(BlazorComponentDemoLibrary)}.csproj";

using var workspace = MSBuildWorkspace.Create();

// Print message for WorkspaceFailed event to help diagnosing project load failures.
workspace.WorkspaceFailed += (o, e) => AnsiConsole.MarkupLineInterpolated($"[red]{e.Diagnostic.Message}[/]");

AnsiConsole.MarkupLineInterpolated($"Loading solution '{solutionPath}'");

// Attach progress reporter so we print projects as they are loaded.
var solution = await workspace.OpenProjectAsync(solutionPath, new ConsoleProgressReporter());
Console.WriteLine($"Finished loading solution '{solutionPath}'");

// TODO: Do analysis on the projects in the loaded solution



var razorFilePath = Path.Combine(directory, $"{nameof(Component1)}.razor");
var razorFileContent = File.ReadAllText(razorFilePath);

WriteFileContent($"Razor File Content:", razorFileContent, "razor");

var csharpFileContent = BlazorComponentToCSharpEngine.BlazorComponentToCSharp(razorFilePath, razorFileContent);

AnsiConsole.WriteLine();

WriteFileContent($"Charp File Content:", csharpFileContent);

static void WriteFileContent(string caption, string content, string lang = "cs")
{
    AnsiConsole.WriteLine(caption);
    AnsiConsole.WriteLine(new string('-', Console.WindowWidth));
    ConsoleHelper.PrintSource(content, lang);
    AnsiConsole.WriteLine(new string('-', Console.WindowWidth));
}

internal class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
{
    public void Report(ProjectLoadProgress loadProgress)
    {
        var projectDisplay = Path.GetFileName(loadProgress.FilePath);
        if (loadProgress.TargetFramework != null)
        {
            projectDisplay += $" ({loadProgress.TargetFramework})";
        }

        AnsiConsole.MarkupLineInterpolated($"[white]{loadProgress.Operation,-15}[/] [lightgreen]{loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff}[/] [gray]{projectDisplay}[/]");
    }
}
