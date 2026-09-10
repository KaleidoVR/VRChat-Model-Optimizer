# KaleidoVR VRChat Model Optimizer

<p align="center">
  <img src="Unity VRChat Optimizer Tool/Editor/Icons/Kali_Logo.png" alt="KaleidoVR" width="300">
</p>

A Unity editor tool that scans a VRChat avatar and applies optional texture, mesh, renderer, audio, and animator optimizations. Every option can be left off. Built-in PC, Quest, and Dual profiles are included, plus user-saved profiles.

Drop a `VRCAvatarDescriptor` avatar (or a skinned character FBX/VRM/GLB) into the window. The tool lists only assets inside that model, then dry-runs or applies the settings you enable.

- Unity **2022.3.22f1** or newer, including Unity 6 (6000.x)
- VRChat SDK3 Avatars is optional (needed for descriptor-based targeting and PhysBone / contact counts)

## Install

1. Copy the `Unity VRChat Optimizer Tool` folder (including its `.meta` files) into your Unity project, typically under `Assets/`.
2. Wait for Unity to compile.
3. Open the tool from the menu bar: **KaleidoVR > VRChat Model Optimizer**.

It appears in the same **KaleidoVR** menu as Asset Organizer.

## Usage

1. Drag the avatar into the Setup tab. Worlds, scenes, folders, and loose textures/materials are rejected.
2. Review **Contents Of Selected Model**. Scan and Apply only use that set.
3. Pick a profile (PC, Quest, Dual, or a saved profile) and enable only the categories you want.
4. Leave **Dry Run** on to preview. Turn it off to write importer and scene changes.
5. Optional logs go to `Logs/KaleidoVR/Optimizer/`.

## Credits

Created and maintained by **KaleidoVR**.

- [kalivr.com](https://kalivr.com)
- [Discord](https://discord.com/invite/cRsufJssTA)

## License

[MIT](LICENSE) — Copyright (c) 2026 KaleidoVR
