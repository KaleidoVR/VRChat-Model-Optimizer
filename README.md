# KaleidoVR VRChat Model Optimizer

<p align="center">
  <img src="KaleidoVR/Editor/Icons/Kali_Logo.png" alt="KaleidoVR" width="300">
</p>

A Unity editor tool that scans a VRChat avatar and applies optional texture, mesh, renderer, audio, and animator optimizations. Every option can be left off. Built-in PC, Quest, Standard, and Everything profiles are included, plus user-saved profiles.

Drop a `VRCAvatarDescriptor` avatar (or a skinned character FBX/VRM/GLB) into the window. The tool lists only assets inside that model, then dry-runs or applies the settings you enable.

- Unity **2022.3.22f1** or newer, including Unity 6 (6000.x)
- VRChat SDK3 Avatars is optional (needed for descriptor-based targeting and PhysBone / contact counts)

<p align="center">
<img width="660" height="1221" alt="1" src="https://github.com/user-attachments/assets/c05bbb0d-38a2-48f6-8f95-934e23b11002" />
</p>

KaleidoVR Unity editor tools live in `KaleidoVR/Editor/` in source and install to `Assets/KaleidoVR/Editor/`.

## Install

1. Download the latest `.unitypackage` from [Releases](https://github.com/KaleidoVR/VRChat-Model-Optimizer/releases).
2. In Unity, choose **Assets > Import Package > Custom Package...** and select the file.
3. Import everything. Files land in `Assets/KaleidoVR/Editor/`.
4. Open the tool from the menu bar: **KaleidoVR > VRChat Model Optimizer**.

To install from source instead, copy the `KaleidoVR` folder (including its `.meta` files) into `Assets/` in your project.

It appears in the same **KaleidoVR** menu as Asset Organizer.

## Usage

1. Drag the avatar into the Setup tab. Worlds, scenes, folders, and loose textures/materials are rejected.
2. Review **Contents Of Selected Model**. Scan and Apply only use that set.
3. Pick a profile (PC, Quest, Standard, Everything, or a saved profile) and enable only the categories you want.
4. **Scan** or **Dry Run** fills Rank (VRChat limits, VRAM, GrabPass, animator, texture flags). Dry Run previews writes; Apply writes only after that dry run.
5. Optional logs go to `Logs/KaleidoVR/Optimizer/`.

## Credits

Created and maintained by **KaleidoVR**.

- [kalivr.com](https://kalivr.com)
- [Discord](https://discord.com/invite/cRsufJssTA)

## Copyright

Copyright (c) 2026 KaleidoVR. All rights reserved.
