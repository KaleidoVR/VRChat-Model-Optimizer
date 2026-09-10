// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. Released under the MIT License.
// Compatible with Unity 2022.3.22f1 through Unity 6 (6000.x)
// VRChat SDK3 Avatars optional (performance counts for PhysBones / contacts)
// Uses 2022.3 LTS AssetDatabase / importer APIs only (no 2023+/Unity 6-only types)

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace KaleidoVR.EditorTools
{
    public enum KaleidoPcTexFormat
    {
        Automatic = 0,
        HighQuality = 1,
        BC7 = 2,
        DXT5 = 3
    }

    public enum KaleidoAndroidTexFormat
    {
        ASTC_4x4 = 0,
        ASTC_5x5 = 1,
        ASTC_6x6 = 2,
        ASTC_8x8 = 3,
        ETC2_RGBA8 = 4
    }

    public enum KaleidoMeshCompressionChoice
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum KaleidoSkinWeightChoice
    {
        FourBones = 0,
        Unlimited = 1
    }

    public enum KaleidoTextureKind
    {
        Albedo = 0,
        Normal = 1,
        Mask = 2,
        Emission = 3,
        Matcap = 4,
        Other = 5
    }

    [Serializable]
    public class KaleidoOptimizerProfile
    {
        public string name = "My Profile";
        public bool includeTextures = true;
        public bool includeMeshes = true;
        public bool includeRenderers = true;
        public bool includeAudio = true;
        public bool includeAnimators = true;
        public bool includeSpecial = false;

        public bool optimizeTextures = true;
        public bool applyAlbedoSize = true;
        public int albedoPc = 2048;
        public int albedoQuest = 1024;
        public bool applyNormalSize = true;
        public int normalPc = 2048;
        public int normalQuest = 1024;
        public bool applyMaskSize = true;
        public int maskPc = 1024;
        public int maskQuest = 512;
        public bool applyEmissionSize = true;
        public int emissionPc = 1024;
        public int emissionQuest = 512;
        public bool applyMatcapSize = true;
        public int matcapPc = 512;
        public int matcapQuest = 256;
        public bool applyOtherSize = true;
        public int otherPc = 1024;
        public int otherQuest = 512;
        public bool applyPcTexFormat = true;
        public KaleidoPcTexFormat pcTexFormat = KaleidoPcTexFormat.HighQuality;
        public bool applyAndroidTexFormat = true;
        public KaleidoAndroidTexFormat androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
        public bool textureDisableReadWrite = true;
        public bool textureApplyMipmaps = true;
        public bool textureEnableMipmaps = true;
        public bool textureDisableStreamingMipmaps = true;
        public bool textureDisableCrunch = true;
        public bool textureApplyAniso = true;
        public int textureAniso = 1;
        public bool autoDetectNormalMaps = true;
        public bool autoLinearMaskMaps = true;
        public bool higherQualityNormalMaps = true;
        public bool alphaIsTransparencyOnAlbedo = true;

        public bool optimizeMeshes = true;
        public bool meshEnableReadWrite = true;
        public bool meshOptimizePolygons = true;
        public bool meshOptimizeVertices = true;
        public bool meshWeldVertices = true;
        public bool meshKeepBlendShapes = true;
        public bool meshDisableQuads = true;
        public bool meshDisableLightmapUVs = true;
        public bool meshDisableImportLightsCameras = true;
        public bool meshOptimizeAnimation = true;
        public bool applySkinWeights = true;
        public KaleidoSkinWeightChoice skinWeights = KaleidoSkinWeightChoice.FourBones;

        public bool optimizeRenderers = true;
        public bool rendererDisableUpdateWhenOffscreen = true;
        public bool rendererDisableShadows = true;
        public bool rendererDisableReceiveShadows = true;
        public bool rendererDisableProbes = true;
        public bool rendererDisableMotionVectors = true;
        public bool rendererForceBone4 = true;
        public bool rendererRecalculateBounds = false;
        public bool applyToPrefabAssets = true;
        public bool optimizeParticles = true;
        public bool disableLightsOnAvatar = false;

        public bool optimizeAudio = true;
        public bool audioLoadInBackground = true;
        public bool audioApplyVorbis = true;
        public float audioQuality = 0.7f;

        public bool optimizeAnimators = true;
        public bool animatorCullWhenOffscreen = true;

        public KaleidoMeshCompressionChoice meshCompression = KaleidoMeshCompressionChoice.Off;
        public bool applyMeshCompression = false;
        public bool meshForceHumanoid = false;
        public bool meshStripBlendShapes = false;
        public bool optimizeMaterials = false;
        public bool materialEnableGpuInstancing = false;
        public bool disableCamerasOnAvatar = false;
        public bool audioForceToMono = false;
        public bool textureEnableCrunch = false;
    }

    [Serializable]
    public class KaleidoOptimizerProfileList
    {
        public KaleidoOptimizerProfile[] profiles = new KaleidoOptimizerProfile[0];
    }

    public class KaleidoVRCOptimizer : EditorWindow
    {
        public static readonly string VERSION = "1.0.2";
        public const string LOGO_FILE_NAME = "Kali_Logo.png";
        public const string FALLBACK_ICON_PATH = "Assets/KaleidoVR/Editor/Icons/Kali_Logo.png";
        public const string PrefsPrefix = "KVR_VrcOpt_";

        public static string ICON_PATH { get { return ResolveIconPath(); } }

        private static string cachedIconPath;
        private Texture2D headerIcon;
        private Vector2 scroll;

        public int tab;
        public int builtinPresetIndex = 2;
        public int selectedUserProfile = -1;
        public string newProfileName = "My Profile";
        public KaleidoOptimizerProfileList userProfiles = new KaleidoOptimizerProfileList();

        public List<UnityEngine.Object> targets = new List<UnityEngine.Object>();
        public List<UnityEngine.Object> ignoreList = new List<UnityEngine.Object>();

        public bool dryRun = true;
        public bool writeLog = true;
        public bool applyToPrefabAssets = true;

        public bool applyAlbedoSize = true;
        public int albedoPc = 2048;
        public int albedoQuest = 1024;
        public bool applyNormalSize = true;
        public int normalPc = 2048;
        public int normalQuest = 1024;
        public bool applyMaskSize = true;
        public int maskPc = 1024;
        public int maskQuest = 512;
        public bool applyEmissionSize = true;
        public int emissionPc = 1024;
        public int emissionQuest = 512;
        public bool applyMatcapSize = true;
        public int matcapPc = 512;
        public int matcapQuest = 256;
        public bool applyOtherSize = true;
        public int otherPc = 1024;
        public int otherQuest = 512;

        public bool optimizeTextures = true;
        public bool applyPcTexFormat = true;
        public KaleidoPcTexFormat pcTexFormat = KaleidoPcTexFormat.HighQuality;
        public bool applyAndroidTexFormat = true;
        public KaleidoAndroidTexFormat androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
        public bool textureDisableReadWrite = true;
        public bool textureApplyMipmaps = true;
        public bool textureEnableMipmaps = true;
        public bool textureDisableStreamingMipmaps = true;
        public bool textureDisableCrunch = true;
        public bool textureApplyAniso = true;
        public int textureAniso = 1;
        public bool autoDetectNormalMaps = true;
        public bool autoLinearMaskMaps = true;
        public bool higherQualityNormalMaps = true;
        public bool alphaIsTransparencyOnAlbedo = true;

        public bool optimizeMeshes = true;
        public bool meshEnableReadWrite = true;
        public bool meshOptimizePolygons = true;
        public bool meshOptimizeVertices = true;
        public bool meshWeldVertices = true;
        public bool meshKeepBlendShapes = true;
        public bool meshDisableQuads = true;
        public bool meshDisableLightmapUVs = true;
        public bool meshDisableImportLightsCameras = true;
        public bool meshOptimizeAnimation = true;
        public bool applySkinWeights = true;
        public KaleidoSkinWeightChoice skinWeights = KaleidoSkinWeightChoice.FourBones;

        public bool optimizeRenderers = true;
        public bool rendererDisableUpdateWhenOffscreen = true;
        public bool rendererDisableShadows = true;
        public bool rendererDisableReceiveShadows = true;
        public bool rendererDisableProbes = true;
        public bool rendererDisableMotionVectors = true;
        public bool rendererForceBone4 = true;
        public bool rendererRecalculateBounds = false;
        public bool optimizeParticles = true;
        public bool disableLightsOnAvatar = false;

        public bool optimizeAudio = true;
        public bool audioLoadInBackground = true;
        public bool audioApplyVorbis = true;
        public float audioQuality = 0.7f;

        public bool optimizeAnimators = true;
        public bool animatorCullWhenOffscreen = true;

        public bool applyMeshCompression = false;
        public KaleidoMeshCompressionChoice meshCompression = KaleidoMeshCompressionChoice.Off;
        public bool meshForceHumanoid = false;
        public bool meshStripBlendShapes = false;
        public bool optimizeMaterials = false;
        public bool materialEnableGpuInstancing = false;
        public bool disableCamerasOnAvatar = false;
        public bool audioForceToMono = false;
        public bool textureEnableCrunch = false;
        public bool optimizeSceneExtras = false;

        public KaleidoOptimizerReport lastReport;
        public List<KaleidoModelInventoryItem> inventory = new List<KaleidoModelInventoryItem>();
        public string inventorySignature = "";
        public Vector2 inventoryScroll;

        [MenuItem("KaleidoVR/VRChat Model Optimizer", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<KaleidoVRCOptimizer>("VRChat Model Optimizer");
            window.InitializeLocalLogo();
            window.LoadEditorPreferences();
            window.minSize = new Vector2(500, 720);
            window.ApplyWindowIcon();
        }

        private void OnEnable()
        {
            InitializeLocalLogo();
            LoadEditorPreferences();
            ApplyWindowIcon();
        }

        private void InitializeLocalLogo()
        {
            cachedIconPath = null;
            headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
        }

        private void ApplyWindowIcon()
        {
            if (headerIcon == null) headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
            titleContent = new GUIContent("VRC Model Optimizer", headerIcon);
        }

        public static string GetToolEditorFolder()
        {
            foreach (string guid in AssetDatabase.FindAssets("KaleidoVRCOptimizer t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (scriptPath.EndsWith("/KaleidoVRCOptimizer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    return scriptPath.Substring(0, scriptPath.LastIndexOf('/'));
                }
            }
            return null;
        }

        private static string ResolveIconPath()
        {
            if (!string.IsNullOrEmpty(cachedIconPath)) return cachedIconPath;

            string editorFolder = GetToolEditorFolder();
            if (!string.IsNullOrEmpty(editorFolder))
            {
                string relativeIcon = editorFolder + "/Icons/" + LOGO_FILE_NAME;
                if (AssetDatabase.LoadMainAssetAtPath(relativeIcon) != null)
                {
                    cachedIconPath = relativeIcon;
                    return cachedIconPath;
                }
            }

            string logoName = Path.GetFileNameWithoutExtension(LOGO_FILE_NAME);
            foreach (string guid in AssetDatabase.FindAssets(logoName + " t:Texture2D"))
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (foundPath.EndsWith("/" + LOGO_FILE_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    cachedIconPath = foundPath;
                    return cachedIconPath;
                }
            }

            cachedIconPath = !string.IsNullOrEmpty(editorFolder) ? editorFolder + "/Icons/" + LOGO_FILE_NAME : FALLBACK_ICON_PATH;
            return cachedIconPath;
        }

        public void ApplyBuiltinPreset(int index)
        {
            builtinPresetIndex = index;
            selectedUserProfile = -1;

            includeTextures = true;
            includeMeshes = true;
            includeRenderers = true;
            includeAudio = true;
            includeAnimators = true;
            includeSpecial = false;

            optimizeTextures = true;
            applyAlbedoSize = applyNormalSize = applyMaskSize = applyEmissionSize = applyMatcapSize = applyOtherSize = true;
            applyPcTexFormat = true;
            applyAndroidTexFormat = true;
            pcTexFormat = KaleidoPcTexFormat.HighQuality;
            androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
            textureDisableReadWrite = true;
            textureApplyMipmaps = true;
            textureEnableMipmaps = true;
            textureDisableStreamingMipmaps = true;
            textureDisableCrunch = true;
            textureEnableCrunch = false;
            textureApplyAniso = true;
            textureAniso = 1;
            autoDetectNormalMaps = true;
            autoLinearMaskMaps = true;
            higherQualityNormalMaps = true;
            alphaIsTransparencyOnAlbedo = true;

            optimizeMeshes = true;
            meshEnableReadWrite = true;
            meshOptimizePolygons = true;
            meshOptimizeVertices = true;
            meshWeldVertices = true;
            meshKeepBlendShapes = true;
            meshDisableQuads = true;
            meshDisableLightmapUVs = true;
            meshDisableImportLightsCameras = true;
            meshOptimizeAnimation = true;
            applySkinWeights = true;
            applyMeshCompression = false;
            meshCompression = KaleidoMeshCompressionChoice.Off;
            meshForceHumanoid = false;
            meshStripBlendShapes = false;

            optimizeRenderers = true;
            rendererDisableUpdateWhenOffscreen = true;
            rendererDisableReceiveShadows = true;
            rendererDisableProbes = true;
            rendererDisableMotionVectors = true;
            rendererRecalculateBounds = false;
            applyToPrefabAssets = true;
            optimizeParticles = true;
            disableLightsOnAvatar = true;

            optimizeAudio = true;
            audioForceToMono = false;
            audioLoadInBackground = true;
            audioApplyVorbis = true;
            audioQuality = 0.7f;

            optimizeAnimators = true;
            animatorCullWhenOffscreen = true;
            optimizeMaterials = false;
            materialEnableGpuInstancing = false;
            disableCamerasOnAvatar = false;
            optimizeSceneExtras = false;

            if (index == 0)
            {
                SetTypeSizes(2048, 2048, 2048, 2048, 1024, 1024, 1024, 1024, 512, 512, 1024, 1024);
                skinWeights = KaleidoSkinWeightChoice.Unlimited;
                rendererDisableShadows = false;
                rendererForceBone4 = false;
            }
            else if (index == 1)
            {
                SetTypeSizes(1024, 1024, 1024, 1024, 512, 512, 512, 512, 256, 256, 512, 512);
                skinWeights = KaleidoSkinWeightChoice.FourBones;
                rendererDisableShadows = true;
                rendererForceBone4 = true;
                disableCamerasOnAvatar = true;
                optimizeMaterials = true;
                materialEnableGpuInstancing = true;
            }
            else
            {
                SetTypeSizes(2048, 1024, 2048, 1024, 1024, 512, 1024, 512, 512, 256, 1024, 512);
                skinWeights = KaleidoSkinWeightChoice.FourBones;
                rendererDisableShadows = true;
                rendererForceBone4 = true;
                disableCamerasOnAvatar = true;
                optimizeMaterials = true;
                materialEnableGpuInstancing = true;
            }
        }

        private void SetTypeSizes(int aPc, int aQ, int nPc, int nQ, int mPc, int mQ, int ePc, int eQ, int cPc, int cQ, int oPc, int oQ)
        {
            albedoPc = aPc; albedoQuest = aQ;
            normalPc = nPc; normalQuest = nQ;
            maskPc = mPc; maskQuest = mQ;
            emissionPc = ePc; emissionQuest = eQ;
            matcapPc = cPc; matcapQuest = cQ;
            otherPc = oPc; otherQuest = oQ;
        }

        public KaleidoOptimizerProfile CaptureProfile(string name)
        {
            KaleidoOptimizerProfile p = CaptureSettings();
            p.name = string.IsNullOrWhiteSpace(name) ? "My Profile" : name.Trim();
            p.includeTextures = includeTextures;
            p.includeMeshes = includeMeshes;
            p.includeRenderers = includeRenderers;
            p.includeAudio = includeAudio;
            p.includeAnimators = includeAnimators;
            p.includeSpecial = includeSpecial;
            return p;
        }

        private KaleidoOptimizerProfile CaptureSettings()
        {
            return new KaleidoOptimizerProfile
            {
                optimizeTextures = optimizeTextures,
                applyAlbedoSize = applyAlbedoSize, albedoPc = albedoPc, albedoQuest = albedoQuest,
                applyNormalSize = applyNormalSize, normalPc = normalPc, normalQuest = normalQuest,
                applyMaskSize = applyMaskSize, maskPc = maskPc, maskQuest = maskQuest,
                applyEmissionSize = applyEmissionSize, emissionPc = emissionPc, emissionQuest = emissionQuest,
                applyMatcapSize = applyMatcapSize, matcapPc = matcapPc, matcapQuest = matcapQuest,
                applyOtherSize = applyOtherSize, otherPc = otherPc, otherQuest = otherQuest,
                applyPcTexFormat = applyPcTexFormat, pcTexFormat = pcTexFormat,
                applyAndroidTexFormat = applyAndroidTexFormat, androidTexFormat = androidTexFormat,
                textureDisableReadWrite = textureDisableReadWrite,
                textureApplyMipmaps = textureApplyMipmaps, textureEnableMipmaps = textureEnableMipmaps,
                textureDisableStreamingMipmaps = textureDisableStreamingMipmaps,
                textureDisableCrunch = textureDisableCrunch, textureEnableCrunch = textureEnableCrunch,
                textureApplyAniso = textureApplyAniso, textureAniso = textureAniso,
                autoDetectNormalMaps = autoDetectNormalMaps, autoLinearMaskMaps = autoLinearMaskMaps,
                higherQualityNormalMaps = higherQualityNormalMaps, alphaIsTransparencyOnAlbedo = alphaIsTransparencyOnAlbedo,
                optimizeMeshes = optimizeMeshes, meshEnableReadWrite = meshEnableReadWrite,
                meshOptimizePolygons = meshOptimizePolygons, meshOptimizeVertices = meshOptimizeVertices,
                meshWeldVertices = meshWeldVertices, meshKeepBlendShapes = meshKeepBlendShapes,
                meshDisableQuads = meshDisableQuads, meshDisableLightmapUVs = meshDisableLightmapUVs,
                meshDisableImportLightsCameras = meshDisableImportLightsCameras,
                meshOptimizeAnimation = meshOptimizeAnimation, applySkinWeights = applySkinWeights, skinWeights = skinWeights,
                optimizeRenderers = optimizeRenderers,
                rendererDisableUpdateWhenOffscreen = rendererDisableUpdateWhenOffscreen,
                rendererDisableShadows = rendererDisableShadows, rendererDisableReceiveShadows = rendererDisableReceiveShadows,
                rendererDisableProbes = rendererDisableProbes, rendererDisableMotionVectors = rendererDisableMotionVectors,
                rendererForceBone4 = rendererForceBone4, rendererRecalculateBounds = rendererRecalculateBounds,
                applyToPrefabAssets = applyToPrefabAssets, optimizeParticles = optimizeParticles,
                disableLightsOnAvatar = disableLightsOnAvatar,
                optimizeMaterials = optimizeMaterials, materialEnableGpuInstancing = materialEnableGpuInstancing,
                disableCamerasOnAvatar = disableCamerasOnAvatar,
                optimizeAudio = optimizeAudio, audioLoadInBackground = audioLoadInBackground,
                audioApplyVorbis = audioApplyVorbis, audioQuality = audioQuality,
                optimizeAnimators = optimizeAnimators, animatorCullWhenOffscreen = animatorCullWhenOffscreen,
                applyMeshCompression = applyMeshCompression, meshCompression = meshCompression,
                meshForceHumanoid = meshForceHumanoid, meshStripBlendShapes = meshStripBlendShapes,
                audioForceToMono = audioForceToMono
            };
        }

        public void LoadProfile(KaleidoOptimizerProfile p, bool respectIncludes)
        {
            if (p == null) return;
            bool tex = !respectIncludes || p.includeTextures;
            bool mesh = !respectIncludes || p.includeMeshes;
            bool rend = !respectIncludes || p.includeRenderers;
            bool aud = !respectIncludes || p.includeAudio;
            bool anim = !respectIncludes || p.includeAnimators;
            bool spec = !respectIncludes || p.includeSpecial;

            includeTextures = p.includeTextures;
            includeMeshes = p.includeMeshes;
            includeRenderers = p.includeRenderers;
            includeAudio = p.includeAudio;
            includeAnimators = p.includeAnimators;
            includeSpecial = p.includeSpecial;

            if (tex)
            {
                optimizeTextures = p.optimizeTextures;
                applyAlbedoSize = p.applyAlbedoSize; albedoPc = p.albedoPc; albedoQuest = p.albedoQuest;
                applyNormalSize = p.applyNormalSize; normalPc = p.normalPc; normalQuest = p.normalQuest;
                applyMaskSize = p.applyMaskSize; maskPc = p.maskPc; maskQuest = p.maskQuest;
                applyEmissionSize = p.applyEmissionSize; emissionPc = p.emissionPc; emissionQuest = p.emissionQuest;
                applyMatcapSize = p.applyMatcapSize; matcapPc = p.matcapPc; matcapQuest = p.matcapQuest;
                applyOtherSize = p.applyOtherSize; otherPc = p.otherPc; otherQuest = p.otherQuest;
                applyPcTexFormat = p.applyPcTexFormat; pcTexFormat = p.pcTexFormat;
                applyAndroidTexFormat = p.applyAndroidTexFormat; androidTexFormat = p.androidTexFormat;
                textureDisableReadWrite = p.textureDisableReadWrite;
                textureApplyMipmaps = p.textureApplyMipmaps; textureEnableMipmaps = p.textureEnableMipmaps;
                textureDisableStreamingMipmaps = p.textureDisableStreamingMipmaps;
                textureDisableCrunch = p.textureDisableCrunch; textureEnableCrunch = p.textureEnableCrunch;
                textureApplyAniso = p.textureApplyAniso; textureAniso = p.textureAniso;
                autoDetectNormalMaps = p.autoDetectNormalMaps; autoLinearMaskMaps = p.autoLinearMaskMaps;
                higherQualityNormalMaps = p.higherQualityNormalMaps; alphaIsTransparencyOnAlbedo = p.alphaIsTransparencyOnAlbedo;
            }
            if (mesh)
            {
                optimizeMeshes = p.optimizeMeshes;
                meshEnableReadWrite = p.meshEnableReadWrite;
                meshOptimizePolygons = p.meshOptimizePolygons;
                meshOptimizeVertices = p.meshOptimizeVertices;
                meshWeldVertices = p.meshWeldVertices;
                meshKeepBlendShapes = p.meshKeepBlendShapes;
                meshDisableQuads = p.meshDisableQuads;
                meshDisableLightmapUVs = p.meshDisableLightmapUVs;
                meshDisableImportLightsCameras = p.meshDisableImportLightsCameras;
                meshOptimizeAnimation = p.meshOptimizeAnimation;
                applySkinWeights = p.applySkinWeights;
                skinWeights = p.skinWeights;
            }
            if (rend)
            {
                optimizeRenderers = p.optimizeRenderers;
                rendererDisableUpdateWhenOffscreen = p.rendererDisableUpdateWhenOffscreen;
                rendererDisableShadows = p.rendererDisableShadows;
                rendererDisableReceiveShadows = p.rendererDisableReceiveShadows;
                rendererDisableProbes = p.rendererDisableProbes;
                rendererDisableMotionVectors = p.rendererDisableMotionVectors;
                rendererForceBone4 = p.rendererForceBone4;
                rendererRecalculateBounds = p.rendererRecalculateBounds;
                applyToPrefabAssets = p.applyToPrefabAssets;
                optimizeParticles = p.optimizeParticles;
                disableLightsOnAvatar = p.disableLightsOnAvatar;
                optimizeMaterials = p.optimizeMaterials;
                materialEnableGpuInstancing = p.materialEnableGpuInstancing;
                disableCamerasOnAvatar = p.disableCamerasOnAvatar;
            }
            if (aud)
            {
                optimizeAudio = p.optimizeAudio;
                audioLoadInBackground = p.audioLoadInBackground;
                audioApplyVorbis = p.audioApplyVorbis;
                audioQuality = p.audioQuality;
            }
            if (anim)
            {
                optimizeAnimators = p.optimizeAnimators;
                animatorCullWhenOffscreen = p.animatorCullWhenOffscreen;
            }
            if (spec)
            {
                applyMeshCompression = p.applyMeshCompression;
                meshCompression = p.meshCompression;
                meshForceHumanoid = p.meshForceHumanoid;
                meshStripBlendShapes = p.meshStripBlendShapes;
                audioForceToMono = p.audioForceToMono;
                textureEnableCrunch = p.textureEnableCrunch;
            }
            else
            {
                applyMeshCompression = false;
                meshForceHumanoid = false;
                meshStripBlendShapes = false;
                audioForceToMono = false;
                textureEnableCrunch = false;
            }
            optimizeSceneExtras = disableLightsOnAvatar || disableCamerasOnAvatar || optimizeParticles;
        }

        public bool includeTextures = true;
        public bool includeMeshes = true;
        public bool includeRenderers = true;
        public bool includeAudio = true;
        public bool includeAnimators = true;
        public bool includeSpecial = false;

        public void SaveUserProfiles()
        {
            if (userProfiles == null) userProfiles = new KaleidoOptimizerProfileList();
            if (userProfiles.profiles == null) userProfiles.profiles = new KaleidoOptimizerProfile[0];
            EditorPrefs.SetString(PrefsPrefix + "ProfilesJson", JsonUtility.ToJson(userProfiles));
        }

        public void LoadUserProfiles()
        {
            string json = EditorPrefs.GetString(PrefsPrefix + "ProfilesJson", "");
            if (string.IsNullOrEmpty(json))
            {
                userProfiles = new KaleidoOptimizerProfileList { profiles = new KaleidoOptimizerProfile[0] };
                return;
            }
            try
            {
                userProfiles = JsonUtility.FromJson<KaleidoOptimizerProfileList>(json) ?? new KaleidoOptimizerProfileList();
                if (userProfiles.profiles == null) userProfiles.profiles = new KaleidoOptimizerProfile[0];
            }
            catch (Exception)
            {
                userProfiles = new KaleidoOptimizerProfileList { profiles = new KaleidoOptimizerProfile[0] };
            }
        }

        private void LoadEditorPreferences()
        {
            LoadUserProfiles();
            tab = GetInt("Tab", 0);
            builtinPresetIndex = GetInt("Builtin", 2);
            selectedUserProfile = GetInt("UserProf", -1);
            newProfileName = EditorPrefs.HasKey(PrefsPrefix + "NewName") ? EditorPrefs.GetString(PrefsPrefix + "NewName") : "My Profile";
            dryRun = GetBool("DryRun", true);
            writeLog = GetBool("WriteLog", true);
            applyToPrefabAssets = GetBool("ApplyPrefabs", true);
            includeTextures = GetBool("IncTex", true);
            includeMeshes = GetBool("IncMesh", true);
            includeRenderers = GetBool("IncRend", true);
            includeAudio = GetBool("IncAud", true);
            includeAnimators = GetBool("IncAnim", true);
            includeSpecial = GetBool("IncSpec", false);

            optimizeTextures = GetBool("OptTex", true);
            applyAlbedoSize = GetBool("ASize", true);
            albedoPc = GetInt("APc", 2048); albedoQuest = GetInt("AQ", 1024);
            applyNormalSize = GetBool("NSize", true);
            normalPc = GetInt("NPc", 2048); normalQuest = GetInt("NQ", 1024);
            applyMaskSize = GetBool("MSize", true);
            maskPc = GetInt("MPc", 1024); maskQuest = GetInt("MQ", 512);
            applyEmissionSize = GetBool("ESize", true);
            emissionPc = GetInt("EPc", 1024); emissionQuest = GetInt("EQ", 512);
            applyMatcapSize = GetBool("CSize", true);
            matcapPc = GetInt("CPc", 512); matcapQuest = GetInt("CQ", 256);
            applyOtherSize = GetBool("OSize", true);
            otherPc = GetInt("OPc", 1024); otherQuest = GetInt("OQ", 512);
            applyPcTexFormat = GetBool("PcFmtOn", true);
            applyAndroidTexFormat = GetBool("AndFmtOn", true);
            pcTexFormat = (KaleidoPcTexFormat)GetInt("PcFmt", (int)KaleidoPcTexFormat.HighQuality);
            androidTexFormat = (KaleidoAndroidTexFormat)GetInt("AndFmt", (int)KaleidoAndroidTexFormat.ASTC_6x6);
            textureDisableReadWrite = GetBool("TexRW", true);
            textureApplyMipmaps = GetBool("TexMipsOn", true);
            textureEnableMipmaps = GetBool("TexMips", true);
            textureDisableStreamingMipmaps = GetBool("TexStream", true);
            textureDisableCrunch = GetBool("TexCrunch", true);
            textureEnableCrunch = GetBool("TexCrunchOn", false);
            textureApplyAniso = GetBool("TexAnisoOn", true);
            textureAniso = GetInt("TexAniso", 1);
            autoDetectNormalMaps = GetBool("TexNorm", true);
            autoLinearMaskMaps = GetBool("TexLinear", true);
            higherQualityNormalMaps = GetBool("TexNormHQ", true);
            alphaIsTransparencyOnAlbedo = GetBool("TexAlpha", true);

            optimizeMeshes = GetBool("OptMesh", true);
            applyMeshCompression = GetBool("MeshCompOn", false);
            meshCompression = (KaleidoMeshCompressionChoice)GetInt("MeshComp", (int)KaleidoMeshCompressionChoice.Off);
            meshEnableReadWrite = GetBool("MeshEnableRW", true);
            meshOptimizePolygons = GetBool("MeshPoly", true);
            meshOptimizeVertices = GetBool("MeshVert", true);
            meshWeldVertices = GetBool("MeshWeld", true);
            meshKeepBlendShapes = GetBool("MeshBS", true);
            meshStripBlendShapes = GetBool("MeshBSOff", false);
            meshDisableQuads = GetBool("MeshQuads", true);
            meshDisableLightmapUVs = GetBool("MeshLM", true);
            meshDisableImportLightsCameras = GetBool("MeshLC", true);
            meshOptimizeAnimation = GetBool("MeshAnim", true);
            applySkinWeights = GetBool("SkinOn", true);
            skinWeights = (KaleidoSkinWeightChoice)GetInt("SkinW", (int)KaleidoSkinWeightChoice.FourBones);
            meshForceHumanoid = GetBool("MeshHum", false);

            optimizeRenderers = GetBool("OptRend", true);
            rendererDisableUpdateWhenOffscreen = GetBool("RendOff", true);
            rendererDisableShadows = GetBool("RendShad", true);
            rendererDisableReceiveShadows = GetBool("RendRecv", true);
            rendererDisableProbes = GetBool("RendProbe", true);
            rendererDisableMotionVectors = GetBool("RendMV", true);
            rendererForceBone4 = GetBool("RendBone4", true);
            rendererRecalculateBounds = GetBool("RendBounds", false);

            optimizeAudio = GetBool("OptAud", true);
            audioForceToMono = GetBool("AudMono", false);
            audioLoadInBackground = GetBool("AudBG", true);
            audioApplyVorbis = GetBool("AudVorb", true);
            audioQuality = EditorPrefs.HasKey(PrefsPrefix + "AudQ") ? EditorPrefs.GetFloat(PrefsPrefix + "AudQ") : 0.7f;

            optimizeAnimators = GetBool("OptAnim", true);
            animatorCullWhenOffscreen = GetBool("AnimCull", true);

            optimizeMaterials = GetBool("OptMat", false);
            materialEnableGpuInstancing = GetBool("MatGPU", false);

            optimizeSceneExtras = GetBool("OptExtra", false);
            disableLightsOnAvatar = GetBool("ExtraLight", true);
            disableCamerasOnAvatar = GetBool("ExtraCam", false);
            optimizeParticles = GetBool("ExtraPart", true);
        }

        public void SaveEditorPreferences()
        {
            SetInt("Tab", tab);
            SetInt("Builtin", builtinPresetIndex);
            SetInt("UserProf", selectedUserProfile);
            EditorPrefs.SetString(PrefsPrefix + "NewName", newProfileName);
            SetBool("DryRun", dryRun);
            SetBool("WriteLog", writeLog);
            SetBool("ApplyPrefabs", applyToPrefabAssets);
            SetBool("IncTex", includeTextures);
            SetBool("IncMesh", includeMeshes);
            SetBool("IncRend", includeRenderers);
            SetBool("IncAud", includeAudio);
            SetBool("IncAnim", includeAnimators);
            SetBool("IncSpec", includeSpecial);

            SetBool("OptTex", optimizeTextures);
            SetBool("ASize", applyAlbedoSize); SetInt("APc", albedoPc); SetInt("AQ", albedoQuest);
            SetBool("NSize", applyNormalSize); SetInt("NPc", normalPc); SetInt("NQ", normalQuest);
            SetBool("MSize", applyMaskSize); SetInt("MPc", maskPc); SetInt("MQ", maskQuest);
            SetBool("ESize", applyEmissionSize); SetInt("EPc", emissionPc); SetInt("EQ", emissionQuest);
            SetBool("CSize", applyMatcapSize); SetInt("CPc", matcapPc); SetInt("CQ", matcapQuest);
            SetBool("OSize", applyOtherSize); SetInt("OPc", otherPc); SetInt("OQ", otherQuest);
            SetBool("PcFmtOn", applyPcTexFormat);
            SetInt("PcFmt", (int)pcTexFormat);
            SetBool("AndFmtOn", applyAndroidTexFormat);
            SetInt("AndFmt", (int)androidTexFormat);
            SetBool("TexRW", textureDisableReadWrite);
            SetBool("TexMipsOn", textureApplyMipmaps);
            SetBool("TexMips", textureEnableMipmaps);
            SetBool("TexStream", textureDisableStreamingMipmaps);
            SetBool("TexCrunch", textureDisableCrunch);
            SetBool("TexCrunchOn", textureEnableCrunch);
            SetBool("TexAnisoOn", textureApplyAniso);
            SetInt("TexAniso", textureAniso);
            SetBool("TexNorm", autoDetectNormalMaps);
            SetBool("TexLinear", autoLinearMaskMaps);
            SetBool("TexNormHQ", higherQualityNormalMaps);
            SetBool("TexAlpha", alphaIsTransparencyOnAlbedo);

            SetBool("OptMesh", optimizeMeshes);
            SetBool("MeshCompOn", applyMeshCompression);
            SetInt("MeshComp", (int)meshCompression);
            SetBool("MeshEnableRW", meshEnableReadWrite);
            SetBool("MeshPoly", meshOptimizePolygons);
            SetBool("MeshVert", meshOptimizeVertices);
            SetBool("MeshWeld", meshWeldVertices);
            SetBool("MeshBS", meshKeepBlendShapes);
            SetBool("MeshBSOff", meshStripBlendShapes);
            SetBool("MeshQuads", meshDisableQuads);
            SetBool("MeshLM", meshDisableLightmapUVs);
            SetBool("MeshLC", meshDisableImportLightsCameras);
            SetBool("MeshAnim", meshOptimizeAnimation);
            SetBool("SkinOn", applySkinWeights);
            SetInt("SkinW", (int)skinWeights);
            SetBool("MeshHum", meshForceHumanoid);

            SetBool("OptRend", optimizeRenderers);
            SetBool("RendOff", rendererDisableUpdateWhenOffscreen);
            SetBool("RendShad", rendererDisableShadows);
            SetBool("RendRecv", rendererDisableReceiveShadows);
            SetBool("RendProbe", rendererDisableProbes);
            SetBool("RendMV", rendererDisableMotionVectors);
            SetBool("RendBone4", rendererForceBone4);
            SetBool("RendBounds", rendererRecalculateBounds);

            SetBool("OptAud", optimizeAudio);
            SetBool("AudMono", audioForceToMono);
            SetBool("AudBG", audioLoadInBackground);
            SetBool("AudVorb", audioApplyVorbis);
            EditorPrefs.SetFloat(PrefsPrefix + "AudQ", audioQuality);

            SetBool("OptAnim", optimizeAnimators);
            SetBool("AnimCull", animatorCullWhenOffscreen);

            SetBool("OptMat", optimizeMaterials);
            SetBool("MatGPU", materialEnableGpuInstancing);

            SetBool("OptExtra", optimizeSceneExtras);
            SetBool("ExtraLight", disableLightsOnAvatar);
            SetBool("ExtraCam", disableCamerasOnAvatar);
            SetBool("ExtraPart", optimizeParticles);
            SaveUserProfiles();
        }

        private bool GetBool(string key, bool fallback)
        {
            string full = PrefsPrefix + key;
            return EditorPrefs.HasKey(full) ? EditorPrefs.GetBool(full) : fallback;
        }

        private int GetInt(string key, int fallback)
        {
            string full = PrefsPrefix + key;
            return EditorPrefs.HasKey(full) ? EditorPrefs.GetInt(full) : fallback;
        }

        private void SetBool(string key, bool value)
        {
            EditorPrefs.SetBool(PrefsPrefix + key, value);
        }

        private void SetInt(string key, int value)
        {
            EditorPrefs.SetInt(PrefsPrefix + key, value);
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            KaleidoVRCOptimizerUI.DrawHeader(headerIcon, VERSION);
            KaleidoVRCOptimizerUI.DrawTabs(this);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            KaleidoVRCOptimizerUI.DrawTabContent(this);
            EditorGUILayout.EndScrollView();
            KaleidoVRCOptimizerUI.DrawActions(this);
            KaleidoVRCOptimizerUI.DrawDiscordButton();
            KaleidoVRCOptimizerUI.DrawFooter();
            if (EditorGUI.EndChangeCheck())
            {
                inventorySignature = "";
                SaveEditorPreferences();
            }
        }

        public void RefreshInventoryIfNeeded()
        {
            string signature = BuildTargetSignature();
            if (signature == inventorySignature) return;
            inventorySignature = signature;
            KaleidoVRCOptimizerLogic.FillInventory(this);
        }

        private string BuildTargetSignature()
        {
            StringBuilder sb = new StringBuilder();
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    UnityEngine.Object obj = targets[i];
                    sb.Append(obj != null ? obj.GetInstanceID() : 0).Append(':');
                }
            }
            if (ignoreList != null)
            {
                sb.Append("|ign|");
                for (int i = 0; i < ignoreList.Count; i++)
                {
                    UnityEngine.Object obj = ignoreList[i];
                    sb.Append(obj != null ? obj.GetInstanceID() : 0).Append(':');
                }
            }
            return sb.ToString();
        }
    }

    [Serializable]
    public class KaleidoModelInventoryItem
    {
        public UnityEngine.Object asset;
        public string path;
        public string typeName;
        public string category;
        public string label;
    }

    [Serializable]
    public class KaleidoOptimizerReport
    {
        public int targetRoots;
        public int triangles;
        public int materialSlots;
        public int uniqueMaterials;
        public int skinnedMeshes;
        public int meshRenderers;
        public int uniqueTextures;
        public long textureBytesEstimate;
        public int physBones;
        public int contacts;
        public int constraints;
        public int blendShapes;
        public int bones;
        public int animators;
        public int lights;
        public int audioSources;
        public int particleSystems;
        public int physBoneColliders;
        public bool meshReadWriteDisabled;
        public string pcRank = "—";
        public string questRank = "—";
        public string summary = "Drop a VRChat avatar model and press Scan Performance.";
        public List<string> notes = new List<string>();
        public List<string> planned = new List<string>();
    }

    public static class KaleidoVRCOptimizerUI
    {
        private static readonly int[] TextureSizes = { 256, 512, 1024, 2048, 4096 };
        private static readonly string[] TextureSizeLabels = { "256", "512", "1024", "2048", "4096" };
        private static readonly string[] BuiltinNames = { "PC", "Quest", "Dual Platform" };

        private static GUIStyle miniWrap;

        private static GUIStyle MiniWrap()
        {
            if (miniWrap == null)
            {
                miniWrap = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            }
            return miniWrap;
        }

        public static void DrawHeader(Texture2D logo, string version)
        {
            GUIStyle centeredTitleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUIStyle centeredVersionStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Space(10); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (logo != null) { Rect logoRect = GUILayoutUtility.GetRect(320, 200, GUILayout.Width(320), GUILayout.Height(200)); GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit); }
            else { GUILayout.Label($"...Place your logo at {KaleidoVRCOptimizer.ICON_PATH}...", EditorStyles.miniLabel); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); GUILayout.Space(2);
            GUILayout.Label("KALEIDO VR MODEL OPTIMIZER", centeredTitleStyle); GUILayout.Label($"v{version}", centeredVersionStyle);
            EditorGUILayout.HelpBox("Avatar models only. Drop a VRChat avatar (VRCAvatarDescriptor) or a skinned character FBX. Worlds, folders, clothing dumps, and loose textures are rejected.", MessageType.Info);
        }

        public static void DrawTabs(KaleidoVRCOptimizer window)
        {
            DrawTabRow(window, new[] { "Setup", "Profiles", "Rank", "Textures" }, 0);
            DrawTabRow(window, new[] { "Meshes", "Scene", "Special" }, 4);
        }

        private static void DrawTabRow(KaleidoVRCOptimizer window, string[] names, int offset)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < names.Length; i++)
            {
                int tab = offset + i;
                bool on = window.tab == tab;
                if (GUILayout.Toggle(on, names[i], EditorStyles.miniButton, GUILayout.Height(24), GUILayout.ExpandWidth(true)))
                {
                    window.tab = tab;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawTabContent(KaleidoVRCOptimizer window)
        {
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 210f;
            switch (window.tab)
            {
                case 0: DrawSetupTab(window); break;
                case 1: DrawProfilesTab(window); break;
                case 2: DrawRankTab(window); break;
                case 3: DrawTexturesTab(window); break;
                case 4: DrawMeshesTab(window); break;
                case 5: DrawSceneTab(window); break;
                default: DrawSpecialTab(window); break;
            }
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        private static void DrawSetupTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("VRChat Avatar Models", EditorStyles.boldLabel);
            DrawWhy("Drop the avatar prefab, scene instance, or character FBX — same as the Organizer. The list below is only what that model uses. Nothing else in the project is scanned.");
            DrawObjectList(window.targets, "Drag & Drop VRChat Avatar Prefab, Instance, or FBX", true, true);

            window.RefreshInventoryIfNeeded();
            DrawModelContents(window);

            GUILayout.Space(8);
            GUILayout.Label("Ignore List", EditorStyles.boldLabel);
            DrawWhy("Anything here is left untouched, including its dependent textures and meshes.");
            DrawObjectList(window.ignoreList, "Drag & Drop Assets To Leave Untouched", false, false);

            GUILayout.Space(8);
            GUILayout.Label("Run Safety", EditorStyles.boldLabel);
            window.dryRun = DrawToggle(window.dryRun, "Dry Run (log only, no writes)", "Use this first. Lists every importer/renderer change without touching files.");
            window.writeLog = DrawToggle(window.writeLog, "Write Log File", "Saves a timestamped report under Logs/KaleidoVR/Optimizer.");
            window.applyToPrefabAssets = DrawToggle(window.applyToPrefabAssets, "Apply renderer changes to prefab assets", "Writes Scene-tab renderer edits onto the .prefab, not only the scene instance. Turn off to test on the instance first.");
        }

        private static void DrawProfilesTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("Profile Selector", EditorStyles.boldLabel);
            DrawWhy("Built-in profiles load recommended values. User profiles remember which options you ticked and which categories to apply.");

            int builtin = EditorGUILayout.Popup("Built-in Profile", window.builtinPresetIndex, BuiltinNames);
            if (builtin != window.builtinPresetIndex)
            {
                window.ApplyBuiltinPreset(builtin);
            }
            EditorGUILayout.LabelField(
                builtin == 0 ? "PC: 2K maps, HQ compression, shadows allowed, unlimited bone weights."
                : builtin == 1 ? "Quest: 1K body maps, 512 masks, ASTC, 4 bone weights, shadows off."
                : "Dual: 2K PC / 1K Quest on body maps. Masks one step smaller. Cross-platform default.",
                MiniWrap());

            GUILayout.Space(8);
            GUILayout.Label("User Profiles", EditorStyles.boldLabel);
            string[] userNames = GetUserProfileNames(window);
            int userIndex = window.selectedUserProfile + 1;
            int picked = EditorGUILayout.Popup("Saved Profile", userIndex, userNames);
            if (picked != userIndex)
            {
                window.selectedUserProfile = picked - 1;
                if (window.selectedUserProfile >= 0)
                {
                    window.LoadProfile(window.userProfiles.profiles[window.selectedUserProfile], true);
                    window.newProfileName = window.userProfiles.profiles[window.selectedUserProfile].name;
                }
            }

            window.newProfileName = EditorGUILayout.TextField("Profile Name", window.newProfileName);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Current As New"))
            {
                SaveNewProfile(window);
            }
            EditorGUI.BeginDisabledGroup(window.selectedUserProfile < 0);
            if (GUILayout.Button("Overwrite Selected"))
            {
                OverwriteSelectedProfile(window);
            }
            if (GUILayout.Button("Delete Selected"))
            {
                DeleteSelectedProfile(window);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Designer — categories this profile applies", EditorStyles.boldLabel);
            DrawWhy("Unchecked categories are left as they currently are when you load this profile. Special Use Case stays off unless you include it.");
            window.includeTextures = EditorGUILayout.ToggleLeft("Textures (sizes, compression, mip maps)", window.includeTextures);
            window.includeMeshes = EditorGUILayout.ToggleLeft("Meshes (read/write, weld, skin weights)", window.includeMeshes);
            window.includeRenderers = EditorGUILayout.ToggleLeft("Scene renderers (shadows, probes, offscreen)", window.includeRenderers);
            window.includeAudio = EditorGUILayout.ToggleLeft("Audio importers", window.includeAudio);
            window.includeAnimators = EditorGUILayout.ToggleLeft("Animator culling", window.includeAnimators);
            DrawSpecialUseCaseHeader();
            window.includeSpecial = EditorGUILayout.ToggleLeft("Special Use Case options", window.includeSpecial);
            DrawWhy("Includes mesh compression, force Humanoid, strip blend shapes, GPU instancing, disable cameras, force mono, crunch.");

            GUILayout.Space(6);
            if (GUILayout.Button("Load Profile Into Editor Tabs", GUILayout.Height(26)))
            {
                if (window.selectedUserProfile >= 0)
                    window.LoadProfile(window.userProfiles.profiles[window.selectedUserProfile], true);
                else
                    window.ApplyBuiltinPreset(window.builtinPresetIndex);
            }
        }

        private static string[] GetUserProfileNames(KaleidoVRCOptimizer window)
        {
            var names = new List<string> { "(none — using built-in)" };
            if (window.userProfiles != null && window.userProfiles.profiles != null)
            {
                foreach (KaleidoOptimizerProfile profile in window.userProfiles.profiles)
                {
                    names.Add(profile != null && !string.IsNullOrEmpty(profile.name) ? profile.name : "(unnamed)");
                }
            }
            return names.ToArray();
        }

        private static void SaveNewProfile(KaleidoVRCOptimizer window)
        {
            List<KaleidoOptimizerProfile> list = new List<KaleidoOptimizerProfile>();
            if (window.userProfiles != null && window.userProfiles.profiles != null) list.AddRange(window.userProfiles.profiles);
            KaleidoOptimizerProfile created = window.CaptureProfile(window.newProfileName);
            list.Add(created);
            window.userProfiles.profiles = list.ToArray();
            window.selectedUserProfile = list.Count - 1;
            window.SaveUserProfiles();
        }

        private static void OverwriteSelectedProfile(KaleidoVRCOptimizer window)
        {
            if (window.selectedUserProfile < 0 || window.userProfiles.profiles == null) return;
            if (window.selectedUserProfile >= window.userProfiles.profiles.Length) return;
            window.userProfiles.profiles[window.selectedUserProfile] = window.CaptureProfile(window.newProfileName);
            window.SaveUserProfiles();
        }

        private static void DeleteSelectedProfile(KaleidoVRCOptimizer window)
        {
            if (window.selectedUserProfile < 0 || window.userProfiles.profiles == null) return;
            List<KaleidoOptimizerProfile> list = new List<KaleidoOptimizerProfile>(window.userProfiles.profiles);
            if (window.selectedUserProfile >= list.Count) return;
            list.RemoveAt(window.selectedUserProfile);
            window.userProfiles.profiles = list.ToArray();
            window.selectedUserProfile = -1;
            window.SaveUserProfiles();
        }

        private static void DrawRankTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("VRChat Performance Snapshot", EditorStyles.boldLabel);
            DrawWhy("Avatar Performance Rank using VRChat's published PC and mobile (Quest/Android/iOS) limits. Worlds are not scanned. PhysBones/contacts need the VRChat SDK. This is an estimate, not the SDK's upload-time rank.");
            DrawStats(window);
        }

        private static void DrawTexturesTab(KaleidoVRCOptimizer window)
        {
            window.optimizeTextures = DrawToggle(window.optimizeTextures, "Process texture importers", "Master switch for this tab. Off = skip every texture even if rows below are ticked.");
            EditorGUI.BeginDisabledGroup(!window.optimizeTextures);

            GUILayout.Space(6);
            GUILayout.Label("Max Size By Texture Type", EditorStyles.boldLabel);
            DrawWhy("Unity downscales on import. VRChat counts the imported size toward texture memory. Uncheck a type to leave that type's resolution alone.");

            DrawTypeSizeRow(ref window.applyAlbedoSize, "Albedo / Diffuse / Main", "Body color maps. VRChat Android docs: stay at 1024 or below. Suggested PC 1024–2048, Quest 512–1024.", ref window.albedoPc, ref window.albedoQuest);
            DrawTypeSizeRow(ref window.applyNormalSize, "Normal", "Bump maps. Match albedo, or one step below if memory is tight. Suggested PC 1024–2048, Quest 512–1024.", ref window.normalPc, ref window.normalQuest);
            DrawTypeSizeRow(ref window.applyMaskSize, "Mask / Metallic / Rough / AO / ORM", "Packed masks are blur-tolerant. Suggested PC 512–1024, Quest 256–512.", ref window.maskPc, ref window.maskQuest);
            DrawTypeSizeRow(ref window.applyEmissionSize, "Emission", "Glow maps. Suggested PC 512–1024, Quest 256–512.", ref window.emissionPc, ref window.emissionQuest);
            DrawTypeSizeRow(ref window.applyMatcapSize, "Matcap / Ramp / Toon", "Tiny lookup textures. Suggested 256–512 on both platforms.", ref window.matcapPc, ref window.matcapQuest);
            DrawTypeSizeRow(ref window.applyOtherSize, "Other / Unclassified", "Anything that did not match a suffix. Suggested PC 512–1024, Quest 512.", ref window.otherPc, ref window.otherQuest);

            GUILayout.Space(8);
            GUILayout.Label("Importer Settings", EditorStyles.boldLabel);
            window.applyPcTexFormat = DrawToggle(window.applyPcTexFormat, "Set PC compression", "Compressed HQ / BC7 is the usual PC choice. Leave off to keep each texture's current PC format.");
            if (window.applyPcTexFormat) window.pcTexFormat = (KaleidoPcTexFormat)EditorGUILayout.EnumPopup("PC Format", window.pcTexFormat);
            window.applyAndroidTexFormat = DrawToggle(window.applyAndroidTexFormat, "Set Android / Quest format", "ASTC 6x6 is the usual Quest balance. 4x4 is sharper/heavier, 8x8 is cheaper/blurrier.");
            if (window.applyAndroidTexFormat) window.androidTexFormat = (KaleidoAndroidTexFormat)EditorGUILayout.EnumPopup("Android Format", window.androidTexFormat);

            window.textureDisableReadWrite = DrawToggle(window.textureDisableReadWrite, "Disable Read / Write", "Saves RAM. Turn off only if a script or editor tool reads pixels from the texture.");
            window.textureApplyMipmaps = DrawToggle(window.textureApplyMipmaps, "Set mip maps", "Avatars in 3D should generate mip maps. Uncheck to leave each texture as-is.");
            if (window.textureApplyMipmaps) window.textureEnableMipmaps = EditorGUILayout.Toggle("Generate Mip Maps", window.textureEnableMipmaps);
            window.textureDisableStreamingMipmaps = DrawToggle(window.textureDisableStreamingMipmaps, "Disable streaming mip maps", "Avatar textures should stay resident. Streaming is for large world textures.");
            window.textureDisableCrunch = DrawToggle(window.textureDisableCrunch, "Disable crunch compression", "Crunch does not reduce VRChat texture memory (the rank metric). It only shrinks download size and costs CPU. Keep it off.");
            window.textureApplyAniso = DrawToggle(window.textureApplyAniso, "Set anisotropic filtering", "1 is enough for avatars. Higher values cost GPU for little gain up close.");
            if (window.textureApplyAniso) window.textureAniso = EditorGUILayout.IntSlider("Aniso Level", window.textureAniso, 0, 16);
            window.autoDetectNormalMaps = DrawToggle(window.autoDetectNormalMaps, "Detect normal maps by name", "Sets Texture Type to Normal Map when the file looks like _n / _norm / _normal. Prevents sRGB lighting errors.");
            window.higherQualityNormalMaps = DrawToggle(window.higherQualityNormalMaps, "Higher quality normals (BC5 / ASTC)", "Uses BC5 on PC and a sharper ASTC block on Quest for detected normals.");
            window.autoLinearMaskMaps = DrawToggle(window.autoLinearMaskMaps, "Linear color for mask maps", "Turns sRGB off on metallic/rough/AO/ORM so packed masks do not get gamma-crushed.");
            window.alphaIsTransparencyOnAlbedo = DrawToggle(window.alphaIsTransparencyOnAlbedo, "Alpha Is Transparency on albedo", "Only if the albedo has an alpha channel (cutout/transparent clothing).");
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawTypeSizeRow(ref bool apply, string title, string why, ref int pc, ref int quest)
        {
            apply = EditorGUILayout.ToggleLeft(title, apply);
            DrawWhy(why);
            EditorGUI.BeginDisabledGroup(!apply);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18);
            GUILayout.Label("PC", GUILayout.Width(36));
            pc = SizePopup(pc);
            GUILayout.Space(16);
            GUILayout.Label("Quest", GUILayout.Width(36));
            quest = SizePopup(quest);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(4);
        }

        private static void DrawMeshesTab(KaleidoVRCOptimizer window)
        {
            window.optimizeMeshes = DrawToggle(window.optimizeMeshes, "Process model importers", "Master switch for FBX/GLB import settings. Off = skip every model.");
            EditorGUI.BeginDisabledGroup(!window.optimizeMeshes);
            window.meshEnableReadWrite = DrawToggle(window.meshEnableReadWrite, "Enable mesh Read / Write", "Required by VRChat. If any mesh on the avatar has Read/Write off, the SDK ranks the avatar Very Poor and blocks upload.");
            window.meshOptimizePolygons = DrawToggle(window.meshOptimizePolygons, "Optimize mesh polygons", "Reorders triangles for the GPU. Safe for avatars. Does not reduce triangle count.");
            window.meshOptimizeVertices = DrawToggle(window.meshOptimizeVertices, "Optimize mesh vertices", "Reorders vertices for cache locality. Safe. Does not decimate.");
            window.meshWeldVertices = DrawToggle(window.meshWeldVertices, "Weld vertices", "Merges duplicates on import. Usually wanted. Uncheck if a mesh relies on split verts for UV islands/sharp edges you already authored.");
            window.meshKeepBlendShapes = DrawToggle(window.meshKeepBlendShapes, "Keep blend shapes", "Forces blend shapes on. Visemes and face shapes need this. Does not delete unused shapes.");
            window.meshDisableQuads = DrawToggle(window.meshDisableQuads, "Keep quads off", "Avatars should triangulate. Leave off if a tool needs quads.");
            window.meshDisableLightmapUVs = DrawToggle(window.meshDisableLightmapUVs, "Disable lightmap UVs", "Avatars are not lightmapped. Turns off extra UV generation and import time.");
            window.meshDisableImportLightsCameras = DrawToggle(window.meshDisableImportLightsCameras, "Skip embedded lights / cameras", "FBX extras become extra components. Avatars should not import them.");
            window.meshOptimizeAnimation = DrawToggle(window.meshOptimizeAnimation, "Optimal animation compression", "Compresses clips on the model importer. Use for character FBX animations.");
            window.applySkinWeights = DrawToggle(window.applySkinWeights, "Set skin weights", "Quest wants 4 bones per vertex. Unlimited is PC-quality and costs more on Android.");
            if (window.applySkinWeights) window.skinWeights = (KaleidoSkinWeightChoice)EditorGUILayout.EnumPopup("Skin Weights", window.skinWeights);
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawSceneTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("Renderers", EditorStyles.boldLabel);
            window.optimizeRenderers = DrawToggle(window.optimizeRenderers, "Process skinned / mesh renderers", "Master switch for this section.");
            EditorGUI.BeginDisabledGroup(!window.optimizeRenderers);
            window.rendererDisableUpdateWhenOffscreen = DrawToggle(window.rendererDisableUpdateWhenOffscreen, "Disable Update When Offscreen", "Big CPU win. Unity keeps animating skinned meshes that are culled if this stays on. Turn off only for meshes that must stay posed while hidden.");
            window.rendererDisableShadows = DrawToggle(window.rendererDisableShadows, "Disable shadow casting", "Quest default. On PC, uncheck if you want the avatar to cast shadows.");
            window.rendererDisableReceiveShadows = DrawToggle(window.rendererDisableReceiveShadows, "Disable receive shadows", "Avatars often skip receiving world shadows. Uncheck if you want contact shadows on the body.");
            window.rendererDisableProbes = DrawToggle(window.rendererDisableProbes, "Disable light / reflection probes", "Stops per-renderer probe sampling. Worlds still light the avatar through VRChat's lighting; this cuts extra probe work.");
            window.rendererDisableMotionVectors = DrawToggle(window.rendererDisableMotionVectors, "Disable motion vectors", "VRChat does not use camera motion blur on avatars. Safe to force off.");
            window.rendererForceBone4 = DrawToggle(window.rendererForceBone4, "Force 4 bone quality on skinned meshes", "Caps GPU skinning at 4 influences. Match Quest. Uncheck on a PC-only avatar if you authored more weights.");
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);
            GUILayout.Label("Animators", EditorStyles.boldLabel);
            window.optimizeAnimators = DrawToggle(window.optimizeAnimators, "Process animator components", "Master switch for animator culling.");
            EditorGUI.BeginDisabledGroup(!window.optimizeAnimators);
            window.animatorCullWhenOffscreen = DrawToggle(window.animatorCullWhenOffscreen, "Cull update when offscreen", "Stops animator graph updates while the avatar is not visible. Keep Always Animate only if hidden objects must keep ticking (some PhysBone setups).");
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);
            GUILayout.Label("Audio", EditorStyles.boldLabel);
            window.optimizeAudio = DrawToggle(window.optimizeAudio, "Process audio importers", "Master switch for clips used by the avatar.");
            EditorGUI.BeginDisabledGroup(!window.optimizeAudio);
            window.audioLoadInBackground = DrawToggle(window.audioLoadInBackground, "Load in background", "Avoids hitches when a clip first plays.");
            window.audioApplyVorbis = DrawToggle(window.audioApplyVorbis, "Vorbis, compressed in memory", "Standard for short avatar SFX. Do not use on huge music beds.");
            if (window.audioApplyVorbis) window.audioQuality = EditorGUILayout.Slider("Vorbis Quality", window.audioQuality, 0.01f, 1f);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);
            GUILayout.Label("Particles", EditorStyles.boldLabel);
            window.optimizeParticles = DrawToggle(window.optimizeParticles, "Strip particle shadows / motion vectors", "Particles on avatars rarely need shadows. Uncheck if a VFX specifically uses them.");
            window.disableLightsOnAvatar = DrawToggle(window.disableLightsOnAvatar, "Disable realtime lights on the avatar", "VRChat PC Excellent allows 0 lights. Android/Quest strips avatar lights entirely.");
            window.optimizeMaterials = DrawToggle(window.optimizeMaterials, "Set GPU instancing on materials", "VRChat Android docs: enable GPU instancing on materials. Has little effect on skinned meshes but is the recommended default for Quest.");
            if (window.optimizeMaterials) window.materialEnableGpuInstancing = EditorGUILayout.Toggle("Enable GPU Instancing", window.materialEnableGpuInstancing);
            window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.disableCamerasOnAvatar;
        }

        private static void DrawSpecialTab(KaleidoVRCOptimizer window)
        {
            DrawSpecialUseCaseHeader();
            EditorGUILayout.HelpBox("These can break visemes, custom bounds, lighting, or audio. Leave them off unless you know you need them.", MessageType.Warning);

            window.applyMeshCompression = DrawToggle(window.applyMeshCompression, "Apply mesh compression", "Unity's mesh compressor distorts blend shapes. Only for static props with no visemes.");
            if (window.applyMeshCompression) window.meshCompression = (KaleidoMeshCompressionChoice)EditorGUILayout.EnumPopup("Compression Level", window.meshCompression);

            window.meshForceHumanoid = DrawToggle(window.meshForceHumanoid, "Force Humanoid rig", "Rewrites the FBX avatar to Humanoid. Can destroy a working Generic/Humanoid mapping. Prefer the Rig tab in the importer.");
            window.meshStripBlendShapes = DrawToggle(window.meshStripBlendShapes, "Disable blend shape import", "Turns blend shapes off on the model. Breaks visemes and face tracking. Only for meshes that truly have none you need.");
            window.rendererRecalculateBounds = DrawToggle(window.rendererRecalculateBounds, "Recalculate skinned bounds", "Resets local bounds from the mesh AABB. Breaks meshes that used oversized bounds so toggled parts stay visible.");
            window.disableCamerasOnAvatar = DrawToggle(window.disableCamerasOnAvatar, "Disable cameras on the avatar", "Android/Quest disable avatar cameras. Can kill preview cameras, mirrors, or VRC tools parented under the avatar.");
            window.audioForceToMono = DrawToggle(window.audioForceToMono, "Force audio to mono", "Halves clip size but collapses stereo / spatial beds. Only for true mono SFX. Audio sources are disabled on Android avatars.");
            window.textureEnableCrunch = DrawToggle(window.textureEnableCrunch, "Enable crunch compression", "Does not lower VRChat texture memory rank. Only download size. VRChat says the package should fit limits without Crunch.");
            window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.disableCamerasOnAvatar;
        }

        public static void DrawActions(KaleidoVRCOptimizer window)
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Performance", GUILayout.Height(32)))
            {
                window.lastReport = KaleidoVRCOptimizerLogic.Scan(window, false);
                window.tab = 2;
            }
            if (GUILayout.Button(window.dryRun ? "Dry Run Optimizations" : "Apply Optimizations", GUILayout.Height(32)))
            {
                if (!window.dryRun)
                {
                    if (!EditorUtility.DisplayDialog(
                        "KaleidoVR VRChat Model Optimizer",
                        "This reimports textures/meshes and edits renderer settings on the selected VRChat avatar models only. Worlds are not touched. Continue?",
                        "Apply",
                        "Cancel"))
                    {
                        return;
                    }
                }
                window.lastReport = KaleidoVRCOptimizerLogic.Scan(window, true);
                window.tab = 2;
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSpecialUseCaseHeader()
        {
            GUILayout.Space(4);
            Color previous = GUI.contentColor;
            GUI.contentColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.78f, 0.28f)
                : new Color(0.55f, 0.32f, 0f);
            GUILayout.Label("Warning (Special Use Case)", EditorStyles.miniBoldLabel);
            GUI.contentColor = previous;
        }

        private static bool DrawToggle(bool value, string title, string why)
        {
            value = EditorGUILayout.ToggleLeft(title, value);
            DrawWhy(why);
            GUILayout.Space(3);
            return value;
        }

        private static void DrawWhy(string why)
        {
            EditorGUILayout.LabelField(why, MiniWrap());
        }

        private static int SizePopup(int current)
        {
            int index = 2;
            for (int i = 0; i < TextureSizes.Length; i++)
            {
                if (TextureSizes[i] == current) index = i;
            }
            int picked = EditorGUILayout.Popup(index, TextureSizeLabels, GUILayout.Width(70));
            return TextureSizes[picked];
        }

        private static void DrawStats(KaleidoVRCOptimizer window)
        {
            KaleidoOptimizerReport report = window.lastReport;
            if (report == null)
            {
                EditorGUILayout.HelpBox("No scan yet. Drop a VRChat avatar model on Setup and press Scan Performance.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(report.summary, MessageType.None);
            EditorGUILayout.LabelField("PC Rank", report.pcRank);
            EditorGUILayout.LabelField("Quest Rank", report.questRank);
            if (report.meshReadWriteDisabled)
            {
                EditorGUILayout.HelpBox("Mesh Read/Write is disabled on at least one mesh. VRChat ranks that avatar Very Poor until Read/Write is enabled.", MessageType.Error);
            }
            EditorGUILayout.LabelField("Triangles", report.triangles.ToString("N0"));
            EditorGUILayout.LabelField("Material Slots", report.materialSlots.ToString("N0"));
            EditorGUILayout.LabelField("Unique Materials", report.uniqueMaterials.ToString("N0"));
            EditorGUILayout.LabelField("Skinned Meshes", report.skinnedMeshes.ToString("N0"));
            EditorGUILayout.LabelField("Basic Meshes", report.meshRenderers.ToString("N0"));
            EditorGUILayout.LabelField("Unique Textures", report.uniqueTextures.ToString("N0"));
            EditorGUILayout.LabelField("Texture Memory (est.)", KaleidoVRCOptimizerHelpers.FormatBytes(report.textureBytesEstimate));
            EditorGUILayout.LabelField("Blend Shapes", report.blendShapes.ToString("N0"));
            EditorGUILayout.LabelField("Bones (max on one mesh)", report.bones.ToString("N0"));
            EditorGUILayout.LabelField("Animators", report.animators.ToString("N0"));
            EditorGUILayout.LabelField("Lights", report.lights.ToString("N0"));
            EditorGUILayout.LabelField("Audio Sources", report.audioSources.ToString("N0"));
            EditorGUILayout.LabelField("Particle Systems", report.particleSystems.ToString("N0"));
            EditorGUILayout.LabelField("PhysBones", report.physBones.ToString("N0"));
            EditorGUILayout.LabelField("PhysBone Colliders", report.physBoneColliders.ToString("N0"));
            EditorGUILayout.LabelField("Contacts", report.contacts.ToString("N0"));
            EditorGUILayout.LabelField("Constraints", report.constraints.ToString("N0"));

            if (report.notes.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Hints", EditorStyles.miniBoldLabel);
                StringBuilder hints = new StringBuilder();
                int shown = Math.Min(report.notes.Count, 12);
                for (int i = 0; i < shown; i++) hints.AppendLine("• " + report.notes[i]);
                if (report.notes.Count > shown) hints.AppendLine("• … " + (report.notes.Count - shown) + " more in the log");
                EditorGUILayout.HelpBox(hints.ToString().TrimEnd(), MessageType.None);
            }

            if (report.planned.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Last planned / applied changes", EditorStyles.miniBoldLabel);
                StringBuilder planned = new StringBuilder();
                int shown = Math.Min(report.planned.Count, 20);
                for (int i = 0; i < shown; i++) planned.AppendLine("• " + report.planned[i]);
                if (report.planned.Count > shown) planned.AppendLine("• … " + (report.planned.Count - shown) + " more in the log");
                EditorGUILayout.HelpBox(planned.ToString().TrimEnd(), MessageType.None);
            }
        }

        private static void DrawModelContents(KaleidoVRCOptimizer window)
        {
            GUILayout.Space(8);
            GUILayout.Label("Contents Of Selected Model", EditorStyles.boldLabel);
            DrawWhy("Same idea as the Organizer: only assets this avatar actually uses. Nothing else in the project is listed or optimized.");

            if (window.inventory == null || window.inventory.Count == 0)
            {
                EditorGUILayout.HelpBox("Drop an avatar above. Its meshes, materials, textures, clips, and menus will list here.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(SummarizeInventory(window.inventory), MiniWrap());
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                window.inventorySignature = "";
                window.RefreshInventoryIfNeeded();
            }
            EditorGUILayout.EndHorizontal();

            window.inventoryScroll = EditorGUILayout.BeginScrollView(window.inventoryScroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(280));
            string lastCategory = null;
            for (int i = 0; i < window.inventory.Count; i++)
            {
                KaleidoModelInventoryItem item = window.inventory[i];
                if (item == null) continue;
                if (item.category != lastCategory)
                {
                    lastCategory = item.category;
                    GUILayout.Space(4);
                    GUILayout.Label(lastCategory, EditorStyles.miniBoldLabel);
                }

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rowRect, i % 2 == 0 ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.23f, 0.23f, 0.23f, 1f));
                GUILayout.Space(5);
                GUIContent icon = new GUIContent(" " + (string.IsNullOrEmpty(item.label) ? item.typeName : item.label), GetInventoryIcon(item.typeName));
                GUILayout.Label(icon, GUILayout.Width(220), GUILayout.Height(18));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(item.asset, typeof(UnityEngine.Object), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private static string SummarizeInventory(List<KaleidoModelInventoryItem> items)
        {
            int tex = 0, mat = 0, mesh = 0, anim = 0, audio = 0, vrc = 0, model = 0;
            for (int i = 0; i < items.Count; i++)
            {
                switch (items[i].category)
                {
                    case "Textures": tex++; break;
                    case "Materials": mat++; break;
                    case "Meshes": mesh++; break;
                    case "Animations": anim++; break;
                    case "Audio": audio++; break;
                    case "VRChat": vrc++; break;
                    case "Model": model++; break;
                }
            }
            return model + " model, " + mesh + " meshes, " + mat + " materials, " + tex + " textures, " + anim + " anim, " + audio + " audio, " + vrc + " VRC";
        }

        private static Texture GetInventoryIcon(string typeName)
        {
            string iconName = typeName switch
            {
                "GameObject" => "Prefab Icon",
                "Texture2D" => "Texture2D Icon",
                "Cubemap" => "Cubemap Icon",
                "Material" => "Material Icon",
                "Mesh" => "Mesh Icon",
                "AudioClip" => "AudioClip Icon",
                "AnimationClip" => "AnimationClip Icon",
                "AnimatorController" => "AnimatorController Icon",
                "AnimatorOverrideController" => "AnimatorOverrideController Icon",
                "BlendTree" => "BlendTree Icon",
                "AvatarMask" => "AvatarMask Icon",
                "Avatar" => "Avatar Icon",
                _ => "DefaultAsset Icon"
            };
            try
            {
                GUIContent content = EditorGUIUtility.IconContent(iconName);
                return content != null ? content.image : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void DrawObjectList(List<UnityEngine.Object> list, string dropLabel, bool striped, bool avatarModelsOnly)
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, dropLabel, EditorStyles.helpBox);
            KaleidoVRCOptimizerHelpers.HandleDragAndDrop(dropArea, list, avatarModelsOnly);

            for (int i = 0; i < list.Count; i++)
            {
                if (avatarModelsOnly && list[i] != null)
                {
                    GameObject resolved;
                    string reason;
                    if (!KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(list[i], out resolved, out reason))
                    {
                        EditorGUILayout.HelpBox(reason, MessageType.Warning);
                    }
                }

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                if (striped && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rowRect, i % 2 == 0 ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.23f, 0.23f, 0.23f, 1f));
                }
                GUILayout.Space(5);
                list[i] = EditorGUILayout.ObjectField(list[i], avatarModelsOnly ? typeof(GameObject) : typeof(UnityEngine.Object), true, GUILayout.ExpandWidth(true));

                Event evt = Event.current;
                if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && rowRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            list[i] = DragAndDrop.objectReferences[0];
                            GUI.changed = true;
                        }
                    }
                    evt.Use();
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    list.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("+", GUILayout.Width(35))) list.Add(null);
            if (GUILayout.Button("-", GUILayout.Width(35)) && list.Count > 0) list.RemoveAt(list.Count - 1);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawDiscordButton()
        {
            GUILayout.Space(2);
            if (GUILayout.Button("💬 Join the Discord Server", GUILayout.Height(24)))
            {
                Application.OpenURL("https://discord.com/invite/cRsufJssTA");
            }
        }

        public static void DrawFooter()
        {
            GUILayout.Space(5);
            GUILayout.Label("Created and maintained by KaleidoVR", EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("Visit kalivr.com", EditorStyles.centeredGreyMiniLabel))
            {
                Application.OpenURL("https://kalivr.com");
            }
            GUILayout.Space(8);
        }
    }

    public static class KaleidoVRCOptimizerHelpers
    {
        public static void HandleDragAndDrop(Rect dropArea, List<UnityEngine.Object> targetList, bool avatarModelsOnly)
        {
            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object dragged in DragAndDrop.objectReferences)
                    {
                        if (dragged == null) continue;
                        UnityEngine.Object toAdd = dragged;
                        if (avatarModelsOnly)
                        {
                            GameObject resolved;
                            string reason;
                            if (!TryResolveVrchatAvatarModel(dragged, out resolved, out reason))
                            {
                                Debug.LogWarning("[KaleidoVR] VRChat Model Optimizer: " + reason);
                                continue;
                            }
                            if (resolved != null) toAdd = resolved;
                        }
                        if (!targetList.Contains(toAdd)) targetList.Add(toAdd);
                    }
                    GUI.changed = true;
                }
                Event.current.Use();
            }
        }

        public static bool TryResolveVrchatAvatarModel(UnityEngine.Object obj, out GameObject avatarRoot, out string reason)
        {
            avatarRoot = null;
            reason = null;
            if (obj == null)
            {
                reason = "Empty slot.";
                return false;
            }

            if (obj is Texture || obj is Material || obj is AudioClip || obj is Shader || obj is MonoScript
                || obj is AnimationClip || obj is RuntimeAnimatorController || obj is Mesh || obj is SceneAsset)
            {
                reason = "Not a VRChat avatar model. Drop the avatar prefab, a scene instance with VRCAvatarDescriptor, or the skinned character FBX.";
                return false;
            }

            string path = NormalizeAssetPath(AssetDatabase.GetAssetPath(obj));
            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
            {
                reason = "Folders are not allowed. Drop one VRChat avatar prefab or character FBX.";
                return false;
            }

            if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Scenes are not allowed. This tool does not optimize VRChat worlds.";
                return false;
            }

            GameObject go = obj as GameObject;
            if (obj is Component component) go = component.gameObject;
            if (go == null && !string.IsNullOrEmpty(path)) go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
            {
                reason = "Could not resolve a GameObject. Drop a VRChat avatar prefab, instance, or FBX.";
                return false;
            }

            if (IsWorldRoot(go))
            {
                reason = "This is a VRChat world (Scene Descriptor), not an avatar model.";
                return false;
            }

            Type avatarDescType = FindTypeByFullName("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            GameObject descriptorRoot = FindAvatarDescriptorRoot(go, avatarDescType);
            if (descriptorRoot != null)
            {
                avatarRoot = descriptorRoot;
                return true;
            }

            if (IsSkinnedCharacterModelAsset(go, path))
            {
                avatarRoot = go;
                return true;
            }

            if (avatarDescType != null)
            {
                reason = "No VRCAvatarDescriptor on this object. Add the VRChat Avatar Descriptor, or drop the character FBX. Worlds, props, and clothing folders are skipped.";
                return false;
            }

            if (LooksLikeAvatarHierarchy(go))
            {
                avatarRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
                return true;
            }

            reason = "Not a VRChat avatar model. Needs a VRCAvatarDescriptor (SDK3 Avatars) or a skinned character mesh.";
            return false;
        }

        private static bool IsWorldRoot(GameObject go)
        {
            Type worldType = FindTypeByFullName("VRC.SDK3.Components.VRCSceneDescriptor");
            if (worldType == null) worldType = FindTypeByFullName("VRCSDK2.VRC_SceneDescriptor");
            if (worldType == null || go == null) return false;
            return go.GetComponent(worldType) != null;
        }

        private static GameObject FindAvatarDescriptorRoot(GameObject go, Type avatarDescType)
        {
            if (go == null || avatarDescType == null) return null;

            Transform current = go.transform;
            while (current != null)
            {
                Component desc = current.GetComponent(avatarDescType);
                if (desc != null) return current.gameObject;
                current = current.parent;
            }

            Component nested = go.GetComponentInChildren(avatarDescType, true);
            return nested != null ? nested.gameObject : null;
        }

        private static bool IsSkinnedCharacterModelAsset(GameObject go, string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string lower = path.ToLowerInvariant();
            bool isModelFile = lower.EndsWith(".fbx") || lower.EndsWith(".blend") || lower.EndsWith(".dae")
                || lower.EndsWith(".vrm") || lower.EndsWith(".glb") || lower.EndsWith(".gltf") || lower.EndsWith(".obj");
            if (!isModelFile) return false;
            if (go.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) return false;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && importer.animationType == ModelImporterAnimationType.Human) return true;
            return go.GetComponentInChildren<Animator>(true) != null || go.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
        }

        private static bool LooksLikeAvatarHierarchy(GameObject go)
        {
            if (go == null) return false;
            return go.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                && go.GetComponentInChildren<Animator>(true) != null;
        }

        public static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return path.Replace("\\", "/").Trim().TrimEnd('/');
        }

        public static bool ShouldIgnoreAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            string lower = path.ToLowerInvariant();
            return lower.EndsWith(".cs") || lower.EndsWith(".dll") || lower.EndsWith(".asmdef") || lower.EndsWith(".pdb")
                   || lower.EndsWith(".meta") || lower.EndsWith(".unity") || lower.EndsWith(".lighting")
                   || lower.StartsWith("packages/")
                   || lower == "assets/editor" || lower.StartsWith("assets/editor/")
                   || lower.StartsWith("library/") || lower.StartsWith("resources/unity_builtin_extra")
                   || lower.Contains("unity default resources") || lower.Contains("unity_builtin_extra");
        }

        public static string GetProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("0.0") + " KB";
            double mb = kb / 1024.0;
            return mb.ToString("0.0") + " MB";
        }

        public static bool LooksLikeNormalMap(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            return name.EndsWith("_n") || name.EndsWith("_norm") || name.EndsWith("_nrm") || name.EndsWith("_nor")
                || name.Contains("_normal") || name.EndsWith("normal") || name.Contains("_norm_");
        }

        public static bool LooksLikeMaskMap(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            return name.Contains("_met") || name.Contains("metallic") || name.Contains("_rough") || name.Contains("roughness")
                || name.Contains("_ao") || name.Contains("occlusion") || name.Contains("_orm") || name.Contains("_rmo")
                || name.Contains("_mask") || name.Contains("_spec") || name.Contains("metallicgloss") || name.Contains("_packed");
        }

        public static bool LooksLikeAlbedo(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            return name.Contains("_diff") || name.Contains("diffuse") || name.Contains("_albedo") || name.Contains("basecolor")
                || name.Contains("_base") || name.Contains("_col") || name.Contains("_color") || name.Contains("_main")
                || name.EndsWith("_d");
        }

        public static bool LooksLikeEmission(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            return name.Contains("_emis") || name.Contains("emission") || name.Contains("_glow") || name.EndsWith("_e") || name.Contains("_emit");
        }

        public static bool LooksLikeMatcap(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            return name.Contains("matcap") || name.Contains("_ramp") || name.Contains("toonramp") || name.Contains("_cap");
        }

        public static KaleidoTextureKind ClassifyTexture(string assetPath, TextureImporter importer)
        {
            if (importer != null && importer.textureType == TextureImporterType.NormalMap) return KaleidoTextureKind.Normal;
            if (LooksLikeNormalMap(assetPath)) return KaleidoTextureKind.Normal;
            if (LooksLikeEmission(assetPath)) return KaleidoTextureKind.Emission;
            if (LooksLikeMatcap(assetPath)) return KaleidoTextureKind.Matcap;
            if (LooksLikeMaskMap(assetPath)) return KaleidoTextureKind.Mask;
            if (LooksLikeAlbedo(assetPath)) return KaleidoTextureKind.Albedo;
            return KaleidoTextureKind.Other;
        }

        public static Type FindTypeByFullName(string fullName)
        {
            Type direct = Type.GetType(fullName + ", VRC.SDK3A") ?? Type.GetType(fullName);
            if (direct != null) return direct;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type found = assembly.GetType(fullName, false);
                    if (found != null) return found;
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        public static int CountComponents(GameObject root, Type type)
        {
            if (root == null || type == null) return 0;
            Component[] found = root.GetComponentsInChildren(type, true);
            return found != null ? found.Length : 0;
        }

        public static bool IsAvatarScopedAsset(UnityEngine.Object asset)
        {
            if (asset == null) return false;
            if (asset is Shader || asset is MonoScript || asset is SceneAsset)
                return false;
            if (asset is ComputeShader || asset is Texture3D)
                return false;

            string path = NormalizeAssetPath(AssetDatabase.GetAssetPath(asset));
            if (ShouldIgnoreAsset(path)) return false;

            if (asset is Texture || asset is Material || asset is Mesh || asset is AudioClip
                || asset is AnimationClip || asset is AvatarMask || asset is Avatar
                || asset is RuntimeAnimatorController)
                return true;

            string typeName = asset.GetType().Name;
            if (typeName == "BlendTree" || typeName == "AnimatorOverrideController")
                return true;
            if (typeName.IndexOf("VRCExpression", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (typeName.IndexOf("VRCPhysBone", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (asset is GameObject)
            {
                if (string.IsNullOrEmpty(path)) return false;
                if (!AssetDatabase.IsMainAsset(asset)) return false;
                string lower = path.ToLowerInvariant();
                return lower.EndsWith(".prefab") || lower.EndsWith(".fbx") || lower.EndsWith(".blend")
                    || lower.EndsWith(".vrm") || lower.EndsWith(".glb") || lower.EndsWith(".gltf") || lower.EndsWith(".dae");
            }

            return false;
        }

        public static string CategorizeInventoryAsset(UnityEngine.Object asset)
        {
            if (asset is Texture) return "Textures";
            if (asset is Material) return "Materials";
            if (asset is Mesh) return "Meshes";
            if (asset is AudioClip) return "Audio";
            if (asset is AnimationClip || asset is RuntimeAnimatorController || asset is AvatarMask)
                return "Animations";
            string typeName = asset != null ? asset.GetType().Name : "";
            if (typeName == "BlendTree" || typeName == "AnimatorOverrideController") return "Animations";
            if (typeName.IndexOf("VRCExpression", StringComparison.OrdinalIgnoreCase) >= 0) return "VRChat";
            if (asset is GameObject || asset is Avatar) return "Model";
            return "Other";
        }

        public static int InventoryCategoryOrder(string category)
        {
            switch (category)
            {
                case "Model": return 0;
                case "Meshes": return 1;
                case "Materials": return 2;
                case "Textures": return 3;
                case "Animations": return 4;
                case "Audio": return 5;
                case "VRChat": return 6;
                default: return 7;
            }
        }

        public static int CountUnityConstraints(GameObject root)
        {
            if (root == null) return 0;
            int count = root.GetComponentsInChildren<IConstraint>(true).Length;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                string fullName = component.GetType().FullName ?? "";
                if (fullName.IndexOf("VRC", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (fullName.IndexOf("Constraint", StringComparison.OrdinalIgnoreCase) < 0) continue;
                count++;
            }
            return count;
        }
    }

    public static class KaleidoVRCOptimizerLogic
    {
        public static KaleidoOptimizerReport Scan(KaleidoVRCOptimizer window, bool apply)
        {
            KaleidoOptimizerReport report = new KaleidoOptimizerReport();
            List<string> logEntries = new List<string>
            {
                "KaleidoVR VRChat Model Optimizer " + KaleidoVRCOptimizer.VERSION + " at " + DateTime.Now,
                apply ? (window.dryRun ? "Mode: Dry Run" : "Mode: Apply") : "Mode: Scan only"
            };

            try
            {
                HashSet<string> ignorePaths = BuildIgnorePaths(window.ignoreList);
                List<GameObject> roots = new List<GameObject>();
                HashSet<string> assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                CollectFromTargets(window.targets, ignorePaths, roots, assetPaths, logEntries, report);
                if (roots.Count == 0 && assetPaths.Count == 0)
                {
                    report.summary = "No VRChat avatar models to process. Drop a prefab/instance with VRCAvatarDescriptor, or a skinned character FBX. Worlds and folders are ignored.";
                    logEntries.Add(report.summary);
                    if (apply)
                    {
                        EditorUtility.DisplayDialog("KaleidoVR VRChat Model Optimizer", report.summary, "OK");
                    }
                    if (window.writeLog)
                    {
                        try
                        {
                            string logDir = Path.Combine(KaleidoVRCOptimizerHelpers.GetProjectRootPath(), "Logs", "KaleidoVR", "Optimizer");
                            Directory.CreateDirectory(logDir);
                            string logFile = Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_optimizer_log.txt");
                            File.WriteAllLines(logFile, logEntries.ToArray());
                        }
                        catch (Exception)
                        {
                        }
                    }
                    return report;
                }

                report.targetRoots = roots.Count;
                PopulateInventory(window, assetPaths);

                GatherStats(roots, assetPaths, report, logEntries);
                BuildHints(report);

                if (apply)
                {
                    ApplyOptimizations(window, roots, assetPaths, ignorePaths, report, logEntries);
                }

                report.summary = apply
                    ? (window.dryRun
                        ? "Dry run complete. " + report.planned.Count + " change(s) would be applied."
                        : "Applied " + report.planned.Count + " change(s). Reimport may take a moment.")
                    : "Scan complete for " + roots.Count + " VRChat avatar model(s) and " + assetPaths.Count + " related asset(s).";
            }
            catch (Exception ex)
            {
                report.summary = "Optimizer failed: " + ex.Message;
                logEntries.Add(ex.ToString());
                Debug.LogException(ex);
            }

            if (window.writeLog)
            {
                try
                {
                    string logDir = Path.Combine(KaleidoVRCOptimizerHelpers.GetProjectRootPath(), "Logs", "KaleidoVR", "Optimizer");
                    Directory.CreateDirectory(logDir);
                    string logFile = Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_optimizer_log.txt");
                    File.WriteAllLines(logFile, logEntries.ToArray());
                    logEntries.Add("Wrote log: " + logFile);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[KaleidoVR] Could not write optimizer log: " + ex.Message);
                }
            }

            if (apply)
            {
                EditorUtility.DisplayDialog(
                    "KaleidoVR VRChat Model Optimizer",
                    report.summary + "\nPC: " + report.pcRank + "\nQuest: " + report.questRank,
                    "OK");
            }

            return report;
        }

        private static HashSet<string> BuildIgnorePaths(List<UnityEngine.Object> ignoreList)
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (ignoreList == null) return paths;
            List<UnityEngine.Object> collected = new List<UnityEngine.Object>();
            foreach (UnityEngine.Object obj in ignoreList)
            {
                if (obj == null) continue;
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path)) paths.Add(KaleidoVRCOptimizerHelpers.NormalizeAssetPath(path));
                collected.Add(obj);
            }
            if (collected.Count > 0)
            {
                foreach (UnityEngine.Object dep in EditorUtility.CollectDependencies(collected.ToArray()))
                {
                    if (dep == null) continue;
                    string depPath = AssetDatabase.GetAssetPath(dep);
                    if (!string.IsNullOrEmpty(depPath)) paths.Add(KaleidoVRCOptimizerHelpers.NormalizeAssetPath(depPath));
                }
            }
            return paths;
        }

        private static void CollectFromTargets(
            List<UnityEngine.Object> targets,
            HashSet<string> ignorePaths,
            List<GameObject> roots,
            HashSet<string> assetPaths,
            List<string> logEntries,
            KaleidoOptimizerReport report)
        {
            HashSet<int> seenRoots = new HashSet<int>();
            foreach (UnityEngine.Object target in targets)
            {
                if (target == null) continue;

                GameObject avatarRoot;
                string reason;
                if (!KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(target, out avatarRoot, out reason))
                {
                    logEntries.Add("Rejected: " + target.name + " — " + reason);
                    report.notes.Add(target.name + ": " + reason);
                    continue;
                }

                if (avatarRoot == null) continue;
                string path = KaleidoVRCOptimizerHelpers.NormalizeAssetPath(AssetDatabase.GetAssetPath(avatarRoot));
                if (!string.IsNullOrEmpty(path) && ignorePaths.Contains(path)) continue;

                if (seenRoots.Add(avatarRoot.GetInstanceID()))
                {
                    roots.Add(avatarRoot);
                    logEntries.Add("Avatar model: " + avatarRoot.name + (string.IsNullOrEmpty(path) ? "" : " (" + path + ")"));
                }
                CollectGameObjectAssets(avatarRoot, ignorePaths, assetPaths);
            }

            logEntries.Add("Collected avatar roots: " + roots.Count + ", related assets: " + assetPaths.Count);
        }

        public static void FillInventory(KaleidoVRCOptimizer window)
        {
            if (window.inventory == null) window.inventory = new List<KaleidoModelInventoryItem>();
            window.inventory.Clear();
            HashSet<string> ignorePaths = BuildIgnorePaths(window.ignoreList);
            List<GameObject> roots = new List<GameObject>();
            HashSet<string> assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            KaleidoOptimizerReport unused = new KaleidoOptimizerReport();
            CollectFromTargets(window.targets, ignorePaths, roots, assetPaths, new List<string>(), unused);
            PopulateInventory(window, assetPaths);
        }

        private static void PopulateInventory(KaleidoVRCOptimizer window, HashSet<string> assetPaths)
        {
            if (window.inventory == null) window.inventory = new List<KaleidoModelInventoryItem>();
            window.inventory.Clear();
            HashSet<int> seen = new HashSet<int>();
            foreach (string path in assetPaths)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets == null) continue;
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset == null) continue;
                    bool meshOrClip = asset is Mesh || asset is AnimationClip || asset is Avatar;
                    if (!meshOrClip && !KaleidoVRCOptimizerHelpers.IsAvatarScopedAsset(asset)) continue;
                    if (asset is GameObject && !AssetDatabase.IsMainAsset(asset)) continue;
                    if (!seen.Add(asset.GetInstanceID())) continue;

                    window.inventory.Add(new KaleidoModelInventoryItem
                    {
                        asset = asset,
                        path = path,
                        typeName = asset.GetType().Name,
                        category = KaleidoVRCOptimizerHelpers.CategorizeInventoryAsset(asset),
                        label = asset.name
                    });
                }
            }

            window.inventory.Sort(delegate (KaleidoModelInventoryItem a, KaleidoModelInventoryItem b)
            {
                int cat = KaleidoVRCOptimizerHelpers.InventoryCategoryOrder(a.category).CompareTo(KaleidoVRCOptimizerHelpers.InventoryCategoryOrder(b.category));
                if (cat != 0) return cat;
                return string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static void CollectGameObjectAssets(GameObject go, HashSet<string> ignorePaths, HashSet<string> assetPaths)
        {
            string ownPath = AssetDatabase.GetAssetPath(go);
            AddAssetPath(ownPath, ignorePaths, assetPaths);

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            AddAssetPath(prefabPath, ignorePaths, assetPaths);

            List<UnityEngine.Object> collect = new List<UnityEngine.Object> { go };
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                collect.Add(renderer);
                if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null) collect.Add(skinned.sharedMesh);
                if (renderer is MeshRenderer)
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null) collect.Add(filter.sharedMesh);
                }
                if (renderer.sharedMaterials != null)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null) collect.Add(material);
                    }
                }
            }

            Animator[] animators = go.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                if (animator != null && animator.runtimeAnimatorController != null) collect.Add(animator.runtimeAnimatorController);
            }

            AudioSource[] sources = go.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource source in sources)
            {
                if (source != null && source.clip != null) collect.Add(source.clip);
            }

            foreach (UnityEngine.Object dep in EditorUtility.CollectDependencies(collect.ToArray()))
            {
                if (!KaleidoVRCOptimizerHelpers.IsAvatarScopedAsset(dep)) continue;
                AddAssetPath(AssetDatabase.GetAssetPath(dep), ignorePaths, assetPaths);
            }
        }

        private static void AddAssetPath(string path, HashSet<string> ignorePaths, HashSet<string> assetPaths)
        {
            path = KaleidoVRCOptimizerHelpers.NormalizeAssetPath(path);
            if (string.IsNullOrEmpty(path)) return;
            if (KaleidoVRCOptimizerHelpers.ShouldIgnoreAsset(path)) return;
            if (ignorePaths.Contains(path)) return;
            assetPaths.Add(path);
        }

        private static void GatherStats(List<GameObject> roots, HashSet<string> assetPaths, KaleidoOptimizerReport report, List<string> logEntries)
        {
            HashSet<Material> uniqueMats = new HashSet<Material>();
            HashSet<Texture> uniqueTex = new HashSet<Texture>();
            Type physBoneType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone");
            Type contactType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver");
            Type contactSenderType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Dynamics.Contact.Components.VRCContactSender");
            Type physBoneColliderType = KaleidoVRCOptimizerHelpers.FindTypeByFullName("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider");

            List<GameObject> statRoots = new List<GameObject>(roots);
            if (statRoots.Count == 0)
            {
                foreach (string path in assetPaths)
                {
                    if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) statRoots.Add(prefab);
                }
            }

            HashSet<int> seen = new HashSet<int>();
            foreach (GameObject root in statRoots)
            {
                if (root == null || !seen.Add(root.GetInstanceID())) continue;

                foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    report.skinnedMeshes++;
                    AccumulateMesh(skinned.sharedMesh, report);
                    AccumulateMaterials(skinned.sharedMaterials, uniqueMats, uniqueTex, report);
                    if (skinned.bones != null && skinned.bones.Length > report.bones) report.bones = skinned.bones.Length;
                    if (skinned.updateWhenOffscreen) report.notes.Add(skinned.name + ": Update When Offscreen is on (CPU cost when culled).");
                }

                foreach (MeshRenderer meshRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    report.meshRenderers++;
                    MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                    if (filter != null) AccumulateMesh(filter.sharedMesh, report);
                    AccumulateMaterials(meshRenderer.sharedMaterials, uniqueMats, uniqueTex, report);
                }

                report.physBones += KaleidoVRCOptimizerHelpers.CountComponents(root, physBoneType);
                report.physBoneColliders += KaleidoVRCOptimizerHelpers.CountComponents(root, physBoneColliderType);
                report.contacts += KaleidoVRCOptimizerHelpers.CountComponents(root, contactType);
                report.contacts += KaleidoVRCOptimizerHelpers.CountComponents(root, contactSenderType);
                report.constraints += KaleidoVRCOptimizerHelpers.CountUnityConstraints(root);
                report.animators += root.GetComponentsInChildren<Animator>(true).Length;
                report.lights += root.GetComponentsInChildren<Light>(true).Length;
                report.audioSources += root.GetComponentsInChildren<AudioSource>(true).Length;
                report.particleSystems += root.GetComponentsInChildren<ParticleSystem>(true).Length;
            }

            foreach (string path in assetPaths)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset is Texture texture) uniqueTex.Add(texture);
                    if (asset is Material material) uniqueMats.Add(material);
                    if (asset is Mesh mesh && roots.Count == 0) AccumulateMesh(mesh, report);
                }
            }

            report.uniqueMaterials = uniqueMats.Count;
            report.uniqueTextures = uniqueTex.Count;
            foreach (Texture texture in uniqueTex)
            {
                report.textureBytesEstimate += EstimateTextureBytes(texture);
            }

            report.pcRank = RankAvatar(report, false);
            report.questRank = RankAvatar(report, true);
            logEntries.Add("Triangles=" + report.triangles + " texEst=" + report.textureBytesEstimate + " pc=" + report.pcRank + " quest=" + report.questRank);
        }

        private static void AccumulateMesh(Mesh mesh, KaleidoOptimizerReport report)
        {
            if (mesh == null) return;
            report.triangles += mesh.triangles.Length / 3;
            report.blendShapes += mesh.blendShapeCount;
            if (!mesh.isReadable) report.meshReadWriteDisabled = true;
        }

        private static void AccumulateMaterials(Material[] materials, HashSet<Material> uniqueMats, HashSet<Texture> uniqueTex, KaleidoOptimizerReport report)
        {
            if (materials == null) return;
            report.materialSlots += materials.Length;
            foreach (Material material in materials)
            {
                if (material == null) continue;
                uniqueMats.Add(material);
                Shader shader = material.shader;
                if (shader == null) continue;
                int count = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < count; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                    Texture texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                    if (texture != null) uniqueTex.Add(texture);
                }
            }
        }

        private static long EstimateTextureBytes(Texture texture)
        {
            int width = texture.width;
            int height = texture.height;
            int bpp = 4;
            if (texture is Texture2D tex2D)
            {
                switch (tex2D.format)
                {
                    case TextureFormat.DXT1:
                    case TextureFormat.BC4:
                    case TextureFormat.ETC_RGB4:
                    case TextureFormat.ETC2_RGB: bpp = 1; break;
                    case TextureFormat.DXT5:
                    case TextureFormat.BC5:
                    case TextureFormat.BC7:
                    case TextureFormat.ETC2_RGBA8:
                    case TextureFormat.ASTC_4x4:
                    case TextureFormat.ASTC_5x5:
                    case TextureFormat.ASTC_6x6:
                    case TextureFormat.ASTC_8x8: bpp = 2; break;
                    default: bpp = 4; break;
                }
            }
            long top = (long)width * height * bpp;
            return (long)(top * 1.333);
        }

        private static string RankAvatar(KaleidoOptimizerReport report, bool quest)
        {
            // Limits from VRChat Creation docs: Avatar Performance Ranking System (PC and mobile tables).
            if (report.meshReadWriteDisabled) return "Very Poor";

            int rank = 0;
            if (quest)
            {
                rank = Math.Max(rank, LimitRank(report.triangles, 7500, 10000, 15000, 20000));
                rank = Math.Max(rank, LimitRankBytes(report.textureBytesEstimate, 10L * 1024 * 1024, 18L * 1024 * 1024, 25L * 1024 * 1024, 40L * 1024 * 1024));
                rank = Math.Max(rank, LimitRank(report.skinnedMeshes, 1, 1, 2, 2));
                rank = Math.Max(rank, LimitRank(report.meshRenderers, 1, 1, 2, 2));
                rank = Math.Max(rank, LimitRank(report.materialSlots, 1, 1, 2, 4));
                rank = Math.Max(rank, LimitRank(report.animators, 1, 1, 1, 2));
                rank = Math.Max(rank, LimitRank(report.bones, 75, 90, 150, 150));
                rank = Math.Max(rank, LimitRank(report.physBones, 0, 4, 6, 8));
                rank = Math.Max(rank, LimitRank(report.physBoneColliders, 0, 4, 8, 16));
                rank = Math.Max(rank, LimitRank(report.contacts, 2, 4, 8, 16));
                rank = Math.Max(rank, LimitRank(report.constraints, 30, 60, 120, 150));
                rank = Math.Max(rank, LimitRank(report.particleSystems, 0, 0, 0, 2));
            }
            else
            {
                rank = Math.Max(rank, LimitRank(report.triangles, 32000, 70000, 70000, 70000));
                rank = Math.Max(rank, LimitRankBytes(report.textureBytesEstimate, 40L * 1024 * 1024, 75L * 1024 * 1024, 110L * 1024 * 1024, 150L * 1024 * 1024));
                rank = Math.Max(rank, LimitRank(report.skinnedMeshes, 1, 2, 8, 16));
                rank = Math.Max(rank, LimitRank(report.meshRenderers, 4, 8, 16, 24));
                rank = Math.Max(rank, LimitRank(report.materialSlots, 4, 8, 16, 32));
                rank = Math.Max(rank, LimitRank(report.animators, 1, 4, 16, 32));
                rank = Math.Max(rank, LimitRank(report.bones, 75, 150, 256, 400));
                rank = Math.Max(rank, LimitRank(report.physBones, 4, 8, 16, 32));
                rank = Math.Max(rank, LimitRank(report.physBoneColliders, 4, 8, 16, 32));
                rank = Math.Max(rank, LimitRank(report.contacts, 8, 16, 24, 32));
                rank = Math.Max(rank, LimitRank(report.constraints, 100, 250, 300, 350));
                rank = Math.Max(rank, LimitRank(report.lights, 0, 0, 0, 1));
                rank = Math.Max(rank, LimitRank(report.audioSources, 1, 4, 8, 8));
                rank = Math.Max(rank, LimitRank(report.particleSystems, 0, 4, 8, 16));
            }

            return RankName(rank);
        }

        private static int LimitRank(int value, int excellent, int good, int medium, int poor)
        {
            if (value <= excellent) return 0;
            if (value <= good) return 1;
            if (value <= medium) return 2;
            if (value <= poor) return 3;
            return 4;
        }

        private static int LimitRankBytes(long value, long excellent, long good, long medium, long poor)
        {
            if (value <= excellent) return 0;
            if (value <= good) return 1;
            if (value <= medium) return 2;
            if (value <= poor) return 3;
            return 4;
        }

        private static string RankName(int rank)
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

        private static void BuildHints(KaleidoOptimizerReport report)
        {
            if (report.meshReadWriteDisabled) report.notes.Add("Enable Mesh Read/Write. VRChat ranks any avatar with it off as Very Poor and the SDK blocks upload.");
            if (report.triangles > 70000) report.notes.Add("PC triangles are above 70k (Poor/Very Poor cutoff). Decimate in Blender.");
            if (report.triangles > 20000) report.notes.Add("Quest Poor allows 20k triangles. Over that, mobile viewers cannot see the avatar without Show Avatar.");
            else if (report.triangles > 10000) report.notes.Add("VRChat recommends under 10k triangles on Android. Quest Good is 10k, Excellent is 7.5k.");
            if (report.triangles > 32000 && report.triangles <= 70000) report.notes.Add("Above PC Excellent (32k triangles).");
            if (report.materialSlots > 32) report.notes.Add("PC material slots are above Poor (32).");
            else if (report.materialSlots > 4) report.notes.Add("Quest Poor allows 4 material slots. Atlas toward 1 material for Excellent/Good on mobile.");
            if (report.skinnedMeshes > 2) report.notes.Add("Quest Poor allows 2 skinned meshes. Aim for 1 skinned mesh on mobile.");
            if (report.textureBytesEstimate > 40L * 1024 * 1024) report.notes.Add("Quest texture memory Poor is 40 MB. Drop max size; Crunch does not reduce this number.");
            else if (report.textureBytesEstimate > 75L * 1024 * 1024) report.notes.Add("PC texture memory is past Good (75 MB). Drop max size.");
            if (report.physBones > 8) report.notes.Add("Quest strips PhysBones if you exceed 8 components. PC Poor allows 32.");
            if (report.lights > 0) report.notes.Add("Any realtime light is already worse than PC Excellent (0). Android disables avatar lights.");
            if (report.constraints > 0) report.notes.Add("Unity constraints are disabled on Android avatars. Use VRChat Constraints; they still count toward rank.");
            if (report.audioSources > 0) report.notes.Add("Audio sources are disabled on Android avatars. PC Excellent allows 1.");
        }

        private static void ApplyOptimizations(
            KaleidoVRCOptimizer window,
            List<GameObject> roots,
            HashSet<string> assetPaths,
            HashSet<string> ignorePaths,
            KaleidoOptimizerReport report,
            List<string> logEntries)
        {
            bool write = !window.dryRun;
            List<string> changedImporters = new List<string>();

            if (write) AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in assetPaths)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(path);
                    if (importer == null) continue;

                    if (window.optimizeTextures && importer is TextureImporter textureImporter)
                    {
                        if (ApplyTextureImporter(window, path, textureImporter, write))
                        {
                            string line = "Texture (" + KaleidoVRCOptimizerHelpers.ClassifyTexture(path, textureImporter) + "): " + path;
                            report.planned.Add(line);
                            logEntries.Add(line);
                            changedImporters.Add(path);
                            if (write) EditorUtility.SetDirty(textureImporter);
                        }
                    }
                    else if (window.optimizeMeshes && importer is ModelImporter modelImporter)
                    {
                        if (ApplyModelImporter(window, path, modelImporter, write))
                        {
                            string line = "Model: " + path;
                            report.planned.Add(line);
                            logEntries.Add(line);
                            changedImporters.Add(path);
                            if (write) EditorUtility.SetDirty(modelImporter);
                        }
                    }
                    else if (window.optimizeAudio && importer is AudioImporter audioImporter)
                    {
                        if (ApplyAudioImporter(window, path, audioImporter, write))
                        {
                            string line = "Audio: " + path;
                            report.planned.Add(line);
                            logEntries.Add(line);
                            changedImporters.Add(path);
                            if (write) EditorUtility.SetDirty(audioImporter);
                        }
                    }
                }

                if (window.optimizeMaterials)
                {
                    foreach (string path in assetPaths)
                    {
                        if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) continue;
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material == null) continue;
                        if (material.enableInstancing == window.materialEnableGpuInstancing) continue;
                        string line = "Material instancing " + window.materialEnableGpuInstancing + ": " + path;
                        report.planned.Add(line);
                        logEntries.Add(line);
                        if (write)
                        {
                            material.enableInstancing = window.materialEnableGpuInstancing;
                            EditorUtility.SetDirty(material);
                        }
                    }
                }
            }
            finally
            {
                if (write)
                {
                    AssetDatabase.StopAssetEditing();
                    foreach (string path in changedImporters)
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                    AssetDatabase.SaveAssets();
                }
            }

            HashSet<string> processedPrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (GameObject root in roots)
            {
                ApplyHierarchy(window, root, write, report, logEntries);

                if (!window.applyToPrefabAssets) continue;
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = AssetDatabase.GetAssetPath(root);
                }
                if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                if (ignorePaths.Contains(KaleidoVRCOptimizerHelpers.NormalizeAssetPath(prefabPath))) continue;
                if (!processedPrefabs.Add(prefabPath)) continue;

                if (!write)
                {
                    string line = "Would update prefab: " + prefabPath;
                    report.planned.Add(line);
                    logEntries.Add(line);
                    continue;
                }

                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    ApplyHierarchy(window, contents, true, report, logEntries);
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    logEntries.Add("Updated prefab: " + prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            foreach (string path in assetPaths)
            {
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                if (!processedPrefabs.Add(path)) continue;
                if (!write)
                {
                    report.planned.Add("Would update prefab: " + path);
                    continue;
                }
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ApplyHierarchy(window, contents, true, report, logEntries);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    logEntries.Add("Updated prefab: " + path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        private static bool ApplyTextureImporter(KaleidoVRCOptimizer window, string path, TextureImporter importer, bool write)
        {
            KaleidoTextureKind kind = KaleidoVRCOptimizerHelpers.ClassifyTexture(path, importer);
            bool normal = kind == KaleidoTextureKind.Normal;
            bool mask = kind == KaleidoTextureKind.Mask;
            bool albedo = kind == KaleidoTextureKind.Albedo;

            bool applySize;
            int pcSize;
            int questSize;
            GetTypeSizes(window, kind, out applySize, out pcSize, out questSize);

            bool dirty = false;
            if (window.autoDetectNormalMaps && normal && importer.textureType != TextureImporterType.NormalMap)
            {
                if (write) importer.textureType = TextureImporterType.NormalMap;
                dirty = true;
            }

            if (window.textureDisableReadWrite && importer.isReadable)
            {
                if (write) importer.isReadable = false;
                dirty = true;
            }

            if (window.textureApplyMipmaps && importer.mipmapEnabled != window.textureEnableMipmaps)
            {
                if (write) importer.mipmapEnabled = window.textureEnableMipmaps;
                dirty = true;
            }

            if (window.textureDisableStreamingMipmaps && importer.streamingMipmaps)
            {
                if (write) importer.streamingMipmaps = false;
                dirty = true;
            }

            if (window.textureApplyAniso && importer.anisoLevel != window.textureAniso)
            {
                if (write) importer.anisoLevel = window.textureAniso;
                dirty = true;
            }

            if (window.textureEnableCrunch && !importer.crunchedCompression)
            {
                if (write) importer.crunchedCompression = true;
                dirty = true;
            }
            else if (window.textureDisableCrunch && !window.textureEnableCrunch && importer.crunchedCompression)
            {
                if (write) importer.crunchedCompression = false;
                dirty = true;
            }

            TextureImporterCompression wantedCompression = window.pcTexFormat == KaleidoPcTexFormat.HighQuality
                ? TextureImporterCompression.CompressedHQ
                : TextureImporterCompression.Compressed;
            if (window.applyPcTexFormat && importer.textureCompression != wantedCompression)
            {
                if (write) importer.textureCompression = wantedCompression;
                dirty = true;
            }

            if (window.autoLinearMaskMaps && mask && importer.sRGBTexture)
            {
                if (write) importer.sRGBTexture = false;
                dirty = true;
            }

            if (window.alphaIsTransparencyOnAlbedo && albedo && importer.DoesSourceTextureHaveAlpha() && !importer.alphaIsTransparency)
            {
                if (write) importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (applySize && importer.maxTextureSize != pcSize)
            {
                if (write) importer.maxTextureSize = pcSize;
                dirty = true;
            }

            bool normalHq = normal && window.higherQualityNormalMaps;
            TextureImporterFormat pcFormat = ToPcFormat(window.pcTexFormat, normalHq);
            TextureImporterFormat androidFormat = ToAndroidFormat(window.androidTexFormat, normalHq);

            if (ApplyPlatform(importer, "Standalone", applySize, pcSize, window.applyPcTexFormat, pcFormat, wantedCompression, window.textureDisableCrunch && !window.textureEnableCrunch, write))
                dirty = true;
            if (ApplyPlatform(importer, "Android", applySize, questSize, window.applyAndroidTexFormat, androidFormat, TextureImporterCompression.Compressed, window.textureDisableCrunch && !window.textureEnableCrunch, write))
                dirty = true;

            return dirty;
        }

        private static void GetTypeSizes(KaleidoVRCOptimizer window, KaleidoTextureKind kind, out bool apply, out int pc, out int quest)
        {
            switch (kind)
            {
                case KaleidoTextureKind.Albedo:
                    apply = window.applyAlbedoSize; pc = window.albedoPc; quest = window.albedoQuest; return;
                case KaleidoTextureKind.Normal:
                    apply = window.applyNormalSize; pc = window.normalPc; quest = window.normalQuest; return;
                case KaleidoTextureKind.Mask:
                    apply = window.applyMaskSize; pc = window.maskPc; quest = window.maskQuest; return;
                case KaleidoTextureKind.Emission:
                    apply = window.applyEmissionSize; pc = window.emissionPc; quest = window.emissionQuest; return;
                case KaleidoTextureKind.Matcap:
                    apply = window.applyMatcapSize; pc = window.matcapPc; quest = window.matcapQuest; return;
                default:
                    apply = window.applyOtherSize; pc = window.otherPc; quest = window.otherQuest; return;
            }
        }

        private static bool ApplyPlatform(
            TextureImporter importer,
            string platform,
            bool applySize,
            int maxSize,
            bool applyFormat,
            TextureImporterFormat format,
            TextureImporterCompression compression,
            bool disableCrunch,
            bool write)
        {
            if (!applySize && !applyFormat && !disableCrunch) return false;

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            bool dirty = false;
            if ((applySize || applyFormat) && !settings.overridden) dirty = true;
            if (applySize && settings.maxTextureSize != maxSize) dirty = true;
            if (applyFormat && settings.format != format) dirty = true;
            if (applyFormat && settings.textureCompression != compression) dirty = true;
            if (disableCrunch && settings.crunchedCompression) dirty = true;
            if (!dirty) return false;
            if (!write) return true;

            settings.overridden = true;
            settings.name = platform;
            if (applySize) settings.maxTextureSize = maxSize;
            if (applyFormat)
            {
                settings.format = format;
                settings.textureCompression = compression;
            }
            if (disableCrunch) settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        private static TextureImporterFormat ToPcFormat(KaleidoPcTexFormat format, bool normalHq)
        {
            if (normalHq) return TextureImporterFormat.BC5;
            switch (format)
            {
                case KaleidoPcTexFormat.BC7: return TextureImporterFormat.BC7;
                case KaleidoPcTexFormat.DXT5: return TextureImporterFormat.DXT5;
                default: return TextureImporterFormat.Automatic;
            }
        }

        private static TextureImporterFormat ToAndroidFormat(KaleidoAndroidTexFormat format, bool normalHq)
        {
            if (normalHq)
            {
                if (format == KaleidoAndroidTexFormat.ASTC_8x8) return TextureImporterFormat.ASTC_6x6;
                if (format == KaleidoAndroidTexFormat.ASTC_6x6) return TextureImporterFormat.ASTC_4x4;
            }
            switch (format)
            {
                case KaleidoAndroidTexFormat.ASTC_4x4: return TextureImporterFormat.ASTC_4x4;
                case KaleidoAndroidTexFormat.ASTC_5x5: return TextureImporterFormat.ASTC_5x5;
                case KaleidoAndroidTexFormat.ASTC_8x8: return TextureImporterFormat.ASTC_8x8;
                case KaleidoAndroidTexFormat.ETC2_RGBA8: return TextureImporterFormat.ETC2_RGBA8;
                default: return TextureImporterFormat.ASTC_6x6;
            }
        }

        private static bool ApplyModelImporter(KaleidoVRCOptimizer window, string path, ModelImporter importer, bool write)
        {
            bool dirty = false;
            if (window.applyMeshCompression)
            {
                ModelImporterMeshCompression compression = ToMeshCompression(window.meshCompression);
                if (importer.meshCompression != compression)
                {
                    if (write) importer.meshCompression = compression;
                    dirty = true;
                }
            }
            if (window.meshEnableReadWrite && !importer.isReadable)
            {
                if (write) importer.isReadable = true;
                dirty = true;
            }
            if (window.meshOptimizePolygons && !importer.optimizeMeshPolygons)
            {
                if (write) importer.optimizeMeshPolygons = true;
                dirty = true;
            }
            if (window.meshOptimizeVertices && !importer.optimizeMeshVertices)
            {
                if (write) importer.optimizeMeshVertices = true;
                dirty = true;
            }
            if (window.meshWeldVertices && !importer.weldVertices)
            {
                if (write) importer.weldVertices = true;
                dirty = true;
            }
            if (window.meshStripBlendShapes && importer.importBlendShapes)
            {
                if (write) importer.importBlendShapes = false;
                dirty = true;
            }
            else if (window.meshKeepBlendShapes && !importer.importBlendShapes)
            {
                if (write) importer.importBlendShapes = true;
                dirty = true;
            }
            if (window.meshDisableQuads && importer.keepQuads)
            {
                if (write) importer.keepQuads = false;
                dirty = true;
            }
            if (window.meshDisableLightmapUVs && importer.generateSecondaryUV)
            {
                if (write) importer.generateSecondaryUV = false;
                dirty = true;
            }
            if (window.meshDisableImportLightsCameras && (importer.importLights || importer.importCameras))
            {
                if (write)
                {
                    importer.importLights = false;
                    importer.importCameras = false;
                }
                dirty = true;
            }
            if (window.meshOptimizeAnimation && importer.animationCompression != ModelImporterAnimationCompression.Optimal)
            {
                if (write) importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                dirty = true;
            }

            if (window.applySkinWeights)
            {
                // 2022.3 LTS only has Standard (4 bones) and Custom. Custom + a high cap is the Unlimited equivalent.
                if (window.skinWeights == KaleidoSkinWeightChoice.Unlimited)
                {
                    if (importer.skinWeights != ModelImporterSkinWeights.Custom || importer.maxBonesPerVertex < 32)
                    {
                        if (write)
                        {
                            importer.skinWeights = ModelImporterSkinWeights.Custom;
                            importer.maxBonesPerVertex = 255;
                        }
                        dirty = true;
                    }
                }
                else if (importer.skinWeights != ModelImporterSkinWeights.Standard)
                {
                    if (write) importer.skinWeights = ModelImporterSkinWeights.Standard;
                    dirty = true;
                }
            }

            if (window.meshForceHumanoid && importer.animationType != ModelImporterAnimationType.Human)
            {
                if (write)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                }
                dirty = true;
            }

            return dirty;
        }

        private static ModelImporterMeshCompression ToMeshCompression(KaleidoMeshCompressionChoice choice)
        {
            switch (choice)
            {
                case KaleidoMeshCompressionChoice.Low: return ModelImporterMeshCompression.Low;
                case KaleidoMeshCompressionChoice.Medium: return ModelImporterMeshCompression.Medium;
                case KaleidoMeshCompressionChoice.High: return ModelImporterMeshCompression.High;
                default: return ModelImporterMeshCompression.Off;
            }
        }

        private static bool ApplyAudioImporter(KaleidoVRCOptimizer window, string path, AudioImporter importer, bool write)
        {
            bool dirty = false;
            if (window.audioForceToMono && !importer.forceToMono)
            {
                if (write) importer.forceToMono = true;
                dirty = true;
            }
            if (window.audioLoadInBackground && !importer.loadInBackground)
            {
                if (write) importer.loadInBackground = true;
                dirty = true;
            }

            if (window.audioApplyVorbis)
            {
                AudioImporterSampleSettings sample = importer.defaultSampleSettings;
                if (sample.compressionFormat != AudioCompressionFormat.Vorbis
                    || sample.loadType != AudioClipLoadType.CompressedInMemory
                    || Math.Abs(sample.quality - window.audioQuality) > 0.001f)
                {
                    if (write)
                    {
                        sample.compressionFormat = AudioCompressionFormat.Vorbis;
                        sample.loadType = AudioClipLoadType.CompressedInMemory;
                        sample.quality = window.audioQuality;
                        importer.defaultSampleSettings = sample;
                    }
                    dirty = true;
                }
            }

            return dirty;
        }

        private static void ApplyHierarchy(
            KaleidoVRCOptimizer window,
            GameObject root,
            bool write,
            KaleidoOptimizerReport report,
            List<string> logEntries)
        {
            if (root == null) return;

            if (window.optimizeRenderers)
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (ApplyRenderer(window, renderer, write))
                    {
                        string line = "Renderer: " + GetPath(renderer.transform);
                        report.planned.Add(line);
                        logEntries.Add(line);
                    }
                }
            }

            if (window.optimizeAnimators)
            {
                foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
                {
                    AnimatorCullingMode wanted = window.animatorCullWhenOffscreen
                        ? AnimatorCullingMode.CullUpdateTransforms
                        : AnimatorCullingMode.AlwaysAnimate;
                    if (animator.cullingMode == wanted) continue;
                    string line = "Animator cull: " + GetPath(animator.transform);
                    report.planned.Add(line);
                    logEntries.Add(line);
                    if (write)
                    {
                        animator.cullingMode = wanted;
                        EditorUtility.SetDirty(animator);
                    }
                }
            }

            if (window.optimizeSceneExtras)
            {
                if (window.disableLightsOnAvatar)
                {
                    foreach (Light light in root.GetComponentsInChildren<Light>(true))
                    {
                        if (!light.enabled) continue;
                        string line = "Disable light: " + GetPath(light.transform);
                        report.planned.Add(line);
                        logEntries.Add(line);
                        if (write)
                        {
                            light.enabled = false;
                            EditorUtility.SetDirty(light);
                        }
                    }
                }

                if (window.disableCamerasOnAvatar)
                {
                    foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        if (!camera.enabled) continue;
                        string line = "Disable camera: " + GetPath(camera.transform);
                        report.planned.Add(line);
                        logEntries.Add(line);
                        if (write)
                        {
                            camera.enabled = false;
                            EditorUtility.SetDirty(camera);
                        }
                    }
                }

                if (window.optimizeParticles)
                {
                    foreach (ParticleSystemRenderer particle in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    {
                        bool changed = false;
                        if (particle.shadowCastingMode != ShadowCastingMode.Off) changed = true;
                        if (particle.motionVectorGenerationMode != MotionVectorGenerationMode.ForceNoMotion) changed = true;
                        if (!changed) continue;
                        string line = "Particle renderer: " + GetPath(particle.transform);
                        report.planned.Add(line);
                        logEntries.Add(line);
                        if (write)
                        {
                            particle.shadowCastingMode = ShadowCastingMode.Off;
                            particle.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                            EditorUtility.SetDirty(particle);
                        }
                    }
                }
            }

            if (write && root.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
        }

        private static bool ApplyRenderer(KaleidoVRCOptimizer window, Renderer renderer, bool write)
        {
            bool dirty = false;
            ShadowCastingMode shadow = window.rendererDisableShadows ? ShadowCastingMode.Off : renderer.shadowCastingMode;
            LightProbeUsage probes = window.rendererDisableProbes ? LightProbeUsage.Off : renderer.lightProbeUsage;
            ReflectionProbeUsage reflections = window.rendererDisableProbes ? ReflectionProbeUsage.Off : renderer.reflectionProbeUsage;
            MotionVectorGenerationMode motion = window.rendererDisableMotionVectors
                ? MotionVectorGenerationMode.ForceNoMotion
                : renderer.motionVectorGenerationMode;

            if (window.rendererDisableShadows && renderer.shadowCastingMode != ShadowCastingMode.Off) dirty = true;
            if (window.rendererDisableReceiveShadows && renderer.receiveShadows) dirty = true;
            if (window.rendererDisableProbes && (renderer.lightProbeUsage != LightProbeUsage.Off || renderer.reflectionProbeUsage != ReflectionProbeUsage.Off)) dirty = true;
            if (window.rendererDisableMotionVectors && renderer.motionVectorGenerationMode != MotionVectorGenerationMode.ForceNoMotion) dirty = true;

            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                if (window.rendererDisableUpdateWhenOffscreen && skinned.updateWhenOffscreen) dirty = true;
                if (window.rendererForceBone4 && skinned.quality != SkinQuality.Bone4) dirty = true;
                if (window.rendererRecalculateBounds && skinned.sharedMesh != null) dirty = true;
            }

            if (!dirty) return false;
            if (!write) return true;

            if (window.rendererDisableShadows) renderer.shadowCastingMode = shadow;
            if (window.rendererDisableReceiveShadows) renderer.receiveShadows = false;
            if (window.rendererDisableProbes)
            {
                renderer.lightProbeUsage = probes;
                renderer.reflectionProbeUsage = reflections;
            }
            if (window.rendererDisableMotionVectors) renderer.motionVectorGenerationMode = motion;

            if (skinned != null)
            {
                if (window.rendererDisableUpdateWhenOffscreen) skinned.updateWhenOffscreen = false;
                if (window.rendererForceBone4) skinned.quality = SkinQuality.Bone4;
                if (window.rendererRecalculateBounds && skinned.sharedMesh != null)
                {
                    skinned.localBounds = skinned.sharedMesh.bounds;
                    skinned.skinnedMotionVectors = false;
                }
            }

            EditorUtility.SetDirty(renderer);
            return true;
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null) return "(null)";
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
    }
}
