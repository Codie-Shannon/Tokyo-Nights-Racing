# Reproduction Guide

## Supported editor

Use the exact editor recorded by the project:

```text
Unity 2022.3.62f1
```

Install that editor through Unity Hub with the Windows Build Support module if
a Windows portfolio build is required.

## Clone

```powershell
git clone https://github.com/Codie-Shannon/Tokyo-Nights-Racing.git
cd Tokyo-Nights-Racing
```

## Package resolution

The public repository must not depend on a package stored under a developer's
personal Downloads folder.

The closure pass converts the original local glTFast reference into a
versioned OpenUPM dependency while preserving the original package identifier
and detected package version.

## Validate locally

Close the Unity Editor, then run:

```powershell
.\tools\validate-unity-project.ps1
```

The validator:

- finds Unity 2022.3.62f1;
- imports and resolves the project in batch mode;
- checks the Unity log for package or compiler failures;
- verifies the package lock is machine-independent;
- runs the repository and evidence verifier.

## Open normally

After the validator passes, open the repository from Unity Hub using Unity
2022.3.62f1.

The enabled build scenes are:

1. `Assets/Scenes/BootScene.unity`
2. `Assets/Scenes/MainMenuScene.unity`
3. `Assets/Scenes/MainScene - Tokyo.unity`
4. `Assets/Scenes/GarageScene.unity`
5. `Assets/Scenes/RaceScene.unity`

## Review path

1. Watch the gameplay demo linked from the README.
2. Review the eleven approved screenshots.
3. Inspect vehicle and race-mode data.
4. Inspect AI checkpoint and waypoint separation.
5. Inspect traffic, settings, scene return, and editor-tool code.
