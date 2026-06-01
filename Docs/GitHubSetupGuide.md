# GitHub Setup Guide

This guide is for the first public GitHub upload of Tokyo Nights Racing.

## 1. Final Unity Lock-In

In Unity:

1. Save all open scenes.
2. Save Project.
3. Clear the Console.
4. Exit Play Mode.
5. Close Unity.
6. Reopen Unity.
7. Run one final smoke test.

Recommended smoke test:

```text
Main Menu → Settings → Back
Main Menu → Garage → Equip Vehicle → Main Menu
Main Menu → Race Modes → Race → Results → Main Menu
Main Menu → Freeroam → Mission Marker Race → Results → Return to Freeroam
Freeroam traffic spawns and drives
AI race position works correctly
```

If the smoke test passes, stop changing gameplay systems.

## 2. Create Repo Folders

In the Unity project root, create:

```text
Screenshots/
Docs/
```

The project root should be the folder that contains:

```text
Assets/
Packages/
ProjectSettings/
```

## 3. Add Documentation Files

Add these files to the project root:

```text
README.md
LICENSE
.gitignore
THIRD_PARTY_NOTICES.md
```

Add these files inside `Docs/`:

```text
Docs/SystemsOverview.md
Docs/DevelopmentNotes.md
Docs/GitHubSetupGuide.md
Docs/ScreenshotChecklist.md
```

## 4. Public Safety Search

Before committing, search the whole project for risky/private terms:

```text
Disney
Pixar
Cars
Lightning
McQueen
Mater
Radiator
Doc Hudson
DoNotUpload
Private
```

If anything risky appears in `Assets/`, remove it, rename it, or move it to a private ignored folder before committing.

## 5. Create GitHub Repository

On GitHub:

1. Create a new repository.
2. Suggested name: `Tokyo-Nights-Racing`
3. Set visibility to Public.
4. Do not initialize with README.
5. Do not initialize with .gitignore.
6. Do not initialize with license.

You already have those files locally.

## 6. Initialize Git Locally

Open a terminal in the project root.

Run:

```bash
git init
git status
```

Make sure generated folders like `Library/`, `Temp/`, `Obj/`, `Logs/`, and `UserSettings/` are not being tracked.

## 7. Add Files

Run:

```bash
git add .gitignore README.md LICENSE THIRD_PARTY_NOTICES.md Docs Screenshots Assets Packages ProjectSettings
git status
```

Check the status carefully.

These should not appear:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
.vs/
DoNotUpload/
_Private_DoNotUpload/
```

## 8. First Commit

Run:

```bash
git commit -m "Initial public portfolio build"
git branch -M main
```

## 9. Connect Remote Repo

Replace `YOUR_USERNAME` with your GitHub username.

```bash
git remote add origin https://github.com/YOUR_USERNAME/Tokyo-Nights-Racing.git
git push -u origin main
```

## 10. After Upload

On GitHub, check:

1. README displays properly.
2. Screenshot links are not broken.
3. No private/prohibited folders are visible.
4. `Assets/`, `Packages/`, and `ProjectSettings/` are present.
5. `Library/`, `Temp/`, and `UserSettings/` are not present.
6. Third-party notices exist.
7. License file exists.

## 11. Optional Second Commit

After adding the YouTube demo link or extra screenshots:

```bash
git add README.md Screenshots Docs
git commit -m "Add documentation and screenshots"
git push
```
