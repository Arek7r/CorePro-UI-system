#if UNITY_EDITOR

namespace CorePro.UI
{
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Creates a SwitchPro hierarchy identical to the reference object in the Demo scene.
    ///
    /// SwitchPro [160×40]
    ///   Background  - stretch,      UISprite Sliced,    (0.22, 0.22, 0.22)
    ///   Highlight   - stretch,      UISprite Sliced,    white  +  CanvasGroup alpha=0
    ///   Track       - centre+offset (45,0), size(30,15), Background.psd Sliced, (0.151…)
    ///   HandleArea  - centre+offset (45,0), size(40,25), no Image
    ///     Handle    - centre anchor, pos(-15,0), size(20,20), Knob.psd white
    ///   Text(TMP)   - left-centre anchor, pos(15,0), size(80,25), 12 pt
    ///
    /// UISwitchPro: animationMode=GenericHandle, handlePosOff=-15, handlePosOn=15
    /// </summary>
    public class UISwitchProCreator : UnityEditor.Editor
    {
        [MenuItem("GameObject/UI Pro/SwitchPro", false, 8)]
        static void Create(MenuCommand menuCommand)
        {
            GameObject parentCanvas = FindOrCreateCanvas();

            // 1. Root 
            GameObject root = new GameObject("SwitchPro",
                typeof(RectTransform),
                typeof(UISwitchPro));

            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin        = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax        = new Vector2(0.5f, 0.5f);
            rootRT.pivot            = new Vector2(0.5f, 0.5f);
            rootRT.sizeDelta        = new Vector2(160f, 40f);
            rootRT.anchoredPosition = Vector2.zero;

            // 2. Background 
            // stretch, UISprite Sliced, dark grey (0.22)
            GameObject bg = CreateGO("Background", root, typeof(Image));
            Image bgImg = bg.GetComponent<Image>();
            bgImg.sprite        = Spr("UI/Skin/UISprite.psd");
            bgImg.type          = Image.Type.Sliced;
            bgImg.color         = new Color(0.22f, 0.22f, 0.22f, 1f);
            bgImg.raycastTarget = true;
            Stretch(bgImg.rectTransform);

            // 3. Highlight 
            // stretch, UISprite Sliced, white image; CanvasGroup alpha=0
            GameObject hl = CreateGO("Highlight", root, typeof(Image), typeof(CanvasGroup));
            Image hlImg = hl.GetComponent<Image>();
            hlImg.sprite        = Spr("UI/Skin/UISprite.psd");
            hlImg.type          = Image.Type.Sliced;
            hlImg.color         = Color.white;
            hlImg.raycastTarget = false;
            Stretch(hlImg.rectTransform);

            CanvasGroup hlCG = hl.GetComponent<CanvasGroup>();
            hlCG.alpha          = 0f;
            hlCG.interactable   = false;
            hlCG.blocksRaycasts = false;

            // 4. Track 
            // centre anchor, offset (45, 0), size (30×15)
            // Background.psd Sliced, darker grey (0.151)
            GameObject track = CreateGO("Track", root, typeof(Image));
            Image trackImg = track.GetComponent<Image>();
            trackImg.sprite        = Spr("UI/Skin/Background.psd");
            trackImg.type          = Image.Type.Sliced;
            trackImg.color         = new Color(0.1509434f, 0.1509434f, 0.1509434f, 1f);
            trackImg.raycastTarget = true;
            CentreAnchor(trackImg.rectTransform, pos: new Vector2(45f, 0f), size: new Vector2(30f, 15f));

            // 5. HandleArea 
            // centre anchor, offset (45, 0), size (40×25), no Image
            GameObject handleArea = CreateGO("HandleArea", root, typeof(RectTransform));
            RectTransform haRT = handleArea.GetComponent<RectTransform>();
            CentreAnchor(haRT, pos: new Vector2(45f, 0f), size: new Vector2(40f, 25f));

            //   Handle - child of HandleArea
            //   centre anchor, starts at OFF pos (-15, 0), size (20×20), Knob.psd white
            GameObject handle = CreateGO("Handle", handleArea, typeof(Image));
            Image handleImg = handle.GetComponent<Image>();
            handleImg.sprite        = Spr("UI/Skin/Knob.psd");
            handleImg.type          = Image.Type.Simple;
            handleImg.color         = Color.white;
            handleImg.raycastTarget = false;
            RectTransform handleRT = handleImg.rectTransform;
            CentreAnchor(handleRT, pos: new Vector2(-15f, 0f), size: new Vector2(20f, 20f));

            // 6. Text (TMP) 
            // left-centre anchor, pos (15, 0), size (80×25), 12 pt white, centre-middle
            GameObject textGO = CreateGO("Text (TMP)", root, typeof(TextMeshProUGUI));
            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text          = "New Text";
            tmp.fontSize      = 12f;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = true;

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin        = new Vector2(0f, 0.5f);
            textRT.anchorMax        = new Vector2(0f, 0.5f);
            textRT.pivot            = new Vector2(0f, 0.5f);
            textRT.anchoredPosition = new Vector2(15f, 0f);
            textRT.sizeDelta        = new Vector2(80f, 25f);

            // 7. Wire UISwitchPro 
            UISwitchPro sw = root.GetComponent<UISwitchPro>();
            SerializedObject so = new SerializedObject(sw);

            so.FindProperty("handleRect")   .objectReferenceValue = handleRT;
            so.FindProperty("highlightCG")  .objectReferenceValue = hlCG;
            so.FindProperty("handlePosOff") .floatValue           = -15f;
            so.FindProperty("handlePosOn")  .floatValue           =  15f;
            so.FindProperty("animationMode").enumValueIndex       = (int)UISwitchPro.AnimationMode.GenericHandle;
            so.FindProperty("isOn")         .boolValue            = false;

            so.ApplyModifiedPropertiesWithoutUndo();

            // 8. Parent, Undo, Select 
            GameObjectUtility.SetParentAndAlign(
                root,
                menuCommand.context as GameObject ?? parentCanvas);

            Undo.RegisterCreatedObjectUndo(root, "Create SwitchPro");
            Selection.activeObject = root;
        }

        // 
        // Helpers
        // 

        /// <summary>Create a child GO with the given components (skips RectTransform - always added first).</summary>
        static GameObject CreateGO(string name, GameObject parent, params System.Type[] extra)
        {
            // RectTransform is always added implicitly with uGUI objects
            var types = new System.Collections.Generic.List<System.Type> { typeof(RectTransform) };
            foreach (var t in extra)
                if (t != typeof(RectTransform)) types.Add(t);

            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        /// <summary>Anchor stretch to fill parent.</summary>
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Anchor to centre (0.5, 0.5), set pos and size.</summary>
        static void CentreAnchor(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        static Sprite Spr(string path) =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        static GameObject FindOrCreateCanvas()
        {
            Canvas existing;
#if UNITY_6000_0_OR_NEWER
            existing = Object.FindAnyObjectByType<Canvas>();
#else
            existing = Object.FindObjectOfType<Canvas>();
#endif
            if (existing != null)
                return existing.gameObject;

            GameObject canvasGO = new GameObject("Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            return canvasGO;
        }
    }
}
#endif
