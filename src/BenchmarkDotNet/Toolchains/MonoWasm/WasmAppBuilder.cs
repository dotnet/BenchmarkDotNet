// -*- indent-tabs-mode: nil -*-
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
    // FIXME: Document

    private readonly WasmSettings WasmSettings;

    private readonly string TargetFrameworkMoniker;

  

    private Dictionary<string, Assembly> _assemblies;
    private AssemblyResolver _resolver;


    public WasmAppBuilder(WasmSettings wasmSettings, string targetFrameworkMoniker)
    {
        WasmSettings = wasmSettings;
        TargetFrameworkMoniker = targetFrameworkMoniker;
    }

    public bool BuildApp (string programName, string projectRoot)
    {
        string appDir = Path.Combine(projectRoot, $"bin/{TargetFrameworkMoniker}/browser-wasm/publish");

        string outputDir = Path.Combine(appDir, "output");

        List<string> assemblySearchPaths = new List<string>
                                  {
                                    appDir
                                  };

        string mainAssemblyPath = Path.Combine(appDir, $"{programName}.dll");


        if (!File.Exists(mainAssemblyPath))
            throw new ArgumentException($"File MainAssembly='{mainAssemblyPath}' doesn't exist.");
        if (!File.Exists(WasmSettings.WasmMainJS))
            throw new ArgumentException($"File MainJS='{WasmSettings.WasmMainJS}' doesn't exist.");

        var paths = new List<string>();
        _assemblies = new Dictionary<string, Assembly>();

        // Collect and load assemblies used by the app
        foreach (string dir in assemblySearchPaths)
        {
            if (!Directory.Exists(dir))
                throw new ArgumentException($"Directory '{dir}' doesn't exist or not a directory.");
            paths.Add(dir);
        }
        _resolver = new AssemblyResolver(paths);
        var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

        Assembly mainAssembly = mlc.LoadFromAssemblyPath (mainAssemblyPath);
        Add(mlc, mainAssembly);

        // Create app
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "managed"));
        foreach (var assembly in _assemblies.Values)
            File.Copy(assembly.Location, Path.Combine(outputDir, "managed", Path.GetFileName(assembly.Location)), true);
        foreach (var f in new string[] { "dotnet.wasm", "dotnet.js" })
            File.Copy(Path.Combine(appDir, f), Path.Combine(outputDir, f), true);
        File.Copy(WasmSettings.WasmMainJS, Path.Combine(outputDir, "runtime.js"),  true);

        string supportFilesDir = Path.Combine(outputDir, "supportFiles");
        Directory.CreateDirectory(supportFilesDir);

        var filesToMap = copySystemPrivateCoreLib(appDir, supportFilesDir);

        using (var sw = File.CreateText(Path.Combine(outputDir, "mono-config.js")))
        {
            sw.WriteLine("config = {");
            sw.WriteLine("\tvfs_prefix: \"managed\",");
            sw.WriteLine("\tdeploy_prefix: \"managed\",");
            sw.WriteLine("\tenable_debugging: 0,");
            sw.WriteLine("\tassembly_list: [");
            foreach (var assembly in _assemblies.Values)
            {
                sw.Write("\t\t\"" + Path.GetFileName(assembly.Location) + "\"");
                sw.WriteLine(",");
            }
            sw.WriteLine ("\t],");
            sw.WriteLine("\tfiles_to_map: [");
            foreach (KeyValuePair<string, List<string>> keyValuePair in filesToMap)
            {
                sw.WriteLine("\t{");
                sw.WriteLine($"\t\tdirectory: \"{keyValuePair.Key}\",");
                sw.WriteLine("\t\tfiles: [");
                foreach (string file in keyValuePair.Value)
                {
                    sw.WriteLine($"\t\t\t\"{file}\",");
                }
                sw.WriteLine("\t\t],");
                sw.WriteLine("\t},");
            }
            sw.WriteLine ("\t],");
            sw.WriteLine ("}");
        }


        using (var sw = File.CreateText(Path.Combine(outputDir, "run-v8.sh")))
        {
            sw.WriteLine("v8 --expose_wasm runtime.js -- --run " + Path.GetFileName(mainAssemblyPath) + " $*");
        }

        return true;
    }

    private void Add(MetadataLoadContext mlc, Assembly assembly)
    {
        if (_assemblies.ContainsKey(assembly.GetName().Name))
            return;
        _assemblies[assembly.GetName().Name] = assembly;


        foreach (var aname in assembly.GetReferencedAssemblies())
        {
            try
            {
                var refAssembly = mlc.LoadFromAssemblyName(aname);
                Add(mlc, refAssembly);
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.Error.WriteLine($"WARNING: Could not load {aname.Name}");
            }
        }
    }

    private Dictionary<string, List<string>> copySystemPrivateCoreLib(string appDir, string supportFilesDir)
    {
        Dictionary<string, List<string>> fileMap = new Dictionary<string, List<string>>();

        string systemPrivateCoreLibPath = $"{appDir}/System.Private.CoreLib.dll";

        string targetPath = Path.GetFileName(systemPrivateCoreLibPath);

        // We normalize paths from `\` to `/` as MSBuild items could use `\`;

        string directory = Path.GetDirectoryName(targetPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(Path.Combine(supportFilesDir, directory));
        }
        else
        {
            directory = "/";
        }

        File.Copy(systemPrivateCoreLibPath, Path.Combine(supportFilesDir, targetPath), true);


        List<string> files = new List<string>();
        files.Add(Path.GetFileName(targetPath));
        fileMap[directory] = files;

        return fileMap;
    }

}


internal class AssemblyResolver : MetadataAssemblyResolver
{
    private List<string> _searchPaths;

    public AssemblyResolver(List<string> searchPaths)
    {
        _searchPaths = searchPaths;
    }

    public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        foreach (var dir in _searchPaths)
        {
            var path = Path.Combine(dir, name + ".dll");
            if (File.Exists(path))
            {
                return context.LoadFromAssemblyPath(path);
            }
        }
        return null;
    }
}
