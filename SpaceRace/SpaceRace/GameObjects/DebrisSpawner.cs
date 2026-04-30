using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpaceRace.Graphics;
using SpaceRace.Physics;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// Periodically spawns <see cref="Debris"/> bodies on the course perimeter and
/// despawns them when their lifetime ends. Each spawned debris is added to
/// <c>Game.Components</c> automatically so it gets ticked + rendered.
/// Set <see cref="SpawnsPerSecond"/> to 0 to pause spawning.
/// </summary>
public sealed class DebrisSpawner : GameComponent
{
    private readonly BepuWorld _world;
    private readonly PrimitiveRenderer _renderer;
    private readonly Random _rng = new();
    private readonly List<Debris> _active = new();
    private float _spawnAccumulator;

    /// <summary>Spawns per second. 0 disables spawning.</summary>
    public float SpawnsPerSecond { get; set; } = 0f;

    /// <summary>Center of the spawn sphere; debris fly toward it then past.</summary>
    public NumVector3 CourseCenter { get; set; } = new(0, 0, -110);

    /// <summary>Radius of the spawn sphere (debris start at this distance, fly inward).</summary>
    public float CourseRadius { get; set; } = 220f;

    /// <summary>Base drift speed (m/s); each debris randomized 0.5×–1.5× this value.</summary>
    public float DriftSpeed { get; set; } = 8f;

    public DebrisSpawner(Game game, BepuWorld world, PrimitiveRenderer renderer) : base(game)
    {
        _world = world;
        _renderer = renderer;
    }

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
            if (_active[i].IsExpired)
            {
                _active[i].RemoveFromSimulation();
                Game.Components.Remove(_active[i]);
                _active[i].Dispose();
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
        float radius = 0.6f + 0.5f * (float)_rng.NextDouble();
        Color color = new(
            (byte)_rng.Next(110, 180),
            (byte)_rng.Next(80, 140),
            (byte)_rng.Next(50, 100));
        var debris = new Debris(Game, _world, _renderer, spawn, vel, radius, lifetime: 35f, color);
        _active.Add(debris);
        Game.Components.Add(debris);
    }

    private NumVector3 RandomUnitVector()
    {
        float u = (float)_rng.NextDouble() * 2f - 1f;
        float theta = (float)_rng.NextDouble() * MathHelper.TwoPi;
        float r = MathF.Sqrt(1f - u * u);
        return new NumVector3(r * MathF.Cos(theta), r * MathF.Sin(theta), u);
    }
}
