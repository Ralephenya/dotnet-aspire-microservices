using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GACS.Components.Components;

public static class TempIconInspector
{
    public static void Inspect()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages",
            "microsoft.fluentui.aspnetcore.components.icons", "4.14.2", "lib", "net8.0");

        var mainAsm = Assembly.LoadFrom(Path.Combine(basePath, "Microsoft.FluentUI.AspNetCore.Components.dll"));
        var filledAsm = Assembly.LoadFrom(Path.Combine(basePath, "Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.dll"));
        var regularAsm = Assembly.LoadFrom(Path.Combine(basePath, "Microsoft.FluentUI.AspNetCore.Components.Icons.Regular.dll"));

        Console.WriteLine("Filled types: " + filledAsm.GetTypes().Length);
        Console.WriteLine("Regular types: " + regularAsm.GetTypes().Length);

        var ns = filledAsm.GetTypes()
            .Select(t => t.Namespace)
            .Where(n => n != null)
            .Distinct()
            .OrderBy(n => n)
            .Take(10);
        foreach (var n in ns)
            Console.WriteLine("NS: " + n);

        var shield = filledAsm.GetTypes().FirstOrDefault(t => t.Name == "Shield");
        if (shield != null)
        {
            Console.WriteLine("Shield found: " + shield.FullName);
            Console.WriteLine("Shield base: " + shield.BaseType?.FullName);
        }
    }
}
