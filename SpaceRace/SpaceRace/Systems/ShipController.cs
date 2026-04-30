using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SpaceRace.GameObjects;
using SpaceRace.Graphics;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.Systems;

/// <summary>
/// Reads keyboard + gamepad each tick and turns input into forces / torques on
/// the ship body. Per the spec, only impulses and angular impulses are applied —
/// position and orientation are never set directly.
/// </summary>
public sealed class ShipController : GameComponent
{
    private readonly Ship _ship;
    private readonly Camera _camera;

    private KeyboardState _previousKeyboard;
    private GamePadState _previousGamepad;

    /// <summary>Forward thrust in m/s² when the thrust input is fully on.</summary>
    public float ThrustAcceleration { get; set; } = 18f;

    /// <summary>Pitch / yaw / roll torque magnitude in (rad/s²)·kg·m² when input is fully deflected.</summary>
    public float TurnTorque { get; set; } = 1.5f;

    /// <summary>Auto-level torque magnitude (X key); damps current angular velocity toward zero.</summary>
    public float AutoLevelStrength { get; set; } = 8f;

    /// <summary>Optional: gate thrust on fuel > 0. Bonus hook; no-op while fuel stays full.</summary>
    public bool RequireFuel { get; set; } = false;

    /// <summary>Burn rate in fuel-units/sec while thrust is engaged (used only if RequireFuel).</summary>
    public float FuelBurnRate { get; set; } = 0.05f;

    /// <summary>Hook for laser/torpedo fire; v1 disabled.</summary>
    public bool CanFire { get; set; } = false;

    public ShipController(Game game, Ship ship, Camera camera) : base(game)
    {
        _ship = ship;
        _camera = camera;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboard = Keyboard.GetState();
        var gamepad = GamePad.GetState(PlayerIndex.One);

        // Camera mode toggle (C, edge-triggered) + right-stick-click as gamepad alt.
        if ((keyboard.IsKeyDown(Keys.C) && !_previousKeyboard.IsKeyDown(Keys.C)) ||
            (gamepad.Buttons.RightStick == ButtonState.Pressed && _previousGamepad.Buttons.RightStick != ButtonState.Pressed))
        {
            _camera.ToggleMode();
        }

        // ===== Input axes =====
        // Pitch: W = nose up, S = nose down (flight-sim "pull back to climb").
        float pitch = (keyboard.IsKeyDown(Keys.W) ? 1 : 0) - (keyboard.IsKeyDown(Keys.S) ? 1 : 0);
        // Yaw: A = yaw right, D = yaw left.
        float yaw = (keyboard.IsKeyDown(Keys.A) ? 1 : 0) - (keyboard.IsKeyDown(Keys.D) ? 1 : 0);
        // Roll: Q (left) / E (right).
        float roll = (keyboard.IsKeyDown(Keys.E) ? 1 : 0) - (keyboard.IsKeyDown(Keys.Q) ? 1 : 0);
        // Thrust: Space = forward, LShift = reverse. Range -1..+1.
        float thrust = (keyboard.IsKeyDown(Keys.Space) ? 1f : 0f)
                     - (keyboard.IsKeyDown(Keys.LeftShift) ? 1f : 0f);

        // Gamepad mirrors: left stick X=yaw, Y=pitch; right stick X=roll;
        // right trigger=forward thrust, left trigger=reverse thrust.
        if (gamepad.IsConnected)
        {
            yaw += gamepad.ThumbSticks.Left.X;
            pitch += -gamepad.ThumbSticks.Left.Y;
            roll += gamepad.ThumbSticks.Right.X;
            thrust += gamepad.Triggers.Right - gamepad.Triggers.Left;
        }

        // Clamp to [-1, 1] in case keyboard + gamepad both push.
        pitch = MathHelper.Clamp(pitch, -1, 1);
        yaw = MathHelper.Clamp(yaw, -1, 1);
        roll = MathHelper.Clamp(roll, -1, 1);
        thrust = MathHelper.Clamp(thrust, -1, 1);

        // ===== Apply forces & torques =====
        var bodyRef = _ship.Body;

        // Local-space torque vector. In Xna's RH convention with -Z forward:
        //   pitch = rotation around local X
        //   yaw   = rotation around local Y
        //   roll  = rotation around local Z
        var localTorque = new NumVector3(pitch, yaw, roll) * TurnTorque;

        // Local thrust along -Z (forward) or +Z (reverse). Gate on fuel if requested.
        float effectiveThrust = thrust;
        if (RequireFuel)
        {
            if (_ship.Fuel <= 0f) effectiveThrust = 0f;
            else _ship.Fuel = MathF.Max(0f, _ship.Fuel - FuelBurnRate * MathF.Abs(thrust) * dt);
        }
        var localForce = new NumVector3(0, 0, -effectiveThrust * ThrustAcceleration * _ship.Mass);

        // Transform local torque + force to world space using current orientation.
        var orientation = bodyRef.Pose.Orientation;
        var worldTorque = NumVector3.Transform(localTorque, orientation);
        var worldImpulse = NumVector3.Transform(localForce * dt, orientation);

        bodyRef.ApplyImpulse(worldImpulse, NumVector3.Zero);
        bodyRef.ApplyAngularImpulse(worldTorque * dt);

        // Auto-level (X): apply angular impulse opposite to current angular velocity.
        if (keyboard.IsKeyDown(Keys.X))
        {
            var counter = -bodyRef.Velocity.Angular * AutoLevelStrength * dt;
            bodyRef.ApplyAngularImpulse(counter);
        }

        // Wake the body so impulses are honored even if it had been sleeping.
        bodyRef.Awake = true;

        _previousKeyboard = keyboard;
        _previousGamepad = gamepad;
    }
}
