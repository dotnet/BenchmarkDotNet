
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Utilities;

namespace BenchmarkDotNet.Diagnostics.Windows.PerfView
{
    /// <summary>
    /// This class is partial copy from PerfView. I leave the commented code because I do not know If I commented too much.
    /// </summary>
    class App
    {
        private static SymbolReader s_symbolReader;
        private static string m_SymbolPath;
        private static string m_SourcePath;
        // Global symbol and source paths
        public static string SymbolPath
        {
            get
            {
                if (m_SymbolPath == null)
                {
                    // Start with _NT_SYMBOL_PATH
                    var symPath = new SymbolPath(Microsoft.Diagnostics.Symbols.SymbolPath.SymbolPathFromEnvironment);

                    bool persistSymPath = true;
                    symPath.Add(Microsoft.Diagnostics.Symbols.SymbolPath.MicrosoftSymbolServerPath);
                    //                    // If we still don't have anything, add a default one
                    //                    // Since the default goes off machine, if we are outside of Microsoft, we have to ask
                    //                    // the user for permission. 
                    //                    if (AppLog.InternalUser)
                    //                    {
                    //                        symPath.Add("SRV*http://symweb.corp.microsoft.com");
                    //                        symPath.Add(Microsoft.Diagnostics.Symbols.SymbolPath.MicrosoftSymbolServerPath);
                    //                    }
                    //                    else if (symPath.Elements.Count == 0)
                    //                    {
                    //                        if (SupportFiles.ProcessArch == ProcessorArchitecture.Arm || App.CommandLineArgs.NoGui)
                    //                        {
                    //                            App.CommandProcessor.LogFile.WriteLine("WARNING NO _NT_SYMBOL_PATH set ...");
                    //                            persistSymPath = false;     // If we could not interact with the user, don't persist the answer.  
                    //                        }
                    //                        else
                    //                        {
                    //                            if (UserOKWithSymbolServerGui())
                    //                            {
                    //                                symPath.Add(Microsoft.Diagnostics.Symbols.SymbolPath.MicrosoftSymbolServerPath);
                    //                            }
                    //                        }
                    //                    }

                    // TODO FIX NOW we will end up with both internal and external symbol servers on the symbol path.  
                    // Should we clean that up?

                    // Remember it.  
                    if (persistSymPath)
                    {
                        SymbolPath = symPath.InsureHasCache(symPath.DefaultSymbolCache()).CacheFirst().ToString();
                    }
                }
                return m_SymbolPath;
            }
            set
            {
                m_SymbolPath = value;
        
            }
        }
        public static string SourcePath
        {
            get
            {
                if (m_SourcePath == null)
                {
                    var symPath = new SymbolPath(Environment.GetEnvironmentVariable("_NT_SOURCE_PATH"));
//                    var savedPath = App.ConfigData["_NT_SOURCE_PATH"];
//                    if (savedPath != null)
//                    {
//                        symPath.Add(savedPath);
//                    }

                    // Remember it.  
                    SourcePath = symPath.ToString();
                }
                return m_SourcePath;
            }
            set
            {
                m_SourcePath = value;
//                App.ConfigData["_NT_SOURCE_PATH"] = value;
            }
        }
        public static SymbolReader GetSymbolReader(string etlFilePath = null, SymbolReaderOptions symbolFlags = SymbolReaderOptions.None)
        {
            var log = TextWriter.Null;
            SymbolPath symPath = new SymbolPath(App.SymbolPath);
            if ((symbolFlags & SymbolReaderOptions.CacheOnly) != 0)
            {
                symPath = new SymbolPath("SRV*" + symPath.DefaultSymbolCache());
            }

            var sourcePath = App.SourcePath;
            string localSymDir = symPath.DefaultSymbolCache();
            if (etlFilePath != null)
            {
                // Add the directory where the file resides and a 'symbols' subdirectory 
                var filePathDir = Path.GetDirectoryName(etlFilePath);
                if (filePathDir.Length != 0)
                {
                    // Then the directory where the .ETL file lives. 
                    symPath.Insert(filePathDir);

                    // If there is a 'symbols' directory next to the data file, look for symbols there
                    // as well.   Note that we also put copies of any symbols here as well (see below)
                    string potentiallocalSymDir = Path.Combine(filePathDir, "symbols");
                    if (Directory.Exists(potentiallocalSymDir))
                    {
                        symPath.Insert(potentiallocalSymDir);
                        symPath.Insert("SRV*" + potentiallocalSymDir);
                        localSymDir = potentiallocalSymDir;
                    }

                    // WPR conventions add any .etl.ngenPDB directory to the path too.   has higher priority still. 
                    var wprSymDir = etlFilePath + ".NGENPDB";
                    if (Directory.Exists(wprSymDir))
                    {
                        symPath.Insert("SRV*" + wprSymDir);
                    }
                    else
                    {
                        // I have now seen both conventions .etl.ngenpdb and .ngenpdb, so look for both.  
                        wprSymDir = Path.ChangeExtension(etlFilePath, ".NGENPDB");
                        if (Directory.Exists(wprSymDir))
                        {
                            symPath.Insert("SRV*" + wprSymDir);
                        }
                    }
                    // VS uses .NGENPDBS as a convention.  
                    wprSymDir = etlFilePath + ".NGENPDBS";
                    if (Directory.Exists(wprSymDir))
                    {
                        symPath.Insert("SRV*" + wprSymDir);
                    }

                    if (!string.IsNullOrWhiteSpace(sourcePath))
                    {
                        sourcePath += ";";
                    }

                    sourcePath += filePathDir;
                    var srcDir = Path.Combine(filePathDir, "src");
                    if (Directory.Exists(srcDir))
                    {
                        sourcePath += ";" + srcDir;
                    }
                }
            }
            // Add the Support Files directory so that you get the tutorial example
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                sourcePath += ";";
            }

            //sourcePath += SupportFiles.SupportFileDir;

            // Can we use the cached symbol reader?
            if (s_symbolReader != null)
            {
                s_symbolReader.SourcePath = sourcePath;
                if (symbolFlags == SymbolReaderOptions.None && s_symbolReader.SymbolPath == symPath.ToString())
                {
                    return s_symbolReader;
                }

                s_symbolReader.Dispose();
                s_symbolReader = null;
            }

            log.WriteLine("Symbol reader _NT_SYMBOL_PATH= {");
            foreach (var element in symPath.Elements)
            {
                log.WriteLine("    {0};", element.ToString());
            }

            log.WriteLine("    }");
            log.WriteLine("This can be set using the File -> Set Symbol Path dialog on the Stack Viewer.");
            SymbolReader ret = new SymbolReader(log, symPath.ToString());
            ret.SourcePath = sourcePath;
            ret.Options = symbolFlags;

//#if !PERFVIEW_COLLECT
            //            if (!AppLog.InternalUser && !App.CommandLineArgs.TrustPdbs)
            //            {
            //                ret.SecurityCheck = delegate (string pdbFile)
            //                {
            //                    //TODO
            ////                    var result = System.Windows.MessageBox.Show("Found " + pdbFile + " in a location that may not be trustworthy, do you trust this file?",
            ////                        "Security Check", System.Windows.MessageBoxButton.YesNo);
            //                    return true;
            //                };
            //            }
            //            else
            //#endif
            //            {
            ret.SecurityCheck = (pdbFile => true);
            //            }

            string tempDir = Environment.GetEnvironmentVariable("TEMP");
            if (tempDir == null)
            {
                tempDir = ".";
            }

            var exeName = "PerfView";
            var CacheDir = Path.Combine(tempDir, exeName);
            Directory.CreateDirectory(CacheDir);
            ret.SourceCacheDirectory = Path.Combine(CacheDir, "src");
            if (localSymDir != null)
            {
                ret.OnSymbolFileFound += (pdbPath, pdbGuid, pdbAge) => CacheInLocalSymDir(localSymDir, pdbPath, pdbGuid, pdbAge, log);
            }

            if (symbolFlags == SymbolReaderOptions.None)
            {
                s_symbolReader = ret;
            }

            return ret;
        }

        /// <summary>
        /// This routine gets called every time we find a PDB.  We copy any PDBs to 'localPdbDir' if it is not
        /// already there.  That way every PDB that is needed is locally available, which is a nice feature.  
        /// We log any action we take to 'log'.  
        /// </summary>
        private static void CacheInLocalSymDir(string localPdbDir, string pdbPath, Guid pdbGuid, int pdbAge, TextWriter log)
        {
            // We do this all in a fire-and-forget task so that it does not block the User.   It is 
            // optional after all.  
            Task.Factory.StartNew(delegate ()
            {
                try
                {
                    var fileName = Path.GetFileName(pdbPath);
                    if (pdbGuid != Guid.Empty)
                    {
                        var pdbPathPrefix = Path.Combine(localPdbDir, fileName);
                        // There is a non-trivial possibility that someone puts a FILE that is named what we want the dir to be.  
                        if (File.Exists(pdbPathPrefix))
                        {
                            // If the pdb path happens to be the SymbolCacheDir (a definite possibility) then we would
                            // clobber the source file in our attempt to set up the target.  In this case just give up
                            // and leave the file as it was.  
                            if (string.Compare(pdbPath, pdbPathPrefix, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                return;
                            }

                            log.WriteLine("Removing file {0} from symbol cache to make way for symsrv files.", pdbPathPrefix);
                            File.Delete(pdbPathPrefix);
                        }
                        localPdbDir = Path.Combine(pdbPathPrefix, pdbGuid.ToString("N") + pdbAge.ToString());
                    }

                    if (!Directory.Exists(localPdbDir))
                    {
                        Directory.CreateDirectory(localPdbDir);
                    }

                    var localPdbPath = Path.Combine(localPdbDir, fileName);
                    var fileExists = File.Exists(localPdbPath);
                    if (!fileExists || File.GetLastWriteTimeUtc(localPdbPath) != File.GetLastWriteTimeUtc(pdbPath))
                    {
                        if (fileExists)
                        {
                            log.WriteLine("WARNING: overwriting existing file {0}.", localPdbPath);
                        }

                        log.WriteLine("Copying {0} to local cache {1}", pdbPath, localPdbPath);
                        // Do it as a copy and a move so that the update is atomic.  
                        var newLocalPdbPath = localPdbPath + ".new";
                        FileUtilities.ForceCopy(pdbPath, newLocalPdbPath);
                        FileUtilities.ForceMove(newLocalPdbPath, localPdbPath);
                    }
                }
                catch (Exception e)
                {
                    log.WriteLine("Error trying to update local PDB cache {0}", e.Message);
                }
            });
        }
    }
}
