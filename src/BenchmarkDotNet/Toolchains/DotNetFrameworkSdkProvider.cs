using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BenchmarkDotNet.Toolchains
{
    public class DotNetFrameworkSdkProvider : IDotNetFrameworkSdkProvider
    {
        public IEnumerable<string> GetInstalledFrameworkSdks()
        {
            var versions = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Get1To45VersionFromRegistry(versions);
                Get45PlusFromRegistry(versions);
            }

            return versions;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This code is protected with a runtime OS platform check")]
        private void Get1To45VersionFromRegistry(List<string> versions)
        {
            using (var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\\"))
            {
                if (ndpKey == null)
                {
                    return;
                }

                foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                {
                    if (versionKeyName == "v4")
                    {
                        continue;
                    }

                    if (versionKeyName.StartsWith("v"))
                    {
                        var versionKey = ndpKey.OpenSubKey(versionKeyName);
                        string name = (string)versionKey.GetValue("Version", "");
                        string sp = versionKey.GetValue("SP", "").ToString();
                        string install = versionKey.GetValue("Install", "").ToString();
                        if (string.IsNullOrEmpty(install))
                        {
                            versions.Add(name);
                        }
                        else
                        {
                            if (!(string.IsNullOrEmpty(sp)) && install == "1")
                            {
                                versions.Add(name + " SP" + sp);
                            }
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        foreach (string subKeyName in versionKey.GetSubKeyNames())
                        {
                            var subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)subKey.GetValue("Version", "");
                            if (!string.IsNullOrEmpty(name))
                            {
                                sp = subKey.GetValue("SP", "").ToString();
                            }

                            install = subKey.GetValue("Install", "").ToString();
                            if (string.IsNullOrEmpty(install))
                            {
                                versions.Add(name);
                            }
                            else
                            {
                                if (!(string.IsNullOrEmpty(sp)) && install == "1")
                                {
                                    versions.Add(name + " SP" + sp);
                                }
                                else if (install == "1")
                                {
                                    versions.Add(name);
                                }
                            }
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This code is protected with a runtime OS platform check")]
        private void Get45PlusFromRegistry(List<string> versions)
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subkey))
            {
                if (ndpKey == null)
                {
                    return;
                }

                if (ndpKey.GetValue("Version") != null)
                {
                    versions.Add(ndpKey.GetValue("Version").ToString());
                }
                else
                {
                    if (ndpKey.GetValue("Release") != null)
                    {
                        versions.Add(CheckFor45PlusVersion((int)ndpKey.GetValue("Release")));
                    }
                }
            }
        }

        private string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 533320)
                return "4.8.1";
            if (releaseKey >= 528040)
                return "4.8";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";

            return "";
        }
    }
}