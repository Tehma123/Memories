# Assets Folder Reorganization Log

Date: 2026-04-09

## Goal
Reorganize Unity project folders into a clearer and easier-to-maintain structure.

## New Assets Layout

- Assets/LeanTween
- Assets/Resources
- Assets/TextMesh Pro
- Assets/_Project
  - Assets/_Project/Art
  - Assets/_Project/Gameplay
  - Assets/_Project/Config/Input
  - Assets/_Project/Docs

## Move Summary

### Art
- Assets/Animations -> Assets/_Project/Art/Animations
- Assets/Arts -> Assets/_Project/Art/Arts
- Assets/Scene -> Assets/_Project/Art/Scene
- Assets/Thaleah_PixelFont -> Assets/_Project/Art/Thaleah_PixelFont
- Assets/TilePalette -> Assets/_Project/Art/TilePalette
- Assets/Tiles -> Assets/_Project/Art/Tiles

### Gameplay
- Assets/Prefabs -> Assets/_Project/Gameplay/Prefabs
- Assets/Scenes -> Assets/_Project/Gameplay/Scenes
- Assets/Scripts -> Assets/_Project/Gameplay/Scripts

### Config and docs
- Assets/InputSystem_Actions.inputactions -> Assets/_Project/Config/Input/InputSystem_Actions.inputactions
- Assets/Credit.txt -> Assets/_Project/Docs/Credit.txt

Note: Each moved folder/file was moved together with its matching .meta file to preserve Unity GUID references.

## Configuration Update

- Updated scene path in ProjectSettings/EditorBuildSettings.asset:
  - from: Assets/Scenes/MainMenuScene.unity
  - to: Assets/_Project/Gameplay/Scenes/MainMenuScene.unity
- Updated template default scene path in ProjectSettings/ProjectSettings.asset:
  - from: Assets/Scenes/SampleScene.unity
  - to: Assets/_Project/Gameplay/Scenes/MainMenuScene.unity

## Validation Done

- Confirmed new folder tree exists under Assets/_Project.
- Confirmed top-level Assets no longer contains old project folders.
- Confirmed moved files still have matching .meta files in new locations.
- Left existing unrelated uncommitted changes untouched.

## Suggested Next Check (in Unity Editor)

1. Open the project and allow Unity to refresh assets.
2. Open Build Settings and verify MainMenuScene is still enabled.
3. Run a quick playtest for Main Menu -> Gameplay flow.
4. Check any direct path-based loading logic (if added later) to ensure paths are updated.
