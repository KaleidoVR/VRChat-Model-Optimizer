// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Avatar structure pass: unused cleanup, blend shapes, mesh / slot merge, PhysBones, FX.
// Runs on a clone at upload. Source assets and the scene stay as they are.

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace KaleidoVR.EditorTools
{
    public sealed class KaleidoAvatarPassSettings
    {
        public bool applyOnUpload = true;
        public bool mergeSkinnedMeshes = true;
        public bool mergeIdenticalSlots = true;
        public bool shuffleMaterialSlots = true;
        public bool optimizeBlendShapes = true;
        public bool mergeSameRatioShapes = true;
        public bool mmdCompatibility = true;
        public bool removeUnusedComponents = true;
        public bool removeUnusedGameObjects = false;
        public bool stripUnusedBones = true;
        public bool optimizePhysBones = true;
        public bool optimizeFxLayer = true;
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

        public static KaleidoAvatarPassSettings FromPrefs()
        {
            string p = KaleidoVRCOptimizer.PrefsPrefix;
            KaleidoAvatarPassSettings settings = new KaleidoAvatarPassSettings
            {
                applyOnUpload = EditorPrefs.GetBool(p + "AvUp", true),
                mergeSkinnedMeshes = EditorPrefs.GetBool(p + "AvMerge", true),
                mergeIdenticalSlots = EditorPrefs.GetBool(p + "AvSlots", true),
                shuffleMaterialSlots = EditorPrefs.GetBool(p + "AvShuffle", true),
                optimizeBlendShapes = EditorPrefs.GetBool(p + "AvShape", true),
                mergeSameRatioShapes = EditorPrefs.GetBool(p + "AvRatio", true),
                mmdCompatibility = EditorPrefs.GetBool(p + "AvMmd", true),
                removeUnusedComponents = EditorPrefs.GetBool(p + "AvComp", true),
                removeUnusedGameObjects = EditorPrefs.GetBool(p + "AvGo", false),
                stripUnusedBones = EditorPrefs.GetBool(p + "AvBone", true),
                optimizePhysBones = EditorPrefs.GetBool(p + "AvPb", true),
                optimizeFxLayer = EditorPrefs.GetBool(p + "AvFx", true)
            };
            if (EditorPrefs.GetBool(p + "MeshBSOff", false))
            {
                settings.optimizeBlendShapes = false;
                settings.mergeSameRatioShapes = false;
            }
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
            if (window.meshStripBlendShapes)
            {
                settings.optimizeBlendShapes = false;
                settings.mergeSameRatioShapes = false;
            }
            return settings;
        }

        public static int UploadCallbackOrder()
        {
            return -1;
        }

        public static void Preview(GameObject root, KaleidoAvatarPassSettings settings, List<string> lines, HashSet<Transform> extraExclusions)
        {
            if (root == null || settings == null) return;
            KaleidoAvatarPassResult result = Run(root, settings, true, extraExclusions);
            if (result == null) return;
            for (int i = 0; i < result.lines.Count; i++) lines.Add(result.lines[i]);
        }

        public static KaleidoAvatarPassResult Run(GameObject root, KaleidoAvatarPassSettings settings, bool dryRun, HashSet<Transform> extraExclusions)
        {
            KaleidoAvatarPassResult result = new KaleidoAvatarPassResult();
            if (root == null || settings == null) return result;

            HashSet<Transform> excluded = CollectExclusions(root, extraExclusions);
            AvatarAnimInfo anim = AvatarAnimInfo.Build(root);

            if (settings.removeUnusedComponents || settings.removeUnusedGameObjects)
                SweepUnused(root, settings, anim, excluded, dryRun, result);

            if (settings.optimizeBlendShapes || settings.mergeSameRatioShapes)
                ProcessBlendShapes(root, settings, anim, excluded, dryRun, result);

            if (settings.stripUnusedBones)
                StripUnusedBones(root, anim, excluded, dryRun, result);

            if (settings.mergeIdenticalSlots || settings.shuffleMaterialSlots)
                MergeSlotsOnRenderers(root, settings, anim, excluded, dryRun, result);

            if (settings.mergeSkinnedMeshes)
                MergeTogetherMeshes(root, settings, anim, excluded, dryRun, result);

            if (settings.optimizePhysBones)
                SweepPhysBones(root, anim, excluded, dryRun, result);

            if (settings.optimizeFxLayer)
                OptimizeFx(root, settings, anim, excluded, dryRun, result);

            if (result.lines.Count == 0)
                result.lines.Add("Avatar pass: nothing to change with the current toggles.");
            return result;
        }

        public static GameObject CreatePreviewCopy(GameObject source, KaleidoAvatarPassSettings settings, HashSet<Transform> extraExclusions)
        {
            if (source == null) return null;
            GameObject copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name + " (Optimized Copy)";
            copy.transform.SetParent(source.transform.parent, false);
            copy.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            source.SetActive(false);
            Run(copy, settings, false, RemapExclusions(source, copy, extraExclusions));
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
            if (renderer == null || IsExcluded(renderer, excluded) || IsSensitiveMesh(renderer)) return true;
            SkinnedMeshRenderer smr = renderer as SkinnedMeshRenderer;
            Mesh mesh = smr != null ? smr.sharedMesh : null;
            return IsExternalRuntimeAsset(mesh);
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

        static void SweepUnused(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
            List<Behaviour> remove = new List<Behaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour b = behaviours[i];
                if (b == null || b is Animator || IsExcluded(b, excluded)) continue;
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
                    if (HasRequiredRef(t, root)) continue;
                    result.objectsRemoved++;
                    result.lines.Add("Remove unused object: " + t.name);
                    if (!dryRun) UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }
        }

        static bool HasRequiredRef(Transform t, GameObject root)
        {
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

        static void ProcessBlendShapes(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            HashSet<string> keep = anim.UsedBlendShapes;
            if (settings.mmdCompatibility)
            {
                for (int i = 0; i < MmdShapeNames.Length; i++) keep.Add(MmdShapeNames[i]);
            }
            AddDescriptorShapes(root, keep);

            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer smr = skins[s];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded)) continue;
                Mesh mesh = smr.sharedMesh;
                if (mesh.blendShapeCount == 0) continue;

                List<int> bake = new List<int>();
                List<int> drop = new List<int>();
                if (settings.optimizeBlendShapes)
                {
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        string name = mesh.GetBlendShapeName(i);
                        if (keep.Contains(name) || anim.IsBlendShapeAnimated(smr, root, name)) continue;
                        float w = smr.GetBlendShapeWeight(i);
                        if (Mathf.Abs(w) > 0.01f) bake.Add(i);
                        else drop.Add(i);
                    }
                }

                Dictionary<int, int> ratioInto = settings.mergeSameRatioShapes
                    ? anim.SameRatioPairs(smr, root, mesh)
                    : null;

                if (bake.Count == 0 && drop.Count == 0 && (ratioInto == null || ratioInto.Count == 0)) continue;

                result.shapesBaked += bake.Count;
                result.shapesRemoved += drop.Count;
                if (ratioInto != null) result.shapesMerged += ratioInto.Count;
                result.lines.Add(smr.name + ": bake " + bake.Count + ", drop " + drop.Count + ", merge " + (ratioInto != null ? ratioInto.Count : 0) + " blend shapes");
                if (dryRun) continue;

                Mesh copy = UnityEngine.Object.Instantiate(mesh);
                copy.name = mesh.name + "_Kaleido";
                copy.hideFlags = HideFlags.HideAndDontSave;
                BakeAndStripShapes(copy, smr, bake, drop, ratioInto);
                smr.sharedMesh = copy;
            }
        }

        static void BakeAndStripShapes(Mesh mesh, SkinnedMeshRenderer smr, List<int> bake, List<int> drop, Dictionary<int, int> ratioInto)
        {
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            Vector3[] dV = new Vector3[verts.Length];
            Vector3[] dN = new Vector3[verts.Length];
            Vector3[] dT = new Vector3[verts.Length];

            HashSet<int> remove = new HashSet<int>(drop);
            for (int b = 0; b < bake.Count; b++)
            {
                int idx = bake[b];
                float w = smr.GetBlendShapeWeight(idx) / 100f;
                if (mesh.GetBlendShapeFrameCount(idx) < 1) continue;
                mesh.GetBlendShapeFrameVertices(idx, 0, dV, dN, dT);
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] += dV[i] * w;
                    if (normals != null && normals.Length == verts.Length) normals[i] = (normals[i] + dN[i] * w).normalized;
                    if (tangents != null && tangents.Length == verts.Length)
                    {
                        Vector3 t = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z) + dT[i] * w;
                        tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
                    }
                }
                remove.Add(idx);
                smr.SetBlendShapeWeight(idx, 0f);
            }

            if (ratioInto != null)
            {
                foreach (KeyValuePair<int, int> pair in ratioInto)
                    remove.Add(pair.Key);
            }

            mesh.vertices = verts;
            if (normals != null && normals.Length == verts.Length) mesh.normals = normals;
            if (tangents != null && tangents.Length == verts.Length) mesh.tangents = tangents;

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
            mesh.RecalculateBounds();
        }

        static void AddDescriptorShapes(GameObject root, HashSet<string> keep)
        {
            Component desc = root.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (desc == null) return;
            Type t = desc.GetType();
            FieldInfo visemes = t.GetField("VisemeBlendShapes");
            if (visemes != null)
            {
                string[] names = visemes.GetValue(desc) as string[];
                if (names != null)
                {
                    for (int i = 0; i < names.Length; i++)
                        if (!string.IsNullOrEmpty(names[i])) keep.Add(names[i]);
                }
            }
            foreach (string field in new[] { "customEyeLookSettings", "lipSync" })
            {
                FieldInfo f = t.GetField(field);
                if (f == null) continue;
                object val = f.GetValue(desc);
                if (val == null) continue;
                CollectStringFields(val, keep);
            }
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

        static void StripUnusedBones(GameObject root, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer smr = skins[s];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded)) continue;
                Transform[] bones = smr.bones;
                if (bones == null || bones.Length == 0) continue;
                Mesh mesh = smr.sharedMesh;
                BoneWeight[] weights = mesh.boneWeights;
                if (weights == null || weights.Length != mesh.vertexCount) continue;

                bool[] used = new bool[bones.Length];
                for (int i = 0; i < weights.Length; i++)
                {
                    BoneWeight w = weights[i];
                    if (w.weight0 > 0f && w.boneIndex0 >= 0 && w.boneIndex0 < used.Length) used[w.boneIndex0] = true;
                    if (w.weight1 > 0f && w.boneIndex1 >= 0 && w.boneIndex1 < used.Length) used[w.boneIndex1] = true;
                    if (w.weight2 > 0f && w.boneIndex2 >= 0 && w.boneIndex2 < used.Length) used[w.boneIndex2] = true;
                    if (w.weight3 > 0f && w.boneIndex3 >= 0 && w.boneIndex3 < used.Length) used[w.boneIndex3] = true;
                }

                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] == null) continue;
                    if (anim.IsTransformMoved(bones[i], root.transform)) used[i] = true;
                }

                int keepCount = 0;
                for (int i = 0; i < used.Length; i++) if (used[i]) keepCount++;
                if (keepCount == bones.Length) continue;

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
                copy.hideFlags = HideFlags.HideAndDontSave;
                BoneWeight[] nw = new BoneWeight[weights.Length];
                for (int i = 0; i < weights.Length; i++)
                {
                    BoneWeight w = weights[i];
                    nw[i] = RemapWeight(w, used, map);
                }
                copy.boneWeights = nw;
                copy.bindposes = newBind;
                smr.sharedMesh = copy;
                smr.bones = newBones;
            }
        }

        static void MergeSlotsOnRenderers(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer smr = skins[s];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded)) continue;
                Material[] mats = smr.sharedMaterials;
                Mesh mesh = smr.sharedMesh;
                if (mats == null || mesh.subMeshCount <= 1) continue;

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
                copy.hideFlags = HideFlags.HideAndDontSave;
                copy.subMeshCount = newMats.Count;
                for (int i = 0; i < newMats.Count; i++) copy.SetTriangles(tris[i], i);
                smr.sharedMesh = copy;
                smr.sharedMaterials = newMats.ToArray();
            }
        }

        static void MergeTogetherMeshes(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Dictionary<string, List<SkinnedMeshRenderer>> groups = new Dictionary<string, List<SkinnedMeshRenderer>>();
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer smr = skins[i];
                if (smr == null || smr.sharedMesh == null || ShouldLeaveRenderer(smr, excluded)) continue;
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
                CombineSkinned(pair.Value, settings, root);
            }
        }

        static void CombineSkinned(List<SkinnedMeshRenderer> group, KaleidoAvatarPassSettings settings, GameObject root)
        {
            SkinnedMeshRenderer dest = group[0];
            List<Transform> bones = new List<Transform>();
            List<Matrix4x4> binds = new List<Matrix4x4>();
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector4> tans = new List<Vector4>();
            List<Vector2> uv0 = new List<Vector2>();
            List<BoneWeight> weights = new List<BoneWeight>();
            List<Material> mats = new List<Material>();
            List<int[]> subTris = new List<int[]>();

            for (int g = 0; g < group.Count; g++)
            {
                SkinnedMeshRenderer smr = group[g];
                Mesh mesh = smr.sharedMesh;
                Transform[] sb = smr.bones;
                Matrix4x4[] bp = mesh.bindposes;
                int[] boneMap = new int[sb != null ? sb.Length : 0];
                for (int b = 0; b < boneMap.Length; b++)
                {
                    Transform bone = sb[b];
                    int found = bones.IndexOf(bone);
                    if (found < 0)
                    {
                        found = bones.Count;
                        bones.Add(bone);
                        binds.Add(bp != null && b < bp.Length ? bp[b] : Matrix4x4.identity);
                    }
                    boneMap[b] = found;
                }

                int vertBase = verts.Count;
                Vector3[] mv = mesh.vertices;
                Vector3[] mn = mesh.normals;
                Vector4[] mt = mesh.tangents;
                Vector2[] mu = mesh.uv;
                BoneWeight[] mw = mesh.boneWeights;
                Matrix4x4 local = dest.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
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
                    uv0.Add(mu != null && i < mu.Length ? mu[i] : Vector2.zero);
                    BoneWeight w = mw != null && i < mw.Length ? mw[i] : default(BoneWeight);
                    w.boneIndex0 = SafeMap(w.boneIndex0, boneMap);
                    w.boneIndex1 = SafeMap(w.boneIndex1, boneMap);
                    w.boneIndex2 = SafeMap(w.boneIndex2, boneMap);
                    w.boneIndex3 = SafeMap(w.boneIndex3, boneMap);
                    weights.Add(w);
                }

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

                if (g > 0)
                {
                    smr.sharedMesh = null;
                    smr.enabled = false;
                    smr.gameObject.SetActive(false);
                }
            }

            Mesh combined = new Mesh();
            combined.name = dest.name + "_KaleidoMerged";
            combined.hideFlags = HideFlags.HideAndDontSave;
            combined.indexFormat = verts.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            combined.SetVertices(verts);
            combined.SetNormals(norms);
            combined.SetTangents(tans);
            combined.SetUVs(0, uv0);
            combined.boneWeights = weights.ToArray();
            combined.bindposes = binds.ToArray();
            combined.subMeshCount = subTris.Count;
            for (int i = 0; i < subTris.Count; i++) combined.SetTriangles(subTris[i], i);
            combined.RecalculateBounds();
            dest.sharedMesh = combined;
            dest.bones = bones.ToArray();
            dest.sharedMaterials = mats.ToArray();
        }

        static BoneWeight RemapWeight(BoneWeight w, bool[] used, int[] map)
        {
            BoneWeight n = new BoneWeight();
            AssignWeight(ref n.weight0, ref n.boneIndex0, w.weight0, w.boneIndex0, used, map);
            AssignWeight(ref n.weight1, ref n.boneIndex1, w.weight1, w.boneIndex1, used, map);
            AssignWeight(ref n.weight2, ref n.boneIndex2, w.weight2, w.boneIndex2, used, map);
            AssignWeight(ref n.weight3, ref n.boneIndex3, w.weight3, w.boneIndex3, used, map);
            return n;
        }

        static void AssignWeight(ref float weight, ref int index, float srcWeight, int srcIndex, bool[] used, int[] map)
        {
            if (srcIndex < 0 || srcIndex >= used.Length || !used[srcIndex] || srcWeight <= 0f)
            {
                weight = 0f;
                index = 0;
                return;
            }
            weight = srcWeight;
            index = map[srcIndex];
        }

        static int SafeMap(int index, int[] map)
        {
            if (map == null || map.Length == 0) return 0;
            if (index < 0 || index >= map.Length) return 0;
            return map[index];
        }

        static void SweepPhysBones(GameObject root, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
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

        static void OptimizeFx(GameObject root, KaleidoAvatarPassSettings settings, AvatarAnimInfo anim, HashSet<Transform> excluded, bool dryRun, KaleidoAvatarPassResult result)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int a = 0; a < animators.Length; a++)
            {
                Animator animator = animators[a];
                if (animator == null || IsExcluded(animator, excluded)) continue;
                AnimatorController src = animator.runtimeAnimatorController as AnimatorController;
                if (src == null || IsExternalRuntimeAsset(src)) continue;

                int deadCurves = 0;
                int deadLayers = 0;
                AnimatorControllerLayer[] layers = src.layers;
                for (int l = 0; l < layers.Length; l++)
                {
                    if (settings.mmdCompatibility && l < 3) continue;
                    AnimatorControllerLayer layer = layers[l];
                    if (layer.stateMachine == null || (layer.stateMachine.states.Length == 0 && layer.stateMachine.stateMachines.Length == 0))
                    {
                        deadLayers++;
                        continue;
                    }
                    deadCurves += CountMissingCurves(layer, root);
                }

                if (deadLayers == 0 && deadCurves == 0) continue;
                result.fxLayersRemoved += deadLayers;
                result.curvesRemoved += deadCurves;
                result.lines.Add(animator.name + " FX: drop " + deadLayers + " empty layer(s), " + deadCurves + " missing curve(s)");
                if (dryRun) continue;

                AnimatorController copy = UnityEngine.Object.Instantiate(src);
                copy.name = src.name + "_KaleidoFX";
                copy.hideFlags = HideFlags.HideAndDontSave;
                StripFx(copy, root, settings.mmdCompatibility);
                animator.runtimeAnimatorController = copy;
            }
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
                if (keepMmdLayers && i < 3)
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

        sealed class AvatarAnimInfo
        {
            public readonly HashSet<string> UsedBlendShapes = new HashSet<string>();
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
                if (tog || matAnimated.Contains(path)) return null;
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
