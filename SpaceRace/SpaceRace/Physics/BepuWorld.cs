using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

namespace SpaceRace.Physics;

/// <summary>
/// Wraps the Bepu V2 <see cref="Simulation"/> + its <see cref="BufferPool"/> +
/// <see cref="ThreadDispatcher"/>. Owns the lifetimes of all three; <c>Game1</c>
/// holds one of these and steps it once per fixed tick.
/// </summary>
/// <remarks>
/// In space the gravity vector is zero; the only damping forces come from the
/// pose integrator callbacks. Without damping a body that's been pushed once
/// would drift forever — that's correct physically, but disorienting to play.
/// </remarks>
public sealed class BepuWorld : IDisposable
{
    /// <summary>The underlying Bepu simulation. Game objects add bodies/statics directly.</summary>
    public Simulation Simulation { get; }

    /// <summary>Buffer pool used for all simulation allocations and any custom mesh data.</summary>
    public BufferPool BufferPool { get; }

    /// <summary>Thread dispatcher used for the parallel solver.</summary>
    public ThreadDispatcher ThreadDispatcher { get; }

    public BepuWorld(Vector3 gravity, float linearDamping = 0.4f, float angularDamping = 1.5f)
    {
        BufferPool = new BufferPool();
        // Leave 2 cores free for the rest of the engine + OS.
        int threadCount = Math.Max(1, Environment.ProcessorCount - 2);
        ThreadDispatcher = new ThreadDispatcher(threadCount);

        var poseCallbacks = new SpacePoseIntegratorCallbacks(gravity, linearDamping, angularDamping);
        var narrowCallbacks = new SpaceNarrowPhaseCallbacks(
            springiness: new SpringSettings(30f, 1f),
            maxRecovery: 2f,
            friction: 1f);

        Simulation = Simulation.Create(
            BufferPool,
            narrowCallbacks,
            poseCallbacks,
            new SolveDescription(velocityIterationCount: 8, substepCount: 1));
    }

    /// <summary>Advance the simulation by <paramref name="dt"/> seconds.</summary>
    /// <remarks>
    /// MonoGame's first <c>Update</c> tick can deliver <c>ElapsedGameTime = 0</c>;
    /// Bepu V2 throws on a non-positive dt. We also cap large dt values (e.g.
    /// after a debugger pause) to keep the integrator stable.
    /// </remarks>
    public void Step(float dt)
    {
        if (dt <= 0f) return;
        if (dt > 0.1f) dt = 0.1f;
        Simulation.Timestep(dt, ThreadDispatcher);
    }

    public void Dispose()
    {
        Simulation.Dispose();
        ThreadDispatcher.Dispose();
        BufferPool.Clear();
    }
}
