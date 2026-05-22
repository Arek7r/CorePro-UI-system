using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace CorePro.Favorites
{
    [System.Serializable]
    public class FavoriteEntry
    {
        public string guid; // for assets
        public string globalObjectId; // for scene objects
        public string sceneObjectName; // cached name for placeholder
        public string typeName; // cached type for placeholder icons
        public string category = "Default"; // category (future use)

        [System.NonSerialized]
        public Object cached; // cached ref
    }


    public sealed class FavoriteWindow : EditorWindow
    {
        private Vector2 scroll;
        private GUIStyle labelStyle;
        private int selectedIndex = -1;

        private ReorderableList reorderableList;

        [MenuItem("Window/CorePro/Favorites")]
        private static void Open()
        {
            GetWindow<FavoriteWindow>("Favorites");
        }

        /// <summary>
        /// Called when the window is enabled. Initializes styles and the reorderable list.
        /// </summary>
        private void OnEnable()
        {
            var db = FavoriteDatabase.Instance;

            reorderableList = new ReorderableList(db.entries, typeof(FavoriteEntry), true, false, false, false);

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= db.entries.Count)
                    return;

                DrawEntry(db.entries[index], index, rect);
            };

            reorderableList.onReorderCallback = (ReorderableList list) => { FavoriteDatabase.Save(); };
        }

        /// <summary>
        /// Main GUI loop for the Favorites window.
        /// </summary>
        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.alignment = TextAnchor.MiddleLeft;
            }

            HandleDragAndDrop();

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = scrollScope.scrollPosition;

                if (reorderableList != null)
                    reorderableList.DoLayoutList();
            }
        }

        /// <summary>
        /// Draws a single favorite entry row.
        /// </summary>
        /// <param name="entry">Favorite entry to draw.</param>
        /// <param name="index">Index in the list.</param>
        /// <param name="rect">Row rect to draw into.</param>
        private void DrawEntry(FavoriteEntry entry, int index, Rect rect)
        {
            // Important: ResolveObject is called here. If it returns null due to the new 
            // safety check above, 'obj' will be null and we avoid the warning.
            Object obj = ResolveObject(entry);
            bool isSceneObject = string.IsNullOrEmpty(entry.globalObjectId) == false;
            bool isUnavailable = obj == null && isSceneObject;

            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 70, rect.height);
            Rect pingRect = new Rect(rect.xMax - 65, rect.y, 40, rect.height);
            Rect removeRect = new Rect(rect.xMax - 20, rect.y, 20, rect.height);

            // Your original Selection/Hover logic
            if (index == selectedIndex)
            {
                Color selectedColor = EditorGUIUtility.isProSkin
                    ? new Color(0.24f, 0.48f, 0.90f, 0.6f)
                    : new Color(0.24f, 0.48f, 0.90f, 0.9f);
                EditorGUI.DrawRect(rect, selectedColor);
            }
            else if (rect.Contains(Event.current.mousePosition))
            {
                Color hoverColor = EditorGUIUtility.isProSkin
                    ? new Color(0.24f, 0.48f, 0.90f, 0.3f)
                    : new Color(0.24f, 0.48f, 0.90f, 0.6f);
                EditorGUI.DrawRect(rect, hoverColor);
            }

            // FEEDBACK: Gray out the row if it's a scene object not in the current scene
            if (isUnavailable) GUI.enabled = false;

            if (obj != null)
            {
                string tooltip = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(tooltip)) tooltip = "Scene object";

                GUIContent content = EditorGUIUtility.ObjectContent(obj, obj.GetType());
                content.tooltip = tooltip;
                EditorGUI.LabelField(labelRect, content, labelStyle);
            }
            else if (isSceneObject)
            {
                // Visual feedback for user
                Texture icon = GetIconForType(entry.typeName);
                string displayName = string.IsNullOrEmpty(entry.sceneObjectName)
                    ? "(Not in current scene)"
                    : $"[OTHER SCENE] {entry.sceneObjectName}";

                GUIContent placeholder = new GUIContent(displayName, icon, "This object belongs to another scene or is in DontDestroyOnLoad.");
                EditorGUI.LabelField(labelRect, placeholder, labelStyle);
            }
            else
            {
                EditorGUI.LabelField(labelRect, new GUIContent("(Missing Asset)"), labelStyle);
            }

            if (GUI.Button(pingRect, "Ping"))
            {
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }

            // Re-enable GUI so the 'X' button and context menu work properly
            if (isUnavailable) GUI.enabled = true;

            if (GUI.Button(removeRect, "X"))
            {
                FavoriteDatabase.Instance.entries.RemoveAt(index);
                FavoriteDatabase.Save();
                GUIUtility.ExitGUI();
            }

            Event evt = Event.current;

            // Original Mouse Logic
            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition) && evt.button == 0)
            {
                selectedIndex = index;
                Repaint();

                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    if (evt.clickCount == 2) HandleDoubleClick(obj);

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { obj };
                    DragAndDrop.StartDrag(obj.name);
                    evt.Use();
                }
            }

            // Original Context Menu Logic
            if (evt.type == EventType.ContextClick && rect.Contains(evt.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                if (obj != null)
                {
                    menu.AddItem(new GUIContent("Ping"), false, () => EditorGUIUtility.PingObject(obj));
                    menu.AddItem(new GUIContent("Open"), false, () => AssetDatabase.OpenAsset(obj));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Object unavailable in this scene"));
                }

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    FavoriteDatabase.Instance.entries.RemoveAt(index);
                    FavoriteDatabase.Save();
                });
                menu.ShowAsContext();
                evt.Use();
            }
        }

        /// <summary>
        /// Handles double-click behavior: opens scenes or standard assets.
        /// </summary>
        private void HandleDoubleClick(Object obj)
        {
            if (obj == null) return;

            // If it's a Scene asset, open it
            if (obj is SceneAsset)
            {
                string scenePath = AssetDatabase.GetAssetPath(obj);
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
            else
            {
                // For other assets (Scripts, Prefabs, Materials), use the standard open method
                AssetDatabase.OpenAsset(obj);
            }
        }

        /// <summary>
        /// Resolves a FavoriteEntry to an actual Unity Object.
        /// Performs cleanup if the referenced asset no longer exists.
        /// </summary>
        /// <param name="entry">Favorite entry to resolve.</param>
        /// <returns>Resolved Object or null if not available.</returns>
        private static Object ResolveObject(FavoriteEntry entry)
        {
            if (entry.cached != null)
                return entry.cached;

            Object result = null;

            if (string.IsNullOrEmpty(entry.guid) == false)
            {
                string path = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (string.IsNullOrEmpty(path) == false)
                {
                    result = AssetDatabase.LoadAssetAtPath<Object>(path);
                }
                else
                {
                    // Remove missing assets, but keep scene objects (which don't have entry.guid)
                    FavoriteDatabase.Instance.entries.Remove(entry);
                    FavoriteDatabase.Save();
                    return null;
                }
            }
            else if (string.IsNullOrEmpty(entry.globalObjectId) == false)
            {
                if (GlobalObjectId.TryParse(entry.globalObjectId, out GlobalObjectId gid))
                {
                    // 1. Skip if it's a null/invalid identifier type
                    if (gid.identifierType == 0) return null;

                    // 2. Check the AssetGUID (the scene GUID for scene objects)
                    // If it's all zeros, it's a non-persistent object (like DontDestroyOnLoad)
                    // Calling ToObjectSlow on these will always trigger the "Cross scene references" warning.
                    string sceneGuidStr = gid.assetGUID.ToString();
                    if (sceneGuidStr == "00000000000000000000000000000000")
                    {
                        return null;
                    }

                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuidStr);
                    if (string.IsNullOrEmpty(scenePath))
                    {
                        return null;
                    }

                    // 3. Only attempt resolution if the target scene is actually loaded
                    bool isSceneLoaded = false;
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    {
                        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                        if (scene.path == scenePath && scene.isLoaded)
                        {
                            isSceneLoaded = true;
                            break;
                        }
                    }

                    if (isSceneLoaded)
                    {
                        try
                        {
                            // This is the "dangerous" line. By now we've filtered out DDOL and missing scenes.
                            result = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
            }

            if (result != null)
                entry.cached = result;

            return result;
        }

        /// <summary>
        /// Handles drag & drop into the Favorites window, adding dropped assets or scene objects to the database.
        /// </summary>
        private void HandleDragAndDrop()
        {
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag assets or objects here");

            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj == null) continue;

                        FavoriteEntry entry = new FavoriteEntry();

                        string path = AssetDatabase.GetAssetPath(obj);
                        if (string.IsNullOrEmpty(path) == false)
                        {
                            entry.guid = AssetDatabase.AssetPathToGUID(path);
                            entry.typeName = obj.GetType().AssemblyQualifiedName;
                        }
                        else
                        {
                            entry.globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
                            entry.sceneObjectName = obj.name;
                            entry.typeName = obj.GetType().AssemblyQualifiedName;
                        }

                        FavoriteDatabase.Instance.entries.Add(entry);
                    }

                    FavoriteDatabase.Save();
                }

                evt.Use();
            }
        }

        /// <summary>
        /// Returns an icon texture for the given type name, or a default GameObject icon if the type cannot be resolved.
        /// </summary>
        /// <param name="typeName">Assembly-qualified type name.</param>
        /// <returns>Icon texture for the type.</returns>
        private static Texture GetIconForType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return EditorGUIUtility.IconContent("GameObject Icon").image;

            System.Type type = System.Type.GetType(typeName);
            if (type != null)
            {
                GUIContent content = EditorGUIUtility.ObjectContent(null, type);
                if (content != null && content.image != null)
                    return content.image;
            }

            return EditorGUIUtility.IconContent("GameObject Icon").image;
        }
    }
}