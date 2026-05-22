using System.IO;
using UnityEngine;

namespace CorePro.Tools
{
    public static class FilePathUtils
    {
        /// <summary>
        /// Ensures that all directories in the given path exist. 
        /// If any part of the path doesn't exist, it will be created.
        /// </summary>
        public static void EnsureDirectoryExists(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
                return;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"Created missing directories for path: {directory}");
            }
        }

        /// <summary>
        /// Checks if a file already exists and logs a warning.
        /// Returns true if file exists.
        /// </summary>
        public static bool CheckFileExists(string path)
        {
            if (File.Exists(path))
            {
                Debug.LogWarning($"File already exists and will be overwritten: {path}");
                return true;
            }
            return false;
        }
    }
}