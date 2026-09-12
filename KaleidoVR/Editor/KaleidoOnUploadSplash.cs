// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Splash with loading bar while On Upload runs on the upload copy.

using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace KaleidoVR.EditorTools
{
    public sealed class KaleidoOnUploadSplash : EditorWindow
    {
        static KaleidoOnUploadSplash instance;
        static readonly MethodInfo RepaintImmediate = typeof(EditorWindow).GetMethod(
            "RepaintImmediately", BindingFlags.Instance | BindingFlags.NonPublic);

        string status = "Optimizing…";
        float progress;

        public static bool IsOpen { get { return instance != null; } }

        public static void Open(string avatarName)
        {
            CloseIfOpen();
            string label = "Optimizing " + (string.IsNullOrEmpty(avatarName) ? "avatar" : avatarName) + "…";
            instance = CreateInstance<KaleidoOnUploadSplash>();
            instance.titleContent = new GUIContent("KaleidoVR — On Upload");
            instance.status = label;
            instance.progress = 0.02f;
            instance.minSize = instance.maxSize = new Vector2(400f, 292f);
            Vector2 center = new Vector2(Screen.currentResolution.width * 0.5f, Screen.currentResolution.height * 0.45f);
            instance.position = new Rect(center.x - 200f, center.y - 146f, 400f, 292f);
            instance.ShowPopup();
            instance.Focus();
            PaintNow(instance);
            PaintNow(instance);
        }

        public static void SetProgress(string status, float t)
        {
            if (instance == null) return;
            if (!string.IsNullOrEmpty(status)) instance.status = status;
            instance.progress = Mathf.Clamp01(t);
            PaintNow(instance);
        }

        public static void CloseIfOpen()
        {
            if (instance == null) return;
            KaleidoOnUploadSplash closing = instance;
            instance = null;
            closing.Close();
        }

        static void PaintNow(EditorWindow window)
        {
            if (window == null) return;
            window.Repaint();
            if (RepaintImmediate != null) RepaintImmediate.Invoke(window, null);
        }

        void OnGUI()
        {
            GUILayout.Space(16);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(KaleidoVRCOptimizer.ICON_PATH);
            if (logo != null) KaleidoVRCOptimizerUI.DrawSplashLogo(logo, 280f, 140f);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);

            GUIStyle title = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            GUIStyle body = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label("KaleidoVR — On Upload", title);
            GUILayout.Label(status, body);
            GUILayout.Space(10);
            Rect bar = GUILayoutUtility.GetRect(18f, 22f, GUILayout.ExpandWidth(true));
            bar.x += 24f;
            bar.width -= 48f;
            EditorGUI.ProgressBar(bar, progress, Mathf.RoundToInt(progress * 100f) + "%");
            GUILayout.Space(8);
            GUILayout.Label("Upload copy only. Scene and source assets stay as they are.", body);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
