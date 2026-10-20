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
        public string ending="",lastHighlight="欢迎来到香港，房间里的光刚刚好。";
        public int[] directionCount=new int[4],friendBond=new int[3],friendChapter=new int[3];
        public bool[] festivals=new bool[4],questClaimed=new bool[3];
        public int festivalPending=-1;
        public List<ReturnGift> pending=new List<ReturnGift>();
        public List<Photo> album=new List<Photo>();public List<Letter> letters=new List<Letter>();
        public List<int> owned=new List<int>(),placed=new List<int>(),costumes=new List<int>{0};
        public List<string> memories=new List<string>();
    }
    public class Place
    {
        public int id;public Direction direction;public string name,caption;public int art;
        public Place(int i,Direction d,string n,string c,int a){id=i;direction=d;name=n;caption=c;art=a;}
    }
    public class ShopItem
    {
        public string name,description;public int price,type;
        public ShopItem(string n,int p,int t,string d){name=n;price=p;type=t;description=d;}
    }
    public static class Tuning
    {
        public const int DemoSeconds=30,MaxPending=3,TripsPerSeason=4,YearTrips=16;
        public const int MinTripSeconds=1800,MaxTripSeconds=7200,PetRewards=5;
        public const int TripCoins=35,FestivalCoins=200,DailyCoins=20;
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
        public static readonly string[] DailyTitles={"摸摸土豆子三次","收好一份外出手账","读一封未读来信"};
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
            new ShopItem("窗边小绿植",60,0,"摆放后，回家时心情额外 +3。"),new ShopItem("软乎乎地毯",100,0,"摆放后，回家时体力额外 +5。"),
            new ShopItem("星星台灯",130,0,"摆放后，学习外出额外 +2 智慧。"),new ShopItem("明信片挂绳",110,0,"把回忆挂起来，首次购买魅力 +3。"),
            new ShopItem("小熊抱枕",80,0,"摆放后，回家时陪伴额外 +2。"),new ShopItem("便当小包",45,1,"可反复携带；外出体力消耗减少 6。"),
            new ShopItem("迷你相机",90,1,"可反复携带；每次多带一张照片，最多 3 张。"),new ShopItem("薄荷雨伞",50,1,"可反复携带；雨天额外带回一张照片和一点好心情。")};
        public static string DiaryEnding(int e)
        {
            string[] lines={"那本越翻越厚的笔记，最后变成了一封继续研究的邀请。你没有赶着成为最厉害的土豆，只是一直舍不得放下好奇心。","这一年拍下的路，连成了自己的地图。你决定把这些普通又闪亮的角落讲给更多人听，旅行家土豆的小频道今天开张。","你已经能一手端稳奶茶，一边和熟客打招呼。新的工作邀请来到信箱里，留下来的理由里，有本事，也有人情。","毕业那天，房间有点挤，大家却都说刚刚好。你最珍惜的成果，是这些可以随时说一句一起吃饭吗的人。"};return lines[Math.Max(0,Math.Min(3,e))];
        }
    }
}
