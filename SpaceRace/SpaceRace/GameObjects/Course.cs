using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpaceRace.Graphics;

namespace SpaceRace.GameObjects;

/// <summary>
/// Owns the ring sequence, tracks the current target, counts misses, and fires
/// events for ring-pass and race-finish. Builds a fixed 8-ring course with
/// varied 3D positions and tilts to exercise pitch / yaw / roll.
/// </summary>
public sealed class Course : GameComponent
{
    private readonly Ship _ship;

    /// <summary>All rings in order. Index 0 = first to pass; last = finish line.</summary>
    public List<Ring> Rings { get; } = new();

    /// <summary>Index of the next ring the player should hit.</summary>
    public int CurrentTargetIndex { get; private set; }

    /// <summary>Cumulative count of skipped/out-of-order rings.</summary>
    public int MissedCount { get; private set; }

    /// <summary>True after the player passes the last ring.</summary>
    public bool IsFinished => CurrentTargetIndex >= Rings.Count;

    /// <summary>Fires once when a ring (any) is passed in the forward direction.</summary>
    public event Action? RingPassed;

    /// <summary>Fires once when the player crosses the finish ring.</summary>
    public event Action? RaceFinished;

    private Vector3 _previousShipPosition;
    private bool _hasPreviousPosition;

    public Course(Game game, Ship ship) : base(game) { _ship = ship; }

    /// <summary>
    /// Build the canonical 8-ring course. Caller must add each <see cref="Ring"/>
    /// to <c>Game.Components</c> after this returns (so they receive Draw calls).
    /// </summary>
    public void Build(PrimitiveRenderer renderer)
    {
        // 8-ring path: starts in front of the ship, climbs, banks left, dives,
        // returns to center. Each ring has a different orientation so the player
        // exercises pitch + yaw + roll to thread it cleanly.
        var layout = new (Vector3 Position, Vector3 AxisEuler)[]
        {
            (new Vector3(  0,   0,  -25), new Vector3(0, 0, 0)),
            (new Vector3(  6,   3,  -55), new Vector3(0.2f, 0.3f, 0)),
            (new Vector3( 14,   1,  -85), new Vector3(0, 0.6f, 0.3f)),
            (new Vector3(  8,  -3, -115), new Vector3(-0.4f, 0.4f, -0.2f)),
            (new Vector3( -6,  -2, -145), new Vector3(-0.2f, -0.5f, 0)),
            (new Vector3(-14,   2, -170), new Vector3(0.3f, -0.7f, 0.4f)),
            (new Vector3( -6,   6, -195), new Vector3(0.5f, -0.2f, 0)),
            (new Vector3(  0,   0, -220), new Vector3(0, 0, 0)), // finish
        };

        for (int i = 0; i < layout.Length; i++)
        {
            var orientation = Quaternion.CreateFromYawPitchRoll(
                layout[i].AxisEuler.Y, layout[i].AxisEuler.X, layout[i].AxisEuler.Z);
            var ring = new Ring(Game, renderer, layout[i].Position, orientation);
            Rings.Add(ring);
        }
        RefreshActiveFlags();
    }

    public override void Update(GameTime gameTime)
    {
        if (IsFinished) return;

        Vector3 shipPos = ToXna(_ship.Pose.Position);
        if (!_hasPreviousPosition)
        {
            _previousShipPosition = shipPos;
            _hasPreviousPosition = true;
            return;
        }

        bool ringWasPassedThisTick = false;
        for (int i = 0; i < Rings.Count; i++)
        {
            if (Rings[i].HasBeenPassed) continue;
            // Accept either crossing direction. HasBeenPassed prevents double-counting,
            // and a "forward vs backward" check in 3D is fragile because each ring's
            // axis convention may not align with the player's intended flight direction.
            if (!Rings[i].Trigger.TryPassThrough(_previousShipPosition, shipPos, out _)) continue;

            if (i == CurrentTargetIndex)
            {
                Rings[i].HasBeenPassed = true;
                CurrentTargetIndex++;
                ringWasPassedThisTick = true;
            }
            else if (i > CurrentTargetIndex)
            {
                // Out-of-order pass: mark all skipped intermediates as missed, then this one as passed.
                for (int k = CurrentTargetIndex; k < i; k++)
                {
                    Rings[k].HasBeenPassed = true;
                    MissedCount++;
                }
                Rings[i].HasBeenPassed = true;
                CurrentTargetIndex = i + 1;
                ringWasPassedThisTick = true;
            }
            // i < CurrentTargetIndex (already passed) — ignore.
        }

        if (ringWasPassedThisTick)
        {
            RingPassed?.Invoke();
            RefreshActiveFlags();
            if (IsFinished) RaceFinished?.Invoke();
        }

        _previousShipPosition = shipPos;
    }

    private void RefreshActiveFlags()
    {
        for (int i = 0; i < Rings.Count; i++)
        {
            Rings[i].IsActive = (i == CurrentTargetIndex);
        }
    }

    /// <summary>Reset the race state in place (used on restart). Doesn't touch ring meshes.</summary>
    public void Reset()
    {
        CurrentTargetIndex = 0;
        MissedCount = 0;
        _hasPreviousPosition = false;
        for (int i = 0; i < Rings.Count; i++) Rings[i].HasBeenPassed = false;
        RefreshActiveFlags();
    }

    private static Vector3 ToXna(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
}
