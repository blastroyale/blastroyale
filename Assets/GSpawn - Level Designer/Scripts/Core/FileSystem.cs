#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public static class FileSystem
    {
        public static bool folderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return false;
            return Directory.Exists(folderPath);
        }

        public static bool fileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            return File.Exists(filePath);
        }

        public static void createFolder(string folderPath)
        {
            Directory.CreateDirectory(folderPath);
        }

        public static string findFolderPath(string folderName)
        {
            string[] guids = AssetDatabase.FindAssets(folderName);
            foreach (var guid in guids)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(folderPath)) return folderPath;
            }

            return string.Empty;
        }

        public static List<string> findFolderNamesInPath(string folderPath)
        {
            string[] folderNames = folderPath.Split(new string[] { @"/", @"\" }, System.StringSplitOptions.RemoveEmptyEntries);
            return new List<string>(folderNames);
        }

        public static string findLastFolderNameInPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return string.Empty;
            return new DirectoryInfo(folderPath).Name;
        }

        public static List<string> findAllFilesInFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return new List<string>();
            return new List<string>(Directory.GetFiles(folderPath));
        }

        public static List<string> findImmediateChildFolders(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return new List<string>();
            return new List<string>(Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly));
        }

        public static List<string> findAllFolders(string folderPath, bool includeSelf)
        {
            if (!Directory.Exists(folderPath)) return new List<string>();

            var allFolders = new List<string>();
            if (includeSelf) allFolders.Add(folderPath);

            try
            {
                var folders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);
                if (folders.Length != 0) allFolders.AddRange(folders);
            }
            catch (UnauthorizedAccessException)
            {
                return allFolders;
            }

            return allFolders;
        }

        public static bool isFolder(string folderPath)
        {
            return Directory.Exists(folderPath);
        }

        public static List<string> filterRootFolders(IEnumerable<string> folderPaths)
        {
            List<string> paths = new List<string>(folderPaths);
            paths.RemoveAll(item => !isFolder(item));
            if (paths.Count == 0) return paths;

            for (int pathIndex = 0; pathIndex < paths.Count; )
            {
                string path = Path.GetFullPath(paths[pathIndex]) + Path.DirectorySeparatorChar;
                bool isChild = false;

                for (int otherPathIndex = 0; otherPathIndex < paths.Count; ++otherPathIndex)
                {
                    if (otherPathIndex != pathIndex)
                    {
                        string otherPath = Path.GetFullPath(paths[otherPathIndex]) + Path.DirectorySeparatorChar;
                        if (path.StartsWith(otherPath))
                        {
                            isChild = true;
                            break;
                        }
                    }
                }

                if (isChild)
                {
                    paths.RemoveAt(pathIndex);
                    continue;
                }
                else ++pathIndex;
            }

            return paths;
        }
    }
}
#endif