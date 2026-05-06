using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// Per-frame spatial collision detection between the ship and (a) drifting
/// debris and (b) ring rims. Each new contact (rising-edge) consumes one
/// <see cref="Ship.ShieldCharges"/> if available; otherwise increments
/// <see cref="CollisionPenalty"/>. Game1 reads the penalty into the score.
/// </summary>
/// <remarks>
/// Not driven by Bepu narrow-phase callbacks — those run on the simulation
/// thread and would need a thread-safe queue. Spatial checks are O(1) per
/// debris and per ring, plenty fast for our scale, and keep all collision
/// logic on the main thread.
/// </remarks>
public sealed class CollisionTracker : GameComponent
{
    private readonly Ship _ship;
    private readonly Course _course;
    private readonly DebrisSpawner _debrisSpawner;
    private readonly HashSet<int> _contactingDebris = new();
    private readonly HashSet<int> _contactingRings = new();

    /// <summary>Cumulative score penalty from collisions (after shield).</summary>
    public int CollisionPenalty { get; private set; }

    /// <summary>Cumulative collision count (raw, including shield-absorbed).</summary>
    public int CollisionCount { get; private set; }

    /// <summary>Score deducted per shield-less collision.</summary>
    public int PenaltyPerHit { get; set; } = 15;

    /// <summary>Approximate ship hull radius for spatial checks.</summary>
    public float ShipHullRadius { get; set; } = 1.0f;

    /// <summary>Optional callback fired per new contact (for audio).</summary>
    public System.Action? OnHit;

    public CollisionTracker(Game game, Ship ship, Course course, DebrisSpawner debrisSpawner) : base(game)
    {
        _ship = ship;
        _course = course;
        _debrisSpawner = debrisSpawner;
    }

    public void Reset()
    {
        CollisionPenalty = 0;
        CollisionCount = 0;
        _contactingDebris.Clear();
        _contactingRings.Clear();
    }

    public override void Update(GameTime gameTime)
    {
        NumVector3 shipPos = _ship.Pose.Position;
        TickDebris(shipPos);
        TickRings(shipPos);
    }

    private void TickDebris(NumVector3 shipPos)
    {
        var debrisList = _debrisSpawner.ActiveDebris;
        var stillContacting = new HashSet<int>();
        for (int i = 0; i < debrisList.Count; i++)
        {
            var d = debrisList[i];
            if (d.IsExpired) continue;
            int id = d.BodyHandle.Value;
            NumVector3 delta = shipPos - d.Position;
            float threshold = ShipHullRadius + d.Radius;
            if (delta.LengthSquared() < threshold * threshold)
            {
                stillContacting.Add(id);
                if (!_contactingDebris.Contains(id)) RegisterHit();
            }
        }
        _contactingDebris.Clear();
        foreach (var id in stillContacting) _contactingDebris.Add(id);
    }

    private void TickRings(NumVector3 shipPos)
    {
        var stillContacting = new HashSet<int>();
        for (int i = 0; i < _course.Rings.Count; i++)
        {
            var ring = _course.Rings[i];
            // Distance from ship to nearest point on the ring's rim circle.
            NumVector3 ringCenter = new(ring.Position.X, ring.Position.Y, ring.Position.Z);
            NumVector3 axis = new(ring.Axis.X, ring.Axis.Y, ring.Axis.Z);
            NumVector3 toShip = shipPos - ringCenter;
            float along = NumVector3.Dot(toShip, axis);
            NumVector3 inPlane = toShip - axis * along;
            float radial = inPlane.Length();
            if (radial < 1e-3f) continue; // ship exactly on the axis line — no rim hit
            NumVector3 rimPoint = ringCenter + (inPlane / radial) * ring.MajorRadius;
            float distToRim = NumVector3.Distance(shipPos, rimPoint);
            float threshold = ShipHullRadius + ring.MinorRadius;
            if (distToRim < threshold)
            {
                stillContacting.Add(i);
                if (!_contactingRings.Contains(i)) RegisterHit();
            }
        }
        _contactingRings.Clear();
        foreach (var i in stillContacting) _contactingRings.Add(i);
    }

    private void RegisterHit()
    {
        CollisionCount++;
        OnHit?.Invoke();
        if (_ship.ShieldCharges > 0)
        {
            _ship.ShieldCharges--;
        }
        else
        {
            CollisionPenalty += PenaltyPerHit;
        }
    }
}
