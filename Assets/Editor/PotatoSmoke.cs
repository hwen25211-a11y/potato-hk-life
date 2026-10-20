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
            for(int i=0;i<10;i++)yield return null;Audit("01-title");yield return null;Click("NewGame");yield return null;if(game.MenuVisible){Click("ConfirmNewGame");yield return null;}Audit("02-home-guide");yield return null;Check(game.State.tutorial==0,"starts with one guide step");
            Click("PickDirection");yield return null;Audit("03-direction");yield return null;Click("Direction0");yield return null;Check(game.State.tutorial==1,"direction guide advances");Click("PetPotato");yield return null;Check(game.State.petClicks==1,"pet produces reward");
            Click("Depart");yield return null;Check(game.State.travelling,"depart UI starts trip");Audit("04-away");yield return null;
            var json=Save.Encode(game.State);var restored=Save.Parse(json);Check(restored!=null&&restored.trip.finishUtc==game.State.trip.finishUtc,"active timer roundtrip");
            Click("Settings");yield return null;Audit("05-settings");yield return null;clock+=31;for(int i=0;i<4;i++)yield return null;Click("×");yield return null;Check(game.State.pending.Count==1,"returns while modal open");Click("Depart");yield return null;Audit("06-return");yield return null;Check(game.State.completed==1,"claim advances count");Click("OpenNewAlbum");yield return null;Audit("07-album");yield return null;Check(game.State.tutorial==4,"three step guide complete");
            int photo=game.State.album[0].id;var albumScroll=UnityEngine.Object.FindObjectOfType<ScrollRect>();albumScroll.verticalNormalizedPosition=1;yield return null;Click("Photo"+photo);yield return null;Audit("08-photo");yield return null;Click("×");yield return null;
            Click("Tab来信");yield return null;Audit("09-mail");yield return null;Click("Letter"+game.State.letters[0].id);yield return null;Audit("10-letter");yield return null;Click("ReplyTogether");yield return null;Check(game.State.friendChapter.Any(x=>x>0),"reply advances friend arc");
            Click("Tab小店");yield return null;Audit("11-shop");yield return null;Click("Buy0");yield return null;Check(game.State.placed.Contains(0),"buy places decoration");Click("Tab小屋");yield return null;Audit("12-decorated-home");yield return null;Click("Wardrobe");yield return null;Audit("13-wardrobe");yield return null;Click("CloseWardrobe");yield return null;
            Click("Quests");yield return null;Audit("14-goals");yield return null;Click("Quest1");yield return null;Check(game.State.questClaimed[1],"optional reward once");Click("回小屋");yield return null;
            var offline=Tests.PotatoChecks.Copy(game.State);offline.pending.Clear();offline.travelling=false;offline.lastUtc=clock;offline.energy=100;offline.tutorial=4;offline.festivalPending=-1;Life.Depart(offline,clock);Save.Write(offline);clock+=86400;game.ContinueGame();yield return null;Check(game.State.pending.Count==3,"offline cap through actual save reload");Audit("15-offline-return");yield return null;Click("Depart");yield return null;Click("OpenNewAlbum");yield return null;Click("Tab小屋");yield return null;Click("Festival");yield return null;Audit("16-festival");yield return null;Click("FestivalTogether");yield return null;Check(game.State.costumes.Contains(1),"festival unlocks costume");
            Click("Wardrobe");yield return null;Click("Outfit1");yield return null;Check(game.State.outfit==1,"costume selectable");Click("CloseWardrobe");yield return null;Audit("17-new-costume");yield return null;
            var ending=Tests.PotatoChecks.RunYear(Direction.Friends);ending.lastUtc=clock;Save.Write(ending);game.ContinueGame();yield return null;Click("Graduation");yield return null;Audit("18-ending");yield return null;Click("FinishYear");yield return null;Check(game.State.graduated,"ending returns to ongoing cozy life");
            Check(Save.Parse("{}") == null&&Save.Parse("{broken") == null,"broken saves rejected");Save.Write(game.State);PlayerPrefs.SetString(Save.Key,"broken");Check(Save.Read()!=null&&Save.Error!="","backup recovery");
            Check(Resources.Load<AudioClip>("Audio/Day")!=null&&Resources.Load<AudioClip>("Audio/Night")!=null&&Resources.Load<AudioClip>("Audio/Travel")!=null,"three user music tracks imported");
            var t=Resources.Load<Texture2D>("Art/PotatoAtlas");Check(t!=null&&t.isReadable&&t.GetPixels32().Any(c=>c.a==0),"transparent mascot atlas readable");for(int i=0;i<6;i++)yield return null;
        }
    }
}
