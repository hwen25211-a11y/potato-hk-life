using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace PotatoLife.Editor
{
    [InitializeOnLoad]public static class PotatoSmoke
    {
        static PotatoSmoke(){EditorApplication.update+=Tick;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void MuteTest(){if(SessionState.GetBool("PotatoSmoke",false)){Sound.SilentTest=true;AudioListener.volume=0;}}
        public static void Run(){PotatoBuild.Prepare();SessionState.SetBool("PotatoSmoke",true);SessionState.SetFloat("PotatoSmokeStart",(float)EditorApplication.timeSinceStartup);EditorSceneManager.OpenScene(PotatoBuild.Scene);EditorApplication.isPlaying=true;}
        static void Tick(){if(!SessionState.GetBool("PotatoSmoke",false))return;if(EditorApplication.timeSinceStartup-SessionState.GetFloat("PotatoSmokeStart",0)>240){SessionState.SetBool("PotatoSmoke",false);EditorApplication.Exit(1);return;}if(EditorApplication.isPlaying&&UnityEngine.Object.FindObjectOfType<PotatoGame>()!=null&&UnityEngine.Object.FindObjectOfType<PotatoSmokeRunner>()==null)new GameObject("Smoke runner").AddComponent<PotatoSmokeRunner>();}
    }
    public class PotatoSmokeRunner:MonoBehaviour
    {
        PotatoGame game;List<string> report=new List<string>();long clock=100000;string previous,backup;float music,effects;int overflow;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS "+text);}
        void Click(string name)
        {
            var button=UnityEngine.Object.FindObjectsOfType<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.name==name);Check(button!=null&&button.interactable,"button "+name);Canvas.ForceUpdateCanvases();var rt=(RectTransform)button.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));var evt=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(evt,hits);Check(hits.Count>0&&hits[0].gameObject==button.gameObject,"raycast "+name);ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerClickHandler);
        }
        void Audit(string name)
        {
            foreach(var fade in UnityEngine.Object.FindObjectsOfType<PageFade>())fade.Finish();int n=0;foreach(var text in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>()){text.ForceMeshUpdate();if(text.isTextOverflowing){report.Add("OVERFLOW "+name+" "+text.text.Replace('\n',' '));n++;}}overflow+=n;report.Add("UI "+name+" overflow="+n);ScreenCapture.CaptureScreenshot(Path.GetFullPath("docs/screenshots/"+name+".png"));
        }
        void Restore(){Save.Suppress=true;if(previous==null)PlayerPrefs.DeleteKey(Save.Key);else PlayerPrefs.SetString(Save.Key,previous);if(backup==null)PlayerPrefs.DeleteKey(Save.Backup);else PlayerPrefs.SetString(Save.Backup,backup);PlayerPrefs.SetFloat("PotatoHK.Music",music);PlayerPrefs.SetFloat("PotatoHK.Effects",effects);PlayerPrefs.Save();}
        IEnumerator Start()
        {
            previous=PlayerPrefs.HasKey(Save.Key)?PlayerPrefs.GetString(Save.Key):null;backup=PlayerPrefs.HasKey(Save.Backup)?PlayerPrefs.GetString(Save.Backup):null;music=Sound.MusicVolume;effects=Sound.EffectVolume;Sound.SetMusic(0);Sound.SetEffects(0);Save.Clock=()=>clock;
            Directory.CreateDirectory("docs/screenshots");game=UnityEngine.Object.FindObjectOfType<PotatoGame>();var probe=Probe();while(true){object value=null;bool more=false;try{more=probe.MoveNext();if(more)value=probe.Current;}catch(Exception e){report.Add("FAILED "+e);File.WriteAllLines("docs/smoke-results.txt",report.ToArray());Restore();SessionState.SetBool("PotatoSmoke",false);EditorApplication.Exit(1);yield break;}if(!more)break;yield return value;}
            report.Add("SMOKE_OK total_overflow="+overflow);File.WriteAllLines("docs/smoke-results.txt",report.ToArray());Restore();SessionState.SetBool("PotatoSmoke",false);EditorApplication.Exit(overflow==0?0:1);
        }
        IEnumerator Probe()
        {
            Check(AudioListener.volume==0&&Sound.SilentTest,"test audio is silent before gameplay");
            for(int i=0;i<10;i++)yield return null;Audit("01-title");yield return null;Click("NewGame");yield return null;if(game.MenuVisible){Click("ConfirmNewGame");yield return null;}Audit("02-home-guide");yield return null;Check(game.State.tutorial==0,"starts with one guide step");
            Click("Profile");yield return null;
            for(int i=0;i<3;i++){var number=UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>().Single(t=>t.name=="ProfileValue"+i);number.ForceMeshUpdate();Check(number.text=="0"&&number.textInfo.characterInfo.Take(number.textInfo.characterCount).Any(c=>c.isVisible),"zero profile value is visible "+i);}
            Click("×");yield return null;
            Click("PickDirection");yield return null;Audit("03-direction");yield return null;Click("Direction0");yield return null;Check(game.State.tutorial==1,"direction guide advances");Click("PetPotato");yield return null;Check(game.State.petClicks==1,"pet produces reward");
            Click("Depart");yield return null;Check(game.State.travelling,"depart UI starts trip");Audit("04-away");yield return null;
            var json=Save.Encode(game.State);var restored=Save.Parse(json);Check(restored!=null&&restored.trip.finishUtc==game.State.trip.finishUtc,"active timer roundtrip");
            var legacy=System.Text.RegularExpressions.Regex.Replace(json,",\"(?:daily(?:Day|Pets|Returns|Reads|Claimed)|petTotal)\":\\d+","");var migrated=Save.Parse(legacy);Check(migrated!=null&&migrated.dailyDay>0&&migrated.petTotal>=migrated.petClicks&&migrated.trip.finishUtc==game.State.trip.finishUtc,"v0.1.0 save gains daily tasks without losing trip");
            Click("Settings");yield return null;
            foreach(var slider in UnityEngine.Object.FindObjectsOfType<Slider>())
            {
                foreach(float value in new[]{0f,.5f,1f}){slider.value=value;Canvas.ForceUpdateCanvases();Check(Mathf.Abs(slider.fillRect.rect.width-((RectTransform)slider.transform).rect.width*value)<1,"volume fill stays within track at "+value);}
                slider.value=.5f;
            }
            Check(UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name=="MuteMusic").GetComponentInChildren<TextMeshProUGUI>().text=="静音","volume slider updates mute button label");
            string beforeMusic=Sound.NowPlaying;Click("NextMusic");yield return null;Check(Sound.NowPlaying!=beforeMusic,"next music button changes track");Check(UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>().Any(t=>t.text=="当前曲目 · "+Sound.NowPlaying),"settings displays current music title");Audit("05-settings");yield return null;Sound.SetMusic(0);Sound.SetEffects(0);clock+=31;for(int i=0;i<4;i++)yield return null;Click("×");yield return null;Check(game.State.pending.Count==1,"returns while modal open");Click("Depart");yield return null;Audit("06-return");yield return null;Check(game.State.completed==1,"claim advances count");Click("OpenNewAlbum");yield return null;Audit("07-album");yield return null;Check(game.State.tutorial==4,"three step guide complete");
            int photo=game.State.album[0].id;var albumScroll=UnityEngine.Object.FindObjectOfType<ScrollRect>();albumScroll.verticalNormalizedPosition=1;yield return null;Click("Photo"+photo);yield return null;Audit("08-photo");yield return null;Click("×");yield return null;
            Click("Tab来信");yield return null;Audit("09-mail");yield return null;Click("Letter"+game.State.letters[0].id);yield return null;Audit("10-letter");yield return null;Click("ReplyTogether");yield return null;Check(game.State.friendChapter.Any(x=>x>0),"reply advances friend arc");
            Click("Tab小店");yield return null;Audit("11-shop");yield return null;Click("Buy0");yield return null;Check(game.State.placed.Contains(0),"buy places decoration");Click("Tab小屋");yield return null;Audit("12-decorated-home");yield return null;Click("Wardrobe");yield return null;Audit("13-wardrobe");yield return null;Click("CloseWardrobe");yield return null;
            Click("Quests");yield return null;Audit("14-goals");yield return null;Click("Quest1");yield return null;Check(game.State.questClaimed[1],"optional reward once");Click("回小屋");yield return null;
            var offline=Tests.PotatoChecks.Copy(game.State);offline.pending.Clear();offline.travelling=false;offline.lastUtc=clock;offline.energy=100;offline.tutorial=4;offline.festivalPending=-1;Life.Depart(offline,clock);Save.Write(offline);clock+=86400;game.ContinueGame();yield return null;Check(game.State.pending.Count==3,"offline cap through actual save reload");Audit("15-offline-return");yield return null;Click("Depart");yield return null;Click("OpenNewAlbum");yield return null;Click("Tab小屋");yield return null;Click("Festival");yield return null;Audit("16-festival");yield return null;Click("FestivalTogether");yield return null;Check(game.State.costumes.Contains(1),"festival unlocks costume");
            Click("Wardrobe");yield return null;Click("Outfit1");yield return null;Check(game.State.outfit==1,"costume selectable");Click("CloseWardrobe");yield return null;Audit("17-new-costume");yield return null;
            Click("Profile");yield return null;Audit("19-profile");yield return null;
            for(int i=0;i<3;i++){var number=UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>().Single(t=>t.name=="ProfileValue"+i);number.ForceMeshUpdate();Check(number.textInfo.characterInfo.Take(number.textInfo.characterCount).Any(c=>c.isVisible),"profile number actually renders "+i);}
            Click("×");yield return null;
            Click("Daily0");yield return null;Click("Daily0");yield return null;Click("Daily0");yield return null;int beforeReward=game.State.coins;Click("Daily0");yield return null;Check(game.State.coins==beforeReward+Tuning.DailyCoins&&(game.State.dailyClaimed&1)!=0,"daily task claimed through main board");
            Click("Settings");yield return null;if(game.State.autoRoam){Click("ToggleAuto");yield return null;}Click("×");yield return null;
            Click("Tab小店");yield return null;UnityEngine.Object.FindObjectOfType<ScrollRect>().verticalNormalizedPosition=0;yield return null;Click("Buy6");yield return null;Click("Tab小屋");yield return null;Click("Wardrobe");yield return null;Click("Bag6");yield return null;Click("CloseWardrobe");yield return null;Check(game.State.selectedBag==6,"purchased camera can be packed");
            // Continue the same save to graduation through UI actions; only the clock is accelerated.
            while(game.State.completed<Tuning.YearTrips)
            {
                int beforeTrip=game.State.completed;Click("PickDirection");yield return null;Click("Direction"+(beforeTrip%4));yield return null;
                clock+=30;for(int i=0;i<3;i++)yield return null;Click("Depart");yield return null;Check(game.State.travelling,"full-year UI departure "+(beforeTrip+1));
                if(beforeTrip==4)
                {
                    clock+=10;yield return null;double progress=Life.Progress(game.State,clock);Click("Settings");yield return null;Click("ToggleDemo");yield return null;Check(Math.Abs(Life.Progress(game.State,clock)-progress)<.04,"UI slow mode preserves progress");Click("ToggleDemo");yield return null;Check(Math.Abs(Life.Progress(game.State,clock)-progress)<.04,"UI demo mode preserves progress");Click("×");yield return null;
                }
                clock=game.State.trip.finishUtc+1;for(int i=0;i<4;i++)yield return null;Click("Depart");yield return null;Check(game.State.completed==beforeTrip+1,"full-year UI claim "+(beforeTrip+1));Click("OpenNewAlbum");yield return null;Click("Tab小屋");yield return null;
                if(beforeTrip==4){Click("Quests");yield return null;Click("Quest0");yield return null;Check(game.State.questClaimed[0],"pet milestone can be claimed after collecting a trip");Click("回小屋");yield return null;}
                if(game.State.festivalPending>=0){int season=game.State.festivalPending;Click("Festival");yield return null;Audit("20-festival-"+season);yield return null;Click(season%2==0?"FestivalTogether":"FestivalWish");yield return null;Check(game.State.festivals[season],"UI festival completed "+season);Click("Wardrobe");yield return null;Click("Outfit"+(season+1));yield return null;Click("CloseWardrobe");yield return null;}
                Click("Tab来信");yield return null;int letter=game.State.letters.Last().id;Click("Letter"+letter);yield return null;Click(beforeTrip%2==0?"ReplyTogether":"ReplyAdvice");yield return null;Check(game.State.letters.Last().reply>=0,"UI letter replied "+(beforeTrip+1));Click("Tab小屋");yield return null;
            }
            Check(game.State.festivals.All(x=>x),"all four festivals completed in same UI playthrough");Click("Graduation");yield return null;Audit("18-ending");yield return null;
            foreach(int ending in Life.AvailableEndings(game.State)){Click("Ending"+ending);yield return null;Check(game.State.ending==ending.ToString(),"available ending selectable "+ending);}
            Click("FinishYear");yield return null;Check(game.State.graduated,"ending returns to ongoing cozy life");
            Click("Settings");yield return null;Click("保留进度，返回扉页");yield return null;Click("Continue");yield return null;Check(game.State.graduated&&game.State.completed==16,"continue reloads completed year");
            clock+=30;for(int i=0;i<3;i++)yield return null;Click("Depart");yield return null;clock=game.State.trip.finishUtc+1;for(int i=0;i<3;i++)yield return null;Click("Depart");yield return null;Check(game.State.completed==17,"can collect another trip after graduation");Click("OpenNewAlbum");yield return null;Click("Tab小屋");yield return null;
            Check(Save.Parse("{}") == null&&Save.Parse("{broken") == null,"broken saves rejected");Save.Write(game.State);PlayerPrefs.SetString(Save.Key,"broken");Check(Save.Read()!=null&&Save.Error!="","backup recovery");
            // Freeze game-driven theme updates; exercise real audio playback independently.
            game.enabled=false;Save.Suppress=true;AudioListener.volume=0;
            var sources=UnityEngine.Object.FindObjectOfType<Sound>().GetComponents<AudioSource>();
            foreach(string name in new[]{"Day","Night","Travel","AfternoonJazz","PixelRoomPiano","LazyCoolJazz"})
            {var clip=Resources.Load<AudioClip>("Audio/"+name);Check(clip!=null&&clip.channels==2&&clip.length>50,"music imported "+name);}
            foreach(string name in new[]{"bubble","coin","page","purchase","mail","return","company","stamp"})
            {var clip=Resources.Load<AudioClip>("Audio/"+name);Check(clip!=null&&clip.length>0&&clip.length<3,"feedback sound imported "+name);var data=new float[clip.samples*clip.channels];Check(clip.GetData(data,0)&&data.Any(x=>Mathf.Abs(x)>.001f)&&data.All(x=>Mathf.Abs(x)<.5f),"feedback sound decodes at gentle level "+name);}
            Sound.SetMusic(.4f);Sound.SetEffects(.25f);
            foreach(string theme in new[]{"Day","Night","Travel"})
            {
                Sound.Theme(theme);var heard=new HashSet<string>();
                for(int i=0;i<3;i++)
                {
                    yield return new WaitForSecondsRealtime(1.7f);
                    var active=sources.Where(s=>s.isPlaying&&s.clip!=null).ToArray();
                    Check(active.Length==1&&!active[0].loop&&Mathf.Abs(active[0].volume-.4f)<.01f,"one playing music source after fade "+theme);
                    heard.Add(active[0].clip.name);string title=Sound.NowPlaying;float position=active[0].time;
                    Sound.Theme(theme);yield return null;
                    Check(Sound.NowPlaying==title&&active[0].time>=position-.1f,"repeated theme does not restart "+theme);
                    Sound.NextTrack();
                }
                string[] expected=theme=="Day"?new[]{"Day","AfternoonJazz","PixelRoomPiano"}:theme=="Night"?new[]{"Night","LazyCoolJazz","PixelRoomPiano"}:new[]{"Travel","AfternoonJazz","LazyCoolJazz"};
                Check(heard.SetEquals(expected),"all songs reachable in "+theme+" playlist");
            }
            yield return new WaitForSecondsRealtime(1.7f);
            string travelTitle=Sound.NowPlaying;Sound.Theme("Festival");Check(Sound.NowPlaying==travelTitle,"festival shares travel without restarting");
            var playing=sources.Single(s=>s.isPlaying&&s.clip!=null);
            playing.time=playing.clip.length-1;string endingTitle=Sound.NowPlaying;
            yield return new WaitForSecondsRealtime(.5f);
            Check(Sound.NowPlaying!=endingTitle,"track ending automatically advances");
            foreach(string target in new[]{"Day","Travel","Night","Festival"})
            {
                Sound.Theme(target);float deadline=Time.realtimeSinceStartup+1.4f;
                bool exclusive=true;while(Time.realtimeSinceStartup<deadline){exclusive &= sources.Count(s=>s.isPlaying&&s.clip!=null)<=1;yield return null;}Check(exclusive,"at most one BGM during "+target+" switch");
            }
            Sound.SetMusic(0);Sound.NextTrack();yield return new WaitForSecondsRealtime(.2f);
            Check(sources.Where(s=>s.clip!=null).All(s=>s.volume==0)&&Sound.EffectVolume==.25f,"mute holds through switching without changing effects volume");
            Sound.SetMusic(.4f);yield return new WaitForSecondsRealtime(1.7f);
            var loud=sources.Single(s=>s.isPlaying&&s.clip!=null);Sound.NextTrack();yield return new WaitForSecondsRealtime(.05f);float level=loud.volume;
            Sound.NextTrack();Check(loud.isPlaying&&Mathf.Abs(loud.volume-level)<.01f,"rapid next preserves fade-out level");
            yield return new WaitForSecondsRealtime(1.7f);
            Check(sources.Count(s=>s.isPlaying&&s.clip!=null)==1,"rapid next settles to one source");
            var t=Resources.Load<Texture2D>("Art/PotatoAtlas");Check(t!=null&&t.isReadable&&t.GetPixels32().Any(c=>c.a==0),"transparent mascot atlas readable");for(int i=0;i<6;i++)yield return null;
            game.enabled=true;Click("Settings");yield return null;
            var hover=UnityEngine.Object.FindObjectsOfType<Button>().Single(b=>b.name=="NextMusic");var rect=(RectTransform)hover.transform;var center=rect.TransformPoint(rect.rect.center);ExecuteEvents.Execute(hover.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.3f);Check(Vector3.Distance(center,rect.TransformPoint(rect.rect.center))<.01f,"hover animation stays centered");ExecuteEvents.Execute(hover.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);Click("×");yield return null;
            var canvas=UnityEngine.Object.FindObjectOfType<Canvas>();var sheet=UI.Card(canvas.transform,0,0,1600,1000,UI.Cream).transform;
            for(int i=0;i<12;i++){UI.Actor(sheet,i<8?i:0,i<8?0:i-7,50+i%6*250,80+i/6*430,220,340,false);UI.Text(sheet,i<8?"姿势 "+i:World.CostumeNames[i-7],50+i%6*250,425+i/6*430,220,42,23,UI.Ink,true,TextAlignmentOptions.Center);}
            yield return new WaitForSecondsRealtime(.3f);Audit("21-all-poses");yield return null;
            Check(AudioListener.volume==0,"all testing remained silent");
        }
    }
}
