// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Runs On Upload after other upload passes finish, when that build pipeline is in the project.

using System;
using System.Reflection;
using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(KaleidoVR.EditorTools.KaleidoUploadLastPlugin))]

namespace KaleidoVR.EditorTools
{
    public sealed class KaleidoUploadLastPlugin : Plugin<KaleidoUploadLastPlugin>
    {
        public override string QualifiedName
        {
            get { return "com.kaleidovr.vrchat-model-optimizer"; }
        }

        public override string DisplayName
        {
            get { return "KaleidoVR On Upload"; }
        }

        protected override void Configure()
        {
            BuildPhase phase = BuildPhase.Optimizing;
            FieldInfo finish = typeof(BuildPhase).GetField("PlatformFinish", BindingFlags.Public | BindingFlags.Static);
            if (finish != null)
            {
                BuildPhase late = finish.GetValue(null) as BuildPhase;
                if (late != null) phase = late;
            }

            InPhase(phase).Run("KaleidoVR On Upload", ctx =>
            {
                if (ctx == null || ctx.AvatarRootObject == null) return;
                InvokeAssembledUpload(ctx.AvatarRootObject);
            });
        }

        static void InvokeAssembledUpload(GameObject avatar)
        {
            Type pass = Type.GetType("KaleidoVR.EditorTools.KaleidoAvatarPass, Assembly-CSharp-Editor");
            if (pass == null) return;
            MethodInfo method = pass.GetMethod("ApplyOnAssembledUpload", BindingFlags.Public | BindingFlags.Static);
            if (method == null) return;

            object boxed;
            try
            {
                boxed = method.Invoke(null, new object[] { avatar, true, true });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
            if (boxed == null) return;
            Type resultType = boxed.GetType();
            FieldInfo okField = resultType.GetField("ok");
            FieldInfo failField = resultType.GetField("failReason");
            bool ok = okField == null || (bool)okField.GetValue(boxed);
            if (ok) return;
            string reason = failField != null ? failField.GetValue(boxed) as string : null;
            throw new InvalidOperationException(string.IsNullOrEmpty(reason) ? "On Upload failed." : reason);
        }
    }
}
