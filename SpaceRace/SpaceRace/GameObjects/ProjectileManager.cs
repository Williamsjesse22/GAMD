using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpaceRace.Graphics;
using SpaceRace.Physics;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// Owns the active <see cref="Projectile"/>s, fires new ones when called by
/// <see cref="Systems.ShipController"/>, ticks projectile lifetime, and runs
/// per-frame distance-based collision detection against
/// <see cref="DebrisSpawner.ActiveDebris"/>. On hit, both projectile and debris
/// are removed and <see cref="DebrisShotCount"/> increments.
/// </summary>
public sealed class ProjectileManager : GameComponent
{
    private readonly BepuWorld _world;
    private readonly PrimitiveRenderer _renderer;
    private readonly DebrisSpawner _debrisSpawner;
    private readonly List<Projectile> _active = new();

    /// <summary>Cumulative debris destroyed by player projectiles. Read by Game1 for score bonuses.</summary>
    public int DebrisShotCount { get; private set; }

    public ProjectileManager(Game game, BepuWorld world, PrimitiveRenderer renderer, DebrisSpawner debrisSpawner)
        : base(game)
    {
        _world = world;
        _renderer = renderer;
        _debrisSpawner = debrisSpawner;
    }

    /// <summary>Spawn a projectile at <paramref name="spawnPos"/> with <paramref name="velocity"/>.</summary>
    public void Fire(NumVector3 spawnPos, NumVector3 velocity)
    {
        var projectile = new Projectile(Game, _world, _renderer, spawnPos, velocity);
        _active.Add(projectile);
        Game.Components.Add(projectile);
    }

    /// <summary>Reset state for a new race.</summary>
    public void Reset()
    {
        for (int i = _active.Count - 1; i >= 0; i--) RemoveProjectile(i);
        DebrisShotCount = 0;
    }

    public override void Update(GameTime gameTime)
    {
        // Projectile-vs-debris distance check (cheap; small N on both sides).
        for (int p = _active.Count - 1; p >= 0; p--)
        {
            var proj = _active[p];
            if (proj.IsConsumed) continue;
            NumVector3 projPos = proj.Position;
            float hitRadius = 1.4f;

            var debrisList = _debrisSpawner.ActiveDebris;
            for (int d = 0; d < debrisList.Count; d++)
            {
                var debris = debrisList[d];
                if (debris.IsExpired) continue;
                NumVector3 debrisPos = debris.Position;
                float dx = projPos.X - debrisPos.X;
                float dy = projPos.Y - debrisPos.Y;
                float dz = projPos.Z - debrisPos.Z;
                float threshold = hitRadius + debris.Radius;
                if (dx * dx + dy * dy + dz * dz < threshold * threshold)
                {
                    proj.Consume();
                    debris.Kill();
                    DebrisShotCount++;
                    break;
                }
            }
        }

        // Remove expired/consumed projectiles.
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].IsExpired || _active[i].IsConsumed) RemoveProjectile(i);
        }
    }

    private void RemoveProjectile(int index)
    {
        _active[index].RemoveFromSimulation();
        Game.Components.Remove(_active[index]);
        _active[index].Dispose();
        _active.RemoveAt(index);
    }
}
