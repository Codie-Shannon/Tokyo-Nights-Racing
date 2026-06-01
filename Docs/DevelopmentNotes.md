# Development Notes

This document records important development decisions, bugs solved, known limitations, and future improvement areas for **Tokyo Nights Racing**.

## Project Purpose

Tokyo Nights Racing was built as a technical portfolio project, not as a finished commercial racing game.

The main goal was to prove that multiple Unity/C# systems could be designed, connected, debugged, and packaged into a playable public project.

The project focuses on:

- Systems architecture
- Data-driven design
- Race flow
- AI behavior
- Scene state management
- Editor tooling
- Public GitHub cleanup

## Key Development Decisions

### Use ScriptableObjects for Data

Vehicles and race modes use ScriptableObject assets. This made it easier to configure vehicles, races, AI filtering, and garage data without hardcoding everything into scene scripts.

This approach was used for:

- Vehicle database
- Vehicle data
- Race mode database
- Race mode definitions

### Separate Gameplay, Preview, and AI Prefabs

Vehicles can have separate prefabs for:

- Player gameplay
- Garage preview
- AI racing

This avoided trying to make one prefab handle every use case perfectly.

Garage preview vehicles can have gameplay components disabled, while gameplay and AI prefabs keep their driving logic.

### Separate AI Steering from Race Progress

One major issue was AI race position.

Originally, AI waypoint progress was accidentally treated as checkpoint progress. This caused incorrect rankings when AI used more waypoints than there were checkpoints.

Final architecture:

```text
AI waypoints = steering
Race checkpoints = official race progress
RacePositionManager = ranks racers by checkpoint progress
```

This allows the AI to use extra waypoints for smoother driving without corrupting race position.

### Use Return-State Data for Scene Flow

The project has several scene return paths:

- Garage to main menu
- Race modes to race scene, then back to main menu
- Freeroam marker to race scene, then back to the marker

Static state containers were used to bridge scene loads.

Important lesson:

```text
Temporary scene transition data must be cleared after use.
```

If stale data remains, later flows can break in unexpected ways.

## Major Bugs Solved

### Race Position Bug

Problem:

```text
Player position could jump incorrectly during a race.
AI could appear ahead because waypoint progress was being treated like checkpoint progress.
```

Fix:

```text
Removed AI waypoint progress from race progress.
Checkpoint triggers now update RacerProgress for both player and AI.
RacePositionManager ranks racers using RacerProgress only.
```

### Race Return Marker Bug

Problem:

```text
After finishing a freeroam marker race, the player briefly returned to the correct marker but then snapped back to a default spawn point.
```

Fix:

```text
SelectedVehicleSpawner ignores the normal PlayerSpawnPoint when returning from a race.
FreeroamReturnManager handles race return placement.
A safe two-step placement routine prevents physics settling issues.
```

### Loading Screen Stuck on Main Menu Return

Problem:

```text
RaceScene returned to MainMenuScene, but the loading overlay stayed visible.
```

Fix:

```text
Return flow now uses SceneLoaderWithLoadingScreen instead of manually showing a loading screen and then loading the scene separately.
```

### Garage Return State Bug

Problem:

```text
Returning from Garage did not select the Garage item on the main menu carousel.
```

Fix:

```text
GarageBackButton requests MainMenuReturnState.Garage before loading the main menu.
MainMenuCarousel consumes the request and selects Garage.
```

### Race Modes Return State Bug

Problem:

```text
After a Race Modes race, the player returned to the main menu but later Garage/race flows could be affected by stale RaceLaunchData.
```

Fix:

```text
ResultsScreenUI clears RaceLaunchData when returning to the main menu from a Race Modes race.
```

### Incorrect AI Vehicle Type

Problem:

```text
Race Modes launched the correct race, but AI vehicles could be from the wrong vehicle class.
```

Fix:

```text
VehicleDatabaseRaceAISpawner filters AI vehicles by RaceDefinition.requiredVehicleType.
```

## Known Limitations

Tokyo Nights Racing is a playable technical prototype, not a final game.

Known limitations:

- Vehicle scale and art direction could be normalized further.
- AI behavior is good for the prototype scope but not full commercial racecraft AI.
- Advanced overtaking/blocking logic could be improved.
- Traffic behavior could be expanded with intersections and rule-based driving.
- UI visuals could receive more final polish.
- More track layouts and race types could be added.
- More player progression systems could be added.
- Controller support could be added.
- Some systems could be refactored further into reusable packages.

## Future Improvements

Possible future work:

- Add career/progression mode.
- Add vehicle unlocks.
- Add more race types.
- Add more advanced AI corner-speed prediction.
- Improve AI overtaking and blocking behavior.
- Add better race difficulty settings.
- Add more traffic behavior rules.
- Add intersection handling for traffic.
- Add persistent player profile data.
- Add more polished vehicle previews.
- Add controller support.
- Add a downloadable build/release.
- Split reusable editor tools into separate Unity packages.

## Public Repository Cleanup

Before public upload, the project was cleaned by:

- Removing or excluding private/proprietary scripts.
- Removing risky prototype/copyrighted assets.
- Replacing vehicles with public-safe alternatives.
- Organizing scripts into clearer folders.
- Fixing missing script references.
- Testing main scene flows.
- Preparing README, docs, screenshots, and notices.

## Excluded Tools / Private Work

Some tools and experiments used during development are not included in the public repository.

Reasons for exclusion:

- Experimental one-off scripts.
- Proprietary or future Asset Store tool ideas.
- Messy prototype utilities.
- Tools not required to run the public portfolio build.
- Private cleanup/generation scripts.

The public repository focuses on the playable project and core systems.
