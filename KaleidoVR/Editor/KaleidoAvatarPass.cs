// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Avatar structure pass: unused cleanup, blend shapes, mesh / slot merge, PhysBones, FX.
// Runs on a clone at upload. Source assets and the scene stay as they are.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using Unity.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace KaleidoVR.EditorTools
{
    public sealed class KaleidoAvatarPassSettings
    {
        public bool applyOnUpload = false;
        public bool mergeSkinnedMeshes = true;
        public bool mergeIdenticalSlots = true;
        public bool shuffleMaterialSlots = true;
        public bool optimizeBlendShapes = true;
        public bool mergeSameRatioShapes = false;
        public bool mmdCompatibility = true;
        public bool removeUnusedComponents = true;
        public bool removeUnusedGameObjects = false;
        public bool stripUnusedBones = true;
        public bool optimizePhysBones = true;
        public bool optimizeFxLayer = false;
    }

    public sealed class KaleidoAvatarPassResult
    {
        public readonly List<string> lines = new List<string>();
        public int meshesMerged;
        public int slotsMerged;
        public int shapesRemoved;
        public int shapesBaked;
        public int shapesMerged;
        public int bonesRemoved;
        public int componentsRemoved;
        public int objectsRemoved;
        public int physBonesDisabled;
        public int fxLayersRemoved;
        public int curvesRemoved;
        public bool ok = true;
        public string failReason;

        public string SummaryLine()
        {
            return "meshes −" + meshesMerged
                + ", slots −" + slotsMerged
                + ", shapes −" + shapesRemoved
                + ", bones −" + bonesRemoved
                + ", components −" + componentsRemoved
                + ", objects −" + objectsRemoved
                + ", PhysBones −" + physBonesDisabled
                + ", FX layers −" + fxLayersRemoved;
        }
    }

    public static class KaleidoAvatarPass
    {
        static readonly string[] MmdShapeNames =
        {
            "あ", "い", "う", "え", "お", "あ２", "ん",
            "まばたき", "笑い", "ウィンク", "ウィンク右", "ウィンク２",
            "はぅ", "なごみ", "びっくり", "じと目", "瞳小",
            "a", "i", "u", "e", "o", "blink", "blink_l", "blink_r",
            "smile", "vrc.v_sil", "vrc.v_pp", "vrc.v_ff", "vrc.v_th",
            "vrc.v_dd", "vrc.v_kk", "vrc.v_ch", "vrc.v_ss", "vrc.v_nn",
            "vrc.v_rr", "vrc.v_aa", "vrc.v_e", "vrc.v_ih", "vrc.v_oh", "vrc.v_ou"
        };

        public static HashSet<Transform> ExclusionsOnCopy(KaleidoVRCOptimizer window, GameObject source, GameObject copy)
        {
            return RemapExclusions(source, copy, ExclusionsFrom(window, source));
        }

        public static HashSet<Transform> ExclusionsFrom(KaleidoVRCOptimizer window, GameObject root)
        {
            HashSet<Transform> set = new HashSet<Transform>();
            if (window == null || root == null) return set;
            if (window.ignoreList != null)
            {
                for (int i = 0; i < window.ignoreList.Count; i++)
                {
                    UnityEngine.Object obj = window.ignoreList[i];
                    Transform t = obj as Transform;
                    if (t == null)
                    {
                        Component c = obj as Component;
                        if (c != null) t = c.transform;
                        else
                        {
                            GameObject go = obj as GameObject;
                            if (go != null) t = go.transform;
                        }
                    }
                    if (t != null) set.Add(t);
                }
            }
            return set;
        }

        public static HashSet<Transform> ExclusionsForUpload(KaleidoVRCOptimizer window, GameObject uploadCopy)
        {
            if (uploadCopy == null) return new HashSet<Transform>();
            GameObject source = SetupAvatarRoot(window);
            if (source == null) return ExclusionsFrom(window, uploadCopy);
            return RemapExclusions(source, uploadCopy, ExclusionsFrom(window, source));
        }

        static GameObject SetupAvatarRoot(KaleidoVRCOptimizer window)
        {
            if (window == null || window.targets == null) return null;
            for (int i = 0; i < window.targets.Count; i++)
            {
                GameObject root;
                string reason;
                if (!KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(window.targets[i], out root, out reason))
                    continue;
                if (root != null) return root;
            }
            return null;
        }

        public static KaleidoAvatarPassSettings FromPrefs()
        {
            string p = KaleidoVRCOptimizer.PrefsPrefix;
            KaleidoAvatarPassSettings settings = new KaleidoAvatarPassSettings
            {
                applyOnUpload = EditorPrefs.GetBool(p + "AvUp", false),
                mergeSkinnedMeshes = EditorPrefs.GetBool(p + "AvMerge", true),
                mergeIdenticalSlots = EditorPrefs.GetBool(p + "AvSlots", true),
                shuffleMaterialSlots = EditorPrefs.GetBool(p + "AvShuffle", true),
                optimizeBlendShapes = EditorPrefs.GetBool(p + "AvShape", true),
                mergeSameRatioShapes = EditorPrefs.GetBool(p + "AvRatio", false),
                mmdCompatibility = EditorPrefs.GetBool(p + "AvMmd", true),
                removeUnusedComponents = EditorPrefs.GetBool(p + "AvComp", true),
                removeUnusedGameObjects = EditorPrefs.GetBool(p + "AvGo", false),
                stripUnusedBones = EditorPrefs.GetBool(p + "AvBone", true),
                optimizePhysBones = EditorPrefs.GetBool(p + "AvPb", true),
                optimizeFxLayer = EditorPrefs.GetBool(p + "AvFx", false)
            };
            return settings;
        }

        public static KaleidoAvatarPassSettings FromWindow(KaleidoVRCOptimizer window)
        {
            if (window == null) return new KaleidoAvatarPassSettings();
            KaleidoAvatarPassSettings settings = new KaleidoAvatarPassSettings
            {
                applyOnUpload = window.avatarApplyOnUpload,
                mergeSkinnedMeshes = window.avatarMergeSkinnedMeshes,
                mergeIdenticalSlots = window.avatarMergeIdenticalSlots,
                shuffleMaterialSlots = window.avatarShuffleSlots,
                optimizeBlendShapes = window.avatarOptimizeBlendShapes,
                mergeSameRatioShapes = window.avatarMergeSameRatioShapes,
                mmdCompatibility = window.avatarMmdCompatibility,
                removeUnusedComponents = window.avatarRemoveUnusedComponents,
                removeUnusedGameObjects = window.avatarRemoveUnusedGameObjects,
                stripUnusedBones = window.avatarStripUnusedBones,
                optimizePhysBones = window.avatarOptimizePhysBones,
                optimizeFxLayer = window.avatarOptimizeFxLayer
            };
            return settings;
        }

        public static int UploadCallbackOrder()
        {
            // Lower runs first. Last among preprocess so other upload passes finish first.
            return int.MaxValue;
        }

        public const string GeneratedFolderPath = "Assets/KaleidoVR/Generated";
        public const string OptimizedCopySuffix = " (Optimized Copy)";
        const string GeneratedRoot = "Assets/KaleidoVR";
        static bool persistGenerated;

        public static void Preview(GameObject root, KaleidoAvatarPassSettings settings, List<string> lines, HashSet<Transform> extraExclusions)
        {
            if (root == null || settings == null) return;
            KaleidoAvatarPassResult result = Run(root, settings, true, extraExclusions, false);
            if (result == null) return;
            for (int i = 0; i < result.lines.Count; i++) lines.Add(result.lines[i]);
        }

        public static KaleidoAvatarPassResult Run(GameObject root, KaleidoAvatarPassSettings settings, bool dryRun, HashSet<Transform> extraExclusions)
        {
            return Run(root, settings, dryRun, extraExclusions, !dryRun);
        }

        public static KaleidoAvatarPassResult Run(GameObject root, KaleidoAvatarPassSettings settings, bool dryRun, HashSet<Transform> extraExclusions, bool persist)
        {
            KaleidoAvatarPassResult result = new KaleidoAvatarPassResult();
            if (root == null || settings == null) return result;

            persistGenerated = persist && !dryRun;
            try
            {
                HashSet<Transform> excluded = CollectExclusions(root, extraExclusions);
                AvatarAnimInfo anim = AvatarAnimInfo.Build(root);
                ComponentRefInfo refs = ComponentRefInfo.Build(root);

                ReportProgress("Cleaning unused objects…", 0.08f);
                if (settings.removeUnusedComponents || settings.removeUnusedGameObjects)
                    SweepUnused(root, settings, anim, excluded, dryRun, result, refs);

                ReportProgress("Processing blend shapes…", 0.22f);
                if (settings.optimizeBlendShapes || settings.mergeSameRatioShapes)
                    ProcessBlendShapes(root, settings, anim, excluded, dryRun, result);

                ReportProgress("Trimming unused bones…", 0.38f);
                if (settings.stripUnusedBones)
                    StripUnusedBones(root, anim, excluded, dryRun, result, refs);

                ReportProgress("Merging material slots…", 0.52f);
                if (settings.mergeIdenticalSlots || settings.shuffleMaterialSlots)
                    MergeSlotsOnRenderers(root, settings, anim, excluded, dryRun, result, refs);

                ReportProgress("Merging meshes…", 0.68f);
                if (settings.mergeSkinnedMeshes)
                    MergeTogetherMeshes(root, settings, anim, excluded, dryRun, result, refs);

                ReportProgress("Cleaning PhysBones…", 0.82f);
                if (settings.optimizePhysBones)
                    SweepPhysBones(root, anim, excluded, dryRun, result, refs);

                ReportProgress("Optimizing FX…", 0.92f);
                if (settings.optimizeFxLayer)
                    OptimizeFx(root, settings, excluded, dryRun, result);

                ReportProgress("Saving generated meshes…", 0.98f);
                if (persistGenerated)
                {
                    AssertMeshesSaved(root);
                    AssetDatabase.SaveAssets();
                }
                if (result.lines.Count == 0)
                    result.lines.Add("Avatar pass: nothing to change with the current toggles.");
            }
            catch (Exception ex)
            {
                result.ok = false;
                result.failReason = ex.Message;
                result.lines.Add("On Upload stopped: " + ex.Message);
                if (persistGenerated) throw;
            }
            finally
            {
                persistGenerated = false;
            }
            return result;
        }

        static void ReportProgress(string status, float t)
        {
            if (!KaleidoOnUploadSplash.IsOpen) return;
            KaleidoOnUploadSplash.SetProgress(status, t);
        }

        static string EnsureGeneratedFolder()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedRoot))
                AssetDatabase.CreateFolder("Assets", "KaleidoVR");
            if (!AssetDatabase.IsValidFolder(GeneratedFolderPath))
            {
                AssetDatabase.CreateFolder(GeneratedRoot, "Generated");
                File.WriteAllText(GeneratedFolderPath + "/.gitignore", "*\n!.gitignore\n");
            }
            return GeneratedFolderPath;
        }

        public static bool GeneratedCacheHasFiles()
        {
            if (!Directory.Exists(GeneratedFolderPath)) return false;
            string[] files = Directory.GetFiles(GeneratedFolderPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                if (string.IsNullOrEmpty(name) || name == ".gitignore" || name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;
                return true;
            }
            return false;
        }

        public static bool HasGeneratedCleanup()
        {
            return GeneratedCacheHasFiles() || FindOptimizedPreviewCopies().Count > 0;
        }

        public static void ClearGeneratedCache()
        {
            RemoveOptimizedPreviewCopies();
            if (AssetDatabase.IsValidFolder(GeneratedFolderPath))
                AssetDatabase.DeleteAsset(GeneratedFolderPath);
            if (Directory.Exists(GeneratedFolderPath))
                FileUtil.DeleteFileOrDirectory(GeneratedFolderPath);
            AssetDatabase.Refresh();
        }

        static List<GameObject> FindOptimizedPreviewCopies()
        {
            List<GameObject> found = new List<GameObject>();
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (!IsSceneOptimizedPreviewCopy(go)) continue;
                found.Add(go);
            }
            return found;
        }

        static bool IsSceneOptimizedPreviewCopy(GameObject go)
        {
            if (go == null) return false;
            if (!go.scene.IsValid() || !go.scene.isLoaded) return false;
            if (EditorUtility.IsPersistent(go)) return false;
            if ((go.hideFlags & HideFlags.HideInHierarchy) != 0) return false;
            if (!go.name.EndsWith(OptimizedCopySuffix, StringComparison.Ordinal)) return false;
            return UsesGeneratedAssets(go);
        }

        static bool UsesGeneratedAssets(GameObject root)
        {
            if (root == null) return false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                SkinnedMeshRenderer skin = renderer as SkinnedMeshRenderer;
                if (skin != null && AssetPathIsGenerated(skin.sharedMesh)) return true;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && AssetPathIsGenerated(filter.sharedMesh)) return true;
                Material[] mats = renderer.sharedMaterials;
                if (mats == null) continue;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (AssetPathIsGenerated(mats[m])) return true;
                }
            }
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null && AssetPathIsGenerated(animators[i].runtimeAnimatorController))
                    return true;
            }
            return DescriptorUsesGenerated(root);
        }

        static bool DescriptorUsesGenerated(GameObject root)
        {
            Component desc = root.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (desc == null) return false;
            return FieldUsesGenerated(desc, "baseAnimationLayers") || FieldUsesGenerated(desc, "specialAnimationLayers");
        }

        static bool FieldUsesGenerated(Component desc, string fieldName)
        {
            FieldInfo layers = desc.GetType().GetField(fieldName);
            if (layers == null) return false;
            Array arr = layers.GetValue(desc) as Array;
            if (arr == null) return false;
            for (int i = 0; i < arr.Length; i++)
            {
                object layer = arr.GetValue(i);
                if (layer == null) continue;
                FieldInfo anim = layer.GetType().GetField("animatorController");
                if (anim == null) continue;
                if (AssetPathIsGenerated(anim.GetValue(layer) as UnityEngine.Object)) return true;
            }
            return false;
        }

        static bool AssetPathIsGenerated(UnityEngine.Object asset)
        {
            if (asset == null) return false;
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return false;
            return path.Replace('\\', '/').StartsWith(GeneratedFolderPath, StringComparison.Ordinal);
        }

        static void RemoveOptimizedPreviewCopies()
        {
            List<GameObject> copies = FindOptimizedPreviewCopies();
            GameObject selected = Selection.activeGameObject;
            for (int i = 0; i < copies.Count; i++)
            {
                GameObject copy = copies[i];
                if (copy == null) continue;
                GameObject original = FindOriginalForPreviewCopy(copy);
                bool selectOriginal = selected == copy;
                UnityEngine.Object.DestroyImmediate(copy);
                if (original != null)
                {
                    original.SetActive(true);
                    if (selectOriginal) Selection.activeGameObject = original;
                }
            }
        }

        static GameObject FindOriginalForPreviewCopy(GameObject copy)
        {
            if (copy == null) return null;
            string name = copy.name;
            if (!name.EndsWith(OptimizedCopySuffix, StringComparison.Ordinal)) return null;
            string originalName = name.Substring(0, name.Length - OptimizedCopySuffix.Length);
            if (string.IsNullOrEmpty(originalName)) return null;

            Transform parent = copy.transform.parent;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child != null && child.gameObject != copy && child.name == originalName)
                        return child.gameObject;
                }
            }

            Scene scene = copy.scene;
            if (!scene.IsValid() || !scene.isLoaded) return null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null && root != copy && root.name == originalName)
                    return root;
            }
            return null;
        }

        static string SafeAssetName(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "KaleidoMesh";
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
                sb.Append(Array.IndexOf(invalid, name[i]) >= 0 ? '_' : name[i]);
            string trimmed = sb.ToString().Trim();
            return trimmed.Length == 0 ? "KaleidoMesh" : trimmed;
        }

        static Mesh PersistMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                if (persistGenerated) throw new InvalidOperationException("Generated mesh was missing.");
                return null;
            }
            if (mesh.vertexCount == 0)
            {
                if (persistGenerated) throw new InvalidOperationException("Generated mesh has no vertices: " + mesh.name);
                return mesh;
            }
            mesh.hideFlags = persistGenerated ? HideFlags.None : HideFlags.HideAndDontSave;
            if (!persistGenerated) return mesh;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh))) return mesh;
            string path = AssetDatabase.GenerateUniqueAssetPath(EnsureGeneratedFolder() + "/" + SafeAssetName(mesh.name) + ".asset");
            AssetDatabase.CreateAsset(mesh, path);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                throw new InvalidOperationException("Could not save generated mesh: " + path);
            return mesh;
        }

        static AnimatorController PersistController(AnimatorController src)
        {
            if (src == null) return null;
            if (!persistGenerated)
            {
                AnimatorController tmp = UnityEngine.Object.Instantiate(src);
                tmp.name = src.name + "_KaleidoFX";
                tmp.hideFlags = HideFlags.HideAndDontSave;
                return tmp;
            }

            string folder = EnsureGeneratedFolder();
            string dest = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SafeAssetName(src.name) + "_KaleidoFX.controller");
            string srcPath = AssetDatabase.GetAssetPath(src);
            if (!string.IsNullOrEmpty(srcPath))
            {
                if (!AssetDatabase.CopyAsset(srcPath, dest))
                    throw new InvalidOperationException("Could not copy FX controller: " + src.name);
                AnimatorController copy = AssetDatabase.LoadAssetAtPath<AnimatorController>(dest);
                if (copy == null)
                    throw new InvalidOperationException("Copied FX controller did not load: " + dest);
                return copy;
            }

            AnimatorController inst = UnityEngine.Object.Instantiate(src);
            inst.name = src.name + "_KaleidoFX";
            inst.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(inst, dest);
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(inst)))
                throw new InvalidOperationException("Could not save FX controller: " + dest);
            return inst;
        }

        static void AssertMeshesSaved(GameObject root)
        {
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer smr = skins[i];
                if (smr == null) continue;
                Mesh mesh = smr.sharedMesh;
                if (mesh == null) continue;
                if (mesh.name.IndexOf("_Kaleido", StringComparison.Ordinal) < 0) continue;
                if (mesh.vertexCount == 0)
                    throw new InvalidOperationException("Skinned mesh has no vertices after On Upload: " + smr.name);
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                    throw new InvalidOperationException("Generated mesh was not saved: " + mesh.name);
            }
        }

        public static GameObject CreatePreviewCopy(GameObject source, KaleidoAvatarPassSettings settings, HashSet<Transform> extraExclusions)
        {
            if (source == null) return null;
            GameObject copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name + OptimizedCopySuffix;
            copy.transform.SetParent(source.transform.parent, false);
            copy.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            source.SetActive(false);
            try
            {
                KaleidoAvatarPassResult result = Run(copy, settings, false, RemapExclusions(source, copy, extraExclusions), true);
                if (result != null && !result.ok)
                    throw new InvalidOperationException(string.IsNullOrEmpty(result.failReason) ? "On Upload copy failed." : result.failReason);
            }
            catch (Exception ex)
            {
                UnityEngine.Object.DestroyImmediate(copy);
                source.SetActive(true);
                EditorUtility.DisplayDialog("KaleidoVR", "Could not create the optimized copy.\n\n" + ex.Message, "OK");
                return null;
            }
            Undo.RegisterCreatedObjectUndo(copy, "KaleidoVR Optimized Copy");
            Selection.activeGameObject = copy;
            return copy;
        }

        static HashSet<Transform> RemapExclusions(GameObject source, GameObject copy, HashSet<Transform> extra)
        {
            HashSet<Transform> mapped = new HashSet<Transform>();
            if (extra == null) return mapped;
            foreach (Transform t in extra)
            {
                if (t == null) continue;
                string path = AnimationUtility.CalculateTransformPath(t, source.transform);
                Transform found = string.IsNullOrEmpty(path) ? copy.transform : copy.transform.Find(path);
                if (found != null) mapped.Add(found);
            }
            return mapped;
        }

        static HashSet<Transform> CollectExclusions(GameObject root, HashSet<Transform> extra)
        {
            HashSet<Transform> set = new HashSet<Transform>();
            if (extra != null)
            {
                foreach (Transform t in extra)
                {
                    if (t == null) continue;
                    Transform[] children = t.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < children.Length; i++) set.Add(children[i]);
                }
            }
            return set;
        }

        static bool IsExcluded(Component c, HashSet<Transform> excluded)
        {
            return c != null && excluded != null && excluded.Contains(c.transform);
        }

        static bool IsEditorOnly(GameObject go)
        {
            Transform t = go != null ? go.transform : null;
            while (t != null)
            {
                if (t.CompareTag("EditorOnly")) return true;
                t = t.parent;
            }
            return false;
        }

        static bool IsOwnedType(Type type)
        {
            if (type == null) return true;
            string ns = type.Namespace ?? "";
            string asm = type.Assembly != null ? type.Assembly.GetName().Name : "";
            if (ns.StartsWith("Unity", StringComparison.Ordinal) || ns.StartsWith("TMPro", StringComparison.Ordinal)) return true;
            if (ns.StartsWith("KaleidoVR", StringComparison.Ordinal)) return true;
            if (ns.StartsWith("VRC", StringComparison.Ordinal) || ns.StartsWith("VRCSDK", StringComparison.Ordinal)) return true;
            if (asm.StartsWith("VRC", StringComparison.Ordinal) || asm.StartsWith("VRCSDK", StringComparison.Ordinal)) return true;
            return false;
        }

        static bool IsEditorMarker(Component c)
        {
            if (c == null) return false;
            Type[] ifaces = c.GetType().GetInterfaces();
            for (int i = 0; i < ifaces.Length; i++)
            {
                if (ifaces[i] != null && ifaces[i].Name == "IEditorOnly") return true;
            }
            return false;
        }

        static bool HasExternalWork(GameObject go, bool includeChildren)
        {
            if (go == null) return false;
            Component[] parts = includeChildren
                ? go.GetComponentsInChildren<Component>(true)
                : go.GetComponents<Component>();
            for (int i = 0; i < parts.Length; i++)
            {
                Component c = parts[i];
                if (c == null || c is Transform) continue;
                if (IsEditorMarker(c) || !IsOwnedType(c.GetType())) return true;
            }
            return false;
        }

        static bool IsExternalRuntimeAsset(UnityEngine.Object obj)
        {
            if (obj == null) return false;
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path)) return false;
            string n = obj.name;
            return string.IsNullOrEmpty(n) || n.IndexOf("_Kaleido", StringComparison.Ordinal) < 0;
        }

        static bool ShouldLeaveRenderer(Renderer renderer, HashSet<Transform> excluded)
        {
            return ShouldLeaveRenderer(renderer, excluded, null);
        }

        static bool ShouldLeaveRenderer(Renderer renderer, HashSet<Transform> excluded, GameObject root)
        {
            if (renderer == null || IsExcluded(renderer, excluded) || IsSensitiveMesh(renderer)) return true;
            if (IsAttachedExtra(renderer, root)) return true;
            SkinnedMeshRenderer smr = renderer as SkinnedMeshRenderer;
            Mesh mesh = smr != null ? smr.sharedMesh : null;
            return IsExternalRuntimeAsset(mesh);
        }

        static bool IsAttachedExtra(Renderer renderer, GameObject root)
        {
            if (renderer == null || root == null) return false;
            HashSet<Transform> human = CollectHumanoidBones(root);
            SkinnedMeshRenderer smr = renderer as SkinnedMeshRenderer;
            if (smr != null)
            {
                Transform[] bones = smr.bones;
                if (bones == null || bones.Length == 0)
                    return HasExternalWorkOnAncestors(smr.transform, root) && !IsUnderAvatarSkeleton(smr.transform, root, human);
                return !IsSkinnedToAvatarSkeleton(smr, root, human);
            }
            return HasExternalWorkOnAncestors(renderer.transform, root) && !IsUnderAvatarSkeleton(renderer.transform, root, human);
        }

        static bool HasExternalWorkOnAncestors(Transform t, GameObject root)
        {
            Transform stop = root.transform;
            while (t != null && t != stop)
            {
                if (HasExternalWork(t.gameObject, false)) return true;
                t = t.parent;
            }
            return false;
        }

        static bool IsSkinnedToAvatarSkeleton(SkinnedMeshRenderer smr, GameObject root, HashSet<Transform> human)
        {
            if (smr.rootBone != null && IsUnderAvatarSkeleton(smr.rootBone, root, human))
                return true;
            Transform[] bones = smr.bones;
            if (bones == null) return true;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null && IsUnderAvatarSkeleton(bones[i], root, human))
                    return true;
            }
            return false;
        }

        static bool IsUnderAvatarSkeleton(Transform t, GameObject root, HashSet<Transform> human)
        {
            if (human == null) return true;
            Transform stop = root.transform;
            while (t != null && t != stop)
            {
                if (human.Contains(t)) return true;
                t = t.parent;
            }
            return false;
        }

        static HashSet<Transform> CollectHumanoidBones(GameObject root)
        {
            Animator animator = root != null ? root.GetComponent<Animator>() : null;
            if (animator == null || !animator.isHuman) return null;
            HashSet<Transform> human = new HashSet<Transform>();
            for (int h = 0; h < (int)HumanBodyBones.LastBone; h++)
            {
                Transform bone = animator.GetBoneTransform((HumanBodyBones)h);
                if (bone != null) human.Add(bone);
            }
            return human;
        }

        static bool IsSensitiveMesh(Renderer renderer)
        {
            if (renderer == null) return false;
            string n = renderer.name;
            if (LooksSensitive(n)) return true;
            Material[] mats = renderer.sharedMaterials;
            if (mats == null) return false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && LooksSensitive(mats[i].name)) return true;
            }
            return false;
        }

        static bool LooksSensitive(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string lower = name.ToLowerInvariant();
            return lower.Contains("penetrat") || lower.Contains("orsifice")
                || lower.IndexOf("dps", StringComparison.Ordinal) >= 0
                || lower.IndexOf("tps", StringComparison.Ordinal) >= 0
                || lower.IndexOf("sps", StringComparison.Ordinal) >= 0
                || lower.Contains("orifice");
        }

        static void SweepUnused(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result, ComponentRefInfo refs)
        {
            Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
            List<Behaviour> remove = new List<Behaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour b = behaviours[i];
                if (b == null || b is Animator || b is AudioSource || IsExcluded(b, excluded)) continue;
                if (IsEditorMarker(b) || !IsOwnedType(b.GetType())) continue;
                if (b.enabled) continue;
                string path = AnimationUtility.CalculateTransformPath(b.transform, root.transform);
                if (anim.IsEnabledAnimated(path, b.GetType())) continue;
                remove.Add(b);
            }

            if (settings.removeUnusedComponents)
            {
                for (int i = 0; i < remove.Count; i++)
                {
                    result.componentsRemoved++;
                    result.lines.Add("Remove unused component: " + remove[i].GetType().Name + " on " + remove[i].name);
                    if (!dryRun) UnityEngine.Object.DestroyImmediate(remove[i]);
                }

                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int i = all.Length - 1; i >= 0; i--)
                {
                    if (all[i] == null || all[i] == root.transform || excluded.Contains(all[i])) continue;
                    if (!IsEditorOnly(all[i].gameObject)) continue;
                    if (HasExternalWork(all[i].gameObject, true)) continue;
                    result.objectsRemoved++;
                    result.lines.Add("Remove EditorOnly: " + all[i].name);
                    if (!dryRun) UnityEngine.Object.DestroyImmediate(all[i].gameObject);
                }
            }

            if (settings.removeUnusedGameObjects)
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int i = all.Length - 1; i >= 0; i--)
                {
                    Transform t = all[i];
                    if (t == null || t == root.transform || excluded.Contains(t)) continue;
                    if (HasExternalWork(t.gameObject, true)) continue;
                    if (t.gameObject.activeSelf) continue;
                    string path = AnimationUtility.CalculateTransformPath(t, root.transform);
                    if (anim.IsActiveAnimated(path)) continue;
                    if (HasRequiredRef(t, root, refs)) continue;
                    result.objectsRemoved++;
                    result.lines.Add("Remove unused object: " + t.name);
                    if (!dryRun) UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }
        }

        static bool HasRequiredRef(Transform t, GameObject root, ComponentRefInfo refs)
        {
            if (refs != null && refs.Keeps(t)) return true;
            Animator animator = root.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                {
                    if (animator.GetBoneTransform((HumanBodyBones)i) == t) return true;
                }
            }
            return false;
        }

        static bool IsProtectedShape(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            return n.IndexOf("blink", StringComparison.Ordinal) >= 0
                || n.IndexOf("wink", StringComparison.Ordinal) >= 0
                || n.IndexOf("squint", StringComparison.Ordinal) >= 0
                || n.IndexOf("eyelid", StringComparison.Ordinal) >= 0
                || n.IndexOf("eyeclose", StringComparison.Ordinal) >= 0
                || n.IndexOf("eye_close", StringComparison.Ordinal) >= 0
                || n.IndexOf("close_l", StringComparison.Ordinal) >= 0
                || n.IndexOf("close_r", StringComparison.Ordinal) >= 0
                || n.IndexOf("fcl_eye", StringComparison.Ordinal) >= 0
                || n.IndexOf("ウィンク", StringComparison.Ordinal) >= 0
                || n.IndexOf("まばたき", StringComparison.Ordinal) >= 0
                || n.IndexOf("じと目", StringComparison.Ordinal) >= 0;
        }

        static Dictionary<string, float> SnapshotShapeWeights(SkinnedMeshRenderer smr)
        {
            Dictionary<string, float> weights = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            Mesh mesh = smr != null ? smr.sharedMesh : null;
            if (mesh == null) return weights;
            for (int i = 0; i < mesh.blendShapeCount; i++)
                weights[mesh.GetBlendShapeName(i)] = smr.GetBlendShapeWeight(i);
            return weights;
        }

        static void RestoreShapeWeights(SkinnedMeshRenderer smr, Dictionary<string, float> weights)
        {
            Mesh mesh = smr != null ? smr.sharedMesh : null;
            if (mesh == null) return;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                float w;
                if (weights == null || !weights.TryGetValue(mesh.GetBlendShapeName(i), out w)) w = 0f;
                smr.SetBlendShapeWeight(i, w);
            }
        }

        static void ProcessBlendShapes(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            HashSet<string> keep = new HashSet<string>(anim.UsedBlendShapes, StringComparer.OrdinalIgnoreCase);
            if (settings.mmdCompatibility)
            {
                for (int i = 0; i < MmdShapeNames.Length; i++) keep.Add(MmdShapeNames[i]);
            }
            HashSet<string> descriptorShapes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDescriptorShapes(root, descriptorShapes);
            List<DescriptorShapeIndexMap> indexMaps = new List<DescriptorShapeIndexMap>();
            CollectIndexedDescriptorShapes(root, descriptorShapes, indexMaps);
            foreach (string name in descriptorShapes) keep.Add(name);
            HashSet<string> referencedShapes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddReferencedBlendShapes(root, referencedShapes);
            foreach (string name in referencedShapes) keep.Add(name);

            bool remapped = false;
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer smr = skins[s];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded, root)) continue;
                Mesh mesh = smr.sharedMesh;
                if (mesh.blendShapeCount == 0) continue;

                List<int> drop = new List<int>();
                if (settings.optimizeBlendShapes)
                {
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        string name = mesh.GetBlendShapeName(i);
                        if (IsProtectedShape(name) || keep.Contains(name) || anim.IsBlendShapeAnimated(smr, root, name))
                            continue;
                        if (Mathf.Abs(smr.GetBlendShapeWeight(i)) > 0.01f) continue;
                        drop.Add(i);
                    }
                }

                Dictionary<int, int> ratioInto = settings.mergeSameRatioShapes
                    ? anim.SameRatioPairs(smr, root, mesh)
                    : null;
                if (ratioInto != null)
                {
                    List<int> skip = new List<int>();
                    foreach (KeyValuePair<int, int> pair in ratioInto)
                    {
                        string na = mesh.GetBlendShapeName(pair.Key);
                        string nb = mesh.GetBlendShapeName(pair.Value);
                        if (IsProtectedShape(na) || IsProtectedShape(nb)
                            || descriptorShapes.Contains(na) || descriptorShapes.Contains(nb)
                            || referencedShapes.Contains(na) || referencedShapes.Contains(nb))
                            skip.Add(pair.Key);
                    }
                    for (int i = 0; i < skip.Count; i++) ratioInto.Remove(skip[i]);
                }

                if (drop.Count == 0 && (ratioInto == null || ratioInto.Count == 0)) continue;

                result.shapesRemoved += drop.Count;
                if (ratioInto != null) result.shapesMerged += ratioInto.Count;
                result.lines.Add(smr.name + ": drop " + drop.Count + ", merge " + (ratioInto != null ? ratioInto.Count : 0) + " blend shapes");
                if (dryRun) continue;

                Dictionary<string, float> weights = SnapshotShapeWeights(smr);
                Mesh copy = UnityEngine.Object.Instantiate(mesh);
                copy.name = mesh.name + "_Kaleido";
                StripShapes(copy, drop, ratioInto);
                smr.sharedMesh = PersistMesh(copy);
                RestoreShapeWeights(smr, weights);
                if (RemapIndexedDescriptorShapes(smr, copy, indexMaps)) remapped = true;
            }

            if (remapped)
            {
                Component desc = root.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
                if (desc != null) EditorUtility.SetDirty(desc);
            }
        }

        static void StripShapes(Mesh mesh, List<int> drop, Dictionary<int, int> ratioInto)
        {
            Vector3[] verts = mesh.vertices;
            HashSet<int> remove = new HashSet<int>(drop);
            if (ratioInto != null)
            {
                foreach (KeyValuePair<int, int> pair in ratioInto)
                    remove.Add(pair.Key);
            }

            List<(string name, float frame, Vector3[] v, Vector3[] n, Vector3[] t)> kept = new List<(string, float, Vector3[], Vector3[], Vector3[])>();
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (remove.Contains(i)) continue;
                string name = mesh.GetBlendShapeName(i);
                if (mesh.GetBlendShapeFrameCount(i) < 1) continue;
                Vector3[] kv = new Vector3[verts.Length];
                Vector3[] kn = new Vector3[verts.Length];
                Vector3[] kt = new Vector3[verts.Length];
                float frame = mesh.GetBlendShapeFrameWeight(i, 0);
                mesh.GetBlendShapeFrameVertices(i, 0, kv, kn, kt);
                if (ratioInto != null)
                {
                    foreach (KeyValuePair<int, int> pair in ratioInto)
                    {
                        if (pair.Value != i) continue;
                        Vector3[] ev = new Vector3[verts.Length];
                        Vector3[] en = new Vector3[verts.Length];
                        Vector3[] et = new Vector3[verts.Length];
                        if (mesh.GetBlendShapeFrameCount(pair.Key) < 1) continue;
                        mesh.GetBlendShapeFrameVertices(pair.Key, 0, ev, en, et);
                        float ratio = 1f;
                        for (int p = 0; p < verts.Length; p++)
                        {
                            kv[p] += ev[p] * ratio;
                            kn[p] += en[p] * ratio;
                            kt[p] += et[p] * ratio;
                        }
                    }
                }
                kept.Add((name, frame, kv, kn, kt));
            }

            mesh.ClearBlendShapes();
            for (int i = 0; i < kept.Count; i++)
                mesh.AddBlendShapeFrame(kept[i].name, kept[i].frame, kept[i].v, kept[i].n, kept[i].t);
        }

        static void AddDescriptorShapes(GameObject root, HashSet<string> keep)
        {
            Component desc = root.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (desc == null) return;
            Type t = desc.GetType();
            const BindingFlags fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo visemes = t.GetField("VisemeBlendShapes", fields);
            if (visemes != null)
            {
                string[] names = visemes.GetValue(desc) as string[];
                if (names != null)
                {
                    for (int i = 0; i < names.Length; i++)
                        if (!string.IsNullOrEmpty(names[i])) keep.Add(names[i]);
                }
            }
            FieldInfo mouth = t.GetField("MouthOpenBlendShapeName", fields);
            if (mouth != null)
            {
                string mouthName = mouth.GetValue(desc) as string;
                if (!string.IsNullOrEmpty(mouthName)) keep.Add(mouthName);
            }
            foreach (string field in new[] { "customEyeLookSettings", "lipSync" })
            {
                FieldInfo f = t.GetField(field, fields);
                if (f == null) continue;
                object val = f.GetValue(desc);
                if (val == null) continue;
                CollectStringFields(val, keep);
            }
        }

        static void CollectIndexedDescriptorShapes(GameObject root, HashSet<string> keep, List<DescriptorShapeIndexMap> maps)
        {
            Component desc = root.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (desc == null) return;
            const BindingFlags fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo eye = desc.GetType().GetField("customEyeLookSettings", fields);
            if (eye == null) return;
            object settings = eye.GetValue(desc);
            if (settings == null) return;

            Type st = settings.GetType();
            FieldInfo lidsMesh = st.GetField("eyelidsSkinnedMesh", fields);
            FieldInfo lidsIdx = st.GetField("eyelidsBlendshapes", fields);
            if (lidsMesh == null || lidsIdx == null) return;

            SkinnedMeshRenderer smr = lidsMesh.GetValue(settings) as SkinnedMeshRenderer;
            int[] indices = lidsIdx.GetValue(settings) as int[];
            Mesh mesh = smr != null ? smr.sharedMesh : null;
            if (smr == null || mesh == null || indices == null || indices.Length == 0) return;

            string[] names = new string[indices.Length];
            bool any = false;
            for (int i = 0; i < indices.Length; i++)
            {
                int idx = indices[i];
                if (idx < 0 || idx >= mesh.blendShapeCount) continue;
                string name = mesh.GetBlendShapeName(idx);
                if (string.IsNullOrEmpty(name)) continue;
                names[i] = name;
                keep.Add(name);
                any = true;
            }
            if (any)
            {
                DescriptorShapeIndexMap map = new DescriptorShapeIndexMap();
                map.descriptor = desc;
                map.settingsField = eye;
                map.settings = settings;
                map.indicesField = lidsIdx;
                map.smr = smr;
                map.indices = indices;
                map.names = names;
                maps.Add(map);
            }
        }

        static bool RemapIndexedDescriptorShapes(SkinnedMeshRenderer smr, Mesh newMesh, List<DescriptorShapeIndexMap> maps)
        {
            if (smr == null || newMesh == null || maps == null) return false;
            bool changed = false;
            for (int m = 0; m < maps.Count; m++)
            {
                DescriptorShapeIndexMap map = maps[m];
                if (map.smr != smr || map.indices == null || map.names == null) continue;
                int n = map.indices.Length;
                if (map.names.Length < n) n = map.names.Length;
                bool mapChanged = false;
                for (int i = 0; i < n; i++)
                {
                    if (string.IsNullOrEmpty(map.names[i])) continue;
                    int found = IndexOfBlendShape(newMesh, map.names[i]);
                    if (found < 0 || map.indices[i] == found) continue;
                    map.indices[i] = found;
                    mapChanged = true;
                    changed = true;
                }
                if (!mapChanged || map.descriptor == null) continue;
                SerializedObject so = new SerializedObject(map.descriptor);
                SerializedProperty prop = so.FindProperty("customEyeLookSettings.eyelidsBlendshapes");
                if (prop != null && prop.isArray)
                {
                    int count = prop.arraySize;
                    if (map.indices.Length < count) count = map.indices.Length;
                    for (int i = 0; i < count; i++)
                        prop.GetArrayElementAtIndex(i).intValue = map.indices[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else if (map.indicesField != null && map.settings != null)
                {
                    map.indicesField.SetValue(map.settings, map.indices);
                    if (map.settingsField != null)
                        map.settingsField.SetValue(map.descriptor, map.settings);
                }
            }
            return changed;
        }

        static int IndexOfBlendShape(Mesh mesh, string name)
        {
            if (mesh == null || string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (string.Equals(mesh.GetBlendShapeName(i), name, StringComparison.Ordinal))
                    return i;
            }
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (string.Equals(mesh.GetBlendShapeName(i), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        static void CollectStringFields(object obj, HashSet<string> keep)
        {
            if (obj == null) return;
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(string))
                {
                    string s = fields[i].GetValue(obj) as string;
                    if (!string.IsNullOrEmpty(s)) keep.Add(s);
                }
                else if (fields[i].FieldType == typeof(string[]))
                {
                    string[] arr = fields[i].GetValue(obj) as string[];
                    if (arr == null) continue;
                    for (int a = 0; a < arr.Length; a++)
                        if (!string.IsNullOrEmpty(arr[a])) keep.Add(arr[a]);
                }
            }
        }

        static void AddReferencedBlendShapes(GameObject root, HashSet<string> keep)
        {
            if (root == null || keep == null) return;
            HashSet<string> known = CollectKnownBlendShapeNames(root);
            if (known.Count == 0) return;

            HashSet<int> visited = new HashSet<int>();
            Component[] parts = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                Component c = parts[i];
                if (!ShouldScanForBlendShapeNames(c)) continue;
                try
                {
                    HarvestSerializedShapeNames(c, known, keep, visited);
                }
                catch (Exception)
                {
                }
            }
        }

        static HashSet<string> CollectKnownBlendShapeNames(GameObject root)
        {
            HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (root == null) return known;
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int s = 0; s < skins.Length; s++)
            {
                Mesh mesh = skins[s] != null ? skins[s].sharedMesh : null;
                if (mesh == null) continue;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string name = mesh.GetBlendShapeName(i);
                    if (!string.IsNullOrEmpty(name)) known.Add(name);
                }
            }
            return known;
        }

        static bool ShouldScanForBlendShapeNames(Component c)
        {
            if (c == null || c is Transform) return false;
            if (c is Renderer || c is MeshFilter || c is Animator) return false;
            if (c is AudioSource) return true;
            if (c is Camera || c is Light || c is ParticleSystem) return false;
            if (c is Collider || c is Rigidbody || c is Joint) return false;
            Type t = c.GetType();
            string ns = t.Namespace ?? "";
            if (ns.StartsWith("UnityEngine", StringComparison.Ordinal) || ns.StartsWith("UnityEditor", StringComparison.Ordinal))
                return false;
            return true;
        }

        static void HarvestSerializedShapeNames(UnityEngine.Object obj, HashSet<string> known, HashSet<string> keep, HashSet<int> visited)
        {
            if (obj == null || known == null || keep == null || visited == null) return;
            if (!visited.Add(obj.GetInstanceID())) return;

            List<ScriptableObject> nested = null;
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty p = so.GetIterator();
            while (p.Next(true))
            {
                if (p.propertyType == SerializedPropertyType.String)
                {
                    TryKeepReferencedShape(p.stringValue, known, keep);
                    continue;
                }
                if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                ScriptableObject asset = p.objectReferenceValue as ScriptableObject;
                if (asset == null || asset is MonoScript) continue;
                if (nested == null) nested = new List<ScriptableObject>();
                nested.Add(asset);
            }
            if (nested == null) return;
            for (int i = 0; i < nested.Count; i++)
                HarvestSerializedShapeNames(nested[i], known, keep, visited);
        }

        static void TryKeepReferencedShape(string raw, HashSet<string> known, HashSet<string> keep)
        {
            if (string.IsNullOrEmpty(raw) || known == null || keep == null) return;
            string name = raw.Trim();
            if (name.StartsWith("blendShape.", StringComparison.OrdinalIgnoreCase))
                name = name.Substring("blendShape.".Length);
            if (known.Contains(name)) keep.Add(name);
        }

        static void StripUnusedBones(GameObject root, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result, ComponentRefInfo refs)
        {
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer smr = skins[s];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded, root)) continue;
                Transform[] bones = smr.bones;
                if (bones == null || bones.Length == 0) continue;
                Mesh mesh = smr.sharedMesh;
                bool[] used = new bool[bones.Length];
                if (!MarkUsedBones(mesh, used)) continue;

                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] == null) continue;
                    if (anim.IsTransformMoved(bones[i], root.transform)) used[i] = true;
                    else if (refs != null && refs.Keeps(bones[i])) used[i] = true;
                }

                int keepCount = 0;
                for (int i = 0; i < used.Length; i++) if (used[i]) keepCount++;
                if (keepCount == 0 || keepCount == bones.Length) continue;

                result.bonesRemoved += bones.Length - keepCount;
                result.lines.Add(smr.name + ": drop " + (bones.Length - keepCount) + " unused bones");
                if (dryRun) continue;

                int[] map = new int[bones.Length];
                Transform[] newBones = new Transform[keepCount];
                Matrix4x4[] oldBind = mesh.bindposes;
                Matrix4x4[] newBind = new Matrix4x4[keepCount];
                int n = 0;
                for (int i = 0; i < bones.Length; i++)
                {
                    if (!used[i]) { map[i] = 0; continue; }
                    map[i] = n;
                    newBones[n] = bones[i];
                    if (oldBind != null && i < oldBind.Length) newBind[n] = oldBind[i];
                    n++;
                }

                Mesh copy = UnityEngine.Object.Instantiate(mesh);
                copy.name = mesh.name + "_KaleidoBones";
                RemapMeshBoneWeights(copy, used, map);
                copy.bindposes = newBind;
                smr.sharedMesh = PersistMesh(copy);
                smr.bones = newBones;
            }
        }

        static void MergeSlotsOnRenderers(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result, ComponentRefInfo refs)
        {
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer smr = skins[s];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded, root)) continue;
                if (refs != null && refs.Keeps(smr)) continue;
                Material[] mats = smr.sharedMaterials;
                Mesh mesh = smr.sharedMesh;
                if (mats == null || mesh.subMeshCount <= 1) continue;
                if (anim.HasMaterialSwap(smr, root)) continue;

                int[] order = new int[mesh.subMeshCount];
                for (int i = 0; i < order.Length; i++) order[i] = i;
                if (settings.shuffleMaterialSlots)
                {
                    Array.Sort(order, (a, b) =>
                    {
                        string na = a < mats.Length && mats[a] != null ? mats[a].name : "";
                        string nb = b < mats.Length && mats[b] != null ? mats[b].name : "";
                        return string.CompareOrdinal(na, nb);
                    });
                }

                List<int> remap = new List<int>();
                List<Material> newMats = new List<Material>();
                List<List<int>> tris = new List<List<int>>();
                for (int o = 0; o < order.Length; o++)
                {
                    int i = order[o];
                    Material mat = i < mats.Length ? mats[i] : null;
                    if (anim.IsMaterialSlotSwapped(smr, root, i) || !settings.mergeIdenticalSlots)
                    {
                        remap.Add(newMats.Count);
                        newMats.Add(mat);
                        tris.Add(new List<int>(mesh.GetTriangles(i)));
                        continue;
                    }
                    int found = -1;
                    for (int m = 0; m < newMats.Count; m++)
                    {
                        if (newMats[m] == mat && !anim.IsMaterialSlotSwapped(smr, root, i)) { found = m; break; }
                    }
                    if (found >= 0)
                    {
                        tris[found].AddRange(mesh.GetTriangles(i));
                        remap.Add(found);
                    }
                    else
                    {
                        remap.Add(newMats.Count);
                        newMats.Add(mat);
                        tris.Add(new List<int>(mesh.GetTriangles(i)));
                    }
                }

                if (newMats.Count >= mesh.subMeshCount) continue;
                result.slotsMerged += mesh.subMeshCount - newMats.Count;
                result.lines.Add(smr.name + ": " + mesh.subMeshCount + " slots → " + newMats.Count);
                if (dryRun) continue;

                Mesh copy = UnityEngine.Object.Instantiate(mesh);
                copy.name = mesh.name + "_KaleidoSlots";
                copy.subMeshCount = newMats.Count;
                for (int i = 0; i < newMats.Count; i++) copy.SetTriangles(tris[i], i);
                smr.sharedMesh = PersistMesh(copy);
                smr.sharedMaterials = newMats.ToArray();
            }
        }

        static void MergeTogetherMeshes(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result, ComponentRefInfo refs)
        {
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Dictionary<string, List<SkinnedMeshRenderer>> groups = new Dictionary<string, List<SkinnedMeshRenderer>>();
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer smr = skins[i];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded, root)) continue;
                if (refs != null && refs.Keeps(smr)) continue;
                if (smr.sharedMesh.blendShapeCount > 0) continue;
                string key = anim.TogetherKey(smr, root);
                if (string.IsNullOrEmpty(key)) continue;
                List<SkinnedMeshRenderer> list;
                if (!groups.TryGetValue(key, out list))
                {
                    list = new List<SkinnedMeshRenderer>();
                    groups[key] = list;
                }
                list.Add(smr);
            }

            foreach (KeyValuePair<string, List<SkinnedMeshRenderer>> pair in groups)
            {
                if (pair.Value.Count < 2) continue;
                result.meshesMerged += pair.Value.Count - 1;
                StringBuilder names = new StringBuilder();
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (i > 0) names.Append(", ");
                    names.Append(pair.Value[i].name);
                }
                result.lines.Add("Merge meshes: " + names);
                if (dryRun) continue;
                CombineSkinned(pair.Value, settings);
            }
        }

        static void CombineSkinned(List<SkinnedMeshRenderer> group, KaleidoAvatarPassSettings settings)
        {
            SkinnedMeshRenderer dest = group[0];
            List<Transform> bones = new List<Transform>();
            List<Matrix4x4> binds = new List<Matrix4x4>();
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector4> tans = new List<Vector4>();
            List<Vector2>[] uvs = new List<Vector2>[8];
            bool[] anyUv = new bool[8];
            for (int c = 0; c < 8; c++) uvs[c] = new List<Vector2>();
            List<Color32> colors = new List<Color32>();
            bool anyColor = false;
            List<byte> bonesPerVertex = new List<byte>();
            List<BoneWeight1> mappedWeights = new List<BoneWeight1>();
            List<Material> mats = new List<Material>();
            List<int[]> subTris = new List<int[]>();
            Bounds localBox = dest.localBounds;

            for (int g = 0; g < group.Count; g++)
            {
                SkinnedMeshRenderer smr = group[g];
                Mesh mesh = smr.sharedMesh;
                Transform[] sb = smr.bones;
                Matrix4x4[] bp = mesh.bindposes;
                Matrix4x4 local = dest.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                Matrix4x4 invLocal = local.inverse;
                if (g > 0) localBox.Encapsulate(TransformLocalBounds(smr.localBounds, local));
                int[] boneMap = new int[sb != null ? sb.Length : 0];
                for (int b = 0; b < boneMap.Length; b++)
                {
                    Transform bone = sb[b];
                    Matrix4x4 bind = (bp != null && b < bp.Length ? bp[b] : Matrix4x4.identity) * invLocal;
                    int found = FindBoneSlot(bones, binds, bone, bind);
                    if (found < 0)
                    {
                        found = bones.Count;
                        bones.Add(bone);
                        binds.Add(bind);
                    }
                    boneMap[b] = found;
                }

                int vertBase = verts.Count;
                Vector3[] mv = mesh.vertices;
                Vector3[] mn = mesh.normals;
                Vector4[] mt = mesh.tangents;
                for (int i = 0; i < mv.Length; i++)
                {
                    verts.Add(local.MultiplyPoint3x4(mv[i]));
                    norms.Add(mn != null && i < mn.Length ? local.MultiplyVector(mn[i]).normalized : Vector3.up);
                    if (mt != null && i < mt.Length)
                    {
                        Vector3 tv = local.MultiplyVector(new Vector3(mt[i].x, mt[i].y, mt[i].z));
                        tans.Add(new Vector4(tv.x, tv.y, tv.z, mt[i].w));
                    }
                    else tans.Add(new Vector4(1, 0, 0, 1));
                }
                AppendUvsAndColors(mesh, mv.Length, uvs, anyUv, colors, ref anyColor);
                AppendMappedWeights(mesh, boneMap, bonesPerVertex, mappedWeights);

                Material[] sm = smr.sharedMaterials;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    int[] tri = mesh.GetTriangles(sub);
                    for (int t = 0; t < tri.Length; t++) tri[t] += vertBase;
                    Material mat = sm != null && sub < sm.Length ? sm[sub] : null;
                    int slot = -1;
                    if (settings.mergeIdenticalSlots)
                    {
                        for (int m = 0; m < mats.Count; m++)
                            if (mats[m] == mat) { slot = m; break; }
                    }
                    if (slot >= 0)
                    {
                        int[] old = subTris[slot];
                        int[] merged = new int[old.Length + tri.Length];
                        old.CopyTo(merged, 0);
                        tri.CopyTo(merged, old.Length);
                        subTris[slot] = merged;
                    }
                    else
                    {
                        mats.Add(mat);
                        subTris.Add(tri);
                    }
                }
            }

            Mesh combined = new Mesh();
            combined.name = dest.name + "_KaleidoMerged";
            combined.indexFormat = verts.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            combined.SetVertices(verts);
            combined.SetNormals(norms);
            combined.SetTangents(tans);
            for (int c = 0; c < 8; c++)
            {
                if (anyUv[c]) combined.SetUVs(c, uvs[c]);
            }
            if (anyColor) combined.colors32 = colors.ToArray();
            ApplyMappedBoneWeights(combined, bonesPerVertex, mappedWeights);
            combined.bindposes = binds.ToArray();
            combined.subMeshCount = subTris.Count;
            for (int i = 0; i < subTris.Count; i++) combined.SetTriangles(subTris[i], i);
            combined.RecalculateBounds();
            dest.sharedMesh = PersistMesh(combined);
            dest.bones = bones.ToArray();
            dest.sharedMaterials = mats.ToArray();
            dest.localBounds = localBox;

            for (int g = 1; g < group.Count; g++)
            {
                SkinnedMeshRenderer smr = group[g];
                if (smr != null) UnityEngine.Object.DestroyImmediate(smr);
            }
        }

        static bool MarkUsedBones(Mesh mesh, bool[] used)
        {
            if (mesh == null || used == null || used.Length == 0) return false;
            NativeArray<byte> per = default(NativeArray<byte>);
            NativeArray<BoneWeight1> src = default(NativeArray<BoneWeight1>);
            try
            {
                per = mesh.GetBonesPerVertex();
                src = mesh.GetAllBoneWeights();
                if (per.IsCreated && src.IsCreated && per.Length == mesh.vertexCount)
                {
                    for (int i = 0; i < src.Length; i++)
                    {
                        BoneWeight1 w = src[i];
                        if (w.weight > 0f && w.boneIndex >= 0 && w.boneIndex < used.Length)
                            used[w.boneIndex] = true;
                    }
                    return true;
                }
            }
            finally
            {
                if (per.IsCreated) per.Dispose();
                if (src.IsCreated) src.Dispose();
            }

            BoneWeight[] weights = mesh.boneWeights;
            if (weights == null || weights.Length != mesh.vertexCount) return false;
            for (int i = 0; i < weights.Length; i++)
            {
                BoneWeight w = weights[i];
                if (w.weight0 > 0f && w.boneIndex0 >= 0 && w.boneIndex0 < used.Length) used[w.boneIndex0] = true;
                if (w.weight1 > 0f && w.boneIndex1 >= 0 && w.boneIndex1 < used.Length) used[w.boneIndex1] = true;
                if (w.weight2 > 0f && w.boneIndex2 >= 0 && w.boneIndex2 < used.Length) used[w.boneIndex2] = true;
                if (w.weight3 > 0f && w.boneIndex3 >= 0 && w.boneIndex3 < used.Length) used[w.boneIndex3] = true;
            }
            return true;
        }

        static Bounds TransformLocalBounds(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            Bounds result = new Bounds(matrix.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                Vector3 offset = new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                result.Encapsulate(matrix.MultiplyPoint3x4(c + offset));
            }
            return result;
        }

        static void AppendUvsAndColors(Mesh mesh, int vertCount, List<Vector2>[] uvs, bool[] anyUv, List<Color32> colors, ref bool anyColor)
        {
            for (int c = 0; c < 8; c++)
            {
                List<Vector2> src = new List<Vector2>();
                mesh.GetUVs(c, src);
                bool have = src.Count == vertCount;
                if (have) anyUv[c] = true;
                for (int i = 0; i < vertCount; i++)
                    uvs[c].Add(have ? src[i] : Vector2.zero);
            }
            if (mesh.HasVertexAttribute(VertexAttribute.Color))
            {
                Color32[] srcColors = mesh.colors32;
                if (srcColors != null && srcColors.Length == vertCount)
                {
                    anyColor = true;
                    colors.AddRange(srcColors);
                    return;
                }
            }
            for (int i = 0; i < vertCount; i++) colors.Add(new Color32(255, 255, 255, 255));
        }

        static void AppendMappedWeights(Mesh mesh, int[] boneMap, List<byte> perVertex, List<BoneWeight1> weights)
        {
            NativeArray<byte> per = default(NativeArray<byte>);
            NativeArray<BoneWeight1> src = default(NativeArray<BoneWeight1>);
            try
            {
                per = mesh.GetBonesPerVertex();
                src = mesh.GetAllBoneWeights();
                if (per.IsCreated && per.Length == mesh.vertexCount)
                {
                    int offset = 0;
                    for (int v = 0; v < per.Length; v++)
                    {
                        int n = per[v];
                        int written = 0;
                        for (int k = 0; k < n; k++)
                        {
                            BoneWeight1 w = src[offset + k];
                            w.boneIndex = SafeMap(w.boneIndex, boneMap);
                            if (w.weight <= 0f) continue;
                            weights.Add(w);
                            written++;
                        }
                        offset += n;
                        perVertex.Add((byte)written);
                    }
                    return;
                }
            }
            finally
            {
                if (per.IsCreated) per.Dispose();
                if (src.IsCreated) src.Dispose();
            }

            BoneWeight[] mw = mesh.boneWeights;
            int count = mesh.vertexCount;
            for (int i = 0; i < count; i++)
            {
                BoneWeight w = mw != null && i < mw.Length ? mw[i] : default(BoneWeight);
                int start = weights.Count;
                AddMappedWeight(weights, SafeMap(w.boneIndex0, boneMap), w.weight0);
                AddMappedWeight(weights, SafeMap(w.boneIndex1, boneMap), w.weight1);
                AddMappedWeight(weights, SafeMap(w.boneIndex2, boneMap), w.weight2);
                AddMappedWeight(weights, SafeMap(w.boneIndex3, boneMap), w.weight3);
                perVertex.Add((byte)(weights.Count - start));
            }
        }

        static void AddMappedWeight(List<BoneWeight1> list, int bone, float weight)
        {
            if (weight <= 0f) return;
            list.Add(new BoneWeight1 { boneIndex = bone, weight = weight });
        }

        static void ApplyMappedBoneWeights(Mesh mesh, List<byte> perVertex, List<BoneWeight1> weights)
        {
            NativeArray<byte> per = new NativeArray<byte>(perVertex.ToArray(), Allocator.Temp);
            NativeArray<BoneWeight1> w = new NativeArray<BoneWeight1>(weights.ToArray(), Allocator.Temp);
            try
            {
                mesh.SetBoneWeights(per, w);
            }
            finally
            {
                per.Dispose();
                w.Dispose();
            }
        }

        static void RemapMeshBoneWeights(Mesh mesh, bool[] used, int[] map)
        {
            NativeArray<byte> per = default(NativeArray<byte>);
            NativeArray<BoneWeight1> src = default(NativeArray<BoneWeight1>);
            try
            {
                per = mesh.GetBonesPerVertex();
                src = mesh.GetAllBoneWeights();
                if (per.IsCreated && per.Length == mesh.vertexCount)
                {
                    List<BoneWeight1> kept = new List<BoneWeight1>(src.Length);
                    byte[] newPer = new byte[per.Length];
                    int offset = 0;
                    for (int v = 0; v < per.Length; v++)
                    {
                        int n = per[v];
                        int start = kept.Count;
                        float sum = 0f;
                        for (int i = 0; i < n; i++)
                        {
                            BoneWeight1 w = src[offset + i];
                            if (w.boneIndex < 0 || w.boneIndex >= used.Length || !used[w.boneIndex] || w.weight <= 0f)
                                continue;
                            kept.Add(new BoneWeight1 { boneIndex = map[w.boneIndex], weight = w.weight });
                            sum += w.weight;
                        }
                        offset += n;
                        if (kept.Count > start && sum > 0f && Mathf.Abs(sum - 1f) > 0.001f)
                        {
                            for (int i = start; i < kept.Count; i++)
                            {
                                BoneWeight1 w = kept[i];
                                w.weight /= sum;
                                kept[i] = w;
                            }
                        }
                        newPer[v] = (byte)(kept.Count - start);
                    }
                    ApplyMappedBoneWeights(mesh, new List<byte>(newPer), kept);
                    return;
                }
            }
            finally
            {
                if (per.IsCreated) per.Dispose();
                if (src.IsCreated) src.Dispose();
            }

            BoneWeight[] weights = mesh.boneWeights;
            if (weights == null) return;
            BoneWeight[] nw = new BoneWeight[weights.Length];
            for (int i = 0; i < weights.Length; i++)
                nw[i] = RemapWeight(weights[i], used, map);
            mesh.boneWeights = nw;
        }

        static BoneWeight RemapWeight(BoneWeight w, bool[] used, int[] map)
        {
            BoneWeight n = new BoneWeight();
            n.weight0 = MappedWeight(w.weight0, w.boneIndex0, used);
            n.boneIndex0 = MappedIndex(w.weight0, w.boneIndex0, used, map);
            n.weight1 = MappedWeight(w.weight1, w.boneIndex1, used);
            n.boneIndex1 = MappedIndex(w.weight1, w.boneIndex1, used, map);
            n.weight2 = MappedWeight(w.weight2, w.boneIndex2, used);
            n.boneIndex2 = MappedIndex(w.weight2, w.boneIndex2, used, map);
            n.weight3 = MappedWeight(w.weight3, w.boneIndex3, used);
            n.boneIndex3 = MappedIndex(w.weight3, w.boneIndex3, used, map);
            return n;
        }

        static float MappedWeight(float srcWeight, int srcIndex, bool[] used)
        {
            if (srcIndex < 0 || srcIndex >= used.Length || !used[srcIndex] || srcWeight <= 0f) return 0f;
            return srcWeight;
        }

        static int MappedIndex(float srcWeight, int srcIndex, bool[] used, int[] map)
        {
            if (srcIndex < 0 || srcIndex >= used.Length || !used[srcIndex] || srcWeight <= 0f) return 0;
            return map[srcIndex];
        }

        static int SafeMap(int index, int[] map)
        {
            if (map == null || map.Length == 0) return 0;
            if (index < 0 || index >= map.Length) return 0;
            return map[index];
        }

        static int FindBoneSlot(List<Transform> bones, List<Matrix4x4> binds, Transform bone, Matrix4x4 bind)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i] != bone) continue;
                if (BindposesMatch(binds[i], bind)) return i;
            }
            return -1;
        }

        static bool BindposesMatch(Matrix4x4 a, Matrix4x4 b)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > 0.0001f) return false;
            }
            return true;
        }

        static void SweepPhysBones(GameObject root, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result, ComponentRefInfo refs)
        {
            Type pbType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone");
            Type colType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider");
            if (pbType == null) return;

            Component[] pbs = root.GetComponentsInChildren(pbType, true);
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            FieldInfo rootField = pbType.GetField("rootTransform") ?? pbType.GetField("m_RootTransform");

            for (int i = 0; i < pbs.Length; i++)
            {
                Component pb = pbs[i];
                if (pb == null || IsExcluded(pb, excluded)) continue;
                Transform pbRoot = rootField != null ? rootField.GetValue(pb) as Transform : pb.transform;
                if (pbRoot == null) pbRoot = pb.transform;
                if (refs != null && (refs.Keeps(pb) || refs.Keeps(pbRoot))) continue;
                bool used = false;
                for (int s = 0; s < skins.Length; s++)
                {
                    SkinnedMeshRenderer smr = skins[s];
                    if (smr == null || smr.sharedMesh == null) continue;
                    if (RendererUsesTransform(smr, pbRoot)) { used = true; break; }
                }
                string path = AnimationUtility.CalculateTransformPath(pb.transform, root.transform);
                if (!used && !anim.IsEnabledAnimated(path, pbType))
                {
                    result.physBonesDisabled++;
                    result.lines.Add("Remove unused PhysBone: " + pb.name);
                    if (!dryRun) UnityEngine.Object.DestroyImmediate(pb);
                }
            }

            if (colType == null) return;
            Component[] cols = root.GetComponentsInChildren(colType, true);
            HashSet<Component> referenced = new HashSet<Component>();
            FieldInfo colList = pbType.GetField("colliders");
            pbs = root.GetComponentsInChildren(pbType, true);
            for (int i = 0; i < pbs.Length; i++)
            {
                if (pbs[i] == null || colList == null) continue;
                System.Collections.IList list = colList.GetValue(pbs[i]) as System.Collections.IList;
                if (list == null) continue;
                for (int c = 0; c < list.Count; c++)
                {
                    Component col = list[c] as Component;
                    if (col != null) referenced.Add(col);
                }
            }
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null || referenced.Contains(cols[i]) || IsExcluded(cols[i], excluded)) continue;
                if (refs != null && refs.Keeps(cols[i])) continue;
                result.componentsRemoved++;
                result.lines.Add("Remove unused PhysBone collider: " + cols[i].name);
                if (!dryRun) UnityEngine.Object.DestroyImmediate(cols[i]);
            }
        }

        static bool RendererUsesTransform(SkinnedMeshRenderer smr, Transform bone)
        {
            if (smr.rootBone == bone) return true;
            Transform[] bones = smr.bones;
            if (bones == null) return false;
            for (int i = 0; i < bones.Length; i++)
            {
                Transform t = bones[i];
                while (t != null)
                {
                    if (t == bone) return true;
                    t = t.parent;
                }
            }
            return false;
        }

        static void OptimizeFx(GameObject root, KaleidoAvatarPassSettings settings, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            Dictionary<AnimatorController, AnimatorController> rewritten = new Dictionary<AnimatorController, AnimatorController>();
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int a = 0; a < animators.Length; a++)
            {
                Animator animator = animators[a];
                if (animator == null || IsExcluded(animator, excluded)) continue;
                AnimatorController src = animator.runtimeAnimatorController as AnimatorController;
                AnimatorController copy = RewriteController(src, root, settings, dryRun, result, rewritten);
                if (!dryRun && copy != null && copy != src)
                    animator.runtimeAnimatorController = copy;
            }

            Component desc = root.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (desc == null) return;
            RewriteDescriptorLayers(desc, "baseAnimationLayers", root, settings, excluded, dryRun, result, rewritten);
            RewriteDescriptorLayers(desc, "specialAnimationLayers", root, settings, excluded, dryRun, result, rewritten);
        }

        static void RewriteDescriptorLayers(Component desc, string fieldName, GameObject root, KaleidoAvatarPassSettings settings, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result, Dictionary<AnimatorController, AnimatorController> rewritten)
        {
            FieldInfo layers = desc.GetType().GetField(fieldName);
            if (layers == null) return;
            Array arr = layers.GetValue(desc) as Array;
            if (arr == null) return;
            bool changed = false;
            for (int i = 0; i < arr.Length; i++)
            {
                object layer = arr.GetValue(i);
                if (layer == null) continue;
                FieldInfo anim = layer.GetType().GetField("animatorController");
                if (anim == null) continue;
                AnimatorController src = anim.GetValue(layer) as AnimatorController;
                AnimatorController copy = RewriteController(src, root, settings, dryRun, result, rewritten);
                if (dryRun || copy == null || copy == src) continue;
                anim.SetValue(layer, copy);
                arr.SetValue(layer, i);
                changed = true;
            }
            if (changed) layers.SetValue(desc, arr);
        }

        static AnimatorController RewriteController(AnimatorController src, GameObject root, KaleidoAvatarPassSettings settings, bool dryRun, KaleidoAvatarPassResult result, Dictionary<AnimatorController, AnimatorController> rewritten)
        {
            if (src == null || IsExternalRuntimeAsset(src)) return src;
            AnimatorController cached;
            if (rewritten.TryGetValue(src, out cached)) return cached;

            int deadCurves = 0;
            int deadLayers = 0;
            AnimatorControllerLayer[] layers = src.layers;
            for (int l = 0; l < layers.Length; l++)
            {
                if (settings.mmdCompatibility && l < 3) continue;
                AnimatorControllerLayer layer = layers[l];
                if (IsHandGestureLayer(layer)) continue;
                if (layer.stateMachine == null || (layer.stateMachine.states.Length == 0 && layer.stateMachine.stateMachines.Length == 0))
                {
                    deadLayers++;
                    continue;
                }
                deadCurves += CountMissingCurves(layer, root);
            }

            if (deadLayers == 0 && deadCurves == 0)
            {
                rewritten[src] = src;
                return src;
            }
            result.fxLayersRemoved += deadLayers;
            result.curvesRemoved += deadCurves;
            result.lines.Add(src.name + " FX: drop " + deadLayers + " empty layer(s), " + deadCurves + " missing curve(s)");
            if (dryRun)
            {
                rewritten[src] = src;
                return src;
            }

            AnimatorController copy = PersistController(src);
            if (copy == null)
            {
                rewritten[src] = src;
                return src;
            }
            StripFx(copy, root, settings.mmdCompatibility);
            rewritten[src] = copy;
            rewritten[copy] = copy;
            return copy;
        }

        static int CountMissingCurves(AnimatorControllerLayer layer, GameObject root)
        {
            int n = 0;
            foreach (AnimationClip clip in CollectClips(layer.stateMachine))
            {
                if (clip == null) continue;
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (!BindingExists(root, bindings[i])) n++;
                }
            }
            return n;
        }

        static void StripFx(AnimatorController controller, GameObject root, bool keepMmdLayers)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            List<AnimatorControllerLayer> keep = new List<AnimatorControllerLayer>();
            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorControllerLayer layer = layers[i];
                if ((keepMmdLayers && i < 3) || IsHandGestureLayer(layer))
                {
                    keep.Add(layer);
                    continue;
                }
                if (layer.stateMachine == null || (layer.stateMachine.states.Length == 0 && layer.stateMachine.stateMachines.Length == 0 && layer.defaultWeight <= 0f))
                    continue;
                foreach (AnimationClip clip in CollectClips(layer.stateMachine))
                    StripMissing(clip, root);
                keep.Add(layer);
            }
            if (keep.Count != layers.Length) controller.layers = keep.ToArray();
        }

        static void StripMissing(AnimationClip clip, GameObject root)
        {
            if (clip == null) return;
            string clipPath = AssetDatabase.GetAssetPath(clip);
            if (!string.IsNullOrEmpty(clipPath)
                && !clipPath.Replace('\\', '/').StartsWith(GeneratedFolderPath, StringComparison.Ordinal))
                return;
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                if (BindingExists(root, bindings[i])) continue;
                AnimationUtility.SetEditorCurve(clip, bindings[i], null);
            }
        }

        static bool BindingExists(GameObject root, EditorCurveBinding binding)
        {
            Transform t = string.IsNullOrEmpty(binding.path) ? root.transform : root.transform.Find(binding.path);
            if (t == null) return false;
            if (binding.type == typeof(GameObject) || binding.type == typeof(Transform)) return true;
            return t.GetComponent(binding.type) != null;
        }

        static readonly string[] HandGestureStateNames =
        {
            "idle", "fist", "open", "point", "peace", "rocknroll", "gun", "thumbsup"
        };

        static bool IsHandGestureLayer(AnimatorControllerLayer layer)
        {
            if (layer == null || layer.stateMachine == null) return false;
            if (!LayerUsesHandGestureParameter(layer.stateMachine)) return false;
            return CountStandardGestureStates(layer.stateMachine) >= 4;
        }

        static bool LayerUsesHandGestureParameter(AnimatorStateMachine machine)
        {
            if (machine == null) return false;
            if (TransitionsUseHandGestureParameter(machine.anyStateTransitions)) return true;
            if (TransitionsUseHandGestureParameter(machine.entryTransitions)) return true;
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && TransitionsUseHandGestureParameter(states[i].state.transitions))
                    return true;
            }
            ChildAnimatorStateMachine[] subs = machine.stateMachines;
            for (int i = 0; i < subs.Length; i++)
            {
                if (LayerUsesHandGestureParameter(subs[i].stateMachine)) return true;
            }
            return false;
        }

        static bool TransitionsUseHandGestureParameter(AnimatorTransitionBase[] transitions)
        {
            if (transitions == null) return false;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] == null) continue;
                AnimatorCondition[] conditions = transitions[i].conditions;
                if (conditions == null) continue;
                for (int c = 0; c < conditions.Length; c++)
                {
                    if (IsHandGestureParameter(conditions[c].parameter)) return true;
                }
            }
            return false;
        }

        static bool IsHandGestureParameter(string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return false;
            return parameter.Equals("GestureLeft", StringComparison.OrdinalIgnoreCase)
                || parameter.Equals("GestureRight", StringComparison.OrdinalIgnoreCase)
                || parameter.Equals("GestureLeftWeight", StringComparison.OrdinalIgnoreCase)
                || parameter.Equals("GestureRightWeight", StringComparison.OrdinalIgnoreCase);
        }

        static int CountStandardGestureStates(AnimatorStateMachine machine)
        {
            if (machine == null) return 0;
            HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
            CollectStandardGestureStates(machine, found);
            return found.Count;
        }

        static void CollectStandardGestureStates(AnimatorStateMachine machine, HashSet<string> found)
        {
            if (machine == null) return;
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null || string.IsNullOrEmpty(state.name)) continue;
                string n = NormalizeGestureStateName(state.name);
                for (int g = 0; g < HandGestureStateNames.Length; g++)
                {
                    if (n != HandGestureStateNames[g]) continue;
                    found.Add(HandGestureStateNames[g]);
                    break;
                }
            }
            ChildAnimatorStateMachine[] subs = machine.stateMachines;
            for (int i = 0; i < subs.Length; i++)
                CollectStandardGestureStates(subs[i].stateMachine, found);
        }

        static string NormalizeGestureStateName(string name)
        {
            string n = name.Trim().ToLowerInvariant().Replace("_", " ").Replace("-", " ");
            while (n.IndexOf("  ", StringComparison.Ordinal) >= 0)
                n = n.Replace("  ", " ");
            return n.Replace(" ", "");
        }

        static IEnumerable<AnimationClip> CollectClips(AnimatorStateMachine machine)
        {
            if (machine == null) yield break;
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                Motion motion = states[i].state != null ? states[i].state.motion : null;
                foreach (AnimationClip clip in ClipsFromMotion(motion)) yield return clip;
            }
            ChildAnimatorStateMachine[] subs = machine.stateMachines;
            for (int i = 0; i < subs.Length; i++)
            {
                foreach (AnimationClip clip in CollectClips(subs[i].stateMachine)) yield return clip;
            }
        }

        static IEnumerable<AnimationClip> ClipsFromMotion(Motion motion)
        {
            AnimationClip clip = motion as AnimationClip;
            if (clip != null) { yield return clip; yield break; }
            BlendTree tree = motion as BlendTree;
            if (tree == null) yield break;
            ChildMotion[] children = tree.children;
            for (int i = 0; i < children.Length; i++)
            {
                foreach (AnimationClip c in ClipsFromMotion(children[i].motion)) yield return c;
            }
        }

        sealed class ComponentRefInfo
        {
            public readonly HashSet<Transform> Transforms = new HashSet<Transform>();
            public readonly HashSet<Component> Components = new HashSet<Component>();
            Transform avatarRoot;
            Dictionary<string, Transform> byPath;
            Dictionary<string, List<Transform>> byName;

            public static ComponentRefInfo Build(GameObject avatar)
            {
                ComponentRefInfo info = new ComponentRefInfo();
                if (avatar == null) return info;
                info.avatarRoot = avatar.transform;
                info.IndexHierarchy(avatar.transform);
                HashSet<int> visited = new HashSet<int>();
                Component[] parts = avatar.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < parts.Length; i++)
                {
                    Component c = parts[i];
                    if (!ShouldScanForBlendShapeNames(c)) continue;
                    try
                    {
                        info.Harvest(c, visited);
                    }
                    catch (Exception)
                    {
                    }
                }
                return info;
            }

            public bool Keeps(Transform t)
            {
                return t != null && Transforms.Contains(t);
            }

            public bool Keeps(Component c)
            {
                if (c == null) return false;
                if (Components.Contains(c)) return true;
                return Keeps(c.transform);
            }

            void IndexHierarchy(Transform root)
            {
                byPath = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
                byName = new Dictionary<string, List<Transform>>(StringComparer.OrdinalIgnoreCase);
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t == null) continue;
                    string path = AnimationUtility.CalculateTransformPath(t, root);
                    if (!string.IsNullOrEmpty(path)) byPath[path] = t;
                    List<Transform> named;
                    if (!byName.TryGetValue(t.name, out named))
                    {
                        named = new List<Transform>();
                        byName[t.name] = named;
                    }
                    named.Add(t);
                }
            }

            void Harvest(UnityEngine.Object obj, HashSet<int> visited)
            {
                if (obj == null || visited == null) return;
                if (!visited.Add(obj.GetInstanceID())) return;

                List<ScriptableObject> nested = null;
                SerializedObject so = new SerializedObject(obj);
                SerializedProperty p = so.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType == SerializedPropertyType.String)
                    {
                        KeepPath(p.stringValue);
                        continue;
                    }
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    UnityEngine.Object value = p.objectReferenceValue;
                    if (value == null) continue;
                    KeepObject(value);
                    ScriptableObject asset = value as ScriptableObject;
                    if (asset == null || asset is MonoScript) continue;
                    if (nested == null) nested = new List<ScriptableObject>();
                    nested.Add(asset);
                }
                if (nested == null) return;
                for (int i = 0; i < nested.Count; i++)
                    Harvest(nested[i], visited);
            }

            void KeepObject(UnityEngine.Object value)
            {
                Transform t = value as Transform;
                if (t != null)
                {
                    KeepTransform(t);
                    return;
                }
                GameObject go = value as GameObject;
                if (go != null)
                {
                    KeepTransform(go.transform);
                    return;
                }
                Component c = value as Component;
                if (c == null) return;
                Components.Add(c);
                KeepTransform(c.transform);
            }

            void KeepTransform(Transform t)
            {
                if (!UnderAvatar(t)) return;
                while (t != null && t != avatarRoot)
                {
                    Transforms.Add(t);
                    t = t.parent;
                }
            }

            bool UnderAvatar(Transform t)
            {
                while (t != null)
                {
                    if (t == avatarRoot) return true;
                    t = t.parent;
                }
                return false;
            }

            void KeepPath(string raw)
            {
                if (string.IsNullOrEmpty(raw) || avatarRoot == null) return;
                if (raw.IndexOf('\n') >= 0 || raw.Length > 260) return;
                string path = raw.Trim();
                if (path.Length == 0) return;
                if (path.StartsWith("blendShape.", StringComparison.OrdinalIgnoreCase)) return;

                Transform found;
                if (byPath != null && byPath.TryGetValue(path, out found))
                {
                    KeepTransform(found);
                    return;
                }
                if (path.IndexOf('/') >= 0)
                {
                    found = avatarRoot.Find(path);
                    if (found != null) KeepTransform(found);
                    return;
                }
                List<Transform> named;
                if (byName != null && byName.TryGetValue(path, out named) && named != null)
                {
                    for (int i = 0; i < named.Count; i++)
                        KeepTransform(named[i]);
                }
            }
        }

        sealed class DescriptorShapeIndexMap
        {
            public Component descriptor;
            public FieldInfo settingsField;
            public object settings;
            public FieldInfo indicesField;
            public SkinnedMeshRenderer smr;
            public int[] indices;
            public string[] names;
        }

        sealed class AvatarAnimInfo
        {
            public readonly HashSet<string> UsedBlendShapes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            readonly HashSet<string> enabled = new HashSet<string>(StringComparer.Ordinal);
            readonly HashSet<string> actives = new HashSet<string>(StringComparer.Ordinal);
            readonly HashSet<string> moved = new HashSet<string>(StringComparer.Ordinal);
            readonly HashSet<string> swapped = new HashSet<string>(StringComparer.Ordinal);
            readonly HashSet<string> matAnimated = new HashSet<string>(StringComparer.Ordinal);
            readonly Dictionary<string, List<Keyframe[]>> shapeCurves = new Dictionary<string, List<Keyframe[]>>();

            public static AvatarAnimInfo Build(GameObject root)
            {
                AvatarAnimInfo info = new AvatarAnimInfo();
                HashSet<RuntimeAnimatorController> seen = new HashSet<RuntimeAnimatorController>();
                Animator[] animators = root.GetComponentsInChildren<Animator>(true);
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null || animators[i].runtimeAnimatorController == null) continue;
                    if (!seen.Add(animators[i].runtimeAnimatorController)) continue;
                    info.IngestController(animators[i].runtimeAnimatorController);
                }
                info.IngestDescriptorLayers(root);
                return info;
            }

            void IngestDescriptorLayers(GameObject root)
            {
                Component desc = root.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
                if (desc == null) return;
                FieldInfo layers = desc.GetType().GetField("baseAnimationLayers") ?? desc.GetType().GetField("specialAnimationLayers");
                if (layers == null) return;
                Array arr = layers.GetValue(desc) as Array;
                if (arr == null) return;
                for (int i = 0; i < arr.Length; i++)
                {
                    object layer = arr.GetValue(i);
                    if (layer == null) continue;
                    FieldInfo anim = layer.GetType().GetField("animatorController");
                    if (anim == null) continue;
                    RuntimeAnimatorController c = anim.GetValue(layer) as RuntimeAnimatorController;
                    if (c != null) IngestController(c);
                }
                FieldInfo special = desc.GetType().GetField("specialAnimationLayers");
                if (special == null || special == layers) return;
                arr = special.GetValue(desc) as Array;
                if (arr == null) return;
                for (int i = 0; i < arr.Length; i++)
                {
                    object layer = arr.GetValue(i);
                    if (layer == null) continue;
                    FieldInfo anim = layer.GetType().GetField("animatorController");
                    if (anim == null) continue;
                    RuntimeAnimatorController c = anim.GetValue(layer) as RuntimeAnimatorController;
                    if (c != null) IngestController(c);
                }
            }

            void IngestController(RuntimeAnimatorController controller)
            {
                AnimatorController ac = controller as AnimatorController;
                AnimatorOverrideController ovr = controller as AnimatorOverrideController;
                if (ovr != null) ac = ovr.runtimeAnimatorController as AnimatorController;
                if (ac == null) return;
                AnimatorControllerLayer[] layers = ac.layers;
                for (int i = 0; i < layers.Length; i++)
                {
                    foreach (AnimationClip clip in CollectClips(layers[i].stateMachine))
                        IngestClip(clip);
                }
            }

            void IngestClip(AnimationClip clip)
            {
                if (clip == null) return;
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                for (int i = 0; i < bindings.Length; i++)
                {
                    EditorCurveBinding b = bindings[i];
                    if (b.propertyName == "m_Enabled") enabled.Add(b.path + "|" + b.type.FullName);
                    if (b.propertyName == "m_IsActive") actives.Add(b.path);
                    if (b.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal)
                        || b.propertyName.StartsWith("m_LocalRotation", StringComparison.Ordinal)
                        || b.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal)
                        || b.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal))
                        moved.Add(b.path);
                    if (b.propertyName.StartsWith("m_Materials.Array.data", StringComparison.Ordinal))
                        swapped.Add(b.path + "|" + b.propertyName);
                    if (b.propertyName.StartsWith("material.", StringComparison.Ordinal))
                        matAnimated.Add(b.path);
                    if (b.propertyName.StartsWith("blendShape.", StringComparison.Ordinal))
                    {
                        string shape = b.propertyName.Substring("blendShape.".Length);
                        UsedBlendShapes.Add(shape);
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);
                        if (curve == null) continue;
                        string sk = b.path + "|" + shape;
                        List<Keyframe[]> list;
                        if (!shapeCurves.TryGetValue(sk, out list))
                        {
                            list = new List<Keyframe[]>();
                            shapeCurves[sk] = list;
                        }
                        list.Add(curve.keys);
                    }
                }
                EditorCurveBinding[] objects = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].propertyName.StartsWith("m_Materials.Array.data", StringComparison.Ordinal))
                        swapped.Add(objects[i].path + "|" + objects[i].propertyName);
                }
            }

            public bool IsEnabledAnimated(string path, Type type)
            {
                return enabled.Contains(path + "|" + (type != null ? type.FullName : ""));
            }

            public bool IsActiveAnimated(string path)
            {
                return actives.Contains(path);
            }

            public bool IsTransformMoved(Transform bone, Transform root)
            {
                if (bone == null) return false;
                string path = AnimationUtility.CalculateTransformPath(bone, root);
                return moved.Contains(path);
            }

            public bool IsBlendShapeAnimated(SkinnedMeshRenderer smr, GameObject root, string name)
            {
                if (UsedBlendShapes.Contains(name)) return true;
                string path = AnimationUtility.CalculateTransformPath(smr.transform, root.transform);
                return shapeCurves.ContainsKey(path + "|" + name);
            }

            public bool IsMaterialSlotSwapped(SkinnedMeshRenderer smr, GameObject root, int slot)
            {
                string path = AnimationUtility.CalculateTransformPath(smr.transform, root.transform);
                return swapped.Contains(path + "|m_Materials.Array.data[" + slot + "]");
            }

            public string TogetherKey(SkinnedMeshRenderer smr, GameObject root)
            {
                string path = AnimationUtility.CalculateTransformPath(smr.transform, root.transform);
                bool tog = enabled.Contains(path + "|" + typeof(SkinnedMeshRenderer).FullName) || actives.Contains(path);
                if (tog || matAnimated.Contains(path) || HasMaterialSwap(path)) return null;
                Transform t = smr.transform.parent;
                while (t != null && t != root.transform)
                {
                    string p = AnimationUtility.CalculateTransformPath(t, root.transform);
                    if (actives.Contains(p)) return null;
                    t = t.parent;
                }
                int layer = smr.gameObject.layer;
                return "always|" + layer + "|" + (smr.updateWhenOffscreen ? "1" : "0");
            }

            public bool HasMaterialSwap(SkinnedMeshRenderer smr, GameObject root)
            {
                if (smr == null || root == null) return false;
                return HasMaterialSwap(AnimationUtility.CalculateTransformPath(smr.transform, root.transform));
            }

            bool HasMaterialSwap(string path)
            {
                string prefix = path + "|m_Materials.Array.data";
                foreach (string s in swapped)
                {
                    if (s.StartsWith(prefix, StringComparison.Ordinal)) return true;
                }
                return false;
            }

            public Dictionary<int, int> SameRatioPairs(SkinnedMeshRenderer smr, GameObject root, Mesh mesh)
            {
                Dictionary<int, int> map = new Dictionary<int, int>();
                string path = AnimationUtility.CalculateTransformPath(smr.transform, root.transform);
                for (int a = 0; a < mesh.blendShapeCount; a++)
                {
                    string na = mesh.GetBlendShapeName(a);
                    List<Keyframe[]> ca;
                    if (!shapeCurves.TryGetValue(path + "|" + na, out ca) || ca.Count == 0) continue;
                    for (int b = a + 1; b < mesh.blendShapeCount; b++)
                    {
                        if (map.ContainsKey(b)) continue;
                        string nb = mesh.GetBlendShapeName(b);
                        List<Keyframe[]> cb;
                        if (!shapeCurves.TryGetValue(path + "|" + nb, out cb) || cb.Count != ca.Count) continue;
                        if (!SameRatio(ca, cb)) continue;
                        map[b] = a;
                    }
                }
                return map;
            }

            static bool SameRatio(List<Keyframe[]> a, List<Keyframe[]> b)
            {
                if (a.Count != b.Count) return false;
                for (int i = 0; i < a.Count; i++)
                {
                    Keyframe[] ka = a[i];
                    Keyframe[] kb = b[i];
                    if (ka.Length != kb.Length || ka.Length == 0) return false;
                    float ratio = 0f;
                    bool have = false;
                    for (int k = 0; k < ka.Length; k++)
                    {
                        if (Mathf.Abs(ka[k].time - kb[k].time) > 0.0001f) return false;
                        if (Mathf.Abs(ka[k].value) < 0.0001f)
                        {
                            if (Mathf.Abs(kb[k].value) > 0.0001f) return false;
                            continue;
                        }
                        float r = kb[k].value / ka[k].value;
                        if (!have) { ratio = r; have = true; }
                        else if (Mathf.Abs(r - ratio) > 0.02f) return false;
                    }
                }
                return true;
            }
        }
    }
}
