#if UNITY_EDITOR
using CorePro.Editor.Framework;
using CorePro.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CorePro.UI.Editor
{
    [CustomEditor(typeof(HorizontalSelectorPro))]
    public class HorizontalSelectorProEditor : CoreProEditor
    {
        // == SerializedProperties ==

        // Animation
        SerializedProperty _selectorAnimator;

        // Labels
        SerializedProperty _labelIncoming;
        SerializedProperty _labelOutgoing;
        SerializedProperty _labelInstant;

        // Icons
        SerializedProperty _iconIncoming;
        SerializedProperty _iconOutgoing;

        // Indicators
        SerializedProperty _indicatorParent;
        SerializedProperty _indicatorPrefab;

        // Navigation Buttons
        SerializedProperty _useNavigationButtons;
        SerializedProperty _prevButton;
        SerializedProperty _nextButton;

        // Saving
        SerializedProperty _saveSelected;
        SerializedProperty _saveKey;

        // Behaviour
        SerializedProperty _invokeOnAwake;
        SerializedProperty _invertAnimation;
        SerializedProperty _loopSelection;

        // Items
        SerializedProperty _defaultIndex;
        SerializedProperty _items;

        // Event
        SerializedProperty _onValueChanged;

        // == Foldouts ==

        bool _foldReferences;
        bool _foldBehaviour;
        bool _foldSaving;
        bool _foldItems;
        bool _foldEvents;

        // == Lifecycle ==

        protected override void OnEnable()
        {
            base.OnEnable();

            _selectorAnimator     = serializedObject.FindProperty("selectorAnimator");
            _labelIncoming        = serializedObject.FindProperty("labelIncoming");
            _labelOutgoing        = serializedObject.FindProperty("labelOutgoing");
            _labelInstant         = serializedObject.FindProperty("labelInstant");
            _iconIncoming         = serializedObject.FindProperty("iconIncoming");
            _iconOutgoing         = serializedObject.FindProperty("iconOutgoing");
            _indicatorParent      = serializedObject.FindProperty("indicatorParent");
            _indicatorPrefab      = serializedObject.FindProperty("indicatorPrefab");
            _useNavigationButtons = serializedObject.FindProperty("useNavigationButtons");
            _prevButton           = serializedObject.FindProperty("prevButton");
            _nextButton           = serializedObject.FindProperty("nextButton");
            _saveSelected         = serializedObject.FindProperty("saveSelected");
            _saveKey              = serializedObject.FindProperty("saveKey");
            _invokeOnAwake        = serializedObject.FindProperty("invokeOnAwake");
            _invertAnimation      = serializedObject.FindProperty("invertAnimation");
            _loopSelection        = serializedObject.FindProperty("loopSelection");
            _defaultIndex         = serializedObject.FindProperty("defaultIndex");
            _items                = serializedObject.FindProperty("items");
            _onValueChanged       = serializedObject.FindProperty("onValueChanged");

            _foldReferences = LoadFoldout("References", true);
            _foldBehaviour  = LoadFoldout("Behaviour",  true);
            _foldSaving     = LoadFoldout("Saving",     false);
            _foldItems      = LoadFoldout("Items",      true);
            _foldEvents     = LoadFoldout("Events",     false);
        }

        // == Main Draw ==

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            HorizontalSelectorPro selector = (HorizontalSelectorPro)target;

            DrawPreview(selector);
            EditorGUILayout.Space(4);

            DrawSection("Items",       ref _foldItems,      "Items",      DrawItems);
            DrawSection("References",  ref _foldReferences, "References", DrawReferences);
            DrawSection("Behaviour",   ref _foldBehaviour,  "Behaviour",  DrawBehaviour);
            DrawSection("Saving",      ref _foldSaving,     "Saving",     DrawSaving);
            DrawSection("Events",      ref _foldEvents,     "Events",     DrawEvents);

            if (Application.isPlaying)
                DrawRuntimeControls(selector);

            serializedObject.ApplyModifiedProperties();
        }

        // == Preview ==

        void DrawPreview(HorizontalSelectorPro selector)
        {
            int count        = _items.arraySize;
            int currentIndex = Application.isPlaying ? selector.Index : _defaultIndex.intValue;
            currentIndex     = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, count - 1));

            string itemTitle = "No Items";
            if (count > 0)
            {
                var titleProp = _items.GetArrayElementAtIndex(currentIndex)?.FindPropertyRelative("itemTitle");
                itemTitle = titleProp != null ? titleProp.stringValue : currentIndex.ToString();
            }

            float btnHeight = Theme.previewBarHeight;

            Rect totalRect = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(btnHeight),
                GUILayout.ExpandWidth(true));
            totalRect = EditorGUI.IndentedRect(totalRect);

            float btnWidth  = btnHeight * 1.6f;
            float gap       = 4f;
            float labelWidth = totalRect.width - btnWidth * 2f - gap * 2f;

            Rect rectPrev  = new Rect(totalRect.x,                              totalRect.y, btnWidth, btnHeight);
            Rect rectLabel = new Rect(totalRect.x + btnWidth + gap,             totalRect.y, labelWidth, btnHeight);
            Rect rectNext  = new Rect(totalRect.xMax - btnWidth,                totalRect.y, btnWidth, btnHeight);

            // Prev button
            bool canPrev = count > 1 && (currentIndex > 0 || _loopSelection.boolValue);
            EditorGUI.DrawRect(rectPrev, canPrev ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.17f, 0.17f, 0.17f, 1f));
            DrawBorder(rectPrev, Theme.previewBarBorder);
            DrawLabelWithShadow(rectPrev, "◀");

            // Label
            string labelText = count > 0 ? $"{itemTitle}  ({currentIndex + 1} / {count})" : "No Items";
            EditorGUI.DrawRect(rectLabel, Theme.previewBarBg);
            DrawBorder(rectLabel, Theme.previewBarBorder);
            DrawLabelWithShadow(rectLabel, labelText);

            // Next button
            bool canNext = count > 1 && (currentIndex < count - 1 || _loopSelection.boolValue);
            EditorGUI.DrawRect(rectNext, canNext ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.17f, 0.17f, 0.17f, 1f));
            DrawBorder(rectNext, Theme.previewBarBorder);
            DrawLabelWithShadow(rectNext, "▶");

            // Click handling
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (canPrev && rectPrev.Contains(Event.current.mousePosition))
                {
                    if (Application.isPlaying)
                        selector.PreviousItem();
                    else
                    {
                        int oldIdx = _defaultIndex.intValue;
                        int newIdx = oldIdx - 1;
                        if (_loopSelection.boolValue && newIdx < 0) newIdx = count - 1;
                        newIdx = Mathf.Clamp(newIdx, 0, count - 1);
                        _defaultIndex.intValue = newIdx;
                        ApplyEditModeSelection(selector, oldIdx, newIdx);
                    }
                    Event.current.Use();
                    Repaint();
                }
                else if (canNext && rectNext.Contains(Event.current.mousePosition))
                {
                    if (Application.isPlaying)
                        selector.NextItem();
                    else
                    {
                        int oldIdx = _defaultIndex.intValue;
                        int newIdx = oldIdx + 1;
                        if (_loopSelection.boolValue && newIdx >= count)
                            newIdx = 0;
                        newIdx = Mathf.Clamp(newIdx, 0, count - 1);
                        _defaultIndex.intValue = newIdx;
                        ApplyEditModeSelection(selector, oldIdx, newIdx);
                    }
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        void ApplyEditModeSelection(HorizontalSelectorPro selector, int oldIndex, int newIndex)
        {
            if (_items.arraySize == 0)
                return;

            oldIndex = Mathf.Clamp(oldIndex, 0, _items.arraySize - 1);
            newIndex = Mathf.Clamp(newIndex, 0, _items.arraySize - 1);

            if (oldIndex != newIndex)
            {
                var oldEntry = _items.GetArrayElementAtIndex(oldIndex);
                var oldAnimatorProp = oldEntry?.FindPropertyRelative("animator");
                Animator oldAnimator = oldAnimatorProp?.objectReferenceValue as Animator;

                if (oldAnimator != null && oldAnimator.gameObject != null)
                {
                    EditorUtility.SetDirty(oldAnimator.gameObject);
                    oldAnimator.gameObject.SetActive(false);
                }
            }

            var newEntry = _items.GetArrayElementAtIndex(newIndex);
            var newAnimatorProp = newEntry?.FindPropertyRelative("animator");
            Animator newAnimator = newAnimatorProp?.objectReferenceValue as Animator;

            if (newAnimator != null && newAnimator.gameObject != null)
            {
                EditorUtility.SetDirty(newAnimator.gameObject);
                newAnimator.gameObject.SetActive(true);
            }

            var titleProp = newEntry?.FindPropertyRelative("itemTitle");
            var iconProp = newEntry?.FindPropertyRelative("itemIcon");

            var labelIncomingObj = _labelIncoming.objectReferenceValue as TextMeshProUGUI;
            var labelOutgoingObj = _labelOutgoing.objectReferenceValue as TextMeshProUGUI;
            var labelInstantObj = _labelInstant.objectReferenceValue as TextMeshProUGUI;
            var iconIncomingObj = _iconIncoming.objectReferenceValue as Image;
            var iconOutgoingObj = _iconOutgoing.objectReferenceValue as Image;

            if (labelIncomingObj != null && titleProp != null)
            {
                EditorUtility.SetDirty(labelIncomingObj);
                labelIncomingObj.text = titleProp.stringValue;
            }

            if (labelOutgoingObj != null && titleProp != null)
            {
                EditorUtility.SetDirty(labelOutgoingObj);
                labelOutgoingObj.text = titleProp.stringValue;
            }

            if (labelInstantObj != null && titleProp != null)
            {
                EditorUtility.SetDirty(labelInstantObj);
                labelInstantObj.text = titleProp.stringValue;
            }

            if (iconIncomingObj != null && iconProp != null)
            {
                EditorUtility.SetDirty(iconIncomingObj);
                iconIncomingObj.sprite = iconProp.objectReferenceValue as Sprite;
            }

            if (iconOutgoingObj != null && iconProp != null)
            {
                EditorUtility.SetDirty(iconOutgoingObj);
                iconOutgoingObj.sprite = iconProp.objectReferenceValue as Sprite;
            }

            UpdateIndicatorsEditMode(selector);
            EditorUtility.SetDirty(selector);
        }

        void UpdateIndicatorsEditMode(HorizontalSelectorPro selector)
        {
            var indicatorParentObj = _indicatorParent.objectReferenceValue as Transform;
            var indicatorPrefabObj = _indicatorPrefab.objectReferenceValue as SelectorIndicator;

            if (indicatorParentObj == null || indicatorPrefabObj == null)
                return;

            int itemCount = _items.arraySize;
            int currentIndex = _defaultIndex.intValue;

            for (int i = 0; i < indicatorParentObj.childCount; i++)
            {
                var indicator = indicatorParentObj.GetChild(i).GetComponent<SelectorIndicator>();
                if (indicator != null)
                {
                    EditorUtility.SetDirty(indicator.gameObject);
                    indicator.SetActive(i == currentIndex);
                }
            }
        }

        // == Section Bodies ==

        void DrawReferences()
        {
            DrawSubHeader("Animation");
            EditorGUILayout.PropertyField(_selectorAnimator, new GUIContent("Selector Animator",
                "Optional. Plays Next/Prev transition states."));

            EditorGUILayout.Space(2);
            DrawSubHeader("Labels");
            EditorGUILayout.PropertyField(_labelIncoming, new GUIContent("Label Incoming",
                "Displays the incoming item title during transition."));
            EditorGUILayout.PropertyField(_labelOutgoing, new GUIContent("Label Outgoing",
                "Displays the outgoing item title during transition."));
            EditorGUILayout.PropertyField(_labelInstant,  new GUIContent("Label Instant",
                "Updated instantly without animation. Optional."));

            EditorGUILayout.Space(2);
            DrawSubHeader("Icons");
            EditorGUILayout.PropertyField(_iconIncoming, new GUIContent("Icon Incoming"));
            EditorGUILayout.PropertyField(_iconOutgoing, new GUIContent("Icon Outgoing"));

            EditorGUILayout.Space(2);
            DrawSubHeader("Indicators");
            EditorGUILayout.PropertyField(_indicatorParent, new GUIContent("Indicator Parent",
                "Container where indicator dots are spawned."));
            EditorGUILayout.PropertyField(_indicatorPrefab, new GUIContent("Indicator Prefab",
                "SelectorIndicator prefab instantiated once per item."));

            bool hasParent = _indicatorParent.objectReferenceValue != null;
            bool hasPrefab = _indicatorPrefab.objectReferenceValue != null;
            if (hasParent != hasPrefab)
                EditorGUILayout.HelpBox("Both Indicator Parent and Indicator Prefab must be assigned for indicators to work.", MessageType.Warning);

            EditorGUILayout.Space(2);
            DrawSubHeader("Navigation Buttons");
            EditorGUILayout.PropertyField(_useNavigationButtons, new GUIContent("Use Navigation Buttons"));

            if (_useNavigationButtons.boolValue)
            {
                EditorGUILayout.PropertyField(_prevButton, new GUIContent("Prev Button"));
                EditorGUILayout.PropertyField(_nextButton, new GUIContent("Next Button"));

                if (_prevButton.objectReferenceValue == null || _nextButton.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Prev Button and Next Button must both be assigned when Use Navigation Buttons is enabled.", MessageType.Warning);
            }
        }

        void DrawBehaviour()
        {
            EditorGUILayout.PropertyField(_invokeOnAwake, new GUIContent("Invoke On Awake",
                "Fires onValueChanged and onItemSelect for the default item on Awake."));
            EditorGUILayout.PropertyField(_loopSelection, new GUIContent("Loop Selection",
                "Wraps selection from the last item back to the first and vice versa."));
            EditorGUILayout.PropertyField(_invertAnimation, new GUIContent("Invert Animation",
                "Swaps the Prev and Next transition animations."));
        }

        void DrawSaving()
        {
            EditorGUILayout.PropertyField(_saveSelected, new GUIContent("Save Selected",
                "Persists the selected index in PlayerPrefs between sessions."));

            if (_saveSelected.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_saveKey, new GUIContent("Save Key",
                    "Unique key used to persist the selected index. Must be unique across the project."));
                if (EditorGUI.EndChangeCheck() && string.IsNullOrWhiteSpace(_saveKey.stringValue))
                    _saveKey.stringValue = "Selector";

                EditorGUILayout.HelpBox($"PlayerPrefs key: \"HorizontalSelector_{_saveKey.stringValue}\"", MessageType.None);

                EditorGUI.indentLevel--;

                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(2);
                    if (GUILayout.Button("Clear Saved Selection"))
                    {
                        var method = typeof(HorizontalSelectorPro).GetMethod(
                            "ClearSavedSelection",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(target, null);
                    }
                }
            }
        }

        void DrawItems()
        {
            int count = _items.arraySize;
            HorizontalSelectorPro selector = (HorizontalSelectorPro)target;

            // Default index slider
            EditorGUI.BeginChangeCheck();
            int newDefault = EditorGUILayout.IntSlider(
                new GUIContent("Default Index", "Index of the item selected on Awake."),
                _defaultIndex.intValue, 0, Mathf.Max(0, count - 1));
            if (EditorGUI.EndChangeCheck())
            {
                int oldDefault = _defaultIndex.intValue;
                _defaultIndex.intValue = newDefault;
                if (!Application.isPlaying)
                    ApplyEditModeSelection(selector, oldDefault, newDefault);
            }

            if (count > 0)
            {
                var entry    = _items.GetArrayElementAtIndex(Mathf.Clamp(newDefault, 0, count - 1));
                var titleProp = entry?.FindPropertyRelative("itemTitle");
                if (titleProp != null)
                    EditorGUILayout.HelpBox($"Default item: \"{titleProp.stringValue}\"", MessageType.None);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_items, new GUIContent("Items"), true);

            if (count == 0)
                EditorGUILayout.HelpBox("No items added. The selector will not function.", MessageType.Warning);

            // Edit Mode quick-add / remove buttons
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Add Item"))
                {
                    _items.InsertArrayElementAtIndex(count);
                    var newItem = _items.GetArrayElementAtIndex(count);
                    newItem.FindPropertyRelative("itemTitle").stringValue = $"Item {count + 1}";
                }

                using (new EditorGUI.DisabledScope(count == 0))
                    if (GUILayout.Button("Remove Last"))
                        _items.DeleteArrayElementAtIndex(count - 1);

                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawEvents()
        {
            EditorGUILayout.PropertyField(_onValueChanged, new GUIContent("On Value Changed (int)"));

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox("Per-item events (onItemSelect) are configured in each Item entry in the Items section.", MessageType.None);
        }

        // == Runtime Controls ==

        void DrawRuntimeControls(HorizontalSelectorPro selector)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            int count = selector.Items.Count;

            EditorGUILayout.LabelField(
                $"Index: {selector.Index}   Item: \"{(count > 0 ? selector.Items[selector.Index].itemTitle : "none")}\"   Total: {count}",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(selector.Index <= 0 && !_loopSelection.boolValue))
                if (GUILayout.Button("Previous"))
                    selector.PreviousItem();

            using (new EditorGUI.DisabledScope(selector.Index >= count - 1 && !_loopSelection.boolValue))
                if (GUILayout.Button("Next"))
                    selector.NextItem();

            EditorGUILayout.EndHorizontal();

            // Quick-select by index buttons (max 10 shown to avoid clutter)
            if (count > 1 && count <= 20)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Jump to Item", EditorStyles.miniLabel);

                int cols = 5;
                for (int row = 0; row * cols < count; row++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int col = 0; col < cols; col++)
                    {
                        int idx = row * cols + col;
                        if (idx >= count) break;

                        using (new EditorGUI.DisabledScope(idx == selector.Index))
                            if (GUILayout.Button(selector.Items[idx].itemTitle, GUILayout.MinWidth(40)))
                                selector.SetIndex(idx);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        // == Helpers ==

        private new void DrawSubHeader(string label)
        {
            EditorGUILayout.Space(3);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax + 1f, r.width, 1f), new Color(0.35f, 0.35f, 0.35f, 0.6f));
            EditorGUILayout.Space(2);
        }
    }
}
#endif