//TODO this file is copied form PerfView repo.

/****************************************************************************/
/*                           DirectoryUtilities.cs                          */
/****************************************************************************/

/*  Copyright (c) Microsoft Corporation.  All rights reserved. */
/* AUTHOR: Vance Morrison   
 * Date  : 10/20/2007  */
/****************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Utilities
{
    /******************************************************************************/
    /// <summary>
    /// General purpose utilities dealing with archiveFile system directories. 
    /// </summary>
#if UTILITIES_PUBLIC
    public 
#endif
    static class DirectoryUtilities
    {
        /// <summary>
        /// SafeCopy sourceDirectory to directoryToVersion recursively. The target directory does
        /// no need to exist
        /// </summary>
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            Copy(sourceDirectory, targetDirectory, SearchOption.AllDirectories);
        }

        /// <summary>
        /// SafeCopy all files from sourceDirectory to directoryToVersion.  If searchOptions == AllDirectories
        /// then the copy is recursive, otherwise it is just one level.  The target directory does not
        /// need to exist. 
        /// </summary>
        public static void Copy(string sourceDirectory, string targetDirectory, SearchOption searchOptions)
        {
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
            {
                string targetFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));
                FileUtilities.ForceCopy(sourceFile, targetFile);
            }
            if (searchOptions == SearchOption.AllDirectories)
            {
                foreach (string sourceDir in Directory.GetDirectories(sourceDirectory))
                {
                    string targetDir = Path.Combine(targetDirectory, Path.GetFileName(sourceDir));
                    Copy(sourceDir, targetDir, searchOptions);
                }
            }
        }

        /// <summary>
        /// Clean is sort of a 'safe' recursive delete of a directory.  It either deletes the
        /// files or moves them to '*.deleting' names.  It deletes directories that are completely
        /// empty.  Thus it will do a recursive delete when that is possible.  There will only 
        /// be *.deleting files after this returns.  It returns the number of files and directories
        /// that could not be deleted.  
        /// </summary>
        public static int Clean(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            int ret = 0;
            foreach (string file in Directory.GetFiles(directory))
            {
                if (!FileUtilities.ForceDelete(file))
                {
                    ret++;
                }
            }

            foreach (string subDir in Directory.GetDirectories(directory))
            {
                ret += Clean(subDir);
            }

            if (ret == 0)
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {
                    ret++;
                }
            }
            else
            {
                ret++;
            }

            return ret;
        }

        /// <summary>
        /// Removes the oldest directories directly under 'directoryPath' so that 
        /// only 'numberToKeep' are left. 
        /// </summary>
        /// <param variable="directoryPath">Directory to removed old files from.</param>
        /// <param variable="numberToKeep">The number of files to keep.</param>
        /// <returns> true if there were no errors deleting files</returns>
        public static bool DeleteOldest(string directoryPath, int numberToKeep)
        {
            if (!Directory.Exists(directoryPath))
            {
                return true;
            }

            string[] dirs = Directory.GetDirectories(directoryPath);
            int numToDelete = dirs.Length - numberToKeep;
            if (numToDelete <= 0)
            {
                return true;
            }

            Array.Sort<string>(dirs, delegate (string x, string y)
            {
                return File.GetLastWriteTimeUtc(x).CompareTo(File.GetLastWriteTimeUtc(y));
            });

            bool ret = true;
            for (int i = 0; i < numToDelete; i++)
            {
                try
                {
                    Directory.Delete(dirs[i]);
                }
                catch (Exception)
                {
                    // TODO trace message;
                    ret = false;
                }
            }
            return ret;
        }

        /// <summary>
        /// DirectoryUtilities.GetFiles is basicaly the same as Directory.GetFiles 
        /// however it returns IEnumerator, which means that it lazy.  This is very important 
        /// for large directory trees.  A searchPattern can be specified (Windows wildcard conventions)
        /// that can be used to filter the set of archiveFile names returned. 
        /// 
        /// Suggested Usage
        /// 
        ///     foreach(string fileName in DirectoryUtilities.GetFiles("c:\", "*.txt")){
        ///         Console.WriteLine(fileName);
        ///     }
        ///
        /// </summary>
        /// <param variable="directoryPath">The base directory to enumerate</param>
        /// <param variable="searchPattern">A pattern to filter the names (windows filename wildcards * ?)</param>
        /// <param variable="searchOptions">Indicate if the search is recursive or not.  </param>
        /// <returns>The enumerator for all archiveFile names in the directory (recursively). </returns>
        public static IEnumerable<string> GetFiles(string directoryPath, string searchPattern, SearchOption searchOptions)
        {

            string[] fileNames = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
            Array.Sort<string>(fileNames, StringComparer.OrdinalIgnoreCase);
            foreach (string fileName in fileNames)
            {
                yield return fileName;
            }

            if (searchOptions == SearchOption.AllDirectories)
            {
                string[] subDirNames = Directory.GetDirectories(directoryPath);
                Array.Sort<string>(subDirNames);
                foreach (string subDir in subDirNames)
                {
                    foreach (string fileName in DirectoryUtilities.GetFiles(subDir, searchPattern, searchOptions))
                    {
                        yield return fileName;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a lazy enumerable for every path in 'directoryName' that matchs 'searchPattern' (default is *)MO
        /// </summary>
        public static IEnumerable<string> GetFiles(string directoryName, string searchPattern = "*")
        {
            return GetFiles(directoryName, searchPattern, SearchOption.TopDirectoryOnly);
        }
    }
}