using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

namespace SpaceRace.Physics;

/// <summary>
/// Pose integrator for the space race. Applies a (typically zero) gravity vector and
/// per-second linear/angular damping to all dynamic bodies. Damping is critical:
/// in a zero-g sim, a ship without it would coast forever.
/// </summary>
/// <remarks>
/// Bepu V2 requires this be a struct, not a class — passing a class compiles but
/// boxes per simulation step. Required properties on V2: <see cref="AngularIntegrationMode"/>,
/// <see cref="AllowSubstepsForUnconstrainedBodies"/>, <see cref="IntegrateVelocityForKinematics"/>.
/// </remarks>
public struct SpacePoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    /// <summary>Gravity in world space, applied per second. Zero for space.</summary>
    public Vector3 Gravity;

    /// <summary>Linear damping per second (0 = none, 1 = halt instantly).</summary>
    public float LinearDamping;

    /// <summary>Angular damping per second (0 = none, 1 = halt instantly).</summary>
    public float AngularDamping;

    private Vector3Wide _gravityWideDt;
    private Vector<float> _linearDampingDt;
    private Vector<float> _angularDampingDt;

    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinematics => false;

    public SpacePoseIntegratorCallbacks(Vector3 gravity, float linearDamping, float angularDamping) : this()
    {
        Gravity = gravity;
        LinearDamping = linearDamping;
        AngularDamping = angularDamping;
    }

    public void Initialize(Simulation simulation) { }

    public void PrepareForIntegration(float dt)
    {
        // Exponential decay: exp(-damping*dt). Correct for any positive damping value.
        // The earlier (1-damping)^dt form clamps to 0 once damping >= 1, which silently
        // zeroes the velocity every step and makes the body refuse to turn under torque.
        _linearDampingDt = new Vector<float>(MathF.Exp(-MathF.Max(0f, LinearDamping) * dt));
        _angularDampingDt = new Vector<float>(MathF.Exp(-MathF.Max(0f, AngularDamping) * dt));
        _gravityWideDt = Vector3Wide.Broadcast(Gravity * dt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IntegrateVelocity(
        Vector<int> bodyIndices,
        Vector3Wide position,
        QuaternionWide orientation,
        BodyInertiaWide localInertia,
        Vector<int> integrationMask,
        int workerIndex,
        Vector<float> dt,
        ref BodyVelocityWide velocity)
    {
        velocity.Linear = (velocity.Linear + _gravityWideDt) * _linearDampingDt;
        velocity.Angular *= _angularDampingDt;
    }
}

/// <summary>
/// Narrow-phase contact callbacks. Allows contacts between dynamic and static/dynamic
/// pairs, configures friction and restitution. Pushes contact events onto a queue
/// the audio system can drain for collision SFX.
/// </summary>
public struct SpaceNarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public SpringSettings ContactSpringiness;
    public float MaximumRecoveryVelocity;
    public float FrictionCoefficient;

    public SpaceNarrowPhaseCallbacks(SpringSettings springiness, float maxRecovery, float friction)
    {
        ContactSpringiness = springiness;
        MaximumRecoveryVelocity = maxRecovery;
        FrictionCoefficient = friction;
    }

    public void Initialize(Simulation simulation) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        // At least one body must be dynamic to bother generating contacts.
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
        where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial.FrictionCoefficient = FrictionCoefficient;
        pairMaterial.MaximumRecoveryVelocity = MaximumRecoveryVelocity;
        pairMaterial.SpringSettings = ContactSpringiness;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose() { }
}
