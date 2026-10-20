using System;
using System.Collections.Generic;
using System.Linq;
namespace PotatoLife
{
    public enum Direction { Study, Explore, Work, Friends }
    [Serializable] public class Trip
    {
        public int number,place,outfit,bag=-1,weather,season,normalDuration;
        public Direction direction;
        public long startUtc,finishUtc;
    }
    [Serializable] public class Photo
    {
        public int id,trip,outfit,season;public string caption;
        // A newly delivered postcard stays sealed until the player opens it in the album.
        public bool unopened;
    }
    [Serializable] public class Letter
    {
        public int id,friend,chapter,kind;public string title,body,previousReplies="";public bool read;public int reply=-1;
    }
    [Serializable] public class ReturnGift
    {
        public Trip trip=new Trip();public int coins,wisdom,charm,company,energy,mood;
        public List<Photo> photos=new List<Photo>();public Letter letter=new Letter();public string highlight;
    }
    [Serializable] public class PotatoState
    {
        public int version=1,seed=19,rng,coins=120,wisdom,charm,company,completed,selectedBag=-1,outfit,petClicks,petTotal,tutorial;
        public float energy=85,mood=80;
        public bool demo=true,autoRoam=true,travelling,graduated;
        public Direction direction=Direction.Study;
        public long lastUtc;public Trip trip=new Trip();
        public long dailyDay;public int dailyPets,dailyReturns,dailyReads,dailyClaimed;
        // Daily board: each slot shows one task; claiming swaps in a new one at once, up to Tuning.DailyCap a day.
        public int[] dailyTask={0,1,2},dailyBase=new int[3];public int dailyClaims,dailyStudy,dailyTodos,dailyDeparts,dailyReplies,dailyPlaces;
        // Companion study: seconds this session, the running segment and the next stretch break.
        public bool studyRunning,studyBreak;public long studySince;public int studySession,studyBlocks,studyTotal,todoRewards,todoNext=1;
        public List<Todo> todos=new List<Todo>();
        public int room;public List<int> rooms=new List<int>{0,1,2};
        // Where the player dragged each decoration, per room; anything not listed uses World.Slots.
        public List<Spot> spots=new List<Spot>();
        public string ending="",lastHighlight="欢迎来到香港，房间里的光刚刚好。";
        public int[] directionCount=new int[4],friendBond=new int[3],friendChapter=new int[3];
        public bool[] festivals=new bool[4],questClaimed=new bool[3];
        public int festivalPending=-1;
        public List<ReturnGift> pending=new List<ReturnGift>();
        public List<Photo> album=new List<Photo>();public List<Letter> letters=new List<Letter>();
        public List<int> owned=new List<int>(),placed=new List<int>(),costumes=new List<int>{0};
        public List<string> memories=new List<string>();
    }
    [Serializable] public class Spot
    {
        public int room,id,x,y;
    }
    [Serializable] public class Todo
    {
        public int id;public string text;public bool done;
    }
    public class DailyTask
    {
        // kind: 0 pets, 1 returns, 2 reads, 3 study minutes, 4 own todos, 5 departures, 6 replies, 7 room changes
        public string title;public int kind,need;
        public DailyTask(string t,int k,int n){title=t;kind=k;need=n;}
    }
    public class Place
    {
        public int id;public Direction direction;public string name,caption;public int art;
        public Place(int i,Direction d,string n,string c,int a){id=i;direction=d;name=n;caption=c;art=a;}
    }
    public class ShopItem
    {
        public string name,description;public int price,type,shop;
        // Bonuses while placed: on each return (mood, energy, company; wisdom only after study trips),
        // per companion-study block (studyCoins, studyWisdom) and per rewarded own todo (todoCoins).
        public int mood,energy,wisdom,company,studyCoins,studyWisdom,todoCoins;
        public ShopItem(string n,int p,int t,string d,int shop=0){name=n;price=p;type=t;description=d;this.shop=shop;}
    }
    public static class Tuning
    {
        public const int DemoSeconds=30,MaxPending=3,TripsPerSeason=4,YearTrips=16;
        public const int MinTripSeconds=1800,MaxTripSeconds=7200,PetRewards=5;
        public const int TripCoins=35,FestivalCoins=200,DailyCoins=20,DailyCap=9,MaxPlaced=8;
        public const int StudyBreakSeconds=2700,StudyBlockSeconds=900,StudyBlockCoins=5,StudyDailyBlocks=16;
        public const int TodoCoins=8,TodoDailyRewards=5,MaxTodos=30,TodoLength=30;
        public const float NormalHomeRecoveryPerSecond=.02f,DemoHomeRecoveryPerSecond=3f;
    }
    public static class World
    {
        public static readonly string[] Directions={"好好学习","到处玩玩","打工赚钱","交交朋友"};
        public static readonly string[] DirectionNotes={"常去图书馆，也可能被海风拐个弯。","带相机出门，给没去过的地方留一页。","奶茶、找零和一句谢谢，都是生活经验。","约一顿饭，认识几位不用客气的朋友。"};
        public static readonly string[] Seasons={"春 · 初来乍到","夏 · 越来越熟","秋 · 有人惦记","冬 · 满载而归"};
        public static readonly string[] Weather={"晴天","云朵散步","小雨","海风","回南天","凉凉的早晨"};
        public static readonly string[] Friends={"阿晴","阿乐","荣叔"};
        public static readonly string[] FriendNotes={"总怕自己准备得不够的同学","习惯说没事、其实也需要陪伴的朋友","记得你少冰口味的茶餐厅老板"};
        public static readonly string[] CostumeNames={"小红书包","兔子灯帽","龙舟头巾","花灯披肩","毕业帽"};
        public static readonly string[] FestivalNames={"春日灯会","夏日龙舟","中秋花灯","冬日毕业派对"};
        public static readonly string[] Endings={"学霸土豆","旅行家土豆","打工皇帝土豆","人缘王土豆"};
        public static readonly DailyTask[] DailyTasks={
            new DailyTask("摸摸土豆子三次",0,3),new DailyTask("收好一份外出手账",1,1),new DailyTask("读一封未读来信",2,1),
            new DailyTask("陪土豆子专注 25 分钟",3,25),new DailyTask("完成一项自己的待办",4,1),new DailyTask("送土豆子出门一次",5,1),
            new DailyTask("给朋友回一封信",6,1),new DailyTask("给小屋换一件摆设",7,1),new DailyTask("再摸摸土豆子五次",0,5),
            new DailyTask("陪土豆子专注 45 分钟",3,45),new DailyTask("完成两项自己的待办",4,2)};
        // Prepared replies: warm, ordinary and tired. None of them is punished; each still moves the friendship on.
        public static readonly string[] ReplyTone={"积极","一般","消极"};
        public static readonly string[] ReplyLines={"好呀！下次一起走走，我请你喝奶茶。","收到啦，我把自己的小办法写给你，慢慢来就好。","最近有点忙也有点累，可能要晚点再见，你也照顾好自己。"};
        public static readonly string[] ReplyNotes={"默契 +5 · 陪伴 +2","默契 +3 · 陪伴 +2 · 智慧 +2","默契 +1 · 陪伴 +1 · 体力 +5"};
        public static readonly string[] Praise={
            "你认真的样子，在发光哦。","今天的你，也比昨天多走了一小步。","慢慢来也没关系，你一直在前进。","能坐下来专注，本身就很了不起。",
            "累了就伸个懒腰，我会一直在这里。","你写下的每一个字，都会变成底气。","被你陪着学习，我也变聪明了一点。","不用和别人比，和自己比就很棒啦。",
            "这一页做完，就奖励自己一口好吃的吧。","香港的风很忙，你也很努力。","你比自己以为的更有耐心。","今天的进度，我都悄悄记下来了。"};
        public static readonly string[,] LetterTitles={
            {"要不要一起找教室？","今天，我在课堂上举手了","下一段路，也敢问问题"},
            {"吃顿不聊效率的饭，好吗？","今天想请你帮一点忙","毕业照背面，写着你的名字"},
            {"照旧少冰，已经记住啦","靠窗那张桌，又热闹了一点","毕业那天，大桌给你留着"}};
        public static readonly Place[] Places={
            new Place(0,Direction.Study,"图书馆四楼","空调还是有点冷，刚好把刚学会的知识捂热。",0),
            new Place(1,Direction.Study,"靠窗自习室","今天划掉了三项待办，第四项写着：夸夸自己。",0),
            new Place(2,Direction.Study,"校园小课堂","问了一个不太成熟的问题，老师说：从这里开始很好。",0),
            new Place(3,Direction.Explore,"天星小轮","没赶上上一班船，却刚好赶上这一阵海风。",1),
            new Place(4,Direction.Explore,"山顶看云","导航说八分钟，没说是上坡八分钟。云很好看，值了。",1),
            new Place(5,Direction.Explore,"大澳慢慢走","走得慢一点，路边的小事情就会主动跑过来。",1),
            new Place(6,Direction.Work,"荣叔的茶餐厅","端稳了三杯奶茶，也接住了三句多谢。",2),
            new Place(7,Direction.Work,"街角便利店","找零越来越熟练，隔壁阿姨还认出了你的书包。",2),
            new Place(8,Direction.Work,"校园小卖部","今天的工钱不算多，但第一笔自己赚的钱闪闪发亮。",2),
            new Place(9,Direction.Friends,"校园草地","大家把便当打开，没有人问今天效率怎么样。",3),
            new Place(10,Direction.Friends,"社团小聚","原来一句一起走吗，就能成为一段友谊的开头。",3),
            new Place(11,Direction.Friends,"饭后散步","聊到路灯亮起来，回家的路好像短了一点。",3)};
        // Types: 0 decoration; 1 reusable trip item. No recurring rent or mandatory spending.
        public static readonly ShopItem[] Items={
            new ShopItem("窗边小绿植",60,0,"摆放后，回家时心情额外 +3。"){mood=3},new ShopItem("软乎乎地毯",100,0,"摆放后，回家时体力额外 +5。"){energy=5},
            new ShopItem("星星台灯",130,0,"摆放后，学习外出额外 +2 智慧。"){wisdom=2},new ShopItem("明信片挂绳",110,0,"把回忆挂起来，首次购买魅力 +3。"),
            new ShopItem("小熊抱枕",80,0,"摆放后，回家时陪伴额外 +2。"){company=2},new ShopItem("便当小包",45,1,"可反复携带；外出体力消耗减少 6。"),
            new ShopItem("迷你相机",90,1,"可反复携带；每次多带一张照片，最多 3 张。"),new ShopItem("薄荷雨伞",50,1,"可反复携带；雨天额外带回一张照片和一点好心情。"),
            // 素白良物: minimalist wood-and-white home goods (an original shop, not a real brand).
            new ShopItem("橡木小书架",180,0,"摆放后，学习外出额外 +3 智慧。",1){wisdom=3},new ShopItem("棉麻懒人沙发",260,0,"摆放后，回家时体力额外 +8。",1){energy=8},
            new ShopItem("香薰加湿器",150,0,"摆放后，回家时心情额外 +4。",1){mood=4},new ShopItem("原木挂钟",120,0,"陪伴学习每满 15 分钟，金币额外 +2。",1){studyCoins=2},
            new ShopItem("藤编收纳篮",90,0,"摆放后，回家时心情额外 +2。",1){mood=2},new ShopItem("棉麻落地灯",200,0,"陪伴学习每满 15 分钟，智慧额外 +1。",1){studyWisdom=1},
            new ShopItem("白色收纳推车",160,0,"摆放后，回家时体力额外 +4。",1){energy=4},new ShopItem("橡木边几",140,0,"摆放后，回家时陪伴额外 +3。",1){company=3},
            new ShopItem("亚麻靠垫",70,0,"摆放后，回家时体力 +3、心情 +1。",1){energy=3,mood=1},new ShopItem("白瓷马克杯",50,0,"陪伴学习每满 15 分钟，金币额外 +1。",1){studyCoins=1},
            new ShopItem("墙面置物架",150,0,"摆放后，回家时心情额外 +3。",1){mood=3},new ShopItem("软木留言板",120,0,"完成自己的待办时，金币额外 +3。",1){todoCoins=3}};
        public static readonly string[] Shops={"小屋小物","素白良物"};
        public static readonly string[] ShopNotes={"花费只发生在你点击购买时。没有房租和自动扣款，也没有必须买的东西。","素白良物 · 木与白的生活用品，简单、安静、好用。最多同时摆放 8 件。"};
        public static readonly string[] Rooms={"原木书桌房","木柜大床房","奶咖窗景房","海景大平层","山顶别墅"};
        public static readonly string[] RoomNotes={"悬空书桌、白色吊柜和暖光灯带，窗外就是维港。","整排原木吊柜和软包床头，两盏壁灯，安静又宽敞。","窗边小床、日落油画和奶咖色衣柜，午后阳光最好。","整面落地窗对着维港，大沙发和开放式客厅，晚上能看灯光。","挑高木梁、通往花园露台的落地门，壁炉和楼梯，远处是海和小岛。"};
        public static readonly string[] RoomArt={"Art/Home","Art/Home2","Art/Home3","Art/Home4","Art/Home5"};
        // One-off unlock in coins; the first three rooms are free. There is still no rent.
        public static readonly int[] RoomPrice={0,0,0,1500,3000};
        // Where each decoration stands in each room (1536x1024 px): x, y, w, h from the top-left.
        public static readonly int[][][] Slots={
            new int[][]{new[]{830,372,88,98},new[]{500,808,560,165},new[]{650,300,86,118},new[]{805,480,190,78},new[]{205,515,140,140},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{520,346,54,70},new[]{737,10,92,92},new[]{268,868,170,118},new[]{420,385,80,255},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{45,468,110,100},new[]{422,378,46,40},new[]{1000,482,180,90},new[]{40,40,170,140}},
            new int[][]{new[]{838,310,82,92},new[]{500,808,560,165},new[]{238,300,80,100},new[]{1040,190,190,78},new[]{330,470,130,130},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{952,340,50,62},new[]{1085,40,92,92},new[]{268,868,170,118},new[]{610,272,80,250},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{120,520,110,100},new[]{325,358,40,38},new[]{1060,350,170,85},new[]{1255,150,140,120}},
            new int[][]{new[]{95,365,85,92},new[]{500,808,560,165},new[]{752,333,70,96},new[]{650,178,190,70},new[]{300,500,120,120},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{660,300,52,66},new[]{452,58,84,84},new[]{268,868,170,118},new[]{505,330,75,245},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{60,470,100,90},new[]{432,332,42,38},new[]{292,250,110,60},new[]{850,180,90,85}},
            new int[][]{new[]{1240,330,80,90},new[]{500,808,560,165},new[]{300,385,65,88},new[]{165,215,125,50},new[]{205,380,100,100},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{455,410,48,60},new[]{1290,60,90,90},new[]{268,868,170,118},new[]{1150,330,75,250},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{60,400,100,90},new[]{345,440,40,36},new[]{20,330,170,80},new[]{165,60,120,100}},
            new int[][]{new[]{300,290,70,80},new[]{500,808,560,165},new[]{1195,262,70,90},new[]{395,250,110,45},new[]{1220,560,110,110},null,null,null,new[]{110,720,200,135},new[]{1170,800,270,215},new[]{1290,300,48,55},new[]{230,40,80,80},new[]{268,868,170,118},new[]{1040,330,75,250},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{150,505,100,90},new[]{1400,320,40,35},new[]{1110,95,120,60},new[]{1135,170,100,85}}};
        public static string DiaryEnding(int e)
        {
            string[] lines={"那本越翻越厚的笔记，最后变成了一封继续研究的邀请。你没有赶着成为最厉害的土豆，只是一直舍不得放下好奇心。","这一年拍下的路，连成了自己的地图。你决定把这些普通又闪亮的角落讲给更多人听，旅行家土豆的小频道今天开张。","你已经能一手端稳奶茶，一边和熟客打招呼。新的工作邀请来到信箱里，留下来的理由里，有本事，也有人情。","毕业那天，房间有点挤，大家却都说刚刚好。你最珍惜的成果，是这些可以随时说一句一起吃饭吗的人。"};return lines[Math.Max(0,Math.Min(3,e))];
        }
    }
}
