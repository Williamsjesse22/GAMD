using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpaceRace.Physics;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// HOOK (bonus feature: space debris). Periodically spawns <see cref="Debris"/>
/// bodies on the course perimeter and despawns them when their lifetime ends.
/// <see cref="SpawnsPerSecond"/> defaults to 0 (disabled) — flip to e.g. 0.5 to
/// activate a low-density debris field.
/// </summary>
public sealed class DebrisSpawner : GameComponent
{
    private readonly BepuWorld _world;
    private readonly Random _rng = new();
    private readonly List<Debris> _active = new();
    private float _spawnAccumulator;

    /// <summary>Spawn rate. v1 = 0 (off).</summary>
    public float SpawnsPerSecond { get; set; } = 0f;

    /// <summary>Center of the course perimeter sphere from which debris spawns.</summary>
    public NumVector3 CourseCenter { get; set; } = NumVector3.Zero;

    /// <summary>Radius of the spawn sphere; debris fly in toward the center.</summary>
    public float CourseRadius { get; set; } = 250f;

    /// <summary>Base drift speed (m/s); each debris randomized 0.5×–1.5× this value.</summary>
    public float DriftSpeed { get; set; } = 6f;

    public DebrisSpawner(Game game, BepuWorld world) : base(game) { _world = world; }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _spawnAccumulator += SpawnsPerSecond * dt;
        while (_spawnAccumulator >= 1f)
        {
            _spawnAccumulator -= 1f;
            SpawnOne();
        }

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            _active[i].Tick(dt);
            if (_active[i].IsExpired)
            {
                _active[i].Remove();
                _active.RemoveAt(i);
            }
        }
    }

    private void SpawnOne()
    {
        NumVector3 dir = RandomUnitVector();
        NumVector3 spawn = CourseCenter + dir * CourseRadius;
        float speed = DriftSpeed * (0.5f + (float)_rng.NextDouble());
        NumVector3 vel = -dir * speed;
        float radius = 0.4f + 0.3f * (float)_rng.NextDouble();
        _active.Add(new Debris(_world, spawn, vel, radius, lifetime: 30f));
    }

    private NumVector3 RandomUnitVector()
    {
        // Marsaglia method for uniform points on a unit sphere.
        float u = (float)_rng.NextDouble() * 2f - 1f;
        float theta = (float)_rng.NextDouble() * MathHelper.TwoPi;
        float r = MathF.Sqrt(1f - u * u);
        return new NumVector3(r * MathF.Cos(theta), r * MathF.Sin(theta), u);
    }
}
