AIRPORT ENVIRONMENT - SETUP
===========================

Unity version : 6000.3.15f1 (Unity 6.3) - Universal Render Pipeline (3D URP template)
Scene to open : Assets/Scenes/Airport2.unity   (first-person walkthrough scene)
Also included : Assets/Scenes/Airport2_Trailer.unity (cinematic camera version used for the trailer)

REQUIRED PACKAGES (Window > Package Manager > Unity Registry). A .unitypackage cannot carry these, so install
them BEFORE importing (or right after, then let the project recompile):
  - Cinemachine            3.1.7    (com.unity.cinemachine)   - first-person camera prefab
  - AI Navigation          2.0.12   (com.unity.ai.navigation) - NavMesh for pedestrians / staff
  - Input System           1.19.0   (com.unity.inputsystem)   - player controls (choose "Yes" if asked to enable the new input backend)
  - Universal RP           17.3.0   (already in the 3D URP template)

Lighting: baked ceiling/fill lights (lightmaps + light probes) are included with the scene; the sun and exterior lights are real-time.
Play the scene with the FPP player: WASD to move, mouse to look.

Asset sources and licenses: see Assets/Credits.txt
