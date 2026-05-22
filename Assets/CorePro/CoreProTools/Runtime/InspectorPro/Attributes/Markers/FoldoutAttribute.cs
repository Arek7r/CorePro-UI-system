using UnityEngine;

namespace InspectorPro
{
    /// <summary>
    /// Groups fields in the Unity Inspector under a foldout section.
    /// </summary>
    /// <remarks>
    /// Usage examples:
    /// <code>
    /// [Foldout("General Settings")]
    /// public float moveSpeed;
    /// 
    /// [Foldout("Combat Settings")]
    /// public int damage;
    /// 
    /// // Group multiple fields under the same foldout
    /// [Foldout("Audio", foldEverything: true)]
    /// public AudioClip shootSound;
    /// public AudioClip reloadSound; // Will be included in "Audio" foldout
    /// 
    /// // Control order of foldouts
    /// [Foldout("Important", order: 0)] // Will appear first
    /// public bool enabled;
    /// </code>
    /// </remarks>
    public class FoldoutAttribute : PropertyAttribute
    {
        public string name;
        public bool foldEverything;

        public FoldoutAttribute(string name, bool foldEverything = false, int order = 100)
        {
            this.name = name;
            this.foldEverything = foldEverything;
            this.order = order;
        }
    }
}