using BlazorComponentToCSharp;
using BlazorComponentDemoLibrary;

using Spectre.Console;

using XpoRecords.Demo.Helpers;

AnsiConsole.WriteLine("Hello, World!");

var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location);

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
