# KaleidoVR VRChat Model Optimizer

<p align="center">
  <img src="KaleidoVR/Editor/Icons/Kali_Logo.png" alt="KaleidoVR" width="300">
</p>

<p align="center">
  For the good of the instance — optimize your avatar.
</p>

A Unity editor tool for one VRChat avatar at a time. Drop the model on Setup. Scan and Rank show PC and Quest performance. Dry Run previews writes. Apply writes only the options you turned on.

PC and Quest are separate workspaces. Texture, mesh, and scene writes stay on the workspace you are in. Confirm, Fix, and Apply write into this project and stay on the assets. Scan only reports.

Profiles store tab recipes — built-in PC, Quest, Standard, and Everything, plus your own. Special stays off unless you tick it.

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
4. **Rank** — Scan or Dry Run. Shows VRChat limits plus VRAM, GrabPass, animator cost, Write Defaults, and texture flags. Some orange rows can be fixed here. Write Defaults and PC mesh Read/Write ask Confirm.
5. **Textures / Meshes / Scene** — enable only what you want. Ignore on a texture skips the type cap; Set still writes that row.
6. **On Upload** — tick Apply on upload to run mesh merge, unused cleanup, blend shapes, PhysBones, unused contacts, and optional FX on the upload copy. That choice follows this Unity editor, not the project. FX is off by default and can change gestures and face. Unused cleanup and merge leave what other components on this avatar still name or point at. Create an Optimized Copy to preview; do not edit it or upload it as the master.
7. **Special** — compression, Humanoid, Recalculate bounds, and other high-risk writes. Apply warns when those are not at their defaults.
8. Optional logs go to `Logs/KaleidoVR/Optimizer/`.

Blue **Scan** only looks. Orange **Dry Run** previews importer and scene writes. Green **Apply** writes into this Unity project and stays. Apply stays off until a Dry Run succeeds. Each workspace has its own Scan / Dry Run / Apply.

### Setup

One avatar slot. Dropping another replaces it. Contents Of Selected Model lists what that avatar uses versus what is packed in the FBX.

Ignore List leaves those objects and their dependent textures and meshes untouched, including on the upload copy.

Write Log File saves a timestamped report under `Logs/KaleidoVR/Optimizer`. Clear Log Cache deletes those files and leaves the folder.

Apply renderer changes to prefab assets writes Scene-tab renderer edits onto the `.prefab`, not only the scene instance.

### Profiles

Built-in **PC**, **Quest**, **Standard**, and **Everything**. Standard is the default. Built-ins never turn Special on.

Saved profiles live in this Unity editor. Tick which tabs a profile should overwrite when it loads. Unchecked tabs keep what you already set. Save as new, overwrite, delete, export selected, back up all, or import JSON. Import never overwrites a name you already have — it renames the incoming one.

### Rank

VRChat rank plus triangles, materials, meshes, textures, blend shapes, bones, animators, lights, audio, particles, PhysBones, colliders, contacts, and leftover Unity constraints.

When Apply on upload is on, Rank adds **Now / On Upload / Change**. On Upload is a hidden copy and does not write the scene. When other upload passes are in the project, that copy is assembled first so the numbers match VRChat upload. Texture VRAM stays on Now.

Fix and Confirm on this tab write into Unity right away. You do not need Apply after those.

- **VRAM** — texture and mesh memory, plus an estimate after Apply. Rank does not count most of this.
- **Hidden cost** — GrabPass, blendshape triangles, Any State transitions, animator layers, Write Defaults, empty states, mesh Read/Write. GrabPass and blendshape load are reported only.
- Write Defaults — On / Off / Ignore, then Confirm. Writes every animator state on this avatar. On Upload can set WD On or WD Off on the upload copy. Direct blend trees and additive layers stay on there. WD Off adds a rest-pose layer on FX.
- Empty animator states — Fix assigns the shared empty clip. On Upload can fill them on the upload copy, with that clip or one you pick.
- Mesh Read/Write — PC: On / Off / Ignore, then Confirm (writes the FBX). Quest: report only, shown in red. Quest upload is blocked while any mesh is off; Enable mesh Read / Write on On Upload copies those meshes readable on the clone only.
- **Texture flags** — crunched textures, normals that are not BC5, animation-swap textures, streaming mip maps off. Fix writes streaming on.
- Unity constraints — Fix runs the VRChat SDK converter so Play Mode matches what the client loads. Android disables leftover Unity constraints.

Orange means the row needs a look. Red is Quest only, when mesh Read/Write is off.

### Textures

Process texture importers is the master switch for this tab. Caps never raise a texture. Ignore on a row skips the type cap; Set still writes that texture by hand. Ignore and Set are remembered.

**Textures On This Model** lists Current and New size, Ignore, and (on PC with Crunch Enable) a per-row Crunch %. Raising a size asks Confirm. Click a thumbnail for Texture Preview and the materials that sample it.

**Max Sizes Only** runs the type caps and any row you set by hand. Compression, mip maps, meshes, scene, and Special are not touched.

**PC:** Crunch on/off and %, Reset all to 50, Read/Write, mip maps, streaming mip maps, aniso, detect normals by name, higher quality normals (BC5), linear masks, Alpha Is Transparency.

**Quest:** streaming mip maps, higher quality normals (ASTC). Compression format lives on Special.

### Meshes

Process model importers is the master switch for this tab.

**PC:** Enable mesh Read/Write starts off. Optimize polygons, optimize vertices, weld (off), keep blend shapes, quads off, lightmap UVs off, skip embedded lights/cameras, animation compression, skin weights.

**Quest:** Set skin weights to 4 bones. Enable mesh Read/Write is not on this tab.

### On Upload

Off until you tick Apply on upload. Skipped in Play Mode. Runs last, after other upload passes finish, and only once on that copy. Unsaved meshes already skinned to this avatar can be copied into Generated. Extras that still have their own armature stay.

**Meshes:** Quest Enable mesh Read/Write (readable copies on the clone; the FBX stays as it is). Merge skinned meshes that animate together. Merge identical material slots. Allow shuffling material slots. Disable Update When Offscreen (off). That one also stays on Scene for a project write.

**Blend shapes:** Remove unused blend shapes. Merge same-ratio blend shapes (off). MMD world compatibility.

**Cleanup:** Remove unused components (disabled ones no clip turns on; EditorOnly; audio sources stay). Remove unused GameObjects (off by default). Keep only weighted bones.

**PhysBones:** Disable PhysBones when unused.

**Contacts:** Remove unused contacts (on by default when Apply on upload is ticked). Disabled senders and receivers that never turn on. Enabled senders stay. Receivers whose parameter still goes into FX, menus, or Expression Parameters stay.

**Animator:** Write Defaults (off). WD On or WD Off. Direct blend trees and additive layers stay on. WD Off adds a rest-pose layer on FX. Empty animator states (off). Leave the clip empty to use the shared empty clip, or pick your own. Rank can still Confirm or Fix the project. Optimize FX layer (off by default, with a warning). Empty layers and missing curves only. Hand gesture clips stay. MMD keeps layers 0–2.

Dry Run On Upload, Create optimized copy in the scene, and Clear cache for `Assets/KaleidoVR/Generated`.

### Scene

**PC:** Update When Offscreen off, receive shadows off, probes off, motion vectors off, animator cull when offscreen, audio load in background, Vorbis, particle shadows / motion vectors off.

**Quest:** shadow casting off, 4 bone quality on skinned meshes. Offscreen, probes, particles, and audio stay in the PC workspace.

### Special

Enable writes that flag on. Disable writes it off. Neither leaves the avatar as-is. Apply only warns when a Special write is not at its default.

**PC:** set compression (Auto BC7/DXT1 and other blocks), mesh compression, Humanoid rig, blend-shape import on/off, recalculate skinned bounds (2 m cube), realtime lights on/off, force audio mono/stereo.

**Quest:** ASTC format, GPU instancing, realtime lights on/off, cameras on/off.

## Credits

This is my tool. I started it years ago. I sat on it through a lot of life, including a house fire. I never forgot it. I just was not in a place to finish it. When I got my head back on straight, I did.

I wrote it. My name is on it. It is not done by AI. Anyone who says otherwise has been glued to their Reddit keyboard too long. When the commits went up is not when the work was done.

Created and maintained by KaleidoVR.
KaleidoVR@hotmail.com

- [kalivr.com](https://kalivr.com)
- [Discord](https://discord.com/invite/cRsufJssTA)

## Copyright

Copyright (c) 2026 KaleidoVR. All rights reserved.
