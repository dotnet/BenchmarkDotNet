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

public class WasmAppBuilder
{
    // FIXME: Document

    public string AppDir { get; set; }
    public string MicrosoftNetCoreAppRuntimePackDir { get; set; }
    public string MainAssembly { get; set; }
    public string TargetPath { get; set; }

    public string MainJS { get; set; }

    public List<string> AssemblySearchPaths { get; set; }
    public List<string> ExtraAssemblies { get; set; }
    public List<string> FilesToIncludeInFileSystem { get; set; }
   
    Dictionary<string, Assembly>_assemblies;
    private Resolver _resolver;

    public bool Execute ()
    {
        if (!File.Exists(MainAssembly))
            throw new ArgumentException($"File MainAssembly='{MainAssembly}' doesn't exist.");
        if (!File.Exists(MainJS))
            throw new ArgumentException($"File MainJS='{MainJS}' doesn't exist.");

        var paths = new List<string>();
        _assemblies = new Dictionary<string, Assembly>();

        // Collect and load assemblies used by the app
        foreach (string dir in AssemblySearchPaths)
        {
            if (!Directory.Exists(dir))
                throw new ArgumentException($"Directory '{dir}' doesn't exist or not a directory.");
            paths.Add(dir);
        }
        _resolver = new Resolver(paths);
        var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

        var mainAssembly = mlc.LoadFromAssemblyPath (MainAssembly);
        Add(mlc, mainAssembly);

        if (ExtraAssemblies != null)
        {
            foreach (var item in ExtraAssemblies)
            {
                var refAssembly = mlc.LoadFromAssemblyPath(item);
                Add(mlc, refAssembly);
            }
        }

        // Create app
        Directory.CreateDirectory(AppDir);
        Directory.CreateDirectory(Path.Combine(AppDir, "managed"));
        foreach (var assembly in _assemblies.Values)
            File.Copy(assembly.Location, Path.Combine(AppDir, "managed", Path.GetFileName(assembly.Location)), true);
        foreach (var f in new string[] { "dotnet.wasm", "dotnet.js" })
            File.Copy(Path.Combine(MicrosoftNetCoreAppRuntimePackDir, "native", f), Path.Combine(AppDir, f), true);
        File.Copy(MainJS, Path.Combine(AppDir, "runtime.js"),  true);

        var filesToMap = new Dictionary<string, List<string>>();
        if (FilesToIncludeInFileSystem != null)
        {
            string supportFilesDir = Path.Combine(AppDir, "supportFiles");
            Directory.CreateDirectory(supportFilesDir);

            foreach (var item in FilesToIncludeInFileSystem)
            {
                if (string.IsNullOrEmpty(TargetPath))
                {
                    TargetPath = Path.GetFileName(item);
                }

                // We normalize paths from `\` to `/` as MSBuild items could use `\`.
                TargetPath = TargetPath.Replace('\\', '/');

                string directory = Path.GetDirectoryName(TargetPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(Path.Combine(supportFilesDir, directory));
                }
                else
                {
                    directory = "/";
                }

                File.Copy(item, Path.Combine(supportFilesDir, TargetPath), true);

                if (filesToMap.TryGetValue(directory, out List<string> files))
                {
                    files.Add(Path.GetFileName(TargetPath));
                }
                else
                {
                    files = new List<string>();
                    files.Add(Path.GetFileName(TargetPath));
                    filesToMap[directory] = files;
                }
            }
        }

        using (var sw = File.CreateText(Path.Combine(AppDir, "mono-config.js")))
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
            catch (System.IO.FileNotFoundException e)
            {
                Console.Error.WriteLine($"WARNING: Could not load {aname.Name}");
            }
        }
    }
}

internal class Resolver : MetadataAssemblyResolver
{
    private List<string> _searchPaths;

    public Resolver(List<string> searchPaths)
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
