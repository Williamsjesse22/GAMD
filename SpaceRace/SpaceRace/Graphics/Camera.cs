using System;
using BepuPhysics;
using Microsoft.Xna.Framework;

namespace SpaceRace.Graphics;

/// <summary>
/// Owns view + projection matrices. Two modes: chase (follows behind/above the ship)
/// and first-person (just inside the cockpit). Toggle by setting <see cref="Mode"/> —
/// the host (Game1) is responsible for binding a key to the toggle.
/// </summary>
public sealed class Camera
{
    public enum CameraMode { Chase, FirstPerson }

    /// <summary>Active camera mode.</summary>
    public CameraMode Mode { get; set; } = CameraMode.Chase;

    /// <summary>Vertical field of view in radians.</summary>
    public float Fov { get; set; } = MathHelper.PiOver4;

    /// <summary>Near clip plane. Wide enough that chase cam doesn't clip the ship.</summary>
    public float Near { get; set; } = 0.3f;

    /// <summary>Far clip plane. Skybox is rendered before world geometry, so this can be modest.</summary>
    public float Far { get; set; } = 2000f;

    /// <summary>Aspect ratio (width / height). Set on viewport changes.</summary>
    public float Aspect { get; set; } = 16f / 9f;

    /// <summary>Smoothing factor for chase-cam follow (0 = snap, 1 = never move). Default 0.15 = mildly smoothed.</summary>
    public float ChaseSmoothing { get; set; } = 0.15f;

    /// <summary>Local offset of the chase cam from the ship: behind + above.</summary>
    public Vector3 ChaseOffsetLocal { get; set; } = new(0f, 1.5f, 6f);

    /// <summary>Local offset of the first-person cam (just at the ship's nose, slightly above).</summary>
    public Vector3 FirstPersonOffsetLocal { get; set; } = new(0f, 0.25f, -0.4f);

    /// <summary>Current world-space camera position. Read by skybox component.</summary>
    public Vector3 Position { get; private set; }

    /// <summary>Current view matrix.</summary>
    public Matrix View { get; private set; } = Matrix.Identity;

    /// <summary>Current projection matrix.</summary>
    public Matrix Projection { get; private set; } = Matrix.Identity;

    /// <summary>
    /// Update camera pose to follow a body's pose. Call each frame after physics step.
    /// </summary>
    public void Follow(RigidPose shipPose)
    {
        Matrix shipWorld = shipPose.ToWorldMatrix();
        Vector3 shipForward = -shipWorld.Forward; // Xna's "Forward" is -Z; double-flip for clarity.
        // Xna's Matrix.Forward is already -Z basis vector — use it directly.
        Vector3 forward = shipWorld.Forward;
        Vector3 up = shipWorld.Up;

        Vector3 desiredPos;
        Vector3 lookAt;
        if (Mode == CameraMode.Chase)
        {
            // Chase: behind ship along its local -forward, above along its local up.
            Vector3 worldOffset = -forward * ChaseOffsetLocal.Z + up * ChaseOffsetLocal.Y + shipWorld.Right * ChaseOffsetLocal.X;
            desiredPos = shipPose.Position.ToXna() + worldOffset;
            lookAt = shipPose.Position.ToXna() + forward * 4f;
            // Smooth toward desired position.
            Position = Vector3.Lerp(Position, desiredPos, 1f - ChaseSmoothing);
        }
        else
        {
            // First-person: snap to nose, no smoothing.
            Vector3 worldOffset = forward * (-FirstPersonOffsetLocal.Z) + up * FirstPersonOffsetLocal.Y + shipWorld.Right * FirstPersonOffsetLocal.X;
            desiredPos = shipPose.Position.ToXna() + worldOffset;
            Position = desiredPos;
            lookAt = Position + forward;
        }

        View = Matrix.CreateLookAt(Position, lookAt, up);
        Projection = Matrix.CreatePerspectiveFieldOfView(Fov, Aspect, Near, Far);
    }

    /// <summary>Static-camera helper for early-stage rendering before a ship exists.</summary>
    public void LookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        Position = position;
        View = Matrix.CreateLookAt(position, target, up);
        Projection = Matrix.CreatePerspectiveFieldOfView(Fov, Aspect, Near, Far);
    }

    /// <summary>Toggle between Chase and FirstPerson.</summary>
    public void ToggleMode()
    {
        Mode = Mode == CameraMode.Chase ? CameraMode.FirstPerson : CameraMode.Chase;
    }
}
