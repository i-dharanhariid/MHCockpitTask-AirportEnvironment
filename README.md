# Airport Environment: Evening (Unity 6 / URP)

Trailer: https://youtu.be/fHZIuCogd24

Walkthrough: https://youtu.be/LYziugdY5h4

File: https://github.com/i-dharanhariid/MHCockpitTask-AirportEnvironment

A real-time airport environment built for Unity 6.3 (6000.3.15f1) with the Universal Render Pipeline.
It is set at dusk and has a full airport: a multi-level terminal, runway and taxiway, apron, hangars,
a toll plaza and hospital, and surrounding fencing and grounds.

## Highlights
- **Evening lighting:** a real-time sun plus runway, taxiway, apron, street and stadium lights.
- **Baked lighting:** the interior ceiling and fill lights are lightmapped, with light probes for moving objects. Only a few real-time lights are left.
- **Life in the scene:** animated planes taking off and taxiing, baggage train, traffic with toll plazas, pedestrians and staff on NavMesh, working escalators.
- **Optimisation:** combined meshes, consolidated materials, unused assets removed, and camera-based culling of distant lights and NPC animation.
- **Two scenes:** `Airport2` is the first-person walkthrough, and `Airport2_Trailer` is the cinematic camera version.

## Getting started
1. Use Unity **6000.3.15f1** with the 3D (URP) template.
2. Install [Git LFS](https://git-lfs.com) **before cloning**, since the large models and lightmaps are stored with it.
3. Open the project and load `Assets/Scenes/Airport2.unity`.
4. Press Play. Move with **WASD** and look with the mouse.

Required packages (already listed in `Packages/manifest.json`): Cinemachine 3.1.7, AI Navigation 2.0.12,
Input System 1.19.0, Universal RP 17.3.0.

## Credits
Models come from Sketchfab, CGTrader and the Unity Asset Store, modified in Blender. Full attribution and licenses are in
[`Assets/Credits.txt`](Assets/Credits.txt).
