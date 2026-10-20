using UnityEngine;
namespace PotatoLife
{
    public class Sound:MonoBehaviour
    {
        static Sound instance;AudioSource a,b,fx;AudioSource current,previous;string theme="";float fade;
        public static float MusicVolume{get{return PlayerPrefs.GetFloat("PotatoHK.Music",.32f);}}
        public static float EffectVolume{get{return PlayerPrefs.GetFloat("PotatoHK.Effects",.5f);}}
        public static void Initialize(GameObject parent){if(instance==null)instance=parent.AddComponent<Sound>();}
        void Awake(){instance=this;a=gameObject.AddComponent<AudioSource>();b=gameObject.AddComponent<AudioSource>();fx=gameObject.AddComponent<AudioSource>();a.loop=b.loop=true;Theme("Day");}
        void Update(){fade=Mathf.Min(1,fade+Time.unscaledDeltaTime*.7f);if(current!=null)current.volume=MusicVolume*fade;if(previous!=null){previous.volume=MusicVolume*(1-fade);if(fade>=1)previous.Stop();}}
        public static void Theme(string name)
        {
            if(instance==null||instance.theme==name)return;var clip=Resources.Load<AudioClip>("Audio/"+(name=="Festival"?"Travel":name));if(clip==null)return;instance.theme=name;instance.previous=instance.current;instance.current=instance.current==instance.a?instance.b:instance.a;instance.current.clip=clip;instance.current.volume=0;instance.current.Play();instance.fade=0;
        }
        public static void SetMusic(float v){PlayerPrefs.SetFloat("PotatoHK.Music",Mathf.Clamp01(v));PlayerPrefs.Save();}
        public static void SetEffects(float v){PlayerPrefs.SetFloat("PotatoHK.Effects",Mathf.Clamp01(v));PlayerPrefs.Save();}
        public static void Fx(string kind)
        {
            if(instance==null)return;var clip=Resources.Load<AudioClip>("Audio/"+kind);if(clip==null)return;instance.fx.pitch=Random.Range(.96f,1.06f);instance.fx.PlayOneShot(clip,EffectVolume);
        }
    }
}
