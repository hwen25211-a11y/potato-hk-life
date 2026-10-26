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
        // v0.1.3: stop = city postcard spot passed on the way home (0 none); story = 1 for the Tai O trip with 阿晴.
        public int stop,story;
    }
    [Serializable] public class Photo
    {
        public int id,trip,outfit,season;public string caption;
        // A newly delivered postcard stays sealed until the player opens it in the album.
        public bool unopened;
        // Later trips that brought the same postcard again: one line per visit, the picture is kept once.
        public string revisits="";
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
        // v0.1.3: 阿晴的周末邀请 and the travel frame's picture (a city postcard id, 0 = not chosen).
        public WeekendStory weekend=new WeekendStory();public int framePhoto;
    }
    // One-off story. Every field starts at 0 = not yet, so older saves begin cleanly and each step pays out once.
    [Serializable] public class WeekendStory
    {
        // stage: 0 not invited, 1 invitation waiting, 2 route chosen (A: packing), 3 A: on the trip, 4 note waiting, 5 replied
        // route: 1 大澳, 2 校园附近吃饭, 3 自己待着. bag: trip item id + 1, 0 = nothing. rain: 0 not yet, 1 unread, 2 seen.
        // rainReply / reply: chosen line + 1. echo: 0 none, 1 waits for the next ordinary return, 2 delivered on echoTrip.
        public int stage,route,bag,trip,rain,rainReply,reply,echo,echoTrip;public long invitedUtc;
    }
    // Real Hong Kong photos supplied by the owner (Resources/Photos/<key>.jpg, <key>_thumb.jpg).
    // Places that cannot be confirmed are filed as 城市漫游. Captions never claim when or in what weather a photo was taken.
    public class PhotoSpot
    {
        public int id,width,height;public string key,area,title,note,back;
        public PhotoSpot(int i,string k,string a,string t,int w,int h,string n,string b){id=i;key=k;area=a;title=t;width=w;height=h;note=n;back=b;}
        public bool Portrait{get{return height>width;}}
    }
    public class Keepsake
    {
        public string name,line;public Keepsake(string n,string l){name=n;line=l;}
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
        public const int DemoSeconds=5,MaxPending=3,TripsPerSeason=4,YearTrips=16;
        public const int MinTripSeconds=1800,MaxTripSeconds=7200,PetRewards=5;
        public const int TripCoins=35,FestivalCoins=200,DailyCoins=20,DailyCap=9,MaxPlaced=8;
        public const int StudyBreakSeconds=2700,StudyBlockSeconds=900,StudyBlockCoins=5,StudyDailyBlocks=16;
        public const int TodoCoins=8,TodoDailyRewards=5,MaxTodos=30,TodoLength=30;
        // The weekend story pays the same base reward whichever route and reply is chosen.
        public const int StoryCoins=30,StoryMood=5,StoryCompany=3,StoryBond=3,FrameItem=20,FirstSpot=100;
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
        // Types: 0 decoration; 1 reusable trip item; 2 carried card, active once owned. No recurring rent or mandatory spending.
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
            new ShopItem("墙面置物架",150,0,"摆放后，回家时心情额外 +3。",1){mood=3},new ShopItem("软木留言板",120,0,"完成自己的待办时，金币额外 +3。",1){todoCoins=3},
            // v0.1.3: drawn in code, not in Items.png. Click it in the room to choose a collected city postcard.
            new ShopItem("旅行相框",60,0,"竖版木相框，挂一张收藏的城市明信片，点它可以换照片。"),
            // 港硕必备 (Items2.png, 4x2): what new postgraduates buy in their first weeks. Type 2 cards work once owned, no placing.
            new ShopItem("英式转换插头",30,0,"三脚插座终于对得上。陪伴学习每满 15 分钟，智慧额外 +1。",2){studyWisdom=1},
            new ShopItem("小电饭煲",120,0,"自己煮一锅饭，省钱又暖胃。回家时体力额外 +5。",2){energy=5},
            new ShopItem("抽湿机",220,0,"回南天墙壁不再冒水。回家时心情额外 +4。",2){mood=4},
            new ShopItem("座地风扇",90,0,"香港的夏天全靠它。回家时体力 +2、心情 +2。",2){energy=2,mood=2},
            new ShopItem("折叠晾衣架",60,0,"房间再小也有地方晾衣服。回家时心情额外 +2。",2){mood=2},
            new ShopItem("保温水壶",50,0,"图书馆空调太冷时的救星。陪伴学习每满 15 分钟，金币额外 +1。",2){studyCoins=1},
            new ShopItem("交通储值卡",50,2,"随身带着，不用摆放。搭车不用找零，每次回家体力 +3。",2){energy=3},
            new ShopItem("本地电话卡",40,2,"随身带着，不用摆放。和朋友联络方便了，每次回家陪伴 +2。",2){company=2}};
        public static readonly string[] Shops={"小屋小物","素白良物","港硕必备"};
        public static readonly string[] ShopNotes={"花费只发生在你点击购买时。没有房租和自动扣款，也没有必须买的东西。","素白良物 · 木与白的生活用品，简单、安静、好用。最多同时摆放 8 件。","刚来香港最常买的几样：转换插头、电饭煲、抽湿机……卡片类随身生效，不占摆放位置。"};
        public static readonly string[] Rooms={"原木书桌房","木柜大床房","奶咖窗景房","海景大平层","山顶别墅"};
        public static readonly string[] RoomNotes={"悬空书桌、白色吊柜和暖光灯带，窗外就是维港。","整排原木吊柜和软包床头，两盏壁灯，安静又宽敞。","窗边小床、日落油画和奶咖色衣柜，午后阳光最好。","整面落地窗对着维港，大沙发和开放式客厅，晚上能看灯光。","挑高木梁、通往花园露台的落地门，壁炉和楼梯，远处是海和小岛。"};
        public static readonly string[] RoomArt={"Art/Home","Art/Home2","Art/Home3","Art/Home4","Art/Home5"};
        // One-off unlock in coins; the first three rooms are free. There is still no rent.
        public static readonly int[] RoomPrice={0,0,0,1500,3000};
        // Where each decoration stands in each room (1536x1024 px): x, y, w, h from the top-left.
        public static readonly int[][][] Slots={
            new int[][]{new[]{830,372,88,98},new[]{500,808,560,165},new[]{650,300,86,118},new[]{805,480,190,78},new[]{205,515,140,140},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{520,346,54,70},new[]{737,10,92,92},new[]{268,868,170,118},new[]{420,385,80,255},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{45,468,110,100},new[]{422,378,46,40},new[]{1000,482,180,90},new[]{40,40,170,140},new[]{1215,400,110,150},new[]{610,410,56,44},new[]{450,340,105,92},new[]{1000,560,95,130},new[]{410,500,100,170},new[]{1180,540,170,135},new[]{372,350,38,85},null,null},
            new int[][]{new[]{838,310,82,92},new[]{500,808,560,165},new[]{238,300,80,100},new[]{1040,190,190,78},new[]{330,470,130,130},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{952,340,50,62},new[]{1085,40,92,92},new[]{268,868,170,118},new[]{610,272,80,250},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{120,520,110,100},new[]{325,358,40,38},new[]{1060,350,170,85},new[]{1255,150,140,120},new[]{1270,400,110,150},new[]{800,410,50,40},new[]{1040,520,105,92},new[]{1290,520,95,130},new[]{420,510,100,170},new[]{1120,580,180,145},new[]{720,355,38,85},null,null},
            new int[][]{new[]{95,365,85,92},new[]{500,808,560,165},new[]{752,333,70,96},new[]{650,178,190,70},new[]{300,500,120,120},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{660,300,52,66},new[]{452,58,84,84},new[]{268,868,170,118},new[]{505,330,75,245},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{60,470,100,90},new[]{432,332,42,38},new[]{292,250,110,60},new[]{850,180,90,85},new[]{1010,390,110,150},new[]{870,410,48,38},new[]{760,365,95,84},new[]{1290,560,95,130},new[]{380,500,100,170},new[]{1000,640,180,145},new[]{930,420,38,85},null,null},
            new int[][]{new[]{1240,330,80,90},new[]{500,808,560,165},new[]{300,385,65,88},new[]{165,215,125,50},new[]{205,380,100,100},null,null,null,new[]{1222,610,220,145},new[]{1170,800,270,215},new[]{455,410,48,60},new[]{1290,60,90,90},new[]{268,868,170,118},new[]{1150,330,75,250},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{60,400,100,90},new[]{345,440,40,36},new[]{20,330,170,80},new[]{165,60,120,100},new[]{1395,200,110,150},new[]{1050,840,50,40},new[]{150,440,105,92},new[]{1240,540,95,130},new[]{300,560,100,170},new[]{150,650,190,150},new[]{1090,800,38,85},null,null},
            new int[][]{new[]{300,290,70,80},new[]{500,808,560,165},new[]{1195,262,70,90},new[]{395,250,110,45},new[]{1220,560,110,110},null,null,null,new[]{110,720,200,135},new[]{1170,800,270,215},new[]{1290,300,48,55},new[]{230,40,80,80},new[]{268,868,170,118},new[]{1040,330,75,250},new[]{1060,595,115,170},new[]{1035,860,125,115},new[]{150,505,100,90},new[]{1400,320,40,35},new[]{1110,95,120,60},new[]{1135,170,100,85},new[]{960,400,110,150},new[]{300,700,50,40},new[]{330,620,105,92},new[]{1330,590,95,130},new[]{170,600,100,170},new[]{1100,860,190,150},new[]{600,950,38,85},null,null}};
        // Ending paths are shown side by side, never ranked. Ids match Endings.
        public static readonly string[] EndingPaths={"继续求知","记录城市","在城市立足","与人同行"};
        public static readonly PhotoSpot[] Spots={
            new PhotoSpot(100,"tram-139","城市漫游","叮叮车经过街口",1020,640,"叮叮车慢悠悠地开过来，车身上的字比我读过的文献标题还大。","等过马路的时候，一辆叮叮车刚好停在街口。上下两层，开得比走路快不了多少。土豆子不赶时间，就站在路边，看它叮叮两声，慢慢开走。"),
            new PhotoSpot(102,"central-wheel","中环海滨","摩天轮和高楼",1020,628,"摩天轮转得很慢，旁边的高楼一动不动，好像在比谁更有耐心。","海滨长廊上，有人跑步，有人遛狗，有人坐在台阶上发呆。土豆子想数摩天轮有多少个车厢，数到一半就忘了，干脆不数了。"),
            new PhotoSpot(103,"chungking-mansions","尖沙咀","重庆大厦门口",1016,632,"红绿灯前停着小巴，招牌和人潮几秒钟就换一个画面。","弥敦道上永远有人在等过马路。门口有问路的、拎着大包小包的，空气里飘着咖喱的味道。土豆子没有进去，只在路口看了一会儿这里的热闹。"),
            new PhotoSpot(104,"night-arches","城市漫游","夜里亮着灯的大楼",1200,1600,"晚上的大楼一格一格亮着灯，像一本没合上的笔记本。","写完作业出来已经很晚。抬头看见一栋楼，窗户一格一格，有的亮，有的暗。土豆子想：每一格里，大概也有人在赶 deadline 吧。"),
            new PhotoSpot(105,"back-alley","城市漫游","夜里的后巷",1600,1200,"拉闸后的后巷很安静，椅子还摆在原地，好像主人只是去吃个宵夜。","抄近路回家，拐进一条后巷。铁闸拉下来了，梯子靠在墙边，一张木椅子等着明天开铺。白天的香港很吵，夜里的小巷像是在偷偷休息。"),
            new PhotoSpot(106,"nam-cheong-pawn","城市漫游","南昌押的招牌",1200,1600,"一个「押」字挂得很高，楼上的窗格子比字还多。","南昌押的招牌从楼上一路写到楼下，生怕路人看不见。土豆子站在对面马路，第一次认真想「押」字是什么意思，回家查了查，学到一个新词。"),
            new PhotoSpot(107,"colour-tenements","城市漫游","彩色的旧楼",1600,1200,"蓝的、黄的、粉的，每一格窗户外都挂着自己的冷气机。","楼顶长满天线，每一层都挂着冷气机和晾着的衣服。土豆子想起自己租的房间，窗外也是这样一格一格的邻居：谁也不认识谁，但都在认真过日子。"),
            new PhotoSpot(108,"red-taxi-arches","城市漫游","红色的士驶过拱门",1080,1440,"白色的拱门很安静，红色的士一下就开过去了。","等红灯的时候，一辆红色的士从白色大楼前开过。土豆子还没来得及举起相机，车已经拐弯了，只好在心里按了一下快门。"),
            new PhotoSpot(109,"monster-building","鲗鱼涌","益昌大厦的天井",900,1600,"抬头只看见一小块蓝天，四周的窗户密得像蜂巢。","穿过楼下的小店，就到了这个天井。很多人举着手机仰拍，土豆子也仰着头站了很久。脖子有点酸，但这一小块天空很好看。"),
            new PhotoSpot(110,"stonecutters-harbour","城市漫游","远处的昂船洲大桥",1600,1200,"海上停满了工程船，远处大桥的两根塔细细地立着，像在帮大家量天有多高。","从高处往西边看，昂船洲大桥站在山前，码头的吊机排成一排。近处的渡轮刚靠岸，一条快艇在海上划出白线。土豆子数工程船数到一半，又忘了。"),
            new PhotoSpot(111,"central-across-harbour","维多利亚港","隔着海看中环",1440,1080,"两艘船在海面上划出白线，对岸的高楼一座挨着一座，山腰上也是房子。","站在海这边，对岸的中环像一排排得整整齐齐的书脊，阳光把海面照得发亮。土豆子想，原来每天赶路的那一边，从远处看是这个样子。"),
            new PhotoSpot(112,"canal-road-villain","鹅颈桥","桥底下的打小人",1200,1600,"红色胶凳叠得高高的，背后一辆叮叮车刚好开过。","天桥底下，一排小摊摆着红色胶凳和神坛，招牌上写着「打小人」。有人排队，有人路过回头看。土豆子没有试，只在旁边坐了一会儿，听着拍纸的声音，觉得这城市的烦恼也有地方放。")};
        // Removed at the owner's request; older saves lose these cards (each turns into +1 company) and the id is never reused.
        public static readonly int[] RetiredSpots={101};
        public static PhotoSpot Spot(int id){return Spots.FirstOrDefault(p=>p.id==id);}
        // 阿晴的周末邀请. Index 0-2 = route A/B/C; replies and echoes are [route, reply].
        public const string StoryInvite="报告终于交了。想找一天出去走走，要不要一起？";
        public static readonly string[] StoryRoutes={"去大澳慢慢走。","在校园附近吃顿饭。","今天想自己待着。"};
        public static readonly string[] StoryRouteNotes={"一次外出，和平时出门一样用时间和体力。那天预报有雨。","不用出远门，在校园附近吃顿饭。","留在小屋休息，阿晴会明白的。"};
        // Bags on the Tai O trip: none, bento (5), camera (6), umbrella (7). Each one is its own complete fragment.
        public static readonly string[] StoryBagHints={"什么都不带，也能好好走完这一天。","找个地方坐下来，一起吃饭。","记下沿路的风景和小事。","下雨也不怕，两个人可以共一把伞。"};
        public static readonly string[] StoryBagTitles={"大澳 · 雨后的巴士站","大澳 · 屋檐下的便当","大澳 · 棚屋窗外的雨","大澳 · 一把伞下的两个人"};
        public static readonly string[] StoryBagLines={
            "什么也没带，回程时在巴士站等车，雨又下大了。原本以为今天会被雨打乱，最后反而坐下来聊了很久。",
            "找到一张有屋檐的长椅，把便当打开。阿晴分了你一块街口刚炸好的虾饼，说这才叫出门吃饭。",
            "相机里存了一排棚屋、水面和晾着的咸鱼。最喜欢的一张，是阿晴趴在栏杆上看雨。",
            "薄荷雨伞不算大，两个人挤在下面，肩膀各湿了一半，谁也没提要不要再买一把。"};
        public const string StoryRain="真的下雨了。要找个地方等一等吗？";
        public static readonly string[] StoryRainReplies={"找个地方喝杯东西。","雨不大，我们慢慢走。","稍后再看。"};
        public static readonly string[] StoryRainLines={
            "消息还没来得及回，阿晴已经找到一处屋檐，朝你招了招手。",
            "你们钻进一家小店，点了两杯热柠茶，看雨从铁皮屋顶上滑下来。",
            "雨不大，你们沿着水边慢慢走，鞋子湿了，话却越说越多。"};
        // Tai O note, chosen by the rain reply sent on the way (none / drink / walk); it never quotes a line the player did not send.
        public static readonly string[] StoryTaiONotes={
            "今天在巴士站等车的时候，我才发现自己很久没有这样歇一会儿了。最近连休息都觉得应该安排得很好。",
            "今天坐下来等雨的时候，我才发现自己很久没有这样歇一会儿了。最近连休息都觉得应该安排得很好。",
            "今天在雨里慢慢走的时候，我才发现自己很久没有这样不赶路了。最近连休息都觉得应该安排得很好。"};
        public static readonly string[] StorySceneTitles={"","校园附近 · 饭堂靠窗的位子","小屋 · 一个人的下午"};
        public static readonly string[] StorySceneLines={"",
            "你们在校园附近的饭堂排队，阿晴犹豫了很久，最后还是点了平时那份烧味饭。窗外有人抱着电脑赶去上课，你们没有赶，慢慢喝完了一整碗例汤。",
            "你回了阿晴一句「今天想自己待着」，她回了一个点头的表情。下午你把窗帘拉开一半，煮了一碗即食面，看完上周存下的那集剧，什么都没有安排。"};
        public static readonly string[] StoryNotes={"",
            "报告交完以后，原本以为要好好庆祝一下。结果只是吃了顿饭，反而觉得刚刚好。谢谢你陪我喝完那碗汤。",
            "收到啦。我今天也在宿舍躺了一下午，原来不出门也算休息。下次想出去走走的时候，再一起。"};
        public static readonly string[,] StoryReplies={
            {"下次也可以只是吃顿饭。","我也会这样，慢慢练习吧。","最近我也需要一点自己的时间。"},
            {"下次换我请，去试试那家车仔面。","能一起坐一会儿就很好了。","下周我可能比较忙，晚点再约。"},
            {"谢谢你没有追问。","下次天气好，我们去大澳吧。","我今天过得挺好的。"}};
        public static readonly string[,] StoryResponses={
            {"好，下次不用做攻略。","原来你也会。知道这件事就安心一点了。","明白，想见面的时候再叫我。"},
            {"好呀，我先去打听哪家的汤底比较好。","嗯，我也是这样想的。","没关系，饭堂又跑不掉。"},
            {"不用谢，想说的时候再说。","好，我先记下来，不用急。","那就好，我也是。"}};
        public static readonly string[,] StoryEchoes={
            {"阿晴发来一张茶餐厅菜单的照片：「说好的，不做攻略，随便点。」","阿晴说她这周吃完午饭没有马上回邮件，下楼走了一圈：「在练习。」","这一路没有阿晴的消息。土豆子路过一家很安静的糖水铺，记下了名字，等她想出门的时候再说。"},
            {"阿晴发来消息：「打听到了，那家车仔面的咖喱汁是自己煮的。」","回家路上在饭堂门口碰见阿晴，她挥了挥手，没有停下来聊，但笑得很轻松。","门口贴着阿晴留的便利贴：「忙完记得吃饭。」"},
            {"阿晴分享了一首歌，没有附带任何话。","阿晴发来一张天气预报截图：「下周六好像不下雨。不过不急。」","阿晴说她也去楼下公园坐了一会儿，带着一本没看完的小说。"}};
        public static readonly Keepsake[] Keepsakes={
            new Keepsake("雨天巴士票","那天下雨，我们没有急着赶下一个地方。"),
            new Keepsake("饭堂的取餐小票","号码叫到我们的时候，话刚好说到一半。"),
            new Keepsake("留给自己的半日券","这半天不用回复任何人，也没关系。")};
        public static string DiaryEnding(int e)
        {
            string[] lines={"那本越翻越厚的笔记，最后变成了一封继续研究的邀请。你没有赶着成为最厉害的土豆，只是一直舍不得放下好奇心。","这一年拍下的路，连成了自己的地图。你决定把这些普通又闪亮的角落讲给更多人听，旅行家土豆的小频道今天开张。","你已经能一手端稳奶茶，一边和熟客打招呼。新的工作邀请来到信箱里，留下来的理由里，有本事，也有人情。","毕业那天，房间有点挤，大家却都说刚刚好。你最珍惜的成果，是这些可以随时说一句一起吃饭吗的人。"};return lines[Math.Max(0,Math.Min(3,e))];
        }
    }
}
