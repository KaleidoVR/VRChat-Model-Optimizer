// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Applies On Upload on the assembled upload clone. Skips Play Mode.
// Does not write the scene. Leaves meshes and markers Kaleido did not create.

#if VRC_SDK_VRCSDK3 || VRCSDK3_AVATARS
using UnityEditor;
using UnityEngine;
using System;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.BuildPipeline;

namespace KaleidoVR.EditorTools
{
    [InitializeOnLoad]
    static class KaleidoUploadCleanup
    {
        static KaleidoUploadCleanup()
        {
            VRCSdkControlPanel.OnSdkPanelEnable += OnSdkPanelEnable;
        }

        static void OnSdkPanelEnable(object sender, EventArgs e)
        {
            IVRCSdkAvatarBuilderApi builder;
            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out builder) || builder == null) return;
            builder.OnSdkUploadSuccess -= OnUploadSuccess;
            builder.OnSdkUploadSuccess += OnUploadSuccess;
        }

        static void OnUploadSuccess(object sender, string message)
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += KaleidoAvatarPass.ClearGeneratedCache;
            };
        }
    }

    public sealed class KaleidoAvatarBuildHook : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder { get { return KaleidoAvatarPass.UploadCallbackOrder(); } }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            if (avatarGameObject == null) return true;
            if (Application.isPlaying) return true;

            KaleidoVRCOptimizer[] windows = Resources.FindObjectsOfTypeAll<KaleidoVRCOptimizer>();
            KaleidoVRCOptimizer window = windows != null && windows.Length > 0 ? windows[0] : null;
            KaleidoAvatarPassSettings settings = window != null
                ? KaleidoAvatarPass.FromWindow(window)
                : KaleidoAvatarPass.FromPrefs();
            if (settings == null || !settings.applyOnUpload) return true;

            bool ok = true;
            string fail = null;
            Exception thrown = null;
            try
            {
                KaleidoOnUploadSplash.Open(avatarGameObject.name);
                KaleidoAvatarPassResult result = KaleidoAvatarPass.Run(
                    avatarGameObject,
                    settings,
                    false,
                    KaleidoAvatarPass.ExclusionsFrom(window, avatarGameObject),
                    true);
                if (result != null && !result.ok)
                {
                    ok = false;
                    fail = result.failReason;
                }
            }
            catch (Exception ex)
            {
                ok = false;
                thrown = ex;
                fail = ex.Message;
            }
            finally
            {
                KaleidoOnUploadSplash.CloseIfOpen();
            }
            if (!ok) LogUploadAbort(avatarGameObject, fail, thrown);
            return ok;
        }

        static void LogUploadAbort(GameObject avatar, string reason, Exception ex)
        {
            string name = avatar != null ? avatar.name : "avatar";
            if (string.IsNullOrEmpty(reason)) reason = "On Upload failed.";
            Debug.LogError("[KaleidoVR] On Upload stopped for \"" + name + "\". The VRChat upload was cancelled. " + reason);
            if (ex != null) Debug.LogException(ex);
        }
    }
}
#endif
