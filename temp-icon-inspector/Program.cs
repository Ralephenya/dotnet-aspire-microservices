using System.Reflection;
using System.Runtime.Loader;

var basePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".nuget", "packages",
    "microsoft.fluentui.aspnetcore.components.icons", "4.14.2", "lib", "net8.0");

var filledPath = Path.Combine(basePath, "Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.dll");
var regularPath = Path.Combine(basePath, "Microsoft.FluentUI.AspNetCore.Components.Icons.Regular.dll");
var mainPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".nuget", "packages",
    "microsoft.fluentui.aspnetcore.components", "4.14.2", "lib", "net8.0",
    "Microsoft.FluentUI.AspNetCore.Components.dll");

var mainCtx = new AssemblyLoadContext("main");
var mainAsm = mainCtx.LoadFromAssemblyPath(mainPath);
var filledCtx = new AssemblyLoadContext("filled", true);
// Need to resolve dependencies
filledCtx.Resolving += (ctx, name) =>
{
    if (name.Name == "Microsoft.FluentUI.AspNetCore.Components")
        return mainAsm;
    return null;
};

var filledAsm = filledCtx.LoadFromAssemblyPath(filledPath);

Console.WriteLine("Filled types: " + filledAsm.GetTypes().Length);

var ns = filledAsm.GetTypes()
    .Select(t => t.Namespace)
    .Where(n => n != null)
    .Distinct()
    .OrderBy(n => n)
    .Take(20);
foreach (var n in ns)
    Console.WriteLine("NS: " + n);

var shield = filledAsm.GetTypes().FirstOrDefault(t => t.Name == "Shield");
if (shield != null)
{
    Console.WriteLine("Shield found: " + shield.FullName);
    Console.WriteLine("Shield base: " + shield.BaseType?.FullName);
}

