using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
namespace PotatoLife
{
    public static class UI
    {
        public static TMP_FontAsset Font;
        public static Color Cream=Hex("FFF8EC"),Ink=Hex("34556A"),Blue=Hex("3A7CA5"),Mango=Hex("F2B33D"),Coral=Hex("EE6C4D"),Mint=Hex("6BBF8E"),Lilac=Hex("A38ACB"),Pale=Hex("E9F2EF"),Muted=Hex("748A8D");
        public static readonly Color[] Colors={Blue,Mint,Mango,Coral};
        static Sprite round;static Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        public static Color Hex(string h){Color c;ColorUtility.TryParseHtmlString("#"+h,out c);return c;}
        public static RectTransform Rect(string name,Transform p,float x,float y,float w,float h)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        static Sprite Rounded
        {
            get{if(round!=null)return round;var t=new Texture2D(64,64,TextureFormat.RGBA32,false);var p=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Math.Max(0,Math.Max(14-x,x-49)),dy=Math.Max(0,Math.Max(14-y,y-49));p[y*64+x]=new Color(1,1,1,Mathf.Clamp01(15-Mathf.Sqrt(dx*dx+dy*dy)));}t.SetPixels(p);t.Apply();round=Sprite.Create(t,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));return round;}
        }
        public static Image Card(Transform p,float x,float y,float w,float h,Color color,string name="Card")
        {var r=Rect(name,p,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=Rounded;im.type=Image.Type.Sliced;im.color=color;im.raycastTarget=false;return im;}
        public static TextMeshProUGUI Text(Transform p,string content,float x,float y,float w,float h,float size=22,Color? color=null,bool bold=false,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
        {var r=Rect("Text",p,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=Font;t.text=content;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=Math.Max(16,size*.85f);t.fontSizeMax=size;t.color=color??Ink;t.alignment=align;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Ellipsis;t.enableWordWrapping=true;t.lineSpacing=4;return t;}
        public static Button Button(Transform p,string label,float x,float y,float w,float h,Action action,Color? color=null,bool enabled=true,string id=null)
        {var bg=Card(p,x,y,w,h,color??Blue,id??label);bg.raycastTarget=true;var b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;b.interactable=enabled;Text(bg.transform,label,12,2,w-24,h-4,21,Color.white,true,TextAlignmentOptions.Midline);if(action!=null)b.onClick.AddListener(()=>{Sound.Fx("bubble");action();});bg.gameObject.AddComponent<HoverPop>();return b;}
        public static RawImage Picture(Transform p,Texture tex,float x,float y,float w,float h,int quadrant=-1)
        {var r=Rect("Scene",p,x,y,w,h);var im=r.gameObject.AddComponent<RawImage>();im.texture=tex;im.raycastTarget=false;if(quadrant>=0)im.uvRect=new Rect((quadrant%2)*.5f,1-(quadrant/2+1)*.5f,.5f,.5f);return im;}
        static Sprite Frame(string path,int index,int cols,int rows)
        {
            string key=path+index;if(sprites.ContainsKey(key))return sprites[key];var t=Resources.Load<Texture2D>(path);if(t==null)return null;
            int cw=t.width/cols,ch=t.height/rows,sx=index%cols*cw,sy=t.height-(index/cols+1)*ch;
            // The lower row was drawn with uneven gutters: the waving hand and
            // cushion cross the nominal grid. Slice at the actual empty gutters.
            if(path=="Art/PotatoAtlas"&&index>=4){float[] edges={0,416f/1536f,800f/1536f,.75f,1};int col=index-4;sx=Mathf.RoundToInt(edges[col]*t.width);cw=Mathf.RoundToInt(edges[col+1]*t.width)-sx;}
            var colors=t.GetPixels32();int minX=cw,minY=ch,maxX=0,maxY=0;
            for(int y=0;y<ch;y++)for(int x=0;x<cw;x++)if(colors[(sy+y)*t.width+sx+x].a>40){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
            if(minX>maxX){minX=minY=0;maxX=cw-1;maxY=ch-1;}int margin=3;minX=Math.Max(0,minX-margin);minY=Math.Max(0,minY-margin);maxX=Math.Min(cw-1,maxX+margin);maxY=Math.Min(ch-1,maxY+margin);
            var s=Sprite.Create(t,new Rect(sx+minX,sy+minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,.5f));sprites[key]=s;return s;
        }
        public static RectTransform Actor(Transform p,int pose,int outfit,float x,float y,float w,float h,bool animate=true)
        {var r=Rect("Potato",p,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=outfit>0?Frame("Art/Costumes",outfit-1,2,2):Frame("Art/PotatoAtlas",pose,4,2);im.preserveAspect=true;im.raycastTarget=false;if(animate)r.gameObject.AddComponent<PotatoMotion>().pose=pose;return r;}
        // Shop goods: ids 0-19 in the 5x4 Items.png, 21-28 (港硕必备) in the 4x2 Items2.png; 20 is drawn in code.
        public static Image Item(Transform p,int id,float x,float y,float w,float h)
        {var r=Rect("Item"+id,p,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=id>=21?Frame("Art/Items2",id-21,4,2):Frame("Art/Items",id,5,4);im.preserveAspect=true;im.raycastTarget=false;return im;}
        public static TMP_InputField Field(Transform p,string placeholder,float x,float y,float w,float h,float size,string id)
        {
            var r=Rect(id,p,x,y,w,h);r.gameObject.SetActive(false);var bg=r.gameObject.AddComponent<Image>();bg.sprite=Rounded;bg.type=Image.Type.Sliced;bg.color=Color.white;
            var area=Rect("Text Area",r,16,4,w-32,h-8);area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI Line(string name,string content,Color color)
            {var t=Rect(name,area,0,0,w-32,h-8).gameObject.AddComponent<TextMeshProUGUI>();t.font=Font;t.text=content;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.MidlineLeft;t.enableWordWrapping=false;t.raycastTarget=false;return t;}
            var hint=Line("Placeholder",placeholder,Muted);var text=Line("Text","",Ink);
            var input=r.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=bg;input.textViewport=area;input.textComponent=text;input.placeholder=hint;input.fontAsset=Font;input.pointSize=size;input.lineType=TMP_InputField.LineType.SingleLine;input.caretColor=Ink;input.selectionColor=new Color(.42f,.75f,.56f,.35f);
            r.gameObject.SetActive(true);return input;
        }
        // Frame strips made with agent-sprite-forge (sources in ArtSource/forge): four equal cells, left to right.
        static Dictionary<string,Sprite[]> strips=new Dictionary<string,Sprite[]>();
        static Sprite[] Strip(string path,int count)
        {
            if(strips.ContainsKey(path))return strips[path];var t=Resources.Load<Texture2D>(path);Sprite[] s=null;
            if(t!=null){s=new Sprite[count];int cw=t.width/count;for(int i=0;i<count;i++)s[i]=Sprite.Create(t,new Rect(i*cw,0,cw,t.height),new Vector2(.5f,.5f));}
            strips[path]=s;return s;
        }
        // Keeps the static pose's height, so animated and still potatoes read the same size.
        public static RectTransform Animated(Transform p,string path,float fps,float x,float y,float w,float h)
        {
            var frames=Strip(path,4);if(frames==null)return null;float fw=h*frames[0].rect.width/frames[0].rect.height;
            var r=Rect("Potato",p,x+(w-fw)/2,y,fw,h);var im=r.gameObject.AddComponent<Image>();im.sprite=frames[0];im.preserveAspect=true;im.raycastTarget=false;
            var a=r.gameObject.AddComponent<FramePlayer>();a.frames=frames;a.fps=fps;return r;
        }
        // The strips show the plain potato with its red backpack; costumes keep their still pose.
        public static RectTransform Wave(Transform p,int pose,int outfit,float x,float y,float w,float h)
        {return (outfit==0?Animated(p,"Art/PotatoWave",6,x,y,w,h):null)??Actor(p,pose,outfit,x,y,w,h);}
        public static Image Bar(Transform p,float x,float y,float w,float h,float value,Color color)
        {Card(p,x,y,w,h,Pale);var fill=Card(p,x,y,w*Life.Cap(value)/100f,h,color);fill.gameObject.AddComponent<SmoothBar>().fullWidth=w;return fill;}
        public static RectTransform Scroll(Transform p,float x,float y,float w,float h,float full)
        {var root=Rect("Scroll",p,x,y,w,h);var sr=root.gameObject.AddComponent<ScrollRect>();var v=Rect("Viewport",root,0,0,w,h);v.gameObject.AddComponent<RectMask2D>();var im=v.gameObject.AddComponent<Image>();im.color=new Color(1,1,1,.002f);var c=Rect("Content",v,0,0,w-16,Math.Max(h,full));sr.viewport=v;sr.content=c;sr.horizontal=false;sr.movementType=ScrollRect.MovementType.Clamped;sr.scrollSensitivity=45;
            if(full>h){var rail=Card(root,w-12,0,12,h,Pale);rail.raycastTarget=true;var thumb=Card(rail.transform,0,0,12,h,Mint);thumb.raycastTarget=true;thumb.rectTransform.anchorMin=Vector2.zero;thumb.rectTransform.anchorMax=Vector2.one;thumb.rectTransform.offsetMin=thumb.rectTransform.offsetMax=Vector2.zero;var sb=rail.gameObject.AddComponent<Scrollbar>();sb.handleRect=thumb.rectTransform;sb.direction=Scrollbar.Direction.BottomToTop;sb.targetGraphic=thumb;sr.verticalScrollbar=sb;}return c;}
    }
    public class HoverPop:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        void Awake(){var r=(RectTransform)transform;var center=new Vector2(.5f,.5f);r.anchoredPosition+=Vector2.Scale(r.rect.size,center-r.pivot);r.pivot=center;}
        void OnDisable(){over=false;transform.localScale=Vector3.one;}
        bool over;public void OnPointerEnter(PointerEventData e){over=GetComponent<Button>().interactable;}public void OnPointerExit(PointerEventData e){over=false;}
        void Update(){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(over&&GetComponent<Button>().interactable?1.025f:1),Time.unscaledDeltaTime*14);}
    }
    public class PotatoMotion:MonoBehaviour
    {
        public int pose;Vector2 origin;RectTransform r;float start;
        void Start(){r=(RectTransform)transform;origin=r.anchoredPosition;start=Time.unscaledTime;}
        void Update(){float t=Time.unscaledTime-start;float wave=Mathf.Sin(t*(pose==5?1.6f:3));r.anchoredPosition=origin+new Vector2(0,wave*(pose==5?2:5));r.localScale=new Vector3(1+wave*.018f,1-wave*.018f,1);r.localRotation=Quaternion.Euler(0,0,pose==3?Mathf.Sin(t*3)*2:Mathf.Sin(t*1.3f)*1.2f);}
    }
    // Lets the player drag a decoration around the room; it stays inside the room picture.
    public class DragItem:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public Action<Vector2> dropped;RectTransform r;int index;float scale=1;
        public void OnBeginDrag(PointerEventData e){r=(RectTransform)transform;index=r.GetSiblingIndex();var c=GetComponentInParent<Canvas>();scale=c==null?1:c.scaleFactor;r.SetAsLastSibling();r.localScale=Vector3.one*1.06f;}
        public void OnDrag(PointerEventData e)
        {if(r==null)return;var p=(RectTransform)r.parent;var a=r.anchoredPosition+e.delta/scale;a.x=Mathf.Clamp(a.x,0,p.rect.width-r.rect.width);a.y=Mathf.Clamp(a.y,-(p.rect.height-r.rect.height),0);r.anchoredPosition=a;}
        public void OnEndDrag(PointerEventData e){if(r==null)return;r.localScale=Vector3.one;r.SetSiblingIndex(index);if(dropped!=null)dropped(r.anchoredPosition);}
    }
    public class FramePlayer:MonoBehaviour
    {
        public Sprite[] frames;public float fps=8;Image im;float start;
        void Start(){im=GetComponent<Image>();start=Time.unscaledTime;}
        void Update(){if(frames!=null&&frames.Length>0)im.sprite=frames[(int)((Time.unscaledTime-start)*fps)%frames.Length];}
    }
    public class WalkOut:MonoBehaviour
    {
        public Action done;public float distance=400,duration=1.6f;Vector2 from;RectTransform r;CanvasGroup group;float t;
        void Start(){r=(RectTransform)transform;from=r.anchoredPosition;group=gameObject.AddComponent<CanvasGroup>();}
        void Update()
        {
            t+=Time.unscaledDeltaTime;float k=Mathf.Clamp01(t/duration);r.anchoredPosition=from+new Vector2(distance*k,Mathf.Abs(Mathf.Sin(t*14.1f))*6);group.alpha=1-Mathf.Clamp01((k-.7f)/.3f);
            if(k>=1&&done!=null){var d=done;done=null;d();}
        }
    }
    public class SmoothBar:MonoBehaviour
    {
        public float target=0,fullWidth;bool initialized;
        void Update(){var r=(RectTransform)transform;if(!initialized){target=r.sizeDelta.x;initialized=true;}r.sizeDelta=new Vector2(Mathf.Lerp(r.sizeDelta.x,target,Time.unscaledDeltaTime*8),r.sizeDelta.y);}
        public void Set(float value){initialized=true;target=fullWidth*Life.Cap(value)/100;}
    }
    public class FloatingNote:MonoBehaviour
    {
        float life;Vector2 at;RectTransform r;CanvasGroup group;
        void Start(){r=(RectTransform)transform;at=r.anchoredPosition;group=gameObject.AddComponent<CanvasGroup>();}
        void Update(){life+=Time.unscaledDeltaTime;r.anchoredPosition=at+Vector2.up*life*45;group.alpha=1-Mathf.Max(0,life-1);if(life>2)Destroy(gameObject);}
    }
    public class PageFade:MonoBehaviour
    {CanvasGroup group;float t;public void Finish(){group.alpha=1;enabled=false;Destroy(this);}void Awake(){group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;}void Update(){t+=Time.unscaledDeltaTime;group.alpha=Mathf.Clamp01(t/.2f);if(t>.2f)Finish();}}
}
