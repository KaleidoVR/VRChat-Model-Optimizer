// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Applies On Upload on the assembled upload clone. Skips Play Mode.
// Does not write the scene. Leaves meshes and markers Kaleido did not create.

#if VRC_SDK_VRCSDK3 || VRCSDK3_AVATARS
using UnityEditor;
using UnityEngine;
using System;
using VRC.SDKBase.Editor.BuildPipeline;

namespace KaleidoVR.EditorTools
{
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
            try
            {
                KaleidoOnUploadSplash.Open(avatarGameObject.name);
                KaleidoAvatarPassResult result = KaleidoAvatarPass.Run(
                    avatarGameObject,
                    settings,
                    false,
                    KaleidoAvatarPass.ExclusionsFrom(window, avatarGameObject),
                    true);
                ok = result == null || result.ok;
            }
            catch (Exception)
            {
                ok = false;
            }
            finally
            {
                KaleidoOnUploadSplash.CloseIfOpen();
            }
            return ok;
        }
    }
}
#endif
