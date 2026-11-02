using System;
using System.Collections.Generic;
using System.Linq;
namespace PotatoLife
{
    // 新生活: pets and the Hong Kong life chapters. Every field starts empty, so older saves begin cleanly.
    [Serializable] public class CityState
    {
        public string pet="",ending="";public float food=60,clean=60,joy=60,health=95;public long petUtc;
        public int picked,friends,quiz,weekSim,weekCard,weekId,weekHome;public bool noPet,hkid;
        public List<int> answers=new List<int>(),ate=new List<int>(),stamps=new List<int>();
    }
    public class Breed
    {
        public string key,name,size,traits;public bool dog;
        public Breed(string k,string n,bool d,string s,string t){key=k;name=n;dog=d;size=s;traits=t;}
    }
    public class CityPlace
    {
        public string kind,name,where,note,photo,line;public int art;
        public CityPlace(string k,string n,string w,string t,int a,string p,string l){kind=k;name=n;where=w;note=t;art=a;photo=p;line=l;}
    }
    public class MtrLine
    {
        public string key,name,color;public int[] path;
        public MtrLine(string k,string n,string c,params int[] p){key=k;name=n;color=c;path=p;}
    }
    public static class City
    {
        public const int FeedCost=20,BathCost=30,LitterCost=10,VetCost=480,FirstReward=10;
        // Needs fall per minute in slow mode; demo mode is ten times faster so a short session still shows them.
        public const float FoodFall=.3f,CleanFall=.2f,JoyFall=.16f,HealthFall=.1f,HealthFloor=30,Low=25,DemoSpeed=10;
        public const int MaxAwayMinutes=1440;
        public static readonly Breed[] Breeds={
            new Breed("corgi","柯基",true,"中小型","短腿长腰，要控制体重，少上下跳。掉毛较多，每天都要出门。"),
            new Breed("westie","西高地",true,"小型","白毛要常梳理、定期美容，皮肤也要多留意。"),
            new Breed("shiba","柴犬",true,"中小型","运动量大、有主见，要耐心训练。换毛期掉毛很多。"),
            new Breed("poodle","贵宾",true,"小型","掉毛较少，但要定期美容。聪明黏人，需要人陪。"),
            new Breed("tong","唐狗",true,"中型","香港常见的本地混种，不少在领养机构等家。要训练和足够运动。"),
            new Breed("ginger","橘猫",false,"中型","贪吃，要控制体重。性格亲人，适合第一次养猫。"),
            new Breed("bsh","英短蓝猫",false,"中型","性格安静，但容易发胖，要定期梳毛。"),
            new Breed("ragdoll","布偶猫",false,"大型","长毛要经常梳理，购买和日常开销都比较高。"),
            new Breed("ash","美短虎斑",false,"中型","活泼好动，要有时间陪它玩。"),
            new Breed("siamese","暹罗猫",false,"中型","爱叫又黏人，租的小房子要考虑邻居。")};
        public static readonly string[][] Quiz={
            new[]{"租的房子可以养宠物吗？","租约写明可以","还没问过房东","租约不允许","先问清楚。很多租约不允许养，发现后可能要搬走或把宠物送走。"},
            new[]{"每天能留多少时间给它？","1 小时以上","半小时左右","赶论文时几乎没有","狗每天都要出门。猫也要清砂、喂食和陪玩，deadline 那周也一样。"},
            new[]{"每个月的开销算过吗？","粮、砂、疫苗都算了","大概想过","还没算","除了日常开销，看病可能是一笔突然的大开支。"},
            new[]{"毕业以后它怎么办？","已查好带它走的手续","留港工作继续养","还没想好","这是最重要的一题。它的一辈子，比你在香港的这一年长得多。"}};
        public static readonly string[][] Foods={
            new[]{"菠萝油","热菠萝包夹一块冻牛油。茶餐厅、面包店都有。","菠萝油一个！"},
            new[]{"丝袜奶茶","港式奶茶。点单可以说「走冰」「少甜」。","一杯热奶茶，少甜！"},
            new[]{"蛋挞","酥皮或牛油皮，刚出炉的最好吃。","蛋挞一个！"},
            new[]{"咖喱鱼蛋","街头小食，边走边吃。","咖喱鱼蛋一串！"},
            new[]{"车仔面","自己选面、选汤、选配料，按配料算钱。","车仔面，加鱼蛋！"},
            new[]{"云吞面","细面配鲜虾云吞，老字号很多。","云吞面一碗！"},
            new[]{"煲仔饭","天气冷时最受欢迎，记得刮锅底的饭焦。","煲仔饭一个！"},
            new[]{"西多士","炸面包配牛油和糖浆，茶餐厅下午茶经典。","西多士一份！"}};
        // Frames of the hand-held sheet per dish; dishes not listed are eaten with chopsticks at the table.
        public static int[] Held(int food){return food==0?new[]{2,4}:food==1?new[]{0,5}:food==2?new[]{1,4}:food==3?new[]{3,4}:food==7?new[]{4,4}:null;}
        // Fees are read off the Hospital Authority posters effective 1.1.2026.
        public static readonly string[][] Fees={
            new[]{"急症室（每次）","$400","$2,100"},new[]{"家庭医学诊所（每次）","$150","$500"},new[]{"家庭医学处方（每种）","$5","$40"},
            new[]{"专科诊所（每次）","$250","$850"},new[]{"专科处方（每种）","$20","$90"},new[]{"注射或敷药（每次）","$50","$250"},new[]{"住院普通病床（每日）","$300","$7,400"}};
        public static readonly string[] PlaceKinds={"景点","展览","看剧","购物"};
        public static readonly CityPlace[] Places={
            new CityPlace("景点","太平山顶","中环上山","看整个维港和高楼，傍晚最好看。",2,null,null),
            new CityPlace("景点","天星小轮","中环至尖沙咀","坐船过海，便宜又好看。",3,null,null),
            new CityPlace("景点","中环海滨","中环","摩天轮和海滨长廊。",-1,"central-wheel","tw"),
            new CityPlace("景点","维多利亚港","尖沙咀海边","隔着海看中环的高楼。",-1,"central-across-harbour","tw"),
            new CityPlace("景点","益昌大厦","鲗鱼涌","高楼围成的天井，这里是民居，拍照请小声。",-1,"monster-building","is"),
            new CityPlace("景点","鹅颈桥打小人","铜锣湾与湾仔之间","天桥底下的香港民俗。",-1,"canal-road-villain","is"),
            new CityPlace("展览","M+ 博物馆","西九文化区","当代视觉文化博物馆，海边草坪也适合发呆。",0,null,"tc"),
            new CityPlace("展览","香港故宫文化博物馆","西九文化区","看中国文物，留意学生优惠和免费日。",1,null,"tc"),
            new CityPlace("展览","大馆","中环","旧中区警署改建的古迹与艺术空间。",4,null,"tw"),
            new CityPlace("看剧","香港文化中心","尖沙咀","话剧、音乐会常在这里。在 URBTIX 买票。",5,null,"tw"),
            new CityPlace("购物","圆方","九龙站","大型商场，品牌多，奢侈品也集中。",6,null,"tc"),
            new CityPlace("购物","东荟城名店仓","东涌","奥特莱斯折扣商场，靠近机场。",7,null,"tc"),
            new CityPlace("购物","铜锣湾","港岛","时代广场、希慎广场和小店，买衣服常来。",-2,null,"is")};
        // Schematic map in a 860x580 box: x,y pairs per line. Not to scale.
        public static readonly MtrLine[] Lines={
            new MtrLine("tw","荃湾线","E2231A",230,90,260,180,360,230,360,270,360,360,360,430,300,430),
            new MtrLine("is","港岛线","0075C2",80,430,300,430,360,430,470,430,560,420,800,440),
            new MtrLine("kt","观塘线","00AB4E",420,360,360,300,360,270,360,230,470,190,560,190,660,270,720,300),
            new MtrLine("tko","将军澳线","7D499D",560,420,680,330,720,300,780,240),
            new MtrLine("tc","东涌线","F7943E",290,415,300,350,260,180,120,250,50,300),
            new MtrLine("ae","机场快线","00888E",282,420,290,345,245,170,110,240,30,360),
            new MtrLine("er","东铁线","53B7E8",360,430,400,350,470,190,510,100,520,20),
            new MtrLine("tm","屯马线","923011",60,60,270,220,400,350,500,300,560,190,640,90),
            new MtrLine("si","南港岛线","BAC429",360,430,330,550),
            new MtrLine("dl","迪士尼线","E777CB",120,250,150,320)};
        public static readonly object[][] Stations={
            new object[]{230,90,"荃湾"},new object[]{360,230,"太子"},new object[]{360,270,"旺角"},new object[]{360,360,"尖沙咀"},new object[]{360,430,"金钟"},new object[]{300,430,"中环"},
            new object[]{470,430,"铜锣湾"},new object[]{470,190,"九龙塘"},new object[]{560,190,"钻石山"},new object[]{50,300,"东涌"},new object[]{300,350,"九龙"},new object[]{400,350,"红磡"},
            new object[]{30,360,"机场"},new object[]{80,430,"坚尼地城"},new object[]{800,440,"柴湾"},new object[]{520,20,"罗湖"}};
        public static readonly string[][] Pay={
            new[]{"港铁","手机二维码和交通卡都能用。二维码要走有标记的闸机。","都可以"},
            new[]{"九巴等专营巴士","上车拍卡或扫码。部分路线的收费方式不同，以车上说明为准。","都可以"},
            new[]{"绿色小巴","部分小巴没有扫码机，只能用交通卡或现金。","看车而定"},
            new[]{"叮叮车 / 渡轮","最稳妥是交通卡。扫码是否支持，以现场机器为准。","看现场"}};
        public static readonly string[][] Taxi={new[]{"尖沙咀到中环","0"},new[]{"沙田到大埔","1"},new[]{"东涌到昂坪","2"}};
        public static readonly string[][] LaterPlans={
            new[]{"学英语","香港有政府资助和公益机构办的平价英语课程，适合想练口语的同学。报名条件要看身份。","开班时间和报名条件以最新公布为准。"},
            new[]{"做公益","社区中心、动物义工和环保活动都欢迎学生。周末去帮帮忙，也能认识本地朋友。","具体机构写进游戏前再逐家核实。"},
            new[]{"毕业留港","毕业前查清楚留港工作的签证和期限，提前准备简历。","政策以入境处最新公告为准。"},
            new[]{"离港收尾","退租、拿回押金、注销电话卡、处理交通卡余额，宠物的去向也要提前安排。","提前一个月列清单。"}};
        public static readonly string[] PetEndings={"你查清了疫苗、芯片、检疫和航空要求。这是一笔不小的开销，但它一直和你在一起。","你和新主人见了几次面，把它的习惯都写了下来。它的照片每个月都会寄来。"};
        public static Breed BreedOf(string key){return Breeds.FirstOrDefault(b=>b.key==key);}
        public static bool HasPet(PotatoState s){return BreedOf(s.city.pet)!=null;}
        public static bool IsDog(PotatoState s){var b=BreedOf(s.city.pet);return b!=null&&b.dog;}
        public static Breed MyPet(PotatoState s){return BreedOf(s.city.pet);}
        public static void Normalize(PotatoState s)
        {
            if(s.city==null)s.city=new CityState();var c=s.city;if(c.pet==null||BreedOf(c.pet)==null)c.pet="";if(c.ending==null)c.ending="";
            if(c.answers==null)c.answers=new List<int>();if(c.ate==null)c.ate=new List<int>();if(c.stamps==null)c.stamps=new List<int>();
            c.answers=c.answers.Where(a=>a>=0&&a<3).Take(Quiz.Length).ToList();c.quiz=c.answers.Count;c.ate=c.ate.Where(i=>i>=0&&i<Foods.Length).Distinct().ToList();c.stamps=c.stamps.Where(i=>i>=0&&i<Places.Length).Distinct().ToList();
            c.food=Fix(c.food,60);c.clean=Fix(c.clean,60);c.joy=Fix(c.joy,60);c.health=Math.Max(HealthFloor,Fix(c.health,95));c.picked=Math.Max(0,c.picked);c.friends=Math.Max(0,c.friends);
        }
        static float Fix(float v,float fallback){return float.IsNaN(v)||float.IsInfinity(v)?fallback:Math.Max(0,Math.Min(100,v));}
        public static bool Answer(PotatoState s,int choice){var c=s.city;if(c.quiz>=Quiz.Length||choice<0||choice>2)return false;c.answers.Add(choice);c.quiz=c.answers.Count;return true;}
        public static void ResetQuiz(PotatoState s){var c=s.city;c.noPet=false;c.quiz=0;c.answers.Clear();}
        public static int Risky(PotatoState s){return s.city.answers.Count(a=>a==2);}
        public static bool LeaseBlocked(PotatoState s){return s.city.answers.Count>0&&s.city.answers[0]==2;}
        // The lease question cannot be skipped: the game never lets the player keep a pet in secret.
        public static bool Adopt(PotatoState s,string key,long utc)
        {
            var c=s.city;if(HasPet(s)||c.quiz<Quiz.Length||LeaseBlocked(s)||BreedOf(key)==null)return false;
            c.pet=key;c.food=c.clean=c.joy=60;c.health=95;c.petUtc=utc;c.noPet=false;Life.Remember(s,"领养了"+BreedOf(key).name+"。");return true;
        }
        // Neglect makes a pet unhappy and ill but never kills it; health stops at the floor. Time away counts up to one day.
        public static void Decay(PotatoState s,long utc)
        {
            var c=s.city;if(!HasPet(s)||utc<=c.petUtc)return;long gone=utc-c.petUtc;if(gone<30&&gone>=0)return;
            float m=Math.Min(MaxAwayMinutes,gone/60f)*(s.demo?DemoSpeed:1);c.petUtc=utc;
            bool neglected=c.food<Low||c.clean<Low;c.food=Math.Max(0,c.food-FoodFall*m);c.clean=Math.Max(0,c.clean-CleanFall*m);c.joy=Math.Max(0,c.joy-JoyFall*m);
            neglected|=c.food<Low||c.clean<Low;if(neglected)c.health=Math.Max(HealthFloor,c.health-HealthFall*m);
        }
        static bool Pay2(PotatoState s,int cost){if(!HasPet(s)||s.coins<cost)return false;s.coins-=cost;return true;}
        public static bool Feed(PotatoState s){if(!Pay2(s,FeedCost))return false;s.city.food=Math.Min(100,s.city.food+30);return true;}
        public static bool Hug(PotatoState s){if(!HasPet(s))return false;s.city.joy=Math.Min(100,s.city.joy+18);return true;}
        public static int CleanCost(PotatoState s){return IsDog(s)?BathCost:LitterCost;}
        public static bool Clean(PotatoState s){if(!Pay2(s,CleanCost(s)))return false;s.city.clean=Math.Min(100,s.city.clean+35);return true;}
        public static bool Vet(PotatoState s){if(!Pay2(s,VetCost))return false;s.city.health=100;return true;}
        // The third answer (abandoning it) is rejected: the game cannot end that way.
        public static bool PetEnding(PotatoState s,int choice)
        {
            if(!HasPet(s)||choice<0||choice>=PetEndings.Length)return false;bool first=s.city.ending=="";s.city.ending=PetEndings[choice];if(first)Life.Remember(s,s.city.ending);return true;
        }
        public static bool PickedUp(PotatoState s){s.city.picked++;return true;}
        public static void Friend(PotatoState s){s.city.friends++;}
        static int First(PotatoState s){s.coins+=FirstReward;return FirstReward;}
        // Each first time pays a little once, so learning the city feeds the pet budget.
        public static int WeekTask(PotatoState s,string key)
        {
            var c=s.city;if(key=="sim"){if(c.weekSim!=0)return 0;c.weekSim=1;}else if(key=="card"){if(c.weekCard!=0)return 0;c.weekCard=1;}else if(key=="id"){if(c.weekId!=0)return 0;c.weekId=1;c.hkid=true;}else if(key=="home"){if(c.weekHome!=0)return 0;c.weekHome=1;}else return 0;return First(s);
        }
        public static int Ate(PotatoState s,int food){if(food<0||food>=Foods.Length||s.city.ate.Contains(food))return 0;s.city.ate.Add(food);return First(s);}
        public static int Stamp(PotatoState s,int place){if(place<0||place>=Places.Length||s.city.stamps.Contains(place))return 0;s.city.stamps.Add(place);return First(s);}
        public static string RightLine(string name){var line=Lines.FirstOrDefault(l=>l.key==name);return line==null?null:line.name;}
    }
}
