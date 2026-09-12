// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Scan-time evaluator: VRAM, GrabPass, animator cost, and asset flags.
// Does not write assets. Dry Run / Apply still own every importer change.

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KaleidoVR.EditorTools
{
    public static class KaleidoVRCOptimizerEval
    {
        const long PcTexExcellent = 40L * 1048576;
        const long PcTexGood = 75L * 1048576;
        const long PcTexMedium = 110L * 1048576;
        const long PcTexPoor = 150L * 1048576;
        const long QuestTexExcellent = 10L * 1048576;
        const long QuestTexGood = 18L * 1048576;
        const long QuestTexMedium = 25L * 1048576;
        const long QuestTexPoor = 40L * 1048576;
        const long PcMeshExcellent = 20L * 1048576;
        const long PcMeshGood = 35L * 1048576;
        const long PcMeshMedium = 55L * 1048576;
        const long PcMeshPoor = 80L * 1048576;
        const long QuestMeshExcellent = 5L * 1048576;
        const long QuestMeshGood = 10L * 1048576;
        const long QuestMeshMedium = 15L * 1048576;
        const long QuestMeshPoor = 25L * 1048576;

        static readonly Regex GrabPassRegex = new Regex(@"GrabPass\s*\{", RegexOptions.Compiled);

        public static void Evaluate(KaleidoVRCOptimizer window, List<GameObject> roots, KaleidoOptimizerReport report)
        {
            if (report == null) return;
            report.grabPassShaders = new List<string>();
            report.writeDefaultOutliers = new List<string>();
            report.emptyStates = new List<string>();
            report.crunchedTextures = new List<string>();
            report.nonBc5Normals = new List<string>();
            report.blendshapeMeshLines = new List<string>();
            report.materialSwapNames = new List<string>();
            report.missingStreamingMipmaps = new List<string>();

            HashSet<Shader> grabShaders = new HashSet<Shader>();
            HashSet<Mesh> seenMeshes = new HashSet<Mesh>();
            List<(string name, int triangles, int shapes)> blendMeshes = new List<(string, int, int)>();

            if (roots != null)
            {
                HashSet<int> seen = new HashSet<int>();
                for (int r = 0; r < roots.Count; r++)
                {
                    GameObject root = roots[r];
                    if (root == null || !seen.Add(root.GetInstanceID())) continue;
                    EvaluateRenderers(root, report, grabShaders, seenMeshes, blendMeshes);
                    EvaluateAnimators(root, report);
                }
            }

            blendMeshes.Sort((a, b) => b.triangles.CompareTo(a.triangles));
            int shown = Math.Min(blendMeshes.Count, 12);
            for (int i = 0; i < shown; i++)
            {
                report.blendshapeMeshLines.Add(blendMeshes[i].name + ": " + blendMeshes[i].triangles.ToString("N0")
                    + " tris, " + blendMeshes[i].shapes + " shapes");
            }

            foreach (Shader shader in grabShaders)
            {
                if (shader != null) report.grabPassShaders.Add(shader.name);
            }
            report.grabPasses = report.grabPassShaders.Count;

            EvaluateTextures(window, report);

            report.vramAll = report.textureVramAll + report.meshVramAll;
            report.vramActive = report.textureVramActive + report.meshVramActive;
            report.textureBytesEstimate = report.textureVramAll;
            bool quest = window != null && window.IsQuestWorkspace;
            report.textureVramQuality = RankName(VramRank(report.textureVramAll, quest ? QuestTexExcellent : PcTexExcellent, quest ? QuestTexGood : PcTexGood, quest ? QuestTexMedium : PcTexMedium, quest ? QuestTexPoor : PcTexPoor));
            report.meshVramQuality = RankName(VramRank(report.meshVramAll, quest ? QuestMeshExcellent : PcMeshExcellent, quest ? QuestMeshGood : PcMeshGood, quest ? QuestMeshMedium : PcMeshMedium, quest ? QuestMeshPoor : PcMeshPoor));
            report.grabPassQuality = report.grabPasses <= 0 ? "Excellent" : report.grabPasses == 1 ? "Medium" : "Very Poor";
            report.anyStateQuality = RankName(VramRank(report.anyStateTransitions, 50, 80, 100, 150));
            report.layerCountQuality = RankName(VramRank(report.animatorLayers, 12, 20, 30, 45));
            report.blendshapeQuality = RankName(VramRank(report.blendshapeTriangles, 8000, 16000, 32000, 50000));
        }

        static void EvaluateRenderers(
            GameObject root,
            KaleidoOptimizerReport report,
            HashSet<Shader> grabShaders,
            HashSet<Mesh> seenMeshes,
            List<(string name, int triangles, int shapes)> blendMeshes)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsEditorOnly(renderer.gameObject)) continue;
                bool active = renderer.gameObject.activeInHierarchy;
                Mesh mesh = null;
                SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                if (skinned != null) mesh = skinned.sharedMesh;
                else
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null) mesh = filter.sharedMesh;
                }
                if (mesh != null && seenMeshes.Add(mesh))
                {
                    long bytes = MeshVramBytes(mesh);
                    report.meshVramAll += bytes;
                    if (active) report.meshVramActive += bytes;
                    if (mesh.blendShapeCount > 0)
                    {
                        int tris = mesh.triangles.Length / 3;
                        report.blendshapeTriangles += tris;
                        report.blendshapeMeshes++;
                        blendMeshes.Add((renderer.name, tris, mesh.blendShapeCount));
                    }
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null) continue;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null || material.shader == null) continue;
                    if (ShaderHasGrabPass(material.shader)) grabShaders.Add(material.shader);
                }
            }
        }

        static void EvaluateAnimators(GameObject root, KaleidoOptimizerReport report)
        {
            HashSet<AnimatorController> controllers = new HashSet<AnimatorController>();
            CollectControllers(root, controllers);

            List<string> wdOnStates = new List<string>();
            List<string> wdOffStates = new List<string>();
            foreach (AnimatorController controller in controllers)
            {
                if (controller == null || controller.layers == null) continue;
                for (int i = 0; i < controller.layers.Length; i++)
                {
                    AnimatorControllerLayer layer = controller.layers[i];
                    if (layer == null || layer.stateMachine == null) continue;
                    report.animatorLayers++;
                    WalkStateMachine(layer.stateMachine, layer.name, report, wdOnStates, wdOffStates);
                }
            }

            report.writeDefaultsMostlyOn = report.writeDefaultsOnCount >= report.writeDefaultsOffCount;
            report.writeDefaultsMixed = report.writeDefaultsOnCount > 0 && report.writeDefaultsOffCount > 0;
            List<string> outliers = report.writeDefaultsMostlyOn ? wdOffStates : wdOnStates;
            int cap = Math.Min(outliers.Count, 40);
            for (int i = 0; i < cap; i++) report.writeDefaultOutliers.Add(outliers[i]);
        }

        static void WalkStateMachine(
            AnimatorStateMachine machine,
            string path,
            KaleidoOptimizerReport report,
            List<string> wdOnStates,
            List<string> wdOffStates)
        {
            if (machine == null) return;
            if (machine.anyStateTransitions != null) report.anyStateTransitions += machine.anyStateTransitions.Length;
            if (machine.states != null)
            {
                for (int i = 0; i < machine.states.Length; i++)
                {
                    AnimatorState state = machine.states[i].state;
                    if (state == null) continue;
                    string name = path + "/" + state.name;
                    if (state.motion == null)
                    {
                        report.emptyStateCount++;
                        if (report.emptyStates.Count < 40) report.emptyStates.Add(name);
                    }
                    if (state.writeDefaultValues)
                    {
                        report.writeDefaultsOnCount++;
                        if (wdOnStates.Count < 80) wdOnStates.Add(name);
                    }
                    else
                    {
                        report.writeDefaultsOffCount++;
                        if (wdOffStates.Count < 80) wdOffStates.Add(name);
                    }
                }
            }
            if (machine.stateMachines == null) return;
            for (int i = 0; i < machine.stateMachines.Length; i++)
            {
                AnimatorStateMachine child = machine.stateMachines[i].stateMachine;
                string childName = child != null ? child.name : "Sub";
                WalkStateMachine(child, path + "/" + childName, report, wdOnStates, wdOffStates);
            }
        }

        static void EvaluateTextures(KaleidoVRCOptimizer window, KaleidoOptimizerReport report)
        {
            if (window == null || window.textureUsages == null) return;
            bool quest = window.IsQuestWorkspace;
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage == null || usage.texture == null) continue;
                long now = TextureVramBytes(usage.texture);
                usage.vramBytes = now;
                usage.formatLabel = FormatLabel(usage.texture);
                report.textureVramAll += now;
                if (usage.isActive) report.textureVramActive += now;

                TextureImporter importer = string.IsNullOrEmpty(usage.path) ? null : AssetImporter.GetAtPath(usage.path) as TextureImporter;
                if (importer != null && importer.crunchedCompression)
                {
                    usage.crunched = true;
                    if (report.crunchedTextures.Count < 40) report.crunchedTextures.Add(usage.texture.name);
                }
                if (importer != null && importer.mipmapEnabled && !importer.streamingMipmaps)
                {
                    usage.missingStreamingMipmaps = true;
                    report.missingStreamingCount++;
                    if (report.missingStreamingMipmaps.Count < 40) report.missingStreamingMipmaps.Add(usage.texture.name);
                }
                if (usage.kind == KaleidoTextureKind.Normal && usage.texture is Texture2D tex2D && tex2D.format != TextureFormat.BC5)
                {
                    if (report.nonBc5Normals.Count < 40) report.nonBc5Normals.Add(usage.texture.name);
                }
                if (usage.fromAnimationSwap && report.materialSwapNames.Count < 40)
                    report.materialSwapNames.Add(usage.texture.name);

                int current = quest ? usage.currentQuest : usage.currentPc;
                int planned = KaleidoVRCOptimizerLogic.GetPlannedRowSize(window, usage, quest);
                if (planned <= 0) planned = current;
                float curBpp = BitsPerPixel(usage.texture);
                float newBpp = curBpp;
                if (importer != null)
                {
                    bool applyFormat = quest ? window.applyAndroidTexFormat : window.applyPcTexFormat;
                    if (applyFormat && window.optimizeTextures)
                    {
                        TextureImporterFormat plannedFormat = KaleidoVRCOptimizerLogic.GetPlannedTextureFormat(window, importer, usage.kind);
                        float plannedBpp = BitsPerPixel(plannedFormat);
                        if (plannedBpp > 0) newBpp = plannedBpp;
                    }
                }
                report.textureVramPlanned += ScaleVram(now, current, planned, curBpp, newBpp);
            }
        }

        const string EmptyMotionName = "KaleidoEmptyMotion";

        public static int ConvertUnityConstraints(List<GameObject> roots)
        {
            if (roots == null) return 0;
            Type setup = FindAvatarDynamicsSetup();
            if (setup == null) return -1;

            int before = 0;
            List<GameObject> targets = new List<GameObject>();
            List<IConstraint> unityConstraints = new List<IConstraint>();
            Component descriptor = null;
            Type descType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            for (int i = 0; i < roots.Count; i++)
            {
                GameObject root = roots[i];
                if (root == null) continue;
                targets.Add(root);
                IConstraint[] found = root.GetComponentsInChildren<IConstraint>(true);
                if (found != null)
                {
                    before += found.Length;
                    unityConstraints.AddRange(found);
                }
                if (descriptor == null && descType != null)
                {
                    descriptor = root.GetComponent(descType);
                    if (descriptor == null) descriptor = root.GetComponentInChildren(descType, true);
                }
            }
            if (before == 0) return 0;

            if (TryInvokeConstraintConvert(setup, "ConvertUnityConstraintsAcrossGameObjects", targets, false))
                return CountRemainingUnityConstraints(targets, before);
            if (TryInvokeConstraintConvert(setup, "ConvertUnityConstraintsToVrChatConstraints", targets, false))
                return CountRemainingUnityConstraints(targets, before);

            MethodInfo doConvert = setup.GetMethod("DoConvertUnityConstraints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (doConvert != null)
            {
                ParameterInfo[] args = doConvert.GetParameters();
                if (args.Length >= 3)
                {
                    try
                    {
                        doConvert.Invoke(null, new object[] { unityConstraints.ToArray(), descriptor, true });
                        return CountRemainingUnityConstraints(targets, before);
                    }
                    catch (TargetInvocationException)
                    {
                        return 0;
                    }
                }
            }

            return -1;
        }

        static int CountRemainingUnityConstraints(List<GameObject> roots, int before)
        {
            int after = 0;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null) continue;
                after += roots[i].GetComponentsInChildren<IConstraint>(true).Length;
            }
            int converted = before - after;
            return converted > 0 ? converted : 0;
        }

        static bool TryInvokeConstraintConvert(Type setup, string methodName, List<GameObject> targets, bool isAutoFix)
        {
            MethodInfo[] methods = setup.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.Name != methodName) continue;
                ParameterInfo[] args = method.GetParameters();
                try
                {
                    if (args.Length == 1)
                    {
                        method.Invoke(null, new object[] { targets });
                        return true;
                    }
                    if (args.Length == 2 && args[1].ParameterType == typeof(bool))
                    {
                        method.Invoke(null, new object[] { targets, isAutoFix });
                        return true;
                    }
                }
                catch (TargetInvocationException)
                {
                    return false;
                }
            }
            return false;
        }

        static Type FindAvatarDynamicsSetup()
        {
            string[] names =
            {
                "VRC.SDK3.Avatars.AvatarDynamicsSetup",
                "VRC.SDK3.Avatars.Components.AvatarDynamicsSetup"
            };
            for (int i = 0; i < names.Length; i++)
            {
                Type found = KaleidoVRCOptimizerHelpers.FindTypeByFullName(names[i]);
                if (found != null) return found;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Assembly assembly = assemblies[a];
                if (assembly == null) continue;
                string assemblyName = assembly.GetName().Name ?? "";
                if (assemblyName.IndexOf("VRC", StringComparison.OrdinalIgnoreCase) < 0
                    && assemblyName.IndexOf("VRChat", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                for (int i = 0; i < names.Length; i++)
                {
                    Type found = assembly.GetType(names[i], false);
                    if (found != null) return found;
                }
                try
                {
                    Type[] types = assembly.GetTypes();
                    for (int t = 0; t < types.Length; t++)
                    {
                        if (types[t] != null && types[t].Name == "AvatarDynamicsSetup") return types[t];
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }
            return null;
        }

        public static int SetStreamingMipmaps(KaleidoVRCOptimizer window)
        {
            if (window == null || window.textureUsages == null) return 0;
            int changed = 0;
            List<TextureImporter> importers = new List<TextureImporter>();
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage == null || string.IsNullOrEmpty(usage.path)) continue;
                TextureImporter importer = AssetImporter.GetAtPath(usage.path) as TextureImporter;
                if (importer == null || !importer.mipmapEnabled || importer.streamingMipmaps) continue;
                Undo.RecordObject(importer, "Enable Streaming Mip Maps");
                importer.streamingMipmaps = true;
                EditorUtility.SetDirty(importer);
                importers.Add(importer);
                changed++;
            }
            for (int i = 0; i < importers.Count; i++)
                importers[i].SaveAndReimport();
            return changed;
        }

        public static int SetEmptyMotions(List<GameObject> roots)
        {
            if (roots == null) return 0;
            AnimationClip clip = GetOrCreateEmptyMotion();
            if (clip == null) return 0;

            HashSet<AnimatorController> controllers = new HashSet<AnimatorController>();
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null) continue;
                CollectControllers(roots[i], controllers);
            }

            int changed = 0;
            foreach (AnimatorController controller in controllers)
                changed += ApplyEmptyMotions(controller, clip);
            if (changed > 0) AssetDatabase.SaveAssets();
            return changed;
        }

        static AnimationClip GetOrCreateEmptyMotion()
        {
            string[] guids = AssetDatabase.FindAssets(EmptyMotionName + " t:AnimationClip");
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (existing != null && existing.name == EmptyMotionName) return existing;
                }
            }

            string folder = KaleidoVRCOptimizer.GetToolEditorFolder();
            if (string.IsNullOrEmpty(folder)) folder = "Assets/KaleidoVR/Editor";
            folder = folder.Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/KaleidoVR")) AssetDatabase.CreateFolder("Assets", "KaleidoVR");
                if (!AssetDatabase.IsValidFolder("Assets/KaleidoVR/Editor")) AssetDatabase.CreateFolder("Assets/KaleidoVR", "Editor");
                folder = "Assets/KaleidoVR/Editor";
            }

            string assetPath = folder + "/" + EmptyMotionName + ".anim";
            AnimationClip atPath = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (atPath != null) return atPath;

            AnimationClip clip = new AnimationClip();
            clip.name = EmptyMotionName;
            clip.frameRate = 60f;
            AssetDatabase.CreateAsset(clip, assetPath);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        static int ApplyEmptyMotions(AnimatorController controller, AnimationClip clip)
        {
            if (controller == null || controller.layers == null || clip == null) return 0;
            int changed = 0;
            for (int i = 0; i < controller.layers.Length; i++)
            {
                AnimatorControllerLayer layer = controller.layers[i];
                if (layer == null || layer.stateMachine == null) continue;
                changed += ApplyEmptyMotions(layer.stateMachine, clip);
            }
            if (changed > 0) EditorUtility.SetDirty(controller);
            return changed;
        }

        static int ApplyEmptyMotions(AnimatorStateMachine machine, AnimationClip clip)
        {
            if (machine == null) return 0;
            int changed = 0;
            if (machine.states != null)
            {
                for (int i = 0; i < machine.states.Length; i++)
                {
                    AnimatorState state = machine.states[i].state;
                    if (state == null || state.motion != null) continue;
                    Undo.RecordObject(state, "Fill Empty Motion");
                    state.motion = clip;
                    EditorUtility.SetDirty(state);
                    changed++;
                }
            }
            if (machine.stateMachines == null) return changed;
            for (int i = 0; i < machine.stateMachines.Length; i++)
                changed += ApplyEmptyMotions(machine.stateMachines[i].stateMachine, clip);
            return changed;
        }

        public static int SetWriteDefaults(List<GameObject> roots, bool on)
        {
            if (roots == null) return 0;
            HashSet<AnimatorController> controllers = new HashSet<AnimatorController>();
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null) continue;
                CollectControllers(roots[i], controllers);
            }

            int changed = 0;
            foreach (AnimatorController controller in controllers)
                changed += ApplyWriteDefaults(controller, on);
            if (changed > 0) AssetDatabase.SaveAssets();
            return changed;
        }

        static void CollectControllers(GameObject root, HashSet<AnimatorController> controllers)
        {
            if (root == null || controllers == null) return;
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
                AddController(controllers, animators[i] != null ? animators[i].runtimeAnimatorController : null);
            CollectDescriptorControllers(root, controllers);
        }

        static int ApplyWriteDefaults(AnimatorController controller, bool on)
        {
            if (controller == null || controller.layers == null) return 0;
            int changed = 0;
            for (int i = 0; i < controller.layers.Length; i++)
            {
                AnimatorControllerLayer layer = controller.layers[i];
                if (layer == null || layer.stateMachine == null) continue;
                changed += ApplyWriteDefaults(layer.stateMachine, on);
            }
            if (changed > 0) EditorUtility.SetDirty(controller);
            return changed;
        }

        static int ApplyWriteDefaults(AnimatorStateMachine machine, bool on)
        {
            if (machine == null) return 0;
            int changed = 0;
            if (machine.states != null)
            {
                for (int i = 0; i < machine.states.Length; i++)
                {
                    AnimatorState state = machine.states[i].state;
                    if (state == null || state.writeDefaultValues == on) continue;
                    Undo.RecordObject(state, "Set Write Defaults");
                    state.writeDefaultValues = on;
                    EditorUtility.SetDirty(state);
                    changed++;
                }
            }
            if (machine.stateMachines == null) return changed;
            for (int i = 0; i < machine.stateMachines.Length; i++)
                changed += ApplyWriteDefaults(machine.stateMachines[i].stateMachine, on);
            return changed;
        }

        static void AddController(HashSet<AnimatorController> controllers, RuntimeAnimatorController runtime)
        {
            if (runtime == null || controllers == null) return;
            AnimatorController ac = runtime as AnimatorController;
            if (ac == null)
            {
                AnimatorOverrideController ov = runtime as AnimatorOverrideController;
                if (ov != null) ac = ov.runtimeAnimatorController as AnimatorController;
            }
            if (ac != null) controllers.Add(ac);
        }

        static void CollectDescriptorControllers(GameObject root, HashSet<AnimatorController> controllers)
        {
            Type descType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (descType == null || root == null) return;
            Component desc = root.GetComponent(descType);
            if (desc == null) desc = root.GetComponentInChildren(descType, true);
            if (desc == null) return;
            CollectLayerControllers(desc, "baseAnimationLayers", controllers);
            CollectLayerControllers(desc, "specialAnimationLayers", controllers);
        }

        static void CollectLayerControllers(Component desc, string fieldName, HashSet<AnimatorController> controllers)
        {
            FieldInfo field = desc.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return;
            Array layers = field.GetValue(desc) as Array;
            if (layers == null) return;
            foreach (object layer in layers)
            {
                if (layer == null) continue;
                FieldInfo controllerField = layer.GetType().GetField("animatorController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (controllerField == null) continue;
                AddController(controllers, controllerField.GetValue(layer) as RuntimeAnimatorController);
            }
        }

        public static void CollectAnimationMaterials(GameObject root, Action<Material> onMaterial)
        {
            if (root == null || onMaterial == null) return;
            HashSet<AnimatorController> controllers = new HashSet<AnimatorController>();
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
                AddController(controllers, animators[i] != null ? animators[i].runtimeAnimatorController : null);
            CollectDescriptorControllers(root, controllers);
            foreach (AnimatorController controller in controllers)
            {
                if (controller == null) continue;
                AnimationClip[] clips = controller.animationClips;
                if (clips == null) continue;
                for (int c = 0; c < clips.Length; c++)
                {
                    AnimationClip clip = clips[c];
                    if (clip == null) continue;
                    EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    for (int b = 0; b < bindings.Length; b++)
                    {
                        EditorCurveBinding binding = bindings[b];
                        if (!binding.isPPtrCurve) continue;
                        if (binding.propertyName == null || !binding.propertyName.StartsWith("m_Materials", StringComparison.Ordinal)) continue;
                        ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        if (keys == null) continue;
                        for (int k = 0; k < keys.Length; k++)
                        {
                            Material material = keys[k].value as Material;
                            if (material != null) onMaterial(material);
                        }
                    }
                }
            }
        }

        public static bool IsEditorOnly(GameObject go)
        {
            if (go == null) return false;
            try
            {
                Transform t = go.transform;
                while (t != null)
                {
                    if (t.CompareTag("EditorOnly")) return true;
                    t = t.parent;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public static bool ShaderHasGrabPass(Shader shader)
        {
            if (shader == null) return false;
            string path = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                return GrabPassRegex.IsMatch(File.ReadAllText(path));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static long TextureVramBytes(Texture texture)
        {
            if (texture == null) return 0;
            float bpp = BitsPerPixel(texture);
            int width = texture.width;
            int height = texture.height;
            int mips = 1;
            if (texture is Texture2D t2d) mips = Math.Max(1, t2d.mipmapCount);
            else if (texture is Cubemap cube) mips = Math.Max(1, cube.mipmapCount);
            else if (texture is Texture2DArray arr)
            {
                mips = Math.Max(1, arr.mipmapCount);
                return MipBytes(width, height, mips, bpp) * Math.Max(1, arr.depth);
            }
            long bytes = MipBytes(width, height, mips, bpp);
            if (texture is Cubemap) bytes *= 6;
            return bytes;
        }

        public static long MeshVramBytes(Mesh mesh)
        {
            if (mesh == null) return 0;
            long vertexStride = 0;
            UnityEngine.Rendering.VertexAttributeDescriptor[] attrs = mesh.GetVertexAttributes();
            bool skinned = mesh.HasVertexAttribute(VertexAttribute.BlendIndices)
                && mesh.HasVertexAttribute(VertexAttribute.BlendWeight);
            for (int i = 0; i < attrs.Length; i++)
            {
                UnityEngine.Rendering.VertexAttributeDescriptor attr = attrs[i];
                int mul = 1;
                if (skinned && (attr.attribute == VertexAttribute.Position
                    || attr.attribute == VertexAttribute.Normal
                    || attr.attribute == VertexAttribute.Tangent))
                    mul = 2;
                vertexStride += VertexFormatBytes(attr.format) * attr.dimension * mul;
            }
            long bytes = vertexStride * mesh.vertexCount;
            if (mesh.blendShapeCount > 0)
                bytes += (long)mesh.blendShapeCount * mesh.vertexCount * 12;
            return bytes;
        }

        public static float BitsPerPixel(Texture texture)
        {
            if (texture is Texture2D t2d) return BitsPerPixel(t2d.format);
            if (texture is Texture2DArray arr) return BitsPerPixel(arr.format);
            if (texture is Cubemap cube) return BitsPerPixel(cube.format);
            return 16f;
        }

        public static float BitsPerPixel(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.BC4:
                case TextureFormat.EAC_R:
                case TextureFormat.EAC_R_SIGNED:
                    return 4f;
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC5:
                case TextureFormat.BC7:
                case TextureFormat.BC6H:
                case TextureFormat.EAC_RG:
                case TextureFormat.EAC_RG_SIGNED:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ASTC_4x4:
                    return 8f;
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGB:
                case TextureFormat.ETC2_RGBA1:
                    return 4f;
                case TextureFormat.ASTC_5x5: return 5.12f;
                case TextureFormat.ASTC_6x6: return 3.56f;
                case TextureFormat.ASTC_8x8: return 2f;
                case TextureFormat.RGB24: return 24f;
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    return 32f;
                case TextureFormat.RGBAHalf: return 64f;
                case TextureFormat.RGBAFloat: return 128f;
                default: return 16f;
            }
        }

        public static float BitsPerPixel(TextureImporterFormat format)
        {
            switch (format)
            {
                case TextureImporterFormat.DXT1:
                case TextureImporterFormat.BC4:
                case TextureImporterFormat.ETC2_RGB4:
                    return 4f;
                case TextureImporterFormat.DXT5:
                case TextureImporterFormat.BC5:
                case TextureImporterFormat.BC7:
                case TextureImporterFormat.ETC2_RGBA8:
                case TextureImporterFormat.ASTC_4x4:
                    return 8f;
                case TextureImporterFormat.ASTC_5x5: return 5.12f;
                case TextureImporterFormat.ASTC_6x6: return 3.56f;
                case TextureImporterFormat.ASTC_8x8: return 2f;
                case TextureImporterFormat.Automatic:
                    return 8f;
                default: return 8f;
            }
        }

        static long MipBytes(int width, int height, int mips, float bpp)
        {
            long bytes = 0;
            int w = Math.Max(1, width);
            int h = Math.Max(1, height);
            int count = Math.Max(1, mips);
            for (int i = 0; i < count; i++)
            {
                bytes += (long)Mathf.RoundToInt(w * h * bpp / 8f);
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
            }
            return bytes;
        }

        static long ScaleVram(long currentVram, int currentSize, int plannedSize, float currentBpp, float plannedBpp)
        {
            if (currentVram <= 0) return 0;
            double area = 1d;
            if (currentSize > 0 && plannedSize > 0)
                area = (double)plannedSize / currentSize;
            double bpp = plannedBpp > 0.1f && currentBpp > 0.1f ? plannedBpp / currentBpp : 1d;
            return (long)Math.Max(0, currentVram * area * area * bpp);
        }

        static int VertexFormatBytes(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.UInt8:
                case VertexAttributeFormat.SInt8:
                    return 1;
                case VertexAttributeFormat.UNorm16:
                case VertexAttributeFormat.SNorm16:
                case VertexAttributeFormat.UInt16:
                case VertexAttributeFormat.SInt16:
                case VertexAttributeFormat.Float16:
                    return 2;
                default:
                    return 4;
            }
        }

        static int VramRank(long value, long excellent, long good, long medium, long poor)
        {
            if (value < excellent) return 0;
            if (value < good) return 1;
            if (value < medium) return 2;
            if (value < poor) return 3;
            return 4;
        }

        static string RankName(int rank)
        {
            switch (rank)
            {
                case 0: return "Excellent";
                case 1: return "Good";
                case 2: return "Medium";
                case 3: return "Poor";
                default: return "Very Poor";
            }
        }

        static string FormatLabel(Texture texture)
        {
            if (texture is Texture2D t2d) return t2d.format.ToString();
            if (texture is Texture2DArray arr) return arr.format.ToString();
            if (texture is Cubemap cube) return cube.format.ToString();
            return texture != null ? texture.GetType().Name : "";
        }
    }
}
