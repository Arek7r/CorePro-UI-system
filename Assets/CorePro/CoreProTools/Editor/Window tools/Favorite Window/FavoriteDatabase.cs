using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace CorePro.Favorites
{
    public sealed class FavoriteDatabase : ScriptableObject
    {
        public List<FavoriteEntry> entries = new List<FavoriteEntry>(32);

        private static FavoriteDatabase instance;
        public static FavoriteDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = AssetDatabase.LoadAssetAtPath<FavoriteDatabase>(
                        "Assets/CorePro/CoreTools/Editor/Favorite Window/FavoriteDatabase.asset");

                    if (instance == null)
                    {
                        instance = ScriptableObject.CreateInstance<FavoriteDatabase>();

                        string folderPath = "Assets/CorePro/CoreTools/Editor/Favorite Window";
                        if (AssetDatabase.IsValidFolder(folderPath) == false)
                        {
                            AssetDatabase.CreateFolder("Assets/CorePro/CoreTools/Editor", "Favorite Window");
                        }

                        AssetDatabase.CreateAsset(instance, folderPath + "/FavoriteDatabase.asset");
                        AssetDatabase.SaveAssets();
                    }
                }
                return instance;
            }
        }

        public static void Save()
        {
            EditorUtility.SetDirty(Instance);
            AssetDatabase.SaveAssets();
        }
    }

}