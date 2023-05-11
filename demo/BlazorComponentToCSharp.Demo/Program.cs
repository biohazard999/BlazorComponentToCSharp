// See https://aka.ms/new-console-template for more information
using BlazorComponentDemoLibrary;

using BlazorComponentToCSharp;

Console.WriteLine("Hello, World!");

var razorFilePath = $"../{nameof(BlazorComponentDemoLibrary)}/{nameof(Component1)}.razor";
var razorFileContent = File.ReadAllText(razorFilePath);

WriteFileContent($"Razor File Content:", razorFileContent);

var csharpFileContent = BlazorComponentToCSharpEngine.BlazorComponentToCSharp(razorFilePath, razorFileContent);

Console.WriteLine();

WriteFileContent($"Charp File Content:", csharpFileContent);

static void WriteFileContent(string caption, string content)
{
    Console.WriteLine(caption);
    Console.WriteLine(new string('-', Console.WindowWidth));
    Console.WriteLine(content);
    Console.WriteLine(new string('-', Console.WindowWidth));
}