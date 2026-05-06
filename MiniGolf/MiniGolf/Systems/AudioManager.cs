using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace MiniGolf.Systems;

/// <summary>
/// Loads and plays the three required sound effects: ball struck, ball bouncing
/// off a wall/obstacle, and ball dropping into the hole.
///
/// Bypasses MGCB and reads <c>.wav</c> files directly via <see cref="WavLoader"/>
/// so each clip can have a per-channel <c>OffsetSeconds</c> trim (skip leading
/// silence) and a per-instance <see cref="MaxClipSeconds"/> cap (cut off after
/// N seconds, so a multi-shot WAV only plays the first shot).
/// </summary>
public sealed class AudioManager
{
    private SoundEffect? _strike;
    private SoundEffect? _bounce;
    private SoundEffect? _drop;

    /// <summary>Time-gate to avoid spamming the bounce SFX during deep penetrations.</summary>
    private double _lastBounceSeconds;

    /// <summary>Maximum seconds any one-shot clip will play before being cut off. 0 = play to end.</summary>
    public float MaxClipSeconds { get; set; } = 0.5f;

    /// <summary>Seconds of leading audio to skip in <c>strike.wav</c>. Tunes for WAVs with silence at the start.</summary>
    public float StrikeOffsetSeconds { get; set; } = 0.5f;

    /// <summary>Seconds of leading audio to skip in <c>bounce.wav</c>.</summary>
    public float BounceOffsetSeconds { get; set; } = 0.5f;

    /// <summary>Seconds of leading audio to skip in <c>drop.wav</c>.</summary>
    public float DropOffsetSeconds { get; set; } = 0.0f;

    /// <summary>Active instances and remaining play time (seconds); ticked from <see cref="Tick"/>.</summary>
    private readonly List<TimedInstance> _playing = new();

    /// <summary>Load the three WAV files from <c>{ContentRoot}/Sounds/</c> with their offsets applied.</summary>
    public void Load(ContentManager content)
    {
        string baseDir = Path.Combine(AppContext.BaseDirectory, content.RootDirectory, "Sounds");
        _strike = WavLoader.LoadWithOffset(Path.Combine(baseDir, "strike.wav"), StrikeOffsetSeconds);
        _bounce = WavLoader.LoadWithOffset(Path.Combine(baseDir, "bounce.wav"), BounceOffsetSeconds);
        _drop = WavLoader.LoadWithOffset(Path.Combine(baseDir, "drop.wav"), DropOffsetSeconds);
    }

    /// <summary>Tick active instance timers; call once per frame from Game1.</summary>
    public void Tick(float dt)
    {
        for (int i = _playing.Count - 1; i >= 0; i--)
        {
            var t = _playing[i];
            t.Remaining -= dt;
            if (t.Remaining <= 0f)
            {
                t.Instance.Stop();
                t.Instance.Dispose();
                _playing.RemoveAt(i);
            }
            else
            {
                _playing[i] = t;
            }
        }
    }

    /// <summary>Plays the strike SFX (ball hit by club).</summary>
    public void PlayStrike() => PlayClipped(_strike);

    /// <summary>Plays the bounce SFX, throttled to at most ~10 times/second.</summary>
    public void PlayBounce(double currentTimeSeconds)
    {
        if (currentTimeSeconds - _lastBounceSeconds < 0.1) return;
        _lastBounceSeconds = currentTimeSeconds;
        PlayClipped(_bounce);
    }

    /// <summary>Plays the drop SFX (ball into hole).</summary>
    public void PlayDrop() => PlayClipped(_drop);

    private void PlayClipped(SoundEffect? sfx)
    {
        if (sfx is null) return;
        var instance = sfx.CreateInstance();
        instance.Play();
        float fullLength = (float)sfx.Duration.TotalSeconds;
        float playFor = MaxClipSeconds > 0f ? MathF.Min(MaxClipSeconds, fullLength) : fullLength;
        _playing.Add(new TimedInstance(instance, playFor));
    }

    private struct TimedInstance
    {
        public SoundEffectInstance Instance;
        public float Remaining;
        public TimedInstance(SoundEffectInstance instance, float remaining)
        {
            Instance = instance;
            Remaining = remaining;
        }
    }
}
