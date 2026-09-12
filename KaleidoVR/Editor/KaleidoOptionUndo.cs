// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
// Stores pre-Apply values so a checked option can be undone after it has been written.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace KaleidoVR.EditorTools
{
    public static class KaleidoOptionUndo
    {
        public const string SizeAlbedo = "applyAlbedoSize";
        public const string SizeNormal = "applyNormalSize";
        public const string SizeMask = "applyMaskSize";
        public const string SizeEmission = "applyEmissionSize";
        public const string SizeMatcap = "applyMatcapSize";
        public const string SizeOther = "applyOtherSize";
        public const string AndroidFormat = "applyAndroidTexFormat";
        public const string PcFormat = "applyPcTexFormat";
        public const string HigherQualityNormals = "higherQualityNormalMaps";
        public const string StreamingMipmaps = "textureEnableStreamingMipmaps";
        public const string TextureReadWrite = "textureDisableReadWrite";
        public const string TextureMipmaps = "textureApplyMipmaps";
        public const string TextureCrunchOff = "textureDisableCrunch";
        public const string TextureCrunchOn = "textureEnableCrunch";
        public const string TextureAniso = "textureApplyAniso";
        public const string DetectNormals = "autoDetectNormalMaps";
        public const string LinearMasks = "autoLinearMaskMaps";
        public const string AlphaIsTransparency = "alphaIsTransparencyOnAlbedo";
        public const string MeshReadWrite = "meshEnableReadWrite";
        public const string SkinWeights = "applySkinWeights";
        public const string OptimizePolygons = "meshOptimizePolygons";
        public const string OptimizeVertices = "meshOptimizeVertices";
        public const string WeldVertices = "meshWeldVertices";
        public const string KeepBlendShapes = "meshKeepBlendShapes";
        public const string StripBlendShapes = "meshStripBlendShapes";
        public const string DisableQuads = "meshDisableQuads";
        public const string DisableLightmapUvs = "meshDisableLightmapUVs";
        public const string SkipLightsCameras = "meshDisableImportLightsCameras";
        public const string OptimizeAnimation = "meshOptimizeAnimation";
        public const string MeshCompression = "applyMeshCompression";
        public const string ForceHumanoid = "meshForceHumanoid";
        public const string DisableShadows = "rendererDisableShadows";
        public const string ForceBone4 = "rendererForceBone4";
        public const string DisableUpdateOffscreen = "rendererDisableUpdateWhenOffscreen";
        public const string DisableReceiveShadows = "rendererDisableReceiveShadows";
        public const string DisableProbes = "rendererDisableProbes";
        public const string DisableMotionVectors = "rendererDisableMotionVectors";
        public const string RecalculateBounds = "rendererRecalculateBounds";
        public const string AnimatorCull = "animatorCullWhenOffscreen";
        public const string AudioLoadBackground = "audioLoadInBackground";
        public const string AudioVorbis = "audioApplyVorbis";
        public const string AudioMono = "audioForceToMono";
        public const string Particles = "optimizeParticles";
        public const string DisableLights = "disableLightsOnAvatar";
        public const string DisableCameras = "disableCamerasOnAvatar";
        public const string GpuInstancing = "optimizeMaterials";

        public static string SizeOptionId(KaleidoTextureKind kind)
        {
            switch (kind)
            {
                case KaleidoTextureKind.Albedo: return SizeAlbedo;
                case KaleidoTextureKind.Normal: return SizeNormal;
                case KaleidoTextureKind.Mask: return SizeMask;
                case KaleidoTextureKind.Emission: return SizeEmission;
                case KaleidoTextureKind.Matcap: return SizeMatcap;
                default: return SizeOther;
            }
        }

        public static bool Has(string optionId)
        {
            if (string.IsNullOrEmpty(optionId)) return false;
            EnsureLoaded();
            if (HasStored(optionId)) return true;
            return IsSpecial(optionId) && LogHasApplied(optionId);
        }

        public static void BeginApplyLog(List<string> entries, bool write)
        {
            applyLog = entries;
            applyWrite = write;
            specialWrites = 0;
            WroteSpecial = false;
        }

        public static void EndApplyLog()
        {
            if (applyWrite && applyLog != null)
            {
                string ran = specialWrites == 0
                    ? "Special ran: none"
                    : "Special ran: " + specialWrites + " change(s).";
                applyLog.Add(ran);
                NoteHeader(ran);
            }
            applyLog = null;
            applyWrite = false;
            logStamp = DateTime.MinValue;
        }

        public static bool WroteSpecial { get; private set; }

        public static void Capture(string optionId, string kind, string assetPath, string hierarchy, string scenePath, string slot, string data)
        {
            if (string.IsNullOrEmpty(optionId) || string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(data)) return;
            EnsureLoaded();
            string asset = assetPath ?? "";
            string path = hierarchy ?? "";
            string scene = scenePath ?? "";
            string keySlot = slot ?? "";
            for (int i = 0; i < records.Count; i++)
            {
                UndoRecord existing = records[i];
                if (!string.Equals(existing.id, optionId, StringComparison.Ordinal)) continue;
                if (!string.Equals(existing.kind, kind, StringComparison.Ordinal)) continue;
                if (!string.Equals(existing.asset, asset, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(existing.hierarchy, path, StringComparison.Ordinal)) continue;
                if (!string.Equals(existing.scene, scene, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(existing.slot, keySlot, StringComparison.Ordinal)) continue;
                return;
            }

            records.Add(new UndoRecord
            {
                id = optionId,
                kind = kind,
                asset = asset,
                hierarchy = path,
                scene = scene,
                slot = keySlot,
                data = data
            });
            dirty = true;
            if (IsSpecial(optionId)) NoteApplied(optionId, kind, asset, path, scene, keySlot, data);
        }

        public static void Save()
        {
            if (!loaded || !dirty) return;
            try
            {
                string path = StorePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                UndoFile file = new UndoFile { records = records.ToArray() };
                File.WriteAllText(path, JsonUtility.ToJson(file));
                dirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KaleidoVR] Could not save option undo: " + ex.Message);
            }
        }

        public static int Restore(string optionId, out int missed)
        {
            missed = 0;
            if (string.IsNullOrEmpty(optionId)) return 0;
            EnsureLoaded();
            ImportLogRecords(optionId);

            List<UndoRecord> mine = new List<UndoRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                if (string.Equals(records[i].id, optionId, StringComparison.Ordinal)) mine.Add(records[i]);
            }
            if (mine.Count == 0) return 0;

            int restored = 0;
            List<string> reimport = new List<string>();
            HashSet<string> restoredKeys = new HashSet<string>(StringComparer.Ordinal);

            EditorUtility.DisplayProgressBar("Undo", "Restoring saved values...", 0.2f);
            try
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < mine.Count; i++)
                    {
                        UndoRecord record = mine[i];
                        if (record.kind == "texture" || record.kind == "model" || record.kind == "audio" || record.kind == "material")
                        {
                            if (RestoreAsset(record))
                            {
                                restored++;
                                restoredKeys.Add(Key(record));
                                if (record.kind != "material" && !string.IsNullOrEmpty(record.asset)) reimport.Add(record.asset);
                            }
                            else missed++;
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                Dictionary<string, List<UndoRecord>> prefabs = new Dictionary<string, List<UndoRecord>>(StringComparer.OrdinalIgnoreCase);
                List<UndoRecord> sceneRecords = new List<UndoRecord>();
                for (int i = 0; i < mine.Count; i++)
                {
                    UndoRecord record = mine[i];
                    if (record.kind == "texture" || record.kind == "model" || record.kind == "audio" || record.kind == "material") continue;
                    if (!string.IsNullOrEmpty(record.asset) && record.asset.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        List<UndoRecord> list;
                        if (!prefabs.TryGetValue(record.asset, out list))
                        {
                            list = new List<UndoRecord>();
                            prefabs[record.asset] = list;
                        }
                        list.Add(record);
                    }
                    else sceneRecords.Add(record);
                }

                foreach (KeyValuePair<string, List<UndoRecord>> pair in prefabs)
                {
                    GameObject contents = null;
                    try
                    {
                        contents = PrefabUtility.LoadPrefabContents(pair.Key);
                        if (contents == null)
                        {
                            missed += pair.Value.Count;
                            continue;
                        }
                        bool changed = false;
                        for (int i = 0; i < pair.Value.Count; i++)
                        {
                            UndoRecord record = pair.Value[i];
                            Transform target = FindRelative(contents.transform, record.hierarchy);
                            if (target == null || !ApplyComponent(target, record))
                            {
                                missed++;
                                continue;
                            }
                            restored++;
                            restoredKeys.Add(Key(record));
                            changed = true;
                        }
                        if (changed) PrefabUtility.SaveAsPrefabAsset(contents, pair.Key);
                    }
                    catch (Exception)
                    {
                        missed += pair.Value.Count;
                    }
                    finally
                    {
                        if (contents != null) PrefabUtility.UnloadPrefabContents(contents);
                    }
                }

                for (int i = 0; i < sceneRecords.Count; i++)
                {
                    UndoRecord record = sceneRecords[i];
                    Transform target = FindSceneTransform(record.scene, record.hierarchy);
                    if (target == null || !ApplyComponent(target, record))
                    {
                        missed++;
                        continue;
                    }
                    restored++;
                    restoredKeys.Add(Key(record));
                    if (target.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
                }
                if (sceneRecords.Count > 0) SceneView.RepaintAll();

                for (int i = 0; i < reimport.Count; i++)
                {
                    AssetDatabase.ImportAsset(reimport[i], ImportAssetOptions.ForceUpdate);
                }
                if (reimport.Count > 0 || prefabs.Count > 0) AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (restoredKeys.Count > 0)
            {
                records.RemoveAll(delegate (UndoRecord record) { return restoredKeys.Contains(Key(record)); });
                dirty = true;
                Save();
                if (IsSpecial(optionId)) NoteUndone(optionId);
            }
            return restored;
        }

        public static string Int(string key, int value)
        {
            return key + "=" + value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Float(string key, float value)
        {
            return key + "=" + value.ToString("R", CultureInfo.InvariantCulture);
        }

        public static string Join(params string[] pairs)
        {
            return string.Join(";", pairs);
        }

        public static string FullPath(Transform transform)
        {
            if (transform == null) return "";
            string path = transform.name;
            Transform current = transform.parent;
            int guard = 0;
            while (current != null && guard++ < 64)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        public static string RelativePath(Transform root, Transform target)
        {
            if (target == null) return "";
            if (root == null || target == root) return "";
            string path = target.name;
            Transform current = target.parent;
            int guard = 0;
            while (current != null && current != root && guard++ < 64)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        static bool RestoreAsset(UndoRecord record)
        {
            if (string.IsNullOrEmpty(record.asset)) return false;
            if (record.kind == "material")
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(record.asset);
                if (material == null) return false;
                Dictionary<string, string> map = Parse(record.data);
                int instancing;
                if (TryInt(map, "instancing", out instancing)) material.enableInstancing = instancing != 0;
                EditorUtility.SetDirty(material);
                return true;
            }

            AssetImporter importer = AssetImporter.GetAtPath(record.asset);
            if (importer == null) return false;
            Dictionary<string, string> values = Parse(record.data);
            if (record.kind == "texture")
            {
                TextureImporter texture = importer as TextureImporter;
                if (texture == null) return false;
                ApplyTexture(texture, record, values);
            }
            else if (record.kind == "model")
            {
                ModelImporter model = importer as ModelImporter;
                if (model == null) return false;
                ApplyModel(model, values);
            }
            else if (record.kind == "audio")
            {
                AudioImporter audio = importer as AudioImporter;
                if (audio == null) return false;
                ApplyAudio(audio, values);
            }
            else return false;

            EditorUtility.SetDirty(importer);
            return true;
        }

        static void ApplyTexture(TextureImporter importer, UndoRecord record, Dictionary<string, string> map)
        {
            int value;
            if (TryInt(map, "readable", out value)) importer.isReadable = value != 0;
            if (TryInt(map, "mipmaps", out value)) importer.mipmapEnabled = value != 0;
            if (TryInt(map, "streaming", out value)) importer.streamingMipmaps = value != 0;
            if (TryInt(map, "aniso", out value)) importer.anisoLevel = value;
            if (TryInt(map, "crunch", out value)) importer.crunchedCompression = value != 0;
            if (TryInt(map, "compression", out value)) importer.textureCompression = (TextureImporterCompression)value;
            if (TryInt(map, "srgb", out value)) importer.sRGBTexture = value != 0;
            if (TryInt(map, "alpha", out value)) importer.alphaIsTransparency = value != 0;
            if (TryInt(map, "texType", out value)) importer.textureType = (TextureImporterType)value;
            if (TryInt(map, "max", out value)) importer.maxTextureSize = value;

            bool platform = TryInt(map, "platMax", out value) || map.ContainsKey("platFmt") || map.ContainsKey("platComp") || map.ContainsKey("platCrunch") || map.ContainsKey("platOver");
            if (!platform) return;

            string platformName = record.slot == "android" ? "Android" : "Standalone";
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
            settings.name = platformName;
            if (TryInt(map, "platMax", out value)) settings.maxTextureSize = value;
            if (TryInt(map, "platFmt", out value)) settings.format = (TextureImporterFormat)value;
            if (TryInt(map, "platComp", out value)) settings.textureCompression = (TextureImporterCompression)value;
            if (TryInt(map, "platCrunch", out value)) settings.crunchedCompression = value != 0;

            int wasOverridden;
            bool hasOverridden = TryInt(map, "platOver", out wasOverridden);
            if (hasOverridden && wasOverridden == 0 && !OtherPlatformRecord(record))
                settings.overridden = false;
            else if (map.ContainsKey("platMax") || map.ContainsKey("platFmt") || map.ContainsKey("platComp") || map.ContainsKey("platCrunch"))
                settings.overridden = true;

            importer.SetPlatformTextureSettings(settings);
        }

        static bool OtherPlatformRecord(UndoRecord record)
        {
            for (int i = 0; i < records.Count; i++)
            {
                UndoRecord other = records[i];
                if (string.Equals(other.id, record.id, StringComparison.Ordinal)) continue;
                if (!string.Equals(other.kind, "texture", StringComparison.Ordinal)) continue;
                if (!string.Equals(other.asset, record.asset, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(other.slot, record.slot, StringComparison.Ordinal)) continue;
                Dictionary<string, string> map = Parse(other.data);
                if (map.ContainsKey("platOver") || map.ContainsKey("platMax") || map.ContainsKey("platFmt") || map.ContainsKey("platCrunch"))
                    return true;
            }
            return false;
        }

        static void ApplyModel(ModelImporter importer, Dictionary<string, string> map)
        {
            int value;
            if (TryInt(map, "readable", out value)) importer.isReadable = value != 0;
            if (TryInt(map, "optPoly", out value)) importer.optimizeMeshPolygons = value != 0;
            if (TryInt(map, "optVert", out value)) importer.optimizeMeshVertices = value != 0;
            if (TryInt(map, "weld", out value)) importer.weldVertices = value != 0;
            if (TryInt(map, "blend", out value)) importer.importBlendShapes = value != 0;
            if (TryInt(map, "quads", out value)) importer.keepQuads = value != 0;
            if (TryInt(map, "uv2", out value)) importer.generateSecondaryUV = value != 0;
            if (TryInt(map, "lights", out value)) importer.importLights = value != 0;
            if (TryInt(map, "cams", out value)) importer.importCameras = value != 0;
            if (TryInt(map, "animComp", out value)) importer.animationCompression = (ModelImporterAnimationCompression)value;
            if (TryInt(map, "meshComp", out value)) importer.meshCompression = (ModelImporterMeshCompression)value;
            if (TryInt(map, "animType", out value)) importer.animationType = (ModelImporterAnimationType)value;
            if (TryInt(map, "avatarSetup", out value)) importer.avatarSetup = (ModelImporterAvatarSetup)value;
            if (TryInt(map, "skin", out value)) importer.skinWeights = (ModelImporterSkinWeights)value;
            if (TryInt(map, "maxBones", out value)) importer.maxBonesPerVertex = value;
        }

        static void ApplyAudio(AudioImporter importer, Dictionary<string, string> map)
        {
            int value;
            if (TryInt(map, "mono", out value)) importer.forceToMono = value != 0;
            if (TryInt(map, "loadBg", out value)) importer.loadInBackground = value != 0;
            if (!map.ContainsKey("fmt") && !map.ContainsKey("load") && !map.ContainsKey("quality")) return;
            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            if (TryInt(map, "fmt", out value)) sample.compressionFormat = (AudioCompressionFormat)value;
            if (TryInt(map, "load", out value)) sample.loadType = (AudioClipLoadType)value;
            float quality;
            if (TryFloat(map, "quality", out quality)) sample.quality = quality;
            importer.defaultSampleSettings = sample;
        }

        static bool ApplyComponent(Transform target, UndoRecord record)
        {
            if (target == null) return false;
            Dictionary<string, string> map = Parse(record.data);
            int value;
            if (record.kind == "renderer")
            {
                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer == null) return false;
                if (TryInt(map, "recv", out value)) renderer.receiveShadows = value != 0;
                if (TryInt(map, "probes", out value)) renderer.lightProbeUsage = (LightProbeUsage)value;
                if (TryInt(map, "reflect", out value)) renderer.reflectionProbeUsage = (ReflectionProbeUsage)value;
                if (TryInt(map, "motion", out value)) renderer.motionVectorGenerationMode = (MotionVectorGenerationMode)value;
                if (TryInt(map, "shadows", out value)) renderer.shadowCastingMode = (ShadowCastingMode)value;
                SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                if (skinned != null)
                {
                    if (map.ContainsKey("cx"))
                    {
                        float cx, cy, cz, sx, sy, sz;
                        if (TryFloat(map, "cx", out cx) && TryFloat(map, "cy", out cy) && TryFloat(map, "cz", out cz)
                            && TryFloat(map, "sx", out sx) && TryFloat(map, "sy", out sy) && TryFloat(map, "sz", out sz))
                        {
                            skinned.updateWhenOffscreen = false;
                            skinned.localBounds = new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));
                        }
                        if (TryInt(map, "smv", out value)) skinned.skinnedMotionVectors = value != 0;
                        if (TryInt(map, "uwo", out value)) skinned.updateWhenOffscreen = value != 0;
                    }
                    else if (TryInt(map, "uwo", out value)) skinned.updateWhenOffscreen = value != 0;
                    if (TryInt(map, "quality", out value)) skinned.quality = (SkinQuality)value;
                }
                EditorUtility.SetDirty(renderer);
                NotePrefabInstance(renderer);
                return true;
            }
            if (record.kind == "animator")
            {
                Animator animator = target.GetComponent<Animator>();
                if (animator == null || !TryInt(map, "cull", out value)) return false;
                animator.cullingMode = (AnimatorCullingMode)value;
                EditorUtility.SetDirty(animator);
                NotePrefabInstance(animator);
                return true;
            }
            if (record.kind == "light")
            {
                Light light = target.GetComponent<Light>();
                if (light == null || !TryInt(map, "enabled", out value)) return false;
                light.enabled = value != 0;
                EditorUtility.SetDirty(light);
                NotePrefabInstance(light);
                return true;
            }
            if (record.kind == "camera")
            {
                Camera camera = target.GetComponent<Camera>();
                if (camera == null || !TryInt(map, "enabled", out value)) return false;
                camera.enabled = value != 0;
                EditorUtility.SetDirty(camera);
                NotePrefabInstance(camera);
                return true;
            }
            if (record.kind == "particle")
            {
                ParticleSystemRenderer particle = target.GetComponent<ParticleSystemRenderer>();
                if (particle == null) return false;
                if (TryInt(map, "shadows", out value)) particle.shadowCastingMode = (ShadowCastingMode)value;
                if (TryInt(map, "motion", out value)) particle.motionVectorGenerationMode = (MotionVectorGenerationMode)value;
                EditorUtility.SetDirty(particle);
                NotePrefabInstance(particle);
                return true;
            }
            return false;
        }

        static void NotePrefabInstance(UnityEngine.Object component)
        {
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        static Transform FindRelative(Transform root, string relative)
        {
            if (root == null) return null;
            if (string.IsNullOrEmpty(relative)) return root;
            return root.Find(relative);
        }

        static Transform FindSceneTransform(string scenePath, string hierarchy)
        {
            if (string.IsNullOrEmpty(hierarchy)) return null;
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (!string.IsNullOrEmpty(scenePath) && !string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase)) continue;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    Transform found = FindByFullPath(roots[r].transform, hierarchy);
                    if (found != null) return found;
                }
            }
            return null;
        }

        static Transform FindByFullPath(Transform transform, string hierarchy)
        {
            if (string.Equals(FullPath(transform), hierarchy, StringComparison.Ordinal)) return transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform found = FindByFullPath(transform.GetChild(i), hierarchy);
                if (found != null) return found;
            }
            return null;
        }

        static Dictionary<string, string> Parse(string data)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(data)) return map;
            string[] pairs = data.Split(';');
            for (int i = 0; i < pairs.Length; i++)
            {
                int split = pairs[i].IndexOf('=');
                if (split <= 0) continue;
                map[pairs[i].Substring(0, split)] = pairs[i].Substring(split + 1);
            }
            return map;
        }

        static bool TryInt(Dictionary<string, string> map, string key, out int value)
        {
            value = 0;
            string raw;
            if (map == null || !map.TryGetValue(key, out raw)) return false;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        static bool TryFloat(Dictionary<string, string> map, string key, out float value)
        {
            value = 0f;
            string raw;
            if (map == null || !map.TryGetValue(key, out raw)) return false;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        static string Key(UndoRecord record)
        {
            return record.id + "\n" + record.kind + "\n" + record.asset + "\n" + record.hierarchy + "\n" + record.scene + "\n" + record.slot;
        }

        static bool HasStored(string optionId)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (string.Equals(records[i].id, optionId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        static bool IsSpecial(string optionId)
        {
            return optionId == MeshCompression
                || optionId == ForceHumanoid
                || optionId == StripBlendShapes
                || optionId == RecalculateBounds
                || optionId == DisableLights
                || optionId == DisableCameras
                || optionId == GpuInstancing
                || optionId == AudioMono
                || optionId == TextureCrunchOn;
        }

        static string SpecialTitle(string optionId)
        {
            if (optionId == MeshCompression) return "Apply mesh compression";
            if (optionId == ForceHumanoid) return "Force Humanoid rig";
            if (optionId == StripBlendShapes) return "Disable blend shape import";
            if (optionId == RecalculateBounds) return "Recalculate skinned bounds";
            if (optionId == DisableLights) return "Disable realtime lights";
            if (optionId == DisableCameras) return "Disable cameras";
            if (optionId == GpuInstancing) return "GPU instancing";
            if (optionId == AudioMono) return "Force audio to mono";
            if (optionId == TextureCrunchOn) return "Enable crunch compression";
            return optionId;
        }

        public static void NoteHeader(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            try
            {
                string path = RunsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "\t" + line + "\n");
            }
            catch (Exception)
            {
            }
        }

        static void NoteApplied(string optionId, string kind, string asset, string hierarchy, string scene, string slot, string data)
        {
            specialWrites++;
            WroteSpecial = true;
            string target = !string.IsNullOrEmpty(hierarchy) ? hierarchy : asset;
            if (applyLog != null)
                applyLog.Add("Special wrote: " + SpecialTitle(optionId) + " — " + target);
            try
            {
                string path = RunsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string when = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string readable = when + "\tWrote\t" + SpecialTitle(optionId) + "\t" + target;
                string line = string.Join("\t", new[]
                {
                    "APPLIED",
                    DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
                    optionId,
                    kind ?? "",
                    asset ?? "",
                    hierarchy ?? "",
                    scene ?? "",
                    slot ?? "",
                    data ?? ""
                });
                File.AppendAllText(path, readable + "\n" + line + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KaleidoVR] Could not log Special apply: " + ex.Message);
            }
            logStamp = DateTime.MinValue;
        }

        static void NoteUndone(string optionId)
        {
            if (applyLog != null)
                applyLog.Add("Special undone: " + SpecialTitle(optionId));
            try
            {
                string path = RunsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, "UNDONE\t" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + "\t" + optionId + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KaleidoVR] Could not log Special undo: " + ex.Message);
            }
            logStamp = DateTime.MinValue;
            logIndexLoaded = false;
        }

        static bool LogHasApplied(string optionId)
        {
            EnsureLogIndex();
            return logApplied.Contains(optionId);
        }

        static void ImportLogRecords(string optionId)
        {
            EnsureLogIndex();
            if (!logApplied.Contains(optionId)) return;
            for (int i = 0; i < logRecords.Count; i++)
            {
                UndoRecord source = logRecords[i];
                if (!string.Equals(source.id, optionId, StringComparison.Ordinal)) continue;
                bool exists = false;
                for (int r = 0; r < records.Count; r++)
                {
                    if (Key(records[r]) == Key(source))
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;
                records.Add(source);
                dirty = true;
            }
        }

        static void EnsureLogIndex()
        {
            string dir = LogDir();
            DateTime stamp = Directory.Exists(dir) ? Directory.GetLastWriteTimeUtc(dir) : DateTime.MinValue;
            string runs = RunsPath();
            if (File.Exists(runs))
            {
                DateTime fileStamp = File.GetLastWriteTimeUtc(runs);
                if (fileStamp > stamp) stamp = fileStamp;
            }
            if (logIndexLoaded && stamp == logStamp) return;
            logStamp = stamp;
            logIndexLoaded = true;
            logApplied = new HashSet<string>(StringComparer.Ordinal);
            logRecords = new List<UndoRecord>();

            List<UndoRecord> pending = new List<UndoRecord>();
            Dictionary<string, long> undoneAt = new Dictionary<string, long>(StringComparer.Ordinal);
            if (Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir, "*_optimizer_log.txt");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < files.Length; i++)
                {
                    long ticks = File.GetLastWriteTimeUtc(files[i]).Ticks;
                    CollectOldLog(files[i], ticks, pending, undoneAt);
                }
            }
            CollectRuns(runs, pending, undoneAt);

            for (int i = 0; i < pending.Count; i++)
            {
                UndoRecord record = pending[i];
                long cleared;
                if (undoneAt.TryGetValue(record.id, out cleared) && record.ticks <= cleared) continue;
                logApplied.Add(record.id);
                logRecords.Add(record);
            }
        }

        static void CollectOldLog(string path, long ticks, List<UndoRecord> pending, Dictionary<string, long> undoneAt)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception)
            {
                return;
            }
            bool apply = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf("Mode: Apply", StringComparison.Ordinal) >= 0) apply = true;
                if (lines[i].IndexOf("Mode: Dry Run", StringComparison.Ordinal) >= 0) apply = false;
            }
            if (!apply) return;
            for (int i = 0; i < lines.Length; i++)
            {
                UndoRecord record = ParseOldSpecialLine(lines[i], ticks);
                if (record == null) continue;
                long cleared;
                if (undoneAt.TryGetValue(record.id, out cleared) && ticks <= cleared) continue;
                pending.Add(record);
            }
        }

        static UndoRecord ParseOldSpecialLine(string line, long ticks)
        {
            if (string.IsNullOrEmpty(line)) return null;
            const string light = "Disable light: ";
            const string camera = "Disable camera: ";
            const string instancing = "Material instancing ";
            if (line.StartsWith(light, StringComparison.Ordinal))
            {
                return OldRecord(DisableLights, "light", "", line.Substring(light.Length).Trim(), "", "enabled=1", ticks);
            }
            if (line.StartsWith(camera, StringComparison.Ordinal))
            {
                return OldRecord(DisableCameras, "camera", "", line.Substring(camera.Length).Trim(), "", "enabled=1", ticks);
            }
            if (!line.StartsWith(instancing, StringComparison.Ordinal)) return null;
            int split = line.IndexOf(": ", StringComparison.Ordinal);
            if (split < 0) return null;
            string value = line.Substring(instancing.Length, split - instancing.Length).Trim();
            string asset = line.Substring(split + 2).Trim();
            string previous = value.Equals("True", StringComparison.OrdinalIgnoreCase) ? "0" : "1";
            return OldRecord(GpuInstancing, "material", asset, "", "", "instancing=" + previous, ticks);
        }

        static UndoRecord OldRecord(string optionId, string kind, string asset, string hierarchy, string scene, string data, long ticks)
        {
            return new UndoRecord
            {
                id = optionId,
                kind = kind,
                asset = asset ?? "",
                hierarchy = hierarchy ?? "",
                scene = scene ?? "",
                slot = "",
                data = data,
                ticks = ticks
            };
        }

        static void CollectRuns(string path, List<UndoRecord> pending, Dictionary<string, long> undoneAt)
        {
            if (!File.Exists(path)) return;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception)
            {
                return;
            }
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split('\t');
                if (parts.Length < 3) continue;
                long ticks;
                if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks)) continue;
                if (parts[0] == "UNDONE")
                {
                    undoneAt[parts[2]] = ticks;
                    pending.RemoveAll(delegate (UndoRecord record)
                    {
                        return string.Equals(record.id, parts[2], StringComparison.Ordinal) && record.ticks <= ticks;
                    });
                    continue;
                }
                if (parts[0] != "APPLIED" || parts.Length < 9) continue;
                pending.Add(new UndoRecord
                {
                    id = parts[2],
                    kind = parts[3],
                    asset = parts[4],
                    hierarchy = parts[5],
                    scene = parts[6],
                    slot = parts[7],
                    data = parts[8],
                    ticks = ticks
                });
            }
        }

        static string LogDir()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "KaleidoVR", "Optimizer");
        }

        static string RunsPath()
        {
            return Path.Combine(LogDir(), "special-runs.log");
        }

        static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            records = new List<UndoRecord>();
            try
            {
                string path = StorePath();
                if (!File.Exists(path)) return;
                UndoFile file = JsonUtility.FromJson<UndoFile>(File.ReadAllText(path));
                if (file == null || file.records == null) return;
                for (int i = 0; i < file.records.Length; i++)
                {
                    if (file.records[i] != null && !string.IsNullOrEmpty(file.records[i].id)) records.Add(file.records[i]);
                }
            }
            catch (Exception)
            {
                records = new List<UndoRecord>();
            }
        }

        static string StorePath()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "KaleidoVR", "optimizer-undo.json");
        }

        static bool loaded;
        static bool dirty;
        static bool applyWrite;
        static bool logIndexLoaded;
        static int specialWrites;
        static DateTime logStamp = DateTime.MinValue;
        static List<string> applyLog;
        static HashSet<string> logApplied = new HashSet<string>(StringComparer.Ordinal);
        static List<UndoRecord> logRecords = new List<UndoRecord>();
        static List<UndoRecord> records = new List<UndoRecord>();

        [Serializable]
        public class UndoFile
        {
            public UndoRecord[] records;
        }

        [Serializable]
        public class UndoRecord
        {
            public string id;
            public string kind;
            public string asset;
            public string hierarchy;
            public string scene;
            public string slot;
            public string data;
            public long ticks;
        }
    }
}
