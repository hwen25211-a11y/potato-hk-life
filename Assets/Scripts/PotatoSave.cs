using System;
using System.Linq;
using UnityEngine;
namespace PotatoLife
{
    public static class Save
    {
        public const string Key="PotatoHK.State.v1",Backup="PotatoHK.Backup.v1";
        public static string Error="";public static bool Suppress;
        public static Func<long> Clock=()=>DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public static long Now{get{return Clock();}}
        public static bool Exists{get{return Read()!=null;}}
        public static string Encode(PotatoState s){return JsonUtility.ToJson(s);}
        public static void Write(PotatoState s)
        {
            if(Suppress||s==null)return;
            try{string json=Encode(s);if(Parse(json)==null)throw new Exception("状态校验未通过，保留上次存档");string old=PlayerPrefs.GetString(Key,"");if(Parse(old)!=null)PlayerPrefs.SetString(Backup,old);PlayerPrefs.SetString(Key,json);PlayerPrefs.Save();Error="";}catch(Exception e){Error="保存未完成："+e.Message;Debug.LogWarning(Error);}
        }
        static bool ValidTrip(Trip t){return t!=null&&t.place>=0&&t.place<12&&t.outfit>=0&&t.outfit<5&&(t.bag==-1||t.bag>=0&&t.bag<World.Items.Length&&World.Items[t.bag].type==1)&&(int)t.direction>=0&&(int)t.direction<4&&t.season>=0&&t.season<4&&t.weather>=0&&t.weather<6;}
        static bool ValidPhoto(Photo p){return p!=null&&p.id>=0&&p.id<16&&p.outfit>=0&&p.outfit<5&&p.season>=0&&p.season<4;}
        static bool ValidLetter(Letter l){return l!=null&&l.friend>=0&&l.friend<3&&(l.kind==0&&l.chapter>=1&&l.chapter<=3||l.kind==1&&l.chapter==0)&&l.reply>=-1&&l.reply<=2;}
        public static PotatoState Parse(string json)
        {
            try
            {
                if(string.IsNullOrEmpty(json)||!json.Contains("\"version\"")||!json.Contains("\"lastUtc\""))return null;
                var s=JsonUtility.FromJson<PotatoState>(json);if(s==null||s.version!=1||s.lastUtc<0||s.coins<0||s.completed<0||s.tutorial<0||s.tutorial>4||s.outfit<0||s.outfit>4)return null;
                if(s.pending==null||s.pending.Count>3||s.album==null||s.letters==null||s.owned==null||s.placed==null||s.costumes==null||s.memories==null)return null;
                if(s.directionCount==null||s.directionCount.Length!=4||s.friendBond==null||s.friendBond.Length!=3||s.friendChapter==null||s.friendChapter.Length!=3||s.festivals==null||s.festivals.Length!=4||s.questClaimed==null||s.questClaimed.Length!=3)return null;
                if(!ValidTrip(s.trip)||s.travelling&&(s.trip.finishUtc<=s.trip.startUtc||s.trip.normalDuration<1800||s.trip.normalDuration>7200)||s.pending.Any(g=>g==null||!ValidTrip(g.trip)||g.photos==null||g.photos.Any(p=>!ValidPhoto(p))||!ValidLetter(g.letter)))return null;
                if(s.album.Any(p=>!ValidPhoto(p))||s.letters.Any(l=>!ValidLetter(l))||s.owned.Any(i=>i<0||i>=World.Items.Length)||s.placed.Any(i=>!s.owned.Contains(i)||World.Items[i].type!=0)||s.placed.Count>Tuning.MaxPlaced||s.costumes.Any(i=>i<0||i>4)||!s.costumes.Contains(s.outfit))return null;
                if(float.IsNaN(s.energy)||float.IsInfinity(s.energy)||float.IsNaN(s.mood)||float.IsInfinity(s.mood)||s.wisdom<0||s.charm<0||s.company<0||(int)s.direction<0||(int)s.direction>3||s.festivalPending<-1||s.festivalPending>3)return null;
                if(s.petTotal<0||s.dailyDay<0||s.dailyPets<0||s.dailyReturns<0||s.dailyReads<0||s.dailyClaimed<0||s.dailyClaimed>7)return null;
                if(s.dailyTask==null||s.dailyTask.Length!=3||s.dailyTask.Any(t=>t<0||t>=World.DailyTasks.Length)||s.dailyTask.Distinct().Count()!=3||s.dailyBase==null||s.dailyBase.Length!=3||s.dailyBase.Any(b=>b<0))return null;
                if(s.dailyClaims<0||s.dailyClaims>Tuning.DailyCap||s.dailyStudy<0||s.dailyTodos<0||s.dailyDeparts<0||s.dailyReplies<0||s.dailyPlaces<0||s.studySession<0||s.studyBlocks<0||s.studyBlocks>Tuning.StudyDailyBlocks||s.studyTotal<0||s.todoRewards<0)return null;
                if(s.rooms==null||!s.rooms.Contains(0)||s.rooms.Any(r=>r<0||r>=World.Rooms.Length)||!s.rooms.Contains(s.room)||s.spots==null||s.spots.Any(p=>p==null||p.room<0||p.room>=World.Rooms.Length||p.id<0||p.id>=World.Items.Length||World.Items[p.id].type!=0||p.x<0||p.x>1536||p.y<0||p.y>1024)||s.todos==null||s.todos.Count>Tuning.MaxTodos+20||s.todos.Any(t=>t==null||string.IsNullOrEmpty(t.text)||t.text.Length>Tuning.TodoLength||t.id<=0||t.id>=s.todoNext))return null;
                s.petTotal=Math.Max(s.petTotal,Math.Max(s.petClicks,s.dailyPets));
                if(s.dailyDay==0)Life.EnsureDaily(s,s.lastUtc);Life.MigrateDaily(s);
                Life.NormalizeLetters(s);s.energy=Life.Cap(s.energy);s.mood=Life.Cap(s.mood);return s;
            }catch{return null;}
        }
        public static PotatoState Read(){Error="";var s=Parse(PlayerPrefs.GetString(Key,""));if(s!=null)return s;s=Parse(PlayerPrefs.GetString(Backup,""));if(s!=null)Error="已从上一份备份找回土豆子的手账。";return s;}
    }
}
