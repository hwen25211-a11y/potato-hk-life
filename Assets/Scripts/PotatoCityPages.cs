using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static PotatoLife.UI;
namespace PotatoLife
{
    // 新生活: the ten chapters. Each view draws a scene on the stage and its controls on the board.
    public partial class PotatoGame
    {
        static readonly string[][] Chapters={new[]{"anim","土豆动起来"},new[]{"home","领养与照顾"},new[]{"park","宠物公园"},new[]{"mall","宠物商场"},new[]{"week","抵港第一周"},new[]{"trans","交通"},new[]{"clinic","看医生"},new[]{"food","美食"},new[]{"out","周末出门"},new[]{"later","一年后"}};
        const string L="Art/Life/";
        string cityView="home",transTab="map",placeKind="景点",breedPick="westie";int placeIndex=-1;
        LifeStage st;Board bd;RectTransform cityNav;Potato2D potato;Pet2D pet;Sp seat;Image cafeTable;bool acting,eating;
        CityState C{get{return State.city;}}
        Color Red2=Hex("C8312B"),Green2=Hex("3F9A68");
        void DrawCity()
        {
            var card=Card(page,44,176,1040,694,Color.white,"CityStage");st=card.gameObject.AddComponent<LifeStage>();st.Build();
            var side=Card(page,1110,176,444,694,Color.white).transform;bd=new Board(side,20,16,410,662);cityNav=UI.Rect("CityNav",page,44,882,1510,56);GoCity(cityView);
        }
        void ClearCity(){st=null;bd=null;cityNav=null;potato=null;pet=null;seat=null;}
        void Nav()
        {
            for(int i=cityNav.childCount-1;i>=0;i--){var c=cityNav.GetChild(i).gameObject;c.SetActive(false);Destroy(c);}
            for(int i=0;i<Chapters.Length;i++){string v=Chapters[i][0];Button(cityNav,Chapters[i][1],i*151.6f,0,144,50,()=>{Sound.Fx("page");GoCity(v);},cityView==v?Blue:Muted,true,"City"+v);}
        }
        public void GoCity(string v)
        {
            cityView=v;st.Clear();bd.Clear();potato=null;pet=null;seat=null;cafeTable=null;acting=eating=false;Nav();
            if(v=="anim")ViewAnim();else if(v=="home")ViewHome();else if(v=="park")ViewPark();else if(v=="mall")ViewMall();else if(v=="week")ViewWeek();else if(v=="trans")ViewTrans();else if(v=="clinic")ViewClinic();else if(v=="food")ViewFood();else if(v=="out")ViewOut();else ViewLater();
        }
        void Pane(string title){bd.Clear();bd.Title(title);}
        void Keep(){Save.Write(State);RefreshStats();}
        void Pay(int reward){if(reward>0){Sound.Fx("coin");Float("第一次完成 · 金币 +"+reward,Mint);}Keep();}
        static Texture2D Art(string name){return Resources.Load<Texture2D>(L+name);}
        void Scene(string art,float x=50,float w=24){st.Back(Art(art));potato=Potato2D.Make(st.world,w,x,92);}
        // ① the potato's new moves
        void ViewAnim()
        {
            st.Back(home);potato=Potato2D.Make(st.world,48,50,94);st.Say("土豆子会眨眼、呼吸了。试试让它动起来。");
            Pane("土豆动起来");bd.P("土豆子换成了逐帧 2D：会眨眼，跳之前先蹲，落地会压扁一下，走路有真正的脚步。");
            bd.Btn("跳一下",()=>potato.Hop(),Coral,"AnimHop");bd.Btn("走两步",()=>potato.Go(potato.s.x>50?30:70),Blue,"AnimWalk");bd.Btn("挥手",()=>potato.Wave(),Green2,"AnimWave");bd.EndRow();
            bd.Muted("小屋里的土豆子也换成了这套动作。穿节日服装时保留原来的样子。");
        }
        // ② adoption and care
        float[] PetLevels(){return new[]{C.food,C.clean,C.joy,C.health};}
        public void RefreshPet(){if(cityView=="home"&&bd!=null&&bd.fills!=null)bd.SetBars(PetLevels());}
        void ViewHome()
        {
            st.Back(home);potato=Potato2D.Make(st.world,26,34,92);
            if(City.HasPet(State)){pet=Pet2D.Make(st.world,C.pet,18,66,92,()=>C.joy);CareBoard();}else QuizBoard();
        }
        void QuizBoard()
        {
            if(C.noPet){Pane("这次先不养");bd.P("不养也是负责任的决定。想清楚之前，可以去宠物公园和朋友的狗玩，也可以当义工。");bd.Btn("重新想一想",()=>{City.ResetQuiz(State);Keep();QuizBoard();},Ghost,"QuizAgain");st.Say("先把自己照顾好，也很棒。");return;}
            if(C.quiz<City.Quiz.Length)
            {
                var q=City.Quiz[C.quiz];st.Say("养宠物之前，我们先想清楚。");Pane("领养前想一想 "+(C.quiz+1)+" / "+City.Quiz.Length);bd.P(q[0]);
                for(int i=0;i<3;i++){int id=i;bd.Choice(q[i+1],()=>{City.Answer(State,id);Keep();QuizBoard();},"Quiz"+i);}bd.Muted(q[4]);return;
            }
            int risky=City.Risky(State);bool blocked=City.LeaseBlocked(State);st.Say(blocked?"租约不允许，这次不能领养。":risky>0?"还有几件事没想好。":"你想得很清楚。");
            Pane("你的领养清单");bd.Ticket(blocked?"<b>租约不允许</b>：先换房或问房东，游戏不让你偷偷养。":risky>0?"有 <b>"+risky+"</b> 项风险还没解决。可以继续，但后面会更辛苦。":"四项都想好了。");bd.Muted("领养后要负责它的吃喝拉撒、看病，一直到它老去。");
            if(!blocked)bd.Btn("看看可以领养谁",()=>BreedBoard(breedPick),Coral,"SeeBreeds");bd.Btn("这次先不养",SkipPet,Ghost,"NoPet");if(blocked)bd.Btn("重新想一想",()=>{City.ResetQuiz(State);Keep();QuizBoard();},Ghost,"QuizAgain");
        }
        void SkipPet(){C.noPet=true;Keep();GoCity("home");}
        Sp preview;
        void BreedBoard(string pick)
        {
            breedPick=pick;var b=City.BreedOf(pick);if(preview!=null)Destroy(preview.gameObject);preview=Sp.Make(st.world,Pet2D.Atlas(pick),4,2,20,68,92);preview.Frame(7);st.Say(b.name+"："+(b.dog?"狗":"猫")+" · "+b.size);
            Pane("选一只合适的");bd.Muted("10 个常见品种。不要只看可爱，先看它需要什么。");
            bd.Grid(5,City.Breeds.Length,i=>City.Breeds[i].name,i=>Pet2D.Atlas(City.Breeds[i].key),i=>new[]{4,2,7},i=>City.Breeds[i].key==pick,i=>BreedBoard(City.Breeds[i].key),"Breed",Hex("FFF1D6"));
            bd.Ticket("<b>"+b.name+"</b>（"+b.size+"）\n"+b.traits);bd.Muted("这是品种的一般特点，每只都不一样。优先考虑领养，先问租约和大厦规定。");
            bd.Btn("领养"+b.name,()=>Adopt(pick),Coral,"Adopt");bd.Btn("这次先不养",SkipPet,Ghost,"NoPet");
        }
        void Adopt(string key){if(!City.Adopt(State,key,Save.Now))return;Keep();GoCity("home");st.Say(City.MyPet(State).name+"来到小屋了！点旁边的按钮照顾它。");potato.Hop();}
        // Both walk to the same spot first, then play a shared drawn moment.
        void Together(Action<float> fn)
        {
            if(acting)return;acting=true;float px=Mathf.Clamp(pet.s.x,52,76);pet.follow=px;
            st.Every(.06f,()=>{if(Mathf.Abs(pet.s.x-px)>=1.2f)return true;pet.follow=null;pet.busy=true;pet.s.flip=1;pet.s.lift=0;pet.s.Place();potato.Go(px-20,()=>fn(px));return false;});
        }
        void ReleasePet(){pet.busy=false;pet.mode="sit";pet.wait=1.4f;acting=false;}
        void Feed()
        {
            if(acting)return;if(State.coins<City.FeedCost){st.Say("金币不够了，先送土豆子出门赚一点。");return;}st.Say("开饭啦，等我一下！");
            Together(px=>{potato.s.Show(false);var pour=Sp.Make(st.world,L+"Actions",4,2,33,px-14,92);pour.Frame(6);st.Say("把粮倒进碗里。");
                st.Later(()=>{Destroy(pour.gameObject);potato.s.Show(true);int n=0;
                    st.Every(.3f,()=>{pet.s.Frame(4+(n++%2));pet.s.sy=n%2==1?.97f:1.02f;pet.s.Place();if(n==2)st.Say(C.pet==""?"":City.MyPet(State).name+"埋头吃得好香，嚼得腮帮子鼓鼓的。");if(n<=9)return true;ReleasePet();City.Feed(State);Keep();RefreshPet();st.Say("吃饱了，碗要记得洗。");return false;});},1);});
        }
        void Hug()
        {
            if(acting)return;st.Say("过来抱抱！");
            Together(px=>{potato.s.Show(false);pet.s.Show(false);var h=Sp.Make(st.world,Pet2D.Atlas(C.pet),4,2,33,px-10,92);h.Frame(6);float t=0,end=2.1f;
                st.Tick(dt=>{t+=dt;h.sy=1+Mathf.Sin(t*8)*.02f;h.sx=1-Mathf.Sin(t*8)*.015f;h.Place();if(t<end)return true;Destroy(h.gameObject);potato.s.Show(true);pet.s.Show(true);ReleasePet();City.Hug(State);Keep();RefreshPet();return false;});
                st.Hearts(px-10,52);st.Say(City.MyPet(State).name+"把头埋进土豆子怀里。");});
        }
        void CareBoard()
        {
            bool dog=City.IsDog(State);Pane("照顾"+City.MyPet(State).name);bd.Bars(new[]{"饱腹","清洁","开心","健康"},PetLevels());bd.Muted("开销从你的金币里扣。忘了照顾不会死，但它会不开心、生病，要花钱看兽医。");
            bd.Btn("喂食 -"+City.FeedCost,Feed,Green2,"PetFeed");bd.Btn("抱抱",Hug,Green2,"PetHug");
            bd.Btn(dog?"洗澡 -"+City.BathCost:"铲猫砂 -"+City.LitterCost,()=>{if(acting)return;if(!City.Clean(State)){st.Say("金币不够了，先送土豆子出门赚一点。");return;}potato.Pose(5);st.Say(dog?"洗完香喷喷。":"猫砂要每天清。");Keep();RefreshPet();},Blue,"PetClean");
            bd.Btn("看兽医 -"+City.VetCost,()=>{if(acting)return;if(!City.Vet(State)){st.Say("这次的钱不够。这笔开销要提前留好。");return;}st.Say("打了疫苗，也做了检查。这笔钱要提前留好。");Keep();RefreshPet();},Ghost,"PetVet");
            if(dog)bd.Btn("去遛狗",()=>GoCity("park"),Coral,"PetWalk");bd.Btn("毕业那天",Ending,Coral,"PetEnding");st.Say(dog?"它在等你带它出门。":"它在窗边晒太阳。");
        }
        void Ending()
        {
            Pane("毕业那天，它怎么办？");bd.P("你要离开香港了。这是养宠物最难的一题。");
            bd.Choice("半年前开始办手续，带它一起走",()=>EndPet(0),"Ending0");bd.Choice("提前找到可靠的新家，定期看望",()=>EndPet(1),"Ending1");
            bd.Choice("留在公园，就当放生",()=>{st.Say("这不是一个选项。");Pane("不可以这样做");bd.Ticket("遗弃动物是违法的，它也没办法自己活下去。游戏不允许这样结束。");bd.P("土豆子把牵绳握得更紧了。");bd.Btn("回去重新选",Ending,Coral,"EndingBack");},"Ending2");
            bd.Btn("回到小屋",CareBoard,Ghost,"EndingHome");
        }
        void EndPet(int choice){City.PetEnding(State,choice);Keep();st.Say("毕业快乐，也谢谢你负责到底。");Pane("写进毕业手账");bd.Ticket(C.ending);bd.Btn("回到小屋",CareBoard,Ghost,"EndingHome");}
        // ③ the park
        bool poopWaiting;RectTransform poop;
        void ViewPark()
        {
            st.Back(Art("Park"));potato=Potato2D.Make(st.world,22,22,92);bool mine=City.IsDog(State);pet=Pet2D.Make(st.world,mine?C.pet:"corgi",15,9,93);pet.follow=9;poop=null;poopWaiting=false;
            st.Say(mine?"和"+City.MyPet(State).name+"来公园啦。":City.HasPet(State)?"猫一般不用遛。这里示范朋友的柯基。":"先和朋友的柯基试试看遛狗是什么感觉。");ParkBoard();
        }
        void ParkBoard()
        {
            Pane("宠物公园");bd.P("全程要牵好绳，带袋子，把便便捡走。香港有不少康文署的宠物公园和宠物共享公园，规则各不同，出门前要看清楚。");bd.Muted("已经捡了 <b>"+C.picked+"</b> 次便便。");
            bd.Btn("开始散步",Walk,Coral,"ParkWalk");bd.Btn("手账小漫画",Comic,Blue,"ParkComic");if(City.HasPet(State))bd.Btn("回小屋",()=>GoCity("home"),Ghost,"ParkHome");
        }
        void Walk()
        {
            if(potato==null||potato.Busy||poopWaiting)return;float to=potato.s.x<50?72:22;st.Say("慢慢走，看看风景。");pet.follow=to-13*(to>potato.s.x?1:-1);potato.Go(to,()=>{pet.follow=pet.s.x;MakePoop();});
        }
        void MakePoop()
        {
            poopWaiting=true;float px=Mathf.Clamp(pet.s.x-pet.s.flip*8,6,94);var b=Button(st.world,"",st.W*px/100-26,st.H*.88f-26,52,44,Pick,Hex("8A5A36"),true,"Poop");poop=(RectTransform)b.transform;poopX=px;st.Say("糟糕，它便便了！");
            Pane("要怎么做？");bd.P("路上有人看着你。");bd.Btn("用袋子捡起来",Pick,Coral,"PickUp");
            bd.Btn("假装没看见",()=>{st.Say("路人：这样不行喔！不清理可能会被罚款。");Pane("被人看到了");bd.Ticket("不清理狗便便是违规行为，也有人会拍下来举报。罚款金额写进游戏前再按官方核实。");bd.Btn("还是捡起来",Pick,Coral,"PickUp");},Ghost,"Ignore");
        }
        float poopX;
        void Pick()
        {
            if(!poopWaiting||poop==null||potato.Busy)return;potato.Go(poopX+4,()=>{potato.Pose(5,1.1f);st.Later(()=>{if(poop!=null)Destroy(poop.gameObject);poop=null;poopWaiting=false;City.PickedUp(State);Keep();st.Say("捡好了，丢进垃圾桶。下一位路人会感谢你。");ParkBoard();},.7f);});
        }
        void Comic()
        {
            var p=Modal("手账小漫画：屋顶上的袋子","人物和情节为虚构示意",1240,640);string[] lines={"有人遛狗，四下看了看。","他没有捡，还把袋子抛上了房顶。","对面的手机拍下了全过程。","最后收到了处罚通知。袋子不能抛，便便要带走。"};var art=Sp.Sheet(L+"Comic",4,1);
            for(int i=0;i<4;i++){var c=Card(p,32+i*297,146,285,330,Color.white).transform;var im=UI.Rect("Panel",c,12,12,261,196).gameObject.AddComponent<Image>();im.sprite=art[i];im.preserveAspect=true;im.raycastTarget=false;Text(c,lines[i],14,218,257,104,20);}
            Text(p,"正式版会做成小动画，处罚内容要按官方规定写。",32,492,1176,34,18,Muted);Button(p,"关闭",32,548,1176,56,CloseModal,Coral,true,"ComicClose");
        }
        // ④ the mall
        static readonly object[][] Spots={new object[]{22f,50f,"宠物咖啡","和宠物一起吃午饭。店铺规则各不同，进门前要先问。"},new object[]{78f,50f,"用品店","比较粮、砂和玩具的价格，别被可爱的东西冲昏头。"},new object[]{92f,62f,"饮水站","走累了先喝水。天气热时，要留意地面温度。"}};
        void ViewMall()
        {
            st.Back(Art("Mall"));potato=Potato2D.Make(st.world,22,40,93);pet=Pet2D.Make(st.world,City.HasPet(State)?C.pet:"corgi",15,58,94);pet.min=42;pet.max=74;st.Say("周末，来 Airside 式的宠物商场逛逛。");friendHere=false;
            foreach(var s in Spots){float x=(float)s[0],y=(float)s[1];string name=(string)s[2],note=(string)s[3];st.Hot(x,y,name,()=>potato.Go(Mathf.Clamp(x,20,80),()=>{st.Say(note);potato.Hop();}),"Spot"+name);}MallBoard();
        }
        bool friendHere;
        void MallBoard()
        {
            Pane("宠物商场");bd.P("点画面上的地点逛一逛。有些区域可以牵绳，有些要放进宠物车，这些细节按商场规定写进任务。");bd.Muted("在这里认识了 <b>"+C.friends+"</b> 位宠物朋友。");
            bd.Btn("交个朋友",Meet,Coral,"MallMeet");bd.Btn("看！滑板柯基",Skate,Blue,"MallSkate");
        }
        void Meet()
        {
            if(friendHere)return;friendHere=true;var mine=City.MyPet(State);var f=Pet2D.Make(st.world,mine!=null&&!mine.dog?"poodle":"ragdoll",15,110,94);f.follow=74;pet.follow=60;
            st.Later(()=>{f.mode="play";f.follow=null;f.wait=2;pet.mode="play";pet.follow=null;pet.wait=2;City.Friend(State);Keep();st.Say("它们玩起来了！主人约你下周一起去公园。");MallBoard();},2.6f);
            st.Later(()=>{if(f!=null)Destroy(f.gameObject);friendHere=false;},7);
        }
        void Skate()
        {
            var r=UI.Rect("Skater",st.over,-st.W*.22f,st.H*.98f-st.W*.17f,st.W*.17f,st.W*.17f);float s=st.W*.17f;var im=UI.Rect("Dog",r,0,0,s,s*.88f).gameObject.AddComponent<Image>();im.sprite=Sp.Sheet(Pet2D.Atlas("corgi"),4,2)[1];im.raycastTarget=false;
            Card(r,s*.08f,s*.86f,s*.84f,s*.08f,Coral);Card(r,s*.18f,s*.9f,s*.1f,s*.1f,Ink);Card(r,s*.72f,s*.9f,s*.1f,s*.1f,Ink);st.Say("一只柯基踩着滑板从你身边滑过去，全场都举起了手机。");
            float t=0;st.Tick(dt=>{t+=dt;r.anchoredPosition=new Vector2(Mathf.Lerp(-.22f,1.05f,t/3.4f)*st.W,r.anchoredPosition.y);if(t<3.4f)return true;Destroy(r.gameObject);return false;});
        }
        // ⑤ first week
        void WeekHead(){bd.Steps(new[]{"电话卡","交通卡","身份证","住处"},new[]{C.weekSim>0,C.weekCard>0,C.weekId>0,C.weekHome>0});}
        void ViewWeek()
        {
            Scene("Store",42);st.Say("抵港第一周，先把这四件事办好。");st.Hot(88,27,"电话卡货架",SimTask,"HotSim");st.Hot(78,47,"收银台",CardTask,"HotCard");st.Hot(14,30,"手机：预约身份证",IdTask,"HotId");st.Hot(50,16,"看房清单",HomeTask,"HotHome");WeekBoard();
        }
        void WeekBoard()
        {
            Pane("抵港第一周");WeekHead();bd.P("点画面里的地方，土豆子会走过去办。");bd.Btn("买电话卡",SimTask,Coral,"WeekSim");bd.Btn("买交通卡",CardTask,Coral,"WeekCard");bd.Btn("预约身份证",IdTask,Coral,"WeekId");bd.Btn("看房清单",HomeTask,Coral,"WeekHome");
        }
        void SimTask()
        {
            potato.Go(74,()=>{st.Say("电话卡在哪里买比较好？");Pane("买电话卡");WeekHead();bd.P("两边都能买，看你更在意什么。");
                bd.Choice("便利店：拿了就走，套餐一般比营业厅优惠一些",()=>SimDone("便利店买的卡现买现用，一般更便宜。看清楚流量、有效期和能否延期。"),"Sim0");bd.Choice("营业厅：有人帮你选，可以问学生优惠",()=>SimDone("营业厅可以当面比较套餐。带上学生证，问问有没有学生优惠。"),"Sim1");});
        }
        void SimDone(string t){Pay(City.WeekTask(State,"sim"));potato.Hop();st.Say("手机有信号了！");Pane("电话卡办好了");WeekHead();bd.Ticket(t);bd.Btn("继续",WeekBoard,Ghost,"WeekBack");}
        void CardTask()
        {
            potato.Go(66,()=>{st.Say("出门之前，先准备好怎么坐车。");Pane("交通卡");WeekHead();bd.P("怎么坐车最省心？");
                bd.Choice("在便利店买一张交通卡",()=>CardDone("便利店和地铁站都能买交通卡，上车拍一下就行，还能在便利店、超市付款。"),"Card0");bd.Choice("去地铁站客务中心买",()=>CardDone("地铁站也能买，新手可以顺便问问怎么增值。"),"Card1");
                bd.Choice("只用手机微信 / 支付宝二维码",()=>CardDone("手机二维码可以坐地铁和不少巴士，但<b>部分交通工具不支持</b>。建议身上再备一张交通卡，到「交通」里试试看。"),"Card2");});
        }
        void CardDone(string t){Pay(City.WeekTask(State,"card"));potato.Hop();Pane("可以出门了");WeekHead();bd.Ticket(t);bd.Btn("去试试过闸",()=>{transTab="pay";GoCity("trans");},Coral,"GoPay");bd.Btn("回到第一周",WeekBoard,Ghost,"WeekBack");}
        // A phone-shaped overlay with its own small button column.
        Board Phone(string head,string line,out RectTransform frame)
        {
            float w=st.W*.34f,h=st.H*.8f;frame=Card(st.over,st.W*.04f,st.H*.14f,w,h,Ink,"Phone").rectTransform;Card(frame,7,7,w-14,h-14,Color.white,"Screen");var b=new Board(frame,18,18,w-36,h-36,8);bd2=b;b.Title2(head);b.Muted(line);return b;
        }
        Board bd2;
        void IdTask()
        {
            potato.Go(30,()=>{st.Say("逗留超过 180 天，要在抵港 30 天内申请身份证。");RectTransform m;var b=Phone("入境事务处官网（模拟）","申请香港身份证 · 网上预约",out m);
                Pane("预约身份证");WeekHead();bd.P("身份证要在官网预约，再本人去办。先选一个时间。");bd.Muted("这是模拟页面，流程以入境处官网为准。");
                string[] slots={"下周二 10:30 · 九龙办事处","下周四 14:15 · 香港办事处"};for(int i=0;i<2;i++){string s=slots[i];b.Choice(s,()=>IdDocs(b,m,s),"Slot"+i);}});
        }
        static readonly string[] Docs={"护照或旅行证件","入境时的逗留标签 / 学生签证","可爱的土豆照片","预约确认"};
        void IdDocs(Board b,RectTransform m,string slot)
        {
            b.Clear();b.Muted("已选："+slot);b.Muted("到场要带什么？（点选三样）");int n=0;
            for(int i=0;i<4;i++)
            {
                int id=i;Button one=null;one=b.Choice(Docs[i],()=>{one.interactable=false;if(id==2){st.Say("这个不用带。");return;}one.GetComponentInChildren<TextMeshProUGUI>().text="✓ "+Docs[id];if(++n<3)return;
                    Destroy(m.gameObject);Pay(City.WeekTask(State,"id"));C.hkid=true;Keep();st.Say("预约好了！办好身份证，以后看病的收费也不同。");potato.Hop();Pane("身份证预约好了");WeekHead();bd.Ticket("记得按时本人去办。拿到有效身份证后，公立医院按「符合资格人士」收费。");bd.Btn("去看收费差别",()=>GoCity("clinic"),Coral,"GoClinic");bd.Btn("回到第一周",WeekBoard,Ghost,"WeekBack");},"Doc"+i);
            }
        }
        void HomeTask()
        {
            potato.Go(50,()=>{st.Say("看房的时候，这三件事要问清楚。");int left=3;Pane("看房清单");WeekHead();bd.P("点开每一项确认。");string[] items={"租约：租期多长？能不能提前退租？","押金和水电：谁付？怎么算？","能不能养宠物？"};
                for(int i=0;i<3;i++){Button one=null;one=bd.Choice(items[i],()=>{one.interactable=false;if(--left>0)return;Pay(City.WeekTask(State,"home"));potato.Hop();st.Say("住处定下来了。");Pane("住处安顿好了");WeekHead();bd.Ticket("签约前看清楚再付款。养宠物的事，这时候就要问。");bd.Btn("继续",WeekBoard,Ghost,"WeekBack");},"Lease"+i);}});
        }
        // ⑥ getting around
        void ViewTrans(){st.Clear();potato=null;if(transTab=="pay")TransPay();else if(transTab=="bus")Minibus();else if(transTab=="taxi")TransTaxi();else TransMap();}
        void TransTabs()
        {
            string[][] t={new[]{"map","地铁线路"},new[]{"pay","怎么付款"},new[]{"bus","坐小巴"},new[]{"taxi","的士颜色"}};bd.EndRow();
            foreach(var x in t){string k=x[0];bd.Btn(x[1],()=>{transTab=k;ViewTrans();},transTab==k?Blue:Ghost,"Trans"+k);}bd.EndRow();
        }
        RectTransform mapBox;TextMeshProUGUI routeText;int routeStep=-1;
        void DrawMap(string on)
        {
            for(int i=mapBox.childCount-1;i>=0;i--){var c=mapBox.GetChild(i).gameObject;c.SetActive(false);Destroy(c);}
            float k=Mathf.Min(st.W*.96f/860,st.H*.96f/580),ox=st.W*.02f,oy=st.H*.02f;Func<int,float> X=v=>ox+v*k,Y=v=>oy+v*k;
            foreach(var l in City.Lines)
            {
                float th=(on==l.key?13:8)*k;var col=Hex(l.color);if(on!=null&&on!=l.key)col.a=.2f;
                for(int i=0;i+2<l.path.Length;i+=2)
                {
                    float x0=X(l.path[i]),y0=Y(l.path[i+1]),x1=X(l.path[i+2]),y1=Y(l.path[i+3]),len=Mathf.Sqrt((x1-x0)*(x1-x0)+(y1-y0)*(y1-y0));
                    var s=UI.Rect("Seg",mapBox,x0,y0,len,th);s.pivot=new Vector2(0,.5f);s.anchoredPosition=new Vector2(x0,-y0);s.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(y1-y0,x1-x0)*Mathf.Rad2Deg);s.gameObject.AddComponent<Image>().color=col;s.GetComponent<Image>().raycastTarget=false;
                    Dot(x0,y0,th,col);if(i+4>=l.path.Length)Dot(x1,y1,th,col);
                }
            }
            foreach(var s in City.Stations){float x=X((int)s[0]),y=Y((int)s[1]);Dot(x,y,16*k,Ink);Dot(x,y,10*k,Color.white);Text(mapBox,(string)s[2],x+8*k,y-32*k,120,28,18,Ink,true);}
        }
        void Dot(float x,float y,float d,Color c){Card(mapBox,x-d/2,y-d/2,d,d,c,"Dot");}
        void LineButtons(Action<MtrLine> pick){foreach(var l in City.Lines){var line=l;bd.Btn(l.name,()=>pick(line),Hex(l.color),"Line"+l.key,true,38);}bd.EndRow(38);}
        void TransMap()
        {
            st.Fill(Cream);mapBox=UI.Rect("Map",st.world,0,0,st.W,st.H);DrawMap(null);routeStep=-1;
            Pane("地铁线路图");bd.P("每条线有自己的颜色，换乘时跟着颜色走。点颜色看线路。");bd.Muted("示意图，只画了主要车站，位置不按比例。");
            LineButtons(l=>{if(routeStep<0){DrawMap(l.key);routeText.text=l.name+"亮起来了。";return;}RouteStep(l);});routeText=bd.Live(62);bd.Btn("小任务：九龙塘去中环",()=>{routeStep=0;routeText.text="第 1 步：从九龙塘上车，先坐哪条线？";},Coral,"RouteTask");TransTabs();
        }
        void RouteStep(MtrLine l)
        {
            string[] seq={"kt","tw"};if(routeStep>=2)return;if(l.key!=seq[routeStep]){routeText.text="这条线到不了，再看看地图。";return;}routeStep++;DrawMap(l.key);
            routeText.text=routeStep==1?"对！绿色观塘线到太子或旺角下车。第 2 步：换哪条线去中环？":"到了！红色荃湾线一路坐到中环。";
        }
        TextMeshProUGUI payText;
        void TransPay()
        {
            Scene("Mtr",50);st.Say("试试看：这趟车能不能用手机扫码？");Pane("怎么付款");bd.P("选一种交通工具，看看手机二维码行不行。");
            for(int i=0;i<City.Pay.Length;i++){var p=City.Pay[i];bd.Choice(p[0],()=>{bool ok=p[2]=="都可以";potato.Go(ok?56:40,()=>{if(ok)potato.Hop();else potato.Pose(0,1);st.Say(ok?"嘀，过了！":"这台机器扫不了，幸好带了交通卡。");});payText.text="<b>"+p[0]+"："+p[2]+"</b>\n"+p[1];},"Pay"+i);}
            bd.Muted("内地版微信、支付宝和香港版的支持范围可能不同，出发前先试一次。");payText=bd.Live(118);TransTabs();
        }
        void TransTaxi()
        {
            Scene("Street",50,20);var back=st.world.GetChild(0).GetComponent<RawImage>();back.uvRect=new Rect(0,0,.5f,1);TaxiAsk(0);
        }
        void TaxiAsk(int i)
        {
            if(i==City.Taxi.Length){st.Say("三种颜色都认识了。");Pane("的士颜色");bd.Ticket("红色跑市区，绿色跑新界，蓝色跑大屿山。起步价写进正式版前再按运输署核对。");TransTabs();return;}
            st.Say(City.Taxi[i][0]+"，拦哪种颜色的的士？");Pane("的士颜色");bd.P(City.Taxi[i][0]);int right=int.Parse(City.Taxi[i][1]);string[] names={"红的士","绿的士","蓝的士"};Color[] cols={Red2,Green2,Blue};
            for(int c=0;c<3;c++){int id=c;bd.Btn(names[c],()=>{if(id==right){potato.Hop();TaxiAsk(i+1);}else st.Say("这个颜色不跑这里，再想想。");},cols[c],"Taxi"+c);}TransTabs();
        }
        void Minibus()
        {
            var road=st.Back(Art("Street"),new Rect(0,0,.5f,1));road.texture.wrapMode=TextureWrapMode.Repeat;float W=st.W,H=st.H;
            var pole=UI.Rect("Pole",st.world,0,H*.34f,W*.03f,H*.4f);Card(pole,W*.0132f,H*.04f,W*.0036f,H*.36f,Hex("555555"));var sign=UI.Rect("Sign",pole,-W*.027f,0,W*.084f,W*.084f*92/108).gameObject.AddComponent<Image>();sign.sprite=Sp.Sheet(L+"MinibusStop",1,1)[0];sign.raycastTarget=false;
            var busArt=Sp.Sheet(L+"Minibus",1,1)[0];float bw=W*.44f,bh=bw*busArt.rect.height/busArt.rect.width;var bus=UI.Rect("Bus",st.world,W*.24f,H*.93f-bh,bw,bh);var im=bus.gameObject.AddComponent<Image>();im.sprite=busArt;im.raycastTarget=false;float by=bus.anchoredPosition.y;
            st.Say("快到站时喊「前面有落」，司机才会停。");float x=130,scroll=0,slow=1;bool done=false;
            Action<bool,string> finish=(ok,t)=>{st.Say(t);Pane(ok?"下车成功":"再试一次");bd.Ticket(ok?"记下了：快到时要提前喊。":"要提前一点，司机才来得及靠边。");bd.Btn("再坐一次",()=>{transTab="bus";ViewTrans();},Coral,"BusAgain");TransTabs();};
            Pane("坐小巴");bd.P("绿色小巴有固定路线，车头写着路线和车费。想下车要提前大声喊，司机才知道。");bd.Muted("看到站牌靠近时，按下面的按钮。");
            bd.Btn("前面有落！",()=>{if(done)return;done=true;if(x>82){slow=0;finish(false,"还太远了，司机：「边度落呀？」");return;}if(x<46){slow=0;finish(false,"喊晚了，坐过站了，要走回去。");return;}st.Say("司机：好！");slow=.5f;},Coral,"BusShout");TransTabs();
            float clockBus=0;st.Tick(dt=>{clockBus+=dt;if(slow==0)return false;float v=slow==1?26:Mathf.Max(0,(x-40)*2.4f);if(slow!=1&&x<=41){slow=0;finish(true,"稳稳停在站牌旁边。");return false;}
                x-=dt*v;scroll+=dt*v;road.uvRect=new Rect(scroll*.0019f,0,.5f,1);pole.anchoredPosition=new Vector2(W*x/100-W*.015f,pole.anchoredPosition.y);bus.anchoredPosition=new Vector2(bus.anchoredPosition.x,by+(slow==1?Mathf.Sin(clockBus*11):0));if(x<-8&&!done){done=true;slow=0;finish(false,"一直没喊，小巴开过站了。");return false;}return true;});
        }
        // ⑦ the clinic
        void ViewClinic()
        {
            Scene("Clinic",50);st.Say("土豆子发烧了，要去哪里看？");Pane("生病了怎么办");bd.P("先判断有多急，再决定去哪里。");
            bd.Choice("感冒发烧，不算急：公立家庭医学门诊",HaGo,"ClinicHa");
            bd.Choice("胸痛、呼吸困难这类紧急情况：急症室",()=>{st.Say("紧急情况不用预约。");Pane("急症室");bd.Ticket("<b>紧急情况打 999 或直接去急症室，不需要预约。</b>护士会先分流，病情较轻要等比较久。");bd.Btn("看收费",Fees,Coral,"SeeFees");bd.Btn("返回",()=>GoCity("clinic"),Ghost,"ClinicBack");},"ClinicAe");
            bd.Choice("想快一点：私家诊所",()=>{st.Say("私家诊所一般不用等太久，但费用自付，先问清楚价钱。");Pane("私家诊所");bd.Ticket("可以直接上门或电话预约。诊金和药费每家不同，先问清楚。学校的保险或保健处也可以先问问。");bd.Btn("返回",()=>GoCity("clinic"),Ghost,"ClinicBack");},"ClinicGp");
            bd.Btn("看 2026 收费标准",Fees,Blue,"SeeFees");
        }
        void HaGo()
        {
            potato.Go(30,()=>{st.Say("公立门诊要提前预约。打开 HA Go。");RectTransform m;var b=Phone("HA Go（模拟）","预约普通科门诊",out m);Pane("用 HA Go 预约");bd.P("打开手机预约。第一次使用要先注册并启动账户。名额常常很快满。");bd.Muted("这是模拟页面。");
                b.Choice("家附近的诊所：今日已满",()=>st.Say("名额满了，看看附近的诊所。"),"HaFull");
                b.Choice("附近诊所：今日 15:30 有位",()=>{Destroy(m.gameObject);potato.Go(52,()=>{potato.Hop();st.Say("按时到登记处，出示证件。");Pane("预约成功");bool has=C.hkid;bd.Ticket("按预约时间到诊所登记。你现在"+(has?"有":"还没有")+"有效身份证，这次按<b>"+(has?"符合资格人士":"非符合资格人士")+"</b>收费：诊症 "+(has?"$150":"$500")+"，每种药 "+(has?"$5":"$40")+"。");bd.Btn("看完整收费",Fees,Coral,"SeeFees");bd.Btn("返回",()=>GoCity("clinic"),Ghost,"ClinicBack");});},"HaOpen");});
        }
        void Fees()
        {
            bool has=C.hkid;Pane("2026 公立医院收费");bd.P("从 2026 年 1 月 1 日起。有效香港身份证，差别很大。");bd.Table(new[]{"项目","符合资格","非符合资格"},City.Fees,has?1:2);
            bd.Muted("单位港币。海报写明：持有效香港身份证者、香港居民的 11 岁以下子女等，才按符合资格收费。非本地学生的情况以医管局确认为准。");bd.Muted(PosterSource);bd.Btn("看收费海报原图",PosterView,Coral,"Poster");bd.Btn("返回",()=>GoCity("clinic"),Ghost,"ClinicBack");st.Say(has?"你有身份证，看左边一栏。":"还没办身份证，看右边一栏。");
        }
        const string PosterSource="资料来源：医院管理局 2026 年公立医院收费海报，开发者摄于仁济医院大堂。";
        // Whole poster first; each jump opens one poster at close to 1:1 so the small print stays readable.
        void PosterView()
        {
            var p=Modal("医管局 2026 收费海报","选一张海报看清楚，拖动画面可以移动。"+PosterSource,1540,960);var tex=Resources.Load<Texture2D>("Hospital/FeePoster");float vw=1476,vh=690,fit=Mathf.Min(vw/tex.width,vh/tex.height);
            var view=UI.Rect("PosterView",p,32,134,vw,vh);var bg=view.gameObject.AddComponent<Image>();bg.color=Color.white;view.gameObject.AddComponent<RectMask2D>();var sr=view.gameObject.AddComponent<ScrollRect>();sr.viewport=view;sr.movementType=ScrollRect.MovementType.Clamped;sr.scrollSensitivity=45;
            // The holder is what scrolls: it stays at least as big as the view, so the whole poster sits centred.
            var holder=UI.Rect("Holder",view,0,0,vw,vh);var img=UI.Rect("Poster",holder,0,0,tex.width*fit,tex.height*fit);img.gameObject.AddComponent<RawImage>().texture=tex;img.GetComponent<RawImage>().raycastTarget=false;sr.content=holder;
            Action show=()=>{holder.sizeDelta=new Vector2(vw,vh);holder.anchoredPosition=Vector2.zero;img.sizeDelta=new Vector2(tex.width*fit,tex.height*fit);img.anchoredPosition=new Vector2((vw-img.sizeDelta.x)/2,-(vh-img.sizeDelta.y)/2);};show();
            Action<float,float> zoom=(rx,ry)=>{float z=Mathf.Max(fit,1);holder.sizeDelta=img.sizeDelta=new Vector2(tex.width*z,tex.height*z);img.anchoredPosition=Vector2.zero;holder.anchoredPosition=new Vector2(-rx*holder.sizeDelta.x,ry*holder.sizeDelta.y);};
            Button(p,"符合资格人士",32,846,270,58,()=>zoom(.03f,.335f),Green2,true,"PosterA");Button(p,"私家症",318,846,200,58,()=>zoom(.335f,.33f),Green2,true,"PosterB");Button(p,"非符合资格人士",534,846,290,58,()=>zoom(.635f,.32f),Green2,true,"PosterC");
            Button(p,"看全图",840,846,200,58,show,Muted,true,"PosterFit");Button(p,"关闭",1056,846,452,58,CloseModal,Coral,true,"PosterClose");
        }
        // ⑧ food: walks to the booth once, then stays seated behind the table between dishes.
        void ViewFood()
        {
            st.Back(Art("Cafe"));st.Prop(L+"Bench",43,17,38,640f/430);cafeTable=st.Prop(L+"Table",47,1,40,640f/368);potato=Potato2D.Make(st.world,24,20,94);st.Say("今天吃什么？点一样，土豆子就去卡座坐下点单。");FoodList();
        }
        void SitDown(Action done)
        {
            if(seat!=null){done();return;}
            potato.Go(40,()=>{potato.s.Show(false);seat=Sp.Make(st.world,L+"EatHand",3,2,27,44,75);seat.Frame(4);seat.transform.SetSiblingIndex(cafeTable.transform.GetSiblingIndex());float t=0;
                st.Tick(dt=>{t=Mathf.Min(1,t+dt*4);seat.x=44+(62-44)*t;seat.lift=Mathf.Sin(t*Mathf.PI)*st.H*.07f;seat.Place();if(t<1)return true;done();return false;});});
        }
        void Eat(int i)
        {
            if(eating)return;eating=true;st.Say("先走到卡座坐下来。");
            SitDown(()=>{var e=seat;st.Say("土豆子举手：「唔该，"+City.Foods[i][2]+"」");
                st.Later(()=>{st.Say("伙计：「好，即刻嚟！」");st.Later(()=>{
                    var d=Sp.Make(st.world,L+"Food",4,2,11,112,71.5f);d.Frame(i);float slide=0;st.Tick(dt=>{slide=Mathf.Min(1,slide+dt*2);d.x=112+(73-112)*(1-(1-slide)*(1-slide));d.Place();return slide<1;});st.Say(City.Foods[i][1]);
                    int[] h=City.Held(i);bool inHand=h!=null&&i!=7;
                    st.Later(()=>{if(inHand)Destroy(d.gameObject);if(h==null)e.Set(L+"EatChopsticks",2,1);int n=0;
                        st.Every(.42f,()=>{e.Frame(h!=null?h[n%2]:n%2);if(!inHand){d.sx=d.sy=Mathf.Max(.2f,1-n*.14f);d.Place();}if(++n<=6)return true;
                            if(!inHand)Destroy(d.gameObject);e.Set(L+"EatHand",3,2);e.Frame(4);int paid=City.Ate(State,i);eating=false;FoodList();Pay(paid);st.Say(City.Foods[i][0]+"吃完了，满足！想吃别的再点。");return false;});},.9f);
                },.9f);},1);});
        }
        void FoodList()
        {
            Pane("香港美食");bd.P("点一样，土豆子会走到卡座坐下、点单，等餐上桌再吃。");bd.Muted("已经吃过 <b>"+C.ate.Count+"</b> / "+City.Foods.Length+" 样。");
            bd.Grid(4,City.Foods.Length,i=>City.Foods[i][0],i=>L+"Food",i=>new[]{4,2,i},i=>C.ate.Contains(i),Eat,"Food",Hex("CFEBDA"));
        }
        // ⑨ weekends
        void PlaceBack(CityPlace p)
        {
            if(p.photo!=null){var t=Resources.Load<Texture2D>("Photos/"+p.photo);st.Contain(t,new Rect(0,0,1,1),t.width/(float)t.height);}
            else if(p.art==-2)st.Back(Art("Street"),new Rect(0,0,.5f,1));
            else st.Contain(Art("Places"),new Rect((p.art%4)/4f,1-(p.art/4+1)/2f,.25f,.5f),.75f);
        }
        void ViewOut(){int first=Array.FindIndex(City.Places,p=>p.kind==placeKind);PlaceView(placeIndex>=0&&City.Places[placeIndex].kind==placeKind?placeIndex:first);}
        void PlaceView(int i)
        {
            placeIndex=i;var p=City.Places[i];st.Clear();PlaceBack(p);potato=Potato2D.Make(st.world,20,18,94);st.Say(p.name+" · "+p.where);
            bd.Clear();bd.Title("周末去哪里");foreach(string k in City.PlaceKinds){string kind=k;bd.Btn(k,()=>{placeKind=kind;placeIndex=-1;ViewOut();},placeKind==k?Blue:Ghost,"Kind"+k,true,38);}bd.EndRow(38);
            for(int n=0;n<City.Places.Length;n++)if(City.Places[n].kind==placeKind){int id=n;bd.Btn((C.stamps.Contains(n)?"✓ ":"")+City.Places[n].name,()=>PlaceView(id),n==i?Blue:Ghost,"Place"+n,true,38);}bd.EndRow(38);
            bd.P(p.note);bd.Muted("已打卡 <b>"+C.stamps.Count+"</b> / "+City.Places.Length+" 个地方。图片是实拍照或示意插画。");
            bd.Btn("打卡",()=>{Pay(City.Stamp(State,i));potato.Hop();st.Say("打卡成功，盖一个章。");st.Later(()=>PlaceView(i),.9f);},Coral,"Stamp");if(p.line!=null)bd.Btn("坐哪条线去？",()=>LineQuiz(i),Blue,"LineQuiz");
            if(p.kind=="看剧")bd.Btn("用 URBTIX 买学生票",Urbtix,Green2,"Urbtix");if(p.kind=="购物")bd.Btn("逛一逛",()=>Shopping(i),Green2,"Browse");
        }
        void LineQuiz(int i)
        {
            var p=City.Places[i];st.Say("去"+p.name+"，主要坐哪条线？");Pane("坐哪条线去？");bd.P("选一条颜色。");
            foreach(var l in City.Lines.Where(x=>x.key=="tw"||x.key=="is"||x.key=="tc"||x.key=="kt")){var line=l;bd.Btn(l.name,()=>{if(line.key==p.line){potato.Hop();st.Say("对，"+line.name+"就能到。");st.Later(()=>PlaceView(i),1);}else st.Say("这条线不太顺路，再看看。");},Hex(l.color),"Quiz"+l.key);}
        }
        void Urbtix()
        {
            st.Say("先选一场戏。");Pane("URBTIX 买票（模拟）");bd.P("城市售票网可以买话剧、音乐会的票。");Action<string> done=t=>{potato.Hop();st.Say("买到票了，周末去看戏！");Pane("买好了");bd.Ticket(t);bd.Btn("返回",()=>PlaceView(placeIndex),Ghost,"UrbBack");};
            bd.Choice("话剧（示例场次）· 标准票",()=>done("标准票谁都能买。"),"Urb0");bd.Choice("话剧（示例场次）· 学生票",()=>done("学生票一般限量，入场时要出示学生证。优惠以该场演出的 URBTIX 页面为准。"),"Urb1");
        }
        void Shopping(int i)
        {
            string n=City.Places[i].name;string[] t=n=="圆方"?new[]{"只逛不买，吃顿下午茶","大牌橱窗看看就好，别刷爆信用卡。"}:n=="铜锣湾"?new[]{"在小店买一件平价衣服","人多，地铁站出口也多，先看好出口。"}:new[]{"挑一件打折的外套","奥特莱斯有折扣，也要看清楚能否退换。"};
            st.Say("逛街也要看预算。");Pane("逛"+n);bd.P("这个月生活费还剩 $1,200（示例）。");bd.Choice(t[0],()=>{potato.Hop();st.Say(t[1]);st.Later(()=>PlaceView(i),1.6f);},"Shop0");bd.Choice("先不买，回去比比价",()=>{st.Say("也很好，想清楚再买。");st.Later(()=>PlaceView(i),1.2f);},"Shop1");
        }
        // ⑩ a year on
        void ViewLater()
        {
            st.Back(home);potato=Potato2D.Make(st.world,26,50,92);st.Say("一年过去了，接下来想做什么？");Pane("一年后");bd.P("点一项，看看可以怎么做。");
            for(int i=0;i<City.LaterPlans.Length;i++){var l=City.LaterPlans[i];bd.Choice(l[0],()=>{potato.Hop();st.Say(l[0]);Pane(l[0]);bd.Ticket(l[1]);bd.Muted(l[2]);bd.Btn("返回",()=>GoCity("later"),Ghost,"LaterBack");},"Later"+i);}
        }
        Color Ghost{get{return Muted;}}
    }
}
