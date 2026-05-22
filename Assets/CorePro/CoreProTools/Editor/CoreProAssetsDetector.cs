using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using UnityEditor.Build;

public class CoreProAssetsDetector : MonoBehaviour
{
    [InitializeOnLoad]
    public static class WSPDetector
    {
        private const string DefineSymbol = "COREPRO_WEAPON_SYSTEM_PRO";
        private const string TargetAssembly = "CorePro.WeaponSystemPro";

        static WSPDetector()
        {
            EditorApplication.delayCall += CheckAndDefine;
        }

        private static void CheckAndDefine()
        {
            // Check for assembly by name - fast and non-allocating
            bool hasWSP = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == TargetAssembly);

            // Get the current build target group and convert it to NamedBuildTarget
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            // Use the new API: GetScriptingDefineSymbols with NamedBuildTarget
            string currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            
            // Efficiently check if symbol already exists
            string[] definesArray = currentDefines.Split(';', StringSplitOptions.RemoveEmptyEntries);
            bool alreadyDefined = definesArray.Contains(DefineSymbol);

            if (hasWSP && !alreadyDefined)
            {
                // Add symbol
                string newDefines = string.IsNullOrEmpty(currentDefines) ? DefineSymbol : currentDefines + ";" + DefineSymbol;
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
                Debug.Log($"<b>[CorePro]</b> WeaponSystemPro detected. Integration Enabled (Symbol: {DefineSymbol})");
            }
            else if (!hasWSP && alreadyDefined)
            {
                // Remove symbol
                string newDefines = string.Join(";", definesArray.Where(d => d != DefineSymbol));
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
                Debug.Log("<b>[CorePro]</b> WeaponSystemPro not found. Integration Disabled.");
            }
        }
    }
}
