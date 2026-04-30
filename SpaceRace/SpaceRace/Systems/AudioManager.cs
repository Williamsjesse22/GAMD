using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace SpaceRace.Systems;

/// <summary>
/// Loads and plays the race game's sound effects. Each clip try-loads with a
/// graceful fallback so missing WAV files leave the channel silent instead of
/// crashing the game. Channels: ring pass, ship hit, race finish, engine loop.
/// Supports a per-AudioManager <see cref="MaxClipSeconds"/> truncation so WAV
/// files that contain the same sound repeated multiple times don't play their
/// full length on every trigger.
/// </summary>
public sealed class AudioManager
{
    private SoundEffect? _ringPass;
    private SoundEffect? _hit;
    private SoundEffect? _finish;
    private SoundEffectInstance? _engineLoop;

    /// <summary>Maximum seconds any one-shot clip will play before being cut off. 0 = play to end.</summary>
    public float MaxClipSeconds { get; set; } = 0.5f;

    /// <summary>Time-gate to avoid spamming the hit SFX on multi-frame contacts.</summary>
    private double _lastHitSeconds;

    /// <summary>Active instances and their remaining play time in seconds; ticked from <see cref="Tick"/>.</summary>
    private readonly List<TimedInstance> _playing = new();

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

    public void PlayRingPass() => PlayClipped(_ringPass);

    public void PlayHit(double currentTimeSeconds)
    {
        if (currentTimeSeconds - _lastHitSeconds < 0.1) return;
        _lastHitSeconds = currentTimeSeconds;
        PlayClipped(_hit);
    }

    public void PlayFinish() => PlayClipped(_finish);

    public void StartEngine() => _engineLoop?.Play();
    public void StopEngine() => _engineLoop?.Stop();

    /// <summary>Plays <paramref name="sfx"/> via a tracked instance; auto-stops at <see cref="MaxClipSeconds"/>.</summary>
    private void PlayClipped(SoundEffect? sfx)
    {
        if (sfx is null) return;
        var instance = sfx.CreateInstance();
        instance.Play();

        float fullLength = (float)sfx.Duration.TotalSeconds;
        float playFor = MaxClipSeconds > 0f ? MathF.Min(MaxClipSeconds, fullLength) : fullLength;
        _playing.Add(new TimedInstance(instance, playFor));
    }

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
