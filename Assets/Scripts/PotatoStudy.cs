using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static PotatoLife.UI;
namespace PotatoLife
{
    // Companion study: the potato naps in the room, studies beside you while the timer runs,
    // wakes up to praise you when tapped, and asks you to stand up every 45 minutes.
    public partial class PotatoGame
    {
        TextMeshProUGUI studyClock,studyNext,studyStats,studySpeech;
        void ClearStudy(){studyClock=studyNext=studyStats=studySpeech=null;}
        void DrawStudy()
        {
            var scene=Card(page,44,176,1040,704,Color.white).rectTransform;Picture(scene,home,0,0,1040,704);DrawDecorations(scene);
            StudyPotato(scene,State.studyBreak?-1:State.studyRunning?1:5);
            var tag=Card(scene,21,20,353,93,new Color(1,.98f,.93f,.95f)).transform;Text(tag,"陪伴学习",17,12,322,32,23,Ink,true);
            Text(tag,State.studyRunning?"一起专注中 · 点点它会夸你":State.studyBreak?"休息时间 · 看看窗外":"土豆子在打盹 · 点点它",17,55,322,28,17,Muted);
            var bubble=Card(scene,277,157,610,99,new Color(1,1,1,.96f)).transform;
            studySpeech=Text(bubble,State.studyRunning?"我在旁边一起看书，你专心就好。":State.studyBreak?"休息一下！看看窗外的海，眼睛会说谢谢你。":"呼……我先眯一会儿。点点我，或者开始陪伴学习吧。",21,17,568,67,23,Ink);
            var p=Card(page,1110,176,444,704,Color.white).transform;Card(p,155,-7,134,24,Hex("FFDF91"));
            Text(p,"陪伴学习",25,27,393,45,29,Ink,true);Text(p,"土豆子陪你专注。每满 45 分钟，它会提醒你起身走走、看看窗外。",25,80,393,58,19,Muted);
            studyClock=Text(p,"",25,145,393,86,62,Blue,true,TextAlignmentOptions.Center);studyNext=Text(p,"",25,235,393,30,19,Muted,false,TextAlignmentOptions.Center);
            string label=State.studyBreak?"休息好了，继续陪伴":State.studyRunning?"暂停一下":State.studySession>0?"继续陪伴":"开始陪伴学习";
            Button(p,label,25,280,393,56,StudyToggle,Coral,true,"StudyToggle");
            Button(p,"结束这次陪伴",25,348,393,46,StudyFinish,Blue,State.studySession>0||State.studyRunning||State.studyBreak,"StudyEnd");
            studyStats=Text(p,"",25,408,393,62,18,Ink);
            Text(p,"我的待办",25,486,240,36,23,Ink,true);Button(p,"管理清单",284,486,135,36,QuestDialog,Mint,true,"StudyTodos");
            var open=State.todos.Where(t=>!t.done).Take(4).ToArray();
            if(open.Length==0)Text(p,"还没有待办。点「管理清单」写下今天想做的小事。",25,532,393,60,18,Muted);
            for(int i=0;i<open.Length;i++){int id=open[i].id;var c=Card(p,25,530+i*42,393,36,Pale).transform;Text(c,open[i].text,12,4,296,28,18,Ink);Button(c,"完成",316,3,70,30,()=>FinishTodo(id),Mint,true,"StudyTodo"+id);}
            RefreshStudy();
        }
        // pose -1 waves (just woke up or break time); 1 reads beside you; 5 naps on its cushion.
        // At home it studies in its plain look: costumes only have a standing pose.
        RectTransform StudyPotato(Transform scene,int pose)
        {
            var actor=pose<0?Wave(scene,4,0,361,281,318,357):Actor(scene,pose,0,361,281,318,357);
            var image=actor.GetComponent<Image>();image.raycastTarget=true;var button=actor.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(StudyPraise);actor.name="StudyPotato";return actor;
        }
        void RefreshStudy()
        {
            if(studyClock==null)return;long now=Save.Now;int sec=Life.StudySeconds(State,now);
            studyClock.text=(sec/3600).ToString("00")+":"+(sec/60%60).ToString("00")+":"+(sec%60).ToString("00");
            studyNext.text=State.studyBreak?"休息时间 · 起身活动一下吧":State.studyRunning?"距离下次起身休息 "+((Life.StudyToBreak(State,now)+59)/60)+" 分钟":"计时已暂停";
            studyStats.text="今日专注 "+(State.dailyStudy/60)+" 分钟 · 奖励 "+State.studyBlocks+" / "+Tuning.StudyDailyBlocks+"\n每满 15 分钟：金币 +"+Life.StudyBlockCoins(State)+" · 智慧 +1 · 陪伴 +1";
        }
        void StudyToggle()
        {
            long now=Save.Now;if(State.studyBreak)Life.StudyContinue(State,now);else if(State.studyRunning)Life.StudyPause(State,now);else Life.StudyStart(State,now);
            Sound.Fx("page");Save.Write(State);Render();
        }
        void StudyFinish(){int total=Life.StudyEnd(State,Save.Now);Save.Write(State);Render();Float(total>=60?"这次一起专注了 "+(total/60)+" 分钟，辛苦啦！":"下次再一起专注吧。",Coral);}
        void StudyPraise()
        {
            int before=State.coins;string note=Life.Pet(State);string line=World.Praise[State.petTotal%World.Praise.Length];Save.Write(State);Sound.Fx(State.coins>before?"coin":"company");RefreshStats();
            if(!State.studyRunning&&!State.studyBreak)StartCoroutine(WakeUp(line));else if(studySpeech!=null)studySpeech.text=line;
            Float(note,Coral);
        }
        IEnumerator WakeUp(string line)
        {
            var sleeping=page.GetComponentsInChildren<RectTransform>().FirstOrDefault(r=>r.name=="StudyPotato");if(sleeping==null)yield break;
            var scene=sleeping.parent;Destroy(sleeping.gameObject);StudyPotato(scene,-1);if(studySpeech!=null)studySpeech.text="（揉揉眼睛）你来啦！"+line;
            yield return new WaitForSecondsRealtime(4f);
            if(enabled&&tab=="陪伴"&&modal==null&&!State.studyRunning&&!State.studyBreak&&studySpeech!=null)Render();
        }
        void StudyBreakDialog()
        {
            Sound.Fx("mail");var p=Modal("起身活动一下吧","已经一起专注 45 分钟啦",900,600);Wave(p,4,0,40,150,230,280);
            Text(p,"站起来伸个懒腰、喝口水，\n再看看窗外远处的山和海，让眼睛休息一会儿。\n\n活动好了，点下面的按钮，土豆子会继续陪你计时。",300,160,560,260,24,Ink);
            Button(p,"我活动好了，继续陪伴",32,452,836,58,()=>{Life.StudyContinue(State,Save.Now);Save.Write(State);Render();Sound.Fx("page");},Coral,true,"StudyContinue");
            Button(p,"今天先到这里",32,524,836,46,()=>{CloseModal();StudyFinish();},Blue,true,"StudyStop");
        }
        void FinishTodo(int id)
        {
            int coins=Life.CompleteTodo(State,id);if(coins<0)return;bool listOpen=modal!=null;Save.Write(State);Sound.Fx(coins>0?"coin":"company");
            if(listOpen)QuestDialog();else Render();RefreshStats();
            Float(coins>0?"完成啦！金币 +"+coins+" · 心情 +2 · 智慧 +1":"完成啦！今天的待办奖励领满了，也替你开心。",Mango);
        }
    }
}
