using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using static PotatoLife.UI;
namespace PotatoLife
{
    // v0.1.3: 阿晴的周末邀请, real-photo city postcards, the travel frame and the year-in-review lines.
    public partial class PotatoGame
    {
        static readonly Dictionary<string,Texture2D> photoCache=new Dictionary<string,Texture2D>();
        static Texture2D PhotoTexture(PhotoSpot spot,bool thumb)
        {string path="Photos/"+spot.key+(thumb?"_thumb":"");Texture2D t;if(!photoCache.TryGetValue(path,out t)){t=Resources.Load<Texture2D>(path);photoCache[path]=t;}return t;}
        // The whole photo inside the box at its own aspect ratio: never stretched, never cropped, no generated fill.
        static UnityEngine.Rect Fit(PhotoSpot spot,float x,float y,float w,float h)
        {float a=(float)spot.width/spot.height,fw=w,fh=w/a;if(fh>h){fh=h;fw=h*a;}return new UnityEngine.Rect(x+(w-fw)/2,y+(h-fh)/2,fw,fh);}
        RawImage CityPicture(Transform p,UnityEngine.Rect r,PhotoSpot spot,bool thumb)
        {
            var tex=PhotoTexture(spot,thumb);
            if(tex==null){Card(p,r.x,r.y,r.width,r.height,Pale);Text(p,"照片文件缺失\n"+spot.key,r.x+6,r.y+r.height/2-24,r.width-12,48,16,Muted,false,TextAlignmentOptions.Center);return null;}
            return Picture(p,tex,r.x,r.y,r.width,r.height);
        }
        RawImage CityPicture(Transform p,PhotoSpot spot,float x,float y,float w,float h,bool thumb){return CityPicture(p,Fit(spot,x,y,w,h),spot,thumb);}
        // A paper ticket used for story memories and keepsakes that have no picture of their own.
        void TicketCard(Transform p,float x,float y,float w,float h,string title,string body,float titleSize=30,float bodySize=24)
        {
            var c=Card(p,x,y,w,h,Hex("FFF1D6"),"Ticket").transform;Card(c,0,0,14,h,Mango);
            for(int i=0;i<(int)((h-20)/26);i++)Card(c,w-40,12+i*26,6,13,Hex("E2CFA8"));
            Text(c,title,34,titleSize*.7f,w-96,titleSize*1.6f,titleSize,Ink,true);Text(c,body,34,titleSize*2.6f,w-96,h-titleSize*2.6f-16,bodySize,Ink);
        }
        // ---- 阿晴的周末邀请 ----
        // The notice-board entry when the story needs the player; null when it can wait quietly.
        string StoryEntry()
        {
            var w=State.weekend;if(w.stage==1)return "阿晴发来周末邀请";if(w.stage==3&&w.rain==1)return "阿晴发来一条消息";if(w.stage==4)return "拆开阿晴的纸条";
            return Life.StoryTripWaiting(State)&&!State.travelling&&State.pending.Count==0?"准备行囊，和阿晴去大澳":null;
        }
        void StoryAction()
        {var w=State.weekend;if(w.stage==1)InviteDialog();else if(w.stage==3&&w.rain>0)RainDialog();else if(w.stage==4)StoryNoteDialog();else if(Life.StoryTripWaiting(State))StoryPackDialog();}
        void InviteDialog()
        {
            if(State.weekend.stage!=1)return;var p=Modal(World.StoryInvite,"阿晴 / 周末邀请 · 现在决定或稍后再说都可以",1110,830);
            Actor(p,6,0,32,145,120,150,false);Text(p,"三个安排都很好，没有对错。基础奖励一样，不同的是这一天的故事和留下的纪念。",176,170,900,70,22,Muted);
            for(int i=0;i<3;i++)
            {
                int route=i+1;var c=Card(p,32,262+i*146,1046,130,Color.white).transform;
                Text(c,World.StoryRoutes[i],26,16,680,44,28,Ink,true);Text(c,World.StoryRouteNotes[i],26,68,680,50,20,Muted);
                Button(c,"就这样安排",748,38,272,54,()=>ChooseRoute(route),Blue,true,"Route"+route);
            }
            Button(p,"稍后再说，邀请会留在今日待办和来信里",32,722,1046,56,CloseModal,Muted,true,"StoryLater");
        }
        void ChooseRoute(int route)
        {if(!Life.StoryChoose(State,route))return;Save.Write(State);Sound.Fx("page");tab="小屋";Render();if(route==1)StoryPackDialog();else StorySceneDialog();}
        void StoryPackDialog()
        {
            if(!Life.StoryTripWaiting(State))return;var p=Modal("出发前，带点什么？","和阿晴去大澳 · 预报说这天会下雨",1130,810);int[] ids={-1,5,6,7};
            for(int i=0;i<4;i++)
            {
                int id=ids[i];bool have=id<0||State.owned.Contains(id),on=State.selectedBag==id;var c=Card(p,32+i*270,148,250,340,on?Hex("FFF1D6"):Color.white).transform;
                if(id>=0)Item(c,id,55,16,140,112);else Actor(c,0,State.outfit,70,8,110,124,false);
                Text(c,id<0?"轻装出门":World.Items[id].name,12,140,226,36,24,Ink,true,TextAlignmentOptions.Center);Text(c,World.StoryBagHints[i],16,184,218,86,20,Muted,false,TextAlignmentOptions.Center);
                Button(c,on?"已带好":have?"带这个":"小店有售",16,280,218,44,()=>{if(Life.Pack(State,id)){Save.Write(State);StoryPackDialog();}},on?Blue:have?Mint:Muted,have&&!on&&!State.travelling,"StoryBag"+id);
            }
            string locked=Life.DepartureLock(State);
            Text(p,locked!=""?locked+"。准备好了再出发，行囊会替你留着。":"行囊只改变路上的片段，没有最好的选择；什么都不带，也能完整走完这一天。",32,510,1066,64,21,locked!=""?Coral:Muted);
            Button(p,"和阿晴出发去大澳",32,600,1066,64,()=>{if(!Life.Depart(State,Save.Now))return;Sound.Fx("page");Save.Write(State);tab="小屋";Render();WalkOutOfDoor();},Coral,locked=="","StoryGo");
            Button(p,"改天再出发",32,690,1066,52,CloseModal,Muted,true,"StoryPackLater");
        }
        void RainDialog()
        {
            var w=State.weekend;if(w.rain==0)return;if(Life.StoryRainSeen(State)){Save.Write(State);pageStale=true;}
            bool open=w.stage==3&&w.rainReply==0&&State.travelling&&State.trip.story==1;
            var p=Modal(World.StoryRain,"阿晴 / 路上的消息 · 不回复也能好好走完",1000,670);Actor(p,6,0,32,145,120,150,false);
            if(open)
            {
                Text(p,"回一句，或者先放着。回家以后也能再看这条消息。",176,170,790,64,22,Muted);string[] names={"RainDrink","RainWalk","RainLater"};
                for(int i=0;i<3;i++){int choice=i+1;Button(p,World.StoryRainReplies[i],32,320+i*82,936,64,()=>{bool sent=choice<3&&Life.StoryRain(State,choice);if(sent){Save.Write(State);Sound.Fx("mail");}Render();if(sent)Float("回复了阿晴："+World.StoryRainReplies[choice-1],Blue);},i==2?Muted:Blue,true,names[i]);}
                return;
            }
            Text(p,w.rainReply>0?"你回了："+World.StoryRainReplies[w.rainReply-1]:"那时没有回复，也没关系。",176,166,790,44,24,Ink,true);
            Text(p,World.StoryRainLines[w.rainReply],176,224,790,110,23,Muted);Button(p,"知道啦",32,566,936,60,CloseModal,Blue,true,"RainClose");
        }
        // The Tai O trip's return: the memory first, the numbers in a quieter side column.
        void StoryHarvestDialog(ReturnGift g,int coins,int wisdom,int charm,int company,int others)
        {
            var w=State.weekend;int bag=Life.StoryBag(State);var p=Modal("这趟，留下了什么？","第 "+g.trip.number+" 次外出 · 和阿晴去大澳",1120,830);
            TicketCard(p,32,148,700,440,World.StoryBagTitles[bag],World.StoryBagLines[bag]+"\n\n"+World.StoryRainLines[w.rainReply]);
            var side=Card(p,760,148,328,440,Pale).transform;Text(side,"顺便带回",22,18,284,34,22,Blue,true);
            Text(side,"金币 +"+coins+"\n智慧 +"+wisdom+" · 魅力 +"+charm+"\n陪伴 +"+company+"\n明信片 · 大澳慢慢走"+(others>0?"\n还有 "+others+" 份日常手账":""),22,62,284,230,20,Muted);
            Text(side,"大澳没有真实照片，这一页用票根记下。",22,330,284,80,18,Muted);
            Button(p,"拆开阿晴的纸条",32,620,1056,66,StoryNoteDialog,Coral,true,"OpenNote");Button(p,"先收进手账，稍后再拆",32,706,1056,52,()=>{tab="小屋";Render();},Muted,true,"NoteLater");
        }
        // Routes B and C: a short scene at home, no trip and no Tai O results.
        void StorySceneDialog()
        {
            var w=State.weekend;if(w.route<2)return;int r=w.route-1;var p=Modal("这一天，留下了什么？",r==1?"校园附近 · 和阿晴吃顿饭":"小屋 · 今天想自己待着",1120,830);
            TicketCard(p,32,148,700,440,World.StorySceneTitles[r],World.StorySceneLines[r]);
            var side=Card(p,760,148,328,440,Pale).transform;Actor(side,r==1?6:5,r==1?State.outfit:0,64,30,200,240,false);
            Text(side,"不用出门，也不花体力。回完阿晴的纸条，奖励和其他安排一样。",22,300,284,120,19,Muted);
            if(w.stage==4){Button(p,"拆开阿晴的纸条",32,620,1056,66,StoryNoteDialog,Coral,true,"OpenNote");Button(p,"先收进手账，稍后再拆",32,706,1056,52,()=>{tab="小屋";Render();},Muted,true,"NoteLater");}
            else Button(p,"合上这一页",32,620,1056,66,CloseModal,Blue,true,"SceneClose");
        }
        Letter StoryLetter(){return State.letters.FirstOrDefault(x=>x.kind==2&&x.id==Life.StoryLetterId(State.weekend.route));}
        void StoryNoteDialog()
        {
            var w=State.weekend;var l=StoryLetter();if(l==null)return;if(w.stage>=5){StoryRepliedDialog();return;}
            bool first=Life.ReadLetter(State,l.id);Save.Write(State);RefreshStats();if(first)Sound.Fx("mail");int r=w.route-1;
            var p=Modal("阿晴的纸条",(r==0?"大澳回来以后":r==1?"吃完饭以后":"那天晚上")+" / 读信后心情 +5、陪伴 +2（仅首次）",1080,860);
            var note=Card(p,32,148,1016,200,Hex("FFF1D6")).transform;Text(note,l.body,28,22,960,160,27,Ink);
            Text(p,"选一句回给阿晴。三句的奖励一样：金币 +"+Tuning.StoryCoins+" · 心情 +"+Tuning.StoryMood+" · 陪伴 +"+Tuning.StoryCompany+"，还有一件纪念。",32,372,1016,64,21,Muted);
            for(int i=0;i<3;i++){int choice=i;Button(p,World.StoryReplies[r,i],32,452+i*82,1016,64,()=>{if(!Life.Reply(State,l.id,choice))return;Save.Write(State);Sound.Fx("coin");pageStale=true;StoryRepliedDialog();},Blue,true,"StoryReply"+i);}
            Button(p,"稍后再回",32,710+48,1016,50,CloseModal,Muted,true,"StoryReplyLater");
        }
        void StoryRepliedDialog()
        {
            var w=State.weekend;int k=Life.Keepsake(State);if(k<0)return;int r=w.route-1;
            var p=Modal(Life.StoryResponse(State),"阿晴回你 / 你写了："+World.StoryReplies[r,w.reply-1],1080,800);
            Text(p,"金币 +"+Tuning.StoryCoins+" · 心情 +"+Tuning.StoryMood+" · 陪伴 +"+Tuning.StoryCompany+" · 默契 +"+Tuning.StoryBond,32,150,1016,34,20,Muted);
            Text(p,"收进手账的纪念",32,200,1016,36,24,Blue,true);TicketCard(p,32,246,1016,250,World.Keepsakes[k].name,World.Keepsakes[k].line+"\n点手账里的这张票，可以回看这一天的安排和回信。",32,24);
            Text(p,w.echo==2?"后来的回响已经收到，也记在这张票的背面。":"下次普通外出回来时，也许会收到阿晴的一点回响。",32,516,1016,40,21,Muted);
            Button(p,"收进手账，去看看纪念",32,590,1016,66,()=>{tab="手账";albumToStory=true;Render();},Coral,true,"KeepsakeAlbum");Button(p,"回小屋",32,676,1016,52,()=>{tab="小屋";Render();},Muted,true,"StoryHome");
        }
        // Looks back on the whole story from its keepsake: arrangement, bag, messages, both notes and the echo.
        void StoryReview()
        {
            var w=State.weekend;int k=Life.Keepsake(State);if(k<0)return;int r=w.route-1;var l=StoryLetter();
            var blocks=new List<string[]>{new[]{"那天的安排",World.StoryRoutes[r]}};
            if(r==0)
            {
                int bag=Life.StoryBag(State);blocks.Add(new[]{"带的行囊",bag==0?"轻装出门":World.Items[bag+4].name});
                blocks.Add(new[]{"路上的消息",World.StoryRain+"\n"+(w.rainReply>0?"你回了："+World.StoryRainReplies[w.rainReply-1]:"那时没有回复，也没关系。")});
                blocks.Add(new[]{World.StoryBagTitles[bag],World.StoryBagLines[bag]+"\n"+World.StoryRainLines[w.rainReply]});
            }
            else blocks.Add(new[]{World.StorySceneTitles[r],World.StorySceneLines[r]});
            if(l!=null)blocks.Add(new[]{"阿晴的纸条",l.body});blocks.Add(new[]{"你的回复",World.StoryReplies[r,w.reply-1]});blocks.Add(new[]{"阿晴的回答",Life.StoryResponse(State)});
            if(w.echo==2)blocks.Add(new[]{"后来的回响 · 第 "+w.echoTrip+" 次外出",Life.StoryEcho(State)});
            var p=Modal(World.Keepsakes[k].name,"故事纪念 / 阿晴的周末邀请 · "+World.Keepsakes[k].line,1160,880);
            float y=0;var heights=blocks.Select(b=>44+34*Mathf.Ceil(b[1].Length/36f+b[1].Count(c=>c=='\n')*.6f)+30).ToArray();var list=Scroll(p,32,148,1096,600,heights.Sum());
            for(int i=0;i<blocks.Count;i++){Text(list,blocks[i][0],8,y,1060,36,22,Blue,true);Text(list,blocks[i][1],8,y+42,1060,heights[i]-56,24,Ink);y+=heights[i];}
            Button(p,"合上这一页",32,776,1096,62,CloseModal,Blue,true,"ReviewClose");
        }
        bool albumToStory;
        void KeepsakeCard(Transform list,float x,float y)
        {
            int k=Life.Keepsake(State);var c=Card(list,x,y,354,348,Color.white,"KeepsakeCard").transform;
            if(k<0){Card(c,14,14,326,229,Pale);Text(c,"阿晴的周末邀请\n还没有写完",30,86,296,84,25,Muted,false,TextAlignmentOptions.Center);Text(c,"故事纪念",16,264,322,42,24,Muted,true,TextAlignmentOptions.Center);return;}
            TicketCard(c,14,14,326,229,World.Keepsakes[k].name,World.Keepsakes[k].line,26,21);Text(c,"故事纪念 · 阿晴",16,260,322,39,24,Ink,true);Button(c,"回看这一天",16,304,322,35,StoryReview,Coral,true,"Keepsake");
        }
        // ---- City postcards: real photos in the album ----
        void CityCard(Transform list,PhotoSpot spot,float x,float y)
        {
            var photo=State.album.FirstOrDefault(p=>p.id==spot.id);var c=Card(list,x,y,354,348,Color.white).rectTransform;int id=spot.id;
            if(photo==null){Card(c,14,14,326,229,Pale);Text(c,"还有一张明信片\n正在路上",30,86,296,84,25,Muted,false,TextAlignmentOptions.Center);Text(c,spot.area,16,264,322,42,24,Muted,true,TextAlignmentOptions.Center);return;}
            if(photo.unopened){PostcardBack(c,photo,14,14,326,229);Text(c,spot.area,16,260,322,39,24,Ink,true);Button(c,"拆开这张明信片",16,304,322,35,()=>OpenPostcard(id),Coral,true,"Open"+id);return;}
            Card(c,14,14,326,229,Hex("F3EEE4"));CityPicture(c,spot,20,20,314,217,true);Text(c,spot.title,16,260,322,39,24,Ink,true);
            Button(c,spot.area+" · 打开这一页",16,304,322,35,()=>PhotoDialog(id),Blue,true,"Photo"+id);
        }
        bool cityStacked,cityStackedSet;
        // Landscape card: the photo whole on the left at its own ratio, place and notes on the right; the back holds the longer story.
        void CityPostcard(int id,bool back)
        {
            var photo=State.album.FirstOrDefault(p=>p.id==id);var spot=World.Spot(id);if(photo==null||spot==null)return;
            if(!cityStackedSet){cityStackedSet=true;cityStacked=Screen.height>0&&(float)Screen.width/Screen.height<1.25f;}
            if(cityStacked){CityPostcardStacked(photo,spot);return;}
            var p=Modal(spot.area+" · "+spot.title,"城市明信片 / 第 "+photo.trip+" 次外出，回家路上经过",1340,880);var card=Card(p,32,145,1276,612,Color.white,"Postcard").transform;
            Card(card,1276-100,22,72,84,Colors[photo.season]);Card(card,1276-94,28,60,72,Cream);Text(card,"港",1276-94,40,60,46,26,Colors[photo.season],true,TextAlignmentOptions.Center);
            if(back)
            {
                Text(card,"背面",40,30,600,36,22,Blue,true);Text(card,spot.back,40,80,700,300,25,Ink);
                if(!string.IsNullOrEmpty(photo.revisits))Text(card,photo.revisits,40,396,700,120,19,Muted);
                Card(card,780,40,3,500,Hex("E2CFA8"));Text(card,"寄给：在香港读研的你",820,190,400,40,23,Ink);for(int i=0;i<3;i++)Card(card,820,260+i*56,400,3,Hex("E2CFA8"));
                Actor(card,2,photo.outfit,1100,430,130,150,false);
                Text(card,"这是真实拍摄的香港街景。游戏里的季节、日期和天气，不是这张照片的拍摄信息。",40,548,1100,44,18,Muted);
            }
            else
            {
                var box=Fit(spot,24,24,600,564);box.x=24;Card(card,box.x-8,box.y-8,box.width+16,box.height+16,Hex("F3EEE4"));var pic=CityPicture(card,box,spot,false);
                if(pic!=null){pic.name="PhotoZoom";pic.raycastTarget=true;var zoom=pic.gameObject.AddComponent<Button>();zoom.targetGraphic=pic;zoom.onClick.AddListener(()=>{Sound.Fx("page");PhotoViewer(id,back);});}
                float tx=box.x+box.width+48,tw=1276-tx-30;
                Text(card,spot.area,tx,30,tw-110,32,21,Blue,true);Text(card,spot.title,tx,68,tw-110,50,32,Ink,true);Text(card,"第 "+photo.trip+" 次外出 · 回家路上经过",tx,126,tw,30,19,Muted);
                Card(card,tx,170,tw,3,Hex("E2CFA8"));Text(card,spot.note,tx,194,tw,210,26,Ink);
                // The potato sticks to the paper beside the text, never on the photo.
                Actor(card,2,photo.outfit,1276-170,420,130,150,false);Text(card,"—— 土豆子",tx,440,tw-180,34,21,Muted);
            }
            Button(p,back?"看看正面":"看看背面",32,782,250,58,()=>CityPostcard(id,!back),Blue,true,"PostcardFlip");
            Button(p,"看大图",298,782,190,58,()=>PhotoViewer(id,back),Mint,true,"PhotoBig");
            Button(p,"上下阅读",504,782,200,58,()=>{cityStacked=true;CityPostcard(id,false);},Muted,true,"PostcardStack");
            Button(p,"把这一页收好",720,782,588,58,CloseModal,Coral,true,"PostcardDone");
        }
        // Narrow windows: photo on top at its own ratio, then the front and back text in one scrolling column.
        void CityPostcardStacked(Photo photo,PhotoSpot spot)
        {
            int id=spot.id;var p=Modal(spot.title,spot.area+" / 城市明信片 · 第 "+photo.trip+" 次外出，回家路上经过",1000,940);
            var r=Fit(spot,0,0,920,620);r.x=(920-r.width)/2;float y=r.height+28;bool revisits=!string.IsNullOrEmpty(photo.revisits);
            var list=Scroll(p,32,140,936,680,y+180+260+(revisits?140:0)+90);
            Card(list,r.x-6,r.y,r.width+12,r.height+6,Hex("F3EEE4"));var pic=CityPicture(list,new UnityEngine.Rect(r.x,r.y+3,r.width,r.height),spot,false);
            if(pic!=null){pic.name="PhotoZoom";pic.raycastTarget=true;var zoom=pic.gameObject.AddComponent<Button>();zoom.targetGraphic=pic;zoom.onClick.AddListener(()=>PhotoViewer(id,false));}
            Text(list,spot.note,4,y,900,160,28,Ink);y+=180;Text(list,spot.back,4,y,900,240,26,Ink);y+=260;
            if(revisits){Text(list,photo.revisits,4,y,900,120,21,Muted);y+=140;}
            Text(list,"这是真实拍摄的香港街景。游戏里的季节、日期和天气，不是这张照片的拍摄信息。",4,y,900,70,19,Muted);
            Button(p,"换成明信片版面",32,850,300,58,()=>{cityStacked=false;CityPostcard(id,false);},Muted,true,"PostcardStack");
            Button(p,"看大图",348,850,190,58,()=>PhotoViewer(id,false),Mint,true,"PhotoBig");Button(p,"把这一页收好",554,850,414,58,CloseModal,Coral,true,"PostcardDone");
        }
        // The full photo over a dark backdrop; closing returns to the same side of the postcard.
        void PhotoViewer(int id,bool back)
        {
            var spot=World.Spot(id);if(spot==null)return;CloseModal();modal=UI.Rect("Modal",root,0,0,1600,1000);
            Card(modal,0,0,1600,1000,new Color(.1f,.12f,.14f,.95f)).raycastTarget=true;var r=Fit(spot,40,30,1520,860);var pic=CityPicture(modal,r,spot,false);if(pic!=null)pic.name="PhotoFull";
            Text(modal,spot.area+" · "+spot.title,40,912,1150,44,22,Color.white,true);Button(modal,"返回明信片",1300,904,260,58,()=>CityPostcard(id,back),Coral,true,"ViewerBack");
        }
        // ---- The travel frame (item 20): ordinary furniture drawn in code, sized to the chosen photo ----
        Image Furniture(Transform p,int id,float x,float y,float w,float h)
        {
            if(id!=Tuning.FrameItem)return Item(p,id,x,y,w,h);
            var r=UI.Rect("Item"+id,p,x,y,w,h);var hit=r.gameObject.AddComponent<Image>();hit.color=new Color(1,1,1,0);hit.raycastTarget=false;
            var spot=World.Spot(State.framePhoto);float a=spot!=null?(float)spot.width/spot.height:.75f,fw=w,fh=w/a;if(fh>h){fh=h;fw=h*a;}float fx=(w-fw)/2,fy=(h-fh)/2,m=Mathf.Max(4,Mathf.Min(fw,fh)*.1f);
            Card(r,fx,fy,fw,fh,Hex("A87A4F"),"Wood");Card(r,fx+m*.5f,fy+m*.5f,fw-m,fh-m,Hex("C99B6B"),"Wood");Card(r,fx+m,fy+m,fw-2*m,fh-2*m,Cream,"Mat");
            if(spot!=null)CityPicture(r,spot,fx+m*1.4f,fy+m*1.4f,fw-2.8f*m,fh-2.8f*m,true);else Text(r,"点我\n挑照片",fx+m,fy+m,fw-2*m,fh-2*m,16,Muted,true,TextAlignmentOptions.Center);
            return hit;
        }
        void FramePicker()
        {
            var opened=State.album.Where(ph=>!ph.unopened&&World.Spot(ph.id)!=null).Select(ph=>World.Spot(ph.id)).ToArray();
            var p=Modal("挂哪一张在墙上？","旅行相框 / 只能挂已经拆开的城市明信片，换了也会保存",1300,860);
            if(opened.Length==0){Text(p,"还没有拆开的城市明信片。\n土豆子出门回家时，会顺路寄一张回来。",32,300,1236,120,27,Muted,false,TextAlignmentOptions.Center);Button(p,"知道啦",32,764,1236,58,CloseModal,Blue,true,"FrameClose");return;}
            var list=Scroll(p,32,145,1236,590,(opened.Length+3)/4*300);
            for(int i=0;i<opened.Length;i++)
            {
                var spot=opened[i];int id=spot.id;bool on=State.framePhoto==id;var c=Card(list,(i%4)*305,(i/4)*300,290,286,on?Hex("FFF1D6"):Color.white).transform;
                Card(c,12,12,266,180,Hex("F3EEE4"));CityPicture(c,spot,16,16,258,172,true);Text(c,spot.title,12,198,266,34,20,Ink,true);
                Button(c,on?"正挂着":"挂这张",12,238,266,36,()=>{if(!Life.ChooseFrame(State,id))return;Save.Write(State);Render();Float("相框里换上了「"+spot.title+"」。",Mint);},on?Muted:Coral,!on,"Frame"+id);
            }
            Button(p,"回小屋",32,764,1236,58,CloseModal,Blue,true,"FrameClose");
        }
        // ---- Graduation: a year in review built only from what was actually recorded ----
        string[] YearJournal()
        {
            var s=State;var lines=new List<string>();int best=Life.SuggestEnding(s);
            if(s.directionCount[best]>0)lines.Add("最常选的方向是「"+World.Directions[best]+"」，一共 "+s.directionCount[best]+" 次。四个方向："+string.Join(" · ",Enumerable.Range(0,4).Where(i=>s.directionCount[i]>0).Select(i=>World.Directions[i]+" "+s.directionCount[i]+" 次").ToArray())+"。");
            int legacy=Life.LegacyPhotos(s),city=Life.CityPhotos(s);
            if(legacy+city>0)lines.Add("收进手账的照片：插画明信片 "+legacy+" / 16"+(city>0?"，真实城市明信片 "+city+" / "+World.Spots.Length+"（"+string.Join("、",s.album.Where(p=>World.Spot(p.id)!=null).Select(p=>World.Spot(p.id).title).ToArray())+"）":"")+"。");
            for(int i=0;i<3;i++){int read=s.letters.Count(l=>l.friend==i&&l.read),replied=s.letters.Count(l=>l.friend==i&&l.reply>=0);if(read+replied>0)lines.Add(World.Friends[i]+"：读了 "+read+" 封信，回了 "+replied+" 封。");}
            if(s.studyTotal>=60){int m=s.studyTotal/60;lines.Add("陪伴学习累计 "+(m>=60?m/60+" 小时 ":"")+m%60+" 分钟。");}
            int k=Life.Keepsake(s);if(k>=0)lines.Add("完成的故事：阿晴的周末邀请（"+World.StoryRoutes[s.weekend.route-1].TrimEnd('。')+"），留下了「"+World.Keepsakes[k].name+"」。");
            return lines.ToArray();
        }
    }
    // A plain click on a decoration (not the end of a drag); used by the travel frame to change its picture.
    public class ClickItem:MonoBehaviour,IPointerClickHandler
    {public Action clicked;public void OnPointerClick(PointerEventData e){if(!e.dragging&&clicked!=null)clicked();}}
}
