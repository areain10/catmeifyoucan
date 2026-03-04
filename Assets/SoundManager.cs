using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop this on a single GameObject in your scene (or in a persistent scene).
/// Call from anywhere with:
///
///   SoundManager.Play("Jump");
///   SoundManager.Play("Tag", transform.position);   // positional 3D audio
///   SoundManager.PlayWithVolume("Footstep", 0.4f);
///   SoundManager.PlayWithPitch("Coin", 1.2f);
///   SoundManager.Stop("Music");
///
/// Add your clips in the Inspector under "Sound Library".
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class SoundEntry
    {
        public string name;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;

        [Tooltip("If true, this sound loops (good for music or ambience).")]
        public bool loop = false;

        [Tooltip("0 = fully 2D (UI sounds), 1 = fully 3D (world sounds).")]
        [Range(0f, 1f)]
        public float spatialBlend = 0f;

        // Runtime — the pooled AudioSource playing this sound
        [System.NonSerialized]
        public AudioSource source;
    }

    [Header("Sound Library")]
    public List<SoundEntry> sounds = new List<SoundEntry>();

    [Header("Pool Settings")]
    [Tooltip("How many one-shot AudioSources to pre-create for overlapping sounds.")]
    public int poolSize = 10;

    // Pool for fire-and-forget one-shot sounds
    List<AudioSource> pool = new List<AudioSource>();

    // Quick lookup by name
    Dictionary<string, SoundEntry> lookup = new Dictionary<string, SoundEntry>();

    // ---------------------------------------------------------------
    // Unity
    // ---------------------------------------------------------------

    void Awake()
    {
        // Singleton — survives scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build lookup dictionary
        foreach (var s in sounds)
        {
            if (!string.IsNullOrEmpty(s.name) && !lookup.ContainsKey(s.name))
                lookup[s.name] = s;
        }

        // Pre-warm the pool
        for (int i = 0; i < poolSize; i++)
            pool.Add(CreatePooledSource());
    }

    // ---------------------------------------------------------------
    // Public API  (static wrappers so you never need a reference)
    // ---------------------------------------------------------------

    /// <summary>Play a sound by name (2D, at the manager's position).</summary>
    public static void Play(string name)
        => Instance?.PlayInternal(name, null, null, null);

    /// <summary>Play a sound at a world position (3D spatial audio).</summary>
    public static void Play(string name, Vector3 position)
        => Instance?.PlayInternal(name, position, null, null);

    /// <summary>Play with a custom volume override (0–1).</summary>
    public static void PlayWithVolume(string name, float volume)
        => Instance?.PlayInternal(name, null, volume, null);

    /// <summary>Play with a custom pitch override.</summary>
    public static void PlayWithPitch(string name, float pitch)
        => Instance?.PlayInternal(name, null, null, pitch);

    /// <summary>Play with full overrides.</summary>
    public static void PlayFull(string name, Vector3? position = null, float? volume = null, float? pitch = null)
        => Instance?.PlayInternal(name, position, volume, pitch);

    /// <summary>Stop a looping sound by name.</summary>
    public static void Stop(string name)
        => Instance?.StopInternal(name);

    /// <summary>Stop ALL sounds immediately.</summary>
    public static void StopAll()
        => Instance?.StopAllInternal();

    /// <summary>Check if a looping sound is currently playing.</summary>
    public static bool IsPlaying(string name)
        => Instance != null && Instance.IsPlayingInternal(name);

    // ---------------------------------------------------------------
    // Internal implementation
    // ---------------------------------------------------------------

    void PlayInternal(string name, Vector3? position, float? volumeOverride, float? pitchOverride)
    {
        if (!lookup.TryGetValue(name, out SoundEntry entry))
        {
            Debug.LogWarning($"SoundManager: sound '{name}' not found in library.");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"SoundManager: sound '{name}' has no AudioClip assigned.");
            return;
        }

        if (entry.loop)
        {
            // Looping sounds get their own dedicated AudioSource
            PlayLooping(entry, position, volumeOverride, pitchOverride);
        }
        else
        {
            // One-shots go through the pool
            PlayOneShot(entry, position, volumeOverride, pitchOverride);
        }
    }

    void PlayOneShot(SoundEntry entry, Vector3? position, float? volumeOverride, float? pitchOverride)
    {
        AudioSource src = GetPooledSource();
        if (src == null) return;

        src.clip = entry.clip;
        src.volume = volumeOverride ?? entry.volume;
        src.pitch = pitchOverride ?? entry.pitch;
        src.loop = false;
        src.spatialBlend = entry.spatialBlend;

        if (position.HasValue)
        {
            src.transform.position = position.Value;
            src.spatialBlend = Mathf.Max(entry.spatialBlend, 1f); // force 3D if position given
        }
        else
        {
            src.transform.position = transform.position;
        }

        src.Play();
    }

    void PlayLooping(SoundEntry entry, Vector3? position, float? volumeOverride, float? pitchOverride)
    {
        // If already playing, just update values
        if (entry.source != null && entry.source.isPlaying)
        {
            entry.source.volume = volumeOverride ?? entry.volume;
            entry.source.pitch = pitchOverride ?? entry.pitch;
            return;
        }

        // Create a dedicated source for this looping sound
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = entry.clip;
        src.volume = volumeOverride ?? entry.volume;
        src.pitch = pitchOverride ?? entry.pitch;
        src.loop = true;
        src.spatialBlend = entry.spatialBlend;
        src.Play();

        entry.source = src;
    }

    void StopInternal(string name)
    {
        if (!lookup.TryGetValue(name, out SoundEntry entry)) return;

        if (entry.source != null)
        {
            entry.source.Stop();
            Destroy(entry.source);
            entry.source = null;
        }
    }

    void StopAllInternal()
    {
        foreach (var src in pool)
            if (src.isPlaying) src.Stop();

        foreach (var entry in sounds)
        {
            if (entry.source != null)
            {
                entry.source.Stop();
                Destroy(entry.source);
                entry.source = null;
            }
        }
    }

    bool IsPlayingInternal(string name)
    {
        if (!lookup.TryGetValue(name, out SoundEntry entry)) return false;
        return entry.source != null && entry.source.isPlaying;
    }

    // ---------------------------------------------------------------
    // Pool helpers
    // ---------------------------------------------------------------

    AudioSource CreatePooledSource()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        return src;
    }

    AudioSource GetPooledSource()
    {
        // Find a free (not playing) source in the pool
        foreach (var src in pool)
            if (!src.isPlaying) return src;

        // Pool exhausted — expand it
        Debug.LogWarning("SoundManager: pool exhausted, expanding. Consider increasing Pool Size.");
        var newSrc = CreatePooledSource();
        pool.Add(newSrc);
        return newSrc;
    }
}