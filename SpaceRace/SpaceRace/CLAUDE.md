# Project 1B — The Great Space Race

## Context
Individual project for ComS 4370. Graded. AI-assisted development is permitted, but the **requirements below are hard constraints** — if any chat instruction conflicts with this file, flag it before proceeding.

This is a sibling project to **Project 1A** (`../MiniGolf/`). Code may be **copied** from 1A into this project's `Systems/` folder, but **never reference** or **modify** 1A. The two projects build and ship independently.

## Tech stack
- MonoGame 3.8.4+ (DesktopGL template, `mgdesktopgl`)
- C# / .NET 9
- **BepuPhysics V2** (`BepuPhysics` NuGet package) — required for all 3D objects
- VS Code with C# Dev Kit
- MGCB for content pipeline (via local dotnet tool manifest)

## Hard requirements (do not skip, do not reinterpret)
1. **3D MonoGame game.** All physics goes through Bepu V2 — no kinematic transforms on physics-bearing objects.
2. **Spacecraft control via forces only** ("steering behaviors, not kinematic"). Apply impulses + angular impulses to the ship body each tick; never set its position or orientation directly.
3. **Inputs.** Keyboard primary, gamepad supported. Controls expose yaw, pitch, roll, and forward thrust.
4. **Camera.** First-person cockpit OR behind-the-ship — both implemented, toggled with `C`. (Spec allows either; we ship both.)
5. **At least 7 rings** forming a course. Player flies through them in order.
6. **Active-ring accent** — the next-target ring is visibly distinguished (color/glow) from the others.
7. **Race-against-time clock.** Running clock during the race; final time displayed when the player crosses the finish line.
8. **Missed-rings counter** displayed during and after the race.
9. **Balanced final score** — must combine time + rings missed such that flying directly to the last ring is *worse* than visiting all rings.
10. **Rings are infinite mass** — they don't move when hit. (Implemented as Bepu Statics.)
11. **Skybox required** for the background.
12. **MonoGame component architecture.** `GameComponent`/`DrawableGameComponent` for game objects and systems; `Game1.cs` stays thin (~150 lines).

## Architecture

### Directory layout
```
SpaceRace/
├── Content/
│   ├── Content.mgcb
│   ├── Effects/Skybox.fx          (TextureCube sampler)
│   └── Sounds/                    (engine, ringPass, hit, finish — WAVs supplied later)
├── Physics/
│   ├── BepuWorld.cs               (Simulation + BufferPool + ThreadDispatcher; lifecycle)
│   ├── PhysicsCallbacks.cs        (struct INarrowPhaseCallbacks + IPoseIntegratorCallbacks)
│   └── Trigger.cs                 (reusable pass-through volume — rings, pitstops, fuel pickups)
├── Graphics/
│   ├── Camera.cs                  (chase + first-person, C toggle)
│   ├── Skybox.cs
│   ├── MeshFactory.cs             (procedural torus / ship-wedge / inverted cube / asteroid)
│   └── PrimitiveRenderer.cs       (BasicEffect helpers + glow pass)
├── GameObjects/
│   ├── Ship.cs                    (Bepu dynamic body + Fuel field)
│   ├── Ring.cs                    (Bepu static mesh torus + Trigger)
│   ├── Course.cs                  (ring sequence, target index, missed count)
│   ├── Pitstop.cs                 (HOOK)
│   ├── Debris.cs / DebrisSpawner.cs (HOOK)
│   ├── GravityWell.cs             (HOOK)
│   └── Projectile.cs              (HOOK)
├── Systems/
│   ├── ShipController.cs          (input → forces/torques)
│   ├── HudComponent.cs            (clock, missed, fuel, score, overlays)
│   ├── AudioManager.cs
│   ├── BitmapText.cs              (copied from 1A)
│   └── TextureFactory.cs          (copied from 1A, extended for cubemap faces)
├── Game1.cs                       (thin: state machine PreRace / Racing / Finished)
└── Program.cs
```

### Bepu V2 notes
- `Simulation.Create(BufferPool, narrowPhase, poseIntegrator, new SolveDescription(...))`
- `INarrowPhaseCallbacks` and `IPoseIntegratorCallbacks` must be **structs**, not classes — V2 requires this and a class will allocate per step.
- `IPoseIntegratorCallbacks` must implement `AngularIntegrationMode` and `AllowSubstepsForUnconstrainedBodies` — older tutorials omit them and won't compile.
- Apply forces: `bodyRef.ApplyImpulse(impulse, offset)`, `bodyRef.ApplyAngularImpulse(torque * dt)`. Set `bodyRef.Awake = true` after applying or sleeping bodies will silently ignore.
- For "constant W thrust": `impulse = desiredAccel * mass * dt`. Never set velocity directly (violates forces-only requirement).
- Linear + angular damping configured at body creation — zero-g would otherwise drift forever.
- Mesh data (e.g. ring torus triangles) must outlive the Simulation. Store buffers on `BepuWorld` and dispose after `Simulation.Dispose()`.

### Controls
- **Keyboard:** W/S = pitch, A/D = yaw, Q/E = roll, Space = thrust forward, X = auto-level (small angular impulse to zero roll).
- **Gamepad:** left stick = yaw/pitch, right stick = roll, right trigger = thrust.
- **Camera:** `C` toggles between Chase and FirstPerson.
- **System:** `Esc` quits, `R` restarts after race finishes.

### Pass-through detection (Trigger)
Parametric segment-vs-plane: given prev frame position `p0` and current `p1`, find `t = -d0/(d1-d0)` where `d0`/`d1` are signed distances to the ring plane. Interpolate hit point `p0 + t*(p1-p0)`. If distance from hit point to ring center < inner radius → pass. Direction of crossing distinguishes forward pass from backwards.

### Score formula
```
Score = max(0, 1000 - timeSeconds) - 30 * missedCount
```
30 pts/missed; missing 6 rings (skip-to-end) = -180. Saved seconds from skipping is small relative to the penalty — visiting all rings always wins.

## Coding conventions
- `PascalCase` for public members/types, `_camelCase` for private fields.
- No LINQ in physics-adjacent hot paths (per-frame allocations).
- Use `Microsoft.Xna.Framework.Vector3` / `Quaternion` / `Matrix` throughout.
- XML doc comments on public members of `Physics/`, `Graphics/`, `Systems/`.
- `Game1.cs` should stay under ~150 lines — extract to a component if it grows.

## Build & run
```bash
# From SpaceRace/ root
dotnet tool restore       # first time only, restores MGCB
dotnet restore
dotnet run --project SpaceRace
```

## Bonus features (architected as hooks; not populated in v1)
- More than one player (network or split-screen)
- Space debris (drift-across-course bodies)
- Fuel + pitstops (Ship.Fuel field already present)
- Stationary gravity wells (per-tick force)
- Replay with selectable cameras
- Lasers / torpedoes
- Limited-use shield

## Rules for Claude Code working on this repo
1. **Bepu V2 IS allowed** — it's a hard spec requirement. Do not propose alternative physics engines.
2. **Other NuGet packages** require the user's OK. The default package set is: `MonoGame.Framework.DesktopGL`, `MonoGame.Content.Builder.Task`, `BepuPhysics`.
3. **Steering behaviors only.** Never set body position or orientation directly on a physics-bearing object during gameplay. Apply impulses or angular impulses.
4. **Respect the directory layout above.** New physics in `Physics/`, new components in `GameObjects/` or `Systems/`, new graphics helpers in `Graphics/`.
5. **Keep `Game1.cs` thin.** If a change to `Game1.cs` is over ~30 lines, propose extracting to a component first.
6. **Do not modify `../MiniGolf/`.** 1A and 1B ship independently. Code may be copied; references and modifications are not allowed.
7. **Sound effects:** load via `Content.Load<SoundEffect>` through `AudioManager` with graceful fallback (silent if missing). Don't scatter `SoundEffect.Play()` across game objects.
8. **Tests:** if asked, put pure-logic tests (physics math, trigger detection, score formula) in a separate test project under `SpaceRace.Tests/` using xUnit.
