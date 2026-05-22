using CorePro.UI;

namespace CorePro.UITools
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public static class SimpleBarCreator
    {
        [MenuItem("GameObject/CorePro/UI tools/Create Simple Bar UI", false, 5000)]
        public static void CreateHealthBarUI()
        {
            EnsureWhitePixel();

            Sprite whiteSprite = Resources.Load<Sprite>("white_pixel");
            if (whiteSprite == null)
            {
                var tex = Resources.Load<Texture2D>("white_pixel");
                if (tex != null)
                {
                    whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    Debug.Log("White pixel loaded as Texture2D, converted to Sprite.");
                }
            }

            
            if (whiteSprite == null)
            {
                Debug.LogError("White pixel sprite could not be loaded!");
                return;
            }

            GameObject root;
            // Root object
            if (Selection.activeTransform != null)
            {
                root = Selection.activeTransform.gameObject;
            }
            else
            {
                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        
                if (canvas == null)
                {
                    GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                    canvas = canvasGO.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
                    var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
            
                    Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
                    Debug.Log("Canvas created automatically.");
                }
        
                root = canvas.gameObject;
            }


            //HEALTH BAR
            GameObject healthObj = CreateBar<SimpleBar>(
                "SimpleBar",
                root.gameObject.transform,
                Color.green,
                new Color(1f, 0.84f, 0f, 0.6f),
                out SimpleBar healthBar,
                whiteSprite
            );

            healthObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 25);

            Selection.activeGameObject = root;
        }


        private static GameObject CreateBar<T>(string name, Transform parent, Color mainColor, Color overchargeColor, out T barComponent, Sprite whiteSprite)
            where T : UIBar
        {
            GameObject barObj = new GameObject(name, typeof(RectTransform));
            barObj.transform.SetParent(parent, false);

            // Background
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(barObj.transform, false);
            var bgImg = background.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.6f);
            bgImg.sprite = whiteSprite;
            var bgRect = bgImg.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Overcharge fill
            GameObject overFill = CreateImage("OverchargeFill", barObj.transform, overchargeColor, whiteSprite);

            // Main fill
            GameObject mainFill = CreateImage("MainFill", barObj.transform, mainColor, whiteSprite);

            // Value text (TMP)
            GameObject textObj = CreateTextTMP("ValueText", barObj.transform, Color.white);

            // Add bar script
            barComponent = barObj.AddComponent<T>();

            // Assign refs
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            barComponent.GetType().GetField("mainFill", flags)?.SetValue(barComponent, mainFill.GetComponent<Image>());
            barComponent.GetType().GetField("overchargeFill", flags)?.SetValue(barComponent, overFill.GetComponent<Image>());
            barComponent.GetType().GetField("valueText", flags)?.SetValue(barComponent, textObj.GetComponent<TMPro.TextMeshProUGUI>());

            return barObj;
        }


        private static GameObject CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;

            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }


        private static GameObject CreateTextTMP(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "100/100";
            text.color = color;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.fontSize = 24;

            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }


        private static void EnsureWhitePixel()
        {
            string folderPath = "Assets/CorePro/CoreTools/HealthSystem/Resources";
            string filePath = folderPath + "/white_pixel.png";

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            bool createNew = !File.Exists(filePath);

            if (createNew)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();

                File.WriteAllBytes(filePath, tex.EncodeToPNG());
            }

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 1;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;

                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
            if (sprite != null)
            {
                // Debug in need
                //Debug.Log($"White pixel imported as Sprite at {filePath}");
            }
            else
                Debug.LogError($"White pixel still not Sprite at {filePath}");
        }
    }
}