// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Short splash shown while On Upload runs on the upload copy.

using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace KaleidoVR.EditorTools
{
    public sealed class KaleidoOnUploadSplash : EditorWindow
    {
        static KaleidoOnUploadSplash instance;
        string status = "Optimizing…";

        public static void Open(string avatarName)
        {
            CloseIfOpen();
            instance = CreateInstance<KaleidoOnUploadSplash>();
            instance.titleContent = new GUIContent("KaleidoVR — On Upload");
            instance.status = "Optimizing " + (string.IsNullOrEmpty(avatarName) ? "avatar" : avatarName) + "…";
            instance.minSize = instance.maxSize = new Vector2(380f, 248f);
            Vector2 center = new Vector2(Screen.currentResolution.width * 0.5f, Screen.currentResolution.height * 0.45f);
            instance.position = new Rect(center.x - 190f, center.y - 124f, 380f, 248f);
            instance.ShowUtility();
            instance.Focus();
            PaintNow(instance);
        }

        public static void CloseIfOpen()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }
        }

        static void PaintNow(EditorWindow window)
        {
            if (window == null) return;
            window.Repaint();
            MethodInfo paint = typeof(EditorWindow).GetMethod("RepaintImmediately", BindingFlags.Instance | BindingFlags.NonPublic);
            if (paint != null) paint.Invoke(window, null);
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
            GUILayout.Label("Upload copy only. Scene and source assets stay as they are.", body);
        }
    }
}
