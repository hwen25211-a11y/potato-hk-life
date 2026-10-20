using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using static PotatoLife.UI;
namespace PotatoLife
{
    public partial class PotatoGame:MonoBehaviour
    {
        public PotatoState State{get;private set;}
        public bool MenuVisible{get{return menu;}}
        RectTransform root,page,modal,effects;Texture2D home,places;string tab="小屋";bool menu=true,pageStale;
        TextMeshProUGUI coinText,energyText,moodText,countdown;SmoothBar energyBar,moodBar,tripBar;long lastSecond,lastSaved;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(FindObjectOfType<PotatoGame>()==null)new GameObject("Potato life").AddComponent<PotatoGame>();}
        void Awake()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;home=Resources.Load<Texture2D>("Art/Home");places=Resources.Load<Texture2D>("Art/PlacesAtlas");
            UI.Font=Resources.Load<TMP_FontAsset>("Fonts/Potato SDF");if(UI.Font==null){UI.Font=TMP_FontAsset.CreateFontAsset(Resources.Load<UnityEngine.Font>("Fonts/NotebookSans"));UI.Font.atlasPopulationMode=AtlasPopulationMode.Dynamic;UI.Font.isMultiAtlasTexturesEnabled=true;}
            var go=new GameObject("Potato Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,1000);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root=UI.Rect("Root",go.transform,0,0,1600,1000);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            if(FindObjectOfType<EventSystem>()==null)new GameObject("Events",typeof(EventSystem),typeof(StandaloneInputModule));var cam=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>();cam.backgroundColor=Cream;cam.clearFlags=CameraClearFlags.SolidColor;cam.orthographic=true;
            new GameObject("Listener",typeof(AudioListener));Sound.Initialize(gameObject);Render();
        }
        void Update()
        {
            if(State==null||menu)return;long now=Save.Now;
            if(now!=lastSecond){lastSecond=now;bool arrived=Life.Tick(State,now);if(arrived){Sound.Fx("return");Save.Write(State);if(tab=="小屋"&&modal==null)Render();else{pageStale=true;Float("土豆子带回了新的手账，去门口收好吧。",Mango);}}RefreshStats();if(now-lastSaved>=5){lastSaved=now;Save.Write(State);}}
            if(Input.GetKeyDown(KeyCode.Escape)&&modal!=null)CloseModal();
        }
        void OnApplicationPause(bool p){if(p&&State!=null){Life.Tick(State,Save.Now);Save.Write(State);}}
        void OnApplicationQuit(){if(State!=null){Life.Tick(State,Save.Now);Save.Write(State);}}
        void Clear(Transform target){for(int i=target.childCount-1;i>=0;i--){var c=target.GetChild(i);c.gameObject.SetActive(false);Destroy(c.gameObject);}}
        public void NewGame(){State=Life.New(Save.Now,UnityEngine.Random.Range(1,1000000));menu=false;tab="小屋";Save.Write(State);Render();}
        public void ContinueGame(){State=Save.Read();if(State==null)return;Life.Tick(State,Save.Now);menu=false;tab="小屋";Save.Write(State);Render();}
        public void Render()
        {
            Clear(root);modal=null;pageStale=false;coinText=energyText=moodText=countdown=null;energyBar=moodBar=tripBar=null;Card(root,0,0,1600,1000,Cream);
            page=UI.Rect("Page",root,0,0,1600,1000);page.gameObject.AddComponent<PageFade>();effects=UI.Rect("Feedback",root,0,0,1600,1000);
            if(menu){DrawTitle();return;}Header();if(tab=="手账")DrawAlbum();else if(tab=="来信")DrawLetters();else if(tab=="小店")DrawShop();else DrawRoom();RefreshStats();
            Text(page,Save.Error!=""?Save.Error:"已悄悄保存 · 今天也没有必须完成的任务",46,957,1160,28,16,Muted);
            Text(page,State.demo?"演示模式 · 一次外出 30 秒":"慢生活 · 一次外出 30—120 分钟",1160,957,394,28,16,Blue,false,TextAlignmentOptions.TopRight);
        }
        void Header()
        {
            Text(page,"土豆子的港硕生活",44,29,640,57,36,Ink,true);Text(page,"从一只行李箱，到一间越来越像家的小屋。",47,96,925,33,20,Muted);
            string[] tabs={"小屋","手账","来信","小店"};for(int i=0;i<4;i++){string t=tabs[i];Button(page,t+(t=="来信"&&State.letters.Any(l=>!l.read)?" ·":""),905+i*132,30,118,50,()=>{tab=t;if(t=="手账"){Life.OpenAlbum(State);Save.Write(State);}Sound.Fx("page");Render();},tab==t?Blue:Muted,true,"Tab"+t);}
            Button(page,"设置",1450,30,105,50,Settings,Muted,true,"Settings");
            var c=Card(page,1050,99,159,49,Hex("FFE9B4")).transform;coinText=Text(c,"",13,9,134,32,21,Ink,true);
            c=Card(page,1222,99,159,49,Hex("E1F3DC")).transform;energyText=Text(c,"",12,5,134,27,19,Ink);energyBar=Bar(c,12,36,134,5,State.energy,Mint).GetComponent<SmoothBar>();
            c=Card(page,1394,99,159,49,Hex("FFE4DC")).transform;moodText=Text(c,"",12,5,134,27,19,Ink);moodBar=Bar(c,12,36,134,5,State.mood,Coral).GetComponent<SmoothBar>();
        }
        void DrawTitle()
        {
            Picture(page,home,0,0,1600,1000);Card(page,59,73,677,814,new Color(1,.98f,.93f,.97f));Text(page,"POTATO, ONE YEAR IN HONG KONG",100,115,593,33,18,Blue,true);
            Text(page,"土豆子的\n港硕生活",95,203,600,178,64,Ink,true);Text(page,"你来选方向，\n它把日子过成小惊喜。",101,422,590,95,30,Blue);
            Text(page,"没有挂科，没有催促。\n出门看一看，回来收一页手账。",103,551,580,92,25,Muted);Actor(page,0,0,960,402,370,444);
            Button(page,"和土豆子一起抵港  →",102,696,589,65,()=>{if(Save.Exists)Restart();else NewGame();},Coral,true,"NewGame");Button(page,"回到我的小屋",102,779,589,61,ContinueGame,Blue,Save.Exists,"Continue");
            Text(page,"首次试玩默认 30 秒演示外出，可在设置切回慢生活。",782,916,748,38,21,Ink);
        }
        void DrawRoom()
        {
            var scene=Card(page,44,176,1040,704,Color.white).rectTransform;Picture(scene,State.travelling?places:home,0,0,1040,704,State.travelling?World.Places[State.trip.place].art:-1);
            var tag=Card(scene,21,20,353,93,new Color(1,.98f,.93f,.95f)).transform;Text(tag,World.Seasons[Life.Season(State)],17,12,322,32,23,Ink,true);Text(tag,State.travelling?World.Weather[State.trip.weather]+" · "+World.Places[State.trip.place].name:"第 "+(State.completed+1)+" 页日常 · 小屋里有好天气",17,55,322,28,17,Muted);
            int pose=State.travelling?((int)State.trip.direction==0?1:(int)State.trip.direction==1?2:(int)State.trip.direction==2?3:6):State.energy<30?5:State.pending.Count>0?4:0;
            var actor=Actor(scene,pose,State.travelling?State.trip.outfit:State.outfit,361,281,318,357);var button=actor.gameObject.AddComponent<Button>();actor.GetComponent<Image>().raycastTarget=true;button.targetGraphic=actor.GetComponent<Image>();button.onClick.AddListener(()=>{Sound.Fx("company");Float(Life.Pet(State),Coral);RefreshStats();Save.Write(State);});actor.name="PetPotato";
            if(!State.travelling)DrawDecorations(scene);
            var speech=Card(scene,277,157,610,99,new Color(1,1,1,.96f)).transform;
            Text(speech,State.travelling?World.Places[State.trip.place].caption:State.pending.Count>0?"我回来啦！这次也带了好多小事情给你。":State.energy<30?"呼……先在家充一会儿电，什么都不会错过。":"今天想往哪个方向走？也可以先抱抱我。",21,17,568,67,23,Ink);
            DrawNoticeBoard();
            Button(page,State.tutorial==0?"① 选今天的方向":"方向 · "+World.Directions[(int)State.direction],44,902,327,53,DirectionDialog,Blue,true,"PickDirection");
            Button(page,"行囊 / 换装",386,902,288,53,Wardrobe,Mint,State.tutorial>=1,"Wardrobe");
            string locked=Life.DepartureLock(State);Button(page,State.pending.Count>0?"带回 "+State.pending.Count+" 份手账 · 收好它们":State.travelling?"土豆子正在替你过日子":State.energy<25?"正在自动充电…":"② 送土豆子出门",690,902,394,53,()=>{if(State.pending.Count>0)CollectGifts();else if(Life.Depart(State,Save.Now)){Sound.Fx("page");Save.Write(State);Render();}},Coral,State.pending.Count>0||locked==""&&State.tutorial>0,"Depart");
        }
        void DrawNoticeBoard()
        {
            var p=Card(page,1110,176,444,704,Color.white).transform;Card(p,155,-7,134,24,Hex("FFDF91"));
            string heading=State.tutorial<4?"只教你三件小事":State.pending.Count>0?"门口有新收获":"今天，慢一点也可以";Text(p,heading,25,34,393,44,27,Ink,true);
            string line=State.tutorial==0?"先点下方蓝色按钮，告诉土豆子今天想做什么。":State.tutorial==1?"方向选好了。点「送土豆子出门」，剩下的交给它。":State.tutorial==2?"它在外面忙自己的小事。回来后收好照片，再点上方「手账」。":State.tutorial==3?"③ 点上方「手账」，看看土豆子拍了什么。":State.lastHighlight;
            Text(p,line,25,105,393,117,25,Muted);
            countdown=Text(p,"",25,240,393,52,31,Blue,true);tripBar=Bar(p,25,313,393,12,0,Mint).GetComponent<SmoothBar>();
            Text(p,State.travelling?"可以关掉游戏。再次回来时，最多收 3 份外出收获。":"每次回来都有照片、金币和朋友来信。\n不必一直盯着屏幕。",25,349,393,85,20,Muted);
            if(State.festivalPending>=0)Button(p,"拆开"+World.FestivalNames[State.festivalPending]+"邀请",25,455,393,62,FestivalDialog,Mango,true,"Festival");
            else if(Life.YearReady(State))Button(p,"翻开这一年的毕业手账",25,455,393,62,EndingDialog,Mango,true,"Graduation");
            else Button(p,"看看土豆子的资料卡",25,455,393,62,Profile,Mango,true,"Profile");
            Button(p,"可选的小目标",25,537,393,54,QuestDialog,Mint,true,"Quests");Text(p,"已收好 "+State.completed+" 次外出 / 四季手账 16 次\n照片 "+State.album.Count+" / 16 · 朋友来信 "+State.letters.Count,25,619,393,63,18,Muted);
        }
        void RefreshStats()
        {
            if(State==null)return;if(coinText!=null)coinText.text="金币  "+State.coins;if(energyText!=null)energyText.text="体力  "+State.energy.ToString("0");if(moodText!=null)moodText.text="心情  "+State.mood.ToString("0");if(energyBar!=null)energyBar.Set(State.energy);if(moodBar!=null)moodBar.Set(State.mood);
            if(countdown!=null){long remaining=State.travelling?Math.Max(0,State.trip.finishUtc-Math.Max(Save.Now,State.lastUtc)):0;countdown.text=State.travelling?(remaining>=60?(remaining/60)+" 分 "+(remaining%60)+" 秒后回来":remaining+" 秒后回来"):State.pending.Count>0?State.pending.Count+" 份小惊喜等你收":"在家，慢慢充电中";}
            if(tripBar!=null)tripBar.Set(State.travelling?(float)Life.Progress(State,Save.Now)*100:State.pending.Count>0?100:0);
            Sound.Theme(State.festivalPending>=0?"Festival":State.travelling?"Travel":DateTime.Now.Hour>=20||DateTime.Now.Hour<6?"Night":"Day");
        }
        void Float(string message,Color color){var t=Text(effects,message,380,476,850,72,29,color,true,TextAlignmentOptions.Center);t.gameObject.AddComponent<FloatingNote>();}
        RectTransform Modal(string title,string subtitle,float w=1060,float h=808)
        {CloseModal();modal=UI.Rect("Modal",root,0,0,1600,1000);var shade=Card(modal,0,0,1600,1000,new Color(.18f,.26f,.29f,.5f));shade.raycastTarget=true;var p=Card(modal,(1600-w)/2,(1000-h)/2,w,h,Cream).rectTransform;p.gameObject.AddComponent<PageFade>();Text(p,subtitle,32,24,w-110,31,18,Blue,true);Text(p,title,32,73,w-110,54,34,Ink,true);Button(p,"×",w-74,24,43,43,CloseModal,Muted);return p;}
        void CloseModal(){if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}if(pageStale){pageStale=false;Render();}}
        void CollectGifts()
        {
            int count=State.pending.Count;int coins=State.pending.Sum(g=>g.coins);int photos=State.pending.Sum(g=>g.photos.Count);string note=State.pending.Last().highlight;
            Life.Collect(State,Save.Now);Save.Write(State);Render();Sound.Fx("stamp");var p=Modal("回来啦，收获都在这里。","第 "+State.completed+" 次外出 · 已存进你的手账",1040,734);Actor(p,4,State.outfit,29,174,241,299);Text(p,note,310,177,690,88,31,Blue,true);
            Text(p,"金币 +"+coins+"\n带回 "+photos+" 张照片 · "+count+" 封来信\n智慧、魅力和陪伴也悄悄长大了。",310,301,690,155,25,Ink);Text(p,"重复照片会化为陪伴，不会白跑一趟。",32,527,975,41,20,Muted);
            Button(p,"③ 翻开新手账  →",32,623,975,65,()=>{Life.OpenAlbum(State);tab="手账";Save.Write(State);Render();Sound.Fx("page");},Coral,true,"OpenNewAlbum");
        }
    }
}
