#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CorePro.UI.Editor
{
    // UIStyleImage inspector 

    [CustomEditor(typeof(UIStyleImage))]
    public class UIStyleImageEditor : UIStyleBaseEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawStyleSheetField();

            var sheet = GetSheet();
            DrawColorSlotDropdown("colorSlot", sheet);
            DrawBoolField("preserveAlpha");
            DrawBoolField("useCustomColor");

            serializedObject.ApplyModifiedProperties();
            ApplyIfChanged(sheet);
        }
    }

    // UIStyleText inspector 

    [CustomEditor(typeof(UIStyleText))]
    public class UIStyleTextEditor : UIStyleBaseEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawStyleSheetField();

            var sheet = GetSheet();

            DrawFontSlotDropdown("fontSlot", sheet);
            EditorGUILayout.Space();
            DrawFontColorSlotDropdown("fontColorSlot", sheet);
            EditorGUILayout.Space();

            DrawBoolField("preserveAlpha");
            DrawBoolField("useCustomColor");
            DrawBoolField("useCustomFont");

            DrawBoolField("useCustomFontSize");

            serializedObject.ApplyModifiedProperties();
            ApplyIfChanged(sheet);
        }
    }

    // Shared base 

    public abstract class UIStyleBaseEditor : UnityEditor.Editor
    {
        private bool _changed;

        protected void DrawStyleSheetField()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("styleSheet"));
            if (EditorGUI.EndChangeCheck()) _changed = true;
        }

        protected UIStyleSheet GetSheet()
            => serializedObject.FindProperty("styleSheet").objectReferenceValue as UIStyleSheet;

        /// <summary>
        /// Draws a named dropdown for the color slot with a read-only color preview.
        /// The preview reflects the currently active theme so you see the real color
        /// the component will use at runtime.
        /// </summary>
        protected void DrawColorSlotDropdown(string propName, UIStyleSheet sheet)
        {
            var sp = serializedObject.FindProperty(propName);
            EditorGUI.BeginChangeCheck();

            if (sheet == null || sheet.Colors.Count == 0)
            {
                EditorGUILayout.PropertyField(sp, new GUIContent("Color Slot (no sheet)"));
            }
            else
            {
                var    colors  = sheet.Colors;
                string[] names = BuildNames(colors, e => e.name, e => e.stableId, "Color");
                int storedId   = sp.intValue;
                int current    = IdToPopupIndex(colors, e => e.stableId, storedId);
                int next       = EditorGUILayout.Popup("Color Slot", current, names);
                if (next != current) sp.intValue = colors[next].stableId;

                // Preview shows the active-theme color, not the slot default
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ColorField(GUIContent.none, sheet.GetColor(sp.intValue), false, true, false);
            }

            if (EditorGUI.EndChangeCheck()) _changed = true;
        }

        /// <summary>
        /// Draws a named dropdown for the font color slot with a read-only color preview.
        /// Uses sheet.FontColors – separate from UI colors.
        /// </summary>
        protected void DrawFontColorSlotDropdown(string propName, UIStyleSheet sheet)
        {
            var sp = serializedObject.FindProperty(propName);
            EditorGUI.BeginChangeCheck();

            if (sheet == null || sheet.FontColors.Count == 0)
            {
                EditorGUILayout.PropertyField(sp, new GUIContent("Font Color Slot (no sheet)"));
            }
            else
            {
                var    fontColors = sheet.FontColors;
                string[] names    = BuildNames(fontColors, e => e.name, e => e.stableId, "FontColor");
                int storedId      = sp.intValue;
                int current       = IdToPopupIndex(fontColors, e => e.stableId, storedId);
                int next          = EditorGUILayout.Popup("Font Color Slot", current, names);
                if (next != current) sp.intValue = fontColors[next].stableId;

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ColorField(GUIContent.none, sheet.GetFontColor(sp.intValue), false, true, false);
            }

            if (EditorGUI.EndChangeCheck()) _changed = true;
        }

        /// <summary>
        /// Draws a named dropdown for the font slot with a read-only size preview.
        /// </summary>
        protected void DrawFontSlotDropdown(string propName, UIStyleSheet sheet)
        {
            var sp = serializedObject.FindProperty(propName);
            EditorGUI.BeginChangeCheck();

            if (sheet == null || sheet.Fonts.Count == 0)
            {
                EditorGUILayout.PropertyField(sp, new GUIContent("Font Slot (no sheet)"));
            }
            else
            {
                var    fonts   = sheet.Fonts;
                string[] names = BuildNames(fonts, e => e.name, e => e.stableId, "Font");
                int storedId   = sp.intValue;
                int current    = IdToPopupIndex(fonts, e => e.stableId, storedId);
                int next       = EditorGUILayout.Popup("Font Slot", current, names);
                if (next != current) sp.intValue = fonts[next].stableId;

                if (sheet.TryGetFont(sp.intValue, out _, out float size))
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.FloatField("  └ Size", size);
                }
            }

            if (EditorGUI.EndChangeCheck()) _changed = true;
        }

        protected void DrawBoolField(string propName)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propName));
            if (EditorGUI.EndChangeCheck()) _changed = true;
        }

        /// <summary>
        /// Re-applies the style on the target component when any field changed
        /// this inspector frame, then resets the dirty flag.
        /// </summary>
        protected void ApplyIfChanged(UIStyleSheet sheet)
        {
            if (!_changed) return;
            _changed = false;
            if (sheet == null) return;
            ((UIStyle)target).ForceApplyInEditor(sheet);
        }

        // Returns popup index (list position) of the entry whose stableId matches storedId.
        // Falls back to 0 when not found (e.g. after a slot was deleted).
        private static int IdToPopupIndex<T>(
            System.Collections.Generic.IReadOnlyList<T> list,
            System.Func<T, int> getId,
            int storedId)
        {
            for (int i = 0; i < list.Count; i++)
                if (getId(list[i]) == storedId) return i;
            return 0;
        }

        private static string[] BuildNames<T>(
            System.Collections.Generic.IReadOnlyList<T> list,
            System.Func<T, string> getName,
            System.Func<T, int>    getId,
            string prefix)
        {
            var result = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                string n  = getName(list[i]);
                int    id = getId(list[i]);
                result[i] = string.IsNullOrWhiteSpace(n) ? $"{prefix} {id}" : $"{id}: {n}";
            }
            return result;
        }
    }
}
#endif
