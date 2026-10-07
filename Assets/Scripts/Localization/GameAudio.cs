using System;
using UnityEngine;
using UnityEngine.EventSystems;
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

    public class GameAudio : MonoBehaviour
    {
        private static GameAudio instance;
        private AudioSource music, effects;
        private AudioClip click, place, reveal, win, draw;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => instance = null;
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
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = effects.playOnAwake = false;
            music.loop = true;
            music.ignoreListenerPause = effects.ignoreListenerPause = true;
            music.clip = Resources.Load<AudioClip>("Audio/Ambient");
            click = Resources.Load<AudioClip>("Audio/Click");
            place = Resources.Load<AudioClip>("Audio/Place");
            reveal = Resources.Load<AudioClip>("Audio/Reveal");
            win = Resources.Load<AudioClip>("Audio/Win");
            draw = Resources.Load<AudioClip>("Audio/Draw");
            AudioPreferences.Changed += Apply;
            Apply();
            if (music.clip != null) music.Play();
        }
        private void Apply()
        {
            float master = AudioPreferences.Muted ? 0 : AudioPreferences.Master;
            music.volume = master * AudioPreferences.Music;
            effects.volume = master * AudioPreferences.Effects;
        }
        public static void Play(string cue)
        {
            if (instance == null || AudioPreferences.Muted) return;
            AudioClip clip = cue switch {
                "place" => instance.place, "reveal" => instance.reveal,
                "win" => instance.win, "draw" => instance.draw, _ => instance.click
            };
            if (clip != null) instance.effects.PlayOneShot(clip);
        }
        private void OnApplicationPause(bool paused) { if (paused) AudioPreferences.Save(); }
        private void OnApplicationQuit() => AudioPreferences.Save();
        private void OnDestroy() { AudioPreferences.Changed -= Apply; if (instance == this) instance = null; }
    }
}
