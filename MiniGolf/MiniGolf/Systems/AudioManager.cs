using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace MiniGolf.Systems;

/// <summary>
/// Loads and plays the three required sound effects: ball struck, ball bouncing
/// off a wall/obstacle, and ball dropping into the hole. Loads via the MGCB
/// content pipeline; if a clip is missing, that channel is silently disabled
/// (so the game still runs while WAV assets are being added).
/// </summary>
public sealed class AudioManager
{
    private SoundEffect? _strike;
    private SoundEffect? _bounce;
    private SoundEffect? _drop;

    /// <summary>Time-gate to avoid spamming the bounce SFX during deep penetrations.</summary>
    private double _lastBounceSeconds;

    /// <summary>Try-load each clip. Missing files leave the channel null (silent).</summary>
    public void Load(ContentManager content)
    {
        _strike = TryLoad(content, "Sounds/strike");
        _bounce = TryLoad(content, "Sounds/bounce");
        _drop = TryLoad(content, "Sounds/drop");
    }

    /// <summary>Plays the strike SFX (ball hit by club).</summary>
    public void PlayStrike() => _strike?.Play();

    /// <summary>Plays the bounce SFX, throttled to at most ~10 times/second.</summary>
    public void PlayBounce(double currentTimeSeconds)
    {
        if (currentTimeSeconds - _lastBounceSeconds < 0.1) return;
        _lastBounceSeconds = currentTimeSeconds;
        _bounce?.Play();
    }

    /// <summary>Plays the drop SFX (ball into hole).</summary>
    public void PlayDrop() => _drop?.Play();

    private static SoundEffect? TryLoad(ContentManager content, string name)
    {
        try
        {
            return content.Load<SoundEffect>(name);
        }
        catch (ContentLoadException)
        {
            // Asset not present yet — silent fallback, keep the rest of the game running.
            return null;
        }
    }
}
