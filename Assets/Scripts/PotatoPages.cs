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
            Text(page,"把普通的日子，贴成一本书。",45,183,1485,56,37,Ink,true);Text(page,"12 个香港角落 + 4 张节日纪念。照片保留拍摄时的衣服，回忆不会被换掉。",47,250,1489,36,21,Muted);
            var list=Scroll(page,44,314,1512,605,1460);
            var order=State.album.OrderByDescending(p=>p.trip).Select(p=>p.id).Concat(Enumerable.Range(0,16).Where(id=>!State.album.Any(p=>p.id==id))).ToArray();
            for(int position=0;position<16;position++)
            {
                int id=order[position],index=id;var photo=State.album.FirstOrDefault(p=>p.id==id);int row=position/4,col=position%4;var c=Card(list,col*374,row*356,354,348,Color.white).rectTransform;
                if(photo==null){Card(c,14,14,326,229,Pale);Text(c,"还有一个小故事\n正在路上",30,86,296,84,25,Muted,false,TextAlignmentOptions.Center);Text(c,id<12?World.Places[id].name:World.FestivalNames[id-12],16,264,322,42,24,Muted,true,TextAlignmentOptions.Center);}
                else{PhotoVisual(c,photo,14,14,326,229);Text(c,id<12?World.Places[id].name:World.FestivalNames[id-12],16,260,322,39,24,Ink,true);Button(c,"第 "+photo.trip+" 次外出 · 打开这一页",16,304,322,35,()=>PhotoDialog(index),Colors[photo.season],true,"Photo"+id);}
            }
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
            for(int i=0;i<letters.Length;i++){var l=letters[i];float y=i*141;var c=Card(list,0,y,1489,125,Color.white).transform;Text(c,(l.read?"已读":"新来信")+" / "+World.Friends[l.friend]+" · 第 "+l.chapter+" 封心事",23,14,1102,29,18,Colors[l.friend],true);Text(c,l.title,23,58,1095,42,28,Ink,true);Button(c,l.reply>=0?"看看我们的回信":"拆开信封",1198,35,266,54,()=>LetterDialog(l.id),Coral,true,"Letter"+l.id);}
        }
        void LetterDialog(int id)
        {
            var l=State.letters.First(x=>x.id==id);bool first=Life.ReadLetter(State,id);Save.Write(State);RefreshStats();if(first)Sound.Fx("company");
            var p=Modal(l.title,"来自 "+World.Friends[l.friend]+" / 读信后心情 +5、陪伴 +2（仅首次）",1060,785);Actor(p,6,0,30,170,196,245);Text(p,l.body,262,161,761,256,26,Ink);
            Text(p,l.reply<0?"回一小句就好。两种回复都会让关系更近一点。":"你写过："+(l.reply==0?"下次一起走走。":"我把自己的小办法也寄给你。"),32,461,996,77,22,Muted);
            Button(p,l.reply<0?"下次一起走走":"已寄出陪伴",32,571,996,60,()=>{Life.Reply(State,id,0);Save.Write(State);Render();},Coral,l.reply<0,"ReplyTogether");Button(p,l.reply<0?"把自己的小办法写给对方":"回信已收进回忆",32,653,996,60,()=>{Life.Reply(State,id,1);Save.Write(State);Render();},Blue,l.reply<0,"ReplyAdvice");
        }
        void DrawShop()
        {
            Text(page,"让小屋，多一点你的样子。",45,181,1479,55,37,Ink,true);Text(page,"花费只发生在你点击购买时。没有房租和自动扣款，也没有必须买的东西。",47,251,1478,38,21,Muted);
            var list=Scroll(page,44,317,1511,608,739);
            for(int i=0;i<World.Items.Length;i++)
            {
                int id=i;var item=World.Items[i];bool owned=State.owned.Contains(i);float x=(i%4)*374,y=(i/4)*355;var c=Card(list,x,y,354,333,Color.white).transform;
                Decoration(c,i,112,19,130,115);Text(c,item.name,19,150,316,36,27,Ink,true);Text(c,item.description,19,200,316,64,21,Muted);
                string label=owned?(item.type==0?(State.placed.Contains(i)?"已摆放 · 收起来":"摆进房间"):(State.selectedBag==i?"已放进行囊":"放进行囊")):"购买 · "+item.price+" 金币";
                Button(c,label,19,281,316,34,()=>{if(!owned){if(Life.Buy(State,id)){Save.Write(State);Render();Float("新物件！魅力 +3",Mint);Sound.Fx("coin");}}else{if(item.type==0){if(!Life.PlaceItem(State,id)){Float("先收起一件喜欢的物品，给它腾个位置吧。",Blue);return;}}else Life.Pack(State,id);Save.Write(State);Render();}},owned?Mint:Coral,owned?(item.type==0||!State.travelling):State.coins>=item.price,"Buy"+id);
            }
        }
        void Profile()
        {
            var p=Modal("一颗越来越自在的土豆。","这三项只记录成长，不给你排排名。",1010,744);Actor(p,0,State.outfit,26,153,250,319);
            string[] names={"智慧","魅力","陪伴"};int[] values={State.wisdom,State.charm,State.company};for(int i=0;i<3;i++){Text(p,names[i],319,172+i*95,340,37,29,Ink,true);Text(p,values[i].ToString(),714,172+i*95,250,40,33,Colors[i],true,TextAlignmentOptions.TopRight);}
            Text(p,"最常去的方向："+World.Directions[Life.SuggestEnding(State)]+"\n认识了 "+State.friendBond.Count(x=>x>0)+" 位朋友 · 收到 "+State.letters.Count+" 封来信",32,500,946,97,25,Muted);Button(p,"再去过一点喜欢的日子",32,641,946,61,CloseModal,Blue);
        }
        void QuestDialog()
        {
            var p=Modal("有空就做，完成就庆祝。","三个小目标 / 没完成也不会扣任何东西",1020,700);string[] titles={"摸摸土豆子三次","收好第一次外出的手账","给房间或行囊添一件物品"};
            for(int i=0;i<3;i++){int id=i;var c=Card(p,32,154+i*145,956,127,Color.white).transform;Text(c,titles[i],21,15,648,37,26,Ink,true);Text(c,"奖励 25 金币 · 心情 +5",21,72,648,32,21,Muted);Button(c,State.questClaimed[i]?"已领小奖励":Life.QuestReady(State,i)?"收下奖励":"慢慢来",708,35,225,53,()=>{Life.ClaimQuest(State,id);Save.Write(State);RefreshStats();Sound.Fx("stamp");QuestDialog();},State.questClaimed[i]?Muted:Mint,!State.questClaimed[i]&&Life.QuestReady(State,i),"Quest"+i);}
            Button(p,"回小屋",32,611,956,50,CloseModal,Blue);
        }
        void FestivalDialog()
        {
            int season=State.festivalPending;if(season<0)return;string[] body={"新来的土豆们约着去灯会。有人提着兔子灯，有人猜灯谜。你不必表现得很熟练，只要来，就有人给你留位置。","海边响起了鼓点。阿乐问你要不要一起看龙舟，荣叔说看完来喝一杯凉凉的。夏天忽然有了可以期待的小事。","阿晴把花灯拍成一颗小月亮，说想寄给家里看看。你发现，想家和喜欢这里，可以同时放在心里。","大家挤进小屋，把照片铺了一桌。有上坡八分钟，有没赶上的船，还有很多次一起吃饭。毕业不是把这些日子关起来，是把它们带走。"};
            var p=Modal(World.FestivalNames[season],"季节主题故事 / 完成即获得纪念照和新衣服",1110,850);Picture(p,places,32,145,1046,273,3);Actor(p,6,season+1,444,179,204,225,false);Text(p,body[season],32,444,1046,127,26,Ink);
            Button(p,"和大家一起，把热闹拍下来",32,606,1046,64,()=>FinishFestival(0),Coral,true,"FestivalTogether");Button(p,"写下祝福，留一份安静的纪念",32,694,1046,64,()=>FinishFestival(1),Blue,true,"FestivalWish");
        }
        void FinishFestival(int choice){Life.Festival(State,choice);Save.Write(State);Render();Sound.Fx("stamp");Float("节日纪念 +1 · 新衣服 +1 · 金币 +200",Coral);}
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
            Text(p,"背景音乐",32,445,180,32,22,Ink);VolumeSlider(p,232,455,615,Sound.MusicVolume,v=>Sound.SetMusic(v));Button(p,Sound.MusicVolume<.01f?"开启":"静音",866,441,132,42,()=>{Sound.SetMusic(Sound.MusicVolume<.01f?.32f:0);Settings();},Muted);
            Text(p,"气泡与提示音",32,520,180,32,22,Ink);VolumeSlider(p,232,530,615,Sound.EffectVolume,v=>Sound.SetEffects(v));
            Button(p,"保留进度，返回扉页",32,636,469,56,()=>{Save.Write(State);menu=true;Render();},Blue);Button(p,"重新开启一段生活",519,636,479,56,Restart,Muted);
            Text(p,"新版独立存档，不会影响《港硕这一年》的游戏进度。",32,728,966,32,18,Muted);
        }
        void VolumeSlider(Transform p,float x,float y,float w,float value,Action<float> changed)
        {
            var r=UI.Rect("Volume",p,x,y,w,26);var bg=Card(r,0,9,w,8,Pale);bg.raycastTarget=true;var fill=Card(r,0,9,w,8,Mint);var handle=Card(r,0,0,25,26,Blue);var slider=r.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;handle.raycastTarget=true;slider.minValue=0;slider.maxValue=1;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
        void Restart()
        {var p=Modal("给新的故事，留一本空手账？","新游戏会覆盖本游戏当前进度，旧港硕游戏不受影响。",960,440);Button(p,"继续现在的小日子",32,290,429,70,()=>{if(menu)Render();else CloseModal();},Blue);Button(p,"确认重新开始",479,290,449,70,NewGame,Coral,true,"ConfirmNewGame");}
        void DrawDecorations(Transform scene)
        {
            foreach(int id in State.placed){float[] xs={131,356,221,721,754};float[] ys={431,546,355,220,498};Decoration(scene,id,xs[id],ys[id],id==1?311:125,id==1?85:117);}
        }
        void Decoration(Transform parent,int id,float x,float y,float w,float h)
        {
            var p=UI.Rect("Decoration"+id,parent,x,y,w,h);float s=Math.Min(w/130,h/115);var r=UI.Rect("Drawing",p,0,0,130,115);r.localScale=Vector3.one*s;
            if(id==0){Card(r,38,62,55,47,Coral);var a=Card(r,20,13,45,57,Mint).rectTransform;a.localRotation=Quaternion.Euler(0,0,30);var b=Card(r,67,4,36,61,Mint).rectTransform;b.localRotation=Quaternion.Euler(0,0,-24);Card(r,61,45,7,38,Blue);}
            else if(id==1){Card(r,1,31,128,64,Hex("97D8B2"));Card(r,15,42,101,40,Hex("D7EFCD"));}
            else if(id==2){Card(r,42,94,60,11,Mango);Card(r,68,36,9,60,Blue);Card(r,22,14,95,43,Mango);Text(r,"+",51,13,48,38,29,Color.white);}
            else if(id==3){Card(r,8,19,115,4,Blue);for(int i=0;i<3;i++){var c=Card(r,5+i*43,25,37,55,Color.white).transform;Card(c,4,6,29,35,Colors[i]);}}
            else if(id==4){Card(r,33,16,28,29,Mango);Card(r,75,16,28,29,Mango);Card(r,23,34,89,70,Mango);Card(r,48,59,9,10,Ink);Card(r,80,59,9,10,Ink);Card(r,60,81,17,10,Coral);}
            else if(id==5){Card(r,21,25,95,79,Coral);Card(r,41,7,54,32,Mango);Card(r,36,44,64,20,Cream);}
            else if(id==6){Card(r,7,29,116,75,Blue);Card(r,35,16,34,21,Blue);Card(r,45,42,53,52,Cream);Card(r,55,52,33,32,Ink);Card(r,15,42,17,11,Mango);}
            else{Card(r,63,38,7,65,Blue);Card(r,13,14,106,43,Mint);Card(r,62,96,24,10,Blue);}
        }
    }
}
