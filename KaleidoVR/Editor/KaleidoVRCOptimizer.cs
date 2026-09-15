// KaleidoVR VRChat Model Optimizer
// Created and maintained by KaleidoVR - https://kalivr.com
// Copyright (c) 2026 KaleidoVR. All rights reserved.
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
        public bool includeAvatar = true;
        public bool includeSpecial = false;

        public bool optimizeTextures = true;
        public bool applyAlbedoSize = false;
        public int albedoPc = 2048;
        public int albedoQuest = 1024;
        public bool applyNormalSize = false;
        public int normalPc = 2048;
        public int normalQuest = 1024;
        public bool applyMaskSize = false;
        public int maskPc = 1024;
        public int maskQuest = 512;
        public bool applyEmissionSize = false;
        public int emissionPc = 1024;
        public int emissionQuest = 512;
        public bool applyMatcapSize = false;
        public int matcapPc = 512;
        public int matcapQuest = 256;
        public bool applyOtherSize = false;
        public int otherPc = 1024;
        public int otherQuest = 512;
        public bool applyPcTexFormat = false;
        public KaleidoPcTexFormat pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
        public bool applyAndroidTexFormat = false;
        public KaleidoAndroidTexFormat androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
        public bool textureEnableReadWrite = false;
        public bool textureDisableReadWrite = true;
        public bool textureApplyMipmaps = true;
        public bool textureEnableMipmaps = true;
        public bool textureEnableStreamingMipmaps = true;
        public bool textureDisableStreamingMipmaps = false;
        public bool textureDisableCrunch = true;
        public bool textureApplyAniso = true;
        public int textureAniso = 1;
        public bool autoDetectNormalMaps = true;
        public bool autoLinearMaskMaps = true;
        public bool autoSrgbMaskMaps = false;
        public bool higherQualityNormalMaps = true;
        public bool alphaIsTransparencyOnAlbedo = false;
        public bool alphaIsTransparencyOffAlbedo = false;

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
        public bool rendererDisableUpdateWhenOffscreen = false;
        public bool rendererDisableShadows = false;
        public bool rendererDisableReceiveShadows = false;
        public bool rendererDisableProbes = false;
        public bool rendererDisableMotionVectors = false;
        public bool rendererForceBone4 = false;
        public bool rendererRecalculateBounds = false;
        public bool applyToPrefabAssets = true;
        public bool optimizeParticles = false;
        public bool disableLightsOnAvatar = false;
        public bool enableLightsOnAvatar = false;

        public bool optimizeAudio = true;
        public bool audioLoadInBackground = false;
        public bool audioApplyVorbis = false;
        public float audioQuality = 0.7f;

        public bool optimizeAnimators = true;
        public bool animatorCullWhenOffscreen = false;

        public KaleidoMeshCompressionChoice meshCompression = KaleidoMeshCompressionChoice.Off;
        public bool applyMeshCompression = false;
        public bool meshForceHumanoid = false;
        public bool meshStripBlendShapes = false;
        public bool optimizeMaterials = false;
        public bool materialEnableGpuInstancing = false;
        public bool disableCamerasOnAvatar = false;
        public bool enableCamerasOnAvatar = false;
        public bool audioForceToMono = false;
        public bool audioForceToStereo = false;
        public bool meshRestoreBlendShapes = false;
        public bool textureEnableCrunch = false;
        public int textureCrunchQuality = 50;
        public bool avatarApplyOnUpload = false;
        public bool avatarMergeSkinnedMeshes = true;
        public bool avatarMergeIdenticalSlots = true;
        public bool avatarShuffleSlots = true;
        public bool avatarOptimizeBlendShapes = true;
        public bool avatarMergeSameRatioShapes = false;
        public bool avatarMmdCompatibility = true;
        public bool avatarRemoveUnusedComponents = true;
        public bool avatarRemoveUnusedGameObjects = false;
        public bool avatarStripUnusedBones = true;
        public bool avatarOptimizePhysBones = true;
        public bool avatarOptimizeFxLayer = false;
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
        public static readonly string VERSION = "1.0.62";
        public const float WindowMinWidth = 660f;
        public const float WindowMinHeight = 720f;
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

        public bool applyAlbedoSize = false;
        public int albedoPc = 2048;
        public int albedoQuest = 1024;
        public bool applyNormalSize = false;
        public int normalPc = 2048;
        public int normalQuest = 1024;
        public bool applyMaskSize = false;
        public int maskPc = 1024;
        public int maskQuest = 512;
        public bool applyEmissionSize = false;
        public int emissionPc = 1024;
        public int emissionQuest = 512;
        public bool applyMatcapSize = false;
        public int matcapPc = 512;
        public int matcapQuest = 256;
        public bool applyOtherSize = false;
        public int otherPc = 1024;
        public int otherQuest = 512;

        public bool optimizeTextures = true;
        public bool applyPcTexFormat = false;
        public KaleidoPcTexFormat pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
        public bool applyAndroidTexFormat = false;
        public KaleidoAndroidTexFormat androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
        public bool textureEnableReadWrite = false;
        public bool textureDisableReadWrite = true;
        public bool textureApplyMipmaps = true;
        public bool textureEnableMipmaps = true;
        public bool textureEnableStreamingMipmaps = true;
        public bool textureDisableStreamingMipmaps = false;
        public bool textureDisableCrunch = true;
        public bool textureApplyAniso = true;
        public int textureAniso = 1;
        public bool autoDetectNormalMaps = true;
        public bool autoLinearMaskMaps = true;
        public bool autoSrgbMaskMaps = false;
        public bool higherQualityNormalMaps = true;
        public bool alphaIsTransparencyOnAlbedo = false;
        public bool alphaIsTransparencyOffAlbedo = false;

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
        public bool rendererDisableUpdateWhenOffscreen = false;
        public bool rendererDisableShadows = false;
        public bool rendererDisableReceiveShadows = false;
        public bool rendererDisableProbes = false;
        public bool rendererDisableMotionVectors = false;
        public bool rendererForceBone4 = false;
        public bool rendererRecalculateBounds = false;
        public bool optimizeParticles = false;
        public bool disableLightsOnAvatar = false;
        public bool enableLightsOnAvatar = false;

        public bool optimizeAudio = true;
        public bool audioLoadInBackground = false;
        public bool audioApplyVorbis = false;
        public float audioQuality = 0.7f;

        public bool optimizeAnimators = true;
        public bool animatorCullWhenOffscreen = false;

        public bool applyMeshCompression = false;
        public KaleidoMeshCompressionChoice meshCompression = KaleidoMeshCompressionChoice.Off;
        public bool meshForceHumanoid = false;
        public bool meshStripBlendShapes = false;
        public bool optimizeMaterials = false;
        public bool materialEnableGpuInstancing = false;
        public bool disableCamerasOnAvatar = false;
        public bool enableCamerasOnAvatar = false;
        public bool audioForceToMono = false;
        public bool audioForceToStereo = false;
        public bool meshRestoreBlendShapes = false;
        public bool textureEnableCrunch = false;
        public int textureCrunchQuality = 50;
        public bool optimizeSceneExtras = false;
        public bool includeAvatar = true;
        public bool avatarApplyOnUpload = false;
        public bool avatarMergeSkinnedMeshes = true;
        public bool avatarMergeIdenticalSlots = true;
        public bool avatarShuffleSlots = true;
        public bool avatarOptimizeBlendShapes = true;
        public bool avatarMergeSameRatioShapes = false;
        public bool avatarMmdCompatibility = true;
        public bool avatarRemoveUnusedComponents = true;
        public bool avatarRemoveUnusedGameObjects = false;
        public bool avatarStripUnusedBones = true;
        public bool avatarOptimizePhysBones = true;
        public bool avatarOptimizeFxLayer = false;
        public readonly List<string> onUploadPreviewLines = new List<string>();

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
        public bool inventoryFillQueued;
        public int lastAvatarCheckId;
        public bool lastAvatarCheckOk = true;
        public string lastAvatarCheckReason = "";
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
            window.ApplyWindowMinSize();
            window.ApplyWindowIcon();
        }

        private void OnEnable()
        {
            InitializeLocalLogo();
            LoadEditorPreferences();
            KeepSingleAvatarTarget();
            tab = 0;
            ApplyWindowMinSize();
            ApplyWindowIcon();
            KaleidoVRCOptimizerUI.ClearNormalPreviews();
        }

        public void KeepSingleAvatarTarget()
        {
            if (targets == null)
            {
                targets = new List<UnityEngine.Object>();
                return;
            }
            if (targets.Count <= 1) return;
            UnityEngine.Object keep = null;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    keep = targets[i];
                    break;
                }
            }
            targets.Clear();
            if (keep != null) targets.Add(keep);
        }

        private void ApplyWindowMinSize()
        {
            minSize = new Vector2(WindowMinWidth, WindowMinHeight);
            Rect pos = position;
            if (pos.width < WindowMinWidth)
            {
                pos.width = WindowMinWidth;
                position = pos;
            }
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
            includeAvatar = true;
            includeSpecial = false;
            ApplyAvatarTabDefaults(index);

            optimizeTextures = true;
            applyAlbedoSize = applyNormalSize = applyMaskSize = applyEmissionSize = applyMatcapSize = applyOtherSize = false;
            applyPcTexFormat = false;
            applyAndroidTexFormat = false;
            pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
            androidTexFormat = KaleidoAndroidTexFormat.ASTC_6x6;
            textureEnableReadWrite = false;
            textureDisableReadWrite = true;
            textureApplyMipmaps = true;
            textureEnableMipmaps = true;
            textureEnableStreamingMipmaps = true;
            textureDisableStreamingMipmaps = false;
            textureDisableCrunch = true;
            textureEnableCrunch = false;
            textureCrunchQuality = 50;
            textureApplyAniso = true;
            textureAniso = 1;
            autoDetectNormalMaps = true;
            autoLinearMaskMaps = true;
            autoSrgbMaskMaps = false;
            higherQualityNormalMaps = true;
            alphaIsTransparencyOnAlbedo = false;
            alphaIsTransparencyOffAlbedo = false;

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
            meshRestoreBlendShapes = false;

            optimizeRenderers = true;
            rendererDisableUpdateWhenOffscreen = false;
            rendererDisableShadows = false;
            rendererDisableReceiveShadows = false;
            rendererDisableProbes = false;
            rendererDisableMotionVectors = false;
            rendererForceBone4 = false;
            rendererRecalculateBounds = false;
            applyToPrefabAssets = true;
            optimizeParticles = false;
            disableLightsOnAvatar = false;
            enableLightsOnAvatar = false;

            optimizeAudio = true;
            audioForceToMono = false;
            audioForceToStereo = false;
            audioLoadInBackground = false;
            audioApplyVorbis = false;
            audioQuality = 0.7f;

            optimizeAnimators = true;
            animatorCullWhenOffscreen = false;
            optimizeMaterials = false;
            materialEnableGpuInstancing = false;
            disableCamerasOnAvatar = false;
            enableCamerasOnAvatar = false;
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
                applyAlbedoSize = applyNormalSize = applyMaskSize = applyEmissionSize = applyMatcapSize = applyOtherSize = true;
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
                    avatarRemoveUnusedGameObjects = true;
                    applyAlbedoSize = applyNormalSize = applyMaskSize = applyEmissionSize = applyMatcapSize = applyOtherSize = true;
                    meshOptimizeAnimation = true;
                    audioApplyVorbis = true;
                    audioLoadInBackground = true;
                    optimizeParticles = true;
                    optimizeSceneExtras = true;
                    rendererDisableUpdateWhenOffscreen = true;
                    rendererDisableMotionVectors = true;
                    animatorCullWhenOffscreen = true;
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
            p.includeAvatar = includeAvatar;
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
                textureEnableReadWrite = textureEnableReadWrite,
                textureDisableReadWrite = textureDisableReadWrite,
                textureApplyMipmaps = textureApplyMipmaps, textureEnableMipmaps = textureEnableMipmaps,
                textureEnableStreamingMipmaps = textureEnableStreamingMipmaps,
                textureDisableStreamingMipmaps = textureDisableStreamingMipmaps,
                textureDisableCrunch = textureDisableCrunch, textureEnableCrunch = textureEnableCrunch,
                textureCrunchQuality = textureCrunchQuality,
                textureApplyAniso = textureApplyAniso, textureAniso = textureAniso,
                autoDetectNormalMaps = autoDetectNormalMaps, autoLinearMaskMaps = autoLinearMaskMaps,
                autoSrgbMaskMaps = autoSrgbMaskMaps,
                higherQualityNormalMaps = higherQualityNormalMaps,
                alphaIsTransparencyOnAlbedo = alphaIsTransparencyOnAlbedo,
                alphaIsTransparencyOffAlbedo = alphaIsTransparencyOffAlbedo,
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
                disableLightsOnAvatar = disableLightsOnAvatar, enableLightsOnAvatar = enableLightsOnAvatar,
                optimizeMaterials = optimizeMaterials, materialEnableGpuInstancing = materialEnableGpuInstancing,
                disableCamerasOnAvatar = disableCamerasOnAvatar, enableCamerasOnAvatar = enableCamerasOnAvatar,
                optimizeAudio = optimizeAudio, audioLoadInBackground = audioLoadInBackground,
                audioApplyVorbis = audioApplyVorbis, audioQuality = audioQuality,
                optimizeAnimators = optimizeAnimators, animatorCullWhenOffscreen = animatorCullWhenOffscreen,
                applyMeshCompression = applyMeshCompression, meshCompression = meshCompression,
                meshForceHumanoid = meshForceHumanoid, meshStripBlendShapes = meshStripBlendShapes,
                meshRestoreBlendShapes = meshRestoreBlendShapes,
                audioForceToMono = audioForceToMono, audioForceToStereo = audioForceToStereo,
                includeAvatar = includeAvatar,
                avatarApplyOnUpload = avatarApplyOnUpload,
                avatarMergeSkinnedMeshes = avatarMergeSkinnedMeshes,
                avatarMergeIdenticalSlots = avatarMergeIdenticalSlots,
                avatarShuffleSlots = avatarShuffleSlots,
                avatarOptimizeBlendShapes = avatarOptimizeBlendShapes,
                avatarMergeSameRatioShapes = avatarMergeSameRatioShapes,
                avatarMmdCompatibility = avatarMmdCompatibility,
                avatarRemoveUnusedComponents = avatarRemoveUnusedComponents,
                avatarRemoveUnusedGameObjects = avatarRemoveUnusedGameObjects,
                avatarStripUnusedBones = avatarStripUnusedBones,
                avatarOptimizePhysBones = avatarOptimizePhysBones,
                avatarOptimizeFxLayer = avatarOptimizeFxLayer
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
            bool avatar = !respectIncludes || p.includeAvatar;
            bool spec = !respectIncludes || p.includeSpecial;

            includeTextures = p.includeTextures;
            includeMeshes = p.includeMeshes;
            includeRenderers = p.includeRenderers;
            includeAudio = p.includeAudio;
            includeAnimators = p.includeAnimators;
            includeAvatar = p.includeAvatar;
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
                textureEnableReadWrite = p.textureEnableReadWrite;
                textureDisableReadWrite = p.textureDisableReadWrite;
                if (textureEnableReadWrite) textureDisableReadWrite = false;
                textureApplyMipmaps = p.textureApplyMipmaps; textureEnableMipmaps = p.textureEnableMipmaps;
                textureEnableStreamingMipmaps = p.textureEnableStreamingMipmaps;
                textureDisableStreamingMipmaps = p.textureDisableStreamingMipmaps;
                if (textureEnableStreamingMipmaps) textureDisableStreamingMipmaps = false;
                textureDisableCrunch = p.textureDisableCrunch;
                textureApplyAniso = p.textureApplyAniso; textureAniso = p.textureAniso;
                autoDetectNormalMaps = p.autoDetectNormalMaps;
                autoLinearMaskMaps = p.autoLinearMaskMaps;
                autoSrgbMaskMaps = p.autoSrgbMaskMaps;
                if (autoLinearMaskMaps) autoSrgbMaskMaps = false;
                higherQualityNormalMaps = p.higherQualityNormalMaps;
                alphaIsTransparencyOnAlbedo = p.alphaIsTransparencyOnAlbedo;
                alphaIsTransparencyOffAlbedo = p.alphaIsTransparencyOffAlbedo;
                if (alphaIsTransparencyOnAlbedo) alphaIsTransparencyOffAlbedo = false;
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
            if (avatar)
            {
                avatarApplyOnUpload = p.avatarApplyOnUpload;
                avatarMergeSkinnedMeshes = p.avatarMergeSkinnedMeshes;
                avatarMergeIdenticalSlots = p.avatarMergeIdenticalSlots;
                avatarShuffleSlots = p.avatarShuffleSlots;
                avatarOptimizeBlendShapes = p.avatarOptimizeBlendShapes;
                avatarMergeSameRatioShapes = p.avatarMergeSameRatioShapes;
                avatarMmdCompatibility = p.avatarMmdCompatibility;
                avatarRemoveUnusedComponents = p.avatarRemoveUnusedComponents;
                avatarRemoveUnusedGameObjects = p.avatarRemoveUnusedGameObjects;
                avatarStripUnusedBones = p.avatarStripUnusedBones;
                avatarOptimizePhysBones = p.avatarOptimizePhysBones;
                avatarOptimizeFxLayer = p.avatarOptimizeFxLayer;
            }
            if (spec)
            {
                applyMeshCompression = p.applyMeshCompression;
                meshCompression = p.meshCompression;
                meshForceHumanoid = p.meshForceHumanoid;
                meshStripBlendShapes = p.meshStripBlendShapes;
                meshRestoreBlendShapes = p.meshRestoreBlendShapes;
                if (meshRestoreBlendShapes) meshStripBlendShapes = false;
                rendererRecalculateBounds = p.rendererRecalculateBounds;
                optimizeMaterials = p.optimizeMaterials;
                materialEnableGpuInstancing = p.materialEnableGpuInstancing;
                disableLightsOnAvatar = p.disableLightsOnAvatar;
                enableLightsOnAvatar = p.enableLightsOnAvatar;
                if (enableLightsOnAvatar) disableLightsOnAvatar = false;
                disableCamerasOnAvatar = p.disableCamerasOnAvatar;
                enableCamerasOnAvatar = p.enableCamerasOnAvatar;
                if (enableCamerasOnAvatar) disableCamerasOnAvatar = false;
                audioForceToMono = p.audioForceToMono;
                audioForceToStereo = p.audioForceToStereo;
                if (audioForceToMono) audioForceToStereo = false;
                textureEnableCrunch = p.textureEnableCrunch;
                textureCrunchQuality = ClampCrunchQuality(p.textureCrunchQuality);
                if (textureEnableCrunch) textureDisableCrunch = false;
                applyPcTexFormat = p.applyPcTexFormat; pcTexFormat = p.pcTexFormat;
                applyAndroidTexFormat = p.applyAndroidTexFormat; androidTexFormat = p.androidTexFormat;
            }
            else
            {
                applyMeshCompression = false;
                meshForceHumanoid = false;
                meshStripBlendShapes = false;
                meshRestoreBlendShapes = false;
                rendererRecalculateBounds = false;
                optimizeMaterials = false;
                materialEnableGpuInstancing = false;
                disableLightsOnAvatar = false;
                enableLightsOnAvatar = false;
                disableCamerasOnAvatar = false;
                enableCamerasOnAvatar = false;
                audioForceToMono = false;
                audioForceToStereo = false;
                textureEnableCrunch = false;
                applyPcTexFormat = false;
                applyAndroidTexFormat = false;
            }
            optimizeSceneExtras = disableLightsOnAvatar || enableLightsOnAvatar || disableCamerasOnAvatar || enableCamerasOnAvatar || optimizeParticles;
        }

        public static int ClampCrunchQuality(int quality)
        {
            return quality < 1 ? 50 : Mathf.Clamp(quality, 1, 100);
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
            includeAvatar = GetBool("IncAvatar", true);
            includeSpecial = GetBool("IncSpec", false);
            avatarApplyOnUpload = GetBool("AvUp", false);
            avatarMergeSkinnedMeshes = GetBool("AvMerge", true);
            avatarMergeIdenticalSlots = GetBool("AvSlots", true);
            avatarShuffleSlots = GetBool("AvShuffle", true);
            avatarOptimizeBlendShapes = GetBool("AvShape", true);
            avatarMergeSameRatioShapes = GetBool("AvRatio", false);
            avatarMmdCompatibility = GetBool("AvMmd", true);
            avatarRemoveUnusedComponents = GetBool("AvComp", true);
            avatarRemoveUnusedGameObjects = GetBool("AvGo", false);
            avatarStripUnusedBones = GetBool("AvBone", true);
            avatarOptimizePhysBones = GetBool("AvPb", true);
            avatarOptimizeFxLayer = GetBool("AvFx", false);

            optimizeTextures = GetBool("OptTex", true);
            applyAlbedoSize = GetBool("ASize", false);
            albedoPc = GetInt("APc", 2048); albedoQuest = GetInt("AQ", 1024);
            applyNormalSize = GetBool("NSize", false);
            normalPc = GetInt("NPc", 2048); normalQuest = GetInt("NQ", 1024);
            applyMaskSize = GetBool("MSize", false);
            maskPc = GetInt("MPc", 1024); maskQuest = GetInt("MQ", 512);
            applyEmissionSize = GetBool("ESize", false);
            emissionPc = GetInt("EPc", 1024); emissionQuest = GetInt("EQ", 512);
            applyMatcapSize = GetBool("CSize", false);
            matcapPc = GetInt("CPc", 512); matcapQuest = GetInt("CQ", 256);
            applyOtherSize = GetBool("OSize", false);
            otherPc = GetInt("OPc", 1024); otherQuest = GetInt("OQ", 512);
            applyPcTexFormat = GetBool("PcFmtOn", false);
            applyAndroidTexFormat = GetBool("AndFmtOn", false);
            pcTexFormat = (KaleidoPcTexFormat)GetInt("PcFmt", (int)KaleidoPcTexFormat.AutoBc7Dxt1);
            androidTexFormat = (KaleidoAndroidTexFormat)GetInt("AndFmt", (int)KaleidoAndroidTexFormat.ASTC_6x6);
            textureEnableReadWrite = GetBool("TexRWOn", false);
            textureDisableReadWrite = GetBool("TexRW", true);
            if (textureEnableReadWrite) textureDisableReadWrite = false;
            textureApplyMipmaps = GetBool("TexMipsOn", true);
            textureEnableMipmaps = GetBool("TexMips", true);
            textureEnableStreamingMipmaps = GetBool("TexStreamOn", true);
            textureDisableStreamingMipmaps = GetBool("TexStreamOff", false);
            if (textureEnableStreamingMipmaps) textureDisableStreamingMipmaps = false;
            textureDisableCrunch = GetBool("TexCrunch", true);
            textureEnableCrunch = GetBool("TexCrunchOn", false);
            if (textureEnableCrunch) textureDisableCrunch = false;
            textureCrunchQuality = ClampCrunchQuality(GetInt("TexCrunchQ", 50));
            textureApplyAniso = GetBool("TexAnisoOn", true);
            textureAniso = GetInt("TexAniso", 1);
            autoDetectNormalMaps = GetBool("TexNorm", true);
            autoLinearMaskMaps = GetBool("TexLinear", true);
            autoSrgbMaskMaps = GetBool("TexMaskSrgb", false);
            if (autoLinearMaskMaps) autoSrgbMaskMaps = false;
            higherQualityNormalMaps = GetBool("TexNormHQ", true);
            alphaIsTransparencyOnAlbedo = GetBool("TexAlpha", false);
            alphaIsTransparencyOffAlbedo = GetBool("TexAlphaOff", false);
            if (alphaIsTransparencyOnAlbedo) alphaIsTransparencyOffAlbedo = false;

            optimizeMeshes = GetBool("OptMesh", true);
            applyMeshCompression = GetBool("MeshCompOn", false);
            meshCompression = (KaleidoMeshCompressionChoice)GetInt("MeshComp", (int)KaleidoMeshCompressionChoice.Off);
            meshEnableReadWrite = GetBool("MeshEnableRW", true);
            meshOptimizePolygons = GetBool("MeshPoly", true);
            meshOptimizeVertices = GetBool("MeshVert", true);
            meshWeldVertices = GetBool("MeshWeld", false);
            meshKeepBlendShapes = GetBool("MeshBS", true);
            meshStripBlendShapes = GetBool("MeshBSOff", false);
            meshRestoreBlendShapes = GetBool("MeshBSOn", false);
            if (meshRestoreBlendShapes) meshStripBlendShapes = false;
            meshDisableQuads = GetBool("MeshQuads", true);
            meshDisableLightmapUVs = GetBool("MeshLM", true);
            meshDisableImportLightsCameras = GetBool("MeshLC", true);
            meshOptimizeAnimation = GetBool("MeshAnim", false);
            applySkinWeights = GetBool("SkinOn", false);
            skinWeights = (KaleidoSkinWeightChoice)GetInt("SkinW", (int)KaleidoSkinWeightChoice.FourBones);
            meshForceHumanoid = GetBool("MeshHum", false);

            optimizeRenderers = GetBool("OptRend", true);
            rendererDisableUpdateWhenOffscreen = GetBool("RendOff", false);
            rendererDisableShadows = GetBool("RendShad", false);
            rendererDisableReceiveShadows = GetBool("RendRecv", false);
            rendererDisableProbes = GetBool("RendProbe", false);
            rendererDisableMotionVectors = GetBool("RendMV", false);
            rendererForceBone4 = GetBool("RendBone4", false);
            rendererRecalculateBounds = GetBool("RendBounds", false);

            optimizeAudio = GetBool("OptAud", true);
            audioForceToMono = GetBool("AudMono", false);
            audioForceToStereo = GetBool("AudStereo", false);
            if (audioForceToMono) audioForceToStereo = false;
            audioLoadInBackground = GetBool("AudBG", false);
            audioApplyVorbis = GetBool("AudVorb", false);
            audioQuality = EditorPrefs.HasKey(PrefsPrefix + "AudQ") ? EditorPrefs.GetFloat(PrefsPrefix + "AudQ") : 0.7f;

            optimizeAnimators = GetBool("OptAnim", true);
            animatorCullWhenOffscreen = GetBool("AnimCull", false);

            optimizeMaterials = GetBool("OptMat", false);
            materialEnableGpuInstancing = GetBool("MatGPU", false);

            optimizeSceneExtras = GetBool("OptExtra", false);
            disableLightsOnAvatar = GetBool("ExtraLight", false);
            enableLightsOnAvatar = GetBool("ExtraLightOn", false);
            if (enableLightsOnAvatar) disableLightsOnAvatar = false;
            disableCamerasOnAvatar = GetBool("ExtraCam", false);
            enableCamerasOnAvatar = GetBool("ExtraCamOn", false);
            if (enableCamerasOnAvatar) disableCamerasOnAvatar = false;
            optimizeParticles = GetBool("ExtraPart", false);
            optimizeSceneExtras = disableLightsOnAvatar || enableLightsOnAvatar || disableCamerasOnAvatar || enableCamerasOnAvatar || optimizeParticles;

            MigrateEditorPreferences();
        }

        private void MigrateEditorPreferences()
        {
            const int currentSchema = 9;
            int schema = GetInt("Schema", 1);
            if (schema >= currentSchema) return;

            if (schema < 2)
            {
                if (pcTexFormat == KaleidoPcTexFormat.HighQuality)
                    pcTexFormat = KaleidoPcTexFormat.AutoBc7Dxt1;
                autoDetectNormalMaps = true;
            }

            if (schema < 3)
            {
                applyPcTexFormat = false;
                applyAndroidTexFormat = false;
            }

            if (schema < 4)
            {
                rendererDisableUpdateWhenOffscreen = false;
                rendererDisableMotionVectors = false;
                animatorCullWhenOffscreen = false;
                audioLoadInBackground = false;
            }

            if (schema < 5)
            {
                if (builtinPresetIndex != 1 && builtinPresetIndex != 3)
                {
                    applyAlbedoSize = false;
                    applyNormalSize = false;
                    applyMaskSize = false;
                    applyEmissionSize = false;
                    applyMatcapSize = false;
                    applyOtherSize = false;
                }
            }

            if (schema < 6)
            {
                ApplySharedSafeDefaults(builtinPresetIndex);
            }

            if (schema < 7)
            {
                avatarMergeSameRatioShapes = false;
            }

            if (schema < 8)
            {
                avatarApplyOnUpload = false;
            }

            if (schema < 9)
            {
                avatarOptimizeFxLayer = false;
            }

            SetInt("Schema", currentSchema);
            PersistSharedSafeDefaults();
        }

        private void ApplySharedSafeDefaults(int presetIndex)
        {
            optimizeTextures = true;
            applyPcTexFormat = false;
            applyAndroidTexFormat = false;
            textureEnableCrunch = false;
            textureDisableCrunch = true;
            textureApplyMipmaps = true;
            textureEnableMipmaps = true;
            textureEnableStreamingMipmaps = true;
            textureDisableStreamingMipmaps = false;

            optimizeMeshes = true;
            meshKeepBlendShapes = true;
            meshStripBlendShapes = false;
            meshRestoreBlendShapes = false;
            meshWeldVertices = false;
            meshOptimizeAnimation = presetIndex == 3;
            applyMeshCompression = false;
            meshForceHumanoid = false;
            if (presetIndex != 1) applySkinWeights = false;

            optimizeAnimators = true;
            if (presetIndex != 3) animatorCullWhenOffscreen = false;

            includeSpecial = false;
            rendererRecalculateBounds = false;
            optimizeMaterials = false;
            materialEnableGpuInstancing = false;
            audioForceToMono = false;
            audioForceToStereo = false;

            if (presetIndex != 1)
            {
                rendererForceBone4 = false;
                rendererDisableShadows = false;
            }

            if (presetIndex != 1 && presetIndex != 3)
            {
                applyAlbedoSize = false;
                applyNormalSize = false;
                applyMaskSize = false;
                applyEmissionSize = false;
                applyMatcapSize = false;
                applyOtherSize = false;
                rendererDisableUpdateWhenOffscreen = false;
                rendererDisableReceiveShadows = false;
                rendererDisableProbes = false;
                rendererDisableMotionVectors = false;
                audioLoadInBackground = false;
                audioApplyVorbis = false;
                optimizeParticles = false;
                disableLightsOnAvatar = false;
                enableLightsOnAvatar = false;
                disableCamerasOnAvatar = false;
                enableCamerasOnAvatar = false;
                optimizeSceneExtras = false;
            }

            ApplyAvatarTabDefaults(presetIndex);
        }

        private void ApplyAvatarTabDefaults(int presetIndex)
        {
            includeAvatar = true;
            avatarApplyOnUpload = false;
            avatarMergeSkinnedMeshes = true;
            avatarMergeIdenticalSlots = true;
            avatarShuffleSlots = true;
            avatarOptimizeBlendShapes = true;
            avatarMergeSameRatioShapes = false;
            avatarMmdCompatibility = true;
            avatarRemoveUnusedComponents = true;
            avatarRemoveUnusedGameObjects = presetIndex == 3;
            avatarStripUnusedBones = true;
            avatarOptimizePhysBones = true;
            avatarOptimizeFxLayer = false;
        }

        private void PersistSharedSafeDefaults()
        {
            SetInt("PcFmt", (int)pcTexFormat);
            SetBool("TexNorm", autoDetectNormalMaps);
            SetBool("PcFmtOn", applyPcTexFormat);
            SetBool("AndFmtOn", applyAndroidTexFormat);
            SetBool("TexCrunch", textureDisableCrunch);
            SetBool("TexCrunchOn", textureEnableCrunch);
            SetBool("TexMipsOn", textureApplyMipmaps);
            SetBool("TexMips", textureEnableMipmaps);
            SetBool("TexStreamOn", textureEnableStreamingMipmaps);
            SetBool("TexStreamOff", textureDisableStreamingMipmaps);
            SetBool("OptTex", optimizeTextures);
            SetBool("OptMesh", optimizeMeshes);
            SetBool("MeshBS", meshKeepBlendShapes);
            SetBool("MeshBSOff", meshStripBlendShapes);
            SetBool("MeshBSOn", meshRestoreBlendShapes);
            SetBool("MeshWeld", meshWeldVertices);
            SetBool("MeshAnim", meshOptimizeAnimation);
            SetBool("MeshCompOn", applyMeshCompression);
            SetBool("MeshHum", meshForceHumanoid);
            SetBool("SkinOn", applySkinWeights);
            SetBool("OptAnim", optimizeAnimators);
            SetBool("AnimCull", animatorCullWhenOffscreen);
            SetBool("IncSpec", includeSpecial);
            SetBool("RendBounds", rendererRecalculateBounds);
            SetBool("OptMat", optimizeMaterials);
            SetBool("MatGPU", materialEnableGpuInstancing);
            SetBool("AudMono", audioForceToMono);
            SetBool("AudStereo", audioForceToStereo);
            SetBool("RendBone4", rendererForceBone4);
            SetBool("RendShad", rendererDisableShadows);
            SetBool("ASize", applyAlbedoSize);
            SetBool("NSize", applyNormalSize);
            SetBool("MSize", applyMaskSize);
            SetBool("ESize", applyEmissionSize);
            SetBool("CSize", applyMatcapSize);
            SetBool("OSize", applyOtherSize);
            SetBool("RendOff", rendererDisableUpdateWhenOffscreen);
            SetBool("RendRecv", rendererDisableReceiveShadows);
            SetBool("RendProbe", rendererDisableProbes);
            SetBool("RendMV", rendererDisableMotionVectors);
            SetBool("AudBG", audioLoadInBackground);
            SetBool("AudVorb", audioApplyVorbis);
            SetBool("ExtraPart", optimizeParticles);
            SetBool("ExtraLight", disableLightsOnAvatar);
            SetBool("ExtraLightOn", enableLightsOnAvatar);
            SetBool("ExtraCam", disableCamerasOnAvatar);
            SetBool("ExtraCamOn", enableCamerasOnAvatar);
            SetBool("IncAvatar", includeAvatar);
            SetBool("AvUp", avatarApplyOnUpload);
            SetBool("AvMerge", avatarMergeSkinnedMeshes);
            SetBool("AvSlots", avatarMergeIdenticalSlots);
            SetBool("AvShuffle", avatarShuffleSlots);
            SetBool("AvShape", avatarOptimizeBlendShapes);
            SetBool("AvRatio", avatarMergeSameRatioShapes);
            SetBool("AvMmd", avatarMmdCompatibility);
            SetBool("AvComp", avatarRemoveUnusedComponents);
            SetBool("AvGo", avatarRemoveUnusedGameObjects);
            SetBool("AvBone", avatarStripUnusedBones);
            SetBool("AvPb", avatarOptimizePhysBones);
            SetBool("AvFx", avatarOptimizeFxLayer);
        }

        public void SaveEditorPreferences()
        {
            SetInt("Schema", 9);
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
            SetBool("IncAvatar", includeAvatar);
            SetBool("IncSpec", includeSpecial);
            SetBool("AvUp", avatarApplyOnUpload);
            SetBool("AvMerge", avatarMergeSkinnedMeshes);
            SetBool("AvSlots", avatarMergeIdenticalSlots);
            SetBool("AvShuffle", avatarShuffleSlots);
            SetBool("AvShape", avatarOptimizeBlendShapes);
            SetBool("AvRatio", avatarMergeSameRatioShapes);
            SetBool("AvMmd", avatarMmdCompatibility);
            SetBool("AvComp", avatarRemoveUnusedComponents);
            SetBool("AvGo", avatarRemoveUnusedGameObjects);
            SetBool("AvBone", avatarStripUnusedBones);
            SetBool("AvPb", avatarOptimizePhysBones);
            SetBool("AvFx", avatarOptimizeFxLayer);

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
            SetBool("TexRWOn", textureEnableReadWrite);
            SetBool("TexRW", textureDisableReadWrite);
            SetBool("TexMipsOn", textureApplyMipmaps);
            SetBool("TexMips", textureEnableMipmaps);
            SetBool("TexStreamOn", textureEnableStreamingMipmaps);
            SetBool("TexStreamOff", textureDisableStreamingMipmaps);
            SetBool("TexCrunch", textureDisableCrunch);
            SetBool("TexCrunchOn", textureEnableCrunch);
            SetInt("TexCrunchQ", ClampCrunchQuality(textureCrunchQuality));
            SetBool("TexAnisoOn", textureApplyAniso);
            SetInt("TexAniso", textureAniso);
            SetBool("TexNorm", autoDetectNormalMaps);
            SetBool("TexLinear", autoLinearMaskMaps);
            SetBool("TexMaskSrgb", autoSrgbMaskMaps);
            SetBool("TexNormHQ", higherQualityNormalMaps);
            SetBool("TexAlpha", alphaIsTransparencyOnAlbedo);
            SetBool("TexAlphaOff", alphaIsTransparencyOffAlbedo);

            SetBool("OptMesh", optimizeMeshes);
            SetBool("MeshCompOn", applyMeshCompression);
            SetInt("MeshComp", (int)meshCompression);
            SetBool("MeshEnableRW", meshEnableReadWrite);
            SetBool("MeshPoly", meshOptimizePolygons);
            SetBool("MeshVert", meshOptimizeVertices);
            SetBool("MeshWeld", meshWeldVertices);
            SetBool("MeshBS", meshKeepBlendShapes);
            SetBool("MeshBSOff", meshStripBlendShapes);
            SetBool("MeshBSOn", meshRestoreBlendShapes);
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
            SetBool("AudStereo", audioForceToStereo);
            SetBool("AudBG", audioLoadInBackground);
            SetBool("AudVorb", audioApplyVorbis);
            EditorPrefs.SetFloat(PrefsPrefix + "AudQ", audioQuality);

            SetBool("OptAnim", optimizeAnimators);
            SetBool("AnimCull", animatorCullWhenOffscreen);

            SetBool("OptMat", optimizeMaterials);
            SetBool("MatGPU", materialEnableGpuInstancing);

            SetBool("OptExtra", optimizeSceneExtras);
            SetBool("ExtraLight", disableLightsOnAvatar);
            SetBool("ExtraLightOn", enableLightsOnAvatar);
            SetBool("ExtraCam", disableCamerasOnAvatar);
            SetBool("ExtraCamOn", enableCamerasOnAvatar);
            SetBool("ExtraPart", optimizeParticles);
            SaveUserProfiles();
            KaleidoVRCOptimizerLogic.SaveTextureRowPrefs(this);
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
                WorkspaceReadyToApply = false;
                SaveEditorPreferences();
            }
        }

        public void InvalidateInventory()
        {
            inventorySignature = "";
        }

        public void RefreshInventoryIfNeeded()
        {
            string signature = BuildTargetSignature();
            if (signature == inventorySignature) return;
            inventorySignature = signature;
            if (inventory != null) inventory.Clear();
            QueueInventoryFill();
        }

        void QueueInventoryFill()
        {
            if (inventoryFillQueued) return;
            inventoryFillQueued = true;
            EditorApplication.delayCall += FillInventoryDeferred;
        }

        void FillInventoryDeferred()
        {
            inventoryFillQueued = false;
            if (this == null) return;
            KaleidoVRCOptimizerLogic.FillInventory(this);
            Repaint();
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
        public bool usedCustomCrunch;
        public int crunchQuality;
        public bool ignorePc;
        public bool ignoreQuest;
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
    public class KaleidoTextureRowPref
    {
        public string path;
        public bool ignorePc;
        public bool ignoreQuest;
        public bool customPc;
        public bool customQuest;
        public bool customCrunch;
        public int pcSize;
        public int questSize;
        public int crunchQuality;
    }

    [Serializable]
    public class KaleidoTextureRowPrefList
    {
        public KaleidoTextureRowPref[] items = new KaleidoTextureRowPref[0];
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
        public bool hasOnUploadEstimate;
        public string onUploadPcRank = "—";
        public string onUploadQuestRank = "—";
        public int onUploadTriangles;
        public int onUploadMaterialSlots;
        public int onUploadUniqueMaterials;
        public int onUploadSkinnedMeshes;
        public int onUploadMeshRenderers;
        public int onUploadBlendShapes;
        public int onUploadBones;
        public int onUploadAnimators;
        public int onUploadLights;
        public int onUploadAudioSources;
        public int onUploadParticleSystems;
        public int onUploadPhysBones;
        public int onUploadPhysBoneColliders;
        public int onUploadContacts;
        public int onUploadConstraints;
        public int onUploadUnityConstraints;
        public int onUploadVrcConstraints;
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
        private static readonly string[] BuiltinNames = { "PC", "Quest", "Standard", "Everything" };
        private static readonly string[] BuiltinSummaries =
        {
            "PC start. Leaves max sizes and compression formats as-is, keeps blend shapes and mesh Read/Write, does not weld verts or rewrite bone weights. Special stays off.",
            "Quest start. Turns on 1K body map caps, leaves ASTC as-is, 4 bone weights, shadow casting off. Still will not weld, strip visemes, or touch Special. Test hair/toggles after apply.",
            "Default start. Suggested 2K PC / 1K Quest sizes stay in the dropdowns but are not written until you tick a type. Does not rewrite compression, skin weights, weld, or force 4-bone quality. Special stays off.",
            "Full pack. Dual 2K/1K caps on, leaves compression formats as-is (Set compression lives on Special), offscreen cull, motion vectors off, Vorbis SFX, particle shadow strip. Scan/Rank includes VRAM, GrabPass, animator, crunch, and animation-swap flags. Special stays off. Uncheck anything you do not want before Apply."
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
        private const float TextureThumb = 52f;
        private const float TextureListInnerPad = 8f;
        private const float TextureNameCol = 200f;
        private const float TextureListAfterIgnore = 8f;
        private const float TextureSectionSidePad = 16f;
        private const float TextureSizeCol = 88f;
        private const float TextureMidCol = 72f;
        private const float TextureIgnoreCol = 52f;
        private const float TextureStatusGap = 4f;
        private const float TextureListScrollPad = 24f;
        private const float TextureStatusWidth = TextureSizeCol + TextureStatusGap + TextureMidCol + TextureStatusGap + TextureSizeCol + TextureStatusGap + TextureIgnoreCol;
        private const float TextureSectionMaxWidth = TextureThumb + 6f + TextureNameCol + TextureStatusWidth + TextureListAfterIgnore + TextureListInnerPad * 2f + 16f + TextureListScrollPad;
        private static GUIStyle sizeCaptionStyle;
        private static GUIStyle sizeValueStyle;
        private static GUIStyle sizeNewStyle;
        private static GUIStyle sizeUpStyle;
        private static GUIStyle textureNameClipStyle;
        private static GUIStyle textureMetaClipStyle;
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

        private static float outlinedPanelSideSpace = 84f;

        private static void BeginCenteredSection(EditorWindow window, float maxWidth)
        {
            float view = window != null ? window.position.width : EditorGUIUtility.currentViewWidth;
            float width = Mathf.Clamp(view - TextureSectionSidePad * 2f, 360f, maxWidth);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.MaxWidth(maxWidth));
        }

        private static void EndCenteredSection()
        {
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void BeginOutlinedPanel(float sideSpace = 84f)
        {
            outlinedPanelSideSpace = sideSpace;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(sideSpace);
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
            GUILayout.Space(outlinedPanelSideSpace);
            EditorGUILayout.EndHorizontal();
            outlinedPanelSideSpace = 84f;
        }

        public static void DrawHeader(Texture2D logo, string version)
        {
            GUIStyle centeredTitleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUIStyle centeredVersionStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Space(10); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (logo != null) { DrawTrimmedLogo(logo, 160f, 100f); }
            else { GUILayout.Label($"...Place your logo at {KaleidoVRCOptimizer.ICON_PATH}...", EditorStyles.miniLabel); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); GUILayout.Space(10);
            GUILayout.Label("KALEIDO VR MODEL OPTIMIZER", centeredTitleStyle); GUILayout.Label($"v{version}", centeredVersionStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("KaleidoVR (Credits)", GUILayout.Width(160), GUILayout.Height(22)))
                KaleidoVRCreditsWindow.Open();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static string logoTrimPath;
        private static Rect logoTrimUv = new Rect(0f, 0f, 1f, 1f);

        // The logo file carries transparent space around the artwork. Drawing the whole
        // image would show that as empty padding above the header, so only the part that
        // actually has pixels is drawn.
        private static Rect LogoTexCoords(Texture2D logo)
        {
            string path = AssetDatabase.GetAssetPath(logo);
            if (path == logoTrimPath) return logoTrimUv;

            logoTrimPath = path;
            logoTrimUv = new Rect(0f, 0f, 1f, 1f);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return logoTrimUv;

            Texture2D probe = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (probe.LoadImage(File.ReadAllBytes(path), false))
                {
                    Color32[] pixels = probe.GetPixels32();
                    int width = probe.width;
                    int height = probe.height;
                    int minX = width, maxX = -1, minY = height, maxY = -1;
                    for (int y = 0; y < height; y++)
                    {
                        int row = y * width;
                        for (int x = 0; x < width; x++)
                        {
                            if (pixels[row + x].a <= 8) continue;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                    if (maxX >= minX && maxY >= minY)
                    {
                        logoTrimUv = new Rect(
                            minX / (float)width,
                            minY / (float)height,
                            (maxX - minX + 1) / (float)width,
                            (maxY - minY + 1) / (float)height);
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }

            return logoTrimUv;
        }

        public static void DrawSplashLogo(Texture2D logo, float maxWidth, float maxHeight)
        {
            if (logo == null) return;
            DrawTrimmedLogo(logo, maxWidth, maxHeight);
        }

        private static void DrawTrimmedLogo(Texture2D logo, float maxWidth, float maxHeight)
        {
            Rect uv = LogoTexCoords(logo);
            float sourceWidth = Mathf.Max(1f, logo.width * uv.width);
            float sourceHeight = Mathf.Max(1f, logo.height * uv.height);

            float scale = Mathf.Min(maxWidth / sourceWidth, maxHeight / sourceHeight);
            float drawWidth = Mathf.Round(sourceWidth * scale);
            float drawHeight = Mathf.Round(sourceHeight * scale);

            Rect logoRect = GUILayoutUtility.GetRect(drawWidth, drawHeight, GUILayout.Width(drawWidth), GUILayout.Height(drawHeight));
            GUI.DrawTextureWithTexCoords(logoRect, logo, uv);
        }

        public static void DrawTabs(KaleidoVRCOptimizer window)
        {
            DrawWorkspaceBar(window);
            DrawTabRow(window, new[] { "Setup", "Profiles", "Rank", "Textures" }, 0);
            DrawTabRow(window, new[] { "Meshes", "On Upload", "Scene", "Special" }, 4);
        }

        private static void DrawWorkspaceBar(KaleidoVRCOptimizer window)
        {
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("Confirm, Fix, and Apply write into this project and stay on the assets. Scan only reports.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            bool pcOn = !window.IsQuestWorkspace;
            if (GUILayout.Toggle(pcOn, "PC Workspace", EditorStyles.miniButton, GUILayout.Height(28), GUILayout.ExpandWidth(true)) && !pcOn)
                window.workspace = 0;
            if (GUILayout.Toggle(!pcOn, "Quest / Android Workspace", EditorStyles.miniButton, GUILayout.Height(28), GUILayout.ExpandWidth(true)) && pcOn)
                window.workspace = 1;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTabRow(KaleidoVRCOptimizer window, string[] names, int offset)
        {
            if (names == null || names.Length == 0) return;
            Rect row = EditorGUILayout.GetControlRect(false, 24);
            float spacing = 2f;
            float width = (row.width - spacing * (names.Length - 1)) / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                Rect cell = new Rect(row.x + i * (width + spacing), row.y, width, row.height);
                int tab = offset + i;
                if (GUI.Toggle(cell, window.tab == tab, names[i], EditorStyles.miniButton))
                    window.tab = tab;
            }
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
                case 5: DrawAvatarTab(window); break;
                case 6: DrawSceneTab(window); break;
                default: DrawSpecialTab(window); break;
            }
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        private static void DrawSetupTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("VRChat Avatar Model", EditorStyles.boldLabel);
            DrawWhy("Start here. Drop one VRChat avatar. Everything this window lists or changes comes from that model only.");
            window.KeepSingleAvatarTarget();
            DrawAvatarTarget(window);

            window.RefreshInventoryIfNeeded();
            DrawModelContents(window);

            GUILayout.Space(8);
            GUILayout.Label("Ignore List", EditorStyles.boldLabel);
            DrawWhy("Anything here is left untouched, including its dependent textures and meshes.");
            DrawObjectList(window.ignoreList, "Drag & Drop Assets To Leave Untouched");

            GUILayout.Space(8);
            GUILayout.Label("Run Safety", EditorStyles.boldLabel);
            DrawWhy("Each workspace has its own Scan / Dry Run / Apply. A run in one workspace does not write the other. Apply, Confirm, and Fix write into Unity and stay on the assets. Scan only reports.");
            EditorGUILayout.BeginHorizontal();
            window.writeLog = EditorGUILayout.ToggleLeft("Write Log File", window.writeLog);
            if (GUILayout.Button("Clear Log Cache", GUILayout.Width(110), GUILayout.Height(18)))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear Log Cache",
                    "Delete the log files under Logs/KaleidoVR/Optimizer?\n\nThe folder stays.",
                    "Clear",
                    "Cancel"))
                {
                    int removed = KaleidoVRCOptimizerHelpers.ClearOptimizerLogFiles();
                    EditorUtility.DisplayDialog(
                        "Clear Log Cache",
                        removed == 0
                            ? "There were no log files to clear."
                            : "Removed " + removed + " log file(s). The folder is still there.",
                        "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            DrawWhy("Saves a timestamped report under Logs/KaleidoVR/Optimizer. Clear Log Cache deletes those files and leaves the folder.");
            GUILayout.Space(3);
            window.applyToPrefabAssets = DrawToggle(window.applyToPrefabAssets, "Apply renderer changes to prefab assets", "Writes Scene-tab renderer edits onto the .prefab, not only the scene instance. Turn off to test on the instance first.");
        }

        private static void DrawProfilesTab(KaleidoVRCOptimizer window)
        {
            GUILayout.Label("Profiles", EditorStyles.boldLabel);
            DrawWhy("A profile is a saved set of tab settings. Pick a built-in to start, tick which tabs it should change, then save your own if you want to reuse it.");

            EditorGUILayout.HelpBox(DescribeActiveProfile(window), MessageType.Info);

            GUILayout.Space(8);
            GUILayout.Label("1. Start from a built-in", EditorStyles.boldLabel);
            DrawWhy("Click one to fill every tab with that recipe. Standard leaves sizes and formats as-is and keeps Special off. Everything is the full pack. You can still uncheck options afterward.");
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

            window.includeTextures = EditorGUILayout.ToggleLeft("Textures  —  max sizes and importer flags for the active workspace", window.includeTextures);
            window.includeMeshes = EditorGUILayout.ToggleLeft("Meshes tab  —  read/write, weld, blend shapes, skin weights", window.includeMeshes);
            window.includeRenderers = EditorGUILayout.ToggleLeft("Scene  —  offscreen, probes, particles, shadows, 4-bone quality", window.includeRenderers);
            window.includeAudio = EditorGUILayout.ToggleLeft("Scene tab audio  —  load in background, Vorbis", window.includeAudio);
            window.includeAnimators = EditorGUILayout.ToggleLeft("Scene tab animators  —  cull when offscreen", window.includeAnimators);
            window.includeAvatar = EditorGUILayout.ToggleLeft("On Upload  —  merge meshes, unused cleanup, blend shapes, PhysBones, FX", window.includeAvatar);
            window.includeSpecial = EditorGUILayout.ToggleLeft("Special tab  —  texture compression, mesh compression, Humanoid, strip shapes, GPU instancing, lights, cameras, mono", window.includeSpecial);
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
            DrawWhy("VRChat rank plus VRAM, GrabPass, animator cost, and texture flags. Scan or Dry Run fills this tab. Apply still waits for Dry Run. On Upload numbers come from a hidden copy and do not write the scene.");
            EditorGUILayout.HelpBox("Fix and Confirm on this tab write into Unity right away. You do not need Apply after those. Scan again later only if you add assets or change the avatar.", MessageType.Info);
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
                DrawWhy("Caps each type at this size. Textures already smaller stay as they are. Unticked types keep their current size unless you change that row's selector. Ignore on a row skips this type cap; Set still writes that texture.");
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeAlbedo, ref window.applyAlbedoSize, "Albedo / Diffuse / Main", "Suggested 512–1024.", ref window.albedoQuest);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeNormal, ref window.applyNormalSize, "Normal", "Suggested 512–1024.", ref window.normalQuest);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeMask, ref window.applyMaskSize, "Mask / Metallic / Rough / AO / ORM", "Suggested 256–512.", ref window.maskQuest);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeEmission, ref window.applyEmissionSize, "Emission", "Suggested 256–512.", ref window.emissionQuest);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeMatcap, ref window.applyMatcapSize, "Matcap / Ramp / Toon", "Suggested 256–512.", ref window.matcapQuest);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeOther, ref window.applyOtherSize, "Other / Unclassified", "Suggested 512.", ref window.otherQuest);
            }
            else
            {
                GUILayout.Label("Max Size By Type", EditorStyles.boldLabel);
                DrawWhy("Caps each type at this size. Textures already smaller stay as they are. Unticked types keep their current size unless you change that row's selector. Ignore on a row skips this type cap; Set still writes that texture.");
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeAlbedo, ref window.applyAlbedoSize, "Albedo / Diffuse / Main", "Body color maps. Suggested 1024–2048.", ref window.albedoPc);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeNormal, ref window.applyNormalSize, "Normal", "Bump maps. Match albedo, or one step below if memory is tight.", ref window.normalPc);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeMask, ref window.applyMaskSize, "Mask / Metallic / Rough / AO / ORM", "Packed masks are blur-tolerant. Suggested 512–1024.", ref window.maskPc);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeEmission, ref window.applyEmissionSize, "Emission", "Glow maps. Suggested 512–1024.", ref window.emissionPc);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeMatcap, ref window.applyMatcapSize, "Matcap / Ramp / Toon", "Tiny lookup textures. Suggested 256–512.", ref window.matcapPc);
                DrawTypeSizeRow(window, KaleidoOptionUndo.SizeOther, ref window.applyOtherSize, "Other / Unclassified", "Anything that did not match a suffix. Suggested 512–1024.", ref window.otherPc);
            }
            if (EditorGUI.EndChangeCheck()) window.ReadyToApplyMaxSizesOnly = false;

            KaleidoVRCOptimizerLogic.SyncTextureRowDefaults(window);

            GUILayout.Space(8);
            DrawMaxSizesOnlyActions(window, quest);

            GUILayout.Space(8);
            DrawTextureUsageList(window, quest);
            DrawTexturePreview(window);

            GUILayout.Space(8);
            GUILayout.Label("Importer Settings", EditorStyles.boldLabel);
            DrawWhy("Enable writes that flag on. Disable writes it off. Neither leaves each listed texture as-is. Current is what those textures have now. New is what Apply will write.");
            ImporterFlagSnapshot flags = CollectImporterFlagSnapshot(window);
            DrawImporterFlagHeaders();
            if (quest)
            {
                DrawEnableDisableRow(window, "Streaming mip maps", "VRChat expects this on when mip maps are on. Disable only if you must turn streaming off.",
                    KaleidoOptionUndo.StreamingMipmaps, KaleidoOptionUndo.StreamingMipmapsOff,
                    ref window.textureEnableStreamingMipmaps, ref window.textureDisableStreamingMipmaps,
                    flags.streaming, OnOffNew(window.textureEnableStreamingMipmaps, window.textureDisableStreamingMipmaps));
                window.higherQualityNormalMaps = DrawToggle(window, KaleidoOptionUndo.HigherQualityNormals, window.higherQualityNormalMaps, "Higher quality normals (ASTC)", "Uses a sharper ASTC block for detected normals when Set compression format is on in Special. Does not change the other workspace's format.");
            }
            else
            {
                DrawEnableDisableRow(window, "Crunch compression", "Crunch does not lower VRChat texture memory. Enable only shrinks download size.",
                    KaleidoOptionUndo.TextureCrunchOn, KaleidoOptionUndo.TextureCrunchOff,
                    ref window.textureEnableCrunch, ref window.textureDisableCrunch,
                    flags.crunch, OnOffNew(window.textureEnableCrunch, window.textureDisableCrunch));
                if (window.textureEnableCrunch)
                {
                    EditorGUI.indentLevel++;
                    window.textureCrunchQuality = EditorGUILayout.IntSlider("Crunch Compression %", KaleidoVRCOptimizer.ClampCrunchQuality(window.textureCrunchQuality), 1, 100);
                    EditorGUI.indentLevel--;
                }

                DrawEnableDisableRow(window, "Read / Write", "Disable saves RAM. Enable only if a script or editor tool reads pixels from the texture.",
                    KaleidoOptionUndo.TextureReadWriteOn, KaleidoOptionUndo.TextureReadWrite,
                    ref window.textureEnableReadWrite, ref window.textureDisableReadWrite,
                    flags.readable, OnOffNew(window.textureEnableReadWrite, window.textureDisableReadWrite));

                bool mipEnable = window.textureApplyMipmaps && window.textureEnableMipmaps;
                bool mipDisable = window.textureApplyMipmaps && !window.textureEnableMipmaps;
                DrawEnableDisableRow(window, "Mip maps", "Avatars in 3D should generate mip maps. Disable only if a 2D lookup must stay unfiltered.",
                    KaleidoOptionUndo.TextureMipmaps, KaleidoOptionUndo.TextureMipmaps,
                    ref mipEnable, ref mipDisable,
                    flags.mipmaps, OnOffNew(mipEnable, mipDisable));
                window.textureApplyMipmaps = mipEnable || mipDisable;
                if (mipEnable) window.textureEnableMipmaps = true;
                else if (mipDisable) window.textureEnableMipmaps = false;

                DrawEnableDisableRow(window, "Streaming mip maps", "VRChat expects this on when mip maps are on. Disable only if you must turn streaming off.",
                    KaleidoOptionUndo.StreamingMipmaps, KaleidoOptionUndo.StreamingMipmapsOff,
                    ref window.textureEnableStreamingMipmaps, ref window.textureDisableStreamingMipmaps,
                    flags.streaming, OnOffNew(window.textureEnableStreamingMipmaps, window.textureDisableStreamingMipmaps));

                window.textureApplyAniso = DrawToggle(window, KaleidoOptionUndo.TextureAniso, window.textureApplyAniso, "Set anisotropic filtering", "1 is enough for avatars. Higher values cost GPU for little gain up close.");
                if (window.textureApplyAniso) window.textureAniso = EditorGUILayout.IntSlider("Aniso Level", window.textureAniso, 0, 16);
                window.autoDetectNormalMaps = DrawToggle(window, KaleidoOptionUndo.DetectNormals, window.autoDetectNormalMaps, "Detect normal maps by name", "Sets Texture Type to Normal Map when the file looks like _n / _norm / _normal. Prevents sRGB lighting errors.");
                window.higherQualityNormalMaps = DrawToggle(window, KaleidoOptionUndo.HigherQualityNormals, window.higherQualityNormalMaps, "Higher quality normals (BC5)", "Uses BC5 for detected normals when Set compression is on in Special. ASTC sharpness is set in the other workspace.");

                DrawEnableDisableRow(window, "Linear color for mask maps", "Enable turns sRGB off on metallic/rough/AO/ORM. Disable writes sRGB back on.",
                    KaleidoOptionUndo.LinearMasks, KaleidoOptionUndo.LinearMasksOff,
                    ref window.autoLinearMaskMaps, ref window.autoSrgbMaskMaps,
                    flags.maskColor, LinearNew(window.autoLinearMaskMaps, window.autoSrgbMaskMaps), "Linear", "sRGB");

                DrawEnableDisableRow(window, "Alpha Is Transparency", "Only for albedo with an alpha channel (cutout/transparent clothing).",
                    KaleidoOptionUndo.AlphaIsTransparency, KaleidoOptionUndo.AlphaIsTransparencyOff,
                    ref window.alphaIsTransparencyOnAlbedo, ref window.alphaIsTransparencyOffAlbedo,
                    flags.alpha, OnOffNew(window.alphaIsTransparencyOnAlbedo, window.alphaIsTransparencyOffAlbedo));
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawMaxSizesOnlyActions(KaleidoVRCOptimizer window, bool quest)
        {
            GUILayout.Label("Max Sizes Only", EditorStyles.boldLabel);
            DrawWhy("Runs only the max-size-by-type settings above (and any per-texture selector you already changed). Ignore skips the type cap but still writes a row you set by hand. Type caps never raise a texture. Compression, mip maps, meshes, scene, and Special are not touched.");

            EditorGUILayout.BeginHorizontal();
            if (DrawTintedButton("Dry Run Max Sizes Only", ActionDryRunTint(), GUILayout.Height(26)))
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
            if (DrawTintedButton("Apply Max Sizes Only", ActionApplyTint(), GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog(
                    "Apply max sizes only?",
                    "This writes max texture size for ticked types and any row you set by hand. Ignored rows skip the type cap unless you changed Set. It will not change compression, mip maps, Read/Write, meshes, or scene settings.",
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
                EditorGUILayout.HelpBox("Orange Dry Run first. Green Apply stays off until that dry run finds at least one size write.", MessageType.None);
        }

        private static GUIStyle selectedTextureRowStyle;

        private static GUIStyle SelectedTextureRowStyle()
        {
            if (selectedTextureRowStyle == null)
            {
                selectedTextureRowStyle = new GUIStyle(GUI.skin.box);
                selectedTextureRowStyle.normal.background = EditorStyles.helpBox.normal.background;
            }
            return selectedTextureRowStyle;
        }

        private static GUIStyle TextureNameClipStyle()
        {
            if (textureNameClipStyle == null)
            {
                textureNameClipStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    clipping = TextClipping.Clip,
                    alignment = TextAnchor.MiddleLeft
                };
            }
            return textureNameClipStyle;
        }

        private static GUIStyle TextureMetaClipStyle()
        {
            if (textureMetaClipStyle == null)
            {
                textureMetaClipStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    clipping = TextClipping.Clip,
                    alignment = TextAnchor.MiddleLeft
                };
            }
            return textureMetaClipStyle;
        }

        private static void DrawTextureUsageList(KaleidoVRCOptimizer window, bool questPlatform)
        {
            GUILayout.Label("Textures On This Model", EditorStyles.boldLabel);
            BeginCenteredSection(window, TextureSectionMaxWidth);
            DrawWhy("Max size for this workspace. Current is what Unity has now. New is what the selector will write. Ignore skips the type cap so you can set that texture by hand. Changing Set still writes it. Ignore and Set stay for later runs. With Crunch Enable on, a Crunch % slider under Current / New remembers a moved value the same way.");

            if (window.textureUsages == null || window.textureUsages.Count == 0)
            {
                EditorGUILayout.HelpBox("Drop an avatar on Setup. Its textures will list here.", MessageType.None);
                EndCenteredSection();
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
            BeginOutlinedPanel(0f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(TextureListInnerPad);
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            window.textureUsageScroll = EditorGUILayout.BeginScrollView(
                window.textureUsageScroll,
                false,
                false,
                GUILayout.Height(TextureListHeight),
                GUILayout.ExpandWidth(true));
            DrawTextureStatusColumnHeaders();

            for (int i = 0; i < rows.Count; i++)
            {
                KaleidoTextureUsage usage = rows[i];
                if (usage == null) continue;

                bool selected = !string.IsNullOrEmpty(window.previewTexturePath)
                    && string.Equals(window.previewTexturePath, usage.path, StringComparison.OrdinalIgnoreCase);

                EditorGUILayout.BeginVertical(selected ? SelectedTextureRowStyle() : GUI.skin.box);
                EditorGUILayout.BeginHorizontal();

                Rect thumb = GUILayoutUtility.GetRect(TextureThumb, TextureThumb, GUILayout.Width(TextureThumb), GUILayout.Height(TextureThumb));
                if (usage.texture != null)
                {
                    if (GUI.Button(thumb, DisplayTexture(usage)))
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

                EditorGUILayout.BeginVertical(GUILayout.Width(TextureNameCol), GUILayout.MaxWidth(TextureNameCol), GUILayout.ExpandWidth(false));
                string textureName = usage.texture != null ? usage.texture.name : Path.GetFileName(usage.path);
                GUILayout.Label(new GUIContent(textureName, textureName), TextureNameClipStyle(), GUILayout.Width(TextureNameCol));
                GUILayout.Label(KindLabel(usage.kind), TextureMetaClipStyle(), GUILayout.Width(TextureNameCol));
                if (usage.vramBytes > 0 || !string.IsNullOrEmpty(usage.formatLabel))
                {
                    string vram = usage.vramBytes > 0 ? KaleidoVRCOptimizerHelpers.FormatBytes(usage.vramBytes) : "";
                    string fmt = string.IsNullOrEmpty(usage.formatLabel) ? "" : usage.formatLabel;
                    string extra = (fmt + "  " + vram).Trim();
                    if (usage.fromAnimationSwap) extra += "  swap";
                    if (usage.crunched) extra += "  crunch";
                    if (usage.missingStreamingMipmaps) extra += "  no stream";
                    GUILayout.Label(new GUIContent(extra, extra), TextureMetaClipStyle(), GUILayout.Width(TextureNameCol));
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Set", GUILayout.Width(28));
                HandleTextureSizePopup(window, usage, questPlatform);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                DrawTextureSizeStatus(window, usage, questPlatform);
                GUILayout.Space(TextureListAfterIgnore);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.Space(TextureListInnerPad);
            EditorGUILayout.EndHorizontal();
            EndOutlinedPanel();
            EndCenteredSection();
        }

        private static void DrawTextureStatusColumnHeaders()
        {
            GUIStyle headerPad = new GUIStyle
            {
                margin = GUI.skin.box.margin,
                padding = GUI.skin.box.padding
            };
            GUIStyle headerCenter = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            EditorGUILayout.BeginHorizontal(headerPad);
            GUILayout.Space(TextureThumb);
            EditorGUILayout.BeginVertical(GUILayout.Width(TextureNameCol), GUILayout.MaxWidth(TextureNameCol), GUILayout.ExpandWidth(false));
            GUILayout.Label("Texture", EditorStyles.miniBoldLabel, GUILayout.Width(TextureNameCol));
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginHorizontal(GUILayout.Width(TextureStatusWidth), GUILayout.MaxWidth(TextureStatusWidth), GUILayout.ExpandWidth(false));
            GUILayout.Label("Current", headerCenter, GUILayout.Width(TextureSizeCol), GUILayout.Height(16));
            GUILayout.Space(TextureStatusGap);
            GUILayout.Label("", headerCenter, GUILayout.Width(TextureMidCol), GUILayout.Height(16));
            GUILayout.Space(TextureStatusGap);
            GUILayout.Label("New", headerCenter, GUILayout.Width(TextureSizeCol), GUILayout.Height(16));
            GUILayout.Space(TextureStatusGap);
            GUILayout.Label("Ignore", headerCenter, GUILayout.Width(TextureIgnoreCol), GUILayout.Height(16));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(TextureListAfterIgnore);
            EditorGUILayout.EndHorizontal();
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
            bool ignored = questPlatform ? usage.ignoreQuest : usage.ignorePc;
            bool custom = questPlatform ? usage.usedCustomQuest : usage.usedCustomPc;
            int planned = KaleidoVRCOptimizerLogic.GetPlannedRowSize(window, usage, questPlatform);
            bool willWrite = custom || (!ignored && typeApply);
            int revert = questPlatform ? usage.questRevertSize : usage.pcRevertSize;
            bool increasing = revert > 0 && planned > revert;
            bool changing = (willWrite && planned != current) || increasing;
            GUIStyle changeStyle = increasing ? sizeUpStyle : sizeNewStyle;

            GUIStyle currentCaption = new GUIStyle(sizeCaptionStyle) { alignment = TextAnchor.MiddleCenter };
            GUIStyle currentValue = new GUIStyle(sizeValueStyle) { alignment = TextAnchor.MiddleCenter };
            GUIStyle midCaption = new GUIStyle(sizeCaptionStyle) { alignment = TextAnchor.MiddleCenter };
            GUIStyle midArrow = new GUIStyle(sizeValueStyle) { alignment = TextAnchor.MiddleCenter };
            if (changing)
            {
                midCaption.normal.textColor = changeStyle.normal.textColor;
                midArrow.normal.textColor = changeStyle.normal.textColor;
            }

            currentCaption.margin = new RectOffset(0, 0, 0, 0);
            currentCaption.padding = new RectOffset(0, 0, 0, 0);
            currentValue.margin = new RectOffset(0, 0, 0, 0);
            currentValue.padding = new RectOffset(0, 0, 0, 0);
            midCaption.margin = new RectOffset(0, 0, 0, 0);
            midCaption.padding = new RectOffset(0, 0, 0, 0);
            midArrow.margin = new RectOffset(0, 0, 0, 0);
            midArrow.padding = new RectOffset(0, 0, 0, 0);
            GUIStyle newCaption = new GUIStyle(sizeCaptionStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            GUIStyle newValue = new GUIStyle(changing ? changeStyle : sizeValueStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            const float LineH = 16f;
            EditorGUILayout.BeginVertical(GUILayout.Width(TextureStatusWidth), GUILayout.MaxWidth(TextureStatusWidth), GUILayout.ExpandWidth(false));
            EditorGUILayout.BeginHorizontal(GUILayout.Width(TextureStatusWidth), GUILayout.MaxWidth(TextureStatusWidth), GUILayout.ExpandWidth(false));
            EditorGUILayout.BeginVertical(GUILayout.Width(TextureSizeCol), GUILayout.MaxWidth(TextureSizeCol), GUILayout.ExpandWidth(false));
            GUILayout.Label("Current", currentCaption, GUILayout.Width(TextureSizeCol), GUILayout.Height(LineH));
            GUILayout.Label(current > 0 ? current + " px" : "—", currentValue, GUILayout.Width(TextureSizeCol), GUILayout.Height(LineH));
            EditorGUILayout.EndVertical();

            GUILayout.Space(TextureStatusGap);
            EditorGUILayout.BeginVertical(GUILayout.Width(TextureMidCol), GUILayout.MaxWidth(TextureMidCol), GUILayout.ExpandWidth(false));
            GUILayout.Label(changing ? "Will apply" : " ", midCaption, GUILayout.Width(TextureMidCol), GUILayout.Height(LineH));
            if (increasing)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(1f, 0.72f, 0.28f, 1f)
                    : new Color(1f, 0.78f, 0.40f, 1f);
                if (GUILayout.Button("Confirm", GUILayout.Width(TextureMidCol), GUILayout.Height(18)))
                {
                    if (questPlatform) usage.questRevertSize = 0;
                    else usage.pcRevertSize = 0;
                    KaleidoVRCOptimizerLogic.QueueImmediateTextureSize(window, usage.path, planned, questPlatform, current);
                }
                GUI.backgroundColor = prev;
                if (GUILayout.Button("Cancel", GUILayout.Width(TextureMidCol), GUILayout.Height(18)))
                    CancelPendingTextureIncrease(usage, questPlatform);
            }
            else
            {
                GUILayout.Label("→", midArrow, GUILayout.Width(TextureMidCol), GUILayout.Height(LineH));
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(TextureStatusGap);
            EditorGUILayout.BeginVertical(GUILayout.Width(TextureSizeCol), GUILayout.MaxWidth(TextureSizeCol), GUILayout.ExpandWidth(false));
            GUILayout.Label("New", newCaption, GUILayout.Width(TextureSizeCol), GUILayout.Height(LineH));
            GUILayout.Label(((willWrite || increasing) ? planned : current) + " px", newValue, GUILayout.Width(TextureSizeCol), GUILayout.Height(LineH));
            EditorGUILayout.EndVertical();

            GUILayout.Space(TextureStatusGap);
            EditorGUILayout.BeginVertical(GUILayout.Width(TextureIgnoreCol), GUILayout.MaxWidth(TextureIgnoreCol), GUILayout.ExpandWidth(false));
            GUILayout.Label(" ", newCaption, GUILayout.Width(TextureIgnoreCol), GUILayout.Height(LineH));
            EditorGUILayout.BeginHorizontal(GUILayout.Width(TextureIgnoreCol), GUILayout.Height(LineH));
            GUILayout.FlexibleSpace();
            bool nextIgnore = GUILayout.Toggle(ignored, GUIContent.none, GUILayout.Width(16), GUILayout.Height(16));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (nextIgnore != ignored)
            {
                if (questPlatform) usage.ignoreQuest = nextIgnore;
                else usage.ignorePc = nextIgnore;
                window.ReadyToApplyMaxSizesOnly = false;
                KaleidoVRCOptimizerLogic.RememberTextureRow(usage);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (!questPlatform && window.textureEnableCrunch)
            {
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal(GUILayout.Width(TextureStatusWidth), GUILayout.MaxWidth(TextureStatusWidth));
                GUILayout.Label("Crunch %", EditorStyles.miniLabel, GUILayout.Width(56));
                int shown = KaleidoVRCOptimizerLogic.GetPlannedCrunchQuality(window, usage);
                int next = EditorGUILayout.IntSlider(shown, 1, 100);
                if (next != shown)
                {
                    int globalQ = KaleidoVRCOptimizer.ClampCrunchQuality(window.textureCrunchQuality);
                    if (next == globalQ)
                    {
                        usage.usedCustomCrunch = false;
                        usage.crunchQuality = next;
                    }
                    else
                    {
                        usage.usedCustomCrunch = true;
                        usage.crunchQuality = KaleidoVRCOptimizer.ClampCrunchQuality(next);
                    }
                    KaleidoVRCOptimizerLogic.RememberTextureRow(usage);
                }
                EditorGUILayout.EndHorizontal();
            }
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
                KaleidoVRCOptimizerLogic.RememberTextureRow(usage);
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
            KaleidoVRCOptimizerLogic.RememberTextureRow(usage);
            if (picked == current) return;
            KaleidoVRCOptimizerLogic.QueueImmediateTextureSize(window, usage.path, picked, questPlatform, current);
        }

        private static Material normalPreviewMaterial;
        private static readonly Dictionary<string, string> normalPreviewKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Texture2D> normalPreviews = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        // BC5 / DXT5nm drop the blue channel, so normals preview green until Z is rebuilt.
        private static Material NormalPreviewMaterial()
        {
            if (normalPreviewMaterial != null) return normalPreviewMaterial;
            Shader shader = Shader.Find("Hidden/KaleidoVR/NormalMapPreview");
            if (shader == null) return null;
            normalPreviewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return normalPreviewMaterial;
        }

        // Baked once into a plain texture. Drawing the material straight to the GUI would
        // bypass the scroll view's clip rect and paint rows over the rest of the window.
        private static Texture DisplayTexture(KaleidoTextureUsage usage)
        {
            if (usage == null || usage.texture == null) return null;
            if (usage.kind != KaleidoTextureKind.Normal) return usage.texture;

            string key = usage.texture.width + "x" + usage.texture.height + "|" + usage.formatLabel;
            string cachedKey;
            Texture2D cached;
            if (normalPreviewKeys.TryGetValue(usage.path, out cachedKey)
                && cachedKey == key
                && normalPreviews.TryGetValue(usage.path, out cached)
                && cached != null)
            {
                return cached;
            }

            Material material = NormalPreviewMaterial();
            if (material == null) return usage.texture;

            const int MaxSide = 256;
            float scale = Mathf.Min(1f, (float)MaxSide / Mathf.Max(usage.texture.width, usage.texture.height, 1));
            int width = Mathf.Max(1, Mathf.RoundToInt(usage.texture.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(usage.texture.height * scale));

            RenderTexture temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture active = RenderTexture.active;
            Texture2D baked = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Graphics.Blit(usage.texture, temp, material);
                RenderTexture.active = temp;
                baked.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                baked.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(temp);
            }

            Texture2D previous;
            if (normalPreviews.TryGetValue(usage.path, out previous) && previous != null)
                UnityEngine.Object.DestroyImmediate(previous);

            normalPreviews[usage.path] = baked;
            normalPreviewKeys[usage.path] = key;
            return baked;
        }

        private static void DrawTextureImage(Rect rect, KaleidoTextureUsage usage)
        {
            if (Event.current.type != EventType.Repaint) return;
            Texture display = DisplayTexture(usage);
            if (display == null) return;

            EditorGUI.DrawPreviewTexture(rect, display, null, ScaleMode.ScaleToFit);
        }

        public static void ClearNormalPreviews()
        {
            foreach (KeyValuePair<string, Texture2D> entry in normalPreviews)
            {
                if (entry.Value != null) UnityEngine.Object.DestroyImmediate(entry.Value);
            }
            normalPreviews.Clear();
            normalPreviewKeys.Clear();
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
            DrawTextureImage(preview, usage);
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

        private static void DrawTypeSizeRow(KaleidoVRCOptimizer window, string optionId, ref bool apply, string title, string why, ref int size)
        {
            apply = DrawToggle(window, optionId, apply, title, why);
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

        private static void DrawAvatarTab(KaleidoVRCOptimizer window)
        {
            EditorGUILayout.HelpBox("Off until you tick Apply on upload. Scan, Dry Run, and Apply do not run these options. They run on the assembled upload copy and do not write the scene or source assets. Meshes and components another upload pass already replaced are left alone.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            window.avatarApplyOnUpload = EditorGUILayout.ToggleLeft("Apply on upload", window.avatarApplyOnUpload, GUILayout.ExpandWidth(false));
            GUILayout.Space(8);
            GUILayout.Label("Follows this Unity editor, not the project.", OnUploadPrefNoteStyle());
            EditorGUILayout.EndHorizontal();
            DrawWhy("Off by default. Runs the options below on the assembled upload copy. Does not write the scene. Skipped in Play Mode. Leaves runtime meshes Kaleido did not create.");
            GUILayout.Space(3);

            EditorGUI.BeginDisabledGroup(!window.avatarApplyOnUpload);
            GUILayout.Space(8);
            GUILayout.Label("Meshes", EditorStyles.boldLabel);
            window.avatarMergeSkinnedMeshes = DrawToggle(window.avatarMergeSkinnedMeshes, "Merge skinned meshes that animate together", "Combines always-visible meshes on the same layer. The extra renderer components are removed; the objects stay, so PhysBones and contacts keep working. Toggles, material-swap animations, blend shapes, contact-system meshes, extras that still use their own armature, and any mesh another component still points at stay separate.");
            window.avatarMergeIdenticalSlots = DrawToggle(window.avatarMergeIdenticalSlots, "Merge identical material slots", "Joins submeshes that use the same material. Slots driven by material-swap animations are left alone, and so is a mesh another component still uses.");
            window.avatarShuffleSlots = DrawToggle(window.avatarShuffleSlots, "Allow shuffling material slots", "Reorders slots so identical materials sit together and can merge. Slot order is not used by typical avatar shaders. Meshes other components still use are left as-is.");

            GUILayout.Space(8);
            GUILayout.Label("Blend Shapes", EditorStyles.boldLabel);
            window.avatarOptimizeBlendShapes = DrawToggle(window.avatarOptimizeBlendShapes, "Remove unused blend shapes", "Upload-only. Drops unused shapes that are at zero weight. Visemes, Eye Look blink / look-up / look-down, wink / blink names, shapes named by other components on this avatar, and any shape that still has weight stay. Eye Look indices are rewritten to the new mesh.");
            window.avatarMergeSameRatioShapes = DrawToggle(window.avatarMergeSameRatioShapes, "Merge same-ratio blend shapes", "Off by default. Combines shapes that every clip always drives in the same ratio. Can change expressions. Leave off unless you want that rewrite.");
            window.avatarMmdCompatibility = DrawToggle(window.avatarMmdCompatibility, "MMD world compatibility", "Keeps MMD viseme / face shapes and the first three FX layers.");

            GUILayout.Space(8);
            GUILayout.Label("Cleanup", EditorStyles.boldLabel);
            window.avatarRemoveUnusedComponents = DrawToggle(window.avatarRemoveUnusedComponents, "Remove unused components", "Deletes disabled components that no animation turns on, plus EditorOnly objects.");
            window.avatarRemoveUnusedGameObjects = DrawToggle(window.avatarRemoveUnusedGameObjects, "Remove unused GameObjects", "Deletes inactive objects that never turn on. Off by default. Humanoid bones stay, and so do objects other components still point at.");
            window.avatarStripUnusedBones = DrawToggle(window.avatarStripUnusedBones, "Keep only weighted bones", "Drops bone references with zero weight unless an animation moves them or another component still uses them.");

            GUILayout.Space(8);
            GUILayout.Label("PhysBones", EditorStyles.boldLabel);
            window.avatarOptimizePhysBones = DrawToggle(window.avatarOptimizePhysBones, "Disable PhysBones when unused", "Removes PhysBones that no remaining mesh uses, and colliders nothing references. Ones another component still points at stay.");

            GUILayout.Space(8);
            GUILayout.Label("Animator", EditorStyles.boldLabel);
            DrawSmallRedWarning("Warning — can drop FX layers and unused curves. Can change how gestures and face play.");
            window.avatarOptimizeFxLayer = DrawToggle(window.avatarOptimizeFxLayer, "Optimize FX layer", "Off by default. On the upload copy only: drops empty layers and animation curves whose bindings are gone. Hand gesture clips stay as-is (detected by GestureLeft / GestureRight). MMD keeps layers 0–2.");
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            if (GUILayout.Button("Dry Run On Upload", GUILayout.Height(26)))
            {
                window.onUploadPreviewLines.Clear();
                GameObject previewRoot = FirstDroppedAvatarRoot(window);
                if (previewRoot == null)
                {
                    EditorUtility.DisplayDialog("No avatar", "Drop a VRChat avatar on Setup first.", "OK");
                }
                else
                {
                    if (!window.avatarApplyOnUpload)
                        window.onUploadPreviewLines.Add("Apply on upload is off. Upload will not run these options.");
                    KaleidoAvatarPass.Preview(previewRoot, KaleidoAvatarPass.FromWindow(window), window.onUploadPreviewLines, KaleidoAvatarPass.ExclusionsFrom(window, previewRoot));
                }
            }
            if (window.onUploadPreviewLines.Count > 0)
            {
                GUILayout.Space(6);
                GUILayout.Label("On Upload preview", EditorStyles.boldLabel);
                for (int i = 0; i < window.onUploadPreviewLines.Count; i++)
                    EditorGUILayout.LabelField(window.onUploadPreviewLines[i], MiniWrap());
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Create optimized copy in the scene", GUILayout.Height(26)))
            {
                GameObject source = FirstDroppedAvatarRoot(window);
                if (source == null)
                {
                    EditorUtility.DisplayDialog("No avatar", "Drop a VRChat avatar on Setup first.", "OK");
                }
                else if (EditorUtility.DisplayDialog(
                    "Create optimized copy",
                    "This makes a scene clone and runs On Upload on that clone only. Use it to check what a VRChat upload will do — pose it, look at the face, and compare rank.\n\nDo not edit the copy or upload it as your master. The original is turned off, not deleted. Turn the original back on when you are done, and delete the copy if you do not need it.",
                    "Create",
                    "Cancel"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        KaleidoAvatarPass.CreatePreviewCopy(source, KaleidoAvatarPass.FromWindow(window), KaleidoAvatarPass.ExclusionsFrom(window, source));
                    };
                }
            }
            DrawWhy("Makes a scene copy and runs this tab on that copy so you can test before upload. The original is turned off. Do not edit the copy. A successful VRChat upload removes the copy and turns the original back on.");

            GUILayout.Space(10);
            bool hasCache = KaleidoAvatarPass.HasGeneratedCleanup();
            EditorGUI.BeginDisabledGroup(!hasCache);
            if (GUILayout.Button("Clear cache", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear cache",
                    "Delete generated meshes and FX controllers in " + KaleidoAvatarPass.GeneratedFolderPath + "?\n\nOptimized Copies that use those files are removed and the original avatar is turned back on.",
                    "Delete",
                    "Cancel"))
                {
                    EditorApplication.delayCall += () => KaleidoAvatarPass.ClearGeneratedCache();
                }
            }
            EditorGUI.EndDisabledGroup();
            DrawWhy("Deletes " + KaleidoAvatarPass.GeneratedFolderPath + ". After a successful VRChat upload this happens on its own. Optimized Copies that use the cache are removed and the original is turned back on.");
        }

        private static void DrawMeshesTab(KaleidoVRCOptimizer window)
        {
            if (window.IsQuestWorkspace)
            {
                GUILayout.Label("Mesh Writes", EditorStyles.boldLabel);
                DrawWhy("Dry Run / Apply only writes these importer flags. Weld, quads, lightmap UVs, and animation compression stay in the other workspace.");
                window.optimizeMeshes = DrawToggle(window.optimizeMeshes, "Process model importers", "Master switch for the mesh writes below.");
                EditorGUI.BeginDisabledGroup(!window.optimizeMeshes);
                window.meshEnableReadWrite = DrawToggle(window, KaleidoOptionUndo.MeshReadWrite, window.meshEnableReadWrite, "Enable mesh Read / Write", "Required by VRChat on every platform. If any mesh has Read/Write off, rank is Very Poor and upload is blocked.");
                window.applySkinWeights = DrawToggle(window, KaleidoOptionUndo.SkinWeights, window.applySkinWeights, "Set skin weights to 4 bones", "Caps import weights at 4 influences. Unlimited weights are set from the other workspace.");
                if (window.applySkinWeights) window.skinWeights = KaleidoSkinWeightChoice.FourBones;
                EditorGUI.EndDisabledGroup();
                return;
            }

            window.optimizeMeshes = DrawToggle(window.optimizeMeshes, "Process model importers", "Master switch for FBX/GLB import settings. Off = skip every model.");
            EditorGUI.BeginDisabledGroup(!window.optimizeMeshes);
            window.meshEnableReadWrite = DrawToggle(window, KaleidoOptionUndo.MeshReadWrite, window.meshEnableReadWrite, "Enable mesh Read / Write", "Required by VRChat. If any mesh on the avatar has Read/Write off, the SDK ranks the avatar Very Poor and blocks upload.");
            window.meshOptimizePolygons = DrawToggle(window, KaleidoOptionUndo.OptimizePolygons, window.meshOptimizePolygons, "Optimize mesh polygons", "Reorders triangles for the GPU. Safe for avatars. Does not reduce triangle count.");
            window.meshOptimizeVertices = DrawToggle(window, KaleidoOptionUndo.OptimizeVertices, window.meshOptimizeVertices, "Optimize mesh vertices", "Reorders vertices for cache locality. Safe. Does not decimate.");
            window.meshWeldVertices = DrawToggle(window, KaleidoOptionUndo.WeldVertices, window.meshWeldVertices, "Weld vertices", "Merges duplicates on import. Usually wanted. Uncheck if a mesh relies on split verts for UV islands/sharp edges you already authored.");
            window.meshKeepBlendShapes = DrawToggle(window, KaleidoOptionUndo.KeepBlendShapes, window.meshKeepBlendShapes, "Keep blend shapes", "Forces blend shapes on. Visemes and face shapes need this. Does not delete unused shapes.");
            window.meshDisableQuads = DrawToggle(window, KaleidoOptionUndo.DisableQuads, window.meshDisableQuads, "Keep quads off", "Avatars should triangulate. Leave off if a tool needs quads.");
            window.meshDisableLightmapUVs = DrawToggle(window, KaleidoOptionUndo.DisableLightmapUvs, window.meshDisableLightmapUVs, "Disable lightmap UVs", "Avatars are not lightmapped. Turns off extra UV generation and import time.");
            window.meshDisableImportLightsCameras = DrawToggle(window, KaleidoOptionUndo.SkipLightsCameras, window.meshDisableImportLightsCameras, "Skip embedded lights / cameras", "FBX extras become extra components. Avatars should not import them.");
            window.meshOptimizeAnimation = DrawToggle(window, KaleidoOptionUndo.OptimizeAnimation, window.meshOptimizeAnimation, "Optimal animation compression", "Compresses clips on the model importer. Use for character FBX animations.");
            window.applySkinWeights = DrawToggle(window, KaleidoOptionUndo.SkinWeights, window.applySkinWeights, "Set skin weights", "Unlimited is the usual desktop choice. 4 bones is set from the other workspace.");
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
                window.rendererDisableShadows = DrawToggle(window, KaleidoOptionUndo.DisableShadows, window.rendererDisableShadows, "Disable shadow casting", "Uncheck if this avatar should still cast shadows.");
                window.rendererForceBone4 = DrawToggle(window, KaleidoOptionUndo.ForceBone4, window.rendererForceBone4, "Force 4 bone quality on skinned meshes", "Caps GPU skinning at 4 influences.");
                EditorGUI.EndDisabledGroup();
                return;
            }

            GUILayout.Label("Renderers", EditorStyles.boldLabel);
            window.optimizeRenderers = DrawToggle(window.optimizeRenderers, "Process skinned / mesh renderers", "Master switch for this section.");
            EditorGUI.BeginDisabledGroup(!window.optimizeRenderers);
            window.rendererDisableUpdateWhenOffscreen = DrawToggle(window, KaleidoOptionUndo.DisableUpdateOffscreen, window.rendererDisableUpdateWhenOffscreen, "Disable Update When Offscreen", "Big CPU win. Unity keeps animating skinned meshes that are culled if this stays on. Turn off only for meshes that must stay posed while hidden.");
            window.rendererDisableReceiveShadows = DrawToggle(window, KaleidoOptionUndo.DisableReceiveShadows, window.rendererDisableReceiveShadows, "Disable receive shadows", "Avatars often skip receiving world shadows. Uncheck if you want contact shadows on the body.");
            window.rendererDisableProbes = DrawToggle(window, KaleidoOptionUndo.DisableProbes, window.rendererDisableProbes, "Disable light / reflection probes", "Stops per-renderer probe sampling. Worlds still light the avatar through VRChat's lighting; this cuts extra probe work.");
            window.rendererDisableMotionVectors = DrawToggle(window, KaleidoOptionUndo.DisableMotionVectors, window.rendererDisableMotionVectors, "Disable motion vectors", "VRChat does not use camera motion blur on avatars. Safe to force off.");
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);
            GUILayout.Label("Animators", EditorStyles.boldLabel);
            window.optimizeAnimators = DrawToggle(window.optimizeAnimators, "Process animator components", "Master switch for animator culling.");
            EditorGUI.BeginDisabledGroup(!window.optimizeAnimators);
            window.animatorCullWhenOffscreen = DrawToggle(window, KaleidoOptionUndo.AnimatorCull, window.animatorCullWhenOffscreen, "Cull update when offscreen", "Checked Apply sets Cull Update Transforms. Uncheck to leave each animator as it is.");
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);
            GUILayout.Label("Audio", EditorStyles.boldLabel);
            window.optimizeAudio = DrawToggle(window.optimizeAudio, "Process audio importers", "Master switch for clips used by the avatar.");
            EditorGUI.BeginDisabledGroup(!window.optimizeAudio);
            window.audioLoadInBackground = DrawToggle(window, KaleidoOptionUndo.AudioLoadBackground, window.audioLoadInBackground, "Load in background", "Avoids hitches when a clip first plays.");
            window.audioApplyVorbis = DrawToggle(window, KaleidoOptionUndo.AudioVorbis, window.audioApplyVorbis, "Vorbis, compressed in memory", "Standard for short avatar SFX. Do not use on huge music beds.");
            if (window.audioApplyVorbis) window.audioQuality = EditorGUILayout.Slider("Vorbis Quality", window.audioQuality, 0.01f, 1f);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(8);
            GUILayout.Label("Particles", EditorStyles.boldLabel);
            window.optimizeParticles = DrawToggle(window, KaleidoOptionUndo.Particles, window.optimizeParticles, "Strip particle shadows / motion vectors", "Particles on avatars rarely need shadows. Uncheck if a VFX specifically uses them.");
            window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.enableLightsOnAvatar || window.disableCamerasOnAvatar || window.enableCamerasOnAvatar;
        }

        private static void DrawSpecialTab(KaleidoVRCOptimizer window)
        {
            DrawSpecialUseCaseHeader();
            EditorGUILayout.HelpBox("Enable writes that flag on. Disable writes it off. Neither leaves the avatar as-is. Apply only warns when a Special write is not at its default.", MessageType.Warning);
            DrawWhy("Enable writes that flag on. Disable writes it off. Neither leaves each object as-is. Current is what the dropped avatar has now.");
            SpecialFlagSnapshot flags = CollectSpecialFlagSnapshot(window);
            DrawImporterFlagHeaders();

            if (window.IsQuestWorkspace)
            {
                bool formatOn = window.applyAndroidTexFormat;
                bool formatOff = !window.applyAndroidTexFormat;
                DrawEnableDisableRow(window, "Set compression format", "Enable rewrites listed textures to the ASTC block you pick. Disable leaves the author's format.",
                    KaleidoOptionUndo.AndroidFormat, "",
                    ref formatOn, ref formatOff,
                    flags.format, formatOn ? window.androidTexFormat.ToString() : "—");
                window.applyAndroidTexFormat = formatOn;
                if (window.applyAndroidTexFormat)
                {
                    EditorGUI.indentLevel++;
                    window.androidTexFormat = (KaleidoAndroidTexFormat)EditorGUILayout.EnumPopup("Format", window.androidTexFormat);
                    EditorGUI.indentLevel--;
                }

                bool gpuOn = window.optimizeMaterials && window.materialEnableGpuInstancing;
                bool gpuOff = window.optimizeMaterials && !window.materialEnableGpuInstancing;
                DrawEnableDisableRow(window, "GPU instancing", "Writes enableInstancing on .mat files. Recommended on mobile. Little effect on skinned meshes.",
                    KaleidoOptionUndo.GpuInstancing, KaleidoOptionUndo.GpuInstancing,
                    ref gpuOn, ref gpuOff,
                    flags.instancing, OnOffNew(gpuOn, gpuOff));
                window.optimizeMaterials = gpuOn || gpuOff;
                if (gpuOn) window.materialEnableGpuInstancing = true;
                else if (gpuOff) window.materialEnableGpuInstancing = false;

                DrawEnableDisableRow(window, "Realtime lights", "Mobile strips avatar lights. Enable turns them back on. Disable turns them off.",
                    KaleidoOptionUndo.EnableLights, KaleidoOptionUndo.DisableLights,
                    ref window.enableLightsOnAvatar, ref window.disableLightsOnAvatar,
                    flags.lights, OnOffNew(window.enableLightsOnAvatar, window.disableLightsOnAvatar));
                DrawEnableDisableRow(window, "Cameras", "Mobile disables avatar cameras. Enable turns them back on. Disable turns them off.",
                    KaleidoOptionUndo.EnableCameras, KaleidoOptionUndo.DisableCameras,
                    ref window.enableCamerasOnAvatar, ref window.disableCamerasOnAvatar,
                    flags.cameras, OnOffNew(window.enableCamerasOnAvatar, window.disableCamerasOnAvatar));
                window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.enableLightsOnAvatar || window.disableCamerasOnAvatar || window.enableCamerasOnAvatar;
                return;
            }

            bool pcFormatOn = window.applyPcTexFormat;
            bool pcFormatOff = !window.applyPcTexFormat;
            DrawEnableDisableRow(window, "Set compression", "Enable rewrites listed textures. Auto DXT1 on opaque maps dirties smooth or white fabrics. Disable leaves the author's format.",
                KaleidoOptionUndo.PcFormat, "",
                ref pcFormatOn, ref pcFormatOff,
                flags.format, pcFormatOn ? window.pcTexFormat.ToString() : "—");
            window.applyPcTexFormat = pcFormatOn;
            if (window.applyPcTexFormat)
            {
                EditorGUI.indentLevel++;
                window.pcTexFormat = (KaleidoPcTexFormat)EditorGUILayout.IntPopup(
                    "Format",
                    (int)window.pcTexFormat,
                    PcFormatLabels,
                    PcFormatValues);
                if (window.pcTexFormat == KaleidoPcTexFormat.AutoBc7Dxt1)
                    DrawWhy("Dry Run / Apply only. Opaque → DXT1. Alpha or cutout → BC7. Detected normals stay BC5 while Higher quality normals is on.");
                EditorGUI.indentLevel--;
            }

            bool meshCompOn = window.applyMeshCompression;
            bool meshCompOff = !window.applyMeshCompression;
            DrawEnableDisableRow(window, "Mesh compression", "Unity's mesh compressor distorts blend shapes. Enable writes the level below. Disable leaves each model as-is.",
                KaleidoOptionUndo.MeshCompression, "",
                ref meshCompOn, ref meshCompOff,
                flags.meshComp, meshCompOn ? window.meshCompression.ToString() : "—");
            bool meshCompWasOn = window.applyMeshCompression;
            window.applyMeshCompression = meshCompOn;
            if (window.applyMeshCompression)
            {
                if (!meshCompWasOn && window.meshCompression == KaleidoMeshCompressionChoice.Off)
                    window.meshCompression = KaleidoMeshCompressionChoice.Low;
                EditorGUI.indentLevel++;
                window.meshCompression = (KaleidoMeshCompressionChoice)EditorGUILayout.EnumPopup("Compression Level", window.meshCompression);
                EditorGUI.indentLevel--;
            }

            bool humanOn = window.meshForceHumanoid;
            bool humanOff = !window.meshForceHumanoid;
            DrawEnableDisableRow(window, "Humanoid rig", "Enable rewrites the FBX avatar to Humanoid. Disable leaves the current rig. Prefer the Rig tab in the importer.",
                KaleidoOptionUndo.ForceHumanoid, "",
                ref humanOn, ref humanOff,
                flags.humanoid, humanOn ? "Humanoid" : "—");
            window.meshForceHumanoid = humanOn;

            DrawEnableDisableRow(window, "Blend shape import", "Disable turns blend shapes off and breaks visemes. Enable turns import back on.",
                KaleidoOptionUndo.RestoreBlendShapes, KaleidoOptionUndo.StripBlendShapes,
                ref window.meshRestoreBlendShapes, ref window.meshStripBlendShapes,
                flags.blendShapes, OnOffNew(window.meshRestoreBlendShapes, window.meshStripBlendShapes));

            bool boundsOn = window.rendererRecalculateBounds;
            bool boundsOff = !window.rendererRecalculateBounds;
            DrawEnableDisableRow(window, "Recalculate skinned bounds", "Enable writes a 2 m cube (Extent 1,1,1) at mid-body. Disable leaves each box as-is. Skips DPS / TPS light meshes.",
                KaleidoOptionUndo.RecalculateBounds, "",
                ref boundsOn, ref boundsOff,
                flags.bounds, boundsOn ? "2 m cube" : "—");
            window.rendererRecalculateBounds = boundsOn;

            DrawEnableDisableRow(window, "Realtime lights", "VRChat Excellent allows 0 lights. Enable turns them on. Disable turns them off.",
                KaleidoOptionUndo.EnableLights, KaleidoOptionUndo.DisableLights,
                ref window.enableLightsOnAvatar, ref window.disableLightsOnAvatar,
                flags.lights, OnOffNew(window.enableLightsOnAvatar, window.disableLightsOnAvatar));

            DrawEnableDisableRow(window, "Force audio to mono", "Enable halves clip size but collapses stereo. Disable writes stereo back.",
                KaleidoOptionUndo.AudioMono, KaleidoOptionUndo.AudioStereo,
                ref window.audioForceToMono, ref window.audioForceToStereo,
                flags.mono, OnOffNew(window.audioForceToMono, window.audioForceToStereo));
            window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.enableLightsOnAvatar || window.disableCamerasOnAvatar || window.enableCamerasOnAvatar;
        }

        public static void DrawActions(KaleidoVRCOptimizer window)
        {
            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                window.WorkspaceReadyToApply
                    ? "Orange Dry Run finished. Review Rank, then press green Apply to write those importer and scene changes for this workspace. Apply stays on the assets."
                    : "Blue Scan only looks — it does not write. Orange Dry Run previews importer and scene writes. Green Apply writes into this Unity project and stays. Apply stays off until a Dry Run succeeds.",
                window.WorkspaceReadyToApply ? MessageType.Info : MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (!HasSelectedAvatar(window))
            {
                if (GUILayout.Button("Select a model on Setup", GUILayout.Height(32)))
                    window.tab = 0;
            }
            else if (DrawTintedButton("Scan Performance", ActionScanTint(), GUILayout.Height(32)))
            {
                window.StoreReport(KaleidoVRCOptimizerLogic.Scan(window, false));
                window.tab = 2;
            }
            if (DrawTintedButton("Dry Run", ActionDryRunTint(), GUILayout.Height(32)))
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
            if (DrawTintedButton("Apply", ActionApplyTint(), GUILayout.Height(32)))
            {
                if (!ConfirmApply(window))
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
            DrawActionColorKey();
        }

        private static Color ActionScanTint()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.38f, 0.70f, 1f, 1f)
                : new Color(0.42f, 0.68f, 1f, 1f);
        }

        private static Color ActionDryRunTint()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(1f, 0.78f, 0.28f, 1f)
                : new Color(1f, 0.82f, 0.36f, 1f);
        }

        private static Color ActionApplyTint()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.38f, 0.88f, 0.48f, 1f)
                : new Color(0.40f, 0.82f, 0.42f, 1f);
        }

        private static bool DrawTintedButton(string label, Color tint, params GUILayoutOption[] options)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = tint;
            bool clicked = GUILayout.Button(label, options);
            GUI.backgroundColor = previous;
            return clicked;
        }

        private const float ColorKeyRowHeight = 16f;

        private static void DrawActionColorKey()
        {
            EnsureSizeStyles();

            // Both styles come from miniLabel with cleared padding, so bold and regular
            // text share one baseline instead of sitting at different heights.
            GUIStyle note = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedHeight = ColorKeyRowHeight
            };
            GUIStyle swatch = new GUIStyle(note) { fontStyle = FontStyle.Bold };

            GUIStyle blue = new GUIStyle(swatch);
            blue.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.55f, 0.82f, 1f, 1f)
                : new Color(0.08f, 0.38f, 0.72f, 1f);
            GUIStyle orange = new GUIStyle(swatch);
            orange.normal.textColor = sizeUpStyle.normal.textColor;
            GUIStyle green = new GUIStyle(swatch);
            green.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.48f, 0.92f, 0.55f, 1f)
                : new Color(0.08f, 0.48f, 0.18f, 1f);

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal(GUILayout.Height(ColorKeyRowHeight));
            GUILayout.FlexibleSpace();
            DrawColorKeyPair("Blue", blue, "Scan looks only", note);
            GUILayout.Space(16);
            DrawColorKeyPair("Orange", orange, "Dry Run previews", note);
            GUILayout.Space(16);
            DrawColorKeyPair("Green", green, "Apply writes and stays", note);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawColorKeyPair(string name, GUIStyle nameStyle, string meaning, GUIStyle meaningStyle)
        {
            GUILayout.Label(name, nameStyle, GUILayout.Height(ColorKeyRowHeight), GUILayout.ExpandWidth(false));
            GUILayout.Space(4);
            GUILayout.Label(meaning, meaningStyle, GUILayout.Height(ColorKeyRowHeight), GUILayout.ExpandWidth(false));
        }

        private static GUIStyle specialUseCaseStyle;

        private static void DrawSpecialUseCaseHeader()
        {
            if (specialUseCaseStyle == null)
            {
                specialUseCaseStyle = new GUIStyle(EditorStyles.boldLabel);
                specialUseCaseStyle.fontSize = 18;
                specialUseCaseStyle.wordWrap = true;
                specialUseCaseStyle.fontStyle = FontStyle.Bold;
            }
            specialUseCaseStyle.alignment = TextAnchor.MiddleCenter;
            specialUseCaseStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.18f, 0.18f)
                : new Color(0.82f, 0.04f, 0.04f);
            GUILayout.Space(6);
            GUILayout.Label("Warning (Special Use Case)", specialUseCaseStyle, GUILayout.ExpandWidth(true));
        }

        private static bool ConfirmApply(KaleidoVRCOptimizer window)
        {
            List<string> special = KaleidoVRCOptimizerLogic.CollectSpecialWrites(window);
            if (special.Count > 0)
            {
                return EditorUtility.DisplayDialog(
                    "Warning (Special Use Case)",
                    "Special Use Case options are included. These can break visemes, custom bounds, lighting, audio, or texture quality, and the writes stay on the assets.\n\n"
                    + string.Join("\n", special.ToArray())
                    + "\n\nThis writes the dry-run changes for this workspace only. The other workspace is not touched. Continue?",
                    "Apply anyway",
                    "Cancel");
            }

            return EditorUtility.DisplayDialog(
                "KaleidoVR VRChat Model Optimizer",
                "This writes the dry-run changes for this workspace only. The other workspace is not touched. Continue?",
                "Apply",
                "Cancel");
        }

        private static bool DrawToggle(bool value, string title, string why)
        {
            return DrawToggle(null, null, value, title, why);
        }

        private static bool DrawToggle(KaleidoVRCOptimizer window, string optionId, bool value, string title, string why)
        {
            EditorGUILayout.BeginHorizontal();
            value = EditorGUILayout.ToggleLeft(title, value);
            if (!string.IsNullOrEmpty(optionId) && value && KaleidoOptionUndo.Has(optionId))
            {
                bool enabled = GUI.enabled;
                GUI.enabled = true;
                if (GUILayout.Button("Undo", GUILayout.Width(56), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Undo",
                        "Restore the values from before \"" + title + "\" was applied?",
                        "Undo",
                        "Cancel"))
                    {
                        int missed;
                        int restored = KaleidoOptionUndo.Restore(optionId, out missed);
                        if (restored > 0 && window != null)
                        {
                            window.InvalidateInventory();
                            KaleidoVRCOptimizerLogic.RefreshListedTextureCurrents(window);
                            window.WorkspaceReadyToApply = false;
                        }
                        if (missed > 0)
                        {
                            EditorUtility.DisplayDialog(
                                "Undo",
                                restored == 0
                                    ? "Could not find the saved objects. Nothing was restored."
                                    : "Restored " + restored + ". " + missed + " saved object(s) were not found.",
                                "OK");
                        }
                        if (restored > 0 && !KaleidoOptionUndo.Has(optionId)) value = false;
                    }
                }
                GUI.enabled = enabled;
            }
            EditorGUILayout.EndHorizontal();
            DrawWhy(why);
            GUILayout.Space(3);
            return value;
        }

        private const float FlagEnableW = 58f;
        private const float FlagDisableW = 62f;
        private const float FlagStateW = 52f;

        private struct ImporterFlagSnapshot
        {
            public string readable;
            public string mipmaps;
            public string streaming;
            public string crunch;
            public string maskColor;
            public string alpha;
        }

        private struct SpecialFlagSnapshot
        {
            public string format;
            public string meshComp;
            public string humanoid;
            public string blendShapes;
            public string bounds;
            public string lights;
            public string cameras;
            public string instancing;
            public string mono;
        }

        private static ImporterFlagSnapshot CollectImporterFlagSnapshot(KaleidoVRCOptimizer window)
        {
            ImporterFlagSnapshot snap = new ImporterFlagSnapshot
            {
                readable = "—",
                mipmaps = "—",
                streaming = "—",
                crunch = "—",
                maskColor = "—",
                alpha = "—"
            };
            if (window == null || window.textureUsages == null) return snap;
            int rwOn = 0, rwOff = 0, mipOn = 0, mipOff = 0, streamOn = 0, streamOff = 0, crunchOn = 0, crunchOff = 0, srgbOn = 0, srgbOff = 0, alphaOn = 0, alphaOff = 0;
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage == null || string.IsNullOrEmpty(usage.path)) continue;
                TextureImporter importer = AssetImporter.GetAtPath(usage.path) as TextureImporter;
                if (importer == null) continue;
                if (importer.isReadable) rwOn++; else rwOff++;
                if (importer.mipmapEnabled) mipOn++; else mipOff++;
                if (importer.streamingMipmaps) streamOn++; else streamOff++;
                if (importer.crunchedCompression) crunchOn++; else crunchOff++;
                if (usage.kind == KaleidoTextureKind.Mask)
                {
                    if (importer.sRGBTexture) srgbOn++; else srgbOff++;
                }
                if (usage.kind == KaleidoTextureKind.Albedo && importer.DoesSourceTextureHaveAlpha())
                {
                    if (importer.alphaIsTransparency) alphaOn++; else alphaOff++;
                }
            }
            snap.readable = CountLabel(rwOn, rwOff, "On", "Off");
            snap.mipmaps = CountLabel(mipOn, mipOff, "On", "Off");
            snap.streaming = CountLabel(streamOn, streamOff, "On", "Off");
            snap.crunch = CountLabel(crunchOn, crunchOff, "On", "Off");
            snap.maskColor = CountLabel(srgbOff, srgbOn, "Linear", "sRGB");
            snap.alpha = CountLabel(alphaOn, alphaOff, "On", "Off");
            return snap;
        }

        private static SpecialFlagSnapshot CollectSpecialFlagSnapshot(KaleidoVRCOptimizer window)
        {
            SpecialFlagSnapshot snap = new SpecialFlagSnapshot
            {
                format = "—",
                meshComp = "—",
                humanoid = "—",
                blendShapes = "—",
                bounds = "—",
                lights = "—",
                cameras = "—",
                instancing = "—",
                mono = "—"
            };
            if (window == null) return snap;

            int lightOn = 0, lightOff = 0, camOn = 0, camOff = 0;
            if (window.targets != null)
            {
                for (int i = 0; i < window.targets.Count; i++)
                {
                    GameObject root = window.targets[i] as GameObject;
                    if (root == null) continue;
                    Light[] lights = root.GetComponentsInChildren<Light>(true);
                    for (int l = 0; l < lights.Length; l++)
                    {
                        if (lights[l].enabled) lightOn++; else lightOff++;
                    }
                    Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
                    for (int c = 0; c < cameras.Length; c++)
                    {
                        if (cameras[c].enabled) camOn++; else camOff++;
                    }
                }
            }
            snap.lights = CountLabel(lightOn, lightOff, "On", "Off");
            snap.cameras = CountLabel(camOn, camOff, "On", "Off");

            int instOn = 0, instOff = 0, monoOn = 0, monoOff = 0, blendOn = 0, blendOff = 0, humanOn = 0, humanOff = 0, meshOn = 0, meshOff = 0;
            if (window.inventory != null)
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < window.inventory.Count; i++)
                {
                    KaleidoModelInventoryItem item = window.inventory[i];
                    if (item == null || string.IsNullOrEmpty(item.path) || !seen.Add(item.path)) continue;
                    if (item.asset is Material)
                    {
                        Material material = item.asset as Material;
                        if (material.enableInstancing) instOn++; else instOff++;
                    }
                    AssetImporter importer = AssetImporter.GetAtPath(item.path);
                    ModelImporter model = importer as ModelImporter;
                    if (model != null)
                    {
                        if (model.importBlendShapes) blendOn++; else blendOff++;
                        if (model.animationType == ModelImporterAnimationType.Human) humanOn++; else humanOff++;
                        if (model.meshCompression != ModelImporterMeshCompression.Off) meshOn++; else meshOff++;
                    }
                    AudioImporter audio = importer as AudioImporter;
                    if (audio != null)
                    {
                        if (audio.forceToMono) monoOn++; else monoOff++;
                    }
                }
            }
            snap.instancing = CountLabel(instOn, instOff, "On", "Off");
            snap.mono = CountLabel(monoOn, monoOff, "On", "Off");
            snap.blendShapes = CountLabel(blendOn, blendOff, "On", "Off");
            snap.humanoid = CountLabel(humanOn, humanOff, "Humanoid", "Other");
            snap.meshComp = CountLabel(meshOn, meshOff, "On", "Off");
            return snap;
        }

        private static string CountLabel(int first, int second, string firstName, string secondName)
        {
            if (first == 0 && second == 0) return "—";
            if (first > 0 && second > 0) return "Mixed";
            return first > 0 ? firstName : secondName;
        }

        private static string OnOffNew(bool enable, bool disable)
        {
            if (enable) return "On";
            if (disable) return "Off";
            return "—";
        }

        private static string LinearNew(bool linear, bool srgb)
        {
            if (linear) return "Linear";
            if (srgb) return "sRGB";
            return "—";
        }

        private static void DrawImporterFlagHeaders()
        {
            GUIStyle header = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.MinWidth(140), GUILayout.ExpandWidth(true));
            GUILayout.Label("Enable", header, GUILayout.Width(FlagEnableW));
            GUILayout.Label("Disable", header, GUILayout.Width(FlagDisableW));
            GUILayout.Label("Current", header, GUILayout.Width(FlagStateW));
            GUILayout.Label("New", header, GUILayout.Width(FlagStateW));
            GUILayout.Space(60);
            EditorGUILayout.EndHorizontal();
        }

        private static bool DrawCenteredToggle(bool value, float width)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.MaxWidth(width));
            GUILayout.FlexibleSpace();
            value = GUILayout.Toggle(value, GUIContent.none, GUILayout.Width(16));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private static void DrawEnableDisableRow(
            KaleidoVRCOptimizer window,
            string title,
            string why,
            string enableOptionId,
            string disableOptionId,
            ref bool enable,
            ref bool disable,
            string current,
            string next)
        {
            DrawEnableDisableRow(window, title, why, enableOptionId, disableOptionId, ref enable, ref disable, current, next, "On", "Off");
        }

        private static void DrawEnableDisableRow(
            KaleidoVRCOptimizer window,
            string title,
            string why,
            string enableOptionId,
            string disableOptionId,
            ref bool enable,
            ref bool disable,
            string current,
            string next,
            string enableName,
            string disableName)
        {
            GUIStyle center = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(title, GUILayout.MinWidth(140), GUILayout.ExpandWidth(true));
            bool nextEnable = DrawCenteredToggle(enable, FlagEnableW);
            bool nextDisable = DrawCenteredToggle(disable, FlagDisableW);
            if (nextEnable != enable)
            {
                enable = nextEnable;
                if (enable) disable = false;
            }
            else if (nextDisable != disable)
            {
                disable = nextDisable;
                if (disable) enable = false;
            }
            GUILayout.Label(current, center, GUILayout.Width(FlagStateW));
            GUILayout.Label(next, center, GUILayout.Width(FlagStateW));
            string undoId = enable ? enableOptionId : (disable ? disableOptionId : "");
            if (!string.IsNullOrEmpty(undoId) && KaleidoOptionUndo.Has(undoId))
            {
                bool wasEnabled = GUI.enabled;
                GUI.enabled = true;
                if (GUILayout.Button("Undo", GUILayout.Width(56), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Undo",
                        "Restore the values from before \"" + title + "\" was applied?",
                        "Undo",
                        "Cancel"))
                    {
                        int missed;
                        int restored = KaleidoOptionUndo.Restore(undoId, out missed);
                        if (restored > 0 && window != null)
                        {
                            window.InvalidateInventory();
                            KaleidoVRCOptimizerLogic.RefreshListedTextureCurrents(window);
                            window.WorkspaceReadyToApply = false;
                        }
                        if (missed > 0)
                        {
                            EditorUtility.DisplayDialog(
                                "Undo",
                                restored == 0
                                    ? "Could not find the saved objects. Nothing was restored."
                                    : "Restored " + restored + ". " + missed + " saved object(s) were not found.",
                                "OK");
                        }
                        if (restored > 0 && !KaleidoOptionUndo.Has(undoId))
                        {
                            if (string.Equals(undoId, enableOptionId, StringComparison.Ordinal)) enable = false;
                            else if (string.Equals(undoId, disableOptionId, StringComparison.Ordinal)) disable = false;
                        }
                    }
                }
                GUI.enabled = wasEnabled;
            }
            else GUILayout.Space(60);
            EditorGUILayout.EndHorizontal();
            DrawWhy(why);
            GUILayout.Space(3);
        }

        private static void DrawWhy(string why)
        {
            EditorGUILayout.LabelField(why, MiniWrap());
        }

        private static GUIStyle onUploadPrefNoteStyle;

        private static GUIStyle OnUploadPrefNoteStyle()
        {
            if (onUploadPrefNoteStyle == null)
                onUploadPrefNoteStyle = new GUIStyle(EditorStyles.miniLabel);
            onUploadPrefNoteStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.72f, 0.28f, 1f)
                : new Color(0.72f, 0.38f, 0.04f, 1f);
            return onUploadPrefNoteStyle;
        }

        private static GUIStyle smallRedWarningStyle;

        private static void DrawSmallRedWarning(string text)
        {
            if (smallRedWarningStyle == null)
            {
                smallRedWarningStyle = new GUIStyle(EditorStyles.boldLabel);
                smallRedWarningStyle.wordWrap = true;
            }
            smallRedWarningStyle.fontSize = 18;
            smallRedWarningStyle.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.32f, 0.32f)
                : new Color(0.82f, 0.04f, 0.04f);
            GUILayout.Label(text, smallRedWarningStyle);
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
                    HasSelectedAvatar(window)
                        ? "No scan yet. Press Scan Performance."
                        : "No scan yet. Select a VRChat avatar on Setup first.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(report.summary, MessageType.None);
            if (report.hasOnUploadEstimate)
                DrawNowUploadHeader();
            string rank = window.IsQuestWorkspace ? report.questRank : report.pcRank;
            string uploadRank = window.IsQuestWorkspace ? report.onUploadQuestRank : report.onUploadPcRank;
            DrawStatNowUpload("Rank", rank, uploadRank, report.hasOnUploadEstimate, IsProblemRank(rank));
            if (report.meshReadWriteDisabled)
            {
                EditorGUILayout.HelpBox("Mesh Read/Write is disabled on at least one mesh. VRChat ranks that avatar Very Poor until Read/Write is enabled.", MessageType.Error);
            }
            DrawCountNowUpload("Triangles", report.triangles, report.onUploadTriangles, report.hasOnUploadEstimate);
            DrawCountNowUpload("Material Slots", report.materialSlots, report.onUploadMaterialSlots, report.hasOnUploadEstimate);
            EditorGUILayout.LabelField("Unique Materials", report.uniqueMaterials.ToString("N0"));
            DrawCountNowUpload("Skinned Meshes", report.skinnedMeshes, report.onUploadSkinnedMeshes, report.hasOnUploadEstimate);
            DrawCountNowUpload("Basic Meshes", report.meshRenderers, report.onUploadMeshRenderers, report.hasOnUploadEstimate);
            EditorGUILayout.LabelField("Unique Textures", report.uniqueTextures.ToString("N0"));
            DrawCountNowUpload("Blend Shapes", report.blendShapes, report.onUploadBlendShapes, report.hasOnUploadEstimate);
            DrawCountNowUpload("Bones (max on one mesh)", report.bones, report.onUploadBones, report.hasOnUploadEstimate);
            DrawCountNowUpload("Animators", report.animators, report.onUploadAnimators, report.hasOnUploadEstimate);
            DrawCountNowUpload("Lights", report.lights, report.onUploadLights, report.hasOnUploadEstimate);
            DrawCountNowUpload("Audio Sources", report.audioSources, report.onUploadAudioSources, report.hasOnUploadEstimate);
            DrawCountNowUpload("Particle Systems", report.particleSystems, report.onUploadParticleSystems, report.hasOnUploadEstimate);
            DrawCountNowUpload("PhysBones", report.physBones, report.onUploadPhysBones, report.hasOnUploadEstimate);
            DrawCountNowUpload("PhysBone Colliders", report.physBoneColliders, report.onUploadPhysBoneColliders, report.hasOnUploadEstimate);
            DrawCountNowUpload("Contacts", report.contacts, report.onUploadContacts, report.hasOnUploadEstimate);
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
            DrawWhy("Fix uses the same orange and writes now. Ignore leaves that row alone. Write Defaults still asks Confirm after On or Off. Those writes stay in Unity.");
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

        private static void DrawNowUploadHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
            GUILayout.Label("Now", EditorStyles.miniBoldLabel, GUILayout.Width(88));
            GUILayout.Label("On Upload", EditorStyles.miniBoldLabel, GUILayout.Width(88));
            GUILayout.Label("Change", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawCountNowUpload(string label, int now, int upload, bool hasUpload)
        {
            if (!hasUpload)
            {
                EditorGUILayout.LabelField(label, now.ToString("N0"));
                return;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.LabelField(now.ToString("N0"), GUILayout.Width(88));
            EditorGUILayout.LabelField(upload.ToString("N0"), GUILayout.Width(88));
            int delta = upload - now;
            EditorGUILayout.LabelField(delta == 0 ? "—" : delta.ToString("N0"), GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawStatNowUpload(string label, string now, string upload, bool hasUpload, bool warn)
        {
            EnsureSizeStyles();
            if (!hasUpload)
            {
                DrawStatRow(label, now, warn);
                return;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.LabelField(now ?? "—", warn ? sizeUpStyle : EditorStyles.label, GUILayout.Width(88));
            EditorGUILayout.LabelField(upload ?? "—", GUILayout.Width(88));
            bool same = string.Equals(now, upload, StringComparison.Ordinal);
            EditorGUILayout.LabelField(same ? "—" : (upload ?? "—"), GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
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
            DrawCountNowUpload("Constraints", report.constraints, report.onUploadConstraints, report.hasOnUploadEstimate);
            DrawCountNowUpload("Unity constraints", report.unityConstraints, report.onUploadUnityConstraints, report.hasOnUploadEstimate);
            DrawCountNowUpload("VRChat constraints", report.vrcConstraints, report.onUploadVrcConstraints, report.hasOnUploadEstimate);
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

        private static GameObject FirstDroppedAvatarRoot(KaleidoVRCOptimizer window)
        {
            List<GameObject> roots = CurrentAvatarRoots(window);
            return roots.Count > 0 ? roots[0] : null;
        }

        private static bool HasSelectedAvatar(KaleidoVRCOptimizer window)
        {
            return CurrentAvatarRoots(window).Count > 0;
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
                bool hasAvatar = window.targets != null && window.targets.Count > 0 && window.targets[0] != null;
                EditorGUILayout.HelpBox(
                    hasAvatar && window.inventoryFillQueued
                        ? "Reading this avatar…"
                        : "Drop an avatar above. Its meshes, materials, textures, clips, and menus will list here.",
                    MessageType.None);
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

            UnityEngine.Object current = list != null && list.Count > 0 ? list[0] : null;
            Rect titleRect = new Rect(dropArea.x + 10, dropArea.y + 10, dropArea.width - 20, 24);
            Rect hintRect = new Rect(dropArea.x + 12, dropArea.y + 34, dropArea.width - 24, 36);
            GUI.Label(titleRect, dropLabel, DropTitleStyle());
            string hint = current == null
                ? "Prefab, scene instance, or character FBX  ·  not folders, worlds, or loose textures"
                : "This avatar is ready  ·  drop another to replace it";
            GUI.Label(hintRect, hint, DropHintStyle());
            KaleidoVRCOptimizerHelpers.HandleDragAndDrop(dropArea, list, true, true);
        }

        private static void DrawAvatarTarget(KaleidoVRCOptimizer window)
        {
            List<UnityEngine.Object> list = window != null ? window.targets : null;
            if (list == null) return;
            DrawProminentAvatarDrop(list, "Drop your VRChat avatar here");

            UnityEngine.Object current = list.Count > 0 ? list[0] : null;
            int id = current != null ? current.GetInstanceID() : 0;
            if (id != window.lastAvatarCheckId)
            {
                window.lastAvatarCheckId = id;
                if (current == null)
                {
                    window.lastAvatarCheckOk = true;
                    window.lastAvatarCheckReason = "";
                }
                else
                {
                    GameObject resolved;
                    string reason;
                    window.lastAvatarCheckOk = KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(current, out resolved, out reason);
                    window.lastAvatarCheckReason = reason;
                }
            }
            if (current != null && !window.lastAvatarCheckOk)
                EditorGUILayout.HelpBox(window.lastAvatarCheckReason, MessageType.Warning);

            Rect rowRect = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.18f, 0.18f, 1f));
            }
            GUILayout.Space(5);
            UnityEngine.Object next = EditorGUILayout.ObjectField(current, typeof(GameObject), true, GUILayout.ExpandWidth(true));
            bool applyField = true;

            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && rowRect.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    UnityEngine.Object[] dropped = DragAndDrop.objectReferences;
                    if (dropped == null) dropped = new UnityEngine.Object[0];
                    for (int d = 0; d < dropped.Length; d++)
                    {
                        GameObject resolved;
                        string reason;
                        if (KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(dropped[d], out resolved, out reason) && resolved != null)
                        {
                            SetSingleAvatar(list, resolved);
                            GUI.changed = true;
                            applyField = false;
                            break;
                        }
                        if (dropped[d] != null)
                            Debug.LogWarning("[KaleidoVR] VRChat Model Optimizer: " + reason);
                    }
                }
                evt.Use();
            }

            if (current != null && GUILayout.Button("X", GUILayout.Width(25)))
            {
                list.Clear();
                GUI.changed = true;
                applyField = false;
            }
            EditorGUILayout.EndHorizontal();

            if (applyField && next != current) SetSingleAvatar(list, next);
        }

        private static void SetSingleAvatar(List<UnityEngine.Object> list, UnityEngine.Object obj)
        {
            list.Clear();
            if (obj == null) return;
            GameObject resolved;
            string reason;
            if (KaleidoVRCOptimizerHelpers.TryResolveVrchatAvatarModel(obj, out resolved, out reason) && resolved != null)
                list.Add(resolved);
            else
                list.Add(obj);
        }

        private static void DrawObjectList(List<UnityEngine.Object> list, string dropLabel)
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, dropLabel, EditorStyles.helpBox);
            KaleidoVRCOptimizerHelpers.HandleDragAndDrop(dropArea, list, false);

            for (int i = 0; i < list.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                GUILayout.Space(5);
                list[i] = EditorGUILayout.ObjectField(list[i], typeof(UnityEngine.Object), true, GUILayout.ExpandWidth(true));

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
        public static void HandleDragAndDrop(Rect dropArea, List<UnityEngine.Object> targetList, bool avatarModelsOnly, bool replaceExisting = false)
        {
            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    UnityEngine.Object replacement = null;
                    bool haveReplacement = false;
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
                        if (replaceExisting)
                        {
                            replacement = toAdd;
                            haveReplacement = true;
                            break;
                        }
                        if (!targetList.Contains(toAdd)) targetList.Add(toAdd);
                    }
                    if (replaceExisting && haveReplacement)
                    {
                        targetList.Clear();
                        targetList.Add(replacement);
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

        public static string OptimizerLogDirectory()
        {
            return Path.Combine(GetProjectRootPath(), "Logs", "KaleidoVR", "Optimizer");
        }

        public static int ClearOptimizerLogFiles()
        {
            string dir = OptimizerLogDirectory();
            if (!Directory.Exists(dir)) return 0;

            int removed = 0;
            string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    File.Delete(files[i]);
                    removed++;
                }
                catch (Exception)
                {
                }
            }

            string[] folders = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);
            Array.Sort(folders, (a, b) => b.Length.CompareTo(a.Length));
            for (int i = 0; i < folders.Length; i++)
            {
                try
                {
                    if (Directory.Exists(folders[i]) && Directory.GetFileSystemEntries(folders[i]).Length == 0)
                        Directory.Delete(folders[i]);
                }
                catch (Exception)
                {
                }
            }

            return removed;
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

        public static bool IsPrimaryAlbedoProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return false;
            return propertyName.Equals("_MainTex", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_BaseMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_BaseColorMap", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSpecificNonAlbedoProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return false;
            return propertyName.Equals("_BumpMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_NormalMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_DetailNormalMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_MetallicGlossMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_SpecGlossMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_OcclusionMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_ParallaxMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_EmissionMap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_MatCap", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("_MatcapTex", StringComparison.OrdinalIgnoreCase);
        }

        static readonly Dictionary<string, Type> typeByFullName = new Dictionary<string, Type>();
        static readonly HashSet<string> typeLookupMiss = new HashSet<string>();

        public static Type FindTypeByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            Type cached;
            if (typeByFullName.TryGetValue(fullName, out cached)) return cached;
            if (typeLookupMiss.Contains(fullName)) return null;

            Type found = Type.GetType(fullName + ", VRC.SDK3A") ?? Type.GetType(fullName);
            if (found == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    try
                    {
                        found = assemblies[i].GetType(fullName, false);
                    }
                    catch (Exception)
                    {
                        found = null;
                    }
                    if (found != null) break;
                }
            }
            if (found == null) typeLookupMiss.Add(fullName);
            else typeByFullName[fullName] = found;
            return found;
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
            window.KeepSingleAvatarTarget();
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
                            string logDir = KaleidoVRCOptimizerHelpers.OptimizerLogDirectory();
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
                if (!apply || window.dryRun)
                    FillOnUploadEstimate(window, roots, report, logEntries);
                KaleidoVRCOptimizerEval.Evaluate(window, roots, report);
                BuildHints(report, window.IsQuestWorkspace);

                if (apply)
                {
                    try
                    {
                        ApplyOptimizations(window, roots, assetPaths, ignorePaths, report, logEntries);
                    }
                    finally
                    {
                        KaleidoOptionUndo.EndApplyLog();
                    }
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

            if (window.writeLog || KaleidoOptionUndo.WroteSpecial)
            {
                try
                {
                    string logDir = KaleidoVRCOptimizerHelpers.OptimizerLogDirectory();
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
            window.KeepSingleAvatarTarget();
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
            RememberTextureRow(usage);
        }

        public static void RememberTextureRow(KaleidoTextureUsage usage)
        {
            if (usage == null || string.IsNullOrEmpty(usage.path)) return;
            EnsureTextureRowPrefs();
            bool keep = usage.ignorePc || usage.ignoreQuest || usage.usedCustomPc || usage.usedCustomQuest || usage.usedCustomCrunch;
            if (!keep)
            {
                if (textureRowPrefs.Remove(usage.path)) WriteTextureRowPrefs();
                return;
            }
            textureRowPrefs[usage.path] = new KaleidoTextureRowPref
            {
                path = usage.path,
                ignorePc = usage.ignorePc,
                ignoreQuest = usage.ignoreQuest,
                customPc = usage.usedCustomPc,
                customQuest = usage.usedCustomQuest,
                customCrunch = usage.usedCustomCrunch,
                pcSize = usage.pcSize,
                questSize = usage.questSize,
                crunchQuality = KaleidoVRCOptimizer.ClampCrunchQuality(usage.crunchQuality)
            };
            WriteTextureRowPrefs();
        }

        public static void SaveTextureRowPrefs(KaleidoVRCOptimizer window)
        {
            if (window == null || window.textureUsages == null) return;
            EnsureTextureRowPrefs();
            bool changed = false;
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage == null || string.IsNullOrEmpty(usage.path)) continue;
                bool keep = usage.ignorePc || usage.ignoreQuest || usage.usedCustomPc || usage.usedCustomQuest || usage.usedCustomCrunch;
                if (!keep)
                {
                    if (textureRowPrefs.Remove(usage.path)) changed = true;
                    continue;
                }
                KaleidoTextureRowPref next = new KaleidoTextureRowPref
                {
                    path = usage.path,
                    ignorePc = usage.ignorePc,
                    ignoreQuest = usage.ignoreQuest,
                    customPc = usage.usedCustomPc,
                    customQuest = usage.usedCustomQuest,
                    customCrunch = usage.usedCustomCrunch,
                    pcSize = usage.pcSize,
                    questSize = usage.questSize,
                    crunchQuality = KaleidoVRCOptimizer.ClampCrunchQuality(usage.crunchQuality)
                };
                KaleidoTextureRowPref existing;
                if (textureRowPrefs.TryGetValue(usage.path, out existing)
                    && existing.ignorePc == next.ignorePc
                    && existing.ignoreQuest == next.ignoreQuest
                    && existing.customPc == next.customPc
                    && existing.customQuest == next.customQuest
                    && existing.customCrunch == next.customCrunch
                    && existing.pcSize == next.pcSize
                    && existing.questSize == next.questSize
                    && existing.crunchQuality == next.crunchQuality)
                    continue;
                textureRowPrefs[usage.path] = next;
                changed = true;
            }
            if (changed) WriteTextureRowPrefs();
        }

        private static void ApplyTextureRowPref(KaleidoTextureUsage usage)
        {
            if (usage == null || string.IsNullOrEmpty(usage.path)) return;
            EnsureTextureRowPrefs();
            KaleidoTextureRowPref pref;
            if (!textureRowPrefs.TryGetValue(usage.path, out pref)) return;
            usage.ignorePc = pref.ignorePc;
            usage.ignoreQuest = pref.ignoreQuest;
            if (pref.customPc && pref.pcSize > 0)
            {
                usage.usedCustomPc = true;
                usage.pcSize = pref.pcSize;
            }
            if (pref.customQuest && pref.questSize > 0)
            {
                usage.usedCustomQuest = true;
                usage.questSize = pref.questSize;
            }
            if (pref.customCrunch)
            {
                usage.usedCustomCrunch = true;
                usage.crunchQuality = KaleidoVRCOptimizer.ClampCrunchQuality(pref.crunchQuality);
            }
        }

        private static void EnsureTextureRowPrefs()
        {
            if (textureRowPrefsLoaded) return;
            textureRowPrefsLoaded = true;
            textureRowPrefs = new Dictionary<string, KaleidoTextureRowPref>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = TextureRowPrefsPath();
                if (!File.Exists(path)) return;
                KaleidoTextureRowPrefList file = JsonUtility.FromJson<KaleidoTextureRowPrefList>(File.ReadAllText(path));
                if (file == null || file.items == null) return;
                for (int i = 0; i < file.items.Length; i++)
                {
                    KaleidoTextureRowPref item = file.items[i];
                    if (item == null || string.IsNullOrEmpty(item.path)) continue;
                    textureRowPrefs[item.path] = item;
                }
            }
            catch (Exception)
            {
                textureRowPrefs = new Dictionary<string, KaleidoTextureRowPref>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void WriteTextureRowPrefs()
        {
            try
            {
                string path = TextureRowPrefsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                List<KaleidoTextureRowPref> items = new List<KaleidoTextureRowPref>();
                foreach (KeyValuePair<string, KaleidoTextureRowPref> pair in textureRowPrefs)
                {
                    if (pair.Value == null || string.IsNullOrEmpty(pair.Value.path)) continue;
                    items.Add(pair.Value);
                }
                KaleidoTextureRowPrefList file = new KaleidoTextureRowPrefList { items = items.ToArray() };
                File.WriteAllText(path, JsonUtility.ToJson(file));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KaleidoVR] Could not save texture row choices: " + ex.Message);
            }
        }

        private static string TextureRowPrefsPath()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "KaleidoVR", "optimizer-texture-rows.json");
        }

        private static Dictionary<string, KaleidoTextureRowPref> textureRowPrefs = new Dictionary<string, KaleidoTextureRowPref>(StringComparer.OrdinalIgnoreCase);
        private static bool textureRowPrefsLoaded;

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

        public static int GetPlannedCrunchQuality(KaleidoVRCOptimizer window, KaleidoTextureUsage usage)
        {
            if (usage != null && usage.usedCustomCrunch)
                return KaleidoVRCOptimizer.ClampCrunchQuality(usage.crunchQuality);
            int global = window != null ? window.textureCrunchQuality : 50;
            return KaleidoVRCOptimizer.ClampCrunchQuality(global);
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
                if (usage.ignoreQuest) return usage.currentQuest;
                return CapTypeMaxSize(typeApply, typeQuest, usage.currentQuest);
            }
            if (usage.usedCustomPc) return usage.pcSize;
            if (usage.ignorePc) return usage.currentPc;
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
            bool ignored = questPlatform ? usage.ignoreQuest : usage.ignorePc;
            bool custom = questPlatform ? usage.usedCustomQuest : usage.usedCustomPc;
            if (ignored && !custom) return false;
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
                    bool typeApply;
                    int typePc;
                    int typeQuest;
                    GetTypeSizes(window, usage.kind, out typeApply, out typePc, out typeQuest);
                    if (typeApply)
                    {
                        string slot = questPlatform ? "android" : "pc";
                        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(questPlatform ? "Android" : "Standalone");
                        string data = KaleidoOptionUndo.Join(
                            KaleidoOptionUndo.Int("platOver", settings.overridden ? 1 : 0),
                            KaleidoOptionUndo.Int("platMax", settings.maxTextureSize));
                        if (!questPlatform)
                            data = KaleidoOptionUndo.Join(data, KaleidoOptionUndo.Int("max", importer.maxTextureSize));
                        if (WriteMaxSizeOnly(importer, planned, questPlatform))
                            KaleidoOptionUndo.Capture(KaleidoOptionUndo.SizeOptionId(usage.kind), "texture", usage.path, "", "", slot, data);
                        else continue;
                    }
                    else if (!WriteMaxSizeOnly(importer, planned, questPlatform)) continue;
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
                KaleidoOptionUndo.Save();
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

        public static void RefreshListedTextureCurrents(KaleidoVRCOptimizer window)
        {
            if (window == null || window.textureUsages == null) return;
            for (int i = 0; i < window.textureUsages.Count; i++)
            {
                KaleidoTextureUsage usage = window.textureUsages[i];
                if (usage == null || string.IsNullOrEmpty(usage.path)) continue;
                TextureImporter importer = AssetImporter.GetAtPath(usage.path) as TextureImporter;
                if (importer == null) continue;
                usage.currentPc = importer.maxTextureSize;
                usage.currentQuest = importer.maxTextureSize;
                TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
                if (standalone != null && standalone.overridden) usage.currentPc = standalone.maxTextureSize;
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                if (android != null && android.overridden) usage.currentQuest = android.maxTextureSize;
            }
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

            for (int i = 0; i < window.textureUsages.Count; i++)
                PromoteMainSlotKind(window.textureUsages[i]);

            window.textureUsages.Sort(delegate (KaleidoTextureUsage a, KaleidoTextureUsage b)
            {
                int kind = ((int)a.kind).CompareTo((int)b.kind);
                if (kind != 0) return kind;
                string an = a.texture != null ? a.texture.name : a.path;
                string bn = b.texture != null ? b.texture.name : b.path;
                return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static void PromoteMainSlotKind(KaleidoTextureUsage usage)
        {
            if (usage == null || usage.kind != KaleidoTextureKind.Other || usage.links == null) return;
            bool mainSlot = false;
            for (int i = 0; i < usage.links.Count; i++)
            {
                KaleidoTextureLink link = usage.links[i];
                if (link == null) continue;
                if (KaleidoVRCOptimizerHelpers.IsSpecificNonAlbedoProperty(link.propertyName)) return;
                if (KaleidoVRCOptimizerHelpers.IsPrimaryAlbedoProperty(link.propertyName)) mainSlot = true;
            }
            if (mainSlot) usage.kind = KaleidoTextureKind.Albedo;
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
            if (previous != null && previous.TryGetValue(path, out old))
            {
                usage.ignorePc = old.ignorePc;
                usage.ignoreQuest = old.ignoreQuest;
                if (old.usedCustomPc || old.usedCustomQuest)
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
                if (old.usedCustomCrunch)
                {
                    usage.usedCustomCrunch = true;
                    usage.crunchQuality = KaleidoVRCOptimizer.ClampCrunchQuality(old.crunchQuality);
                }
            }
            else ApplyTextureRowPref(usage);

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

        private static void FillOnUploadEstimate(KaleidoVRCOptimizer window, List<GameObject> roots, KaleidoOptimizerReport report, List<string> logEntries)
        {
            report.hasOnUploadEstimate = false;
            if (window == null || report == null || roots == null || roots.Count == 0) return;
            KaleidoAvatarPassSettings settings = KaleidoAvatarPass.FromWindow(window);
            if (settings == null || !settings.applyOnUpload) return;

            List<GameObject> copies = new List<GameObject>();
            try
            {
                EditorUtility.DisplayProgressBar("KaleidoVR VRChat Model Optimizer", "Estimating On Upload…", 0.55f);
                for (int i = 0; i < roots.Count; i++)
                {
                    GameObject root = roots[i];
                    if (root == null) continue;
                    GameObject copy = UnityEngine.Object.Instantiate(root);
                    copy.name = root.name + "_KaleidoRank";
                    copy.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInHierarchy;
                    KaleidoAvatarPass.Run(copy, settings, false, KaleidoAvatarPass.ExclusionsOnCopy(window, root, copy), false);
                    copies.Add(copy);
                }
                if (copies.Count == 0) return;

                KaleidoOptimizerReport projected = new KaleidoOptimizerReport();
                GatherStats(copies, new HashSet<string>(StringComparer.OrdinalIgnoreCase), projected, new List<string>());
                projected.textureBytesEstimate = report.textureBytesEstimate;
                projected.uniqueTextures = report.uniqueTextures;
                projected.uniqueMaterials = report.uniqueMaterials;
                projected.meshReadWriteDisabled = report.meshReadWriteDisabled;
                projected.pcRank = RankAvatar(projected, false);
                projected.questRank = RankAvatar(projected, true);

                report.hasOnUploadEstimate = true;
                report.onUploadPcRank = projected.pcRank;
                report.onUploadQuestRank = projected.questRank;
                report.onUploadTriangles = projected.triangles;
                report.onUploadMaterialSlots = projected.materialSlots;
                report.onUploadUniqueMaterials = projected.uniqueMaterials;
                report.onUploadSkinnedMeshes = projected.skinnedMeshes;
                report.onUploadMeshRenderers = projected.meshRenderers;
                report.onUploadBlendShapes = projected.blendShapes;
                report.onUploadBones = projected.bones;
                report.onUploadAnimators = projected.animators;
                report.onUploadLights = projected.lights;
                report.onUploadAudioSources = projected.audioSources;
                report.onUploadParticleSystems = projected.particleSystems;
                report.onUploadPhysBones = projected.physBones;
                report.onUploadPhysBoneColliders = projected.physBoneColliders;
                report.onUploadContacts = projected.contacts;
                report.onUploadConstraints = projected.constraints;
                report.onUploadUnityConstraints = projected.unityConstraints;
                report.onUploadVrcConstraints = projected.vrcConstraints;
                if (logEntries != null)
                    logEntries.Add("On Upload estimate: slots " + report.materialSlots + "→" + report.onUploadMaterialSlots
                        + " skinned " + report.skinnedMeshes + "→" + report.onUploadSkinnedMeshes
                        + " shapes " + report.blendShapes + "→" + report.onUploadBlendShapes
                        + " pc " + report.pcRank + "→" + report.onUploadPcRank);
            }
            catch (Exception)
            {
                report.hasOnUploadEstimate = false;
            }
            finally
            {
                for (int i = 0; i < copies.Count; i++)
                {
                    if (copies[i] != null) UnityEngine.Object.DestroyImmediate(copies[i]);
                }
                EditorUtility.ClearProgressBar();
            }
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
            window.optimizeSceneExtras = window.optimizeParticles || window.disableLightsOnAvatar || window.enableLightsOnAvatar || window.disableCamerasOnAvatar || window.enableCamerasOnAvatar;
            List<string> changedImporters = new List<string>();
            KaleidoOptionUndo.BeginApplyLog(logEntries, write);
            logEntries.Add("");
            logEntries.Add("Chosen writes");
            LogTextureChoices(window, logEntries);
            LogSpecialSelection(window, logEntries);
            logEntries.Add("");
            logEntries.Add(write ? "Changes" : "Would change");

            if (write) AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in assetPaths)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(path);
                    if (importer == null) continue;

                    if (window.optimizeTextures && importer is TextureImporter textureImporter)
                    {
                        List<string> details = new List<string>();
                        if (ApplyTextureImporter(window, path, textureImporter, write, details))
                        {
                            KaleidoTextureUsage logged = FindTextureUsage(window, path);
                            KaleidoTextureKind kind = logged != null
                                ? logged.kind
                                : KaleidoVRCOptimizerHelpers.ClassifyTexture(path, textureImporter);
                            string line = "Texture (" + kind + "): " + path;
                            if (details.Count > 0) line += " — " + JoinChangeSummary(details);
                            report.planned.Add(line);
                            logEntries.Add("Texture (" + kind + "): " + path);
                            for (int d = 0; d < details.Count; d++) logEntries.Add(details[d]);
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
                            KaleidoOptionUndo.Capture(KaleidoOptionUndo.GpuInstancing, "material", path, "", "", "", KaleidoOptionUndo.Int("instancing", material.enableInstancing ? 1 : 0));
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
                    KaleidoOptionUndo.Save();
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
                ApplyHierarchy(window, root, write, report, logEntries, null);

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
                    ApplyHierarchy(window, contents, true, report, logEntries, prefabPath);
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
                    ApplyHierarchy(window, contents, true, report, logEntries, path);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    logEntries.Add("Updated prefab: " + path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            if (write) KaleidoOptionUndo.Save();
        }

        public static List<string> CollectSpecialWrites(KaleidoVRCOptimizer window)
        {
            List<string> chosen = new List<string>();
            if (window == null) return chosen;
            if (!window.IsQuestWorkspace && window.applyPcTexFormat) chosen.Add("• Set compression (" + window.pcTexFormat + ")");
            if (window.IsQuestWorkspace && window.applyAndroidTexFormat) chosen.Add("• Set compression format (" + window.androidTexFormat + ")");
            if (!window.IsQuestWorkspace && window.applyMeshCompression) chosen.Add("• Apply mesh compression (" + window.meshCompression + ")");
            if (!window.IsQuestWorkspace && window.meshForceHumanoid) chosen.Add("• Force Humanoid rig");
            if (!window.IsQuestWorkspace && window.meshRestoreBlendShapes) chosen.Add("• Blend shape import On");
            if (!window.IsQuestWorkspace && window.meshStripBlendShapes) chosen.Add("• Blend shape import Off");
            if (!window.IsQuestWorkspace && window.rendererRecalculateBounds) chosen.Add("• Recalculate skinned bounds");
            if (window.enableLightsOnAvatar) chosen.Add("• Realtime lights On");
            if (window.disableLightsOnAvatar) chosen.Add("• Realtime lights Off");
            if (window.IsQuestWorkspace && window.enableCamerasOnAvatar) chosen.Add("• Cameras On");
            if (window.IsQuestWorkspace && window.disableCamerasOnAvatar) chosen.Add("• Cameras Off");
            if (window.IsQuestWorkspace && window.optimizeMaterials)
                chosen.Add(window.materialEnableGpuInstancing ? "• GPU instancing On" : "• GPU instancing Off");
            if (!window.IsQuestWorkspace && window.audioForceToMono) chosen.Add("• Force audio to mono");
            if (!window.IsQuestWorkspace && window.audioForceToStereo) chosen.Add("• Force audio to stereo");
            return chosen;
        }

        private static void LogTextureChoices(KaleidoVRCOptimizer window, List<string> logEntries)
        {
            if (window == null || logEntries == null) return;
            if (!window.optimizeTextures)
            {
                logEntries.Add("  Textures: skipped");
                return;
            }

            int added = 0;
            if (!window.IsQuestWorkspace)
            {
                added += LogChoice(logEntries, "Read / Write", window.textureEnableReadWrite, window.textureDisableReadWrite);
                if (window.textureApplyMipmaps)
                    added += LogChoice(logEntries, "Mip maps", window.textureEnableMipmaps, !window.textureEnableMipmaps);
                added += LogChoice(logEntries, "Streaming mip maps", window.textureEnableStreamingMipmaps, window.textureDisableStreamingMipmaps);
                added += LogChoice(logEntries, "Crunch compression", window.textureEnableCrunch, window.textureDisableCrunch);
                if (window.textureApplyAniso)
                {
                    logEntries.Add("  Aniso: " + window.textureAniso);
                    added++;
                }
                if (window.autoDetectNormalMaps)
                {
                    logEntries.Add("  Detect normal maps: Enable");
                    added++;
                }
                added += LogChoice(logEntries, "Linear color for mask maps", window.autoLinearMaskMaps, window.autoSrgbMaskMaps, "Linear", "sRGB");
                added += LogChoice(logEntries, "Alpha Is Transparency", window.alphaIsTransparencyOnAlbedo, window.alphaIsTransparencyOffAlbedo);
            }
            else
            {
                added += LogChoice(logEntries, "Streaming mip maps", window.textureEnableStreamingMipmaps, window.textureDisableStreamingMipmaps);
            }

            if (added == 0) logEntries.Add("  Textures: max sizes / row selectors only");
        }

        private static int LogChoice(List<string> logEntries, string title, bool enable, bool disable)
        {
            return LogChoice(logEntries, title, enable, disable, "Enable", "Disable");
        }

        private static int LogChoice(List<string> logEntries, string title, bool enable, bool disable, string enableName, string disableName)
        {
            if (enable) logEntries.Add("  " + title + ": " + enableName);
            else if (disable) logEntries.Add("  " + title + ": " + disableName);
            else return 0;
            return 1;
        }

        private static void LogSpecialSelection(KaleidoVRCOptimizer window, List<string> logEntries)
        {
            if (window == null || logEntries == null) return;
            List<string> chosen = CollectSpecialWrites(window);
            if (chosen.Count == 0)
            {
                logEntries.Add("  Special: none");
                KaleidoOptionUndo.NoteHeader("Special chosen: none");
                return;
            }
            logEntries.Add("  Special:");
            for (int i = 0; i < chosen.Count; i++)
                logEntries.Add("    " + chosen[i].Replace("• ", ""));
            KaleidoOptionUndo.NoteHeader("Special chosen: " + string.Join(", ", chosen.ToArray()).Replace("• ", ""));
        }

        private static string JoinChangeSummary(List<string> details)
        {
            if (details == null || details.Count == 0) return "";
            List<string> parts = new List<string>();
            for (int i = 0; i < details.Count; i++)
            {
                string line = details[i] != null ? details[i].Trim() : "";
                if (line.Length > 0) parts.Add(line);
            }
            return string.Join("; ", parts.ToArray());
        }

        private static void AddChange(List<string> details, string name, string from, string to)
        {
            if (details == null) return;
            if (string.Equals(from, to, StringComparison.Ordinal)) return;
            details.Add("  " + name + "  " + from + " → " + to);
        }

        private static string FlagWord(bool on)
        {
            return on ? "On" : "Off";
        }

        private static bool ApplyStreamingMipmaps(KaleidoVRCOptimizer window, TextureImporter importer, string path, bool write, List<string> details)
        {
            if (window == null || importer == null) return false;
            if (window.textureEnableStreamingMipmaps)
            {
                bool mipsOn = window.textureApplyMipmaps ? window.textureEnableMipmaps : importer.mipmapEnabled;
                if (!mipsOn || importer.streamingMipmaps) return false;
                AddChange(details, "Streaming mip maps", "Off", "On");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.StreamingMipmaps, "texture", path, "", "", "", KaleidoOptionUndo.Int("streaming", 0));
                    importer.streamingMipmaps = true;
                }
                return true;
            }
            if (window.textureDisableStreamingMipmaps && importer.streamingMipmaps)
            {
                AddChange(details, "Streaming mip maps", "On", "Off");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.StreamingMipmapsOff, "texture", path, "", "", "", KaleidoOptionUndo.Int("streaming", 1));
                    importer.streamingMipmaps = false;
                }
                return true;
            }
            return false;
        }

        private static bool ApplyTextureImporter(KaleidoVRCOptimizer window, string path, TextureImporter importer, bool write, List<string> details)
        {
            KaleidoTextureUsage row = FindTextureUsage(window, path);
            KaleidoTextureKind kind = row != null
                ? row.kind
                : KaleidoVRCOptimizerHelpers.ClassifyTexture(path, importer);
            bool normal = kind == KaleidoTextureKind.Normal;
            bool mask = kind == KaleidoTextureKind.Mask;
            bool albedo = kind == KaleidoTextureKind.Albedo;

            bool applySize;
            int pcSize;
            int questSize;
            GetTypeSizes(window, kind, out applySize, out pcSize, out questSize);
            if (row != null)
            {
                pcSize = GetPlannedRowSize(window, row, false);
                questSize = GetPlannedRowSize(window, row, true);
            }
            bool ignorePc = row != null && row.ignorePc;
            bool ignoreQuest = row != null && row.ignoreQuest;
            bool customPc = row != null && row.usedCustomPc;
            bool customQuest = row != null && row.usedCustomQuest;
            bool applyPcSize = customPc || (!ignorePc && applySize);
            bool applyQuestSize = customQuest || (!ignoreQuest && applySize);
            bool questWorkspace = window.IsQuestWorkspace;
            bool writePcSize = applyPcSize && AllowMaxSizeWrite(CurrentPlatformMaxSize(importer, false), pcSize, customPc);
            bool writeQuestSize = applyQuestSize && AllowMaxSizeWrite(CurrentPlatformMaxSize(importer, true), questSize, customQuest);

            bool dirty = false;
            bool normalHq = normal && window.higherQualityNormalMaps;

            if (questWorkspace)
            {
                TextureImporterFormat androidFormat = ToAndroidFormat(window.androidTexFormat, normalHq);
                string sizeId = applySize ? KaleidoOptionUndo.SizeOptionId(kind) : null;
                string formatId = window.applyAndroidTexFormat ? KaleidoOptionUndo.AndroidFormat : null;
                string hqId = normalHq && window.applyAndroidTexFormat ? KaleidoOptionUndo.HigherQualityNormals : null;
                if (ApplyPlatform(importer, "Android", path, writeQuestSize, questSize, sizeId, window.applyAndroidTexFormat, androidFormat, TextureImporterCompression.Compressed, formatId, hqId, false, null, write, details))
                    dirty = true;
                if (ApplyStreamingMipmaps(window, importer, path, write, details)) dirty = true;
                return dirty;
            }

            if (window.autoDetectNormalMaps && normal && importer.textureType != TextureImporterType.NormalMap)
            {
                AddChange(details, "Texture type", importer.textureType.ToString(), TextureImporterType.NormalMap.ToString());
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.DetectNormals, "texture", path, "", "", "", KaleidoOptionUndo.Int("texType", (int)importer.textureType));
                    importer.textureType = TextureImporterType.NormalMap;
                }
                dirty = true;
            }

            if (window.textureEnableReadWrite && !importer.isReadable)
            {
                AddChange(details, "Read / Write", "Off", "On");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.TextureReadWriteOn, "texture", path, "", "", "", KaleidoOptionUndo.Int("readable", 0));
                    importer.isReadable = true;
                }
                dirty = true;
            }
            else if (window.textureDisableReadWrite && importer.isReadable)
            {
                AddChange(details, "Read / Write", "On", "Off");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.TextureReadWrite, "texture", path, "", "", "", KaleidoOptionUndo.Int("readable", 1));
                    importer.isReadable = false;
                }
                dirty = true;
            }

            if (window.textureApplyMipmaps && importer.mipmapEnabled != window.textureEnableMipmaps)
            {
                AddChange(details, "Mip maps", FlagWord(importer.mipmapEnabled), FlagWord(window.textureEnableMipmaps));
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.TextureMipmaps, "texture", path, "", "", "", KaleidoOptionUndo.Int("mipmaps", importer.mipmapEnabled ? 1 : 0));
                    importer.mipmapEnabled = window.textureEnableMipmaps;
                }
                dirty = true;
            }

            if (ApplyStreamingMipmaps(window, importer, path, write, details)) dirty = true;

            if (window.textureApplyAniso && importer.anisoLevel != window.textureAniso)
            {
                AddChange(details, "Aniso", importer.anisoLevel.ToString(), window.textureAniso.ToString());
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.TextureAniso, "texture", path, "", "", "", KaleidoOptionUndo.Int("aniso", importer.anisoLevel));
                    importer.anisoLevel = window.textureAniso;
                }
                dirty = true;
            }

            int crunchQuality = GetPlannedCrunchQuality(window, row);
            if (window.textureEnableCrunch && (!importer.crunchedCompression || importer.compressionQuality != crunchQuality))
            {
                string from = importer.crunchedCompression ? "On (" + importer.compressionQuality + "%)" : "Off";
                AddChange(details, "Crunch", from, "On (" + crunchQuality + "%)");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.TextureCrunchOn, "texture", path, "", "", "importer", KaleidoOptionUndo.Join(
                        KaleidoOptionUndo.Int("crunch", importer.crunchedCompression ? 1 : 0),
                        KaleidoOptionUndo.Int("crunchQ", importer.compressionQuality)));
                    importer.crunchedCompression = true;
                    importer.compressionQuality = crunchQuality;
                }
                dirty = true;
            }
            else if (window.textureDisableCrunch && !window.textureEnableCrunch && importer.crunchedCompression)
            {
                AddChange(details, "Crunch", "On", "Off");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.TextureCrunchOff, "texture", path, "", "", "importer", KaleidoOptionUndo.Int("crunch", 1));
                    importer.crunchedCompression = false;
                }
                dirty = true;
            }

            TextureImporterCompression wantedCompression = window.pcTexFormat == KaleidoPcTexFormat.HighQuality
                ? TextureImporterCompression.CompressedHQ
                : TextureImporterCompression.Compressed;
            if (window.applyPcTexFormat && importer.textureCompression != wantedCompression)
            {
                AddChange(details, "Compression", importer.textureCompression.ToString(), wantedCompression.ToString());
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.PcFormat, "texture", path, "", "", "importer", KaleidoOptionUndo.Int("compression", (int)importer.textureCompression));
                    importer.textureCompression = wantedCompression;
                }
                dirty = true;
            }

            if (window.autoLinearMaskMaps && mask && importer.sRGBTexture)
            {
                AddChange(details, "Mask color", "sRGB", "Linear");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.LinearMasks, "texture", path, "", "", "", KaleidoOptionUndo.Int("srgb", 1));
                    importer.sRGBTexture = false;
                }
                dirty = true;
            }
            else if (window.autoSrgbMaskMaps && mask && !importer.sRGBTexture)
            {
                AddChange(details, "Mask color", "Linear", "sRGB");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.LinearMasksOff, "texture", path, "", "", "", KaleidoOptionUndo.Int("srgb", 0));
                    importer.sRGBTexture = true;
                }
                dirty = true;
            }

            if (window.alphaIsTransparencyOnAlbedo && albedo && importer.DoesSourceTextureHaveAlpha() && !importer.alphaIsTransparency)
            {
                AddChange(details, "Alpha Is Transparency", "Off", "On");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.AlphaIsTransparency, "texture", path, "", "", "", KaleidoOptionUndo.Int("alpha", 0));
                    importer.alphaIsTransparency = true;
                }
                dirty = true;
            }
            else if (window.alphaIsTransparencyOffAlbedo && albedo && importer.alphaIsTransparency)
            {
                AddChange(details, "Alpha Is Transparency", "On", "Off");
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.AlphaIsTransparencyOff, "texture", path, "", "", "", KaleidoOptionUndo.Int("alpha", 1));
                    importer.alphaIsTransparency = false;
                }
                dirty = true;
            }

            if (writePcSize && importer.maxTextureSize != pcSize)
            {
                AddChange(details, "Max size", importer.maxTextureSize.ToString(), pcSize.ToString());
                if (write)
                {
                    if (applySize)
                        KaleidoOptionUndo.Capture(KaleidoOptionUndo.SizeOptionId(kind), "texture", path, "", "", "default", KaleidoOptionUndo.Int("max", importer.maxTextureSize));
                    importer.maxTextureSize = pcSize;
                }
                dirty = true;
            }

            TextureImporterFormat pcFormat = ToPcFormat(window.pcTexFormat, normalHq, importer, normal);
            string pcSizeId = applySize ? KaleidoOptionUndo.SizeOptionId(kind) : null;
            string pcFormatId = window.applyPcTexFormat ? KaleidoOptionUndo.PcFormat : null;
            string pcHqId = normalHq && window.applyPcTexFormat ? KaleidoOptionUndo.HigherQualityNormals : null;
            string crunchId = window.textureDisableCrunch && !window.textureEnableCrunch ? KaleidoOptionUndo.TextureCrunchOff : null;
            if (ApplyPlatform(importer, "Standalone", path, writePcSize, pcSize, pcSizeId, window.applyPcTexFormat, pcFormat, wantedCompression, pcFormatId, pcHqId, window.textureDisableCrunch && !window.textureEnableCrunch, crunchId, write, details))
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
            string assetPath,
            bool applySize,
            int maxSize,
            string sizeOptionId,
            bool applyFormat,
            TextureImporterFormat format,
            TextureImporterCompression compression,
            string formatOptionId,
            string hqOptionId,
            bool disableCrunch,
            string crunchOptionId,
            bool write,
            List<string> details)
        {
            if (!applySize && !applyFormat && !disableCrunch) return false;

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            bool sizeChanges = applySize && (!settings.overridden || settings.maxTextureSize != maxSize);
            bool formatChanges = applyFormat && (!settings.overridden || settings.format != format || settings.textureCompression != compression);
            bool crunchChanges = disableCrunch && settings.crunchedCompression;
            if (!sizeChanges && !formatChanges && !crunchChanges) return false;

            string label = platform == "Android" ? "Quest" : "PC";
            if (sizeChanges) AddChange(details, "Max size (" + label + ")", settings.overridden ? settings.maxTextureSize.ToString() : importer.maxTextureSize.ToString(), maxSize.ToString());
            if (formatChanges) AddChange(details, "Format (" + label + ")", settings.overridden ? settings.format.ToString() : "Default", format.ToString());
            if (crunchChanges) AddChange(details, "Crunch (" + label + ")", "On", "Off");
            if (!write) return true;

            string slot = platform == "Android" ? "android" : "pc";
            if (sizeChanges && !string.IsNullOrEmpty(sizeOptionId))
            {
                KaleidoOptionUndo.Capture(sizeOptionId, "texture", assetPath, "", "", slot, KaleidoOptionUndo.Join(
                    KaleidoOptionUndo.Int("platOver", settings.overridden ? 1 : 0),
                    KaleidoOptionUndo.Int("platMax", settings.maxTextureSize)));
            }
            if (formatChanges && !string.IsNullOrEmpty(formatOptionId))
            {
                KaleidoOptionUndo.Capture(formatOptionId, "texture", assetPath, "", "", slot, KaleidoOptionUndo.Join(
                    KaleidoOptionUndo.Int("platOver", settings.overridden ? 1 : 0),
                    KaleidoOptionUndo.Int("platFmt", (int)settings.format),
                    KaleidoOptionUndo.Int("platComp", (int)settings.textureCompression)));
            }
            if (formatChanges && !string.IsNullOrEmpty(hqOptionId))
            {
                KaleidoOptionUndo.Capture(hqOptionId, "texture", assetPath, "", "", slot, KaleidoOptionUndo.Join(
                    KaleidoOptionUndo.Int("platOver", settings.overridden ? 1 : 0),
                    KaleidoOptionUndo.Int("platFmt", (int)settings.format),
                    KaleidoOptionUndo.Int("platComp", (int)settings.textureCompression)));
            }
            if (crunchChanges && !string.IsNullOrEmpty(crunchOptionId))
            {
                KaleidoOptionUndo.Capture(crunchOptionId, "texture", assetPath, "", "", slot, KaleidoOptionUndo.Join(
                    KaleidoOptionUndo.Int("platOver", settings.overridden ? 1 : 0),
                    KaleidoOptionUndo.Int("platCrunch", 1)));
            }

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
                    if (write)
                    {
                        KaleidoOptionUndo.Capture(KaleidoOptionUndo.MeshReadWrite, "model", path, "", "", "", KaleidoOptionUndo.Int("readable", 0));
                        importer.isReadable = true;
                    }
                    dirty = true;
                }
                if (window.applySkinWeights && importer.skinWeights != ModelImporterSkinWeights.Standard)
                {
                    if (write)
                    {
                        KaleidoOptionUndo.Capture(KaleidoOptionUndo.SkinWeights, "model", path, "", "", "", KaleidoOptionUndo.Join(
                            KaleidoOptionUndo.Int("skin", (int)importer.skinWeights),
                            KaleidoOptionUndo.Int("maxBones", importer.maxBonesPerVertex)));
                        importer.skinWeights = ModelImporterSkinWeights.Standard;
                    }
                    dirty = true;
                }
                return dirty;
            }

            if (window.applyMeshCompression)
            {
                ModelImporterMeshCompression compression = ToMeshCompression(window.meshCompression);
                if (importer.meshCompression != compression)
                {
                    if (write)
                    {
                        KaleidoOptionUndo.Capture(KaleidoOptionUndo.MeshCompression, "model", path, "", "", "", KaleidoOptionUndo.Int("meshComp", (int)importer.meshCompression));
                        importer.meshCompression = compression;
                    }
                    dirty = true;
                }
            }
            if (window.meshEnableReadWrite && !importer.isReadable)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.MeshReadWrite, "model", path, "", "", "", KaleidoOptionUndo.Int("readable", 0));
                    importer.isReadable = true;
                }
                dirty = true;
            }
            if (window.meshOptimizePolygons && !importer.optimizeMeshPolygons)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.OptimizePolygons, "model", path, "", "", "", KaleidoOptionUndo.Int("optPoly", 0));
                    importer.optimizeMeshPolygons = true;
                }
                dirty = true;
            }
            if (window.meshOptimizeVertices && !importer.optimizeMeshVertices)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.OptimizeVertices, "model", path, "", "", "", KaleidoOptionUndo.Int("optVert", 0));
                    importer.optimizeMeshVertices = true;
                }
                dirty = true;
            }
            if (window.meshWeldVertices && !importer.weldVertices)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.WeldVertices, "model", path, "", "", "", KaleidoOptionUndo.Int("weld", 0));
                    importer.weldVertices = true;
                }
                dirty = true;
            }
            if (window.meshStripBlendShapes && importer.importBlendShapes)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.StripBlendShapes, "model", path, "", "", "", KaleidoOptionUndo.Int("blend", 1));
                    importer.importBlendShapes = false;
                }
                dirty = true;
            }
            else if ((window.meshRestoreBlendShapes || window.meshKeepBlendShapes) && !importer.importBlendShapes)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(window.meshRestoreBlendShapes ? KaleidoOptionUndo.RestoreBlendShapes : KaleidoOptionUndo.KeepBlendShapes, "model", path, "", "", "", KaleidoOptionUndo.Int("blend", 0));
                    importer.importBlendShapes = true;
                }
                dirty = true;
            }
            if (window.meshDisableQuads && importer.keepQuads)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.DisableQuads, "model", path, "", "", "", KaleidoOptionUndo.Int("quads", 1));
                    importer.keepQuads = false;
                }
                dirty = true;
            }
            if (window.meshDisableLightmapUVs && importer.generateSecondaryUV)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.DisableLightmapUvs, "model", path, "", "", "", KaleidoOptionUndo.Int("uv2", 1));
                    importer.generateSecondaryUV = false;
                }
                dirty = true;
            }
            if (window.meshDisableImportLightsCameras && (importer.importLights || importer.importCameras))
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.SkipLightsCameras, "model", path, "", "", "", KaleidoOptionUndo.Join(
                        KaleidoOptionUndo.Int("lights", importer.importLights ? 1 : 0),
                        KaleidoOptionUndo.Int("cams", importer.importCameras ? 1 : 0)));
                    importer.importLights = false;
                    importer.importCameras = false;
                }
                dirty = true;
            }
            if (window.meshOptimizeAnimation && importer.animationCompression != ModelImporterAnimationCompression.Optimal)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.OptimizeAnimation, "model", path, "", "", "", KaleidoOptionUndo.Int("animComp", (int)importer.animationCompression));
                    importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                }
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
                            KaleidoOptionUndo.Capture(KaleidoOptionUndo.SkinWeights, "model", path, "", "", "", KaleidoOptionUndo.Join(
                                KaleidoOptionUndo.Int("skin", (int)importer.skinWeights),
                                KaleidoOptionUndo.Int("maxBones", importer.maxBonesPerVertex)));
                            importer.skinWeights = ModelImporterSkinWeights.Custom;
                            importer.maxBonesPerVertex = 255;
                        }
                        dirty = true;
                    }
                }
                else if (importer.skinWeights != ModelImporterSkinWeights.Standard)
                {
                    if (write)
                    {
                        KaleidoOptionUndo.Capture(KaleidoOptionUndo.SkinWeights, "model", path, "", "", "", KaleidoOptionUndo.Join(
                            KaleidoOptionUndo.Int("skin", (int)importer.skinWeights),
                            KaleidoOptionUndo.Int("maxBones", importer.maxBonesPerVertex)));
                        importer.skinWeights = ModelImporterSkinWeights.Standard;
                    }
                    dirty = true;
                }
            }

            if (window.meshForceHumanoid && importer.animationType != ModelImporterAnimationType.Human)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.ForceHumanoid, "model", path, "", "", "", KaleidoOptionUndo.Join(
                        KaleidoOptionUndo.Int("animType", (int)importer.animationType),
                        KaleidoOptionUndo.Int("avatarSetup", (int)importer.avatarSetup)));
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
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.AudioMono, "audio", path, "", "", "", KaleidoOptionUndo.Int("mono", 0));
                    importer.forceToMono = true;
                }
                dirty = true;
            }
            else if (window.audioForceToStereo && importer.forceToMono)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.AudioStereo, "audio", path, "", "", "", KaleidoOptionUndo.Int("mono", 1));
                    importer.forceToMono = false;
                }
                dirty = true;
            }
            if (window.audioLoadInBackground && !importer.loadInBackground)
            {
                if (write)
                {
                    KaleidoOptionUndo.Capture(KaleidoOptionUndo.AudioLoadBackground, "audio", path, "", "", "", KaleidoOptionUndo.Int("loadBg", 0));
                    importer.loadInBackground = true;
                }
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
                        KaleidoOptionUndo.Capture(KaleidoOptionUndo.AudioVorbis, "audio", path, "", "", "", KaleidoOptionUndo.Join(
                            KaleidoOptionUndo.Int("fmt", (int)sample.compressionFormat),
                            KaleidoOptionUndo.Int("load", (int)sample.loadType),
                            KaleidoOptionUndo.Float("quality", sample.quality)));
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
            List<string> logEntries,
            string undoAssetPath)
        {
            if (root == null) return;

            if (window.optimizeRenderers)
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (ApplyRenderer(window, renderer, write, root, undoAssetPath))
                    {
                        string line = "Renderer: " + GetPath(renderer.transform);
                        report.planned.Add(line);
                        logEntries.Add(line);
                    }
                }
            }

            ApplySkinnedBoundsFix(window, root, write, report, logEntries, undoAssetPath);

            if (!window.IsQuestWorkspace && window.optimizeAnimators && window.animatorCullWhenOffscreen)
            {
                foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
                {
                    if (animator.cullingMode == AnimatorCullingMode.CullUpdateTransforms) continue;
                    string line = "Animator cull: " + GetPath(animator.transform);
                    report.planned.Add(line);
                    logEntries.Add(line);
                    if (write)
                    {
                        RememberComponent(KaleidoOptionUndo.AnimatorCull, "animator", undoAssetPath, root, animator.transform, KaleidoOptionUndo.Int("cull", (int)animator.cullingMode));
                        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                        EditorUtility.SetDirty(animator);
                    }
                }
            }

            if (window.optimizeSceneExtras)
            {
                if (window.enableLightsOnAvatar || window.disableLightsOnAvatar)
                {
                    foreach (Light light in root.GetComponentsInChildren<Light>(true))
                    {
                        bool next = window.enableLightsOnAvatar;
                        if (light.enabled == next) continue;
                        string line = (next ? "Enable light: " : "Disable light: ") + GetPath(light.transform);
                        report.planned.Add(line);
                        logEntries.Add(line);
                        if (write)
                        {
                            RememberComponent(next ? KaleidoOptionUndo.EnableLights : KaleidoOptionUndo.DisableLights, "light", undoAssetPath, root, light.transform, KaleidoOptionUndo.Int("enabled", light.enabled ? 1 : 0));
                            light.enabled = next;
                            EditorUtility.SetDirty(light);
                        }
                    }
                }

                if (window.IsQuestWorkspace && (window.enableCamerasOnAvatar || window.disableCamerasOnAvatar))
                {
                    foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        bool next = window.enableCamerasOnAvatar;
                        if (camera.enabled == next) continue;
                        string line = (next ? "Enable camera: " : "Disable camera: ") + GetPath(camera.transform);
                        report.planned.Add(line);
                        logEntries.Add(line);
                        if (write)
                        {
                            RememberComponent(next ? KaleidoOptionUndo.EnableCameras : KaleidoOptionUndo.DisableCameras, "camera", undoAssetPath, root, camera.transform, KaleidoOptionUndo.Int("enabled", camera.enabled ? 1 : 0));
                            camera.enabled = next;
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
                            RememberComponent(KaleidoOptionUndo.Particles, "particle", undoAssetPath, root, particle.transform, KaleidoOptionUndo.Join(
                                KaleidoOptionUndo.Int("shadows", (int)particle.shadowCastingMode),
                                KaleidoOptionUndo.Int("motion", (int)particle.motionVectorGenerationMode)));
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

        private static bool ApplyRenderer(KaleidoVRCOptimizer window, Renderer renderer, bool write, GameObject undoRoot, string undoAssetPath)
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
            }

            if (!dirty) return false;
            if (!write) return true;

            if (quest && window.rendererDisableShadows)
            {
                RememberComponent(KaleidoOptionUndo.DisableShadows, "renderer", undoAssetPath, undoRoot, renderer.transform, KaleidoOptionUndo.Int("shadows", (int)renderer.shadowCastingMode));
                renderer.shadowCastingMode = shadow;
            }
            if (!quest && window.rendererDisableReceiveShadows)
            {
                RememberComponent(KaleidoOptionUndo.DisableReceiveShadows, "renderer", undoAssetPath, undoRoot, renderer.transform, KaleidoOptionUndo.Int("recv", renderer.receiveShadows ? 1 : 0));
                renderer.receiveShadows = false;
            }
            if (!quest && window.rendererDisableProbes)
            {
                RememberComponent(KaleidoOptionUndo.DisableProbes, "renderer", undoAssetPath, undoRoot, renderer.transform, KaleidoOptionUndo.Join(
                    KaleidoOptionUndo.Int("probes", (int)renderer.lightProbeUsage),
                    KaleidoOptionUndo.Int("reflect", (int)renderer.reflectionProbeUsage)));
                renderer.lightProbeUsage = probes;
                renderer.reflectionProbeUsage = reflections;
            }
            if (!quest && window.rendererDisableMotionVectors)
            {
                RememberComponent(KaleidoOptionUndo.DisableMotionVectors, "renderer", undoAssetPath, undoRoot, renderer.transform, KaleidoOptionUndo.Int("motion", (int)renderer.motionVectorGenerationMode));
                renderer.motionVectorGenerationMode = motion;
            }

            if (skinned != null)
            {
                if (!quest && window.rendererDisableUpdateWhenOffscreen)
                {
                    RememberComponent(KaleidoOptionUndo.DisableUpdateOffscreen, "renderer", undoAssetPath, undoRoot, skinned.transform, KaleidoOptionUndo.Int("uwo", skinned.updateWhenOffscreen ? 1 : 0));
                    skinned.updateWhenOffscreen = false;
                }
                if (quest && window.rendererForceBone4)
                {
                    RememberComponent(KaleidoOptionUndo.ForceBone4, "renderer", undoAssetPath, undoRoot, skinned.transform, KaleidoOptionUndo.Int("quality", (int)skinned.quality));
                    skinned.quality = SkinQuality.Bone4;
                }
            }

            EditorUtility.SetDirty(renderer);
            return true;
        }

        private static void RememberComponent(string optionId, string kind, string undoAssetPath, GameObject root, Transform target, string data)
        {
            if (root == null || target == null || string.IsNullOrEmpty(data)) return;
            string hierarchy = string.IsNullOrEmpty(undoAssetPath)
                ? KaleidoOptionUndo.FullPath(target)
                : KaleidoOptionUndo.RelativePath(root.transform, target);
            string scene = string.IsNullOrEmpty(undoAssetPath) && root.scene.IsValid() ? root.scene.path : "";
            KaleidoOptionUndo.Capture(optionId, kind, undoAssetPath ?? "", hierarchy, scene, "", data);
        }

        private static readonly Vector3 SkinnedBoundsCube = new Vector3(2f, 2f, 2f);

        private static void ApplySkinnedBoundsFix(
            KaleidoVRCOptimizer window,
            GameObject root,
            bool write,
            KaleidoOptimizerReport report,
            List<string> logEntries,
            string undoAssetPath)
        {
            if (window.IsQuestWorkspace || !window.rendererRecalculateBounds) return;

            Vector3 modelCenter = GetModelCenter(root);
            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh == null) continue;
                if (UsesLightedHapticMaterial(skinned)) continue;

                Bounds planned = PlannedSkinnedBounds(skinned, modelCenter);
                bool boundsChanged = !BoundsApproximatelyEqual(skinned.localBounds, planned);
                bool offscreenChanged = skinned.updateWhenOffscreen;
                if (!boundsChanged && !offscreenChanged) continue;

                string line = "Skinned bounds: " + GetPath(skinned.transform);
                report.planned.Add(line);
                logEntries.Add(line);
                if (!write) continue;

                Bounds previous = skinned.localBounds;
                RememberComponent(KaleidoOptionUndo.RecalculateBounds, "renderer", undoAssetPath, root, skinned.transform, KaleidoOptionUndo.Join(
                    KaleidoOptionUndo.Float("cx", previous.center.x),
                    KaleidoOptionUndo.Float("cy", previous.center.y),
                    KaleidoOptionUndo.Float("cz", previous.center.z),
                    KaleidoOptionUndo.Float("sx", previous.size.x),
                    KaleidoOptionUndo.Float("sy", previous.size.y),
                    KaleidoOptionUndo.Float("sz", previous.size.z),
                    KaleidoOptionUndo.Int("uwo", skinned.updateWhenOffscreen ? 1 : 0)));
                skinned.updateWhenOffscreen = false;
                skinned.localBounds = planned;
                EditorUtility.SetDirty(skinned);
            }
        }

        private static Bounds PlannedSkinnedBounds(SkinnedMeshRenderer skinned, Vector3 modelCenter)
        {
            Transform rootBone = skinned.rootBone != null ? skinned.rootBone : skinned.transform;
            Vector3 localCenter = rootBone.InverseTransformPoint(modelCenter);
            localCenter.y = 0f;
            return new Bounds(localCenter, SkinnedBoundsCube);
        }

        private static float GetModelHeight(GameObject root)
        {
            Animator animator = FindAvatarAnimator(root);
            if (animator != null && animator.isHuman)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                    return Mathf.Max(0f, head.position.y - root.transform.position.y);
            }

            float minY = root.transform.position.y;
            float maxY = minY;
            bool any = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Bounds world = renderer.bounds;
                if (!any)
                {
                    minY = world.min.y;
                    maxY = world.max.y;
                    any = true;
                }
                else
                {
                    if (world.min.y < minY) minY = world.min.y;
                    if (world.max.y > maxY) maxY = world.max.y;
                }
            }
            return any ? Mathf.Max(0f, maxY - minY) : 2f;
        }

        private static Vector3 GetModelCenter(GameObject root)
        {
            Animator animator = FindAvatarAnimator(root);
            if (animator != null && animator.isHuman)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null) return (root.transform.position + head.position) * 0.5f;
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null) return hips.position;
            }
            return root.transform.position + Vector3.up * (GetModelHeight(root) * 0.5f);
        }

        private static Animator FindAvatarAnimator(GameObject root)
        {
            Animator animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.GetComponentInChildren<Animator>();
            return animator;
        }

        private static bool UsesLightedHapticMaterial(Renderer renderer)
        {
            if (renderer.GetComponent<Light>() != null) return true;
            Material[] materials = renderer.sharedMaterials;
            if (materials == null) return false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || material.shader == null) continue;
                string shader = material.shader.name;
                if (shader.IndexOf("DPS", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (shader.IndexOf("TPS", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static bool BoundsApproximatelyEqual(Bounds a, Bounds b)
        {
            return VectorApproximatelyEqual(a.center, b.center) && VectorApproximatelyEqual(a.extents, b.extents);
        }

        private static bool VectorApproximatelyEqual(Vector3 a, Vector3 b)
        {
            const float tolerance = 0.0001f;
            return Mathf.Abs(a.x - b.x) <= tolerance
                && Mathf.Abs(a.y - b.y) <= tolerance
                && Mathf.Abs(a.z - b.z) <= tolerance;
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
