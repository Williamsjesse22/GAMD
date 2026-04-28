# Project 1A — MonoGame Mini-Golf

## Context
Individual project for ComS 437 (Iowa State, Spring 2026). Graded. The assignment explicitly permits AI-assisted development, but the **requirements below are hard constraints** — if any instruction in chat conflicts with this file, flag it before proceeding.

## Tech stack
- MonoGame 3.8.4+ (DesktopGL template, `mgdesktopgl`)
- C# / .NET 9
- VS Code with C# Dev Kit
- MGCB for content pipeline (via local dotnet tool manifest)

## Hard requirements (do not skip, do not reinterpret)
1. **Playable 2D top-down mini-golf** with at least one hole featuring:
   - At least one bank / corner
   - At least one uphill or downhill slope patch (red = uphill from the ball's POV, green = downhill)
   - At least one obstacle (rectangle inside the hole area)
2. **Ball launched from a blue rectangle (tee)** with user-selected:
   - **Direction**: at least 32 distinct angles (360° / 32 = 11.25° steps)
   - **Strength**: at least 16 distinct levels
   - Input must be *intuitive* — no typing numbers. Use mouse aim + hold-to-charge power meter, or equivalent.
3. **Keyboard + mouse only**. No gamepad required.
4. **Stroke count displayed** on screen and updated per shot.
5. **Three required sounds**: ball struck, ball hits wall/obstacle, ball falls into hole.
6. **MonoGame component architecture**. Use `GameComponent` / `DrawableGameComponent`. Don't dump everything into `Game1.cs`.
7. **Custom 2D physics engine**. Must be written by me (with Claude's help). No third-party physics libraries. This is the core graded piece.

## Architecture

### Directory layout
```
MiniGolf/
├── Content/
│   ├── Content.mgcb
│   ├── Sounds/       (strike.wav, bounce.wav, drop.wav)
│   └── Textures/     (ball, tee, obstacle, slope overlays, hole)
├── Physics/
│   ├── PhysicsWorld.cs        — integrates all rigid bodies each tick
│   ├── RigidBody.cs           — position, velocity, mass, friction coefficient
│   ├── Colliders/
│   │   ├── ICollider.cs
│   │   ├── CircleCollider.cs
│   │   └── AabbCollider.cs    — axis-aligned box for walls/obstacles
│   ├── CollisionResolver.cs   — circle-vs-aabb, circle-vs-circle
│   └── SlopeRegion.cs         — rectangular region that applies a constant accel vector
├── GameObjects/
│   ├── Ball.cs                — DrawableGameComponent + RigidBody
│   ├── Tee.cs
│   ├── Hole.cs                — detects ball-in, respects max-entry speed
│   ├── Obstacle.cs
│   └── Course.cs              — loads and owns the level layout
├── Systems/
│   ├── InputManager.cs        — GameComponent; owns aim + power state machine
│   ├── AudioManager.cs        — loads and plays the 3 required SoundEffects
│   └── HudComponent.cs        — DrawableGameComponent; draws stroke count, power meter, aim line
├── Game1.cs                   — thin: sets up components, transitions ball-ready / ball-moving / hole-complete states
└── Program.cs
```

### Physics engine notes
- Fixed timestep (MonoGame's default 60 Hz `Update` is fine). Don't do variable dt in the integrator — it makes friction and slope accel unstable.
- Ball state: `Position`, `Velocity`, `Radius`, `Mass`, `LinearDamping` (friction).
- Per tick, in order:
  1. Sum accelerations (slope regions the ball overlaps contribute their accel vector).
  2. Integrate velocity with damping: `v = (v + a*dt) * (1 - damping*dt)`.
  3. Integrate position: `p = p + v*dt`.
  4. Collision detection and response (reflect velocity about collision normal, apply restitution 0.7–0.9).
  5. If `|v| < epsilon`, snap to zero — ball stops, next shot allowed.
- Ball-in-hole: if ball center is within hole radius AND `|v| < maxEntrySpeed`, it drops. If it's faster than maxEntrySpeed, it skips over — good game feel and justifies the power meter.
- Circle vs AABB: find the closest point on the box to the circle center, compute normal from that point to the center, reflect.

### Input state machine
```
Idle (ball stopped)
  └─ mouse move → Aiming (show 32-quantized aim line)
        └─ LMB down → Charging (power meter animates 0→max→0 in a loop, 16 quantized levels)
              └─ LMB up → Launched (apply impulse, increment stroke count)
                    └─ ball stops → Idle
                    └─ ball in hole → HoleComplete
```

## Coding conventions
- `PascalCase` for public members/types, `_camelCase` for private fields.
- No LINQ in the physics hot path (per-frame allocations).
- Use `Microsoft.Xna.Framework.Vector2` throughout, not custom vector types.
- XML doc comments required on public members of `Physics/` and `Systems/`.
- `Game1.cs` should be under ~150 lines. If it's growing, something belongs in a component.

## Build & run
```bash
# From solution root
dotnet restore
dotnet tool restore       # first time only, restores MGCB
dotnet run --project MiniGolf
```

Edit content (add textures/sounds):
```bash
dotnet mgcb-editor MiniGolf/Content/Content.mgcb
```

## Bonus features (only after base game is done and passes the requirements list)
- Multiple players (hot-seat turn tracking)
- Multiple holes + procedural generation (layout RNG with bank/slope/obstacle placement)
- Slope grid: divide a slope region into cells, each with its own direction vector, render as arrows

## Rules for Claude Code working on this repo
1. **Do not add NuGet packages** beyond what the `mgdesktopgl` template ships with, without asking. In particular: no third-party physics libraries. That breaks requirement 7.
2. **Respect the directory layout above**. New physics code goes in `Physics/`, new components in `Systems/` or `GameObjects/`.
3. **Keep `Game1.cs` thin**. If a change to `Game1.cs` is over ~30 lines, propose extracting it to a component first.
4. **Before implementing a new feature**, re-read the hard requirements section. If the feature can be framed to also satisfy a requirement, prefer that framing.
5. **When writing physics code**, prefer readable math over micro-optimizations. This is an educational project.
6. **Don't use Unity types or terminology**. This is MonoGame; there is no `MonoBehaviour`, `Transform`, or `GameObject` class. Use `GameComponent` / `DrawableGameComponent`.
7. **Sound effects**: load via `Content.Load<SoundEffect>` and play through `AudioManager`. Don't scatter `SoundEffect.Play()` calls across game objects.
8. **Tests**: if asked for tests, put pure-logic tests (physics math, collision detection) in a separate test project under `MiniGolf.Tests/` using xUnit.