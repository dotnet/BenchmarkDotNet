//TODO this file is copied form PerfView repo.

/****************************************************************************/
/*                                  FileUtilities.cs                        */
/****************************************************************************/

/*  Copyright (c) Microsoft Corporation.  All rights reserved. */
/* AUTHOR: Vance Morrison   
 * Date  : 10/20/2007  */
/****************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Diagnostics.Utilities
{
    /******************************************************************************/
    /// <summary>
    /// General purpose utilities dealing with archiveFile system files. 
    /// </summary>
#if UTILITIES_PUBLIC
    public 
#endif
    static class FileUtilities
    {
        /// <summary>
        /// GetLines works much like File.ReadAllLines, however instead of returning a
        /// array of lines, it returns a IEnumerable so that the archiveFile is not read all
        /// at once.  This allows 'foreach' syntax to be used on very large files.  
        /// 
        /// Suggested Usage
        /// 
        ///     foreach(string lineNumber in FileUtilities.GetLines("largeFile.txt")){
        ///         Console.WriteLine(lineNumber);
        ///     }
        /// </summary>
        /// <param variable="fileName">The base directory to enumerate.</param>
        /// <returns>The enumerator for all lines in the archiveFile.</returns>
        public static IEnumerable<string> ReadAllLines(string fileName)
        {
            StreamReader stream = File.OpenText(fileName);
            while (!stream.EndOfStream)
            {
                yield return stream.ReadLine();
            }
            stream.Dispose();
        }

        /// <summary>
        /// Given archiveFile specifications possibly with wildcards in them
        /// Returns an enumerator that returns each expanded archiveFile name in turn. 
        /// 
        /// If searchOpt is AllDirectories it does a recursive match. 
        /// </summary>
        public static IEnumerable<string> ExpandWildcards(string[] fileSpecifications, SearchOption searchOpt = SearchOption.TopDirectoryOnly)
        {
            foreach (string fileSpec in fileSpecifications)
            {
                string dir = Path.GetDirectoryName(fileSpec);
                if (dir.Length == 0)
                {
                    dir = ".";
                }

                string file = Path.GetFileName(fileSpec);
                foreach (string fileName in DirectoryUtilities.GetFiles(dir, file, searchOpt))
                {
                    yield return fileName;
                }
            }
        }

        /// <summary>
        /// Delete works much like File.Delete, except that it will succeed if the
        /// archiveFile does not exist, and will rename the archiveFile so that even if the archiveFile 
        /// is locked the original archiveFile variable will be made available.  
        /// 
        /// It renames the  archiveFile with a '[num].deleting'.  These files might be left 
        /// behind.  
        /// 
        /// It returns true if it was completely successful.  If there is a *.deleting
        /// archiveFile left behind, it returns false. 
        /// </summary>
        /// <param variable="fileName">The variable of the archiveFile to delete</param>
        public static bool ForceDelete(string fileName)
        {
            if (Directory.Exists(fileName))
            {
                return DirectoryUtilities.Clean(fileName) != 0;
            }

            if (!File.Exists(fileName))
            {
                return true;
            }

            // First move the archiveFile out of the way, so that even if it is locked
            // The original archiveFile is still gone.  
            string fileToDelete = fileName;
            bool tryToDeleteOtherFiles = true;
            if (!fileToDelete.EndsWith(".deleting", StringComparison.OrdinalIgnoreCase))
            {
                tryToDeleteOtherFiles = false;
                int i = 0;
                for (i = 0; ; i++)
                {
                    fileToDelete = fileName + "." + i.ToString() + ".deleting";
                    if (!File.Exists(fileToDelete))
                    {
                        break;
                    }

                    tryToDeleteOtherFiles = true;
                }
                try
                {
                    File.Move(fileName, fileToDelete);
                }
                catch (Exception)
                {
                    fileToDelete = fileName;
                }
            }

            bool ret = false;
            try
            {
                ret = TryDelete(fileToDelete);
                if (tryToDeleteOtherFiles)
                {
                    // delete any old *.deleting files that may have been left around 
                    string deletePattern = Path.GetFileName(fileName) + @".*.deleting";
                    foreach (string deleteingFile in Directory.GetFiles(Path.GetDirectoryName(fileName), deletePattern))
                    {
                        TryDelete(deleteingFile);
                    }
                }
            }
            catch { };
            return ret;
        }

        /// <summary>
        /// Try to delete 'fileName' catching any exception.  Returns true if successful.   It will delete read-only files.  
        /// </summary>  
        public static bool TryDelete(string fileName)
        {
            bool ret = false;
            if (!File.Exists(fileName))
            {
                return true;
            }

            try
            {
                FileAttributes attribs = File.GetAttributes(fileName);
                if ((attribs & FileAttributes.ReadOnly) != 0)
                {
                    attribs &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(fileName, attribs);
                }
                File.Delete(fileName);
                ret = true;
            }
            catch (Exception) { }
            return ret;
        }

        /// <summary>
        /// SafeCopy sourceFile to destinationFile.  If the destination exists
        /// used ForceDelete to get rid of it first.  
        /// </summary>
        public static void ForceCopy(string sourceFile, string destinationFile)
        {
            ForceDelete(destinationFile);       // will return immediate if the destination does not exist. 
            File.Copy(sourceFile, destinationFile);
        }

        /// <summary>
        /// Moves sourceFile to destinationFile.  If the destination exists
        /// used ForceDelete to get rid of it first.  
        /// </summary>
        public static void ForceMove(string sourceFile, string destinationFile)
        {
            ForceDelete(destinationFile);       // will return immediate if the destination does not exist. 
            File.Move(sourceFile, destinationFile);
        }

        /// <summary>
        /// Returns true if the two file have exactly the same content (as a stream of bytes). 
        /// </summary>
        public static bool Equals(string fileName1, string fileName2)
        {
            byte[] buffer1 = new byte[8192];
            byte[] buffer2 = new byte[8192];
            using (FileStream file1 = File.Open(fileName1, FileMode.Open, FileAccess.Read))
            {
                using (FileStream file2 = File.Open(fileName2, FileMode.Open, FileAccess.Read))
                {
                    int count1 = file1.Read(buffer1, 0, buffer1.Length);
                    int count2 = file2.Read(buffer2, 0, buffer2.Length);
                    if (count1 != count2)
                    {
                        return false;
                    }

                    for (int i = 0; i < count1; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Utilities associated with file name paths. 
    /// </summary>
#if UTILITIES_PUBLIC
    public 
#endif
    static class PathUtil
    {
        /// <summary>
        /// Given a path and a superdirectory path relativeToDirectory compute the relative path (the path from) relativeToDirectory
        /// </summary>
        public static string PathRelativeTo(string path, string relativeToDirectory)
        {
            Debug.Assert(!relativeToDirectory.EndsWith("\\"));

            string fullPath = Path.GetFullPath(path);
            string fullCurrentDirectory = Path.GetFullPath(relativeToDirectory);

            int commonToSlashIndex = -1;
            for (int i = 0; i < fullPath.Length; i++)
            {
                char cFullPath = fullPath[i];
                if (i >= fullCurrentDirectory.Length)
                {
                    if (cFullPath == '\\')
                    {
                        commonToSlashIndex = i;
                    }

                    break;
                }
                char cCurrentDirectory = fullCurrentDirectory[i];

                if (cCurrentDirectory != cFullPath)
                {
                    if (char.IsLower(cCurrentDirectory))
                    {
                        cCurrentDirectory = (char)(cCurrentDirectory - (char)('a' - 'A'));
                    }

                    if (char.IsLower(cFullPath))
                    {
                        cFullPath = (char)(cFullPath - (char)('a' - 'A'));
                    }

                    if (cCurrentDirectory != cFullPath)
                    {
                        break;
                    }
                }

                if (cCurrentDirectory == '\\')
                {
                    commonToSlashIndex = i;
                }
            }

            // There is no common prefix between the two paths, we give up.
            if (commonToSlashIndex < 0)
            {
                return path;
            }

            string returnVal = "";
            int nextSlash = commonToSlashIndex;
            for (; ; )
            {
                if (nextSlash >= fullCurrentDirectory.Length)
                {
                    break;
                }

                if (returnVal.Length > 0)
                {
                    returnVal += "\\";
                }

                returnVal += @"..";
                if (nextSlash + 1 == fullCurrentDirectory.Length)
                {
                    break;
                }

                nextSlash = fullCurrentDirectory.IndexOf('\\', nextSlash + 1);
                if (nextSlash < 0)
                {
                    break;
                }
            }

            string rest = fullPath.Substring(commonToSlashIndex + 1);
            returnVal = Path.Combine(returnVal, rest);
            Debug.Assert(string.Compare(Path.GetFullPath(Path.Combine(relativeToDirectory, returnVal)), fullPath, StringComparison.OrdinalIgnoreCase) == 0);
            return returnVal;
        }
    }
}