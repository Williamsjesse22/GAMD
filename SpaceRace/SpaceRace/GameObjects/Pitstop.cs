using System;
using Microsoft.Xna.Framework;
using SpaceRace.Physics;

namespace SpaceRace.GameObjects;

/// <summary>
/// HOOK (bonus feature: fuel + pitstops). Reuses the generic
/// <see cref="Trigger"/> volume to detect ship pass-through. Subscribers wire
/// into <see cref="OnPassed"/> to apply a fuel refill or other effect.
/// Not spawned in v1; turn on by instantiating in <c>Game1.LoadContent</c>.
/// </summary>
public sealed class Pitstop
{
    /// <summary>Pass-through volume — same geometry as a ring, smaller radius.</summary>
    public Trigger Trigger { get; }

    /// <summary>Amount of fuel restored on a successful pass (0..1).</summary>
    public float RefuelAmount { get; set; } = 0.5f;

    /// <summary>Fired when the ship passes through this pitstop in the +Axis direction.</summary>
    public event Action<Ship>? OnPassed;

    public Pitstop(Vector3 center, Vector3 axis, float innerRadius)
    {
        Trigger = new Trigger(center, axis, innerRadius);
    }

    /// <summary>Game1 hook: call each tick with the ship's previous and current positions.</summary>
    public void Tick(Ship ship, Vector3 previousShipPosition, Vector3 currentShipPosition)
    {
        if (!Trigger.TryPassThrough(previousShipPosition, currentShipPosition, out int direction)) return;
        if (direction <= 0) return;
        ship.Fuel = MathHelper.Clamp(ship.Fuel + RefuelAmount, 0f, 1f);
        OnPassed?.Invoke(ship);
    }
}
