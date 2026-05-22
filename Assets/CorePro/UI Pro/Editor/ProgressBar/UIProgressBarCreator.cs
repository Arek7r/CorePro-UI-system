#if UNITY_EDITOR

namespace CorePro.UI
{
    using System.IO;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Creates a ProgressBar hierarchy identical to the reference object in the Demo scene.
    ///
    /// ProgressBar [150×10]
    ///   Background      - stretch, Image white simple (no sprite), raycastTarget=false
    ///   Vallue Main     - stretch+offset (1.61 pos, −3.22 delta), TMP white 10pt Center, raycastTarget=false
    ///   Fill            - stretch, Image green (0.2,0.7,0.2) Filled-Horizontal white_pixel + Mask
    ///     Text background - stretch+offset (1.61 pos, −3.22 delta), TMP black 10pt Center, raycastTarget=false
    ///
    /// How the dual-text trick works:
    ///   "Vallue Main"    (white text) sits over the full bar - invisible on white background,
    ///                    visible on the green fill.
    ///   "Text background" (black text) is a child of Fill with a Mask component -
    ///                    visible only within the filled area (green region).
    ///   Net result: black text on green fill, white text on white background (appears hidden).
    ///
    /// UIProgressBar wired:
    ///   mainFill   = Fill Image
    ///   background = Background Image
    ///   valueText  = Vallue Main TMP
    ///   valueText2 = Text background TMP
    ///   solidColor = (0.2, 0.7, 0.2)
    ///   textMode   = Percentage
    ///   initialValue = initialMaxValue = currentValue = 100
    /// </summary>
    public class UIProgressBarCreator : UnityEditor.Editor
    {
        [MenuItem("GameObject/UI Pro/ProgressBar", false, 9)]
        static void Create(MenuCommand menuCommand)
        {
            Sprite whitePixel = LoadOrCreateWhitePixel();
            if (whitePixel == null)
            {
                Debug.LogError("[UIProgressBarCreator] Could not load or create white_pixel sprite.");
                return;
            }

            // 1. Root 
            GameObject root = new GameObject("ProgressBar",
                typeof(RectTransform),
                typeof(UIProgressBar));

            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin        = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax        = new Vector2(0.5f, 0.5f);
            rootRT.pivot            = new Vector2(0.5f, 0.5f);
            rootRT.sizeDelta        = new Vector2(150f, 10f);
            rootRT.anchoredPosition = Vector2.zero;

            // 2. Background 
            // Stretch, plain white Image - no sprite, simple type.
            GameObject bg = CreateGO("Background", root, typeof(Image));
            Image bgImg = bg.GetComponent<Image>();
            bgImg.sprite        = null;
            bgImg.type          = Image.Type.Simple;
            bgImg.color         = Color.white;
            bgImg.raycastTarget = false;
            Stretch(bgImg.rectTransform);

            // 3. Vallue Main (valueText) 
            // White TMP over full bar - visible on green fill, invisible on white background.
            GameObject textMainGO = CreateGO("Vallue Main", root, typeof(TextMeshProUGUI));
            TextMeshProUGUI textMain = textMainGO.GetComponent<TextMeshProUGUI>();
            textMain.text          = "100%";
            textMain.fontSize      = 10f;
            textMain.color         = Color.white;
            textMain.alignment     = TextAlignmentOptions.Center;
            textMain.raycastTarget = false;
            StretchWithOffset(textMain.rectTransform,
                anchoredPosition: new Vector2(1.6115036f, 0f),
                sizeDelta:        new Vector2(-3.2228f,   0f));

            // 4. Fill (mainFill image + Mask) 
            // Green Filled-Horizontal image. The Mask clips its child to the filled region.
            GameObject fill = CreateGO("Fill", root, typeof(Image), typeof(Mask));
            Image fillImg = fill.GetComponent<Image>();
            fillImg.sprite        = whitePixel;
            fillImg.type          = Image.Type.Filled;
            fillImg.fillMethod    = Image.FillMethod.Horizontal;
            fillImg.fillOrigin    = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount    = 1f;
            fillImg.color         = new Color(0.2f, 0.7f, 0.2f, 1f);
            fillImg.raycastTarget = false;
            Stretch(fillImg.rectTransform);

            fill.GetComponent<Mask>().showMaskGraphic = true;

            // 5. Text background (valueText2) - child of Fill 
            // Black TMP clipped by Fill's Mask - only visible inside the filled area.
            GameObject textBgGO = CreateGO("Text background", fill, typeof(TextMeshProUGUI));
            TextMeshProUGUI textBg = textBgGO.GetComponent<TextMeshProUGUI>();
            textBg.text          = "100%";
            textBg.fontSize      = 10f;
            textBg.color         = Color.black;
            textBg.alignment     = TextAlignmentOptions.Center;
            textBg.raycastTarget = false;
            StretchWithOffset(textBg.rectTransform,
                anchoredPosition: new Vector2(1.6115036f, 0f),
                sizeDelta:        new Vector2(-3.2228f,   0f));

            // 6. Wire UIProgressBar 
            UIProgressBar bar = root.GetComponent<UIProgressBar>();
            SerializedObject so = new SerializedObject(bar);

            so.FindProperty("mainFill")        .objectReferenceValue = fillImg;
            so.FindProperty("background")      .objectReferenceValue = bgImg;
            so.FindProperty("valueText")       .objectReferenceValue = textMain;
            so.FindProperty("valueText2")      .objectReferenceValue = textBg;
            so.FindProperty("solidColor")      .colorValue           = new Color(0.2f, 0.7f, 0.2f, 1f);
            so.FindProperty("initialValue")    .floatValue           = 100f;
            so.FindProperty("initialMaxValue") .floatValue           = 100f;
            so.FindProperty("currentValue")    .floatValue           = 100f;
            so.FindProperty("showText")        .boolValue            = true;
            so.FindProperty("textMode")        .enumValueIndex       = (int)UIProgressBar.TextDisplayMode.Percentage;

            so.ApplyModifiedPropertiesWithoutUndo();

            // 7. Parent, Undo, Select 
            GameObjectUtility.SetParentAndAlign(
                root,
                menuCommand.context as GameObject ?? FindOrCreateCanvas());

            Undo.RegisterCreatedObjectUndo(root, "Create ProgressBar");
            Selection.activeObject = root;
        }

        // Helpers

        static GameObject CreateGO(string name, GameObject parent, params System.Type[] extra)
        {
            var types = new System.Collections.Generic.List<System.Type> { typeof(RectTransform) };
            foreach (var t in extra)
                if (t != typeof(RectTransform)) types.Add(t);

            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        /// <summary>Anchor stretch-to-fill, zero offsets.</summary>
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Anchor stretch-to-fill with a custom anchoredPosition and sizeDelta offset.</summary>
        static void StretchWithOffset(RectTransform rt, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta        = sizeDelta;
        }

        /// <summary>
        /// Loads the white_pixel sprite from the known asset path.
        /// Creates the 1×1 PNG and imports it as a Sprite if it does not yet exist.
        /// </summary>
        static Sprite LoadOrCreateWhitePixel()
        {
            const string assetPath = "Assets/CorePro/UI Pro/Resources/white_pixel.png";

            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (spr != null) return spr;

            // Asset missing - create a 1×1 white PNG and import it
            string dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType         = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 1;
                importer.mipmapEnabled       = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

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
