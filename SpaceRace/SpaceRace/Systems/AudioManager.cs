using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace SpaceRace.Systems;

/// <summary>
/// Loads and plays the race game's sound effects. Mirrors the 1A pattern:
/// each clip try-loads with a graceful fallback so missing WAV files leave the
/// channel silent instead of crashing the game. Channels: ring pass, ship hit,
/// race finish. Engine ambient is a hook for later.
/// </summary>
public sealed class AudioManager
{
    private SoundEffect? _ringPass;
    private SoundEffect? _hit;
    private SoundEffect? _finish;
    private SoundEffectInstance? _engineLoop;

    /// <summary>Time-gate to avoid spamming the hit SFX during deep penetrations.</summary>
    private double _lastHitSeconds;

    /// <summary>Try-load each clip from Content/Sounds/. Missing files leave that channel null (silent).</summary>
    public void Load(ContentManager content)
    {
        _ringPass = TryLoad(content, "Sounds/ringPass");
        _hit = TryLoad(content, "Sounds/hit");
        _finish = TryLoad(content, "Sounds/finish");
        var engine = TryLoad(content, "Sounds/engine");
        if (engine != null)
        {
            _engineLoop = engine.CreateInstance();
            _engineLoop.IsLooped = true;
            _engineLoop.Volume = 0.4f;
        }
    }

    /// <summary>Play the ring-pass SFX (player flew through a ring in order).</summary>
    public void PlayRingPass() => _ringPass?.Play();

    /// <summary>Play the hit SFX (ship collided with an obstacle); throttled to ~10/s.</summary>
    public void PlayHit(double currentTimeSeconds)
    {
        if (currentTimeSeconds - _lastHitSeconds < 0.1) return;
        _lastHitSeconds = currentTimeSeconds;
        _hit?.Play();
    }

    /// <summary>Play the race-finish SFX once.</summary>
    public void PlayFinish() => _finish?.Play();

    /// <summary>Start the engine loop (hook for thrust audio; safe no-op if no clip).</summary>
    public void StartEngine() => _engineLoop?.Play();

    /// <summary>Stop the engine loop.</summary>
    public void StopEngine() => _engineLoop?.Stop();

    private static SoundEffect? TryLoad(ContentManager content, string name)
    {
        try
        {
            return content.Load<SoundEffect>(name);
        }
        catch (ContentLoadException)
        {
            return null;
        }
    }
}
