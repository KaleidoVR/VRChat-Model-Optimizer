# KaleidoVR VRChat Model Optimizer

<p align="center">
  <img src="KaleidoVR/Editor/Icons/Kali_Logo.png" alt="KaleidoVR" width="300">
</p>

<p align="center">
  For the good of the instance — optimize your avatar.
</p>

A Unity editor tool for one VRChat avatar at a time. Drop the model on Setup. Scan and Rank show PC and Quest performance. Dry Run previews writes. Apply writes only the options you turned on.

PC and Quest are separate workspaces. Texture, mesh, and scene writes stay on the workspace you are in. Profiles store tab recipes — built-in PC, Quest, Standard, and Everything, plus your own. Special stays off unless you tick it.

On Upload is separate and off by default. Scan, Dry Run, and Apply do not run it. Apply on upload follows this Unity editor, not the project. When it is on, those options run on the assembled VRChat upload copy only. The scene and source assets stay as they are. Blend shapes, objects, bones, PhysBones, contacts, slots, and meshes that other components still name or point at stay. Create an Optimized Copy in the scene to check that pass first. After a successful upload, Generated cache is cleared, that copy is removed, and the original is turned back on.

- Unity **2022.3.22f1** or newer, including Unity 6 (6000.x)
- VRChat SDK3 Avatars is optional (needed for descriptor targeting, PhysBone / contact counts, constraint conversion, and On Upload)

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

1. Drag the avatar into **Setup**. Worlds, scenes, folders, and loose textures/materials are rejected. The Ignore List leaves objects and their assets untouched.
2. Review **Contents Of Selected Model**. Scan and Apply only use that set.
3. **Profiles** — pick a built-in or a saved recipe, and tick which tabs it should change.
4. **Rank** — Scan or Dry Run. Shows VRChat limits plus VRAM, GrabPass, animator cost, Write Defaults, and texture flags. Some orange rows can be fixed here. Write Defaults asks Confirm.
5. **Textures / Meshes / Scene** — enable only what you want. Ignore on a texture skips the type cap; Set still writes that row.
6. **On Upload** — tick Apply on upload to run mesh merge, unused cleanup, blend shapes, PhysBones, unused contacts, and optional FX on the upload copy. That choice follows this Unity editor, not the project. FX is off by default and can change gestures and face. Unused cleanup and merge leave what other components on this avatar still name or point at. Create an Optimized Copy to preview; do not edit it or upload it as the master.
7. **Special** — compression, Humanoid, Recalculate bounds, and other high-risk writes. Apply warns when those are not at their defaults.
8. Optional logs go to `Logs/KaleidoVR/Optimizer/`.

## Credits

This is my tool. I started it years ago. I sat on it through a lot of life, including a house fire. I never forgot it. I just was not in a place to finish it. When I got my head back on straight, I did.

I wrote it. My name is on it. It is not done by AI. Anyone who says otherwise has been glued to their Reddit keyboard too long. When the commits went up is not when the work was done.

Created and maintained by KaleidoVR.
KaleidoVR@hotmail.com

- [kalivr.com](https://kalivr.com)
- [Discord](https://discord.com/invite/cRsufJssTA)

## Copyright

Copyright (c) 2026 KaleidoVR. All rights reserved.
