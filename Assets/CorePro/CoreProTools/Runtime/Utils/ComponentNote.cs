using UnityEngine;

namespace CorePro.EditorTools
{
    /// <summary>
    /// Component for displaying notes in the Inspector.
    /// Useful for reminding you about necessary settings, TODOs, warnings, etc.
    /// </summary>
    public class ComponentNote : MonoBehaviour
    {
        [Tooltip("Enable to allow editing the note field")]
        public bool edit = false;
        [TextArea(5, 10)]
        public string note = "Enter your note here...";
    }
}