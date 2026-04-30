using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MiniGolf.Systems;

/// <summary>
/// Owns the timed aim + timed power input flow. Both the aim direction (32
/// quantized angles around the full circle) and the power level (16 levels)
/// cycle automatically while the ball is at rest; the player taps Space or LMB
/// to lock each one in. Two taps fire a shot. The HUD reads the public state
/// to render the aim line and power meter; <see cref="Game1"/> subscribes to
/// <see cref="ShotFired"/> to apply the impulse and increment the stroke count.
/// </summary>
public sealed class InputManager : GameComponent
{
    /// <summary>States in the aim → charge → launch flow.</summary>
    public enum InputState { Idle, Aiming, Charging, Launched }

    /// <summary>Quantization buckets for direction. 64 = 5.625° per step (2× the assignment minimum).</summary>
    public const int DirectionSteps = 64;

    /// <summary>Quantization buckets for power. 16 = assignment minimum.</summary>
    public const int PowerLevels = 16;

    /// <summary>Seconds for one full revolution of the aim sweep. 4s × 64 steps ≈ 62ms per angle.</summary>
    private const float AngleSweepSeconds = 4.0f;

    /// <summary>Seconds for the power meter to sweep 0 → max → 0.</summary>
    private const float ChargeCycleSeconds = 1.4f;

    /// <summary>Minimum impulse magnitude (px/s) — power level 1 of 16.</summary>
    public float MinPower { get; set; } = 220f;

    /// <summary>Maximum impulse magnitude (px/s) — power level 16 of 16.</summary>
    public float MaxPower { get; set; } = 1100f;

    /// <summary>Current input state.</summary>
    public InputState State { get; private set; } = InputState.Idle;

    /// <summary>Quantized aim direction (unit vector). Sweeps in Aiming, locked in Charging.</summary>
    public Vector2 AimDirection { get; private set; } = Vector2.UnitX;

    /// <summary>Power level 1..PowerLevels while charging, otherwise 0.</summary>
    public int PowerLevel { get; private set; }

    /// <summary>Normalized 0..1 raw charge value (for HUD bar visuals).</summary>
    public float ChargeFraction01 { get; private set; }

    /// <summary>Ball position. Driven by Game1 each tick; used by the HUD as the aim-line origin.</summary>
    public Vector2 BallPosition { get; set; }

    /// <summary>True when the ball is at rest and ready for input.</summary>
    public bool BallAtRest { get; set; } = true;

    /// <summary>Fired when the player locks in the power and launches the ball.</summary>
    public event Action<Vector2>? ShotFired;

    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private float _angleTimer;
    private float _chargeTimer;

    public InputManager(Game game) : base(game) { }

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        // Edge-trigger detection. Primary flow: LMB press locks angle + starts
        // charging; LMB release fires. Space provides a discrete-tap fallback
        // (tap to lock angle, tap again to fire) for keyboard-only play.
        bool lmbPressEdge = mouse.LeftButton == ButtonState.Pressed
                            && _previousMouse.LeftButton != ButtonState.Pressed;
        bool lmbReleaseEdge = mouse.LeftButton == ButtonState.Released
                              && _previousMouse.LeftButton != ButtonState.Released;
        bool spaceTap = keyboard.IsKeyDown(Keys.Space)
                        && !_previousKeyboard.IsKeyDown(Keys.Space);

        // Disable input while the ball is in motion.
        if (!BallAtRest)
        {
            State = InputState.Launched;
            ChargeFraction01 = 0f;
            PowerLevel = 0;
            _previousMouse = mouse;
            _previousKeyboard = keyboard;
            return;
        }

        // Auto-enter Aiming whenever the ball is freshly at rest.
        if (State == InputState.Idle || State == InputState.Launched)
        {
            State = InputState.Aiming;
            _angleTimer = 0f;
            _chargeTimer = 0f;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (State == InputState.Aiming)
        {
            _angleTimer += dt;
            // Map elapsed time to a 0..1 phase, then to one of 32 quantized angles.
            float phase = (_angleTimer / AngleSweepSeconds) % 1f;
            int step = (int)(phase * DirectionSteps) % DirectionSteps;
            float angle = step * (MathHelper.TwoPi / DirectionSteps);
            AimDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            if (lmbPressEdge || spaceTap)
            {
                State = InputState.Charging;
                _chargeTimer = 0f;
            }
        }
        else if (State == InputState.Charging)
        {
            _chargeTimer += dt;
            // Triangle wave 0 → 1 → 0 over ChargeCycleSeconds, then loop.
            float p = (_chargeTimer % ChargeCycleSeconds) / ChargeCycleSeconds;
            ChargeFraction01 = p < 0.5f ? p * 2f : (1f - p) * 2f;
            PowerLevel = QuantizePower(ChargeFraction01);

            if (lmbReleaseEdge || spaceTap) FireShot();
        }

        _previousMouse = mouse;
        _previousKeyboard = keyboard;
    }

    /// <summary>Maps a 0..1 fraction to integer power level 1..<see cref="PowerLevels"/>.</summary>
    private static int QuantizePower(float fraction01)
    {
        int level = (int)MathF.Ceiling(fraction01 * PowerLevels);
        return MathHelper.Clamp(level, 1, PowerLevels);
    }

    private void FireShot()
    {
        // Map quantized power level to actual speed (linear interpolation).
        float t = (PowerLevel - 1f) / (PowerLevels - 1f);
        float speed = MathHelper.Lerp(MinPower, MaxPower, t);
        Vector2 impulse = AimDirection * speed;

        State = InputState.Launched;
        ChargeFraction01 = 0f;
        PowerLevel = 0;
        ShotFired?.Invoke(impulse);
    }
}
