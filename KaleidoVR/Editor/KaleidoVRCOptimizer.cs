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
using UnityEditor.Animations;
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
        DXT5 = 3,
        AutoBc7Dxt1 = 4,
        DXT1 = 5,
        BC5 = 6
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
        public KaleidoPcTexFormat pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
        public bool applyAndroidTexFormat = true;
        public KaleidoAndroidTexFormat androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
        public bool textureDisableReadWrite = true;
        public bool textureApplyMipmaps = true;
        public bool textureEnableMipmaps = true;
        public bool textureEnableStreamingMipmaps = true;
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

    [Serializable]
    public class KaleidoOptimizerProfileFile
    {
        public const string FormatId = "KaleidoVR.VRChatModelOptimizer.Profiles";
        public string format = FormatId;
        public int formatVersion = 1;
        public string toolVersion = "";
        public string exportedUtc = "";
        public KaleidoOptimizerProfile[] profiles = new KaleidoOptimizerProfile[0];
    }

    public class KaleidoVRCOptimizer : EditorWindow
    {
        public static readonly string VERSION = "1.0.22";
        public const string LOGO_FILE_NAME = "Kali_Logo.png";
        public const string FALLBACK_ICON_PATH = "Assets/KaleidoVR/Editor/Icons/Kali_Logo.png";
        public const string PrefsPrefix = "KVR_VrcOpt_";

        public static string ICON_PATH { get { return ResolveIconPath(); } }

        private static string cachedIconPath;
        private Texture2D headerIcon;
        private Vector2 scroll;

        public int tab;
        public int workspace;
        public int textureSort;
        public int builtinPresetIndex = 2;
        public int selectedUserProfile = -1;
        public string newProfileName = "My Profile";
        public KaleidoOptimizerProfileList userProfiles = new KaleidoOptimizerProfileList();

        public List<UnityEngine.Object> targets = new List<UnityEngine.Object>();
        public List<UnityEngine.Object> ignoreList = new List<UnityEngine.Object>();

        public bool dryRun = true;
        public bool writeLog = true;
        public bool applyToPrefabAssets = true;
        public bool readyToApply;
        public bool readyToApplyPc;
        public bool readyToApplyQuest;
        public bool readyToApplyMaxSizesPc;
        public bool readyToApplyMaxSizesQuest;

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
        public KaleidoPcTexFormat pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
        public bool applyAndroidTexFormat = true;
        public KaleidoAndroidTexFormat androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
        public bool textureDisableReadWrite = true;
        public bool textureApplyMipmaps = true;
        public bool textureEnableMipmaps = true;
        public bool textureEnableStreamingMipmaps = true;
        public bool textureDisableCrunch = true;
        public bool textureApplyAniso = true;
        public int textureAniso = 1;
        public bool autoDetectNormalMaps = true;
        public bool autoLinearMaskMaps = true;
        public bool higherQualityNormalMaps = true;
        public bool alphaIsTransparencyOnAlbedo = false;

        public bool optimizeMeshes = true;
        public bool meshEnableReadWrite = true;
        public bool meshOptimizePolygons = true;
        public bool meshOptimizeVertices = true;
        public bool meshWeldVertices = false;
        public bool meshKeepBlendShapes = true;
        public bool meshDisableQuads = true;
        public bool meshDisableLightmapUVs = true;
        public bool meshDisableImportLightsCameras = true;
        public bool meshOptimizeAnimation = false;
        public bool applySkinWeights = false;
        public KaleidoSkinWeightChoice skinWeights = KaleidoSkinWeightChoice.FourBones;

        public bool optimizeRenderers = true;
        public bool rendererDisableUpdateWhenOffscreen = true;
        public bool rendererDisableShadows = false;
        public bool rendererDisableReceiveShadows = false;
        public bool rendererDisableProbes = false;
        public bool rendererDisableMotionVectors = true;
        public bool rendererForceBone4 = false;
        public bool rendererRecalculateBounds = false;
        public bool optimizeParticles = false;
        public bool disableLightsOnAvatar = false;

        public bool optimizeAudio = true;
        public bool audioLoadInBackground = true;
        public bool audioApplyVorbis = false;
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
        public KaleidoOptimizerReport lastPcReport;
        public KaleidoOptimizerReport lastQuestReport;

        public bool IsQuestWorkspace { get { return workspace == 1; } }

        public KaleidoOptimizerReport ActiveReport
        {
            get { return IsQuestWorkspace ? lastQuestReport : lastPcReport; }
        }

        public void StoreReport(KaleidoOptimizerReport report)
        {
            lastReport = report;
            if (IsQuestWorkspace) lastQuestReport = report;
            else lastPcReport = report;
        }

        public bool WorkspaceReadyToApply
        {
            get { return IsQuestWorkspace ? readyToApplyQuest : readyToApplyPc; }
            set
            {
                if (IsQuestWorkspace) readyToApplyQuest = value;
                else readyToApplyPc = value;
                readyToApply = readyToApplyPc || readyToApplyQuest;
            }
        }

        public bool ReadyToApplyMaxSizesOnly
        {
            get { return IsQuestWorkspace ? readyToApplyMaxSizesQuest : readyToApplyMaxSizesPc; }
            set
            {
                if (IsQuestWorkspace) readyToApplyMaxSizesQuest = value;
                else readyToApplyMaxSizesPc = value;
            }
        }

        public void ClearReadyToApply()
        {
            readyToApply = false;
            readyToApplyPc = false;
            readyToApplyQuest = false;
            readyToApplyMaxSizesPc = false;
            readyToApplyMaxSizesQuest = false;
        }

        public List<KaleidoModelInventoryItem> inventory = new List<KaleidoModelInventoryItem>();
        public string inventorySignature = "";
        public Vector2 inventoryScroll;
        public Vector2 inventoryModelFileScroll;
        public List<KaleidoTextureUsage> textureUsages = new List<KaleidoTextureUsage>();
        public Vector2 textureUsageScroll;
        public string previewTexturePath = "";
        public List<KaleidoTextureSizeEdit> textureSizeHistory = new List<KaleidoTextureSizeEdit>();
        public int textureSizeHistoryIndex = -1;
        public int writeDefaultsAction;

        [MenuItem("KaleidoVR/VRChat Model Optimizer", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<KaleidoVRCOptimizer>("VRChat Model Optimizer");
            window.InitializeLocalLogo();
            window.LoadEditorPreferences();
            window.tab = 0;
            window.minSize = new Vector2(500, 720);
            window.ApplyWindowIcon();
        }

        private void OnEnable()
        {
            InitializeLocalLogo();
            LoadEditorPreferences();
            tab = 0;
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
            pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
            androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
            textureDisableReadWrite = true;
            textureApplyMipmaps = true;
            textureEnableMipmaps = true;
            textureEnableStreamingMipmaps = true;
            textureDisableCrunch = true;
            textureEnableCrunch = false;
            textureApplyAniso = true;
            textureAniso = 1;
            autoDetectNormalMaps = true;
            autoLinearMaskMaps = true;
            higherQualityNormalMaps = true;
            alphaIsTransparencyOnAlbedo = false;

            optimizeMeshes = true;
            meshEnableReadWrite = true;
            meshOptimizePolygons = true;
            meshOptimizeVertices = true;
            meshWeldVertices = false;
            meshKeepBlendShapes = true;
            meshDisableQuads = true;
            meshDisableLightmapUVs = true;
            meshDisableImportLightsCameras = true;
            meshOptimizeAnimation = false;
            applySkinWeights = false;
            applyMeshCompression = false;
            meshCompression = KaleidoMeshCompressionChoice.Off;
            meshForceHumanoid = false;
            meshStripBlendShapes = false;

            optimizeRenderers = true;
            rendererDisableUpdateWhenOffscreen = true;
            rendererDisableShadows = false;
            rendererDisableReceiveShadows = false;
            rendererDisableProbes = false;
            rendererDisableMotionVectors = true;
            rendererForceBone4 = false;
            rendererRecalculateBounds = false;
            applyToPrefabAssets = true;
            optimizeParticles = false;
            disableLightsOnAvatar = false;

            optimizeAudio = true;
            audioForceToMono = false;
            audioLoadInBackground = true;
            audioApplyVorbis = false;
            audioQuality = 0.7f;

            optimizeAnimators = true;
            animatorCullWhenOffscreen = true;
            optimizeMaterials = false;
            materialEnableGpuInstancing = false;
            disableCamerasOnAvatar = false;
            optimizeSceneExtras = false;
            dryRun = true;
            ClearReadyToApply();
            if (index == 0) workspace = 0;
            else if (index == 1) workspace = 1;

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
                applySkinWeights = true;
                skinWeights = KaleidoSkinWeightChoice.FourBones;
                rendererDisableShadows = true;
                rendererForceBone4 = true;
            }
            else
            {
                SetTypeSizes(2048, 1024, 2048, 1024, 1024, 512, 1024, 512, 512, 256, 1024, 512);
                skinWeights = KaleidoSkinWeightChoice.FourBones;
                rendererDisableShadows = false;
                rendererForceBone4 = false;
                if (index == 3)
                {
                    meshOptimizeAnimation = true;
                    audioApplyVorbis = true;
                    optimizeParticles = true;
                    optimizeSceneExtras = true;
                }
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
                textureEnableStreamingMipmaps = textureEnableStreamingMipmaps,
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
                textureEnableStreamingMipmaps = p.textureEnableStreamingMipmaps;
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
                applyToPrefabAssets = p.applyToPrefabAssets;
                optimizeParticles = p.optimizeParticles;
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
                rendererRecalculateBounds = p.rendererRecalculateBounds;
                optimizeMaterials = p.optimizeMaterials;
                materialEnableGpuInstancing = p.materialEnableGpuInstancing;
                disableLightsOnAvatar = p.disableLightsOnAvatar;
                disableCamerasOnAvatar = p.disableCamerasOnAvatar;
                audioForceToMono = p.audioForceToMono;
                textureEnableCrunch = p.textureEnableCrunch;
            }
            else
            {
                applyMeshCompression = false;
                meshForceHumanoid = false;
                meshStripBlendShapes = false;
                rendererRecalculateBounds = false;
                optimizeMaterials = false;
                materialEnableGpuInstancing = false;
                disableLightsOnAvatar = false;
                disableCamerasOnAvatar = false;
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
            workspace = GetInt("Workspace", 0);
            textureSort = GetInt("TexSort", 0);
            builtinPresetIndex = GetInt("Builtin", 2);
            selectedUserProfile = GetInt("UserProf", -1);
            newProfileName = EditorPrefs.HasKey(PrefsPrefix + "NewName") ? EditorPrefs.GetString(PrefsPrefix + "NewName") : "My Profile";
            dryRun = true;
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
            pcTexFormat = (KaleidoPcTexFormat)GetInt("PcFmt", (int)KaleidoPcTexFormat.AutoBc7Dxt1);
            androidTexFormat = (KaleidoAndroidTexFormat)GetInt("AndFmt", (int)KaleidoAndroidTexFormat.ASTC_6x6);
            textureDisableReadWrite = GetBool("TexRW", true);
            textureApplyMipmaps = GetBool("TexMipsOn", true);
            textureEnableMipmaps = GetBool("TexMips", true);
            textureEnableStreamingMipmaps = GetBool("TexStreamOn", true);
            textureDisableCrunch = GetBool("TexCrunch", true);
            textureEnableCrunch = GetBool("TexCrunchOn", false);
            textureApplyAniso = GetBool("TexAnisoOn", true);
            textureAniso = GetInt("TexAniso", 1);
            autoDetectNormalMaps = GetBool("TexNorm", true);
            autoLinearMaskMaps = GetBool("TexLinear", true);
            higherQualityNormalMaps = GetBool("TexNormHQ", true);
            alphaIsTransparencyOnAlbedo = GetBool("TexAlpha", false);

            optimizeMeshes = GetBool("OptMesh", true);
            applyMeshCompression = GetBool("MeshCompOn", false);
            meshCompression = (KaleidoMeshCompressionChoice)GetInt("MeshComp", (int)KaleidoMeshCompressionChoice.Off);
            meshEnableReadWrite = GetBool("MeshEnableRW", true);
            meshOptimizePolygons = GetBool("MeshPoly", true);
            meshOptimizeVertices = GetBool("MeshVert", true);
            meshWeldVertices = GetBool("MeshWeld", false);
            meshKeepBlendShapes = GetBool("MeshBS", true);
            meshStripBlendShapes = GetBool("MeshBSOff", false);
            meshDisableQuads = GetBool("MeshQuads", true);
            meshDisableLightmapUVs = GetBool("MeshLM", true);
            meshDisableImportLightsCameras = GetBool("MeshLC", true);
            meshOptimizeAnimation = GetBool("MeshAnim", false);
            applySkinWeights = GetBool("SkinOn", false);
            skinWeights = (KaleidoSkinWeightChoice)GetInt("SkinW", (int)KaleidoSkinWeightChoice.FourBones);
            meshForceHumanoid = GetBool("MeshHum", false);

            optimizeRenderers = GetBool("OptRend", true);
            rendererDisableUpdateWhenOffscreen = GetBool("RendOff", true);
            rendererDisableShadows = GetBool("RendShad", false);
            rendererDisableReceiveShadows = GetBool("RendRecv", false);
            rendererDisableProbes = GetBool("RendProbe", false);
            rendererDisableMotionVectors = GetBool("RendMV", true);
            rendererForceBone4 = GetBool("RendBone4", false);
            rendererRecalculateBounds = GetBool("RendBounds", false);

            optimizeAudio = GetBool("OptAud", true);
            audioForceToMono = GetBool("AudMono", false);
            audioLoadInBackground = GetBool("AudBG", true);
            audioApplyVorbis = GetBool("AudVorb", false);
            audioQuality = EditorPrefs.HasKey(PrefsPrefix + "AudQ") ? EditorPrefs.GetFloat(PrefsPrefix + "AudQ") : 0.7f;

            optimizeAnimators = GetBool("OptAnim", true);
            animatorCullWhenOffscreen = GetBool("AnimCull", true);

            optimizeMaterials = GetBool("OptMat", false);
            materialEnableGpuInstancing = GetBool("MatGPU", false);

            optimizeSceneExtras = GetBool("OptExtra", false);
            disableLightsOnAvatar = GetBool("ExtraLight", false);
            disableCamerasOnAvatar = GetBool("ExtraCam", false);
            optimizeParticles = GetBool("ExtraPart", false);

            MigrateEditorPreferences();
        }

        private void MigrateEditorPreferences()
        {
            const int currentSchema = 2;
            int schema = GetInt("Schema", 1);
            if (schema >= currentSchema) return;

            if (schema < 2)
            {
                if (pcTexFormat == KaleidoPcTexFormat.HighQuality)
                    pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
                autoDetectNormalMaps = true;
            }

            SetInt("Schema", currentSchema);
            SetInt("PcFmt", (int)pcTexFormat);
            SetBool("TexNorm", autoDetectNormalMaps);
        }

        public void SaveEditorPreferences()
        {
            SetInt("Schema", 2);
            SetInt("Workspace", workspace);
            SetInt("TexSort", textureSort);
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
            SetBool("TexStreamOn", textureEnableStreamingMipmaps);
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
                WorkspaceReadyToApply = false;
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
        public bool insideModelFile;
    }

    [Serializable]
    public class KaleidoTextureLink
    {
        public UnityEngine.Object sceneObject;
        public Material material;
        public string propertyName;
    }

    [Serializable]
    public class KaleidoTextureUsage
    {
        public string path;
        public Texture texture;
        public KaleidoTextureKind kind;
        public int sourceWidth;
        public int sourceHeight;
        public int currentPc;
        public int currentQuest;
        public int pcSize;
        public int questSize;
        public bool usedCustomPc;
        public bool usedCustomQuest;
        public int pcRevertSize;
        public int questRevertSize;
        public List<KaleidoTextureLink> links = new List<KaleidoTextureLink>();
        public long vramBytes;
        public string formatLabel = "";
        public bool isActive;
        public bool fromAnimationSwap;
        public bool crunched;
        public bool missingStreamingMipmaps;
    }

    [Serializable]
    public class KaleidoTextureSizeEdit
    {
        public string path;
        public bool questPlatform;
        public int fromSize;
        public int toSize;
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
        public int unityConstraints;
        public int vrcConstraints;
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
        public long textureVramAll;
        public long textureVramActive;
        public long textureVramPlanned;
        public long meshVramAll;
        public long meshVramActive;
        public long vramAll;
        public long vramActive;
        public string textureVramQuality = "—";
        public string meshVramQuality = "—";
        public int grabPasses;
        public string grabPassQuality = "—";
        public List<string> grabPassShaders = new List<string>();
        public long blendshapeTriangles;
        public int blendshapeMeshes;
        public string blendshapeQuality = "—";
        public List<string> blendshapeMeshLines = new List<string>();
        public int anyStateTransitions;
        public string anyStateQuality = "—";
        public int animatorLayers;
        public string layerCountQuality = "—";
        public bool writeDefaultsMixed;
        public bool writeDefaultsMostlyOn;
        public int writeDefaultsOnCount;
        public int writeDefaultsOffCount;
        public List<string> writeDefaultOutliers = new List<string>();
        public int emptyStateCount;
        public List<string> emptyStates = new List<string>();
        public List<string> crunchedTextures = new List<string>();
        public List<string> nonBc5Normals = new List<string>();
        public List<string> materialSwapNames = new List<string>();
        public int missingStreamingCount;
        public List<string> missingStreamingMipmaps = new List<string>();
    }

    public static class KaleidoVRCOptimizerUI
    {
        private static readonly int[] TextureSizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        private static readonly string[] TextureSizeLabels = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
        private static readonly string[] TextureSortLabels =
        {
            "Largest resolution first",
            "Smallest resolution first",
            "Name A–Z",
            "Name Z–A",
            "Type, then name",
            "Largest VRAM first"
        };
        private static Rect pendingOutlineRect;
        private static readonly string[] BuiltinNames = { "PC", "Quest", "Dual Platform", "Everything" };
        private static readonly string[] BuiltinSummaries =
        {
            "Booth-safe PC start. Caps maps at 2K, Auto BC7/DXT1 (BC5 normals), keeps blend shapes and mesh Read/Write, does not weld verts or rewrite bone weights. Special stays off.",
            "Quest start. 1K body maps, ASTC 6x6, 4 bone weights, shadow casting off. Still will not weld, strip visemes, or touch Special. Test hair/toggles after apply.",
            "Booth-safe dual start. 2K PC / 1K Quest body maps, Auto BC7/DXT1 on PC and ASTC 6x6 on Quest. Does not rewrite skin weights, weld, or force 4-bone quality. Special stays off.",
            "Full pack. Dual 2K/1K caps, Auto BC7/DXT1, ASTC 6x6, BC5 normals, Vorbis SFX, particle shadow strip. Scan/Rank includes VRAM, GrabPass, animator, crunch, and animation-swap flags. Special stays off. Uncheck anything you do not want before Apply."
        };
        private static readonly string[] PcFormatLabels =
        {
            "Auto (BC7 / DXT1)",
            "Unity Compressed HQ",
            "BC7",
            "DXT5",
            "DXT1",
            "BC5",
            "Unity Automatic"
        };
        private static readonly int[] PcFormatValues =
        {
            (int)KaleidoPcTexFormat.AutoBc7Dxt1,
            (int)KaleidoPcTexFormat.HighQuality,
            (int)KaleidoPcTexFormat.BC7,
            (int)KaleidoPcTexFormat.DXT5,
            (int)KaleidoPcTexFormat.DXT1,
            (int)KaleidoPcTexFormat.BC5,
            (int)KaleidoPcTexFormat.Automatic
        };

        private static bool evalVramOpen = true;
        private static bool evalHiddenOpen = true;
        private static bool evalFlagsOpen = true;
        private static bool evalWdOpen;
        private static bool evalEmptyOpen;
        private static bool evalGrabOpen;
        private static bool evalBlendOpen;
        private static GUIStyle miniWrap;
        private static GUIStyle dropTitleStyle;
        private static GUIStyle dropHintStyle;
        private static GUIStyle pingLinkStyle;
        private static GUIStyle sizeCaptionStyle;
        private static GUIStyle sizeValueStyle;
        private static GUIStyle sizeNewStyle;
        private static GUIStyle sizeUpStyle;
        private static bool cachedDropProSkin = true;

        private static GUIStyle MiniWrap()
        {
            if (miniWrap == null)
            {
                miniWrap = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            }
            return miniWrap;
        }

        private static void EnsureDropStyles()
        {
            bool pro = EditorGUIUtility.isProSkin;
            if (dropTitleStyle != null && cachedDropProSkin == pro) return;
            cachedDropProSkin = pro;

            dropTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                wordWrap = true
            };
            dropTitleStyle.normal.textColor = pro ? new Color(0.45f, 0.92f, 1f, 1f) : new Color(0.05f, 0.38f, 0.62f, 1f);

            dropHintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            dropHintStyle.normal.textColor = pro ? new Color(0.82f, 0.92f, 0.42f, 1f) : new Color(0.28f, 0.45f, 0.02f, 1f);
        }

        private static GUIStyle DropTitleStyle()
        {
            EnsureDropStyles();
            return dropTitleStyle;
        }

        private static GUIStyle DropHintStyle()
        {
            EnsureDropStyles();
            return dropHintStyle;
        }

        private static void EnsureSizeStyles()
        {
            if (sizeCaptionStyle != null) return;
            bool pro = EditorGUIUtility.isProSkin;
            sizeCaptionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };
            sizeCaptionStyle.normal.textColor = pro ? new Color(0.72f, 0.76f, 0.80f, 1f) : new Color(0.25f, 0.28f, 0.32f, 1f);

            sizeValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12
            };

            sizeNewStyle = new GUIStyle(sizeValueStyle);
            sizeNewStyle.normal.textColor = pro ? new Color(0.45f, 0.92f, 1f, 1f) : new Color(0.05f, 0.42f, 0.70f, 1f);

            sizeUpStyle = new GUIStyle(sizeValueStyle);
            sizeUpStyle.normal.textColor = pro ? new Color(1f, 0.72f, 0.28f, 1f) : new Color(0.72f, 0.38f, 0.04f, 1f);
        }

        private static GUIStyle PingLinkStyle()
        {
            if (pingLinkStyle == null)
            {
                pingLinkStyle = new GUIStyle(EditorStyles.linkLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    wordWrap = false,
                    fixedHeight = 18
                };
            }
            return pingLinkStyle;
        }

        private static void DrawBoxOutline(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), color);
        }

        private static void BeginOutlinedPanel()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(84);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            GUILayout.Space(6);
        }

        private static void EndOutlinedPanel()
        {
            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
                pendingOutlineRect = GUILayoutUtility.GetLastRect();
            DrawBoxOutline(pendingOutlineRect, new Color(0.38f, 0.78f, 1f, 0.95f));
            GUILayout.Space(84);
            EditorGUILayout.EndHorizontal();
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
            DrawWorkspaceBar(window);
            DrawTabRow(window, new[] { "Setup", "Profiles", "Rank", "Textures" }, 0);
            DrawTabRow(window, new[] { "Meshes", "Scene", "Special" }, 4);
        }

        private static void DrawWorkspaceBar(KaleidoVRCOptimizer window)
        {
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            bool pcOn = !window.IsQuestWorkspace;
            if (GUILayout.Toggle(pcOn, "PC Workspace", EditorStyles.miniButton, GUILayout.Height(28), GUILayout.ExpandWidth(true)) && !pcOn)
                window.workspace = 0;
            if (GUILayout.Toggle(!pcOn, "Quest / Android Workspace", EditorStyles.miniButton, GUILayout.Height(28), GUILayout.ExpandWidth(true)) && pcOn)
                window.workspace = 1;
            EditorGUILayout.EndHorizontal();

            if (window.IsQuestWorkspace)
            {
                EditorGUILayout.HelpBox("Scan, Dry Run, and Apply only write Android texture overrides and the renderer settings shown here. The other workspace is left alone.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Scan, Dry Run, and Apply only write Standalone / default importer sizes and the scene options shown here. Android overrides stay untouched until you switch workspaces.", MessageType.Info);
            }
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
            DrawWhy("Start here. Drop one VRChat avatar. Everything this window lists or changes comes from that model only.");
            DrawObjectList(window.targets, "Drop your VRChat avatar here", true, true, true);

            window.RefreshInventoryIfNeeded();
            DrawModelContents(window);

            GUILayout.Space(8);
            GUILayout.Label("Ignore List", EditorStyles.boldLabel);
            DrawWhy("Anything here is left untouched, including its dependent textures and meshes.");
            DrawObjectList(window.ignoreList, "Drag & Drop Assets To Leave Untouched", false, false);

            GUILayout.Space(8);
            GUILayout.Label("Run Safety", EditorStyles.boldLabel);
            DrawWhy("Each workspace has its own Scan / Dry Run / Apply. A run in one workspace does not write the other.");
            window.writeLog = DrawToggle(window.writeLog, "Write Log File", "Saves a timestamped report under Logs/KaleidoVR/Optimizer.");
            window.applyToPrefabAssets = DrawToggle(window.applyToPrefabAssets, "Apply renderer changes to prefab assets", "Writes Scene-tab renderer edits onto the .prefab, not only the scene instance. Turn off to test on the instance first.");
        }

        private static void DrawProfilesTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("Profiles", EditorStyles.boldLabel);
            DrawWhy("A profile is a saved set of tab settings. Pick a built-in to start, tick which tabs it should change, then save your own if you want to reuse it.");

            EditorGUILayout.HelpBox(DescribeActiveProfile(window), MessageType.Info);

            GUILayout.Space(8);
            GUILayout.Label("1. Start from a built-in", EditorStyles.boldLabel);
            DrawWhy("Click one to fill every tab with that recipe. Everything is the full recommended pack. Special stays off. You can still uncheck options afterward.");
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < BuiltinNames.Length; i++)
            {
                bool on = window.selectedUserProfile < 0 && window.builtinPresetIndex == i;
                if (GUILayout.Toggle(on, BuiltinNames[i], EditorStyles.miniButton, GUILayout.Height(26), GUILayout.ExpandWidth(true)))
                {
                    if (window.selectedUserProfile >= 0 || window.builtinPresetIndex != i)
                    {
                        window.ApplyBuiltinPreset(i);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(BuiltinSummaries[Mathf.Clamp(window.builtinPresetIndex, 0, BuiltinSummaries.Length - 1)], MiniWrap());

            GUILayout.Space(10);
            GUILayout.Label("2. Or open a profile you saved", EditorStyles.boldLabel);
            DrawWhy("Saved profiles live in this Unity project (EditorPrefs). Choosing one applies it immediately, but only overwrites the tabs ticked in step 3. Use step 5 to export, back up, or import a JSON file.");

            string[] userNames = GetUserProfileNames(window);
            int userIndex = window.selectedUserProfile + 1;
            int picked = EditorGUILayout.Popup("Your profiles", userIndex, userNames);
            if (picked != userIndex)
            {
                window.selectedUserProfile = picked - 1;
                if (window.selectedUserProfile >= 0)
                {
                    window.LoadProfile(window.userProfiles.profiles[window.selectedUserProfile], true);
                    window.newProfileName = window.userProfiles.profiles[window.selectedUserProfile].name;
                }
            }

            if (window.selectedUserProfile >= 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply again", GUILayout.Height(22)))
                {
                    window.LoadProfile(window.userProfiles.profiles[window.selectedUserProfile], true);
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("3. When this profile loads, change these tabs", EditorStyles.boldLabel);
            DrawWhy("Unchecked tabs keep whatever you already set. Tick only what this recipe should overwrite. Special never runs unless you tick it.");

            window.includeTextures = EditorGUILayout.ToggleLeft("Textures  —  max sizes and compression for the active workspace", window.includeTextures);
            window.includeMeshes = EditorGUILayout.ToggleLeft("Meshes tab  —  read/write, weld, blend shapes, skin weights", window.includeMeshes);
            window.includeRenderers = EditorGUILayout.ToggleLeft("Scene  —  offscreen, probes, particles, shadows, 4-bone quality", window.includeRenderers);
            window.includeAudio = EditorGUILayout.ToggleLeft("Scene tab audio  —  load in background, Vorbis", window.includeAudio);
            window.includeAnimators = EditorGUILayout.ToggleLeft("Scene tab animators  —  cull when offscreen", window.includeAnimators);
            DrawSpecialUseCaseHeader();
            window.includeSpecial = EditorGUILayout.ToggleLeft("Special tab  —  mesh compression, Humanoid, strip shapes, GPU instancing, lights, cameras, mono, crunch", window.includeSpecial);
            DrawWhy("Leave this off unless you intend those high-risk writes. Built-in profiles never include Special.");

            GUILayout.Space(10);
            GUILayout.Label("4. Save the current tabs as your profile", EditorStyles.boldLabel);
            DrawWhy("Stores the settings that are on the tabs right now, plus the checkboxes above.");

            window.newProfileName = EditorGUILayout.TextField("Name", window.newProfileName);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save as new", GUILayout.Height(24)))
            {
                SaveNewProfile(window);
            }
            EditorGUI.BeginDisabledGroup(window.selectedUserProfile < 0);
            if (GUILayout.Button("Overwrite selected", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog(
                    "Overwrite profile?",
                    "Replace \"" + SelectedProfileName(window) + "\" with the settings currently on the tabs?",
                    "Overwrite",
                    "Cancel"))
                {
                    OverwriteSelectedProfile(window);
                }
            }
            if (GUILayout.Button("Delete selected", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete profile?",
                    "Delete \"" + SelectedProfileName(window) + "\"? This cannot be undone.",
                    "Delete",
                    "Cancel"))
                {
                    DeleteSelectedProfile(window);
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("5. Share, backup, or import", EditorStyles.boldLabel);
            DrawWhy("Save a JSON file you can send to someone else, keep as a backup, or import on another PC. Import adds profiles to this project and never overwrites a name you already have.");

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(window.selectedUserProfile < 0);
            if (GUILayout.Button("Export selected", GUILayout.Height(24)))
            {
                ExportProfiles(window, false);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(window.userProfiles == null || window.userProfiles.profiles == null || window.userProfiles.profiles.Length == 0);
            if (GUILayout.Button("Backup all", GUILayout.Height(24)))
            {
                ExportProfiles(window, true);
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Import…", GUILayout.Height(24)))
            {
                ImportProfiles(window);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string DescribeActiveProfile(KaleidoVRCOptimizer window)
        {
            if (window.selectedUserProfile >= 0)
            {
                return "Active: your profile \"" + SelectedProfileName(window) + "\". Other tabs match this save, except tabs you left unticked in step 3.";
            }
            string builtin = BuiltinNames[Mathf.Clamp(window.builtinPresetIndex, 0, BuiltinNames.Length - 1)];
            return "Active: built-in " + builtin + ". Special is off. Use step 4 if you want to keep your tweaks.";
        }

        private static string SelectedProfileName(KaleidoVRCOptimizer window)
        {
            if (window.selectedUserProfile < 0 || window.userProfiles == null || window.userProfiles.profiles == null)
                return "";
            if (window.selectedUserProfile >= window.userProfiles.profiles.Length) return "";
            KaleidoOptimizerProfile profile = window.userProfiles.profiles[window.selectedUserProfile];
            if (profile == null || string.IsNullOrEmpty(profile.name)) return "(unnamed)";
            return profile.name;
        }

        private static string[] GetUserProfileNames(KaleidoVRCOptimizer window)
        {
            var names = new List<string> { "None — using the built-in above" };
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
            string name = window.newProfileName != null ? window.newProfileName.Trim() : "";
            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog("Name this profile", "Type a name before saving.", "OK");
                return;
            }
            List<KaleidoOptimizerProfile> list = new List<KaleidoOptimizerProfile>();
            if (window.userProfiles != null && window.userProfiles.profiles != null) list.AddRange(window.userProfiles.profiles);
            KaleidoOptimizerProfile created = window.CaptureProfile(name);
            list.Add(created);
            window.userProfiles.profiles = list.ToArray();
            window.selectedUserProfile = list.Count - 1;
            window.newProfileName = name;
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

        private const string ProfileFilePrefsKey = "ProfileDir";

        private static string LastProfileDirectory()
        {
            string stored = EditorPrefs.GetString(KaleidoVRCOptimizer.PrefsPrefix + ProfileFilePrefsKey, "");
            if (!string.IsNullOrEmpty(stored) && Directory.Exists(stored)) return stored;
            try
            {
                return KaleidoVRCOptimizerHelpers.GetProjectRootPath();
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static void RememberProfileDirectory(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) EditorPrefs.SetString(KaleidoVRCOptimizer.PrefsPrefix + ProfileFilePrefsKey, dir);
        }

        private static string SanitizeProfileFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Profile";
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Trim());
            for (int i = 0; i < sb.Length; i++)
            {
                for (int c = 0; c < invalid.Length; c++)
                {
                    if (sb[i] == invalid[c])
                    {
                        sb[i] = '_';
                        break;
                    }
                }
            }
            string cleaned = sb.ToString().Trim();
            return string.IsNullOrEmpty(cleaned) ? "Profile" : cleaned;
        }

        private static KaleidoOptimizerProfile CloneProfile(KaleidoOptimizerProfile profile)
        {
            if (profile == null) return null;
            return JsonUtility.FromJson<KaleidoOptimizerProfile>(JsonUtility.ToJson(profile));
        }

        private static KaleidoOptimizerProfileFile BuildProfileFile(KaleidoOptimizerProfile[] profiles)
        {
            return new KaleidoOptimizerProfileFile
            {
                format = KaleidoOptimizerProfileFile.FormatId,
                formatVersion = 1,
                toolVersion = KaleidoVRCOptimizer.VERSION,
                exportedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                profiles = profiles ?? new KaleidoOptimizerProfile[0]
            };
        }

        private static void ExportProfiles(KaleidoVRCOptimizer window, bool allSaved)
        {
            KaleidoOptimizerProfile[] toWrite;
            string suggested;
            if (allSaved)
            {
                if (window.userProfiles == null || window.userProfiles.profiles == null || window.userProfiles.profiles.Length == 0)
                {
                    EditorUtility.DisplayDialog("Nothing to back up", "Save a profile in step 4 first.", "OK");
                    return;
                }
                toWrite = new KaleidoOptimizerProfile[window.userProfiles.profiles.Length];
                for (int i = 0; i < window.userProfiles.profiles.Length; i++)
                    toWrite[i] = CloneProfile(window.userProfiles.profiles[i]);
                suggested = "KaleidoVR-Optimizer-Profiles-Backup-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json";
            }
            else
            {
                if (window.selectedUserProfile < 0 || window.userProfiles == null || window.userProfiles.profiles == null
                    || window.selectedUserProfile >= window.userProfiles.profiles.Length)
                {
                    EditorUtility.DisplayDialog("No profile selected", "Choose a saved profile in step 2, or save one in step 4.", "OK");
                    return;
                }
                KaleidoOptimizerProfile selected = CloneProfile(window.userProfiles.profiles[window.selectedUserProfile]);
                toWrite = new[] { selected };
                suggested = "KaleidoVR-Optimizer-Profile-" + SanitizeProfileFileName(selected != null ? selected.name : "Profile") + ".json";
            }

            string path = EditorUtility.SaveFilePanel(
                allSaved ? "Backup KaleidoVR optimizer profiles" : "Export KaleidoVR optimizer profile",
                LastProfileDirectory(),
                suggested,
                "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(BuildProfileFile(toWrite), true), new UTF8Encoding(false));
                RememberProfileDirectory(path);
                EditorUtility.DisplayDialog(
                    allSaved ? "Backup saved" : "Profile exported",
                    (allSaved ? "Saved " + toWrite.Length + " profile(s) to:\n" : "Saved to:\n") + path,
                    "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Could not export", ex.Message, "OK");
            }
        }

        private static KaleidoOptimizerProfile[] ParseProfileFile(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            KaleidoOptimizerProfileFile file = JsonUtility.FromJson<KaleidoOptimizerProfileFile>(json);
            if (file != null && file.profiles != null && file.profiles.Length > 0)
                return file.profiles;

            KaleidoOptimizerProfileList list = JsonUtility.FromJson<KaleidoOptimizerProfileList>(json);
            if (list != null && list.profiles != null && list.profiles.Length > 0)
                return list.profiles;

            KaleidoOptimizerProfile one = JsonUtility.FromJson<KaleidoOptimizerProfile>(json);
            if (one != null && !string.IsNullOrWhiteSpace(one.name))
                return new[] { one };

            return null;
        }

        private static string UniqueImportedName(List<KaleidoOptimizerProfile> existing, string desired)
        {
            string baseName = string.IsNullOrWhiteSpace(desired) ? "Imported Profile" : desired.Trim();
            if (!NameExists(existing, baseName)) return baseName;
            string imported = baseName + " (imported)";
            if (!NameExists(existing, imported)) return imported;
            int n = 2;
            while (NameExists(existing, baseName + " (" + n + ")")) n++;
            return baseName + " (" + n + ")";
        }

        private static bool NameExists(List<KaleidoOptimizerProfile> existing, string name)
        {
            if (existing == null) return false;
            for (int i = 0; i < existing.Count; i++)
            {
                KaleidoOptimizerProfile profile = existing[i];
                if (profile != null && string.Equals(profile.name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void ImportProfiles(KaleidoVRCOptimizer window)
        {
            string path = EditorUtility.OpenFilePanel("Import KaleidoVR optimizer profiles", LastProfileDirectory(), "json");
            if (string.IsNullOrEmpty(path)) return;

            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Could not import", ex.Message, "OK");
                return;
            }

            KaleidoOptimizerProfile[] incoming = ParseProfileFile(json);
            if (incoming == null || incoming.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Not a KaleidoVR profile file",
                    "This JSON is not an optimizer profile export. Use Export selected or Backup all from this window.",
                    "OK");
                return;
            }

            List<KaleidoOptimizerProfile> list = new List<KaleidoOptimizerProfile>();
            if (window.userProfiles != null && window.userProfiles.profiles != null)
                list.AddRange(window.userProfiles.profiles);

            int added = 0;
            int firstIndex = list.Count;
            StringBuilder renamed = new StringBuilder();
            for (int i = 0; i < incoming.Length; i++)
            {
                KaleidoOptimizerProfile clone = CloneProfile(incoming[i]);
                if (clone == null) continue;
                string original = string.IsNullOrWhiteSpace(clone.name) ? "Imported Profile" : clone.name.Trim();
                clone.name = UniqueImportedName(list, original);
                if (!string.Equals(clone.name, original, StringComparison.Ordinal))
                    renamed.AppendLine("• " + original + " → " + clone.name);
                list.Add(clone);
                added++;
            }

            if (added == 0)
            {
                EditorUtility.DisplayDialog("Could not import", "No usable profiles were in that file.", "OK");
                return;
            }

            window.userProfiles.profiles = list.ToArray();
            window.selectedUserProfile = firstIndex;
            window.LoadProfile(window.userProfiles.profiles[firstIndex], true);
            window.newProfileName = window.userProfiles.profiles[firstIndex].name;
            window.SaveUserProfiles();
            RememberProfileDirectory(path);

            string message = "Added " + added + " profile(s) from:\n" + path;
            if (renamed.Length > 0) message += "\n\nNames already in use were renamed:\n" + renamed.ToString().TrimEnd();
            EditorUtility.DisplayDialog("Profiles imported", message, "OK");
        }

        private static void DrawRankTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("Performance Snapshot", EditorStyles.boldLabel);
            DrawWhy("VRChat rank plus VRAM, GrabPass, animator cost, and texture flags. Scan or Dry Run fills this tab. Apply still waits for Dry Run.");
            DrawRankColorKey();
            DrawStats(window);
        }

        private static void DrawTexturesTab(KaleidoVRCOptimizer window)
        {
            window.RefreshInventoryIfNeeded();
            KaleidoVRCOptimizerLogic.SyncTextureRowDefaults(window);
            bool quest = window.IsQuestWorkspace;

            window.optimizeTextures = DrawToggle(
                window.optimizeTextures,
                "Process texture importers",
                "This workspace only writes the texture settings shown here. Off = skip every texture write.");
            EditorGUI.BeginDisabledGroup(!window.optimizeTextures);

            EditorGUI.BeginChangeCheck();
            GUILayout.Space(6);
            if (quest)
            {
                GUILayout.Label("Max Size By Type", EditorStyles.boldLabel);
                DrawWhy("Caps each type at this size. Textures already smaller stay as they are. Unticked types keep their current size unless you change that row's selector.");
                DrawTypeSizeRow(ref window.applyAlbedoSize, "Albedo / Diffuse / Main", "Suggested 512–1024.", ref window.albedoQuest);
                DrawTypeSizeRow(ref window.applyNormalSize, "Normal", "Suggested 512–1024.", ref window.normalQuest);
                DrawTypeSizeRow(ref window.applyMaskSize, "Mask / Metallic / Rough / AO / ORM", "Suggested 256–512.", ref window.maskQuest);
                DrawTypeSizeRow(ref window.applyEmissionSize, "Emission", "Suggested 256–512.", ref window.emissionQuest);
                DrawTypeSizeRow(ref window.applyMatcapSize, "Matcap / Ramp / Toon", "Suggested 256–512.", ref window.matcapQuest);
                DrawTypeSizeRow(ref window.applyOtherSize, "Other / Unclassified", "Suggested 512.", ref window.otherQuest);
            }
            else
            {
                GUILayout.Label("Max Size By Type", EditorStyles.boldLabel);
                DrawWhy("Caps each type at this size. Textures already smaller stay as they are. Unticked types keep their current size unless you change that row's selector.");
                DrawTypeSizeRow(ref window.applyAlbedoSize, "Albedo / Diffuse / Main", "Body color maps. Suggested 1024–2048.", ref window.albedoPc);
                DrawTypeSizeRow(ref window.applyNormalSize, "Normal", "Bump maps. Match albedo, or one step below if memory is tight.", ref window.normalPc);
                DrawTypeSizeRow(ref window.applyMaskSize, "Mask / Metallic / Rough / AO / ORM", "Packed masks are blur-tolerant. Suggested 512–1024.", ref window.maskPc);
                DrawTypeSizeRow(ref window.applyEmissionSize, "Emission", "Glow maps. Suggested 512–1024.", ref window.emissionPc);
                DrawTypeSizeRow(ref window.applyMatcapSize, "Matcap / Ramp / Toon", "Tiny lookup textures. Suggested 256–512.", ref window.matcapPc);
                DrawTypeSizeRow(ref window.applyOtherSize, "Other / Unclassified", "Anything that did not match a suffix. Suggested 512–1024.", ref window.otherPc);
            }
            if (EditorGUI.EndChangeCheck()) window.ReadyToApplyMaxSizesOnly = false;

            KaleidoVRCOptimizerLogic.SyncTextureRowDefaults(window);

            GUILayout.Space(8);
            DrawMaxSizesOnlyActions(window, quest);

            GUILayout.Space(8);
            DrawTextureUsageList(window, quest);
            DrawTexturePreview(window);

            GUILayout.Space(8);
            if (quest)
            {
                GUILayout.Label("Format", EditorStyles.boldLabel);
                window.applyAndroidTexFormat = DrawToggle(window.applyAndroidTexFormat, "Set compression format", "ASTC 6x6 is the usual balance. 4x4 is sharper/heavier, 8x8 is cheaper/blurrier.");
                if (window.applyAndroidTexFormat) window.androidTexFormat = (KaleidoAndroidTexFormat)EditorGUILayout.EnumPopup("Format", window.androidTexFormat);
                window.higherQualityNormalMaps = DrawToggle(window.higherQualityNormalMaps, "Higher quality normals (ASTC)", "Uses a sharper ASTC block for detected normals. Does not change the other workspace's format.");
                window.textureEnableStreamingMipmaps = DrawToggle(window.textureEnableStreamingMipmaps, "Enable streaming mip maps", "VRChat expects this on when mip maps are on. The client streams lower mips when VRAM is tight. Avatar streaming priority is ignored (always 0).");
            }
            else
            {
                GUILayout.Label("Importer Settings", EditorStyles.boldLabel);
                window.applyPcTexFormat = DrawToggle(window.applyPcTexFormat, "Set compression", "Auto uses DXT1 on opaque maps (4 bpp) and BC7 when alpha is present (8 bpp). Same VRAM as DXT5 for alpha, better quality. Leave off to keep each texture's current format.");
                if (window.applyPcTexFormat)
                {
                    window.pcTexFormat = (KaleidoPcTexFormat)EditorGUILayout.IntPopup(
                        "Format",
                        (int)window.pcTexFormat,
                        PcFormatLabels,
                        PcFormatValues);
                    if (window.pcTexFormat == KaleidoPcTexFormat.AutoBc7Dxt1)
                        DrawWhy("Dry Run / Apply only. Opaque → DXT1. Alpha or cutout → BC7. Detected normals stay BC5 while Higher quality normals is on.");
                }

                window.textureDisableReadWrite = DrawToggle(window.textureDisableReadWrite, "Disable Read / Write", "Saves RAM. Turn off only if a script or editor tool reads pixels from the texture.");
                window.textureApplyMipmaps = DrawToggle(window.textureApplyMipmaps, "Set mip maps", "Avatars in 3D should generate mip maps. Uncheck to leave each texture as-is.");
                if (window.textureApplyMipmaps) window.textureEnableMipmaps = EditorGUILayout.Toggle("Generate Mip Maps", window.textureEnableMipmaps);
                window.textureEnableStreamingMipmaps = DrawToggle(window.textureEnableStreamingMipmaps, "Enable streaming mip maps", "VRChat expects this on when mip maps are on. The client streams lower mips when VRAM is tight. Avatar streaming priority is ignored (always 0).");
                window.textureDisableCrunch = DrawToggle(window.textureDisableCrunch, "Disable crunch compression", "Crunch does not reduce VRChat texture memory. Enable crunch only on the Special tab.");
                window.textureApplyAniso = DrawToggle(window.textureApplyAniso, "Set anisotropic filtering", "1 is enough for avatars. Higher values cost GPU for little gain up close.");
                if (window.textureApplyAniso) window.textureAniso = EditorGUILayout.IntSlider("Aniso Level", window.textureAniso, 0, 16);
                window.autoDetectNormalMaps = DrawToggle(window.autoDetectNormalMaps, "Detect normal maps by name", "Sets Texture Type to Normal Map when the file looks like _n / _norm / _normal. Prevents sRGB lighting errors.");
                window.higherQualityNormalMaps = DrawToggle(window.higherQualityNormalMaps, "Higher quality normals (BC5)", "Uses BC5 for detected normals. ASTC sharpness is set in the other workspace.");
                window.autoLinearMaskMaps = DrawToggle(window.autoLinearMaskMaps, "Linear color for mask maps", "Turns sRGB off on metallic/rough/AO/ORM so packed masks do not get gamma-crushed.");
                window.alphaIsTransparencyOnAlbedo = DrawToggle(window.alphaIsTransparencyOnAlbedo, "Alpha Is Transparency on albedo", "Only if the albedo has an alpha channel (cutout/transparent clothing).");
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawMaxSizesOnlyActions(KaleidoVRCOptimizer window, bool quest)
        {
            GUILayout.Label("Max Sizes Only", EditorStyles.boldLabel);
            DrawWhy("Runs only the max-size-by-type settings above (and any per-texture selector you already changed). Type caps never raise a texture. Compression, mip maps, meshes, scene, and Special are not touched.");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dry Run Max Sizes Only", GUILayout.Height(26)))
            {
                int count;
                string preview;
                KaleidoVRCOptimizerLogic.PreviewMaxSizesOnly(window, quest, out count, out preview);
                window.ReadyToApplyMaxSizesOnly = count > 0;
                GUI.changed = false;
                EditorUtility.DisplayDialog(
                    "Max sizes only",
                    count == 0
                        ? "No max-size writes. Tick a type above, or change a row selector. Everything else is already at the planned size."
                        : "Would write max size on " + count + " texture(s). Nothing else is included.\n\n" + preview,
                    "OK");
            }
            EditorGUI.BeginDisabledGroup(!window.ReadyToApplyMaxSizesOnly);
            if (GUILayout.Button("Apply Max Sizes Only", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog(
                    "Apply max sizes only?",
                    "This writes max texture size for ticked types (and custom row selectors). It will not change compression, mip maps, Read/Write, meshes, or scene settings.",
                    "Apply sizes only",
                    "Cancel"))
                {
                    int written;
                    string preview;
                    KaleidoVRCOptimizerLogic.ApplyMaxSizesOnly(window, quest, out written, out preview);
                    window.ReadyToApplyMaxSizesOnly = false;
                    EditorUtility.DisplayDialog(
                        "Max sizes applied",
                        written == 0
                            ? "No textures needed a max-size write."
                            : "Wrote max size on " + written + " texture(s).\n\n" + preview,
                        "OK");
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            if (!window.ReadyToApplyMaxSizesOnly)
                EditorGUILayout.HelpBox("Dry run these max sizes first. Apply stays off until that dry run finds at least one size write.", MessageType.None);
        }

        private static void DrawTextureUsageList(KaleidoVRCOptimizer window, bool questPlatform)
        {
            GUILayout.Label("Textures On This Model", EditorStyles.boldLabel);
            DrawWhy("Max size for this workspace. Current is what Unity has now. New is what the selector will write. Changing the selector reimports that texture.");

            if (window.textureUsages == null || window.textureUsages.Count == 0)
            {
                EditorGUILayout.HelpBox("Drop an avatar on Setup. Its textures will list here.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Sort", GUILayout.Width(36));
            window.textureSort = EditorGUILayout.Popup(Mathf.Clamp(window.textureSort, 0, TextureSortLabels.Length - 1), TextureSortLabels);
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(window.textureSizeHistoryIndex < 0);
            if (GUILayout.Button("Revert", GUILayout.Width(70), GUILayout.Height(22)))
            {
                KaleidoVRCOptimizerLogic.RevertLastTextureSize(window);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(window.textureSizeHistory == null || window.textureSizeHistoryIndex >= window.textureSizeHistory.Count - 1);
            if (GUILayout.Button("Forward", GUILayout.Width(70), GUILayout.Height(22)))
            {
                KaleidoVRCOptimizerLogic.ForwardTextureSize(window);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            DrawWhy("Revert undoes the last max-size write. Forward reapplies it. One texture change per click.");

            List<KaleidoTextureUsage> rows = SortedTextureUsages(window);
            const float TextureListHeight = 400f;
            BeginOutlinedPanel();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18);
            EditorGUILayout.BeginVertical();
            window.textureUsageScroll = EditorGUILayout.BeginScrollView(window.textureUsageScroll, GUILayout.Height(TextureListHeight));

            GUIStyle headerRight = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(60);
            GUILayout.Label("Texture", EditorStyles.miniBoldLabel, GUILayout.MinWidth(120), GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Current max size", headerRight, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", GUILayout.Width(88));
            GUILayout.FlexibleSpace();
            GUILayout.Label("New max size", EditorStyles.miniBoldLabel, GUILayout.Width(100));
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < rows.Count; i++)
            {
                KaleidoTextureUsage usage = rows[i];
                if (usage == null) continue;

                bool selected = !string.IsNullOrEmpty(window.previewTexturePath)
                    && string.Equals(window.previewTexturePath, usage.path, StringComparison.OrdinalIgnoreCase);

                EditorGUILayout.BeginVertical(selected ? EditorStyles.helpBox : GUI.skin.box);
                EditorGUILayout.BeginHorizontal();

                Rect thumb = GUILayoutUtility.GetRect(52, 52, GUILayout.Width(52), GUILayout.Height(52));
                if (usage.texture != null)
                {
                    if (GUI.Button(thumb, usage.texture))
                    {
                        if (selected) window.previewTexturePath = "";
                        else
                        {
                            window.previewTexturePath = usage.path;
                            EditorGUIUtility.PingObject(usage.texture);
                        }
                    }
                }
                else
                {
                    GUI.Box(thumb, "?");
                }

                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
                GUILayout.Label(usage.texture != null ? usage.texture.name : Path.GetFileName(usage.path), EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                GUILayout.Label(KindLabel(usage.kind), EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                if (usage.vramBytes > 0 || !string.IsNullOrEmpty(usage.formatLabel))
                {
                    string vram = usage.vramBytes > 0 ? KaleidoVRCOptimizerHelpers.FormatBytes(usage.vramBytes) : "";
                    string fmt = string.IsNullOrEmpty(usage.formatLabel) ? "" : usage.formatLabel;
                    string extra = (fmt + "  " + vram).Trim();
                    if (usage.fromAnimationSwap) extra += "  swap";
                    if (usage.crunched) extra += "  crunch";
                    if (usage.missingStreamingMipmaps) extra += "  no stream";
                    GUILayout.Label(extra, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Set", GUILayout.Width(28));
                HandleTextureSizePopup(window, usage, questPlatform);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                DrawTextureSizeStatus(window, usage, questPlatform);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.Space(18);
            EditorGUILayout.EndHorizontal();
            EndOutlinedPanel();
        }

        private static List<KaleidoTextureUsage> SortedTextureUsages(KaleidoVRCOptimizer window)
        {
            List<KaleidoTextureUsage> rows = new List<KaleidoTextureUsage>();
            if (window.textureUsages != null)
            {
                for (int i = 0; i < window.textureUsages.Count; i++)
                {
                    if (window.textureUsages[i] != null) rows.Add(window.textureUsages[i]);
                }
            }
            rows.Sort(delegate (KaleidoTextureUsage a, KaleidoTextureUsage b)
            {
                int areaA = Math.Max(0, a.sourceWidth) * Math.Max(0, a.sourceHeight);
                int areaB = Math.Max(0, b.sourceWidth) * Math.Max(0, b.sourceHeight);
                string nameA = a.texture != null ? a.texture.name : a.path;
                string nameB = b.texture != null ? b.texture.name : b.path;
                switch (window.textureSort)
                {
                    case 1: return areaA.CompareTo(areaB);
                    case 2: return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                    case 3: return string.Compare(nameB, nameA, StringComparison.OrdinalIgnoreCase);
                    case 4:
                        int kind = ((int)a.kind).CompareTo((int)b.kind);
                        return kind != 0 ? kind : string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                    case 5:
                        int vram = b.vramBytes.CompareTo(a.vramBytes);
                        return vram != 0 ? vram : string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                    default:
                        int largest = areaB.CompareTo(areaA);
                        return largest != 0 ? largest : string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
                }
            });
            return rows;
        }

        private static void DrawTextureSizeStatus(KaleidoVRCOptimizer window, KaleidoTextureUsage usage, bool questPlatform)
        {
            EnsureSizeStyles();
            bool typeApply;
            int typePc;
            int typeQuest;
            KaleidoVRCOptimizerLogic.GetTypeSizes(window, usage.kind, out typeApply, out typePc, out typeQuest);
            int current = questPlatform ? usage.currentQuest : usage.currentPc;
            int planned = KaleidoVRCOptimizerLogic.GetPlannedRowSize(window, usage, questPlatform);
            bool willWrite = questPlatform ? (usage.usedCustomQuest || typeApply) : (usage.usedCustomPc || typeApply);
            int revert = questPlatform ? usage.questRevertSize : usage.pcRevertSize;
            bool increasing = revert > 0 && planned > revert;
            bool changing = (willWrite && planned != current) || increasing;
            GUIStyle changeStyle = increasing ? sizeUpStyle : sizeNewStyle;

            GUIStyle currentCaption = new GUIStyle(sizeCaptionStyle) { alignment = TextAnchor.MiddleRight };
            GUIStyle currentValue = new GUIStyle(sizeValueStyle) { alignment = TextAnchor.MiddleRight };
            GUIStyle midCaption = new GUIStyle(sizeCaptionStyle) { alignment = TextAnchor.MiddleCenter };
            GUIStyle midArrow = new GUIStyle(sizeValueStyle) { alignment = TextAnchor.MiddleCenter };
            if (changing)
            {
                midCaption.normal.textColor = changeStyle.normal.textColor;
                midArrow.normal.textColor = changeStyle.normal.textColor;
            }

            const float SizeCol = 100f;
            const float MidCol = 88f;

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(SizeCol), GUILayout.MaxWidth(SizeCol), GUILayout.ExpandWidth(false));
            GUILayout.Label("Current", currentCaption, GUILayout.Width(SizeCol));
            GUILayout.Label(current > 0 ? current + " px" : "—", currentValue, GUILayout.Width(SizeCol));
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(MidCol), GUILayout.MaxWidth(MidCol), GUILayout.ExpandWidth(false));
            GUILayout.Label(changing ? "Will apply" : " ", midCaption, GUILayout.Width(MidCol));
            if (increasing)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(1f, 0.72f, 0.28f, 1f)
                    : new Color(1f, 0.78f, 0.40f, 1f);
                if (GUILayout.Button("Confirm", GUILayout.Width(MidCol), GUILayout.Height(18)))
                {
                    if (questPlatform) usage.questRevertSize = 0;
                    else usage.pcRevertSize = 0;
                    KaleidoVRCOptimizerLogic.QueueImmediateTextureSize(window, usage.path, planned, questPlatform, current);
                }
                GUI.backgroundColor = prev;
                if (GUILayout.Button("Cancel", GUILayout.Width(MidCol), GUILayout.Height(18)))
                    CancelPendingTextureIncrease(usage, questPlatform);
            }
            else
            {
                GUILayout.Label("→", midArrow, GUILayout.Width(MidCol));
            }
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(GUILayout.Width(SizeCol), GUILayout.MaxWidth(SizeCol), GUILayout.ExpandWidth(false));
            GUILayout.Label("New", sizeCaptionStyle, GUILayout.Width(SizeCol));
            GUILayout.Label(((willWrite || increasing) ? planned : current) + " px", changing ? changeStyle : sizeValueStyle, GUILayout.Width(SizeCol));
            EditorGUILayout.EndVertical();
        }

        private static void CancelPendingTextureIncrease(KaleidoTextureUsage usage, bool questPlatform)
        {
            if (usage == null) return;
            if (questPlatform)
            {
                int revert = usage.questRevertSize > 0 ? usage.questRevertSize : usage.currentQuest;
                if (revert > 0) usage.questSize = revert;
                usage.questRevertSize = 0;
            }
            else
            {
                int revert = usage.pcRevertSize > 0 ? usage.pcRevertSize : usage.currentPc;
                if (revert > 0) usage.pcSize = revert;
                usage.pcRevertSize = 0;
            }
        }

        private static void HandleTextureSizePopup(KaleidoVRCOptimizer window, KaleidoTextureUsage usage, bool questPlatform)
        {
            int selected = questPlatform ? usage.questSize : usage.pcSize;
            int picked = SizePopup(selected);
            if (picked == selected) return;
            window.ReadyToApplyMaxSizesOnly = false;

            int current = questPlatform ? usage.currentQuest : usage.currentPc;
            if (picked > selected)
            {
                if (questPlatform)
                {
                    if (usage.questRevertSize <= 0) usage.questRevertSize = selected;
                    usage.questSize = picked;
                    usage.usedCustomQuest = true;
                }
                else
                {
                    if (usage.pcRevertSize <= 0) usage.pcRevertSize = selected;
                    usage.pcSize = picked;
                    usage.usedCustomPc = true;
                }
                return;
            }

            if (questPlatform)
            {
                usage.questSize = picked;
                usage.usedCustomQuest = true;
                usage.questRevertSize = 0;
            }
            else
            {
                usage.pcSize = picked;
                usage.usedCustomPc = true;
                usage.pcRevertSize = 0;
            }
            if (picked == current) return;
            KaleidoVRCOptimizerLogic.QueueImmediateTextureSize(window, usage.path, picked, questPlatform, current);
        }

        private static void DrawTexturePreview(KaleidoVRCOptimizer window)
        {
            KaleidoTextureUsage usage = KaleidoVRCOptimizerLogic.FindTextureUsage(window, window.previewTexturePath);
            if (usage == null) return;

            GUILayout.Space(6);
            GUILayout.Label("Texture Preview", EditorStyles.boldLabel);
            DrawWhy("Click the thumbnail again to close this preview. Linked rows are reference only — click a name to ping it in the Project or Hierarchy.");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawPingableObject(usage.texture, usage.texture != null ? usage.texture.name : Path.GetFileName(usage.path), 0);
            GUILayout.Label(usage.path, MiniWrap());
            GUILayout.Label(KindLabel(usage.kind), EditorStyles.miniLabel);

            Rect preview = GUILayoutUtility.GetRect(16, 220, GUILayout.ExpandWidth(true), GUILayout.Height(220));
            if (usage.texture != null)
            {
                EditorGUI.DrawPreviewTexture(preview, usage.texture, null, ScaleMode.ScaleToFit);
            }
            if (Event.current.type == EventType.MouseDown && preview.Contains(Event.current.mousePosition))
            {
                window.previewTexturePath = "";
                Event.current.Use();
                GUI.changed = true;
            }

            GUILayout.Space(4);
            GUILayout.Label("Linked Materials And Objects", EditorStyles.miniBoldLabel);
            DrawWhy("What this texture is assigned to on the dropped avatar. These are not editable here.");
            const float ObjectCol = 180f;
            const float MaterialCol = 180f;
            const float SlotCol = 110f;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Object", EditorStyles.miniBoldLabel, GUILayout.Width(ObjectCol));
            GUILayout.Label("Material", EditorStyles.miniBoldLabel, GUILayout.Width(MaterialCol));
            GUILayout.Label("Slot", EditorStyles.miniBoldLabel, GUILayout.Width(SlotCol));
            EditorGUILayout.EndHorizontal();
            int shown = 0;
            if (usage.links != null)
            {
                for (int i = 0; i < usage.links.Count; i++)
                {
                    KaleidoTextureLink link = usage.links[i];
                    if (link == null) continue;
                    GameObject linkedObject = link.sceneObject as GameObject;
                    if (!IsObjectOnSelectedAvatar(window, linkedObject)) continue;

                    EditorGUILayout.BeginHorizontal();
                    DrawPingableObject(linkedObject, "(object)", ObjectCol);
                    DrawPingableObject(link.material, "(material)", MaterialCol);
                    GUILayout.Label(string.IsNullOrEmpty(link.propertyName) ? "(slot)" : link.propertyName, GUILayout.Width(SlotCol));
                    EditorGUILayout.EndHorizontal();
                    shown++;
                    if (shown >= 40) break;
                }
                if (shown >= 40 && usage.links.Count > shown)
                {
                    GUILayout.Label("… more slots on this avatar", EditorStyles.miniLabel);
                }
            }
            if (shown == 0)
            {
                EditorGUILayout.HelpBox("No renderer on this avatar samples this texture (it may still be a dependency of a material or nested asset).", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private static bool IsObjectOnSelectedAvatar(KaleidoVRCOptimizer window, GameObject obj)
        {
            if (obj == null || window == null || window.targets == null) return false;
            for (int i = 0; i < window.targets.Count; i++)
            {
                GameObject root;
                string reason;
                if (!KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(window.targets[i], out root, out reason)) continue;
                if (root == null) continue;
                if (obj == root || obj.transform.IsChildOf(root.transform)) return true;
            }
            return false;
        }

        private static void DrawPingableObject(UnityEngine.Object obj, string fallback, float width)
        {
            string name = obj != null ? obj.name : fallback;
            Texture icon = obj != null ? AssetPreview.GetMiniThumbnail(obj) : null;
            GUIContent content = new GUIContent(" " + name, icon, obj != null ? "Ping in Project / Hierarchy" : "");
            GUILayoutOption[] options = width > 0
                ? new[] { GUILayout.Width(width), GUILayout.Height(18) }
                : new[] { GUILayout.MinWidth(120), GUILayout.Height(18) };
            if (GUILayout.Button(content, PingLinkStyle(), options))
            {
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }
        }

        private static string KindLabel(KaleidoTextureKind kind)
        {
            switch (kind)
            {
                case KaleidoTextureKind.Albedo: return "Albedo";
                case KaleidoTextureKind.Normal: return "Normal";
                case KaleidoTextureKind.Mask: return "Mask";
                case KaleidoTextureKind.Emission: return "Emission";
                case KaleidoTextureKind.Matcap: return "Matcap";
                default: return "Other";
            }
        }

        private static void DrawTypeSizeRow(ref bool apply, string title, string why, ref int size)
        {
            apply = EditorGUILayout.ToggleLeft(title, apply);
            DrawWhy(why);
            EditorGUI.BeginDisabledGroup(!apply);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18);
            GUILayout.Label("Set", GUILayout.Width(28));
            size = SizePopup(size);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(4);
        }

        private static void DrawMeshesTab(KaleidoVRCOptimizer window)
        {
            if (window.IsQuestWorkspace)
            {
                GUILayout.Label("Mesh Writes", EditorStyles.boldLabel);
                DrawWhy("Dry Run / Apply only writes these importer flags. Weld, quads, lightmap UVs, and animation compression stay in the other workspace.");
                window.optimizeMeshes = DrawToggle(window.optimizeMeshes, "Process model importers", "Master switch for the mesh writes below.");
                EditorGUI.BeginDisabledGroup(!window.optimizeMeshes);
                window.meshEnableReadWrite = DrawToggle(window.meshEnableReadWrite, "Enable mesh Read / Write", "Required by VRChat on every platform. If any mesh has Read/Write off, rank is Very Poor and upload is blocked.");
                window.applySkinWeights = DrawToggle(window.applySkinWeights, "Set skin weights to 4 bones", "Caps import weights at 4 influences. Unlimited weights are set from the other workspace.");
                if (window.applySkinWeights) window.skinWeights = KaleidoSkinWeightChoice.FourBones;
                EditorGUI.EndDisabledGroup();
                return;
            }

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
            window.applySkinWeights = DrawToggle(window.applySkinWeights, "Set skin weights", "Unlimited is the usual desktop choice. 4 bones is set from the other workspace.");
            if (window.applySkinWeights) window.skinWeights = (KaleidoSkinWeightChoice)EditorGUILayout.EnumPopup("Skin Weights", window.skinWeights);
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawSceneTab(KaleidoVRCOptimizer window)
        {
            if (window.IsQuestWorkspace)
            {
                GUILayout.Label("Renderers", EditorStyles.boldLabel);
                DrawWhy("Dry Run / Apply only writes these renderer flags. Offscreen, probes, particles, and audio stay in the other workspace.");
                window.optimizeRenderers = DrawToggle(window.optimizeRenderers, "Process skinned / mesh renderers", "Master switch for the renderer writes below.");
                EditorGUI.BeginDisabledGroup(!window.optimizeRenderers);
                window.rendererDisableShadows = DrawToggle(window.rendererDisableShadows, "Disable shadow casting", "Uncheck if this avatar should still cast shadows.");
                window.rendererForceBone4 = DrawToggle(window.rendererForceBone4, "Force 4 bone quality on skinned meshes", "Caps GPU skinning at 4 influences.");
                EditorGUI.EndDisabledGroup();
                return;
            }

            GUILayout.Label("Renderers", EditorStyles.boldLabel);
            window.optimizeRenderers = DrawToggle(window.optimizeRenderers, "Process skinned / mesh renderers", "Master switch for this section.");
            EditorGUI.BeginDisabledGroup(!window.optimizeRenderers);
            window.rendererDisableUpdateWhenOffscreen = DrawToggle(window.rendererDisableUpdateWhenOffscreen, "Disable Update When Offscreen", "Big CPU win. Unity keeps animating skinned meshes that are culled if this stays on. Turn off only for meshes that must stay posed while hidden.");
            window.rendererDisableReceiveShadows = DrawToggle(window.rendererDisableReceiveShadows, "Disable receive shadows", "Avatars often skip receiving world shadows. Uncheck if you want contact shadows on the body.");
            window.rendererDisableProbes = DrawToggle(window.rendererDisableProbes, "Disable light / reflection probes", "Stops per-renderer probe sampling. Worlds still light the avatar through VRChat's lighting; this cuts extra probe work.");
            window.rendererDisableMotionVectors = DrawToggle(window.rendererDisableMotionVectors, "Disable motion vectors", "VRChat does not use camera motion blur on avatars. Safe to force off.");
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
            window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.disableCamerasOnAvatar;
        }

        private static void DrawSpecialTab(KaleidoVRCOptimizer window)
        {
            DrawSpecialUseCaseHeader();
            if (window.IsQuestWorkspace)
            {
                EditorGUILayout.HelpBox("These can break cameras, lighting, or material flags. Leave them off unless you know you need them.", MessageType.Warning);
                window.optimizeMaterials = DrawToggle(window.optimizeMaterials, "Set GPU instancing on materials", "Writes enableInstancing on .mat files. Recommended on mobile. Little effect on skinned meshes.");
                if (window.optimizeMaterials) window.materialEnableGpuInstancing = EditorGUILayout.Toggle("Enable GPU Instancing", window.materialEnableGpuInstancing);
                window.disableLightsOnAvatar = DrawToggle(window.disableLightsOnAvatar, "Disable realtime lights on the avatar", "Mobile strips avatar lights. Can change how the avatar looks.");
                window.disableCamerasOnAvatar = DrawToggle(window.disableCamerasOnAvatar, "Disable cameras on the avatar", "Mobile disables avatar cameras. Can kill preview cameras, mirrors, or VRC tools parented under the avatar.");
                window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.disableCamerasOnAvatar;
                return;
            }

            EditorGUILayout.HelpBox("These can break visemes, custom bounds, lighting, or audio. Leave them off unless you know you need them. They only apply when Special is included on the profile.", MessageType.Warning);

            window.applyMeshCompression = DrawToggle(window.applyMeshCompression, "Apply mesh compression", "Unity's mesh compressor distorts blend shapes. Only for static props with no visemes.");
            if (window.applyMeshCompression) window.meshCompression = (KaleidoMeshCompressionChoice)EditorGUILayout.EnumPopup("Compression Level", window.meshCompression);

            window.meshForceHumanoid = DrawToggle(window.meshForceHumanoid, "Force Humanoid rig", "Rewrites the FBX avatar to Humanoid. Can destroy a working Generic/Humanoid mapping. Prefer the Rig tab in the importer.");
            window.meshStripBlendShapes = DrawToggle(window.meshStripBlendShapes, "Disable blend shape import", "Turns blend shapes off on the model. Breaks visemes and face tracking. Only for meshes that truly have none you need.");
            window.rendererRecalculateBounds = DrawToggle(window.rendererRecalculateBounds, "Recalculate skinned bounds", "Resets local bounds from the mesh AABB. Breaks meshes that used oversized bounds so toggled parts stay visible.");
            window.disableLightsOnAvatar = DrawToggle(window.disableLightsOnAvatar, "Disable realtime lights on the avatar", "VRChat Excellent allows 0 lights. Can change how the avatar looks.");
            window.audioForceToMono = DrawToggle(window.audioForceToMono, "Force audio to mono", "Halves clip size but collapses stereo / spatial beds. Only for true mono SFX.");
            window.textureEnableCrunch = DrawToggle(window.textureEnableCrunch, "Enable crunch compression", "Does not lower VRChat texture memory rank. Only download size. VRChat says the package should fit limits without Crunch.");
            window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.disableCamerasOnAvatar;
        }

        public static void DrawActions(KaleidoVRCOptimizer window)
        {
            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                window.WorkspaceReadyToApply
                    ? "Dry run finished. Review the Rank tab, then Apply to write those changes for this workspace."
                    : "Step 1: Dry Run (no files change). Step 2: Apply appears after that dry run.",
                window.WorkspaceReadyToApply ? MessageType.Info : MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Performance", GUILayout.Height(32)))
            {
                window.StoreReport(KaleidoVRCOptimizerLogic.Scan(window, false));
                window.tab = 2;
            }
            if (GUILayout.Button("Dry Run", GUILayout.Height(32)))
            {
                window.dryRun = true;
                KaleidoOptimizerReport report = KaleidoVRCOptimizerLogic.Scan(window, true);
                window.StoreReport(report);
                window.WorkspaceReadyToApply = report != null
                    && report.summary != null
                    && !report.summary.StartsWith("Optimizer failed", StringComparison.Ordinal);
                GUI.changed = false;
                window.tab = 2;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(!window.WorkspaceReadyToApply);
            if (GUILayout.Button("Apply", GUILayout.Height(32)))
            {
                if (!EditorUtility.DisplayDialog(
                    "KaleidoVR VRChat Model Optimizer",
                    "This writes the dry-run changes for this workspace only. The other workspace is not touched. Continue?",
                    "Apply",
                    "Cancel"))
                {
                    EditorGUI.EndDisabledGroup();
                    return;
                }
                window.dryRun = false;
                window.StoreReport(KaleidoVRCOptimizerLogic.Scan(window, true));
                window.dryRun = true;
                window.WorkspaceReadyToApply = false;
                window.tab = 2;
            }
            EditorGUI.EndDisabledGroup();
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
            int index = 5;
            int nearestDelta = int.MaxValue;
            for (int i = 0; i < TextureSizes.Length; i++)
            {
                if (TextureSizes[i] == current)
                {
                    index = i;
                    break;
                }
                int delta = Math.Abs(TextureSizes[i] - current);
                if (current > 0 && delta < nearestDelta)
                {
                    nearestDelta = delta;
                    index = i;
                }
            }
            int picked = EditorGUILayout.Popup(index, TextureSizeLabels, GUILayout.Width(70));
            return TextureSizes[picked];
        }

        private static void DrawStats(KaleidoVRCOptimizer window)
        {
            KaleidoOptimizerReport report = window.ActiveReport ?? window.lastReport;
            if (report == null)
            {
                EditorGUILayout.HelpBox(
                    "No scan yet. Drop a VRChat avatar on Setup and press Scan Performance.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(report.summary, MessageType.None);
            string rank = window.IsQuestWorkspace ? report.questRank : report.pcRank;
            DrawStatRow("Rank", rank, IsProblemRank(rank));
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
            EditorGUILayout.LabelField("Blend Shapes", report.blendShapes.ToString("N0"));
            EditorGUILayout.LabelField("Bones (max on one mesh)", report.bones.ToString("N0"));
            EditorGUILayout.LabelField("Animators", report.animators.ToString("N0"));
            EditorGUILayout.LabelField("Lights", report.lights.ToString("N0"));
            EditorGUILayout.LabelField("Audio Sources", report.audioSources.ToString("N0"));
            EditorGUILayout.LabelField("Particle Systems", report.particleSystems.ToString("N0"));
            EditorGUILayout.LabelField("PhysBones", report.physBones.ToString("N0"));
            EditorGUILayout.LabelField("PhysBone Colliders", report.physBoneColliders.ToString("N0"));
            EditorGUILayout.LabelField("Contacts", report.contacts.ToString("N0"));
            DrawConstraintsRow(window, report);

            DrawEvalSections(window, report);

            if (report.notes.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Hints", EditorStyles.miniBoldLabel);
                StringBuilder hints = new StringBuilder();
                int shown = Math.Min(report.notes.Count, 16);
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

        private static void DrawEvalSections(KaleidoVRCOptimizer window, KaleidoOptimizerReport report)
        {
            GUILayout.Space(8);
            evalVramOpen = EditorGUILayout.Foldout(evalVramOpen, "VRAM (not VRChat rank)", true);
            if (evalVramOpen)
            {
                DrawWhy("Video memory is what GPUs actually choke on. Rank ignores most of this. Active objects still keep inactive VRAM until memory is tight.");
                DrawStatRow("Texture VRAM (all)", KaleidoVRCOptimizerHelpers.FormatBytes(report.textureVramAll) + "  " + report.textureVramQuality, IsProblemRank(report.textureVramQuality));
                EditorGUILayout.LabelField("Texture VRAM (active)", KaleidoVRCOptimizerHelpers.FormatBytes(report.textureVramActive));
                DrawStatRow("Mesh VRAM (all)", KaleidoVRCOptimizerHelpers.FormatBytes(report.meshVramAll) + "  " + report.meshVramQuality, IsProblemRank(report.meshVramQuality));
                EditorGUILayout.LabelField("Mesh VRAM (active)", KaleidoVRCOptimizerHelpers.FormatBytes(report.meshVramActive));
                EditorGUILayout.LabelField("Combined (all)", KaleidoVRCOptimizerHelpers.FormatBytes(report.vramAll));
                EditorGUILayout.LabelField("Combined (active)", KaleidoVRCOptimizerHelpers.FormatBytes(report.vramActive));
                if (report.textureVramPlanned > 0 && report.textureVramPlanned != report.textureVramAll)
                {
                    long saved = report.textureVramAll - report.textureVramPlanned;
                    EditorGUILayout.LabelField("Texture VRAM after Apply (est.)", KaleidoVRCOptimizerHelpers.FormatBytes(report.textureVramPlanned)
                        + (saved > 0 ? "  (−" + KaleidoVRCOptimizerHelpers.FormatBytes(saved) + ")" : ""));
                }
                if (report.vramAll > 0)
                {
                    EditorGUILayout.LabelField("40 copies of this avatar", KaleidoVRCOptimizerHelpers.FormatBytes(report.vramAll * 40), MiniWrap());
                    EditorGUILayout.LabelField("80 copies of this avatar", KaleidoVRCOptimizerHelpers.FormatBytes(report.vramAll * 80), MiniWrap());
                }
            }

            GUILayout.Space(6);
            evalHiddenOpen = EditorGUILayout.Foldout(evalHiddenOpen, "Hidden cost (animator / shaders / blendshapes)", true);
            if (evalHiddenOpen)
            {
                DrawWhy("VRChat rank does not count these. Orange rows need a look. Write Defaults and empty states can be fixed from this tab. GrabPass and blendshape load are reported only.");
                DrawStatRow("GrabPasses", report.grabPasses.ToString("N0") + "  " + report.grabPassQuality, report.grabPasses > 0);
                if (report.grabPassShaders != null && report.grabPassShaders.Count > 0)
                {
                    evalGrabOpen = EditorGUILayout.Foldout(evalGrabOpen, "Shaders with GrabPass", false);
                    if (evalGrabOpen) DrawEvalList(report.grabPassShaders);
                }
                DrawStatRow("Blendshape triangles", report.blendshapeTriangles.ToString("N0") + "  " + report.blendshapeQuality, report.blendshapeTriangles > 32000 || IsProblemRank(report.blendshapeQuality));
                EditorGUILayout.LabelField("Meshes with blendshapes", report.blendshapeMeshes.ToString("N0"));
                if (report.blendshapeMeshLines != null && report.blendshapeMeshLines.Count > 0)
                {
                    evalBlendOpen = EditorGUILayout.Foldout(evalBlendOpen, "Blendshape meshes", false);
                    if (evalBlendOpen) DrawEvalList(report.blendshapeMeshLines);
                }
                DrawStatRow("Any State transitions", report.anyStateTransitions.ToString("N0") + "  " + report.anyStateQuality, report.anyStateTransitions > 50 || IsProblemRank(report.anyStateQuality));
                DrawStatRow("Animator layers", report.animatorLayers.ToString("N0") + "  " + report.layerCountQuality, IsProblemRank(report.layerCountQuality));
                DrawWriteDefaultsRow(window, report);
                if (report.writeDefaultsMixed && report.writeDefaultOutliers != null && report.writeDefaultOutliers.Count > 0)
                {
                    evalWdOpen = EditorGUILayout.Foldout(evalWdOpen, "Write Default outliers (" + report.writeDefaultOutliers.Count + ")", false);
                    if (evalWdOpen) DrawEvalList(report.writeDefaultOutliers);
                }
                DrawEmptyStatesRow(window, report);
            }

            GUILayout.Space(6);
            evalFlagsOpen = EditorGUILayout.Foldout(evalFlagsOpen, "Texture flags (crunch / normals / swaps / streaming)", true);
            if (evalFlagsOpen)
            {
                DrawWhy("Crunch does not lower VRChat texture memory. BC5 is the usual desktop normal format at the same VRAM as BC7. Animation material swaps still cost VRAM even when the slot is unused. Streaming mip maps should be on when mip maps are on.");
                DrawStatRow("Crunched textures", report.crunchedTextures != null ? report.crunchedTextures.Count.ToString("N0") : "0", report.crunchedTextures != null && report.crunchedTextures.Count > 0);
                if (report.crunchedTextures != null && report.crunchedTextures.Count > 0)
                    DrawEvalList(report.crunchedTextures);
                DrawStatRow("Normals not BC5", report.nonBc5Normals != null ? report.nonBc5Normals.Count.ToString("N0") : "0", !window.IsQuestWorkspace && report.nonBc5Normals != null && report.nonBc5Normals.Count > 0);
                if (!window.IsQuestWorkspace && report.nonBc5Normals != null && report.nonBc5Normals.Count > 0)
                    DrawEvalList(report.nonBc5Normals);
                EditorGUILayout.LabelField("Animation-swap textures", report.materialSwapNames != null ? report.materialSwapNames.Count.ToString("N0") : "0");
                if (report.materialSwapNames != null && report.materialSwapNames.Count > 0)
                    DrawEvalList(report.materialSwapNames);
                DrawStreamingMipmapsRow(window, report);
            }
        }

        private static void DrawEvalList(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return;
            StringBuilder sb = new StringBuilder();
            int shown = Math.Min(lines.Count, 16);
            for (int i = 0; i < shown; i++) sb.AppendLine("• " + lines[i]);
            if (lines.Count > shown) sb.AppendLine("• … " + (lines.Count - shown) + " more");
            EditorGUILayout.HelpBox(sb.ToString().TrimEnd(), MessageType.None);
        }

        private static bool IsProblemRank(string quality)
        {
            return quality == "Poor" || quality == "Very Poor";
        }

        private static void DrawRankColorKey()
        {
            EnsureSizeStyles();
            GUILayout.Space(4);
            GUILayout.Label("Color key", EditorStyles.miniBoldLabel);

            GUIStyle redStyle = new GUIStyle(EditorStyles.boldLabel);
            redStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.38f, 0.38f, 1f)
                : new Color(0.72f, 0.08f, 0.08f, 1f);

            DrawColorKeyLine(sizeUpStyle, "Orange", "Needs a look. Poor / Very Poor, mixed Write Defaults, empty states, streaming mip maps off, leftover Unity constraints, GrabPass, and similar flags.");
            DrawColorKeyLine(redStyle, "Red", "Blocks upload. Mesh Read/Write is off on at least one mesh.");
            DrawColorKeyLine(EditorStyles.label, "Normal", "No issue on that row.");
            DrawWhy("Fix uses the same orange. Ignore leaves that row alone. Write Defaults still asks Confirm after On or Off.");
            GUILayout.Space(4);
        }

        private static void DrawColorKeyLine(GUIStyle swatch, string name, string meaning)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(name, swatch, GUILayout.Width(70));
            EditorGUILayout.LabelField(meaning, MiniWrap());
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawStatRow(string label, string value, bool warn)
        {
            EnsureSizeStyles();
            EditorGUILayout.LabelField(label, value, warn ? sizeUpStyle : EditorStyles.label);
        }

        private static void DrawWriteDefaultsRow(KaleidoVRCOptimizer window, KaleidoOptimizerReport report)
        {
            EnsureSizeStyles();
            int onCount = report.writeDefaultsOnCount;
            int offCount = report.writeDefaultsOffCount;
            int total = onCount + offCount;
            string status;
            if (total == 0) status = "—";
            else if (report.writeDefaultsMixed) status = "Mixed — should be all on or all off  (" + onCount + " on, " + offCount + " off)";
            else status = report.writeDefaultsMostlyOn ? "On" : "Off";
            DrawStatRow("Write Defaults", status, report.writeDefaultsMixed);
            if (total == 0) return;

            bool alreadyOn = !report.writeDefaultsMixed && report.writeDefaultsMostlyOn;
            bool alreadyOff = !report.writeDefaultsMixed && !report.writeDefaultsMostlyOn;
            bool pending = (window.writeDefaultsAction == 1 && !alreadyOn) || (window.writeDefaultsAction == 2 && !alreadyOff);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Toggle(window.writeDefaultsAction == 1, "On", EditorStyles.miniButton, GUILayout.Height(18), GUILayout.Width(70)) && window.writeDefaultsAction != 1)
                window.writeDefaultsAction = 1;
            if (GUILayout.Toggle(window.writeDefaultsAction == 2, "Off", EditorStyles.miniButton, GUILayout.Height(18), GUILayout.Width(70)) && window.writeDefaultsAction != 2)
                window.writeDefaultsAction = 2;
            if (GUILayout.Toggle(window.writeDefaultsAction == 0, "Ignore", EditorStyles.miniButton, GUILayout.Height(18), GUILayout.Width(70)) && window.writeDefaultsAction != 0)
                window.writeDefaultsAction = 0;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (!pending) return;

            const float MidCol = 88f;
            GUIStyle midCaption = new GUIStyle(sizeCaptionStyle) { alignment = TextAnchor.MiddleCenter };
            midCaption.normal.textColor = sizeUpStyle.normal.textColor;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            EditorGUILayout.BeginVertical(GUILayout.Width(MidCol), GUILayout.MaxWidth(MidCol), GUILayout.ExpandWidth(false));
            GUILayout.Label("Will apply", midCaption, GUILayout.Width(MidCol));
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.72f, 0.28f, 1f)
                : new Color(1f, 0.78f, 0.40f, 1f);
            if (GUILayout.Button("Confirm", GUILayout.Width(MidCol), GUILayout.Height(18)))
            {
                bool turnOn = window.writeDefaultsAction == 1;
                int written = KaleidoVRCOptimizerEval.SetWriteDefaults(CurrentAvatarRoots(window), turnOn);
                window.writeDefaultsAction = 0;
                window.StoreReport(KaleidoVRCOptimizerLogic.Scan(window, false));
                GUI.changed = false;
                if (written > 0 && window.ActiveReport != null)
                    window.ActiveReport.planned.Insert(0, "Write Defaults " + (turnOn ? "On" : "Off") + " on " + written + " animator state(s).");
            }
            GUI.backgroundColor = prev;
            if (GUILayout.Button("Cancel", GUILayout.Width(MidCol), GUILayout.Height(18)))
                window.writeDefaultsAction = 0;
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            DrawWhy(window.writeDefaultsAction == 1
                ? "Confirm writes Write Defaults On for every animator state on this avatar."
                : "Confirm writes Write Defaults Off for every animator state on this avatar.");
        }

        private static void DrawEmptyStatesRow(KaleidoVRCOptimizer window, KaleidoOptimizerReport report)
        {
            int count = report.emptyStateCount;
            DrawStatRow("Empty animator states", count.ToString("N0"), count > 0);
            if (report.emptyStates != null && report.emptyStates.Count > 0)
            {
                evalEmptyOpen = EditorGUILayout.Foldout(evalEmptyOpen, "States with no motion", false);
                if (evalEmptyOpen) DrawEvalList(report.emptyStates);
            }
            if (count == 0) return;
            if (!DrawIgnoreOrFix()) return;

            int written = KaleidoVRCOptimizerEval.SetEmptyMotions(CurrentAvatarRoots(window));
            window.StoreReport(KaleidoVRCOptimizerLogic.Scan(window, false));
            GUI.changed = false;
            if (written > 0 && window.ActiveReport != null)
                window.ActiveReport.planned.Insert(0, "Assigned the shared empty motion to " + written + " animator state(s).");
        }

        private static void DrawStreamingMipmapsRow(KaleidoVRCOptimizer window, KaleidoOptimizerReport report)
        {
            int count = report.missingStreamingCount;
            DrawStatRow("Streaming mip maps off", count.ToString("N0"), count > 0);
            if (report.missingStreamingMipmaps != null && report.missingStreamingMipmaps.Count > 0)
                DrawEvalList(report.missingStreamingMipmaps);
            if (count == 0) return;
            if (!DrawIgnoreOrFix()) return;

            int written = KaleidoVRCOptimizerEval.SetStreamingMipmaps(window);
            window.StoreReport(KaleidoVRCOptimizerLogic.Scan(window, false));
            GUI.changed = false;
            if (written > 0 && window.ActiveReport != null)
                window.ActiveReport.planned.Insert(0, "Enabled streaming mip maps on " + written + " texture(s).");
        }

        private static void DrawConstraintsRow(KaleidoVRCOptimizer window, KaleidoOptimizerReport report)
        {
            DrawStatRow("Constraints", report.constraints.ToString("N0"), report.unityConstraints > 0);
            DrawStatRow("Unity constraints", report.unityConstraints.ToString("N0"), report.unityConstraints > 0);
            EditorGUILayout.LabelField("VRChat constraints", report.vrcConstraints.ToString("N0"));
            if (report.unityConstraints <= 0)
            {
                if (report.vrcConstraints > 0)
                    DrawWhy("Unity constraints are already converted. Play Mode and rank match what VRChat loads.");
                return;
            }
            DrawWhy("The client converts these on load. Fix runs the VRChat SDK converter now so Unity matches that.");
            if (!DrawIgnoreOrFix()) return;

            int written = KaleidoVRCOptimizerEval.ConvertUnityConstraints(CurrentAvatarRoots(window));
            if (written < 0)
            {
                EditorUtility.DisplayDialog(
                    "VRChat Model Optimizer",
                    "VRChat SDK3 Avatars is required to convert Unity constraints. Open the SDK control panel Utilities menu if you need to install it.",
                    "OK");
                return;
            }
            window.StoreReport(KaleidoVRCOptimizerLogic.Scan(window, false));
            GUI.changed = false;
            if (window.ActiveReport != null)
                window.ActiveReport.planned.Insert(0, written > 0
                    ? "Converted " + written + " Unity constraint(s) to VRChat constraints."
                    : "Constraint converter ran. Scan again if any Unity constraints remain.");
        }

        private static bool DrawIgnoreOrFix()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUILayout.Toggle(true, "Ignore", EditorStyles.miniButton, GUILayout.Height(18), GUILayout.Width(70));
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.72f, 0.28f, 1f)
                : new Color(1f, 0.78f, 0.40f, 1f);
            bool fix = GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Height(18), GUILayout.Width(70));
            GUI.backgroundColor = prev;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return fix;
        }

        private static List<GameObject> CurrentAvatarRoots(KaleidoVRCOptimizer window)
        {
            List<GameObject> roots = new List<GameObject>();
            if (window == null || window.targets == null) return roots;
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < window.targets.Count; i++)
            {
                GameObject root;
                string reason;
                if (!KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(window.targets[i], out root, out reason)) continue;
                if (root != null && seen.Add(root.GetInstanceID())) roots.Add(root);
            }
            return roots;
        }

        private static void DrawModelContents(KaleidoVRCOptimizer window)
        {
            GUILayout.Space(8);
            GUILayout.Label("Contents Of Selected Model", EditorStyles.boldLabel);
            DrawWhy("Only assets this avatar uses. FBX-packed meshes and materials are listed separately — many avatars assign materials on the prefab instead of using the ones inside the FBX.");

            if (window.inventory == null || window.inventory.Count == 0)
            {
                EditorGUILayout.HelpBox("Drop an avatar above. Its meshes, materials, textures, clips, and menus will list here.", MessageType.None);
                return;
            }

            List<KaleidoModelInventoryItem> usedOnAvatar = new List<KaleidoModelInventoryItem>();
            List<KaleidoModelInventoryItem> insideModelFile = new List<KaleidoModelInventoryItem>();
            for (int i = 0; i < window.inventory.Count; i++)
            {
                KaleidoModelInventoryItem item = window.inventory[i];
                if (item == null) continue;
                if (item.insideModelFile) insideModelFile.Add(item);
                else usedOnAvatar.Add(item);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(SummarizeInventory(usedOnAvatar), MiniWrap());
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                window.inventorySignature = "";
                window.RefreshInventoryIfNeeded();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Used On The Avatar", EditorStyles.miniBoldLabel);
            DrawWhy("Prefab, materials, textures, animators, and menus assigned on this avatar — not packed inside the FBX.");
            DrawInventoryRows(usedOnAvatar, ref window.inventoryScroll, 348);

            GUILayout.Space(8);
            GUILayout.Label("Inside The Model File (FBX)", EditorStyles.miniBoldLabel);
            DrawWhy("Lives on the character FBX/VRM/GLB. If you swapped materials on the prefab, these FBX copies are not what the avatar is wearing.");
            if (insideModelFile.Count == 0)
            {
                EditorGUILayout.HelpBox("Nothing in this list — the dropped object is not a model file, or it has no packed sub-assets.", MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField(SummarizeInventory(insideModelFile), MiniWrap());
                DrawInventoryRows(insideModelFile, ref window.inventoryModelFileScroll, 308);
            }
        }

        private static void DrawInventoryRows(List<KaleidoModelInventoryItem> items, ref Vector2 scroll, float height)
        {
            if (items == null || items.Count == 0)
            {
                EditorGUILayout.HelpBox("None on this avatar outside the model file.", MessageType.None);
                return;
            }

            BeginOutlinedPanel();
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(height));
            string lastCategory = null;
            for (int i = 0; i < items.Count; i++)
            {
                KaleidoModelInventoryItem item = items[i];
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
                if (GUILayout.Button(icon, EditorStyles.label, GUILayout.Width(220), GUILayout.Height(18)))
                {
                    if (item.asset != null)
                    {
                        EditorGUIUtility.PingObject(item.asset);
                        Selection.activeObject = item.asset;
                    }
                }
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(item.asset, typeof(UnityEngine.Object), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EndOutlinedPanel();
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

        private static void DrawProminentAvatarDrop(List<UnityEngine.Object> list, string dropLabel)
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 78, GUILayout.ExpandWidth(true));
            bool dragging = DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0;
            bool hover = dragging && dropArea.Contains(Event.current.mousePosition);
            bool pro = EditorGUIUtility.isProSkin;
            Color fill = hover
                ? (pro ? new Color(0.12f, 0.32f, 0.18f, 1f) : new Color(0.72f, 0.92f, 0.74f, 1f))
                : (pro ? new Color(0.13f, 0.20f, 0.28f, 1f) : new Color(0.82f, 0.90f, 0.97f, 1f));
            Color border = hover ? new Color(0.20f, 0.72f, 0.32f, 1f) : new Color(0.20f, 0.62f, 0.90f, 1f);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(dropArea, fill);
                DrawBoxOutline(dropArea, border);
            }

            int ready = 0;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null) ready++;
                }
            }

            Rect titleRect = new Rect(dropArea.x + 10, dropArea.y + 10, dropArea.width - 20, 24);
            Rect hintRect = new Rect(dropArea.x + 12, dropArea.y + 34, dropArea.width - 24, 36);
            GUI.Label(titleRect, dropLabel, DropTitleStyle());
            string hint = ready == 0
                ? "Prefab, scene instance, or character FBX  ·  not folders, worlds, or loose textures"
                : (ready == 1 ? "1 avatar ready  ·  drop another, or use the slots below" : ready + " avatars ready  ·  drop another, or use the slots below");
            GUI.Label(hintRect, hint, DropHintStyle());
            KaleidoVRCOptimizerHelpers.HandleDragAndDrop(dropArea, list, true);
        }

        private static void DrawObjectList(List<UnityEngine.Object> list, string dropLabel, bool striped, bool avatarModelsOnly, bool prominentDrop = false)
        {
            if (prominentDrop) DrawProminentAvatarDrop(list, dropLabel);
            else
            {
                Rect dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, dropLabel, EditorStyles.helpBox);
                KaleidoVRCOptimizerHelpers.HandleDragAndDrop(dropArea, list, avatarModelsOnly);
            }

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

        public static bool IsModelFilePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string lower = path.ToLowerInvariant();
            return lower.EndsWith(".fbx") || lower.EndsWith(".blend") || lower.EndsWith(".dae")
                || lower.EndsWith(".vrm") || lower.EndsWith(".glb") || lower.EndsWith(".gltf") || lower.EndsWith(".obj");
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
            int unity;
            int vrc;
            CountConstraints(root, out unity, out vrc);
            return unity + vrc;
        }

        public static void CountConstraints(GameObject root, out int unity, out int vrc)
        {
            unity = 0;
            vrc = 0;
            if (root == null) return;
            unity = root.GetComponentsInChildren<IConstraint>(true).Length;
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                string fullName = component.GetType().FullName ?? "";
                if (fullName.IndexOf("VRC", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (fullName.IndexOf("Constraint", StringComparison.OrdinalIgnoreCase) < 0) continue;
                vrc++;
            }
        }
    }

    public static class KaleidoVRCOptimizerLogic
    {
        public static KaleidoOptimizerReport Scan(KaleidoVRCOptimizer window, bool apply)
        {
            if (!apply) window.writeDefaultsAction = 0;
            KaleidoOptimizerReport report = new KaleidoOptimizerReport();
            List<string> logEntries = new List<string>
            {
                "KaleidoVR VRChat Model Optimizer " + KaleidoVRCOptimizer.VERSION + " at " + DateTime.Now,
                "Workspace: " + (window.IsQuestWorkspace ? "Quest / Android Workspace" : "PC Workspace"),
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
                PopulateTextureUsages(window, roots, assetPaths);

                GatherStats(roots, assetPaths, report, logEntries);
                KaleidoVRCOptimizerEval.Evaluate(window, roots, report);
                BuildHints(report, window.IsQuestWorkspace);

                if (apply)
                {
                    ApplyOptimizations(window, roots, assetPaths, ignorePaths, report, logEntries);
                }

                report.summary = apply
                    ? (window.dryRun
                        ? "Dry run complete. " + report.planned.Count + " change(s) would be applied."
                        : "Applied " + report.planned.Count + " change(s). Reimport may take a moment.")
                    : "Scan complete for " + roots.Count + " VRChat avatar model(s) and " + assetPaths.Count + " related asset(s).";
                if (report.textureVramAll > 0)
                {
                    report.summary += " Texture VRAM " + KaleidoVRCOptimizerHelpers.FormatBytes(report.textureVramAll);
                    if (report.textureVramPlanned > 0 && report.textureVramPlanned < report.textureVramAll)
                        report.summary += " → " + KaleidoVRCOptimizerHelpers.FormatBytes(report.textureVramPlanned) + " after Apply (est.).";
                    else
                        report.summary += ".";
                }
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
                    report.summary + "\nRank: " + (window.IsQuestWorkspace ? report.questRank : report.pcRank),
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
            PopulateTextureUsages(window, roots, assetPaths);
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
                        label = asset.name,
                        insideModelFile = KaleidoVRCOptimizerHelpers.IsModelFilePath(path)
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

        public static void SyncTextureRowDefaults(KaleidoVRCOptimizer window)
        {
            if (window == null || window.textureUsages == null) return;
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage == null) continue;
                if (!usage.usedCustomPc) usage.pcSize = usage.currentPc > 0 ? usage.currentPc : usage.pcSize;
                if (!usage.usedCustomQuest) usage.questSize = usage.currentQuest > 0 ? usage.currentQuest : usage.questSize;
            }
        }

        private static readonly Dictionary<string, int> textureSizeApplyGeneration = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static void QueueImmediateTextureSize(KaleidoVRCOptimizer window, string path, int size, bool questPlatform)
        {
            QueueImmediateTextureSize(window, path, size, questPlatform, -1);
        }

        public static void QueueImmediateTextureSize(KaleidoVRCOptimizer window, string path, int size, bool questPlatform, int fromSize)
        {
            if (window == null || string.IsNullOrEmpty(path) || size <= 0) return;
            if (fromSize > 0 && fromSize != size) PushTextureSizeHistory(window, path, questPlatform, fromSize, size);
            SyncUsageSelector(window, path, size, questPlatform);
            int generation = NextTextureSizeGeneration(path);
            EditorApplication.delayCall += () => ApplyImmediateTextureSize(window, path, size, questPlatform, generation);
        }

        public static void RevertLastTextureSize(KaleidoVRCOptimizer window)
        {
            if (window == null || window.textureSizeHistory == null) return;
            if (window.textureSizeHistoryIndex < 0 || window.textureSizeHistoryIndex >= window.textureSizeHistory.Count) return;
            KaleidoTextureSizeEdit edit = window.textureSizeHistory[window.textureSizeHistoryIndex];
            window.textureSizeHistoryIndex--;
            QueueTextureSizeWithoutHistory(window, edit);
            window.Repaint();
        }

        public static void ForwardTextureSize(KaleidoVRCOptimizer window)
        {
            if (window == null || window.textureSizeHistory == null) return;
            if (window.textureSizeHistoryIndex + 1 >= window.textureSizeHistory.Count) return;
            window.textureSizeHistoryIndex++;
            KaleidoTextureSizeEdit edit = window.textureSizeHistory[window.textureSizeHistoryIndex];
            QueueTextureSizeWithoutHistory(window, edit, true);
            window.Repaint();
        }

        private static void QueueTextureSizeWithoutHistory(KaleidoVRCOptimizer window, KaleidoTextureSizeEdit edit, bool forward = false)
        {
            if (window == null || edit == null || string.IsNullOrEmpty(edit.path)) return;
            int size = forward ? edit.toSize : edit.fromSize;
            if (size <= 0) return;
            SyncUsageSelector(window, edit.path, size, edit.questPlatform);
            int generation = NextTextureSizeGeneration(edit.path);
            EditorApplication.delayCall += () => ApplyImmediateTextureSize(window, edit.path, size, edit.questPlatform, generation);
        }

        private static void PushTextureSizeHistory(KaleidoVRCOptimizer window, string path, bool questPlatform, int fromSize, int toSize)
        {
            if (window == null || string.IsNullOrEmpty(path) || fromSize <= 0 || toSize <= 0 || fromSize == toSize) return;
            if (window.textureSizeHistory == null) window.textureSizeHistory = new List<KaleidoTextureSizeEdit>();
            if (window.textureSizeHistoryIndex < window.textureSizeHistory.Count - 1)
            {
                int removeAt = window.textureSizeHistoryIndex + 1;
                window.textureSizeHistory.RemoveRange(removeAt, window.textureSizeHistory.Count - removeAt);
            }
            window.textureSizeHistory.Add(new KaleidoTextureSizeEdit
            {
                path = path,
                questPlatform = questPlatform,
                fromSize = fromSize,
                toSize = toSize
            });
            window.textureSizeHistoryIndex = window.textureSizeHistory.Count - 1;
        }

        private static int NextTextureSizeGeneration(string path)
        {
            int generation;
            if (!textureSizeApplyGeneration.TryGetValue(path, out generation)) generation = 0;
            generation++;
            textureSizeApplyGeneration[path] = generation;
            return generation;
        }

        private static void SyncUsageSelector(KaleidoVRCOptimizer window, string path, int size, bool questPlatform)
        {
            KaleidoTextureUsage usage = FindTextureUsage(window, path);
            if (usage == null) return;
            if (questPlatform)
            {
                usage.questSize = size;
                usage.usedCustomQuest = true;
            }
            else
            {
                usage.pcSize = size;
                usage.usedCustomPc = true;
            }
        }

        private static void ApplyImmediateTextureSize(KaleidoVRCOptimizer window, string path, int size, bool questPlatform, int generation)
        {
            if (window == null || string.IsNullOrEmpty(path)) return;
            int currentGeneration;
            if (!textureSizeApplyGeneration.TryGetValue(path, out currentGeneration) || currentGeneration != generation) return;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            try
            {
                if (questPlatform)
                {
                    TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                    android.overridden = true;
                    android.name = "Android";
                    android.maxTextureSize = size;
                    importer.SetPlatformTextureSettings(android);
                }
                else
                {
                    importer.maxTextureSize = size;
                    TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
                    standalone.overridden = true;
                    standalone.name = "Standalone";
                    standalone.maxTextureSize = size;
                    importer.SetPlatformTextureSettings(standalone);
                }

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KaleidoVR] Could not apply texture size for " + path + ": " + ex.Message);
                return;
            }

            KaleidoTextureUsage usage = FindTextureUsage(window, path);
            if (usage != null)
            {
                if (questPlatform) usage.currentQuest = size;
                else usage.currentPc = size;
                if (usage.texture != null)
                {
                    usage.sourceWidth = usage.texture.width;
                    usage.sourceHeight = usage.texture.height;
                }
            }

            window.Repaint();
        }

        private static int CapTypeMaxSize(bool typeApply, int typeSize, int current)
        {
            if (!typeApply || typeSize <= 0) return current;
            if (current > 0 && typeSize > current) return current;
            return typeSize;
        }

        public static int GetPlannedRowSize(KaleidoVRCOptimizer window, KaleidoTextureUsage usage, bool questPlatform)
        {
            if (usage == null) return 0;
            bool typeApply;
            int typePc;
            int typeQuest;
            GetTypeSizes(window, usage.kind, out typeApply, out typePc, out typeQuest);
            if (questPlatform)
            {
                if (usage.usedCustomQuest) return usage.questSize;
                return CapTypeMaxSize(typeApply, typeQuest, usage.currentQuest);
            }
            if (usage.usedCustomPc) return usage.pcSize;
            return CapTypeMaxSize(typeApply, typePc, usage.currentPc);
        }

        public static bool TryGetPlannedMaxSize(KaleidoVRCOptimizer window, KaleidoTextureUsage usage, bool questPlatform, out int current, out int planned)
        {
            current = 0;
            planned = 0;
            if (window == null || usage == null) return false;
            bool typeApply;
            int typePc;
            int typeQuest;
            GetTypeSizes(window, usage.kind, out typeApply, out typePc, out typeQuest);
            current = questPlatform ? usage.currentQuest : usage.currentPc;
            bool custom = questPlatform ? usage.usedCustomQuest : usage.usedCustomPc;
            if (!typeApply && !custom) return false;
            planned = GetPlannedRowSize(window, usage, questPlatform);
            if (planned <= 0) return false;
            if (!custom && current > 0 && planned > current) return false;
            return true;
        }

        public static void PreviewMaxSizesOnly(KaleidoVRCOptimizer window, bool questPlatform, out int count, out string preview)
        {
            List<string> lines = new List<string>();
            CollectMaxSizeOnlyChanges(window, questPlatform, lines, null);
            count = lines.Count;
            preview = FormatMaxSizePreview(lines);
        }

        public static void ApplyMaxSizesOnly(KaleidoVRCOptimizer window, bool questPlatform, out int written, out string preview)
        {
            written = 0;
            List<string> lines = new List<string>();
            List<KaleidoTextureUsage> toWrite = new List<KaleidoTextureUsage>();
            CollectMaxSizeOnlyChanges(window, questPlatform, lines, toWrite);
            preview = FormatMaxSizePreview(lines);
            if (toWrite.Count == 0) return;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < toWrite.Count; i++)
                {
                    KaleidoTextureUsage usage = toWrite[i];
                    if (usage == null || string.IsNullOrEmpty(usage.path)) continue;
                    int current;
                    int planned;
                    if (!TryGetPlannedMaxSize(window, usage, questPlatform, out current, out planned)) continue;
                    if (planned == current) continue;
                    TextureImporter importer = AssetImporter.GetAtPath(usage.path) as TextureImporter;
                    if (importer == null) continue;
                    if (!WriteMaxSizeOnly(importer, planned, questPlatform)) continue;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    PushTextureSizeHistory(window, usage.path, questPlatform, current, planned);
                    if (questPlatform) usage.currentQuest = planned;
                    else usage.currentPc = planned;
                    written++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            window.Repaint();
        }

        private static void CollectMaxSizeOnlyChanges(
            KaleidoVRCOptimizer window,
            bool questPlatform,
            List<string> lines,
            List<KaleidoTextureUsage> toWrite)
        {
            if (window == null || window.textureUsages == null) return;
            window.RefreshInventoryIfNeeded();
            SyncTextureRowDefaults(window);
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage == null) continue;
                int current;
                int planned;
                if (!TryGetPlannedMaxSize(window, usage, questPlatform, out current, out planned)) continue;
                if (planned == current) continue;
                string name = usage.texture != null ? usage.texture.name : Path.GetFileName(usage.path);
                lines.Add(name + ": " + current + " → " + planned);
                if (toWrite != null) toWrite.Add(usage);
            }
        }

        private static string FormatMaxSizePreview(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return "";
            StringBuilder sb = new StringBuilder();
            int shown = Math.Min(lines.Count, 16);
            for (int i = 0; i < shown; i++) sb.AppendLine("• " + lines[i]);
            if (lines.Count > shown) sb.AppendLine("• … " + (lines.Count - shown) + " more");
            return sb.ToString().TrimEnd();
        }

        private static bool WriteMaxSizeOnly(TextureImporter importer, int size, bool questPlatform)
        {
            if (importer == null || size <= 0) return false;
            if (questPlatform)
            {
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                if (android.overridden && android.maxTextureSize == size) return false;
                android.overridden = true;
                android.name = "Android";
                android.maxTextureSize = size;
                importer.SetPlatformTextureSettings(android);
                return true;
            }

            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            bool dirty = importer.maxTextureSize != size || !standalone.overridden || standalone.maxTextureSize != size;
            if (!dirty) return false;
            importer.maxTextureSize = size;
            standalone.overridden = true;
            standalone.name = "Standalone";
            standalone.maxTextureSize = size;
            importer.SetPlatformTextureSettings(standalone);
            return true;
        }

        public static KaleidoTextureUsage FindTextureUsage(KaleidoVRCOptimizer window, string path)
        {
            if (window == null || window.textureUsages == null || string.IsNullOrEmpty(path)) return null;
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage != null && string.Equals(usage.path, path, StringComparison.OrdinalIgnoreCase)) return usage;
            }
            return null;
        }

        private static void PopulateTextureUsages(KaleidoVRCOptimizer window, List<GameObject> roots, HashSet<string> assetPaths)
        {
            Dictionary<string, KaleidoTextureUsage> previous = new Dictionary<string, KaleidoTextureUsage>(StringComparer.OrdinalIgnoreCase);
            if (window.textureUsages != null)
            {
                for (int i = 0; i < window.textureUsages.Count; i++)
                {
                    KaleidoTextureUsage old = window.textureUsages[i];
                    if (old == null || string.IsNullOrEmpty(old.path)) continue;
                    previous[old.path] = old;
                }
            }

            if (window.textureUsages == null) window.textureUsages = new List<KaleidoTextureUsage>();
            window.textureUsages.Clear();
            Dictionary<string, KaleidoTextureUsage> map = new Dictionary<string, KaleidoTextureUsage>(StringComparer.OrdinalIgnoreCase);

            HashSet<int> seenRoots = new HashSet<int>();
            if (roots != null)
            {
                for (int r = 0; r < roots.Count; r++)
                {
                    GameObject root = roots[r];
                    if (root == null || !seenRoots.Add(root.GetInstanceID())) continue;
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer == null || renderer.sharedMaterials == null) continue;
                        if (KaleidoVRCOptimizerEval.IsEditorOnly(renderer.gameObject)) continue;
                        for (int m = 0; m < renderer.sharedMaterials.Length; m++)
                        {
                            Material material = renderer.sharedMaterials[m];
                            if (material == null) continue;
                            CollectMaterialTextures(window, map, previous, renderer.gameObject, material, false);
                        }
                    }
                    KaleidoVRCOptimizerEval.CollectAnimationMaterials(root, delegate (Material swapMat)
                    {
                        CollectMaterialTextures(window, map, previous, root, swapMat, true);
                    });
                }
            }

            if (assetPaths != null)
            {
                foreach (string path in assetPaths)
                {
                    Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    if (texture == null) continue;
                    GetOrAddTextureUsage(window, map, previous, texture);
                }
            }

            window.textureUsages.Sort(delegate (KaleidoTextureUsage a, KaleidoTextureUsage b)
            {
                int kind = ((int)a.kind).CompareTo((int)b.kind);
                if (kind != 0) return kind;
                string an = a.texture != null ? a.texture.name : a.path;
                string bn = b.texture != null ? b.texture.name : b.path;
                return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static void CollectMaterialTextures(
            KaleidoVRCOptimizer window,
            Dictionary<string, KaleidoTextureUsage> map,
            Dictionary<string, KaleidoTextureUsage> previous,
            GameObject sceneObject,
            Material material,
            bool fromAnimationSwap)
        {
            Shader shader = material.shader;
            if (shader != null)
            {
                int count = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < count; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                    string prop = ShaderUtil.GetPropertyName(shader, i);
                    Texture texture = material.GetTexture(prop);
                    if (texture == null) continue;
                    KaleidoTextureUsage usage = GetOrAddTextureUsage(window, map, previous, texture);
                    AddTextureLink(usage, sceneObject, material, prop);
                    if (usage != null)
                    {
                        if (fromAnimationSwap) usage.fromAnimationSwap = true;
                        if (sceneObject != null && sceneObject.activeInHierarchy) usage.isActive = true;
                    }
                }
            }

            if (material.mainTexture != null)
            {
                KaleidoTextureUsage main = GetOrAddTextureUsage(window, map, previous, material.mainTexture);
                AddTextureLink(main, sceneObject, material, "_MainTex");
                if (main != null)
                {
                    if (fromAnimationSwap) main.fromAnimationSwap = true;
                    if (sceneObject != null && sceneObject.activeInHierarchy) main.isActive = true;
                }
            }
        }

        private static KaleidoTextureUsage GetOrAddTextureUsage(
            KaleidoVRCOptimizer window,
            Dictionary<string, KaleidoTextureUsage> map,
            Dictionary<string, KaleidoTextureUsage> previous,
            Texture texture)
        {
            if (texture == null) return null;
            string path = KaleidoVRCOptimizerHelpers.NormalizeAssetPath(AssetDatabase.GetAssetPath(texture));
            if (string.IsNullOrEmpty(path) || KaleidoVRCOptimizerHelpers.ShouldIgnoreAsset(path)) return null;

            KaleidoTextureUsage usage;
            if (map.TryGetValue(path, out usage)) return usage;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            KaleidoTextureKind kind = KaleidoVRCOptimizerHelpers.ClassifyTexture(path, importer);
            bool apply;
            int pc;
            int quest;
            GetTypeSizes(window, kind, out apply, out pc, out quest);

            usage = new KaleidoTextureUsage
            {
                path = path,
                texture = texture,
                kind = kind,
                sourceWidth = texture.width,
                sourceHeight = texture.height,
                currentPc = importer != null ? importer.maxTextureSize : texture.width,
                currentQuest = importer != null ? importer.maxTextureSize : texture.height,
                pcSize = 0,
                questSize = 0,
                links = new List<KaleidoTextureLink>()
            };
            if (importer != null)
            {
                TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
                if (standalone != null && standalone.overridden) usage.currentPc = standalone.maxTextureSize;
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                if (android != null && android.overridden) usage.currentQuest = android.maxTextureSize;
            }
            usage.pcSize = usage.currentPc > 0 ? usage.currentPc : pc;
            usage.questSize = usage.currentQuest > 0 ? usage.currentQuest : quest;
            usage.vramBytes = KaleidoVRCOptimizerEval.TextureVramBytes(texture);
            if (texture is Texture2D t2d) usage.formatLabel = t2d.format.ToString();
            else usage.formatLabel = texture.GetType().Name;

            KaleidoTextureUsage old;
            if (previous != null && previous.TryGetValue(path, out old) && (old.usedCustomPc || old.usedCustomQuest))
            {
                if (old.usedCustomPc)
                {
                    usage.pcSize = old.pcSize;
                    usage.usedCustomPc = true;
                    usage.pcRevertSize = old.pcRevertSize;
                }
                if (old.usedCustomQuest)
                {
                    usage.questSize = old.questSize;
                    usage.usedCustomQuest = true;
                    usage.questRevertSize = old.questRevertSize;
                }
            }

            map[path] = usage;
            window.textureUsages.Add(usage);
            return usage;
        }

        private static void AddTextureLink(KaleidoTextureUsage usage, GameObject sceneObject, Material material, string propertyName)
        {
            if (usage == null) return;
            if (usage.links == null) usage.links = new List<KaleidoTextureLink>();
            for (int i = 0; i < usage.links.Count; i++)
            {
                KaleidoTextureLink existing = usage.links[i];
                if (existing == null) continue;
                if (existing.sceneObject == sceneObject && existing.material == material
                    && string.Equals(existing.propertyName, propertyName, StringComparison.Ordinal))
                    return;
            }
            usage.links.Add(new KaleidoTextureLink
            {
                sceneObject = sceneObject,
                material = material,
                propertyName = propertyName
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
                    if (KaleidoVRCOptimizerEval.IsEditorOnly(skinned.gameObject)) continue;
                    report.skinnedMeshes++;
                    AccumulateMesh(skinned.sharedMesh, report);
                    AccumulateMaterials(skinned.sharedMaterials, uniqueMats, uniqueTex, report);
                    if (skinned.bones != null && skinned.bones.Length > report.bones) report.bones = skinned.bones.Length;
                    if (skinned.updateWhenOffscreen) report.notes.Add(skinned.name + ": Update When Offscreen is on (CPU cost when culled).");
                }

                foreach (MeshRenderer meshRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (KaleidoVRCOptimizerEval.IsEditorOnly(meshRenderer.gameObject)) continue;
                    report.meshRenderers++;
                    MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                    if (filter != null) AccumulateMesh(filter.sharedMesh, report);
                    AccumulateMaterials(meshRenderer.sharedMaterials, uniqueMats, uniqueTex, report);
                }

                report.physBones += KaleidoVRCOptimizerHelpers.CountComponents(root, physBoneType);
                report.physBoneColliders += KaleidoVRCOptimizerHelpers.CountComponents(root, physBoneColliderType);
                report.contacts += KaleidoVRCOptimizerHelpers.CountComponents(root, contactType);
                report.contacts += KaleidoVRCOptimizerHelpers.CountComponents(root, contactSenderType);
                int unityConstraints;
                int vrcConstraints;
                KaleidoVRCOptimizerHelpers.CountConstraints(root, out unityConstraints, out vrcConstraints);
                report.unityConstraints += unityConstraints;
                report.vrcConstraints += vrcConstraints;
                report.constraints += unityConstraints + vrcConstraints;
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

        private static void BuildHints(KaleidoOptimizerReport report, bool quest)
        {
            if (report.meshReadWriteDisabled) report.notes.Add("Enable Mesh Read/Write. VRChat ranks any avatar with it off as Very Poor and the SDK blocks upload.");
            if (quest)
            {
                if (report.triangles > 20000) report.notes.Add("Poor allows 20k triangles. Over that, mobile viewers cannot see the avatar without Show Avatar.");
                else if (report.triangles > 10000) report.notes.Add("VRChat recommends under 10k triangles on Android. Good is 10k, Excellent is 7.5k.");
                if (report.materialSlots > 4) report.notes.Add("Poor allows 4 material slots. Atlas toward 1 material for Excellent/Good on mobile.");
                if (report.skinnedMeshes > 2) report.notes.Add("Poor allows 2 skinned meshes. Aim for 1 skinned mesh on mobile.");
                if (report.textureBytesEstimate > 40L * 1024 * 1024) report.notes.Add("Texture memory Poor is 40 MB. Drop max size; Crunch does not reduce this number.");
                if (report.physBones > 8) report.notes.Add("VRChat strips PhysBones if you exceed 8 components.");
                if (report.lights > 0) report.notes.Add("Android disables avatar lights.");
                if (report.unityConstraints > 0) report.notes.Add(report.unityConstraints + " Unity constraint(s) are still on the avatar. Android disables those. Use Fix on Rank to convert them with the VRChat SDK.");
                if (report.audioSources > 0) report.notes.Add("Audio sources are disabled on Android avatars.");
            }
            else
            {
                if (report.triangles > 70000) report.notes.Add("Triangles are above 70k (Poor/Very Poor cutoff). Decimate in Blender.");
                else if (report.triangles > 32000) report.notes.Add("Above Excellent (32k triangles).");
                if (report.materialSlots > 32) report.notes.Add("Material slots are above Poor (32).");
                if (report.textureBytesEstimate > 75L * 1024 * 1024) report.notes.Add("Texture memory is past Good (75 MB). Drop max size.");
                if (report.lights > 0) report.notes.Add("Any realtime light is already worse than Excellent (0).");
                if (report.audioSources > 1) report.notes.Add("Excellent allows 1 audio source.");
                if (report.nonBc5Normals != null && report.nonBc5Normals.Count > 0)
                    report.notes.Add(report.nonBc5Normals.Count + " normal map(s) are not BC5. BC5 matches BC7 VRAM with better normals.");
            }
            if (report.grabPasses > 0) report.notes.Add("GrabPass shaders are very expensive. VRChat rank does not count them. " + report.grabPasses + " found.");
            if (report.anyStateTransitions > 50) report.notes.Add("Any State transitions are checked every frame. Around 50 is a healthy cap. This avatar has " + report.anyStateTransitions + ".");
            if (report.writeDefaultsMixed) report.notes.Add("Write Defaults is mixed across animator states. Unity wants all on or all off.");
            if (report.emptyStateCount > 0) report.notes.Add(report.emptyStateCount + " animator state(s) have no motion. Use Fix on Rank to assign the shared empty clip.");
            if (report.blendshapeTriangles > 32000) report.notes.Add("Blendshape triangles are above 32k. Split so only one mesh keeps the shapes.");
            if (report.crunchedTextures != null && report.crunchedTextures.Count > 0) report.notes.Add(report.crunchedTextures.Count + " crunch-compressed texture(s). Crunch does not lower VRChat texture memory.");
            if (report.missingStreamingCount > 0) report.notes.Add(report.missingStreamingCount + " mipmapped texture(s) have streaming mip maps off. VRChat expects them on. Use Fix on Rank.");
            if (!quest && report.unityConstraints > 0) report.notes.Add(report.unityConstraints + " Unity constraint(s) are still on the avatar. Use Fix on Rank to convert them with the VRChat SDK so Play Mode matches what the client loads.");
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
                            KaleidoTextureKind kind = KaleidoVRCOptimizerHelpers.ClassifyTexture(path, textureImporter);
                            string formatHint = DescribePlannedTextureFormat(window, textureImporter, kind);
                            string line = "Texture (" + kind + "): " + path;
                            if (!string.IsNullOrEmpty(formatHint)) line += " [" + formatHint + "]";
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
                    else if (!window.IsQuestWorkspace && window.optimizeAudio && importer is AudioImporter audioImporter)
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

                if (window.IsQuestWorkspace && window.optimizeMaterials)
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

        private static bool ApplyStreamingMipmaps(KaleidoVRCOptimizer window, TextureImporter importer, bool write)
        {
            if (window == null || importer == null || !window.textureEnableStreamingMipmaps) return false;
            bool mipsOn = window.textureApplyMipmaps ? window.textureEnableMipmaps : importer.mipmapEnabled;
            if (!mipsOn || importer.streamingMipmaps) return false;
            if (write) importer.streamingMipmaps = true;
            return true;
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
            KaleidoTextureUsage row = FindTextureUsage(window, path);
            if (row != null)
            {
                pcSize = GetPlannedRowSize(window, row, false);
                questSize = GetPlannedRowSize(window, row, true);
            }
            bool applyPcSize = applySize || (row != null && row.usedCustomPc);
            bool applyQuestSize = applySize || (row != null && row.usedCustomQuest);
            bool customPc = row != null && row.usedCustomPc;
            bool customQuest = row != null && row.usedCustomQuest;
            bool questWorkspace = window.IsQuestWorkspace;
            bool writePcSize = applyPcSize && AllowMaxSizeWrite(CurrentPlatformMaxSize(importer, false), pcSize, customPc);
            bool writeQuestSize = applyQuestSize && AllowMaxSizeWrite(CurrentPlatformMaxSize(importer, true), questSize, customQuest);

            bool dirty = false;
            bool normalHq = normal && window.higherQualityNormalMaps;

            if (questWorkspace)
            {
                TextureImporterFormat androidFormat = ToAndroidFormat(window.androidTexFormat, normalHq);
                if (ApplyPlatform(importer, "Android", writeQuestSize, questSize, window.applyAndroidTexFormat, androidFormat, TextureImporterCompression.Compressed, false, write))
                    dirty = true;
                if (ApplyStreamingMipmaps(window, importer, write)) dirty = true;
                return dirty;
            }

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

            if (ApplyStreamingMipmaps(window, importer, write)) dirty = true;

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

            if (writePcSize && importer.maxTextureSize != pcSize)
            {
                if (write) importer.maxTextureSize = pcSize;
                dirty = true;
            }

            TextureImporterFormat pcFormat = ToPcFormat(window.pcTexFormat, normalHq, importer, normal);
            if (ApplyPlatform(importer, "Standalone", writePcSize, pcSize, window.applyPcTexFormat, pcFormat, wantedCompression, window.textureDisableCrunch && !window.textureEnableCrunch, write))
                dirty = true;

            return dirty;
        }

        private static int CurrentPlatformMaxSize(TextureImporter importer, bool questPlatform)
        {
            if (importer == null) return 0;
            string platform = questPlatform ? "Android" : "Standalone";
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            if (settings != null && settings.overridden) return settings.maxTextureSize;
            return importer.maxTextureSize;
        }

        private static bool AllowMaxSizeWrite(int current, int planned, bool custom)
        {
            if (planned <= 0) return false;
            if (!custom && current > 0 && planned > current) return false;
            return true;
        }

        public static void GetTypeSizes(KaleidoVRCOptimizer window, KaleidoTextureKind kind, out bool apply, out int pc, out int quest)
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

        public static TextureImporterFormat GetPlannedTextureFormat(KaleidoVRCOptimizer window, TextureImporter importer, KaleidoTextureKind kind)
        {
            bool normal = kind == KaleidoTextureKind.Normal;
            bool normalHq = normal && window.higherQualityNormalMaps;
            if (window.IsQuestWorkspace) return ToAndroidFormat(window.androidTexFormat, normalHq);
            return ToPcFormat(window.pcTexFormat, normalHq, importer, normal);
        }

        private static string DescribePlannedTextureFormat(KaleidoVRCOptimizer window, TextureImporter importer, KaleidoTextureKind kind)
        {
            bool normal = kind == KaleidoTextureKind.Normal;
            bool normalHq = normal && window.higherQualityNormalMaps;
            if (window.IsQuestWorkspace)
            {
                if (!window.applyAndroidTexFormat) return "";
                return ToAndroidFormat(window.androidTexFormat, normalHq).ToString();
            }
            if (!window.applyPcTexFormat) return "";
            if (normalHq) return TextureImporterFormat.BC5.ToString();
            if (window.pcTexFormat == KaleidoPcTexFormat.HighQuality) return "CompressedHQ";
            if (window.pcTexFormat == KaleidoPcTexFormat.Automatic) return "Automatic";
            return ToPcFormat(window.pcTexFormat, false, importer, normal).ToString();
        }

        private static TextureImporterFormat ToPcFormat(KaleidoPcTexFormat format, bool normalHq, TextureImporter importer, bool classifiedNormal)
        {
            if (normalHq) return TextureImporterFormat.BC5;
            switch (format)
            {
                case KaleidoPcTexFormat.AutoBc7Dxt1:
                    if (classifiedNormal
                        || (importer != null && (importer.textureType == TextureImporterType.NormalMap || importer.DoesSourceTextureHaveAlpha())))
                        return TextureImporterFormat.BC7;
                    return TextureImporterFormat.DXT1;
                case KaleidoPcTexFormat.BC7: return TextureImporterFormat.BC7;
                case KaleidoPcTexFormat.DXT5: return TextureImporterFormat.DXT5;
                case KaleidoPcTexFormat.DXT1: return TextureImporterFormat.DXT1;
                case KaleidoPcTexFormat.BC5: return TextureImporterFormat.BC5;
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
            if (window.IsQuestWorkspace)
            {
                if (window.meshEnableReadWrite && !importer.isReadable)
                {
                    if (write) importer.isReadable = true;
                    dirty = true;
                }
                if (window.applySkinWeights && importer.skinWeights != ModelImporterSkinWeights.Standard)
                {
                    if (write) importer.skinWeights = ModelImporterSkinWeights.Standard;
                    dirty = true;
                }
                return dirty;
            }

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

            if (!window.IsQuestWorkspace && window.optimizeAnimators)
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

                if (window.IsQuestWorkspace && window.disableCamerasOnAvatar)
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

                if (!window.IsQuestWorkspace && window.optimizeParticles)
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
            bool quest = window.IsQuestWorkspace;
            ShadowCastingMode shadow = window.rendererDisableShadows ? ShadowCastingMode.Off : renderer.shadowCastingMode;
            LightProbeUsage probes = window.rendererDisableProbes ? LightProbeUsage.Off : renderer.lightProbeUsage;
            ReflectionProbeUsage reflections = window.rendererDisableProbes ? ReflectionProbeUsage.Off : renderer.reflectionProbeUsage;
            MotionVectorGenerationMode motion = window.rendererDisableMotionVectors
                ? MotionVectorGenerationMode.ForceNoMotion
                : renderer.motionVectorGenerationMode;

            if (quest && window.rendererDisableShadows && renderer.shadowCastingMode != ShadowCastingMode.Off) dirty = true;
            if (!quest && window.rendererDisableReceiveShadows && renderer.receiveShadows) dirty = true;
            if (!quest && window.rendererDisableProbes && (renderer.lightProbeUsage != LightProbeUsage.Off || renderer.reflectionProbeUsage != ReflectionProbeUsage.Off)) dirty = true;
            if (!quest && window.rendererDisableMotionVectors && renderer.motionVectorGenerationMode != MotionVectorGenerationMode.ForceNoMotion) dirty = true;

            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                if (!quest && window.rendererDisableUpdateWhenOffscreen && skinned.updateWhenOffscreen) dirty = true;
                if (quest && window.rendererForceBone4 && skinned.quality != SkinQuality.Bone4) dirty = true;
                if (!quest && window.rendererRecalculateBounds && skinned.sharedMesh != null) dirty = true;
            }

            if (!dirty) return false;
            if (!write) return true;

            if (quest && window.rendererDisableShadows) renderer.shadowCastingMode = shadow;
            if (!quest && window.rendererDisableReceiveShadows) renderer.receiveShadows = false;
            if (!quest && window.rendererDisableProbes)
            {
                renderer.lightProbeUsage = probes;
                renderer.reflectionProbeUsage = reflections;
            }
            if (!quest && window.rendererDisableMotionVectors) renderer.motionVectorGenerationMode = motion;

            if (skinned != null)
            {
                if (!quest && window.rendererDisableUpdateWhenOffscreen) skinned.updateWhenOffscreen = false;
                if (quest && window.rendererForceBone4) skinned.quality = SkinQuality.Bone4;
                if (!quest && window.rendererRecalculateBounds && skinned.sharedMesh != null)
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
