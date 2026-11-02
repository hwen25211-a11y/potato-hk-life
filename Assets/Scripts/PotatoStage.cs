using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace PotatoLife
{
    // One sprite on a stage: a cell of a cols x rows atlas, placed by percent of its parent with its feet on (x,y).
    public class Sp:MonoBehaviour
    {
        public float x,y,w,sx=1,sy=1,lift;public int flip=1;Image im;RectTransform r,area;Sprite[] cells;
        static Dictionary<string,Sprite[]> sheets=new Dictionary<string,Sprite[]>();
        public static Sprite[] Sheet(string path,int cols,int rows)
        {
            string key=path+"/"+cols+"x"+rows;if(sheets.ContainsKey(key))return sheets[key];var t=Resources.Load<Texture2D>(path);var s=new Sprite[cols*rows];float cw=t.width/(float)cols,ch=t.height/(float)rows;
            for(int i=0;i<s.Length;i++)s[i]=Sprite.Create(t,new Rect(i%cols*cw,t.height-(i/cols+1)*ch,cw,ch),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sheets[key]=s;return s;
        }
        public static Sp Make(Transform p,string atlas,int cols,int rows,float w,float x,float y)
        {
            var rt=UI.Rect("Sprite",p,0,0,10,10);rt.pivot=new Vector2(.5f,0);var s=rt.gameObject.AddComponent<Sp>();s.r=rt;s.area=(RectTransform)p;s.im=rt.gameObject.AddComponent<Image>();s.im.raycastTarget=false;s.w=w;s.x=x;s.y=y;s.Set(atlas,cols,rows);s.Frame(0);s.Place();return s;
        }
        public void Set(string atlas,int cols,int rows){cells=Sheet(atlas,cols,rows);}
        public void Frame(int i){im.sprite=cells[i%cells.Length];}
        public void Show(bool on){im.enabled=on;}
        public void Place(){float W=area.rect.width,H=area.rect.height,s=W*w/100;r.sizeDelta=new Vector2(s,s);r.anchoredPosition=new Vector2(W*x/100,-H*y/100+lift);r.localScale=new Vector3(flip*sx,sy,1);}
    }
    // The new potato: blinks, breathes with squash and stretch, hops with anticipation and landing, and walks with real frames.
    public class Potato2D:MonoBehaviour
    {
        const string Idle="Art/Life/Potato",Walk="Art/PotatoWalk",Hello="Art/PotatoWave";
        public Sp s;float t,next,hold;int pose;string state="idle";float target;Action done;
        public static Potato2D Make(Transform p,float w,float x,float y){var s=Sp.Make(p,Idle,4,2,w,x,y);var a=s.gameObject.AddComponent<Potato2D>();a.s=s;a.next=1.5f+UnityEngine.Random.value*2;return a;}
        public bool Busy{get{return state=="walk";}}
        public void Hop(){if(state!="idle")return;state="hop";t=0;}
        public void Pose(int i,float time=1.2f){state="pose";pose=i;t=0;hold=time;}
        public void Go(float x,Action then=null){target=x;done=then;state="walk";t=0;s.Set(Walk,4,1);s.flip=x<s.x?-1:1;}
        public void Wave(){state="wave";t=0;s.Set(Hello,4,1);}
        void Update()
        {
            float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);t+=dt;
            if(state=="idle"){float b=Mathf.Sin(Time.unscaledTime/.62f);s.sx=1+b*.018f;s.sy=1-b*.022f;s.lift=0;next-=dt;s.Frame(next<0&&next>-.14f?1:0);if(next<-.14f)next=2+UnityEngine.Random.value*3;}
            else if(state=="hop")
            {
                if(t<.14f){s.Frame(2);s.sx=1.1f;s.sy=.86f;}else if(t<.56f){float k=(t-.14f)/.42f;s.Frame(3);s.lift=Mathf.Sin(k*Mathf.PI)*((RectTransform)transform.parent).rect.height*.15f;s.sx=.94f;s.sy=1.08f;}
                else if(t<.72f){s.Frame(2);s.lift=0;s.sx=1.1f;s.sy=.88f;}else{s.sx=s.sy=1;state="idle";}
            }
            else if(state=="walk")
            {
                s.Frame((int)(t*9)%4);s.lift=Mathf.Abs(Mathf.Sin(t*14))*3;float d=target-s.x,step=dt*11;
                if(Mathf.Abs(d)<=step){s.x=target;s.lift=0;s.flip=1;s.Set(Idle,4,2);s.Frame(0);state="idle";var f=done;done=null;s.Place();if(f!=null)f();return;}s.x+=Mathf.Sign(d)*step;
            }
            else if(state=="pose"){s.Frame(pose);s.sx=s.sy=1;if(t>hold){state="idle";s.Frame(0);}}
            else if(state=="wave"){s.Frame((int)(t/.14f)%4);if(t>1.6f){s.Set(Idle,4,2);s.Frame(0);state="idle";}}
            s.Place();
        }
    }
    // A pet that keeps moving by itself; what it does depends on how it is being looked after.
    public class Pet2D:MonoBehaviour
    {
        public Sp s;public float min=18,max=82;public float? follow;public bool busy;public string mode="sit";public float wait=1;public Func<float> joy;float t,tx;
        public static string Atlas(string breed){return "Art/Life/Pet_"+breed;}
        public static Pet2D Make(Transform p,string breed,float w,float x,float y,Func<float> joy=null){var s=Sp.Make(p,Atlas(breed),4,2,w,x,y);var a=s.gameObject.AddComponent<Pet2D>();a.s=s;a.tx=x;a.joy=joy;return a;}
        void Update()
        {
            if(busy)return;float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);t+=dt;
            if(follow!=null){float d=follow.Value-s.x;if(Mathf.Abs(d)>1){s.x+=Mathf.Sign(d)*dt*11;s.flip=d<0?-1:1;mode="walk";}else mode="sit";}
            else
            {
                wait-=dt;if(wait<0){float r=UnityEngine.Random.value;if(joy!=null&&joy()<35||r<.25f){mode="sleep";wait=4;}else if(r<.5f){mode="play";wait=1.6f;}else{tx=min+UnityEngine.Random.value*(max-min);mode="walk";wait=99;}}
                if(mode=="walk"&&wait>50){float d=tx-s.x;s.flip=d<0?-1:1;if(Mathf.Abs(d)<1){mode="sit";wait=1.2f+UnityEngine.Random.value*2;}else s.x+=Mathf.Sign(d)*dt*9;}
            }
            s.Frame(mode=="sit"?0:mode=="walk"?1:mode=="play"?2:3);s.lift=mode=="walk"?Mathf.Abs(Mathf.Sin(t*13))*3:mode=="play"?Mathf.Abs(Mathf.Sin(t*6))*7:0;s.sy=mode=="sleep"?1+Mathf.Sin(t*2)*.02f:1;s.Place();
        }
    }
    public class Rise:MonoBehaviour
    {
        public float delay;float t;Vector2 at;RectTransform r;Graphic g;
        void Start(){r=(RectTransform)transform;at=r.anchoredPosition;g=GetComponent<Graphic>();g.color=new Color(g.color.r,g.color.g,g.color.b,0);}
        void Update(){t+=Time.unscaledDeltaTime;float k=(t-delay)/1.5f;if(k<0)return;r.anchoredPosition=at+Vector2.up*(k*80);g.color=new Color(g.color.r,g.color.g,g.color.b,k<.25f?k*4:1-(k-.25f)/.75f);if(k>=1)Destroy(gameObject);}
    }
    // The picture half of a chapter. World holds the scene, Over the speech bubble and hot spots above it.
    public class LifeStage:MonoBehaviour
    {
        public RectTransform world,over;public float W,H;static Sprite heart;RectTransform bubble;TextMeshProUGUI bubbleText;
        public void Build(){var r=(RectTransform)transform;gameObject.AddComponent<RectMask2D>();W=r.rect.width;H=r.rect.height;world=UI.Rect("World",r,0,0,W,H);over=UI.Rect("Over",r,0,0,W,H);}
        void Wipe(Transform t){for(int i=t.childCount-1;i>=0;i--){var c=t.GetChild(i).gameObject;c.SetActive(false);Destroy(c);}}
        public void Clear(){StopAllCoroutines();Wipe(world);Wipe(over);bubble=null;}
        public RawImage Back(Texture tex,Rect? uv=null){var b=UI.Picture(world,tex,0,0,W,H);if(uv!=null)b.uvRect=uv.Value;return b;}
        public void Fill(Color c){UI.Card(world,0,0,W,H,c,"Fill");}
        // Keeps the whole picture, centred on a dark mat: for real photos and portrait illustrations.
        public void Contain(Texture tex,Rect uv,float aspect)
        {
            Fill(UI.Hex("2B3D48"));float w=W,h=W/aspect;if(h>H){h=H;w=H*aspect;}UI.Picture(world,tex,(W-w)/2,(H-h)/2,w,h).uvRect=uv;
        }
        public Image Prop(string path,float leftPct,float bottomPct,float widthPct,float aspect)
        {
            float w=W*widthPct/100,h=w/aspect;var r=UI.Rect("Prop",world,W*leftPct/100,H-H*bottomPct/100-h,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=Sp.Sheet(path,1,1)[0];im.raycastTarget=false;return im;
        }
        public void Say(string text)
        {
            float w=Mathf.Min(W*.84f,720);if(bubble==null){bubble=UI.Card(over,(W-w)/2,H*.04f,w,60,new Color(1,1,1,.95f),"Bubble").rectTransform;bubbleText=UI.Text(bubble,"",16,8,w-32,44,21,UI.Ink,true,TextAlignmentOptions.Center);bubbleText.enableAutoSizing=false;}
            bubbleText.text=text;float h=Mathf.Ceil(bubbleText.GetPreferredValues(text,w-32,0).y)+6;bubbleText.rectTransform.sizeDelta=new Vector2(w-32,h);bubble.sizeDelta=new Vector2(w,h+16);bubble.SetAsLastSibling();
        }
        public Button Hot(float xPct,float yPct,string label,Action fn,string id=null)
        {
            float w=label.Length*21+30;var b=UI.Button(over,label,W*xPct/100-w/2,H*yPct/100-22,w,44,fn,new Color(1,1,1,.94f),true,id);b.GetComponentInChildren<TextMeshProUGUI>().color=UI.Ink;return b;
        }
        public void Later(Action f,float sec){StartCoroutine(After(f,sec));}
        IEnumerator After(Action f,float sec){yield return new WaitForSecondsRealtime(sec);f();}
        // Calls step every sec seconds until it returns false.
        public void Every(float sec,Func<bool> step){StartCoroutine(Loop(sec,step));}
        // Calls step every frame with the real time passed, until it returns false.
        public void Tick(Func<float,bool> step){StartCoroutine(Frames(step));}
        IEnumerator Frames(Func<float,bool> step){while(step(Mathf.Min(.05f,Time.unscaledDeltaTime)))yield return null;}
        IEnumerator Loop(float sec,Func<bool> step){while(step())yield return new WaitForSecondsRealtime(sec);}
        static Sprite Heart
        {
            get
            {
                if(heart!=null)return heart;var t=new Texture2D(64,64,TextureFormat.RGBA32,false);var p=new Color[4096];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){float a=(x-31.5f)/25f,b=(y-28f)/25f,f=Mathf.Pow(a*a+b*b-1,3)-a*a*b*b*b;p[y*64+x]=new Color(1,1,1,Mathf.Clamp01(-f*14));}
                t.SetPixels(p);t.Apply();heart=Sprite.Create(t,new Rect(0,0,64,64),new Vector2(.5f,.5f));return heart;
            }
        }
        public void Hearts(float xPct,float yPct)
        {
            for(int i=0;i<5;i++){var r=UI.Rect("Heart",over,W*(xPct-6+i*3)/100,H*yPct/100,30,30);var im=r.gameObject.AddComponent<Image>();im.sprite=Heart;im.color=UI.Coral;im.raycastTarget=false;r.gameObject.AddComponent<Rise>().delay=i*.12f;}
        }
    }
    // The notice-board half: a scrolling column that stacks what is added, like a web page.
    public class Board
    {
        readonly RectTransform content;readonly ScrollRect scroll;readonly float width,gap;float y;public float Y{get{return y;}}readonly float pad;
        public Image[] fills;public TextMeshProUGUI[] values;public float barWidth;
        public Board(Transform p,float x,float yy,float w,float h,float gapSize=10)
        {
            gap=gapSize;var root=UI.Rect("Board",p,x,yy,w,h);scroll=root.gameObject.AddComponent<ScrollRect>();var v=UI.Rect("Viewport",root,0,0,w,h);v.gameObject.AddComponent<RectMask2D>();var im=v.gameObject.AddComponent<Image>();im.color=new Color(1,1,1,.002f);
            content=UI.Rect("Content",v,0,0,w-16,h);scroll.viewport=v;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;width=w-16;
            var rail=UI.Card(root,w-10,0,10,h,UI.Pale);rail.raycastTarget=true;var thumb=UI.Card(rail.transform,0,0,10,h,UI.Mint);thumb.raycastTarget=true;var tr=thumb.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            var sb=rail.gameObject.AddComponent<Scrollbar>();sb.handleRect=tr;sb.direction=Scrollbar.Direction.BottomToTop;sb.targetGraphic=thumb;scroll.verticalScrollbar=sb;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;scroll.verticalScrollbarSpacing=0;
        }
        void Grow(float h){y+=h+gap;content.sizeDelta=new Vector2(width,y);}
        public void Clear(){for(int i=content.childCount-1;i>=0;i--){var c=content.GetChild(i).gameObject;c.SetActive(false);UnityEngine.Object.Destroy(c);}y=0;rowX=0;inRow=false;fills=null;values=null;content.sizeDelta=new Vector2(width,10);content.anchoredPosition=Vector2.zero;}
        TextMeshProUGUI Block(Transform p,string text,float x,float yy,float w,float size,Color color,bool bold,out float h)
        {
            var t=UI.Text(p,text,x,yy,w,10,size,color,bold);t.enableAutoSizing=false;t.overflowMode=TextOverflowModes.Overflow;h=Mathf.Ceil(t.GetPreferredValues(text,w,0).y)+4;t.rectTransform.sizeDelta=new Vector2(w,h);return t;
        }
        public void Title(string text){float h;Block(content,text,0,y,width,28,UI.Ink,true,out h);Grow(h);}
        public void Title2(string text){float h;Block(content,text,0,y,width,20,UI.Ink,true,out h);Grow(h);}
        public void P(string text){float h;Block(content,text,0,y,width,20,UI.Ink,false,out h);Grow(h);}
        public void Muted(string text){float h;Block(content,text,0,y,width,17,UI.Muted,false,out h);Grow(h);}
        // A text the chapter rewrites later: its height is reserved up front.
        public TextMeshProUGUI Live(float height,float size=19){float h;var t=Block(content," ",0,y,width,size,UI.Ink,false,out h);t.rectTransform.sizeDelta=new Vector2(width,height);Grow(height);return t;}
        public void Ticket(string text)
        {
            float h;var c=UI.Card(content,0,y,width,10,UI.Hex("FFF1D6"));Block(c.transform,text,14,10,width-28,19,UI.Ink,false,out h);c.rectTransform.sizeDelta=new Vector2(width,h+20);Grow(h+20);
        }
        float LabelWidth(string label,float size){var t=UI.Text(content,"",0,0,10,10,size);t.enableAutoSizing=false;float w=t.GetPreferredValues(label,2000,0).x;t.gameObject.SetActive(false);UnityEngine.Object.Destroy(t.gameObject);return w;}
        float rowX;bool inRow;
        public void Row(){if(inRow)y+=44+gap;rowX=0;inRow=false;}
        // Buttons flow left to right and wrap, like the web page's button rows.
        public Button Btn(string label,Action fn,Color? color=null,string id=null,bool enabled=true,float height=42)
        {
            float w=Mathf.Min(width,Mathf.Ceil(LabelWidth(label,19)*1.08f)+36);if(inRow&&rowX+w>width){y+=height+8;rowX=0;}
            var b=UI.Button(content,label,rowX,y,w,height,fn,color,enabled,id);var t=b.GetComponentInChildren<TextMeshProUGUI>();t.fontSize=19;t.enableAutoSizing=false;rowX+=w+8;inRow=true;content.sizeDelta=new Vector2(width,y+height+gap);return b;
        }
        public void EndRow(float height=42){if(inRow){y+=height+gap;inRow=false;rowX=0;content.sizeDelta=new Vector2(width,y);}}
        public Button Choice(string label,Action fn,string id=null)
        {
            EndRow();var probe=UI.Text(content,"",0,0,10,10,19);probe.enableAutoSizing=false;float h=Mathf.Ceil(probe.GetPreferredValues(label,width-24,0).y)+20;probe.gameObject.SetActive(false);UnityEngine.Object.Destroy(probe.gameObject);
            var b=UI.Button(content,label,0,y,width,h,fn,UI.Pale,true,id);var t=b.GetComponentInChildren<TextMeshProUGUI>();t.enableAutoSizing=false;t.fontSize=19;t.fontStyle=FontStyles.Normal;t.color=UI.Ink;t.alignment=TextAlignmentOptions.MidlineLeft;t.overflowMode=TextOverflowModes.Overflow;Grow(h);return b;
        }
        public void Steps(string[] labels,bool[] done)
        {
            EndRow();float x=0;for(int i=0;i<labels.Length;i++){string s=(done[i]?"✓ ":"")+labels[i];float w=Mathf.Ceil(LabelWidth(s,17))+22;var c=UI.Card(content,x,y,w,32,done[i]?UI.Hex("CFEBDA"):UI.Pale);UI.Text(c.transform,s,4,4,w-8,26,17,UI.Ink,done[i],TextAlignmentOptions.Center);x+=w+6;}Grow(32);
        }
        public void Bars(string[] names,float[] levels)
        {
            EndRow();fills=new Image[names.Length];values=new TextMeshProUGUI[names.Length];barWidth=width-110;
            for(int i=0;i<names.Length;i++){UI.Text(content,names[i],0,y,50,26,18);UI.Card(content,56,y+8,barWidth,11,UI.Pale);fills[i]=UI.Card(content,56,y+8,barWidth*levels[i]/100,11,UI.Mint);values[i]=UI.Text(content,"",width-46,y,46,26,18,UI.Ink,false,TextAlignmentOptions.TopRight);y+=30;}Grow(0);SetBars(levels);
        }
        public void SetBars(float[] levels){if(fills==null)return;for(int i=0;i<fills.Length;i++){if(fills[i]==null)return;fills[i].rectTransform.sizeDelta=new Vector2(barWidth*levels[i]/100,11);fills[i].color=levels[i]<35?UI.Coral:UI.Mint;values[i].text=Mathf.RoundToInt(levels[i]).ToString();}}
        // Square picture buttons in a grid: the breed and food pickers.
        public void Grid(int cols,int count,Func<int,string> label,Func<int,string> atlas,Func<int,int[]> cell,Func<int,bool> marked,Action<int> pick,string prefix,Color mark)
        {
            EndRow();float size=(width-(cols-1)*6)/cols,h=size+26;
            for(int i=0;i<count;i++)
            {
                int id=i;var a=cell(i);var b=UI.Button(content,"",i%cols*(size+6),y+i/cols*(h+6),size,h,()=>pick(id),marked(i)?mark:UI.Pale,true,prefix+i);var im=UI.Rect("Icon",b.transform,2,2,size-4,size-4).gameObject.AddComponent<Image>();im.sprite=Sp.Sheet(atlas(i),a[0],a[1])[a[2]];im.raycastTarget=false;
                UI.Text(b.transform,label(i),0,size-2,size,26,16,UI.Ink,false,TextAlignmentOptions.Center).overflowMode=TextOverflowModes.Truncate;
            }
            Grow((count+cols-1)/cols*(h+6));
        }
        public void Table(string[] head,string[][] rows,int on)
        {
            EndRow();float a=width-200;for(int r=-1;r<rows.Length;r++){var c=r<0?head:rows[r];UI.Text(content,c[0],0,y,a,28,17,UI.Ink,r<0);UI.Text(content,c[1],a,y,92,28,17,on==1&&r<0?UI.Coral:UI.Ink,r<0||on==1,TextAlignmentOptions.TopRight);UI.Text(content,c[2],a+92,y,108,28,17,on==2&&r<0?UI.Coral:UI.Ink,r<0||on==2,TextAlignmentOptions.TopRight);y+=30;}Grow(0);
        }
    }
}
