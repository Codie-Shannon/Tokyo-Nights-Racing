# Systems Overview

This document gives a technical overview of the main systems in **Tokyo Nights Racing** and how they connect.

Tokyo Nights Racing is a Unity/C# arcade racing systems prototype built around data-driven vehicles, race modes, AI racing, freeroam traffic, scene flow, UI, settings, and editor tooling.

## High-Level Architecture

```text
MainMenuCarousel
    ↓
MainMenuRaceModesLauncher
    ↓
RaceModeDatabase / RaceModeDefinition
    ↓
RaceLaunchData + RaceLoadRequest
    ↓
RaceSceneAutoStarter
    ↓
RaceManager
    ↓
GridStartManager + VehicleDatabaseRaceAISpawner
    ↓
RacerProgress + RacePositionManager
    ↓
ResultsScreenUI
```

Freeroam marker races use a similar flow:

```text
MissionMarkerInteract
    ↓
RaceLaunchData + RaceLoadRequest
    ↓
RaceScene
    ↓
ResultsScreenUI
    ↓
FreeroamReturnManager
    ↓
Return player to original mission marker
```

## Vehicle System

Vehicles are configured through ScriptableObject data rather than hardcoded scene references.

The vehicle system includes:

- `VehicleData`
- `VehicleDatabase`
- Gameplay prefabs
- Garage preview prefabs
- AI prefabs
- Vehicle type/class information
- Vehicle display names, descriptions, and stats

Vehicle classes:

- Road
- OffRoad
- AllTerrain
- MonsterTruck

Each vehicle can have separate prefabs for gameplay, garage preview, and AI racing. This makes it easier to tune each use case independently.

## Garage System

The garage system allows the player to preview and equip vehicles.

Main responsibilities:

- Display a selected vehicle preview.
- Cycle between available vehicles.
- Show vehicle name, type, description, and stats.
- Save the equipped vehicle using PlayerPrefs.
- Return to the main menu with the Garage carousel item selected.

Garage preview vehicles have gameplay components disabled so they do not drive, fall, or play unnecessary gameplay effects while displayed.

## Race Mode System

Race Modes are controlled through ScriptableObject definitions.

Main scripts/assets:

- `RaceModeDefinition`
- `RaceModeDatabase`
- `MainMenuRaceModesLauncher`

A race mode defines:

- Race ID
- Display name
- Required vehicle type
- Track variant
- Race scene name
- Return scene name
- Optional AI count override

When the player selects Race Modes from the main menu, the launcher checks the currently equipped vehicle and selects a compatible race.

Example:

```text
Road vehicle → Road race
OffRoad vehicle → OffRoad race
AllTerrain vehicle → AllTerrain race
MonsterTruck vehicle → MonsterTruck race
```

## Race System

The race system handles race setup, start, progress, results, and return flow.

Main components:

- `RaceDefinition`
- `RaceSceneAutoStarter`
- `RaceManager`
- `GridStartManager`
- `RaceCountdown`
- `RacerProgress`
- `Checkpoint`
- `RacePositionManager`
- `ResultsScreenUI`

The race scene reads launch data, finds the requested race, applies the selected track variant, prepares player/AI vehicles, starts the grid, and tracks race progress.

## Race Progress and Positioning

Race progress is based on checkpoint gates.

Each racer has a `RacerProgress` component that tracks:

- Current lap
- Completed laps
- Last checkpoint index
- Finish state
- Finish time
- Finish order
- Segment progress
- Distance to next checkpoint

`RacePositionManager` ranks racers using:

1. Finished state
2. Finish order
3. Completed laps
4. Checkpoint progress
5. Segment progress
6. Distance to next checkpoint as a tie-breaker

This keeps race ranking consistent for both player and AI.

## AI Racing System

The AI system uses waypoints for steering and checkpoints for official race progress.

Main AI responsibilities:

- Follow waypoint routes.
- Use lane offsets.
- Detect vehicles in front.
- Detect nearby side vehicles.
- Apply side nudge avoidance.
- Use side grip correction.
- Recover from being stuck or falling.
- Race with vehicle-class-specific AI prefabs.

Important architecture rule:

```text
AI waypoints = steering only
Checkpoints = official race progress
```

This means AI can use extra waypoints for smoother driving without affecting race ranking.

## Vehicle Database AI Filtering

AI vehicles are selected through the vehicle database.

`VehicleDatabaseRaceAISpawner` filters AI vehicles by the race's required vehicle type.

Example:

```text
Road race → Road AI prefabs
OffRoad race → OffRoad AI prefabs
AllTerrain race → AllTerrain AI prefabs
MonsterTruck race → MonsterTruck AI prefabs
```

This keeps race mode launches consistent with the selected vehicle and track type.

## Freeroam and Mission Markers

Freeroam contains mission markers that can launch races.

Mission marker data includes:

- Race ID
- Race display name
- Race scene
- Return scene
- Return marker ID
- Track variant
- Interaction prompt

Freeroam race flow:

```text
Player interacts with mission marker
    ↓
Race data is stored
    ↓
RaceScene loads
    ↓
Race completes
    ↓
Freeroam scene reloads
    ↓
Player returns to the original marker
```

## Traffic System

The traffic system creates freeroam city traffic using node-based spawning.

Main features:

- Traffic node network
- Spawn-only nodes
- Initial population nodes
- Runtime respawn logic
- Traffic vehicle database
- Weighted/random/cycle vehicle selection
- Spawn blocking
- Player avoidance
- City exit despawn support
- Traffic density setting

Traffic density values:

```text
50
100
150
200
```

The settings menu saves traffic density, and the freeroam traffic spawner reads it when the scene starts.

## Main Menu System

The main menu uses a carousel-style UI.

Features:

- Carousel navigation
- Menu descriptions
- Play / Garage / Race Modes / Settings / Exit
- Race Modes launcher integration
- Title card switching
- Return-state handling

Return-state examples:

```text
Garage → Main Menu with Garage selected
Race Modes race → Main Menu with Race Modes selected
```

## Settings and Audio Systems

The settings system includes:

- Master volume
- Music volume
- SFX volume
- Fullscreen toggle
- Quality dropdown
- Traffic density slider

Audio uses Unity AudioMixer groups:

- Master
- Music
- SFX

`PersistentAudioSettingsLoader` keeps saved audio settings applied across scene changes.

## Scene Loading and Return Flow

Scene flow is handled with a loading screen controller and scene loader.

Important flows:

```text
Main Menu → Garage → Main Menu
Main Menu → Race Modes → RaceScene → Results → Main Menu
Main Menu → Freeroam → Mission Marker Race → RaceScene → Results → Freeroam Marker
```

Temporary scene data is cleared after use to prevent stale state from affecting later flows.

## Editor Tools and Setup Workflows

Editor/development tooling was used to reduce repetitive manual setup.

Tools/workflows include:

- Audio routing tool
- Vehicle ground checkpoint aligner
- Vehicle setup/onboarding workflow
- Anti-stuck collider setup workflow
- Race mode/database setup workflow
- Track variant setup workflow
- Traffic node setup workflow

Some experimental or proprietary tools used during development are excluded from the public repository.
