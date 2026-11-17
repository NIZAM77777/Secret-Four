using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretFour.Localization
{
    public static class AudioPreferences
    {
        public static event Action Changed;
        public static float Master => Mathf.Clamp01(PlayerPrefs.GetFloat("SecretFour.Master", .8f));
        public static float Music => Mathf.Clamp01(PlayerPrefs.GetFloat("SecretFour.Music", .25f));
        public static float Effects => Mathf.Clamp01(PlayerPrefs.GetFloat("SecretFour.Effects", .7f));
        public static bool Muted => PlayerPrefs.GetInt("SecretFour.Muted", 0) != 0;
        public static void SetMaster(float value) => Set("SecretFour.Master", value);
        public static void SetMusic(float value) => Set("SecretFour.Music", value);
        public static void SetEffects(float value) => Set("SecretFour.Effects", value);
        private static void Set(string key, float value) { PlayerPrefs.SetFloat(key, Mathf.Clamp01(value)); Changed?.Invoke(); }
        public static void SetMuted(bool value) { PlayerPrefs.SetInt("SecretFour.Muted", value ? 1 : 0); Changed?.Invoke(); }
        public static void Save() => PlayerPrefs.Save();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Changed = null;
    }

    /// <summary>
    /// Persistent music + sound-effect player (created automatically before the first scene).
    /// Menu and match each have their own music loop with a crossfade between them.
    /// Volumes follow Settings (Master, Music, Effects, Mute). Everything is silent while a
    /// full-screen ad is showing. Cues: click, back, place, lock, reveal, move, win, lose, draw,
    /// unlock, reward, select, error, notify.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private const string GameSceneName = "Game";
        private const float CrossfadeSeconds = 1.2f;
        private const int EffectVoices = 6;

        // name, file, volume, random pitch range
        private static readonly (string cue, string file, float volume, float pitchJitter)[] Cues =
        {
            ("click", "Click", .55f, .04f), ("back", "Back", .5f, .03f),
            ("place", "Place", .9f, .05f), ("lock", "Lock", .7f, 0f),
            ("reveal", "Reveal", .8f, 0f), ("move", "Move", .6f, .05f),
            ("win", "Win", 1f, 0f), ("lose", "Lose", .9f, 0f), ("draw", "Draw", .9f, 0f),
            ("unlock", "Unlock", 1f, 0f), ("reward", "Reward", .85f, 0f),
            ("select", "Select", .7f, .02f), ("error", "Error", .6f, 0f), ("notify", "Notify", .75f, 0f),
        };

        private static GameAudio instance;
        private static bool adPlaying;

        private readonly Dictionary<string, (AudioClip clip, float volume, float jitter)> clips = new();
        private readonly Dictionary<string, float> lastPlayed = new();
        private AudioSource[] voices;
        private int nextVoice;
        private AudioSource musicA, musicB, activeMusic;
        private AudioClip menuMusic, gameMusic;
        private float musicFade = 1f, duck = 1f, duckUntil;
        private Coroutine crossfade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { instance = null; adPlaying = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance == null) new GameObject("Game Audio").AddComponent<GameAudio>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);

            musicA = NewSource(true);
            musicB = NewSource(true);
            activeMusic = musicA;
            voices = new AudioSource[EffectVoices];
            for (int i = 0; i < voices.Length; i++) voices[i] = NewSource(false);

            menuMusic = Resources.Load<AudioClip>("Audio/MenuMusic");
            gameMusic = Resources.Load<AudioClip>("Audio/GameMusic");
            foreach (var (cue, file, volume, jitter) in Cues)
            {
                AudioClip clip = Resources.Load<AudioClip>("Audio/" + file);
                if (clip != null) clips[cue] = (clip, volume, jitter);
                else Debug.LogWarning($"[GameAudio] Missing sound Audio/{file}");
            }

            AudioPreferences.Changed += Apply;
            SceneManager.activeSceneChanged += HandleSceneChanged;
            Apply();
            PlayMusicFor(SceneManager.GetActiveScene().name, instant: true);
        }

        private AudioSource NewSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            if (loop) source.priority = 0;
            return source;
        }

        private void HandleSceneChanged(Scene previous, Scene next) => PlayMusicFor(next.name, instant: false);

        private void PlayMusicFor(string sceneName, bool instant)
        {
            AudioClip wanted = string.Equals(sceneName, GameSceneName, StringComparison.OrdinalIgnoreCase) ? gameMusic : menuMusic;
            if (wanted == null || (activeMusic.clip == wanted && activeMusic.isPlaying)) return;

            AudioSource from = activeMusic;
            AudioSource to = activeMusic == musicA ? musicB : musicA;
            to.clip = wanted;
            to.time = 0f;
            to.Play();
            activeMusic = to;

            if (crossfade != null) StopCoroutine(crossfade);
            if (instant || !from.isPlaying)
            {
                from.Stop();
                musicFade = 1f;
                Apply();
            }
            else crossfade = StartCoroutine(Crossfade(from));
        }

        private IEnumerator Crossfade(AudioSource from)
        {
            for (float t = 0; t < CrossfadeSeconds; t += Time.unscaledDeltaTime)
            {
                musicFade = t / CrossfadeSeconds;
                Apply();
                yield return null;
            }
            musicFade = 1f;
            from.Stop();
            Apply();
            crossfade = null;
        }

        private void Update()
        {
            // Music dips under win/lose/draw/unlock stings, then recovers smoothly.
            float target = Time.unscaledTime < duckUntil ? .35f : 1f;
            if (!Mathf.Approximately(duck, target))
            {
                duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * (target < duck ? 4f : .8f));
                Apply();
            }
        }

        private void Apply()
        {
            float master = AudioPreferences.Muted || adPlaying ? 0f : AudioPreferences.Master;
            float music = master * AudioPreferences.Music * duck;
            activeMusic.volume = music * musicFade;
            AudioSource other = activeMusic == musicA ? musicB : musicA;
            other.volume = music * (1f - musicFade);
            foreach (AudioSource voice in voices)
                voice.mute = master <= 0f;
        }

        /// <summary>Plays a named sound effect (see class summary for cue names).</summary>
        public static void Play(string cue)
        {
            if (instance == null || AudioPreferences.Muted || adPlaying || string.IsNullOrEmpty(cue)) return;
            instance.PlayCue(cue);
        }

        private void PlayCue(string cue)
        {
            if (!clips.TryGetValue(cue, out var entry))
            {
                if (cue == "click") return;
                PlayCue("click");
                return;
            }

            // Ignore the same cue fired twice in the same instant (e.g. two listeners on one event).
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(cue, out float last) && now - last < .05f) return;
            lastPlayed[cue] = now;

            float volume = AudioPreferences.Master * AudioPreferences.Effects * entry.volume;
            if (volume <= 0f) return;

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.Stop();
            voice.clip = entry.clip;
            voice.volume = volume;
            voice.pitch = entry.jitter > 0f ? 1f + UnityEngine.Random.Range(-entry.jitter, entry.jitter) : 1f;
            voice.Play();

            if (cue is "win" or "lose" or "draw" or "unlock")
                duckUntil = now + entry.clip.length * .8f;
        }

        /// <summary>Silences music and effects while a full-screen ad is on screen (Google guidance).</summary>
        public static void SetAdPlaying(bool playing)
        {
            adPlaying = playing;
            if (instance == null) return;
            if (playing) foreach (AudioSource voice in instance.voices) voice.Stop();
            instance.Apply();
        }

        private void OnApplicationPause(bool paused) { if (paused) AudioPreferences.Save(); }
        private void OnApplicationQuit() => AudioPreferences.Save();

        private void OnDestroy()
        {
            AudioPreferences.Changed -= Apply;
            SceneManager.activeSceneChanged -= HandleSceneChanged;
            if (instance == this) instance = null;
        }
    }
}
