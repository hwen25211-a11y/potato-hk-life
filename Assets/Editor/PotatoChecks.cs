using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
namespace PotatoLife.Tests
{
    public static class PotatoChecks
    {
        static int count;static List<string> report;
        static void Check(bool b,string text){count++;if(!b)throw new Exception("FAILED: "+text);}
        public static PotatoState Copy(PotatoState s){using(var m=new MemoryStream()){var f=new BinaryFormatter();f.Serialize(m,s);m.Position=0;return (PotatoState)f.Deserialize(m);}}
        public static PotatoState RunYear(Direction d,int seed=19)
        {
            long now=100000;var s=Life.New(now,seed);s.tutorial=4;s.autoRoam=false;Life.DirectionPick(s,d);
            for(int i=0;i<16;i++)
            {
                now+=30;Life.Tick(s,now);Check(Life.Depart(s,now),"departure "+i);Check(s.trip.finishUtc-now==30,"demo duration");now=s.trip.finishUtc;Life.Tick(s,now);Check(s.pending.Count==1,"one return");int money=s.coins;Check(Life.Collect(s,now)==1,"collect once");Check(s.coins>money,"positive income");Check(Life.Collect(s,now)==0,"no duplicate collection");
                foreach(var letter in s.letters.Where(l=>l.reply<0).ToArray()){Life.ReadLetter(s,letter.id);Life.Reply(s,letter.id,i%2);}
                if(s.festivalPending>=0)Check(Life.Festival(s,i%2),"festival choice");
            }
            Check(Life.YearReady(s),"year complete");Check(s.festivals.All(x=>x),"four festivals reached");Check(s.album.Count>=8,"multiple memories collected");Check(Life.AvailableEndings(s).Contains((int)d),"desired ending is reachable");Check(Life.Graduate(s,(int)d),"choose ending");Check(s.graduated&&s.ending==((int)d).ToString(),"ending saved");
            return s;
        }
        public static void Run()
        {
            count=0;report=new List<string>();Directory.CreateDirectory("docs/paths");
            foreach(Direction d in Enum.GetValues(typeof(Direction)))
            {
                var s=RunYear(d);string line=World.Endings[(int)d]+" / trips="+s.completed+" / photos="+s.album.Count+" / letters="+s.letters.Count+" / direction="+string.Join(",",s.directionCount.Select(x=>x.ToString()).ToArray());Console.WriteLine(line);report.Add(line);File.WriteAllLines("docs/paths/"+d+".txt",s.memories.Concat(new[]{line}).ToArray());
            }
            var basic=Life.New(1000,9);Check(basic.energy>0&&basic.coins>0,"friendly start");Life.DirectionPick(basic,Direction.Explore);Check(basic.tutorial==1,"tutorial direction step");Check(Life.Depart(basic,1000)&&basic.tutorial==2,"tutorial departure step");Life.Tick(basic,1000000);Check(basic.pending.Count==1&&!basic.travelling,"tutorial cannot auto leave before first album");Life.Collect(basic,1000000);Check(basic.tutorial==3,"tutorial collecting step");Life.OpenAlbum(basic);Check(basic.tutorial==4,"three-step guide ends");
            var offline=Life.New(1000,24);offline.tutorial=4;Check(Life.Depart(offline,1000),"offline depart");Life.Tick(offline,1000+86400*90);Check(offline.pending.Count==3&&!offline.travelling,"90 days capped at three returns");int rng=offline.rng;Life.Tick(offline,1000+86400*91);Check(offline.pending.Count==3&&offline.rng==rng,"full mailbox has no extra simulated backlog");Life.Collect(offline,offline.lastUtc);Check(offline.completed==3,"offline collect three");Life.Tick(offline,offline.lastUtc+1);Check(offline.pending.Count==0,"cap does not refill instantly after claiming");
            var rewind=Life.New(1000,4);rewind.tutorial=4;Life.Depart(rewind,1000);Life.Tick(rewind,1015);var copy=Copy(rewind);Life.Tick(rewind,900);Check(rewind.lastUtc==1015&&rewind.pending.Count==0,"clock rollback never produces gifts");Life.Tick(rewind,1040);Life.Tick(copy,1040);Check(rewind.pending[0].photos.Select(p=>p.id).SequenceEqual(copy.pending[0].photos.Select(p=>p.id)),"reload deterministic reward");
            var modes=Life.New(1000,18);modes.demo=false;modes.tutorial=4;Life.Depart(modes,1000);int duration=(int)(modes.trip.finishUtc-1000);Check(duration>=1800&&duration<=7200,"normal duration bounds");long half=1000+duration/2;Life.Mode(modes,true,half);Check(Math.Abs(Life.Progress(modes,half)-.5)<.04,"mode switch preserves progress");Life.Mode(modes,false,half);Check(Math.Abs(Life.Progress(modes,half)-.5)<.04,"switch back preserves progress");
            var rest=Life.New(1000,1);rest.energy=1;rest.mood=1;Check(!Life.Depart(rest,1000),"low energy rests at home");Life.Tick(rest,1010);Check(rest.energy>=25&&Life.Depart(rest,1010),"automatic recovery makes departure possible");Check(rest.coins==120,"rest never charges money");
            var pet=Life.New(1,4);int before=pet.coins;for(int i=0;i<5;i++)Life.Pet(pet);Check(pet.coins>before,"pet interaction rewards");before=pet.coins;Life.Pet(pet);Check(pet.coins==before,"pet reward limit but interaction remains");Check(Life.ClaimQuest(pet,0)&&!Life.ClaimQuest(pet,0),"quest reward once");
            Check(Life.DailyReady(pet,0),"daily pet task counts interactions");before=pet.coins;Check(Life.ClaimDaily(pet,0)&&pet.coins==before+Tuning.DailyCoins,"daily reward credited");Check(!Life.ClaimDaily(pet,0)&&!Life.ClaimDaily(pet,1),"daily reward cannot be duplicated or claimed early");
            long previousDay=pet.dailyDay;before=pet.coins;Life.Tick(pet,86401);Check(pet.dailyDay>previousDay&&pet.dailyClaimed==0&&pet.dailyPets==0,"Hong Kong new day refreshes daily tasks");Check(pet.coins==before,"unfinished tasks have no penalty");for(int i=0;i<3;i++)Life.Pet(pet);Check(Life.DailyReady(pet,0)&&Life.ClaimDaily(pet,0),"next day pet task works without an intervening trip");int claimed=pet.dailyClaimed;Life.Tick(pet,1);Check(pet.dailyClaimed==claimed&&!Life.ClaimDaily(pet,0),"clock rollback cannot refresh claimed daily rewards");
            Check(Life.DailyReady(basic,1)&&Life.ClaimDaily(basic,1),"collecting gifts completes daily return task");int firstLetter=basic.letters[0].id;Check(Life.ReadLetter(basic,firstLetter)&&Life.DailyReady(basic,2),"first read completes daily letter task");Life.ClaimDaily(basic,2);Life.Tick(basic,basic.lastUtc+86400);Check(!Life.ReadLetter(basic,firstLetter)&&!Life.DailyReady(basic,2),"rereading does not farm daily letter reward");
            var milestone=Life.New(1,6);for(int i=0;i<3;i++)Life.Pet(milestone);Life.Depart(milestone,1);Life.Tick(milestone,31);Life.Collect(milestone,31);Check(Life.QuestReady(milestone,0),"pet milestone survives collecting a trip");Life.Tick(milestone,86401);Check(Life.QuestReady(milestone,0)&&Life.ClaimQuest(milestone,0),"pet milestone survives new day");
            var shop=Life.New(1,2);Check(!Life.Buy(shop,2),"no overdraft");Check(Life.Buy(shop,0)&&shop.placed.Contains(0),"decoration is visible and equipped");int money2=shop.coins;Check(!Life.Buy(shop,0)&&shop.coins==money2,"cannot double buy");Check(Life.PlaceItem(shop,0)&&!shop.placed.Contains(0),"can remove decoration");Check(!Life.Pack(shop,6),"cannot pack unowned camera");
            var outfit=RunYear(Direction.Explore);int original=outfit.album[0].outfit;Check(Life.Dress(outfit,3),"festival costume selectable");Check(outfit.album[0].outfit==original,"old photo keeps captured outfit");long time=outfit.lastUtc+30;Life.Tick(outfit,time);Life.Depart(outfit,time);Check(outfit.trip.outfit==3,"new trip captures selected costume");Life.Tick(outfit,outfit.trip.finishUtc);Check(outfit.pending[0].photos.All(p=>p.outfit==3),"photos wear departure outfit");
            var letter=outfit.letters.First();int affection=outfit.company;Check(!Life.ReadLetter(outfit,letter.id)&&!Life.Reply(outfit,letter.id,0)&&outfit.company==affection,"letters cannot award repeatedly");
            int following=0;for(int i=0;i<1000;i++){var s=Life.New(1,100+i);s.tutorial=4;Life.DirectionPick(s,Direction.Work);Life.Depart(s,1);if(s.trip.direction==Direction.Work)following++;}Check(following>540&&following<660,"direction is a 60 percent preference, not a command");
            foreach(int seed in new[]{7,38,901}){var s=RunYear(Direction.Friends,seed);Check(s.coins>=0&&s.energy>=0&&s.mood>=0,"alternate worlds never fail");}
            report.Add("PASS "+count+" assertions");Console.WriteLine(report.Last());File.WriteAllLines("docs/rules-results.txt",report.ToArray());
        }
#if POTATO_STANDALONE
        public static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
#endif
    }
}
