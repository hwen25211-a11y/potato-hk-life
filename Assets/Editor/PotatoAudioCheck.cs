using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PotatoLife.Editor
{
    [InitializeOnLoad]
    public static class PotatoAudioCheck
    {
        const string Key = "PotatoAudioCheck";
        static PotatoAudioCheck() { EditorApplication.update += Tick; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Mute() { if (SessionState.GetBool(Key, false)) { Sound.SilentTest = true; AudioListener.volume = 0; } }
        public static void Run()
        {
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + "Start", (float)EditorApplication.timeSinceStartup);
            EditorSceneManager.OpenScene(PotatoBuild.Scene);
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + "Start", 0) > 90) { Finish(1); return; }
            if (EditorApplication.isPlaying && UnityEngine.Object.FindObjectOfType<Sound>() != null &&
                UnityEngine.Object.FindObjectOfType<PotatoAudioCheckRunner>() == null)
                new GameObject("Silent audio checks").AddComponent<PotatoAudioCheckRunner>();
        }
        public static void Finish(int code) { SessionState.SetBool(Key, false); EditorApplication.Exit(code); }
    }
    public class PotatoAudioCheckRunner : MonoBehaviour
    {
        readonly List<string> report = new List<string>();
        void Check(bool value, string message) { if (!value) throw new Exception(message); report.Add("PASS " + message); }
        IEnumerator Start()
        {
            Save.Suppress = true;
            var game = FindObjectOfType<PotatoGame>(); game.enabled = false;
            float volume = Sound.MusicVolume, effects = Sound.EffectVolume;
            bool failed = false;
            var probe = Probe();
            while (true)
            {
                object current = null; bool more = false;
                try { more = probe.MoveNext(); if (more) current = probe.Current; }
                catch (Exception e) { failed = true; report.Add("FAIL " + e); }
                if (!more || failed) break;
                yield return current;
            }
            Sound.SetMusic(volume); Sound.SetEffects(effects);
            report.Add(failed ? "AUDIO_CHECK_FAILED" : "AUDIO_CHECK_OK");
            File.WriteAllLines("docs/audio-unity-v0.1.2.txt", report);
            PotatoAudioCheck.Finish(failed ? 1 : 0);
        }
        IEnumerator Probe()
        {
            Check(Sound.SilentTest && AudioListener.volume == 0, "listener muted before any playback");
            var sound = FindObjectOfType<Sound>();
            sound.SetWebAudioFocus("1"); Sound.SetMusic(.4f); Sound.SetEffects(.3f);
            yield return new WaitForSecondsRealtime(1.5f);
            var sources = sound.GetComponents<AudioSource>();
            var music = sources.Single(s => s.clip != null);
            Check(music.isPlaying, "foreground music starts");
            var duplicate = new GameObject("Duplicate sound check"); duplicate.AddComponent<Sound>();
            yield return null;
            Check(FindObjectsOfType<Sound>().Length == 1 && duplicate.GetComponents<AudioSource>().Length == 0, "duplicate sound manager creates no extra players");
            Destroy(duplicate);
            float position = music.time; var clip = music.clip;
            sound.SetWebAudioFocus("0"); Sound.Fx("coin"); Sound.NextTrack();
            yield return new WaitForSecondsRealtime(1);
            Check(sources.All(s => !s.isPlaying), "background stops music and effects including new requests");
            Check(Sound.MusicVolume == .4f && Sound.EffectVolume == .3f, "focus does not overwrite saved volumes");
            sound.SetWebAudioFocus("1");
            yield return new WaitForSecondsRealtime(1.5f);
            Check(music.isPlaying && music.clip != clip, "foreground starts only the latest pending track");
            clip = music.clip; position = music.time;
            sound.SetWebAudioFocus("0");
            yield return new WaitForSecondsRealtime(.5f);
            sound.SetWebAudioFocus("1");
            yield return new WaitForSecondsRealtime(.2f);
            Check(music.clip == clip && music.time >= position && music.time < position + .5f, "resume preserves track position");
            foreach (string theme in new[] { "Day", "Travel", "Night", "Festival" })
            {
                Sound.Theme(theme);
                bool exclusive = true;
                float end = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < end) { exclusive &= sources.Count(s => s.isPlaying && s.clip != null) <= 1; yield return null; }
                Check(exclusive, "exclusive music during " + theme + " transition");
                sound.SetWebAudioFocus("0"); Sound.NextTrack(); Sound.NextTrack();
                yield return null;
                Check(sources.All(s => !s.isPlaying), "no queued background playback in " + theme);
                sound.SetWebAudioFocus("1"); yield return new WaitForSecondsRealtime(1.5f);
                Check(sources.Count(s => s.isPlaying && s.clip != null) == 1, "one music source after " + theme + " resume");
            }
            Sound.SetMusic(0); sound.SetWebAudioFocus("0"); sound.SetWebAudioFocus("1");
            yield return new WaitForSecondsRealtime(1.5f);
            Check(music.volume == 0, "user mute survives focus changes");
            Check(AudioListener.volume == 0, "entire regression stayed silent");
        }
    }
}
