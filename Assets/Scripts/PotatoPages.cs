using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static PotatoLife.UI;
namespace PotatoLife
{
    public partial class PotatoGame
    {
        void DirectionDialog()
        {
            var p=Modal("今天，想把心放在哪里？","方向是偏好，不是命令；约六成外出会跟随你的选择。",1110,730);
            for(int i=0;i<4;i++){int id=i;float x=32+(i%2)*532,y=157+(i/2)*224;var c=Card(p,x,y,511,204,Color.white).transform;Actor(c,i==0?1:i==1?2:i==2?3:6,0,15,18,124,151);Text(c,World.Directions[i],159,20,328,39,28,Colors[i],true);Text(c,World.DirectionNotes[i],159,76,325,68,20,Muted);Button(c,State.direction==(Direction)i?"就按这个心情出发":"选这个方向",159,153,325,36,()=>{Life.DirectionPick(State,(Direction)id);Save.Write(State);Render();},Colors[i],true,"Direction"+i);}
            Text(p,"去哪里都能带回收获；偶尔偏离计划，也是照片里的惊喜。",32,643,1045,46,22,Muted);
        }
        void Wardrobe()
        {
            var p=Modal("出门前，带点喜欢的东西。","行囊可反复使用；换装会保存在之后拍下的照片里。",1130,850);
            Text(p,"带一件行囊",32,146,1065,36,25,Ink,true);int[] ids={-1,5,6,7};
            for(int i=0;i<4;i++){int id=ids[i];bool have=id<0||State.owned.Contains(id);string name=id<0?"轻装出门":World.Items[id].name;Button(p,name+(State.selectedBag==id?" · 已带好":!have?" · 小店有售":""),32+i*270,200,250,53,()=>{Life.Pack(State,id);Save.Write(State);Wardrobe();},have?(State.selectedBag==id?Blue:Mint):Muted,have&&!State.travelling,"Bag"+id);}
            Text(p,State.travelling?"它已出发，当前行囊不再变动；回来后再准备。":"便当省体力，相机多照片，雨伞让雨天也有好心情。",32,276,1067,47,20,Muted);
            Text(p,"今天穿什么",32,352,1065,36,25,Ink,true);
            for(int i=0;i<5;i++){int id=i;bool have=State.costumes.Contains(i);var c=Card(p,32+i*216,410,199,265,Color.white).transform;Actor(c,0,i,18,11,162,166);Text(c,World.CostumeNames[i],10,184,179,31,20,Ink,true,TextAlignmentOptions.Center);Button(c,State.outfit==i?"穿好啦":have?"换上这套":"节日里解锁",10,225,179,31,()=>{Life.Dress(State,id);Save.Write(State);Render();Wardrobe();},have?Coral:Muted,have,"Outfit"+i);}
            Button(p,"准备好啦",32,752,1065,57,()=>{Save.Write(State);Render();},Blue,true,"CloseWardrobe");
        }
        void DrawAlbum()
        {
            int sealedCount=State.album.Count(p=>p.unopened);
            Text(page,"把普通的日子，贴成一本书。",45,183,1485,56,37,Ink,true);Text(page,(sealedCount>0?"有 "+sealedCount+" 张新明信片等你亲手拆开。":"12 个香港角落 + 4 张节日纪念。")+"照片保留拍摄时的衣服，回忆不会被换掉。",47,250,1489,36,21,sealedCount>0?Coral:Muted);
            var list=Scroll(page,44,314,1512,605,1460);
            var order=State.album.OrderByDescending(p=>p.trip).Select(p=>p.id).Concat(Enumerable.Range(0,16).Where(id=>!State.album.Any(p=>p.id==id))).ToArray();
            for(int position=0;position<16;position++)
            {
                int id=order[position],index=id;var photo=State.album.FirstOrDefault(p=>p.id==id);int row=position/4,col=position%4;var c=Card(list,col*374,row*356,354,348,Color.white).rectTransform;
                if(photo==null){Card(c,14,14,326,229,Pale);Text(c,"还有一个小故事\n正在路上",30,86,296,84,25,Muted,false,TextAlignmentOptions.Center);Text(c,id<12?World.Places[id].name:World.FestivalNames[id-12],16,264,322,42,24,Muted,true,TextAlignmentOptions.Center);}
                else if(photo.unopened){PostcardBack(c,photo,14,14,326,229);Text(c,id<12?World.Places[id].name:World.FestivalNames[id-12],16,260,322,39,24,Ink,true);Button(c,"拆开这张明信片",16,304,322,35,()=>OpenPostcard(index),Coral,true,"Open"+id);}
                else{PhotoVisual(c,photo,14,14,326,229);Text(c,id<12?World.Places[id].name:World.FestivalNames[id-12],16,260,322,39,24,Ink,true);Button(c,"第 "+photo.trip+" 次外出 · 打开这一页",16,304,322,35,()=>PhotoDialog(index),Colors[photo.season],true,"Photo"+id);}
            }
        }
        void PostcardBack(Transform p,Photo photo,float x,float y,float w,float h)
        {
            Card(p,x,y,w,h,Hex("FFF1D6"));Card(p,x+w-74,y+14,58,68,Colors[photo.season]);Card(p,x+w-68,y+20,46,56,Hex("FFF8EC"));Text(p,"港",x+w-68,y+30,46,40,24,Colors[photo.season],true,TextAlignmentOptions.Center);
            for(int i=0;i<3;i++)Card(p,x+20,y+h-70+i*20,w*.55f,3,Hex("E2CFA8"));
            Text(p,"新明信片\n点下面拆开",x+20,y+40,w-110,90,24,Ink,true);
        }
        void OpenPostcard(int id)
        {
            if(!Life.OpenPostcard(State,id))return;Save.Write(State);Sound.Fx("stamp");Render();PhotoDialog(id);Float("拆开了一张新明信片！",Coral);
        }
        void PhotoVisual(Transform p,Photo photo,float x,float y,float w,float h)
        {
            int scene=photo.id<12?World.Places[photo.id].art:3;
            if(photo.id<12){var atlas=Resources.Load<Texture2D>("Art/PostcardAtlas");var picture=Picture(p,atlas,x,y,w,h);float u=.25f,v=1f/3,ratio=(atlas.width/4f)/(atlas.height/3f);if(w/h>ratio)v*=ratio/(w/h);else u*=(w/h)/ratio;picture.uvRect=new UnityEngine.Rect(photo.id%4*.25f+(.25f-u)/2,1-(photo.id/4+1)/3f+(1f/3-v)/2,u,v);}else Picture(p,places,x,y,w,h,scene);
            int pose=photo.id<12?(int)World.Places[photo.id].direction:3;pose=pose==0?1:pose==1?2:pose==2?3:6;
            Actor(p,pose,photo.outfit,x+w*.35f,y+h*.35f,w*.3f,h*.61f,false);Card(p,x+9,y+9,84,36,new Color(1,.98f,.94f,.93f));Text(p,new[]{"春日","夏日","秋日","冬日"}[photo.season],x+15,y+10,75,33,16,Blue,true);
        }
        void PhotoDialog(int id)
        {
            var photo=State.album.FirstOrDefault(p=>p.id==id);if(photo==null)return;var p=Modal(id<12?World.Places[id].name:World.FestivalNames[id-12],"土豆子拍给你的 / 第 "+photo.trip+" 次外出",1000,820);PhotoVisual(p,photo,32,148,936,398);Text(p,photo.caption,32,575,936,132,26,Ink);Button(p,"把这一页收好",32,733,936,50,CloseModal,Blue);
        }
        void DrawLetters()
        {
            Text(page,"有人，把你放在心上。",45,182,1490,53,37,Ink,true);Text(page,"没有必须立刻回复的消息。读一读，回一句，友谊会慢慢长起来。",47,250,1487,36,22,Muted);
            for(int i=0;i<3;i++){int id=i;var c=Card(page,44+i*509,309,490,135,Color.white).transform;Actor(c,6,0,13,10,91,111,false);Text(c,World.Friends[i]+" · 默契 "+State.friendBond[i],123,16,340,35,25,Colors[i],true);Text(c,World.FriendNotes[i],123,66,340,52,19,Muted);}
            if(State.letters.Count==0){Text(page,"信箱还是空的。土豆子第一次回来时，\n会把认识的朋友和一封信一起带给你。",92,560,1400,128,31,Muted,false,TextAlignmentOptions.Center);return;}
            var letters=State.letters.AsEnumerable().Reverse().ToArray();var list=Scroll(page,44,475,1511,439,letters.Length*141);
            for(int i=0;i<letters.Length;i++){var l=letters[i];float y=i*141;var c=Card(list,0,y,1489,125,Color.white).transform;Text(c,(l.read?"已读":"新来信")+" / "+World.Friends[l.friend]+" · "+(l.kind==0?"第 "+l.chapter+" 封心事":"日常小纸条 · 第 "+(l.id/10)+" 次外出"),23,14,1102,29,18,Colors[l.friend],true);Text(c,l.title,23,58,1095,42,28,Ink,true);Button(c,l.reply>=0?"看看我们的回信":"拆开信封",1198,35,266,54,()=>LetterDialog(l.id),Coral,true,"Letter"+l.id);}
        }
        void LetterDialog(int id)
        {
            var l=State.letters.First(x=>x.id==id);bool first=Life.ReadLetter(State,id);Save.Write(State);RefreshStats();if(first)Sound.Fx("mail");
            var p=Modal(l.title,"来自 "+World.Friends[l.friend]+" / 读信后心情 +5、陪伴 +2（仅首次）",1060,785);Actor(p,6,0,30,170,196,245);Text(p,l.body,262,161,761,256,26,Ink);
            if(!string.IsNullOrEmpty(l.previousReplies))Button(p,"看看以前的回复",262,424,761,31,()=>LetterHistory(l),Blue,true,"LetterHistory"+id);
            Text(p,l.reply<0?"选一句回给对方。这三句回复都会让故事继续，不会扣任何东西。":"你写过："+World.ReplyLines[l.reply],32,461,996,70,22,Muted);
            string[] ids={"ReplyTogether","ReplyAdvice","ReplyLater"};Color[] tones={Coral,Blue,Muted};
            for(int i=0;i<3;i++){int choice=i;string label=l.reply<0?World.ReplyLines[i]+"（"+World.ReplyNotes[i]+"）":(l.reply==i?"已寄出 · ":"")+World.ReplyLines[i];Button(p,label,32,540+i*72,996,58,()=>{Life.Reply(State,id,choice);Save.Write(State);Render();},l.reply<0||l.reply==i?tones[i]:Pale,l.reply<0,ids[i]);}
        }
        void LetterHistory(Letter letter)
        {
            var p=Modal("和"+World.Friends[letter.friend]+"的往来记录。","以前的回复也留在这里。",1060,760);
            var list=Scroll(p,32,152,996,470,Math.Max(470,(letter.previousReplies.Split('\n').Length+1)*64));
            Text(list,letter.previousReplies,12,12,944,((RectTransform)list).rect.height-24,23,Ink);
            Button(p,"返回信件",32,665,996,54,()=>LetterDialog(letter.id),Blue,true,"BackToLetter");
        }
        int shopTab;
        void DrawShop()
        {
            Text(page,"让小屋，多一点你的样子。",45,181,900,55,37,Ink,true);
            string[] tabs=World.Shops.Concat(new[]{"换个房间"}).ToArray();
            for(int t=0;t<tabs.Length;t++){int id=t;Button(page,tabs[t],990+t*190,186,176,48,()=>{shopTab=id;Sound.Fx("page");Render();},shopTab==t?Coral:Muted,true,"Shop"+t);}
            if(shopTab==2){DrawRooms();return;}
            Text(page,World.ShopNotes[shopTab]+"  已摆放 "+State.placed.Count+" / "+Tuning.MaxPlaced,47,251,1478,38,21,Muted);
            var ids=Enumerable.Range(0,World.Items.Length).Where(i=>World.Items[i].shop==shopTab).ToArray();
            var list=Scroll(page,44,317,1511,608,(ids.Length+3)/4*355);
            for(int n=0;n<ids.Length;n++)
            {
                int id=ids[n];var item=World.Items[id];bool owned=State.owned.Contains(id);float x=(n%4)*374,y=(n/4)*355;var c=Card(list,x,y,354,333,Color.white).transform;
                Item(c,id,97,14,160,128);Text(c,item.name,19,150,316,36,27,Ink,true);Text(c,item.description,19,200,316,64,21,Muted);
                string label=owned?(item.type==0?(State.placed.Contains(id)?"已摆放 · 收起来":"摆进房间"):(State.selectedBag==id?"已放进行囊":"放进行囊")):"购买 · "+item.price+" 金币";
                Button(c,label,19,281,316,34,()=>{if(!owned){if(Life.Buy(State,id)){Save.Write(State);Render();Float("新物件！魅力 +3",Mint);Sound.Fx("purchase");}}else{if(item.type==0){if(!Life.PlaceItem(State,id)){Float("最多摆 "+Tuning.MaxPlaced+" 件，先收起一件给它腾个位置吧。",Blue);return;}}else Life.Pack(State,id);Save.Write(State);Render();}},owned?Mint:Coral,owned?(item.type==0||!State.travelling):State.coins>=item.price,"Buy"+id);
            }
        }
        // Five homes: three free real-world rooms and two to unlock once. Decorations follow you, each room remembers its own layout.
        void DrawRooms()
        {
            Text(page,"搬家免费，已摆放的物品会跟着你；每间房会记住你拖好的位置。",47,251,1100,38,21,Muted);
            if(State.spots.Any(p=>p.room==State.room))Button(page,"这间的家具放回默认位置",1186,247,360,42,()=>{Life.ResetSpots(State);Save.Write(State);Render();Float("家具都回到原来的位置啦。",Blue);},Blue,true,"ResetSpots");
            var list=Scroll(page,44,317,1511,608,(World.Rooms.Length+2)/3*520);
            for(int i=0;i<World.Rooms.Length;i++)
            {
                int id=i;bool here=State.room==i,owned=State.rooms.Contains(i);int price=World.RoomPrice[i];var c=Card(list,(i%3)*503,(i/3)*520,486,500,Color.white).transform;
                Picture(c,homes[i],14,14,458,305);if(!owned){var tag=Card(c,24,24,190,40,new Color(1,.98f,.93f,.95f)).transform;Text(tag,"需解锁 · "+price+" 金币",10,6,170,28,18,Coral,true,TextAlignmentOptions.Center);}
                Text(c,World.Rooms[i],20,332,446,40,28,Ink,true);Text(c,World.RoomNotes[i],20,382,446,52,19,Muted);
                string label=here?"现在住在这里":owned?"搬进这一间":State.coins>=price?"解锁并搬进去 · "+price+" 金币":"还差 "+(price-State.coins)+" 金币";
                Button(c,label,20,440,446,46,()=>{
                    if(!owned){if(!Life.BuyRoom(State,id))return;Sound.Fx("purchase");}
                    if(Life.ChooseRoom(State,id)){Save.Write(State);if(owned)Sound.Fx("purchase");tab="小屋";Render();Float("搬进了「"+World.Rooms[id]+"」，东西都摆好啦。",Mint);}
                },here?Muted:Coral,!here&&(owned||State.coins>=price),"Room"+i);
            }
        }
        void Profile()
        {
            var p=Modal("一颗越来越自在的土豆。","这三项只记录成长，不给你排排名。",1010,744);Actor(p,0,State.outfit,26,153,250,319);
            string[] names={"智慧","魅力","陪伴"};int[] values={State.wisdom,State.charm,State.company};for(int i=0;i<3;i++){Card(p,302,158+i*95,662,80,Pale);Text(p,names[i],319,169+i*95,340,58,27,Ink,true,TextAlignmentOptions.MidlineLeft);var number=Text(p,values[i].ToString(),714,163+i*95,232,69,32,Colors[i],true,TextAlignmentOptions.MidlineRight);number.name="ProfileValue"+i;}
            Text(p,"最常去的方向："+World.Directions[Life.SuggestEnding(State)]+"\n认识了 "+State.friendBond.Count(x=>x>0)+" 位朋友 · 收到 "+State.letters.Count+" 封来信",32,500,946,97,25,Muted);Button(p,"再去过一点喜欢的日子",32,641,946,61,CloseModal,Blue);
        }
        bool focusTodo;
        void QuestDialog()
        {
            var p=Modal("把小事，放进同一张清单。","每日任务完成即换新；抵港小目标只奖励一次；自己的待办也算数。没完成不扣资源。",1500,860);
            Text(p,"今日待办",32,150,446,43,27,Blue,true);Text(p,"抵港小目标",527,150,446,43,27,Blue,true);Text(p,"我的待办",1022,150,446,43,27,Blue,true);
            string[] titles={"摸摸土豆子三次","收好第一次外出的手账","给房间或行囊添一件物品"};bool full=Life.DailyFull(State);
            for(int i=0;i<3;i++)
            {
                int id=i;float y=210+i*164;bool ready=Life.DailyReady(State,i);
                var c=Card(p,32,y,446,146,Color.white).transform;Text(c,Life.Task(State,i).title,19,10,408,37,23,Ink,true);Text(c,DailyLine(i,full)+" · 心情 +3",19,55,408,31,19,Muted);
                Button(c,full?"今天领满啦":ready?"收下奖励，换个新任务":DailyVerb(Life.Task(State,i).kind),19,98,408,36,()=>DailyAction(id),ready?Coral:Mint,!full,"DailyList"+i);
                c=Card(p,527,y,446,146,Color.white).transform;Text(c,titles[i],19,10,408,37,23,Ink,true);Text(c,"奖励 25 金币 · 心情 +5",19,55,408,31,19,Muted);
                Button(c,State.questClaimed[i]?"已领小奖励":Life.QuestReady(State,i)?"收下奖励":"慢慢来",19,98,408,36,()=>{if(Life.ClaimQuest(State,id))Sound.Fx("coin");Save.Write(State);RefreshStats();QuestDialog();},State.questClaimed[i]?Muted:Mint,!State.questClaimed[i]&&Life.QuestReady(State,i),"Quest"+i);
            }
            Text(p,"完成即换新 · 今日已领 "+State.dailyClaims+" / "+Tuning.DailyCap,32,700,446,32,19,Muted);
            var input=Field(p,"写下想做的小事，按回车添加",1022,210,330,52,21,"TodoInput");input.characterLimit=Tuning.TodoLength;
            System.Action add=()=>{if(Life.AddTodo(State,input.text)==null){Float(string.IsNullOrEmpty(input.text.Trim())?"先写下一件想做的小事吧。":"未完成的待办已经很多啦，先完成几件吧。",Blue);return;}Save.Write(State);Sound.Fx("page");focusTodo=true;QuestDialog();};
            input.onSubmit.AddListener(_=>add());Button(p,"添加",1364,210,104,52,()=>add(),Coral,true,"TodoAdd");
            int bonus=State.placed.Sum(i=>World.Items[i].todoCoins);
            Text(p,"完成一项：金币 +"+(Tuning.TodoCoins+bonus)+" · 心情 +2 · 智慧 +1\n每天前 "+Tuning.TodoDailyRewards+" 项有奖励（今日 "+State.todoRewards+" / "+Tuning.TodoDailyRewards+"）",1022,272,446,58,18,Muted);
            var todos=State.todos.Where(t=>!t.done).Concat(State.todos.Where(t=>t.done).Reverse()).ToArray();
            var list=Scroll(p,1022,340,446,400,System.Math.Max(400,todos.Length*62));
            if(todos.Length==0)Text(list,"还没有自己的待办。\n比如：读完一篇论文、去超市买牛奶、给家里打个电话。",12,20,410,120,20,Muted);
            for(int i=0;i<todos.Length;i++)
            {
                var t=todos[i];int id=t.id;var c=Card(list,0,i*62,430,54,t.done?Pale:Color.white).transform;
                Text(c,(t.done?"✓ ":"")+t.text,14,10,t.done?350:290,34,19,t.done?Muted:Ink);
                if(!t.done)Button(c,"完成",306,9,62,36,()=>FinishTodo(id),Mint,true,"TodoDone"+id);
                Button(c,"×",374,9,44,36,()=>{Life.RemoveTodo(State,id);Save.Write(State);QuestDialog();},Muted,true,"TodoDel"+id);
            }
            Button(p,"回小屋",32,776,1436,52,CloseModal,Blue);
            if(focusTodo){focusTodo=false;input.ActivateInputField();}
        }
        void DailyAction(int id)
        {
            Life.Tick(State,Save.Now);
            if(Life.ClaimDaily(State,id)){bool listOpen=modal!=null;Save.Write(State);Sound.Fx("coin");if(listOpen)QuestDialog();else Render();Float("今日小奖励 · 金币 +"+Tuning.DailyCoins+" · 换上新任务啦",Mango);RefreshStats();return;}
            if(Life.DailyFull(State)){Float("今天的奖励都领满啦，明天再来。",Blue);return;}
            int kind=Life.Task(State,id).kind;
            if(kind==0){bool listOpen=modal!=null;int before=State.coins;string note=Life.Pet(State);Save.Write(State);Sound.Fx(State.coins>before?"coin":"company");if(listOpen)QuestDialog();RefreshStats();Float(note,Coral);}
            else if(kind==1||kind==5)
            {
                if(State.pending.Count>0){CollectGifts();return;}
                if(State.festivalPending>=0){FestivalDialog();return;}
                if(Life.YearReady(State)&&!State.graduated){EndingDialog();return;}
                if(State.tutorial==0){DirectionDialog();return;}
                if(Life.Depart(State,Save.Now)){tab="小屋";Save.Write(State);Render();Sound.Fx("page");}
                else Float(Life.DepartureLock(State),Blue);
            }
            else if(kind==2||kind==6){tab="来信";Render();if(State.letters.All(l=>l.read&&l.reply>=0))Float("先送土豆子出门，回来时会带上新的来信。",Blue);}
            else if(kind==3){tab="陪伴";Render();}
            else if(kind==4)QuestDialog();
            else{tab="小店";Render();}
        }
        void FestivalDialog()
        {
            int season=State.festivalPending;if(season<0)return;string[] body={"新来的土豆们约着去灯会。有人提着兔子灯，有人猜灯谜。你不必表现得很熟练，只要来，就有人给你留位置。","海边响起了鼓点。阿乐问你要不要一起看龙舟，荣叔说看完来喝一杯凉凉的。夏天忽然有了可以期待的小事。","阿晴把花灯拍成一颗小月亮，说想寄给家里看看。你发现，想家和喜欢这里，可以同时放在心里。","大家挤进小屋，把照片铺了一桌。有上坡八分钟，有没赶上的船，还有很多次一起吃饭。毕业不是把这些日子关起来，是把它们带走。"};
            var p=Modal(World.FestivalNames[season],"季节主题故事 / 完成即获得纪念照和新衣服",1110,850);Picture(p,places,32,145,1046,273,3);Actor(p,6,season+1,444,179,204,225,false);Text(p,body[season],32,444,1046,127,26,Ink);
            Button(p,"和大家一起，把热闹拍下来",32,606,1046,64,()=>FinishFestival(0),Coral,true,"FestivalTogether");Button(p,"写下祝福，留一份安静的纪念",32,694,1046,64,()=>FinishFestival(1),Blue,true,"FestivalWish");
        }
        void FinishFestival(int choice){Life.Festival(State,choice);Save.Write(State);Render();Sound.Fx("coin");Float("节日纪念 +1 · 新衣服 +1 · 金币 +200",Coral);}
        void EndingDialog()
        {
            if(!Life.YearReady(State))return;int e=string.IsNullOrEmpty(State.ending)?Life.SuggestEnding(State):int.Parse(State.ending);var p=Modal("这一年，你成为了"+World.Endings[e]+"。","学年纪念 / 没有坏结局，每一页日常都算数",1200,900);
            var list=Scroll(p,32,147,1136,612,1150+Math.Min(16,State.album.Count)*12);
            Actor(list,7,4,0,10,246,280,false);Text(list,World.DiaryEnding(e),281,23,815,164,29,Ink);Text(list,"这一年留下了 "+State.album.Count+" 张照片、"+State.letters.Count+" 封信和 "+State.owned.Count+" 件心爱的小物。",281,220,815,74,24,Blue,true);
            Text(list,"一起走过这一年的朋友",0,333,1098,41,29,Ink,true);for(int i=0;i<3;i++){string line=State.friendChapter[i]>=3?"谢谢你陪我把这一年走熟，下一段路还可以互相写信。":State.friendBond[i]>0?"认识你，是这一年很值得记住的小事。":"还没好好聊过，下一次相遇也值得期待。";Text(list,World.Friends[i]+"："+line,0,398+i*79,1090,66,23,Muted);}
            Text(list,"把照片翻一遍，再出发。",0,660,1098,39,28,Ink,true);for(int i=0;i<Math.Min(8,State.album.Count);i++){var ph=State.album[i];PhotoVisual(list,ph,(i%4)*278,725+(i/4)*190,260,174);}
            var available=Life.AvailableEndings(State);for(int i=0;i<available.Length;i++){int id=available[i];float w=1136f/available.Length-10;Button(p,World.Endings[id]+(id==e?" · 此刻":""),32+i*(w+10),780,w,43,()=>{Life.Graduate(State,id);Save.Write(State);EndingDialog();},id==e?Coral:Blue,true,"Ending"+id);}
            Button(p,"把这一年收好，继续陪伴  →",32,839,1136,40,()=>{if(!State.graduated)Life.Graduate(State,e);Save.Write(State);Render();},Mint,true,"FinishYear");
        }
        void Settings()
        {
            var p=Modal("让节奏，合你的心意。","时间、声音和手账",1030,794);
            Text(p,"外出节奏",32,148,962,36,27,Ink,true);Button(p,State.demo?"正在使用：30 秒演示模式":"切换到 30 秒演示模式",32,200,966,55,()=>{Life.Mode(State,!State.demo,Save.Now);Save.Write(State);RefreshStats();Settings();},Blue,true,"ToggleDemo");
            Text(p,State.demo?"适合课堂和第一次试玩。切换模式会保留当前外出的完成比例。":"慢生活模式：每次外出 30—120 分钟，可关闭游戏，最多积攒 3 份收获。",32,274,966,68,21,Muted);
            Button(p,State.autoRoam?"连续外出：开（最多等你收三份）":"连续外出：关（每次由你送它出门）",32,361,966,52,()=>{State.autoRoam=!State.autoRoam;Save.Write(State);Settings();},Mint,true,"ToggleAuto");
            Text(p,"背景音乐",32,445,180,32,22,Ink);Button mute=null;VolumeSlider(p,232,455,615,Sound.MusicVolume,v=>{Sound.SetMusic(v);if(mute!=null)mute.GetComponentInChildren<TextMeshProUGUI>().text=v<.01f?"开启":"静音";});mute=Button(p,Sound.MusicVolume<.01f?"开启":"静音",866,441,132,42,()=>{Sound.SetMusic(Sound.MusicVolume<.01f?.32f:0);Settings();},Muted,true,"MuteMusic");
            musicTrackText=Text(p,"当前曲目 · "+Sound.NowPlaying,32,500,805,32,21,Muted);Button(p,"下一首",866,495,132,42,()=>{Sound.NextTrack();musicTrackText.text="当前曲目 · "+Sound.NowPlaying;},Blue,true,"NextMusic");
            Text(p,"气泡与提示音",32,568,180,32,22,Ink);VolumeSlider(p,232,578,615,Sound.EffectVolume,v=>Sound.SetEffects(v));
            Button(p,"保留进度，返回扉页",32,636,469,56,()=>{Save.Write(State);menu=true;Render();},Blue);Button(p,"重新开启一段生活",519,636,479,56,Restart,Muted);
            Text(p,"新版独立存档，不会影响《港硕这一年》的游戏进度。",32,728,966,32,18,Muted);
        }
        void VolumeSlider(Transform p,float x,float y,float w,float value,Action<float> changed)
        {
            var r=UI.Rect("Volume",p,x,y,w,26);var bg=Card(r,0,9,w,8,Pale);bg.raycastTarget=true;
            var fillArea=UI.Rect("FillArea",r,0,9,w,8);var fill=Card(fillArea,0,0,w,8,Mint);
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            var handle=Card(r,0,0,25,26,Blue);handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,.5f);handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;
            var slider=r.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;handle.raycastTarget=true;slider.minValue=0;slider.maxValue=1;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
        void Restart()
        {var p=Modal("给新的故事，留一本空手账？","新游戏会覆盖本游戏当前进度，旧港硕游戏不受影响。",960,440);Button(p,"继续现在的小日子",32,290,429,70,()=>{if(menu)Render();else CloseModal();},Blue);Button(p,"确认重新开始",479,290,449,70,NewGame,Coral,true,"ConfirmNewGame");}
        void DrawDecorations(Transform scene)
        {
            // Farther items first, so things nearer the front overlap them. Every item can be dragged.
            float k=1040f/1536f;
            foreach(int id in State.placed.Where(i=>World.Slots[State.room][i]!=null).OrderBy(i=>{var r=Life.ItemRect(State,i);return r[1]+r[3];}))
            {
                var a=Life.ItemRect(State,id);var image=Item(scene,id,a[0]*k,a[1]*k,a[2]*k,a[3]*k);image.raycastTarget=true;int item=id;
                image.gameObject.AddComponent<DragItem>().dropped=pos=>{if(Life.MoveItem(State,item,Mathf.RoundToInt(pos.x/k),Mathf.RoundToInt(-pos.y/k))){Save.Write(State);RefreshStats();}};
            }
        }
    }
}
