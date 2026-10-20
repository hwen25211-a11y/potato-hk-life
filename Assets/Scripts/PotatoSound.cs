using System.Collections.Generic;
using UnityEngine;

namespace PotatoLife
{
    [DisallowMultipleComponent]
    public class Sound : MonoBehaviour
    {
        const float FadeSeconds = .6f;
        static readonly Dictionary<string, string[]> Playlists = new Dictionary<string, string[]>
        {
            {"Day", new[] {"Day", "AfternoonJazz", "PixelRoomPiano"}},
            {"Night", new[] {"Night", "LazyCoolJazz", "PixelRoomPiano"}},
            {"Travel", new[] {"Travel", "AfternoonJazz", "LazyCoolJazz"}}
        };
        static readonly Dictionary<string, string> Titles = new Dictionary<string, string>
        {
            {"Day", "Mellow Lounge"}, {"Night", "Midnight Rhyme"}, {"Travel", "Winding Path"},
            {"AfternoonJazz", "午后慢摇爵士曲"}, {"PixelRoomPiano", "像素小房间钢琴曲"},
            {"LazyCoolJazz", "慵懒松弛冷爵士"}
        };

        static Sound instance;
        readonly Dictionary<string, int> nextTrack = new Dictionary<string, int>();
        AudioSource music, fx;
        AudioClip pendingClip;
        string theme = "", track = "", pendingTrack = "";
        float gain;
        bool hasPlayed;
        bool focused = true, paused;
        float resumeTime;
        bool Suspended { get { return !focused || paused; } }

        // Tests mute the listener before any source starts; saved volume settings stay intact.
        public static bool SilentTest;
        public static float MusicVolume { get { return PlayerPrefs.GetFloat("PotatoHK.Music", .32f); } }
        public static float EffectVolume { get { return PlayerPrefs.GetFloat("PotatoHK.Effects", .5f); } }
        public static string NowPlaying { get { return instance == null ? "" : instance.pendingClip != null ? Titles[instance.pendingTrack] + "（切换中）" : Titles.ContainsKey(instance.track) ? Titles[instance.track] : ""; } }

        public static void Initialize(GameObject parent)
        {
            if (instance == null) instance = parent.AddComponent<Sound>();
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
            if (SilentTest || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-potato-silent-test") >= 0 ||
                Application.absoluteURL.Contains("silent-test=1")) AudioListener.volume = 0;
            music = gameObject.AddComponent<AudioSource>();
            fx = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = fx.playOnAwake = false;
            music.loop = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            // The page grants audio ownership only to its active window.
            focused = false;
#else
            focused = Application.isFocused || Application.isEditor;
#endif
            Theme("Day");
        }

        void OnApplicationFocus(bool value)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (!SilentTest) SetFocus(value);
#endif
        }

        void OnApplicationPause(bool value)
        {
            bool wasSuspended = Suspended;
            paused = value;
            UpdateSuspension(wasSuspended);
        }

        // Called by the WebGL page, including when Unity's render loop is throttled.
        public void SetWebAudioFocus(string value) { SetFocus(value == "1"); }

        void SetFocus(bool value)
        {
            bool wasSuspended = Suspended;
            focused = value;
            UpdateSuspension(wasSuspended);
        }

        void UpdateSuspension(bool wasSuspended)
        {
            if (music == null || wasSuspended == Suspended) return;
            if (Suspended)
            {
                resumeTime = music.time;
                music.Stop();
                fx.Stop();
                gain = 0;
                hasPlayed = false;
                ApplyVolume();
            }
            else if (pendingClip != null) StartPending();
            else if (music.clip != null)
            {
                music.time = Mathf.Clamp(resumeTime, 0, Mathf.Max(0, music.clip.length - .01f));
                music.Play();
            }
        }

        void OnDestroy()
        {
            if (instance != this) return;
            if (music != null) { music.Stop(); Destroy(music); }
            if (fx != null) { fx.Stop(); Destroy(fx); }
            instance = null;
        }

        void Update()
        {
            if (music == null || Suspended) return;
            if (pendingClip != null)
            {
                gain = Mathf.MoveTowards(gain, 0, Time.unscaledDeltaTime / FadeSeconds);
                ApplyVolume();
                if (gain <= 0) StartPending();
                return;
            }
            gain = Mathf.MoveTowards(gain, 1, Time.unscaledDeltaTime / FadeSeconds);
            ApplyVolume();
            if (music.clip == null || AudioListener.pause) return;
            hasPlayed |= music.isPlaying;
            // A browser may wait for the first user gesture before allowing audio.
            // Only advance after playback really started, never just because it is silent.
            if (hasPlayed && (!music.isPlaying || music.clip.length - music.time <= FadeSeconds))
                PlayNext();
        }

        void StartPending()
        {
            if (Suspended) return;
            // Stop the previous clip before replacing it on this instance's music source.
            music.Stop();
            music.clip = pendingClip;
            track = pendingTrack;
            pendingClip = null;
            pendingTrack = "";
            gain = 0;
            hasPlayed = false;
            ApplyVolume();
            music.Play();
        }

        void ApplyVolume()
        {
            music.volume = MusicVolume * gain;
        }

        public static void Theme(string name)
        {
            if (name == "Festival") name = "Travel";
            if (instance == null || instance.theme == name || !Playlists.ContainsKey(name)) return;
            instance.theme = name;
            instance.PlayNext();
        }

        public static void NextTrack()
        {
            if (instance != null) instance.PlayNext();
        }

        void PlayNext()
        {
            var playlist = Playlists[theme];
            int index;
            if (!nextTrack.TryGetValue(theme, out index)) index = 0;
            for (int attempt = 0; attempt < playlist.Length; attempt++)
            {
                string selected = playlist[index];
                index = (index + 1) % playlist.Length;
                if (selected == (pendingClip != null ? pendingTrack : track) && playlist.Length > 1) continue;
                var clip = Resources.Load<AudioClip>("Audio/" + selected);
                if (clip == null) { Debug.LogWarning("Missing music: " + selected); continue; }
                nextTrack[theme] = index;
                pendingClip = clip;
                pendingTrack = selected;
                if (music.clip == null || gain <= 0) StartPending();
                return;
            }
        }

        public static void SetMusic(float value)
        {
            PlayerPrefs.SetFloat("PotatoHK.Music", Mathf.Clamp01(value));
            PlayerPrefs.Save();
            if (instance != null) instance.ApplyVolume();
        }

        public static void SetEffects(float value)
        {
            PlayerPrefs.SetFloat("PotatoHK.Effects", Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        public static void Fx(string kind)
        {
            if (instance == null || instance.Suspended) return;
            var clip = Resources.Load<AudioClip>("Audio/" + kind);
            if (clip == null) return;
            instance.fx.pitch = Random.Range(.96f, 1.06f);
            instance.fx.PlayOneShot(clip, EffectVolume);
        }
    }
}
