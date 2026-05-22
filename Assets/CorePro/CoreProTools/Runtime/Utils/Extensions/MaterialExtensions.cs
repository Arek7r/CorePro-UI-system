#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CorePro.EditorTools
{
    public static class MaterialExtensions
    {
        /// <summary>
        /// Creates and saves a unique copy of this material to a given folder.
        /// </summary>
        /// <param name="original">The original material to copy.</param>
        /// <param name="targetFolder">Folder path relative to the project root (e.g. "Assets/GeneratedMaterials").</param>
        /// <param name="newName">Optional new material name (without .mat extension).</param>
        /// <param name="assignToRenderer">Optional MeshRenderer to auto-assign the new material to.</param>
        /// <returns>The newly created material asset, or null if failed.</returns>
        public static Material GenerateUniqueMaterial(
            this Material original,
            string targetFolder = "Assets/GeneratedMaterials",
            string newName = null,
            MeshRenderer assignToRenderer = null)
        {
            if (original == null)
            {
                Debug.LogWarning("Cannot duplicate material: original is null.");
                return null;
            }

            // Ensure directory exists
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            // Determine new material name
            string matName = string.IsNullOrEmpty(newName)
                ? $"Copy of {original.name}"
                : newName;

            // Create material instance
            Material newMat = new Material(original);

            // Build asset path
            string assetPath = Path.Combine(targetFolder, $"{matName}.mat").Replace("\\", "/");

            // Save and refresh
            AssetDatabase.CreateAsset(newMat, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Assign to renderer if provided
            if (assignToRenderer != null)
            {
                assignToRenderer.sharedMaterial = newMat;
                EditorUtility.SetDirty(assignToRenderer);
            }
            
            // Ping in Project window
            EditorGUIUtility.PingObject(newMat);
            Selection.activeObject = newMat;

            Debug.Log($"Created unique material: {assetPath}");
            return newMat;
        }
    }
}
#endif
