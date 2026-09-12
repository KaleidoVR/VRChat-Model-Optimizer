// KaleidoVR Credits
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. Released under the MIT License.
// Shared by every KaleidoVR Unity editor tool. Same GUID in each repo so packages merge.

using System;
using UnityEditor;
using UnityEngine;

namespace KaleidoVR.EditorTools
{
    public class KaleidoVRCreditsWindow : EditorWindow
    {
        const string WebsiteUrl = "https://kalivr.com";
        const string DiscordUrl = "https://discord.com/invite/cRsufJssTA";
        const string LogoFileName = "Kali_Logo.png";
        const string FallbackLogoPath = "Assets/KaleidoVR/Editor/Icons/Kali_Logo.png";

        // Keep this priority higher than every other KaleidoVR menu item so Credits stays last.
        [MenuItem("KaleidoVR/Credits", false, 10000)]
        public static void ShowCredits()
        {
            Open();
        }

        public static void Open()
        {
            KaleidoVRCreditsWindow window = GetWindow<KaleidoVRCreditsWindow>(true, "KaleidoVR (Credits)", true);
            window.minSize = new Vector2(380, 420);
            window.maxSize = new Vector2(520, 580);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("KaleidoVR (Credits)");
        }

        private void OnGUI()
        {
            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(FindLogoPath());
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            GUIStyle body = new GUIStyle(EditorStyles.wordWrappedLabel) { alignment = TextAnchor.MiddleCenter };

            GUILayout.Space(16);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (logo != null)
            {
                Rect logoRect = GUILayoutUtility.GetRect(260, 160, GUILayout.Width(260), GUILayout.Height(160));
                GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("KaleidoVR", title);
            GUILayout.Label("Created and maintained by KaleidoVR", body);
            GUILayout.Label("Copyright (c) 2026 KaleidoVR  ·  MIT License", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(14);

            if (GUILayout.Button("Website  —  kalivr.com", GUILayout.Height(28)))
                Application.OpenURL(WebsiteUrl);
            if (GUILayout.Button("Discord", GUILayout.Height(28)))
                Application.OpenURL(DiscordUrl);
        }

        static string FindLogoPath()
        {
            foreach (string guid in AssetDatabase.FindAssets("KaleidoVRCredits t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (!scriptPath.EndsWith("/KaleidoVRCredits.cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                string editorFolder = scriptPath.Substring(0, scriptPath.LastIndexOf('/'));
                string relative = editorFolder + "/Icons/" + LogoFileName;
                if (AssetDatabase.LoadMainAssetAtPath(relative) != null)
                    return relative;
                break;
            }

            foreach (string guid in AssetDatabase.FindAssets("Kali_Logo t:Texture2D"))
            {
                string found = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (found.EndsWith("/" + LogoFileName, StringComparison.OrdinalIgnoreCase))
                    return found;
            }

            return FallbackLogoPath;
        }
    }
}
