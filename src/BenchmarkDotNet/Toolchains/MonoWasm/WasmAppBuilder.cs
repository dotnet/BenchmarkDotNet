// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using BenchmarkDotNet.Toolchains.MonoWasm;

public class WasmAppBuilder
{
    private readonly WasmSettings WasmSettings;
    private readonly string TargetFrameworkMoniker;

    public WasmAppBuilder(WasmSettings wasmSettings, string targetFrameworkMoniker)
    {
        WasmSettings = wasmSettings;
        TargetFrameworkMoniker = targetFrameworkMoniker;
    }

    public bool BuildApp (string programName, string projectRoot)
    {
        string[] assemblies;
        string appDir = Path.Combine(projectRoot, $"bin", TargetFrameworkMoniker, "browser-wasm", "publish");
        string outputDir = Path.Combine(appDir, "output");

        string mainAssemblyPath = Path.Combine(appDir, $"{programName}.dll");

        if (!File.Exists(mainAssemblyPath))
            throw new ArgumentException($"File MainAssembly='{mainAssemblyPath}' doesn't exist.");
        if (!File.Exists(WasmSettings.WasmMainJS))
            throw new ArgumentException($"File MainJS='{WasmSettings.WasmMainJS}' doesn't exist.");

        var paths = new List<string>();
        assemblies = Directory.GetFiles(appDir, "*.dll");

        // Create app
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "managed"));
        foreach (var assembly in assemblies)
            File.Copy(assembly, Path.Combine(outputDir, "managed", Path.GetFileName(assembly)), true);

        foreach (var f in new string[] { "dotnet.wasm", "dotnet.js" })
            File.Copy(Path.Combine(appDir, f), Path.Combine(outputDir, f), true);

        File.Copy(WasmSettings.WasmMainJS, Path.Combine(outputDir, "runtime.js"),  true);

        using (var sw = File.CreateText(Path.Combine(outputDir, "mono-config.js")))
        {
            sw.WriteLine("config = {");
            sw.WriteLine("\tvfs_prefix: \"managed\",");
            sw.WriteLine("\tdeploy_prefix: \"managed\",");
            sw.WriteLine("\tenable_debugging: 0,");
            sw.WriteLine("\tassembly_list: [");
            foreach (var assembly in assemblies)
            {
                sw.Write("\t\t\"" + Path.GetFileName(assembly) + "\"");
                sw.WriteLine(",");
            }
            sw.WriteLine ("\t],");
            sw.WriteLine("\tfiles_to_map: [],");

            sw.WriteLine ("}");
        }

        return true;
    }
}
