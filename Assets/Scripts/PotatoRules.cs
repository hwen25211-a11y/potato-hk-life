using System;
using System.Linq;
using System.Collections.Generic;
namespace PotatoLife
{
    public static class Life
    {
        public static PotatoState New(long utc,int seed){var s=new PotatoState{lastUtc=utc,seed=seed};EnsureDaily(s,utc);return s;}
        // Hong Kong calendar day; moving the system clock backwards cannot reset rewards.
        public static bool EnsureDaily(PotatoState s,long utc)
        {
            long day=(Math.Max(utc,s.lastUtc)+28800)/86400+1;if(day<=s.dailyDay)return false;
            s.dailyDay=day;s.dailyPets=s.dailyReturns=s.dailyReads=s.dailyClaimed=0;
            s.dailyClaims=s.dailyStudy=s.dailyTodos=s.dailyDeparts=s.dailyReplies=s.dailyPlaces=s.studyBlocks=s.todoRewards=0;s.dailyTask=new[]{0,1,2};s.dailyBase=new int[3];return true;
        }
        // Saves from v0.1.1 marked claimed slots with bits only; count them and move those slots on.
        public static void MigrateDaily(PotatoState s)
        {if(s.dailyClaims!=0)return;for(int i=0;i<3;i++)if((s.dailyClaimed&(1<<i))!=0){s.dailyClaims++;NextTask(s,i);}}
        public static DailyTask Task(PotatoState s,int slot){return World.DailyTasks[s.dailyTask[slot]];}
        static int Counter(PotatoState s,int kind)
        {return kind==0?s.dailyPets:kind==1?s.dailyReturns:kind==2?s.dailyReads:kind==3?s.dailyStudy:kind==4?s.dailyTodos:kind==5?s.dailyDeparts:kind==6?s.dailyReplies:s.dailyPlaces;}
        // Progress counts only what happened after the task appeared in its slot.
        public static int DailyProgress(PotatoState s,int id)
        {if(id<0||id>2)return 0;var t=Task(s,id);int v=Counter(s,t.kind)-s.dailyBase[id];if(t.kind==3)v/=60;return Math.Max(0,Math.Min(t.need,v));}
        public static bool DailyFull(PotatoState s){return s.dailyClaims>=Tuning.DailyCap;}
        public static bool DailyReady(PotatoState s,int id){return id>=0&&id<3&&!DailyFull(s)&&DailyProgress(s,id)>=Task(s,id).need;}
        public static bool ClaimDaily(PotatoState s,int id)
        {if(!DailyReady(s,id))return false;s.dailyClaims++;s.dailyClaimed|=1<<id;s.coins+=Tuning.DailyCoins;s.mood=Cap(s.mood+3);NextTask(s,id);return true;}
        // A claimed slot gets a different task straight away; the choice is deterministic and never touches the trip RNG.
        static void NextTask(PotatoState s,int slot)
        {
            int n=World.DailyTasks.Length;
            for(int k=1;k<=n;k++){int c=(int)((s.dailyDay*7+s.dailyClaims*3+slot*5+k)%n);if(!s.dailyTask.Contains(c)){s.dailyTask[slot]=c;break;}}
            s.dailyBase[slot]=Counter(s,Task(s,slot).kind);
        }
        public static float Cap(float v){return Math.Max(0,Math.Min(100,v));}
        public static int Random(PotatoState s,int min,int max){unchecked{s.rng++;uint x=(uint)(s.seed+s.rng*747796405);x=(x^(x>>16))*2246822519u;x^=x>>13;return min+(int)(x%(uint)(max-min));}}
        public static int Season(PotatoState s){return Math.Min(3,s.completed/Tuning.TripsPerSeason);}
        public static bool YearReady(PotatoState s){return s.completed>=Tuning.YearTrips;}
        public static void Remember(PotatoState s,string text){s.memories.Add(text);if(s.memories.Count>100)s.memories.RemoveAt(0);}
        public static void DirectionPick(PotatoState s,Direction d){if((int)d<0||(int)d>3)return;s.direction=d;if(s.tutorial==0)s.tutorial=1;}
        public static string DepartureLock(PotatoState s)
        {
            if(s.travelling)return "已经在路上啦";if(s.pending.Count>=Tuning.MaxPending)return "先收好门口的三份手账吧";
            if(s.festivalPending>=0)return "有一封节日邀请等你拆开";
            if(s.energy<25)return "土豆子正在充电，回家休息会自动恢复";
            if(YearReady(s)&&!s.graduated)return "先看看这一年的毕业手账吧";return "";
        }
        public static bool Depart(PotatoState s,long utc)
        {
            Tick(s,utc);if(DepartureLock(s)!="")return false;
            if(StoryTripWaiting(s)){Schedule(s,Math.Max(utc,s.lastUtc),true);s.weekend.stage=3;s.weekend.trip=s.trip.number;s.weekend.bag=s.trip.bag+1;Remember(s,"和阿晴出发去大澳，天上的云有点厚。");}
            else Schedule(s,Math.Max(utc,s.lastUtc));s.dailyDeparts++;if(s.tutorial==1)s.tutorial=2;return true;
        }
        static void Schedule(PotatoState s,long start,bool story=false)
        {
            int d=(int)s.direction;if(Random(s,0,100)>=60)d=(d+Random(s,1,4))%4;
            int place=d*3+Random(s,0,3);int normal=Random(s,Tuning.MinTripSeconds,Tuning.MaxTripSeconds+1);
            int n=s.completed+s.pending.Count+1;s.trip=new Trip{number=n,place=place,direction=(Direction)d,outfit=s.outfit,bag=s.selectedBag,weather=Random(s,0,6),season=Math.Min(3,(n-1)/4),normalDuration=normal,startUtc=start,finishUtc=start+(s.demo?Tuning.DemoSeconds:normal)};
            // The story trip is always Tai O in the rain; it uses the same clock, energy and RNG draws as any trip.
            if(story){s.trip.place=5;s.trip.direction=Direction.Explore;s.trip.weather=2;s.trip.story=1;}else s.trip.stop=CityStop(s,s.trip);
            s.travelling=true;s.energy=Cap(s.energy-(s.selectedBag==5?8:14));s.mood=Cap(s.mood-2);
        }
        public static bool Tick(PotatoState s,long utc)
        {
            // A monotonic high-water mark prevents duplicate rewards when the wall clock moves backwards.
            long now=Math.Max(utc,s.lastUtc);EnsureDaily(s,now);if(now==s.lastUtc)return false;
            bool changed=false;long homeSince=s.travelling?now:s.lastUtc;
            int guard=0;StoryRainCheck(s,now);
            while(s.travelling&&s.trip.finishUtc<=now&&guard++<Tuning.MaxPending)
            {
                if(s.pending.Count>=Tuning.MaxPending){s.travelling=false;homeSince=now;break;}
                long arrived=s.trip.finishUtc;s.pending.Add(MakeGift(s,s.trip));s.travelling=false;homeSince=arrived;changed=true;
                // A three-return mailbox cap is shared by online and offline play. No hidden backlog survives the cap.
                // Auto trips never answer the story for the player: they pause after the Tai O trip and while it waits to be packed.
                // An unanswered invitation does not stop them, so offline returns keep working.
                if(s.autoRoam&&s.tutorial>=4&&s.pending.Last().trip.story==0&&!StoryTripWaiting(s)&&s.pending.Count<Tuning.MaxPending&&s.energy>=39&&s.festivalPending<0&&(s.completed+s.pending.Count<Tuning.YearTrips||s.graduated))Schedule(s,arrived);
            }
            if(!s.travelling){float seconds=Math.Max(0,now-homeSince);float recovery=s.demo?Tuning.DemoHomeRecoveryPerSecond:Tuning.NormalHomeRecoveryPerSecond;s.energy=Cap(s.energy+seconds*recovery);s.mood=Cap(s.mood+seconds*recovery*.3f);}
            s.lastUtc=now;StoryCheck(s,now);return changed;
        }
        static ReturnGift MakeGift(PotatoState s,Trip t)
        {
            var g=new ReturnGift{trip=t,coins=Tuning.TripCoins+Random(s,0,16),wisdom=2,charm=2,company=3,energy=10,mood=8};
            if(t.direction==Direction.Study){g.wisdom+=8;g.highlight="把一个小问题想明白了。";}
            if(t.direction==Direction.Explore){g.charm+=8;g.highlight="带回了一阵刚刚好的海风。";}
            if(t.direction==Direction.Work){g.coins+=55;g.highlight="靠自己的小本事赚到了零花钱。";}
            if(t.direction==Direction.Friends){g.company+=8;g.mood+=6;g.highlight="今天有人主动说：下次还一起。";}
            foreach(int id in Active(s)){var item=World.Items[id];g.mood+=item.mood;g.energy+=item.energy;g.company+=item.company;if(t.direction==Direction.Study)g.wisdom+=item.wisdom;}
            int count=Random(s,1,4);if(t.bag==6||t.bag==7&&(t.weather==2||t.weather==4))count=Math.Min(3,count+1);
            if(t.story==1)
            {
                // The Tai O trip brings its own ticket and note; there is no real Tai O photo, so no city postcard stands in for it.
                g.highlight=World.StoryBagTitles[StoryBag(s)];g.photos.Add(new Photo{id=5,trip=t.number,outfit=t.outfit,season=t.season,caption=World.Places[5].caption+"\n和阿晴一起去的那天，路上下了雨。"});
                g.letter=new Letter{id=StoryLetterId(1),friend=0,kind=2,title="阿晴的纸条",body=World.StoryTaiONotes[Math.Min(2,s.weekend.rainReply)]};return g;
            }
            var known=new HashSet<int>(s.album.Select(p=>p.id));foreach(var gift in s.pending)foreach(var p in gift.photos)known.Add(p.id);
            var places=Enumerable.Range((int)t.direction*3,3).OrderBy(i=>known.Contains(i)).ThenBy(i=>(i-t.place+3)%3).Take(count).ToArray();
            foreach(int id in places)g.photos.Add(new Photo{id=id,trip=t.number,outfit=t.outfit,season=t.season,caption=World.Places[id].caption+"\n"+World.Weather[t.weather]+"，穿着"+World.CostumeNames[t.outfit]+"出门的一天。"});
            var spot=World.Spot(t.stop);if(spot!=null)g.photos.Add(new Photo{id=spot.id,trip=t.number,outfit=t.outfit,season=t.season,caption=spot.note});
            int friend=t.direction==Direction.Work?2:t.direction==Direction.Study?0:Random(s,0,2);g.letter=MakeLetter(s,t,friend);return g;
        }
        static Letter MakeLetter(PotatoState s,Trip t,int friend)
        {
            int chapter=s.friendChapter[friend]+1;
            // Pending gifts reserve story chapters too: several offline returns
            // must not each deliver the same chapter before the player can reply.
            if(chapter>3||s.letters.Any(l=>l.kind==0&&l.friend==friend&&l.chapter==chapter)||
                s.pending.Any(g=>g.letter.kind==0&&g.letter.friend==friend&&g.letter.chapter==chapter))
                return MakeDailyLetter(t,friend);
            string[,] bodies={
                {"我也刚到香港。刚才走错了楼，假装自己只是想多散一会儿步。下次一起找教室吧，迷路就有两颗土豆了。","上次你说不用一次就准备得很完美，我把那句话贴在了电脑旁边。这周终于在课堂上举手了，想先告诉你。","毕业之前想说，谢谢你陪我把陌生的路走熟。下一段学习还很长，但我已经不太怕问一个问题了。"},
                {"大家都说我看起来很稳，其实昨天也对着待办发了好久的呆。可以约一顿不聊效率的饭吗？","那天散步时你没有急着给建议，反而让我轻松了很多。我开始学着说，今天需要帮一点忙。","毕业照背面我写了你的名字。以后不管在哪座城，发一句一起吃饭吗，我都会认真回复。"},
                {"今天的奶茶给你少放了冰。看你背着书包来来回回，就知道最近忙。慢慢喝，饭要记得吃。","你上次介绍朋友来，他也喜欢这张靠窗桌。店里没多一块招牌，却多了几张熟悉的脸。","毕业那天带朋友来吧，我给你们留张大桌。以后回来不用预约，老位置一直记得。"}};
            return new Letter{id=t.number*10+friend,friend=friend,chapter=chapter,title=World.LetterTitles[friend,chapter-1],body=bodies[friend,chapter-1]+(s.mood<45?"\n累的时候，回家歇一歇也很好。":"")};
        }
        static Letter MakeDailyLetter(Trip t,int friend)
        {
            string[,] titles={{"自习室的小提醒","打印机又闹小脾气","书页边的小太阳"},{"今天的猫很有气场","发现一条阴凉的小路","替你试过食堂新菜"},{"菠萝包刚出炉","又见一位新同学","门口长出了新叶子"}};
            string[,] bodies={
                {"自习室的空调今天很认真。我带了外套，也给你留了靠窗的位置。","打印机又吃了一张纸。我决定先吃一块饼干，再和它讲道理。","今天在图书馆找到一本好看的书。页边有人画了小太阳，想把这个小发现告诉你。"},
                {"下楼买面包时看见一只胖猫，坐姿比我开组会还端正。照片下次给你看。","今天绕了远路，发现一条有树荫的小坡。下次不赶时间，一起慢慢走。","食堂的新菜名很长，味道倒挺朴素。我已经替你试过了，可以点。"},
                {"店里刚出炉的菠萝包还温着。忙完路过的话，记得先吃点东西。","今天来了一位把多谢说得特别认真的新同学，让我想起你刚来的时候。","门口那盆绿植终于长新叶了。店里生意和植物一样，慢慢照顾，总会有新动静。"}};
            int note=(t.number+t.weather+friend)%3;
            return new Letter{id=t.number*10+friend,friend=friend,kind=1,chapter=0,title=titles[friend,note],body=bodies[friend,note]};
        }
        public static void NormalizeLetters(PotatoState s)
        {
            var stories=new Dictionary<int,Letter>();var kept=new List<Letter>();
            foreach(var letter in s.letters)
            {
                if(letter.kind!=0){kept.Add(letter);continue;}
                if(letter.reply>=0){letter.read=true;s.friendChapter[letter.friend]=Math.Max(s.friendChapter[letter.friend],letter.chapter);}
                int key=letter.friend*4+letter.chapter;Letter first;
                if(!stories.TryGetValue(key,out first)){stories[key]=letter;letter.title=World.LetterTitles[letter.friend,letter.chapter-1];kept.Add(letter);continue;}
                // Keep earned stats, read state and every existing reply when
                // collapsing duplicates from older saves; never award them again.
                first.read|=letter.read||letter.reply>=0;
                if(letter.reply>=0)
                {
                    string note="第 "+(letter.id/10)+" 次外出后，你写了："+World.ReplyLines[letter.reply];
                    first.previousReplies=(first.previousReplies??"")+note+"\n";
                    if(first.reply<0)first.reply=letter.reply;
                }
                if(!string.IsNullOrEmpty(letter.previousReplies))first.previousReplies=(first.previousReplies??"")+letter.previousReplies;
            }
            s.letters=kept;var reserved=new HashSet<int>(stories.Keys);
            foreach(var gift in s.pending)
            {
                var letter=gift.letter;if(letter.kind!=0)continue;
                int key=letter.friend*4+letter.chapter;
                if(!reserved.Add(key))gift.letter=MakeDailyLetter(gift.trip,letter.friend);
            }
        }
        static void TrimLetters(PotatoState s)
        {
            // Story chapters and their reply history remain in the album of letters.
            while(s.letters.Count>80){int index=s.letters.FindIndex(l=>l.kind==1);if(index<0)break;s.letters.RemoveAt(index);}
        }
        public static int Collect(PotatoState s,long utc)
        {
            Tick(s,utc);int n=s.pending.Count;if(n==0)return 0;
            foreach(var g in s.pending)
            {
                if(g.trip.story==1&&s.weekend.stage==3)s.weekend.stage=4;
                // One ordinary return after the reply carries 阿晴's echo, once.
                if(g.trip.story==0&&s.weekend.echo==1){s.weekend.echo=2;s.weekend.echoTrip=g.trip.number;Remember(s,"阿晴的回响："+StoryEcho(s));}
                s.coins+=g.coins;s.wisdom+=g.wisdom;s.charm+=g.charm;s.company+=g.company;s.energy=Cap(s.energy+g.energy);s.mood=Cap(s.mood+g.mood);
                s.directionCount[(int)g.trip.direction]++;s.completed++;s.lastHighlight=g.highlight;
                foreach(var p in g.photos)
                {
                    var old=s.album.FirstOrDefault(x=>x.id==p.id);if(old==null){p.unopened=true;s.album.Add(p);continue;}
                    // Same picture again: the usual +1 company, plus a line of this visit; the thumbnail is not duplicated.
                    s.company++;var lines=(old.revisits??"").Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries).ToList();lines.Add("第 "+g.trip.number+" 次外出，又带回了这里。");old.revisits=string.Join("\n",lines.Skip(Math.Max(0,lines.Count-8)).ToArray());
                }
                s.letters.Add(g.letter);s.friendBond[g.letter.friend]+=2;
                Remember(s,"第 "+s.completed+" 次回来："+World.Places[g.trip.place].name+"。"+g.highlight);
            }
            s.dailyReturns+=n;s.pending.Clear();s.petClicks=0;if(s.tutorial==2)s.tutorial=3;
            for(int i=0;i<4;i++)if(!s.festivals[i]&&s.completed>=2+i*4){s.festivalPending=i;break;}
            TrimLetters(s);StoryCheck(s,s.lastUtc,true);
            return n;
        }
        public static string Pet(PotatoState s)
        {
            s.petTotal++;
            s.dailyPets++;
            if(s.petClicks>=Tuning.PetRewards)return "土豆子蹭了蹭你：不拿奖励，也想多陪你一会儿。";
            s.petClicks++;bool lucky=Random(s,0,20)==0;int amount=lucky?10:5;s.coins+=amount;s.company++;s.mood=Cap(s.mood+2);s.energy=Cap(s.energy+2);
            return (lucky?"小惊喜！":"")+"金币 +"+amount+" · 陪伴 +1";
        }
        public static void OpenAlbum(PotatoState s){if(s.tutorial==3)s.tutorial=4;}
        public static bool OpenPostcard(PotatoState s,int id){var p=s.album.FirstOrDefault(x=>x.id==id);if(p==null||!p.unopened)return false;p.unopened=false;return true;}
        public static bool ChooseRoom(PotatoState s,int room){if(room<0||room>=World.Rooms.Length||s.room==room||!s.rooms.Contains(room))return false;s.room=room;return true;}
        public static bool BuyRoom(PotatoState s,int room)
        {if(room<0||room>=World.Rooms.Length||s.rooms.Contains(room)||s.coins<World.RoomPrice[room])return false;s.coins-=World.RoomPrice[room];s.rooms.Add(room);Remember(s,"拿到了「"+World.Rooms[room]+"」的钥匙。");return true;}
        // A decoration's rectangle on the room art (1536x1024 px): the dragged spot if any, otherwise the default slot.
        public static int[] ItemRect(PotatoState s,int id)
        {var slot=World.Slots[s.room][id];var spot=s.spots.FirstOrDefault(p=>p.room==s.room&&p.id==id);return spot==null?slot:new[]{spot.x,spot.y,slot[2],slot[3]};}
        public static bool MoveItem(PotatoState s,int id,int x,int y)
        {
            if(!s.placed.Contains(id)||World.Slots[s.room][id]==null)return false;var slot=World.Slots[s.room][id];
            x=Math.Max(0,Math.Min(1536-slot[2],x));y=Math.Max(0,Math.Min(1024-slot[3],y));
            var spot=s.spots.FirstOrDefault(p=>p.room==s.room&&p.id==id);if(spot==null)s.spots.Add(spot=new Spot{room=s.room,id=id});spot.x=x;spot.y=y;s.dailyPlaces++;return true;
        }
        public static bool ResetSpots(PotatoState s){return s.spots.RemoveAll(p=>p.room==s.room)>0;}
        public static bool ReadLetter(PotatoState s,int id)
        {var l=s.letters.FirstOrDefault(x=>x.id==id);if(l==null||l.read)return false;l.read=true;s.dailyReads++;s.mood=Cap(s.mood+5);s.company+=2;return true;}
        public static bool Reply(PotatoState s,int id,int choice)
        {
            var l=s.letters.FirstOrDefault(x=>x.id==id);if(l==null||l.reply>=0||choice<0||choice>2)return false;if(l.kind==2)return StoryReply(s,choice);ReadLetter(s,id);l.reply=choice;s.dailyReplies++;s.friendBond[l.friend]+=choice==0?5:choice==1?3:1;s.company+=choice==2?1:2;
            if(choice==1)s.wisdom+=2;if(choice==2)s.energy=Cap(s.energy+5);s.friendChapter[l.friend]=Math.Max(s.friendChapter[l.friend],l.chapter);Remember(s,"回信给"+World.Friends[l.friend]+"："+World.ReplyLines[choice]);return true;
        }
        public static bool Buy(PotatoState s,int id)
        {if(id<0||id>=World.Items.Length||s.owned.Contains(id)||s.coins<World.Items[id].price)return false;s.coins-=World.Items[id].price;s.owned.Add(id);s.charm+=3;if(World.Items[id].type==0&&s.placed.Count<Tuning.MaxPlaced){s.placed.Add(id);s.dailyPlaces++;}Remember(s,"房间里多了"+World.Items[id].name+"，越来越像家了。");return true;}
        public static bool PlaceItem(PotatoState s,int id)
        {if(!s.owned.Contains(id)||World.Items[id].type!=0)return false;if(s.placed.Contains(id)){s.placed.Remove(id);s.dailyPlaces++;return true;}if(s.placed.Count>=Tuning.MaxPlaced)return false;s.placed.Add(id);s.dailyPlaces++;return true;}
        public static bool Pack(PotatoState s,int id){if(s.travelling||id!=-1&&(!s.owned.Contains(id)||World.Items[id].type!=1))return false;s.selectedBag=id;return true;}
        public static bool Dress(PotatoState s,int id){if(!s.costumes.Contains(id))return false;s.outfit=id;return true;}
        public static bool Festival(PotatoState s,int choice)
        {
            int i=s.festivalPending;if(i<0||i>3||s.festivals[i]||choice<0||choice>1)return false;
            s.festivals[i]=true;s.festivalPending=-1;s.coins+=Tuning.FestivalCoins;s.company+=8;if(choice==0)s.charm+=8;else s.wisdom+=8;
            if(!s.costumes.Contains(i+1))s.costumes.Add(i+1);
            s.album.Add(new Photo{id=12+i,trip=s.completed,outfit=i+1,season=i,unopened=true,caption=choice==0?"把节日的热闹拍下来，也把朋友放进画面里。":"先安静地许个愿，再把祝福写给惦记的人。"});
            Remember(s,World.FestivalNames[i]+"：获得"+World.CostumeNames[i+1]+"，把这一天收进了手账。");
            for(int j=0;j<4;j++)if(!s.festivals[j]&&s.completed>=2+j*4){s.festivalPending=j;break;}
            StoryCheck(s,s.lastUtc);return true;
        }
        public static bool QuestReady(PotatoState s,int id){return id==0?s.petTotal>=3:id==1?s.completed>=1:id==2&&s.owned.Count>=1;}
        public static bool ClaimQuest(PotatoState s,int id){if(id<0||id>2||s.questClaimed[id]||!QuestReady(s,id))return false;s.questClaimed[id]=true;s.coins+=25;s.mood=Cap(s.mood+5);return true;}
        // Companion study counts real seconds and stops by itself at every 45-minute stretch break.
        static int NextBreak(PotatoState s){return (s.studySession/Tuning.StudyBreakSeconds+1)*Tuning.StudyBreakSeconds;}
        public static int StudySeconds(PotatoState s,long utc)
        {return s.studySession+(s.studyRunning?(int)Math.Max(0,Math.Min(utc-s.studySince,NextBreak(s)-s.studySession)):0);}
        public static int StudyToBreak(PotatoState s,long utc){return NextBreak(s)-StudySeconds(s,utc);}
        public static bool StudyStart(PotatoState s,long utc)
        {if(s.studyRunning||s.studyBreak)return false;s.studyRunning=true;s.studySince=Math.Max(utc,s.lastUtc);return true;}
        // Folds the running time into the session. Returns true when the stretch break has just been reached.
        public static bool StudySettle(PotatoState s,long utc)
        {
            if(!s.studyRunning)return false;int limit=NextBreak(s)-s.studySession;int add=(int)Math.Max(0,Math.Min(utc-s.studySince,limit));
            s.studySession+=add;s.studySince+=add;s.dailyStudy+=add;s.studyTotal+=add;CreditStudy(s);
            if(add<limit)return false;s.studyRunning=false;s.studyBreak=true;return true;
        }
        static void CreditStudy(PotatoState s)
        {
            int blocks=Math.Min(Tuning.StudyDailyBlocks,s.dailyStudy/Tuning.StudyBlockSeconds);
            while(s.studyBlocks<blocks){s.studyBlocks++;s.coins+=Tuning.StudyBlockCoins+Active(s).Sum(i=>World.Items[i].studyCoins);s.wisdom+=1+Active(s).Sum(i=>World.Items[i].studyWisdom);s.company++;}
        }
        public static int StudyBlockCoins(PotatoState s){return Tuning.StudyBlockCoins+Active(s).Sum(i=>World.Items[i].studyCoins);}
        public static void StudyPause(PotatoState s,long utc){StudySettle(s,utc);s.studyRunning=false;}
        public static bool StudyContinue(PotatoState s,long utc)
        {if(!s.studyBreak)return false;s.studyBreak=false;s.studyRunning=true;s.studySince=Math.Max(utc,s.lastUtc);return true;}
        public static int StudyEnd(PotatoState s,long utc)
        {StudySettle(s,utc);int total=s.studySession;s.studyRunning=s.studyBreak=false;s.studySession=0;if(total>=60)Remember(s,"陪土豆子专注了 "+(total/60)+" 分钟。");return total;}
        public static Todo AddTodo(PotatoState s,string text)
        {
            text=(text??"").Replace("\n"," ").Trim();if(text.Length==0||s.todos.Count(t=>!t.done)>=Tuning.MaxTodos)return null;
            if(text.Length>Tuning.TodoLength)text=text.Substring(0,Tuning.TodoLength);var todo=new Todo{id=s.todoNext++,text=text};s.todos.Add(todo);return todo;
        }
        // Returns coins earned, 0 once today's rewards are used up, or -1 when nothing changed.
        public static int CompleteTodo(PotatoState s,int id)
        {
            var t=s.todos.FirstOrDefault(x=>x.id==id);if(t==null||t.done)return -1;t.done=true;s.dailyTodos++;
            while(s.todos.Count(x=>x.done)>20)s.todos.Remove(s.todos.First(x=>x.done));
            if(s.todoRewards>=Tuning.TodoDailyRewards)return 0;s.todoRewards++;int coins=Tuning.TodoCoins+Active(s).Sum(i=>World.Items[i].todoCoins);
            s.coins+=coins;s.mood=Cap(s.mood+2);s.wisdom++;return coins;
        }
        public static bool RemoveTodo(PotatoState s,int id){return s.todos.RemoveAll(x=>x.id==id)>0;}
        public static void Mode(PotatoState s,bool demo,long utc)
        {
            Tick(s,utc);if(s.demo==demo)return;long now=Math.Max(utc,s.lastUtc);
            if(s.travelling){double progress=Progress(s,now);int duration=demo?Tuning.DemoSeconds:s.trip.normalDuration;s.trip.startUtc=now-(long)(progress*duration);s.trip.finishUtc=s.trip.startUtc+duration;}
            s.demo=demo;
        }
        public static double Progress(PotatoState s,long utc){if(!s.travelling)return 0;return Math.Max(0,Math.Min(1,(double)(Math.Max(utc,s.lastUtc)-s.trip.startUtc)/Math.Max(1,s.trip.finishUtc-s.trip.startUtc)));}
        // ---- v0.1.4: city postcards, 阿晴的周末邀请, travel frame ----
        // Ordinary trips pass a city spot on the way home: every exploring trip and every second trip otherwise.
        // Unseen spots come first; no RNG is drawn, so existing trip outcomes are unchanged.
        static int CityStop(PotatoState s,Trip t)
        {
            if(t.direction!=Direction.Explore&&t.number%2!=0)return 0;int n=World.Spots.Length,start=(t.number*3)%n;
            var known=new HashSet<int>(s.album.Select(p=>p.id));foreach(var g in s.pending)foreach(var p in g.photos)known.Add(p.id);
            for(int k=0;k<n;k++){var spot=World.Spots[(start+k)%n];if(!known.Contains(spot.id))return spot.id;}
            return World.Spots[start].id;
        }
        // Items whose bonuses count now: placed decorations plus carried cards (type 2) the player owns.
        public static IEnumerable<int> Active(PotatoState s){return s.placed.Concat(s.owned.Where(i=>World.Items[i].type==2));}
        // Drops retired city postcards from a save before it is validated; each removed card becomes +1 company.
        public static void RetireSpots(PotatoState s)
        {
            var gone=new HashSet<int>(World.RetiredSpots);if(s.album!=null)s.company+=s.album.RemoveAll(p=>p!=null&&gone.Contains(p.id));
            if(s.pending!=null)foreach(var g in s.pending){if(g==null)continue;if(g.photos!=null)s.company+=g.photos.RemoveAll(p=>p!=null&&gone.Contains(p.id));if(g.trip!=null&&gone.Contains(g.trip.stop))g.trip.stop=0;}
            if(s.trip!=null&&gone.Contains(s.trip.stop))s.trip.stop=0;if(gone.Contains(s.framePhoto))s.framePhoto=0;
        }
        public static int LegacyPhotos(PotatoState s){return s.album.Count(p=>p.id<16);}
        public static int CityPhotos(PotatoState s){return s.album.Count(p=>World.Spot(p.id)!=null);}
        public static bool StoryTripWaiting(PotatoState s){return s.weekend.stage==2&&s.weekend.route==1;}
        public static int StoryLetterId(int route){return 9000+route;}
        // 0 none, 1 bento, 2 camera, 3 umbrella: the index into the StoryBag* texts.
        public static int StoryBag(PotatoState s){int b=s.weekend.bag-1;return b==5?1:b==6?2:b==7?3:0;}
        public static int Keepsake(PotatoState s){return s.weekend.stage==5&&s.weekend.route>0?s.weekend.route-1:-1;}
        public static string StoryEcho(PotatoState s){var w=s.weekend;return w.route>0&&w.reply>0?World.StoryEchoes[w.route-1,w.reply-1]:"";}
        public static string StoryResponse(PotatoState s){var w=s.weekend;return w.route>0&&w.reply>0?World.StoryResponses[w.route-1,w.reply-1]:"";}
        // The invitation arrives after the first trip has been collected and the album opened, and only while
        // nothing else is happening: no returns waiting, no festival letter, not away - except right after the player
        // collects, so a running chain of auto trips cannot hold it back for ever. Older saves qualify too.
        public static bool StoryCheck(PotatoState s,long utc,bool collecting=false)
        {
            if(s.weekend.stage!=0||s.tutorial<4||s.completed<1||s.travelling&&!collecting||s.pending.Count>0||s.festivalPending>=0)return false;
            s.weekend.stage=1;s.weekend.invitedUtc=Math.Max(utc,s.lastUtc);Remember(s,"阿晴发来消息："+World.StoryInvite);return true;
        }
        public static bool StoryChoose(PotatoState s,int route)
        {
            if(s.weekend.stage!=1||route<1||route>3)return false;s.weekend.route=route;
            if(route==1){s.weekend.stage=2;Remember(s,"答应了阿晴：去大澳慢慢走。");return true;}
            // The other two routes are short scenes at home: no trip, no energy, the note arrives at once.
            s.weekend.stage=4;s.letters.Add(new Letter{id=StoryLetterId(route),friend=0,kind=2,title="阿晴的纸条",body=World.StoryNotes[route-1]});s.friendBond[0]+=2;
            Remember(s,route==2?"和阿晴在校园附近吃了顿饭。":"今天想自己待着，阿晴说明白。");return true;
        }
        static void StoryRainCheck(PotatoState s,long now)
        {if(s.weekend.stage==3&&s.weekend.rain==0&&s.travelling&&s.trip.story==1&&Progress(s,now)>=.5)s.weekend.rain=1;}
        public static bool StoryRainSeen(PotatoState s){if(s.weekend.rain!=1)return false;s.weekend.rain=2;return true;}
        // 1 = find somewhere for a drink, 2 = keep walking. Only while still on the trip; "稍后再看" just marks it seen.
        public static bool StoryRain(PotatoState s,int choice)
        {
            if(choice<1||choice>2||s.weekend.stage!=3||s.weekend.rain==0||s.weekend.rainReply!=0||!s.travelling||s.trip.story!=1)return false;
            s.weekend.rain=2;s.weekend.rainReply=choice;return true;
        }
        // The same base reward for every route and every reply; the keepsake differs by route.
        public static bool StoryReply(PotatoState s,int choice)
        {
            var w=s.weekend;if(w.stage!=4||choice<0||choice>2)return false;var l=s.letters.FirstOrDefault(x=>x.kind==2&&x.id==StoryLetterId(w.route));if(l==null||l.reply>=0)return false;
            ReadLetter(s,l.id);l.reply=choice;w.reply=choice+1;w.stage=5;w.echo=1;s.dailyReplies++;
            s.coins+=Tuning.StoryCoins;s.mood=Cap(s.mood+Tuning.StoryMood);s.company+=Tuning.StoryCompany;s.friendBond[0]+=Tuning.StoryBond;
            Remember(s,"回信给阿晴："+World.StoryReplies[w.route-1,choice]+" 收好了「"+World.Keepsakes[w.route-1].name+"」。");return true;
        }
        public static bool ChooseFrame(PotatoState s,int id)
        {var p=s.album.FirstOrDefault(x=>x.id==id);if(p==null||p.unopened||World.Spot(id)==null||s.framePhoto==id)return false;s.framePhoto=id;return true;}
        // Saves from v0.1.3 and earlier have no story; anything out of range falls back to a state that cannot pay twice.
        public static void NormalizeStory(PotatoState s)
        {
            if(s.weekend==null)s.weekend=new WeekendStory();var w=s.weekend;
            if(w.stage<0||w.stage>5||w.route<0||w.route>3||w.stage>=2&&w.route==0||w.reply<0||w.reply>3||w.rainReply<0||w.rainReply>2||w.rain<0||w.rain>2||w.echo<0||w.echo>2||w.bag<0||w.bag>World.Items.Length)
            {bool done=w.stage>=5||w.reply>0;s.weekend=new WeekendStory{stage=done?5:0,route=done?Math.Max(1,Math.Min(3,w.route)):0,reply=done?Math.Max(1,Math.Min(3,w.reply)):0,echo=done?2:0};w=s.weekend;}
            // A story trip lost from the save (e.g. a crash mid-write) goes back to packing rather than vanishing.
            if(w.stage==3&&!(s.travelling&&s.trip.story==1)&&!s.pending.Any(g=>g.trip.story==1))w.stage=2;
            if(s.framePhoto!=0&&World.Spot(s.framePhoto)==null)s.framePhoto=0;
        }
        public static int SuggestEnding(PotatoState s){int best=0;for(int i=1;i<4;i++)if(s.directionCount[i]>s.directionCount[best])best=i;return best;}
        public static int[] AvailableEndings(PotatoState s){if(!YearReady(s))return new int[0];int best=SuggestEnding(s);return Enumerable.Range(0,4).Where(i=>s.directionCount[i]>=2||i==best).ToArray();}
        public static bool Graduate(PotatoState s,int e){if(!AvailableEndings(s).Contains(e))return false;s.ending=e.ToString();s.graduated=true;s.costumes.Add(4);s.costumes=s.costumes.Distinct().ToList();Remember(s,"学年纪念："+World.Endings[e]+"。每个普通的日子都算数。");return true;}
    }
}
