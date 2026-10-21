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
        static void Bump(PotatoState s,int kind,int n){if(kind==0)s.dailyPets+=n;else if(kind==1)s.dailyReturns+=n;else if(kind==2)s.dailyReads+=n;else if(kind==3)s.dailyStudy+=n*60;else if(kind==4)s.dailyTodos+=n;else if(kind==5)s.dailyDeparts+=n;else if(kind==6)s.dailyReplies+=n;else s.dailyPlaces+=n;}
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
            bool hasDaily=false;
            for(int seed=1;seed<=24;seed++)
            {
                var mail=Life.New(1000,seed);mail.tutorial=4;Life.DirectionPick(mail,Direction.Work);Life.Depart(mail,1000);Life.Tick(mail,87400);
                Check(mail.pending.Select(g=>g.letter).Where(l=>l.kind==0).GroupBy(l=>l.friend*4+l.chapter).All(g=>g.Count()==1),"offline batch reserves unique story chapters "+seed);
                hasDaily|=mail.pending.Any(g=>g.letter.kind==1);Life.Collect(mail,87400);
                var storyKeys=mail.letters.Where(l=>l.kind==0).Select(l=>l.friend*4+l.chapter).ToArray();
                if(mail.festivalPending>=0)Life.Festival(mail,0);
                Life.Depart(mail,87401);Life.Tick(mail,173801);Life.Collect(mail,173801);
                Check(mail.letters.Where(l=>l.kind==0).GroupBy(l=>l.friend*4+l.chapter).All(g=>g.Count()==1),"waiting for replies never duplicates stories "+seed);
                Check(storyKeys.All(k=>mail.letters.Any(l=>l.kind==0&&l.friend*4+l.chapter==k))&&mail.letters.Where(l=>l.kind==0).All(l=>l.chapter==1),"unreplied story chapters are retained without skipping ahead "+seed);
                foreach(var l in mail.letters.Where(l=>l.kind==0).ToArray())Life.Reply(mail,l.id,0);
                Check(mail.letters.Where(l=>l.kind==1).All(l=>l.chapter==0),"daily notes do not impersonate chapter one "+seed);
                var note=mail.letters.First(l=>l.kind==1);int chapterBefore=mail.friendChapter[note.friend];Life.Reply(mail,note.id,1);Check(mail.friendChapter[note.friend]==chapterBefore,"replying to a daily note does not advance story chapter "+seed);
            }
            Check(hasDaily,"daily notes fill repeated-friend offline returns");
            var oldMail=Life.New(1,1);oldMail.company=42;oldMail.letters.Add(new Letter{id=12,friend=2,chapter=1,read=true,reply=0,title="旧标题",body="旧正文"});oldMail.letters.Add(new Letter{id=22,friend=2,chapter=1,read=true,reply=1,title="旧标题",body="旧正文"});oldMail.pending.Add(new ReturnGift{trip=new Trip{number=3},letter=new Letter{id=32,friend=2,chapter=1}});
            Life.NormalizeLetters(oldMail);Check(oldMail.letters.Count==1&&oldMail.letters[0].read&&oldMail.letters[0].reply==0&&oldMail.letters[0].previousReplies.Contains("第 2 次外出"),"legacy duplicate stories merge without losing replies");
            Check(oldMail.company==42&&oldMail.friendChapter[2]==1&&oldMail.pending[0].letter.kind==1,"legacy pending duplicates become daily notes without replaying rewards");
            string priorReplies=oldMail.letters[0].previousReplies;Life.NormalizeLetters(oldMail);Check(oldMail.letters.Count==1&&oldMail.letters[0].previousReplies==priorReplies,"letter migration is idempotent");
            var finishedLetters=RunYear(Direction.Work);Check(finishedLetters.letters.Where(l=>l.kind==0).GroupBy(l=>l.friend*4+l.chapter).All(g=>g.Count()==1),"full year has no repeated story chapters");Check(finishedLetters.letters.Any(l=>l.kind==1)&&finishedLetters.letters.Where(l=>l.kind==0&&l.friend==2).Select(l=>l.chapter).OrderBy(x=>x).SequenceEqual(new[]{1,2,3}),"story progresses through three unique chapters and then daily notes");Check(finishedLetters.letters.Where(l=>l.kind==0&&l.friend==2).Select(l=>l.title).Distinct().Count()==3,"each friend chapter has its own title");
            int following=0;for(int i=0;i<1000;i++){var s=Life.New(1,100+i);s.tutorial=4;Life.DirectionPick(s,Direction.Work);Life.Depart(s,1);if(s.trip.direction==Direction.Work)following++;}Check(following>540&&following<660,"direction is a 60 percent preference, not a command");
            foreach(int seed in new[]{7,38,901}){var s=RunYear(Direction.Friends,seed);Check(s.coins>=0&&s.energy>=0&&s.mood>=0,"alternate worlds never fail");}
            // v0.1.2: daily board refreshes on claim, companion study, own todos and the home-goods shop.
            var board=Life.New(1,3);for(int i=0;i<3;i++)Life.Pet(board);int task0=board.dailyTask[0];Check(Life.ClaimDaily(board,0)&&board.dailyTask[0]!=task0&&board.dailyTask.Distinct().Count()==3,"claimed daily slot refreshes at once with a different task");Check(Life.DailyProgress(board,0)==0,"refreshed task counts only new progress");
            int claims=1,boardCoins=board.coins;for(int r=0;r<20;r++)for(int i=0;i<3;i++){var t=Life.Task(board,i);Bump(board,t.kind,t.need);if(Life.ClaimDaily(board,i))claims++;}
            Check(claims==Tuning.DailyCap&&board.coins==boardCoins+(Tuning.DailyCap-1)*Tuning.DailyCoins,"daily rewards stop at the cap");Check(Life.DailyFull(board)&&!Life.DailyReady(board,1),"nothing is ready after the cap");
            Life.Tick(board,86401);Check(board.dailyClaims==0&&board.dailyTask.SequenceEqual(new[]{0,1,2})&&board.dailyBase.All(b=>b==0),"new day resets the board");
            var legacyBoard=Life.New(1,3);legacyBoard.dailyClaimed=5;Life.MigrateDaily(legacyBoard);Check(legacyBoard.dailyClaims==2&&legacyBoard.dailyTask[0]!=0&&legacyBoard.dailyTask[2]!=2&&legacyBoard.dailyTask[1]==1,"v0.1.1 claimed slots are not claimable again");
            var study=Life.New(1000,5);int c0=study.coins;Check(Life.StudyStart(study,1000)&&!Life.StudyStart(study,1001),"study starts once");Check(!Life.StudySettle(study,1899)&&study.studyBlocks==0,"no reward before 15 minutes");Life.StudySettle(study,1900);Check(study.studyBlocks==1&&study.coins==c0+Tuning.StudyBlockCoins,"each 15 minute block rewards");
            Check(Life.StudySettle(study,6000)&&study.studyBreak&&!study.studyRunning&&Life.StudySeconds(study,99999)==Tuning.StudyBreakSeconds,"timer stops itself at the 45 minute break");Check(study.studyBlocks==3&&study.dailyStudy==Tuning.StudyBreakSeconds,"time past the break is not counted");
            Check(!Life.StudyStart(study,7000),"break needs confirming before counting again");Check(Life.StudyContinue(study,7000)&&study.studyRunning,"continue after stretching");Life.StudySettle(study,7900);Check(Life.StudySeconds(study,7900)==3600&&study.studyBlocks==4,"timer continues after the break");
            Life.StudyPause(study,8000);int frozen=Life.StudySeconds(study,99999);Check(!study.studyRunning&&frozen==3700,"pause freezes the timer");Check(Life.StudyEnd(study,9000)==frozen&&study.studySession==0,"ending returns the session length");
            var back=Life.New(5000,6);Life.StudyStart(back,5000);Life.StudySettle(back,4000);Check(Life.StudySeconds(back,4000)==0&&back.dailyStudy==0,"clock rollback adds no study time");
            var marathon=Life.New(1,8);Life.StudyStart(marathon,1);long tm=1;for(int i=0;i<12;i++){tm+=Tuning.StudyBreakSeconds;Life.StudySettle(marathon,tm);Life.StudyContinue(marathon,tm);}Check(marathon.studyBlocks==Tuning.StudyDailyBlocks,"study rewards cap per day");
            var todo=Life.New(1,9);Check(Life.AddTodo(todo,"   ")==null,"blank todo rejected");var first=Life.AddTodo(todo,"读完第三章");Check(first!=null&&first.text=="读完第三章","own todo added");Check(Life.AddTodo(todo,new string('长',50)).text.Length==Tuning.TodoLength,"todo text trimmed to the limit");
            int todoCoins=todo.coins;Check(Life.CompleteTodo(todo,first.id)==Tuning.TodoCoins&&todo.coins==todoCoins+Tuning.TodoCoins&&todo.dailyTodos==1,"finishing an own todo rewards and counts");Check(Life.CompleteTodo(todo,first.id)==-1&&todo.coins==todoCoins+Tuning.TodoCoins,"a todo cannot be finished twice");
            for(int i=0;i<10;i++)Life.CompleteTodo(todo,Life.AddTodo(todo,"小事"+i).id);Check(todo.todoRewards==Tuning.TodoDailyRewards,"todo rewards cap per day");int open=todo.todos.Count(t=>!t.done);var gone=todo.todos.First(t=>!t.done).id;Check(Life.RemoveTodo(todo,gone)&&todo.todos.Count(t=>!t.done)==open-1,"todo can be deleted");
            var rich=Life.New(1,2);rich.coins=99999;for(int i=0;i<World.Items.Length;i++)Life.Buy(rich,i);Check(rich.owned.Count==World.Items.Length,"every good can be bought");Check(rich.placed.Count==Tuning.MaxPlaced&&rich.placed.All(i=>World.Items[i].type==0),"placement is capped to decorations");
            Check(World.Slots.Length==World.Rooms.Length&&World.Slots.All(room=>room.Length==World.Items.Length&&Enumerable.Range(0,World.Items.Length).All(i=>(World.Items[i].type==0)==(room[i]!=null))),"every decoration has a slot in every room");
            var mover=Life.New(1,2);Check(!Life.ChooseRoom(mover,0)&&Life.ChooseRoom(mover,2)&&mover.room==2&&!Life.ChooseRoom(mover,3),"rooms can be chosen freely and only real rooms");
            var estate=Life.New(1,2);estate.coins=1000;Check(!Life.BuyRoom(estate,3)&&!Life.ChooseRoom(estate,3)&&estate.coins==1000,"locked room needs enough coins");estate.coins=5000;Check(Life.BuyRoom(estate,3)&&estate.coins==3500&&Life.ChooseRoom(estate,3)&&!Life.BuyRoom(estate,3),"room unlocks once and can be moved into");Check(!Life.BuyRoom(estate,0),"free rooms are already owned");
            Life.Buy(estate,9);int[] home9=Life.ItemRect(estate,9);Check(home9.SequenceEqual(World.Slots[3][9]),"undragged item uses the room default");Check(Life.MoveItem(estate,9,99999,-50)&&Life.ItemRect(estate,9)[0]==1536-World.Slots[3][9][2]&&Life.ItemRect(estate,9)[1]==0,"dragged item stays inside the room");
            Life.ChooseRoom(estate,0);Check(Life.ItemRect(estate,9).SequenceEqual(World.Slots[0][9]),"each room keeps its own layout");Life.ChooseRoom(estate,3);Check(Life.ResetSpots(estate)&&Life.ItemRect(estate,9).SequenceEqual(World.Slots[3][9]),"layout resets to default");Check(!Life.MoveItem(estate,1,10,10),"only placed items can be dragged");
            var post=Life.New(1,4);Life.Depart(post,1);Life.Tick(post,31);Life.Collect(post,31);Check(post.album.Count>0&&post.album.All(p=>p.unopened),"new postcards arrive sealed");int sealedId=post.album[0].id;Check(Life.OpenPostcard(post,sealedId)&&!post.album[0].unopened&&!Life.OpenPostcard(post,sealedId),"postcard opens once by hand");
            var tired=post.letters[0];int bond=post.friendBond[tired.friend];float tiredEnergy=post.energy;Check(Life.Reply(post,tired.id,2)&&post.friendBond[tired.friend]==bond+1&&post.energy>=tiredEnergy&&post.friendChapter[tired.friend]>=tired.chapter&&!Life.Reply(post,tired.id,0),"a tired reply still moves the story without penalty");Check(!Life.Reply(post,tired.id,3),"only three reply tones");Check(World.Items.Count(i=>i.shop==1)==12,"twelve home goods in the second shop");
            var plain=Life.New(1000,3);plain.tutorial=4;var cozy=Copy(plain);cozy.owned.Add(9);cozy.placed.Add(9);Life.Depart(plain,1000);Life.Depart(cozy,1000);Life.Tick(plain,1030);Life.Tick(cozy,1030);Check(cozy.pending[0].energy==plain.pending[0].energy+8,"placed sofa adds energy on return");
            report.Add("PASS "+count+" assertions");Console.WriteLine(report.Last());File.WriteAllLines("docs/rules-results.txt",report.ToArray());
        }
#if POTATO_STANDALONE
        public static int Main(){try{Run();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
#endif
    }
}
