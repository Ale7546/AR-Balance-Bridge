# AR Balance Bridge

A portrait Android augmented reality game built with Unity 6. Place a bridge on a horizontal floor or table, then steer a physics ball across it using the MOVE joystick.

## Download and play

[Download the Android APK](Builds/Android/ARBalanceBridge.apk)

Install on an ARCore-supported Android phone and allow camera access. Scan a well-lit, textured floor or table. Once the course preview appears, tap **PLACE BRIDGE HERE**. Push MOVE up toward the finish and use small left/right corrections. Leave about one metre of clear space for the course.

- Level 1: standard bridge.
- Level 2: narrower middle, available after completing Level 1.
- RETRY: reset the current level without changing placement.
- NEW SPOT: return to placement.
- RESET AR: clear the round and restart the camera/AR session. Android's camera privacy switch must be enabled separately.
- Tracking interruption pauses the round; MOVE remains visible but disabled until resume.

## Open the source

Open this repository folder in **Unity 6000.0.85f1**. Install Android Build Support, SDK/NDK and OpenJDK through Unity Hub, and select them in Unity's External Tools preferences. Open `Assets/ARBalanceBridge/Scenes/Main.unity`.

Packages are pinned in `Packages/manifest.json` and `packages-lock.json`: AR Foundation/ARCore 6.0.8, Input System 1.11.2 and Unity UI 2.0.0. Android uses ARM64, IL2CPP, OpenGLES3, minimum API 24 and target API 35. The checked APK is a development build.

Editor menus under **AR Balance Bridge** configure the environment, create the main scene and build the Android APK. The build script writes output to the parent folder's `Builds/Android/` directory. Editor/Windows practice uses WASD, arrows or the joystick and is explicitly labelled as non-AR.

## Verification

- Automated Unity physics simulation: both levels crossed, retried and deliberately fallen from at three orientations (six crossings and six falls).
- Tested on Nothing Phone (3a) Pro with Android 16: placement, joystick, both level completions, progression, retry/fall, pause after backgrounding and AR reset.
- Rendering target is 60 FPS; sustained device FPS was not measured. Physics runs at a 0.02-second timestep.

## Limitations

Tracking depends on lighting and surface texture. Placement is retained in AR session space for short rounds and can drift. No audio, haptics, tilt steering, cloud anchors or moving suspension bridge are implemented. Finish detection uses course-local position bounds rather than a trigger collider.

The report, certificates, student details, raw phone screenshots, local tools and generated build caches are excluded from this repository.
