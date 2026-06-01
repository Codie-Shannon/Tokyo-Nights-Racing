# Third-Party Notices

This project uses Unity, Unity packages, Unity Asset Store assets, third-party 3D models, fonts, audio/video assets, and other project-specific content.

Source code written by the author is provided separately under the repository license. Third-party assets, tools, packages, models, textures, audio, video, fonts, and Unity packages remain under their original licenses.

The repository license applies only to source code written by the author unless otherwise stated. Third-party assets are not automatically relicensed by this repository. Check each asset's original license before reuse.

## Unity

This project was built with Unity.

Unity and Unity-related packages are owned by Unity Technologies and are subject to Unity's license terms.

Website: https://unity.com/

## Unity Packages in Project

The following Unity/project packages were present in the Unity Package Manager/package manifest during development.

| Package | Version | Source | Notes |
|---|---:|---|---|
| glTFast | 6.18.0 | Local/package | Used for glTF import/workflow support. |
| Burst | 1.8.24 | Unity Registry | Unity package. |
| Collections | 1.2.4 | Unity Registry | Unity package. |
| Custom NUnit | 1.0.6 | Unity/Project package | Testing/package dependency. |
| Mathematics | 1.2.6 | Unity Registry | Unity package. |
| Newtonsoft Json | 3.2.2 | Unity Registry | JSON support package. |
| Post Processing | 3.4.0 | Unity Registry | Post-processing effects. |
| Test Framework | 1.1.33 | Unity Registry | Unity testing framework. |
| TextMeshPro | 3.0.9 | Unity Registry | UI text rendering. |
| Unity UI | 1.0.0 | Unity Registry | Unity UI system. |
| Visual Studio Editor | 2.0.27 | Unity Registry | Visual Studio integration. |

## Unity Asset Store Assets

The following Unity Asset Store assets were used during development. These assets remain under their original Unity Asset Store licenses and are not covered by this repository's MIT source-code license.

| Asset | Publisher | Version Used | Source | License / Notes | Used For |
|---|---|---:|---|---|---|
| AQUAS Lite - Built-In Render Pipeline | Dogmatic | 1.2.1 | Unity Asset Store | Free asset. Standard Unity Asset Store EULA. | Water/shader/VFX support where used. |
| Cartoon Race Track - Oval | RCC Design | 1.0.1 | Unity Asset Store | Free asset. Standard Unity Asset Store EULA. | Race track/environment prototype asset. |
| Japanese Otaku City | ZENRIN CO., LTD. | 1.0 | Unity Asset Store | Free asset. Standard Unity Asset Store EULA. | City/freeroam environment asset. |
| Skybox Series Free | Avionx | 4.3 | Unity Asset Store | Free asset. Standard Unity Asset Store EULA. | Skybox/environment lighting visuals. |

## Vehicle Models

The following third-party vehicle models were used as source vehicle assets. These assets remain under their original licenses and are not covered by this repository's MIT source-code license.

| Asset | Source | Creator / Publisher | License / Notes | Used For |
|---|---|---|---|---|
| Monster Truck | Sketchfab | vecarz / heynic | Creative Commons Attribution. Attribution required. Original source: https://sketchfab.com/3d-models/monster-truck-wwwvecarzcom-d8818ef6c01542c998673e2c34dfbde5 | MonsterTruck vehicle class. |
| Humvee | CGTrader | CGUtopia | Free model. Royalty Free License (no AI). Original source: https://www.cgtrader.com/free-3d-models/car/antique-car/humvee-588506ff-fac6-4337-ad94-290c1881e3bc | OffRoad / utility vehicle class. |
| Audi Concept B24 | CGTrader | brunomcardoso | Free model. Royalty Free License (no AI). Original source: https://www.cgtrader.com/free-3d-models/car/concept-car/audi-concept-b24 | Road vehicle class source model. |
| Skyline E34 concept or tuning project | CGTrader | VANO-ABMARD | Free model. Royalty Free License (no AI). Original source: https://www.cgtrader.com/free-3d-models/vehicle/sci-fi-vehicle/skyline-e34-concept-or-tuning-project | Road / AllTerrain-style vehicle class source model. |

> Note: Vehicle models were adapted for use in Unity gameplay prefabs, including collider setup, Rigidbody setup, audio routing, ground check placement, anti-stuck colliders, and player/AI prefab variants.

### Monster Truck Attribution

Monster Truck by vecarz / heynic on Sketchfab.  
Licensed under Creative Commons Attribution.  
Original source: https://sketchfab.com/3d-models/monster-truck-wwwvecarzcom-d8818ef6c01542c998673e2c34dfbde5

## Traffic Vehicle Models

Traffic vehicle models were used as background/freeroam traffic vehicles. These are separate from the main playable vehicle models.

| Asset | Source | Creator / Publisher | License / Notes | Used For |
|---|---|---|---|---|
| Cars Pack | Quaternius | Quaternius | CC0. Free to use in personal and commercial projects. Includes low-poly car models in common 3D formats. Source: https://quaternius.com/packs/cars.html | Freeroam traffic vehicles. |

## Fonts

The scene-used asset report confirmed the project uses Hemi Head and TextMeshPro/Liberation Sans font assets in the scanned scenes.

| Font | Source / Designer | License / Notes | Used For |
|---|---|---|---|
| Hemi Head | Typodermic Fonts / Raymond Larabie | Third-party font used in the project. Remains under its original license and is not covered by this repository's MIT source-code license. Check original license for redistribution and embedding terms. | Main UI/title/button styling. |
| Liberation Sans | Included with TextMeshPro / Unity | Included with Unity/TextMeshPro package licensing. | Fallback/general TMP text rendering. |

### Removed / Not Included Fonts

The following fonts were present during development or checked during cleanup, but are not needed for the public repository unless later confirmed as used and properly licensed:

| Font | Notes |
|---|---|
| Anton | Not required by the scanned scenes. Remove/exclude if unused. |
| Kelvinized | License information found online was inconsistent. Remove/exclude unless original license is confirmed. |
| Racing Observer Demo | Demo/personal-use-only licensing was identified. Remove/exclude unless a suitable commercial/app/game license has been purchased. |

## Environment Assets

This project uses Unity Asset Store environment assets listed above.

Known environment/model assets used in scenes include:

| Asset / Folder | Source / Notes | Used For |
|---|---|---|
| Japanese Otaku City / ZRNAssets / PQ_Remake_AKIHABARA.fbx | Unity Asset Store asset listed above. | Freeroam city environment. |
| Cartoon Race Track - Oval / CartoonTracksPack1 | Unity Asset Store asset listed above. | Race track environment/prototype race scene. |
| AQUAS Lite WaterPlane | Unity Asset Store asset listed above. | Water/shader/VFX support where used. |
| TokyoNights_Garage_Entrance.fbx | Project-specific/generated/custom asset. | Garage/menu environment. |
| garage.glb | Project-specific/generated/custom asset. | Vehicle select/garage environment. |

Any additional environment textures, models, or materials should be listed here before redistribution.

## Audio and Music

The scene-used asset report confirmed the following audio files are used in scenes or gameplay prefabs.

Some audio files were sourced from free sound libraries during development. Exact source links are listed where confirmed. Files with unconfirmed source links are marked for review before reuse outside this project.

| Asset | Source / License Notes | Used For |
|---|---|---|
| Assets/Audio/Songs/menu_soundtrack.mp3 | Free sound/music source used during development. Exact source/license to confirm. | Menu music. |
| Assets/Audio/Vehicle Audio/car-tire-squeal.wav | OpenGameArt - "Car tire squeal skid loop" by qubodup / Iwan Gabovitch. Source/license to confirm on original page before reuse. Source: https://opengameart.org/content/car-tire-squeal-skid-loop | Tire/skid SFX. |
| Assets/Audio/Vehicle Audio/crash-collision.ogg | "Dull Explosion01" by Iwan Gabovitch / qubodup. Embedded metadata credits Iwan Gabovitch, 2010. Exact source/license to confirm. | Crash/collision SFX. |
| Assets/Audio/Vehicle Audio/ground-collision.wav | Free sound library source used during development. Exact source/license to confirm. | Ground/collision SFX. |
| Assets/Audio/Vehicle Audio/landing_thud.mp3 | Free sound library source used during development. Exact source/license to confirm. | Landing SFX. |
| Assets/Audio/Vehicle Audio/v8_acceleration_loopable_smooth_2p5s_crossfade.wav | Edited/looped project version based on free vehicle audio source. Exact source/license to confirm. | V8 acceleration loop. |
| Assets/Textures/Boot/audio_intro.mp3 | Free/generated intro audio source used during development. Exact source/license to confirm. | Boot/intro audio. |

### Audio Files Removed / Not Included

The following audio files were uploaded or present as source/working files, but were not listed as scene-used by the scanned scene-used asset report. Remove/exclude them from the public repository unless they are later confirmed as referenced by scenes, prefabs, ScriptableObjects, or runtime-loaded code.

| Asset | Notes |
|---|---|
| Acc_05570.wav | Not listed as scene-used. Remove/exclude if unused. |
| acceleration_6s_to_16s(1).mp3 | Not listed as scene-used. Remove/exclude if unused. |
| engine_heavy_loop.mp3 | Not listed as scene-used. Remove/exclude if unused. |
| engine-loop.wav | Not listed as scene-used by the report, although it has confirmed OpenGameArt/qubodup metadata. Remove/exclude if not referenced. |

## Video

The scene-used asset report confirmed the following video is used:

| Asset | Source / License Notes | Used For |
|---|---|---|
| Assets/Textures/Boot/Intro.mp4 | Project intro video. TODO: Confirm source/generation tool and reuse rights before redistribution. | Boot/game intro video. |

If the intro video was generated with a third-party service, update this section with the service name and any relevant license/terms note.

## UI Assets, Icons, and Textures

This project may include third-party UI assets, generated UI art, icons, textures, materials, or logo/branding images.

| Asset / Category | Source / License Notes | Used For |
|---|---|---|
| Tokyo Nights UI/logo/branding textures | Project-specific/generated/custom assets unless otherwise stated. | Menu, garage, race UI. |
| UI panels/buttons/backgrounds | Project-specific/generated/custom assets unless otherwise stated. | Game UI. |
| Any additional icons/textures | TODO: Add source/license if third-party. | UI/world visuals. |

## Generated / Author-Created Content

Game-specific code, setup scripts, custom editor tools, documentation, vehicle prefab setup, gameplay wiring, race systems, UI logic, and project-specific integration work were created by the author unless otherwise stated.

Project-specific/generated/custom assets should be reviewed separately before reuse outside this project, especially if they were produced with external generation tools or based on third-party references.

## Editor Tools

Custom editor tools written by the author are part of the project source code unless otherwise stated.

Some internal/proprietary development tools used during production may be excluded from this public repository.

Known public/editor workflow tools and setup helpers include:

- Vehicle ground checkpoint aligner.
- Audio routing tool.
- Vehicle setup/onboarding workflow.
- Anti-stuck collider setup workflow.
- Race mode/database setup workflow.
- Track variant setup workflow.
- Traffic node setup workflow.

## Removed / Excluded Assets

During public cleanup, old or risky prototype assets should be removed/excluded from the public repository, including any unused copyrighted/prototype vehicle assets.

Known examples to remove/exclude if present:

| Asset / Term | Notes |
|---|---|
| Lightning McQueen | Unused/risky prototype asset. Remove/exclude from public repository. |
| doc_hudson | Unused/risky prototype asset. Remove/exclude from public repository. |
| Disney / Pixar / Cars related assets | Remove/exclude unless fully original and safe. |
| DoNotUpload / Private folders | Should not be uploaded. |

## License Reminder

The repository license applies only to source code written by the author unless otherwise stated.

Third-party assets are not automatically relicensed by this repository. Check each asset's original license before reuse.

If any source or license is uncertain, either confirm it before public redistribution or remove/exclude the asset from the public repository.
