using System;
using UnityEngine;
using TMPro;
#if UNITY_ANDROID && !UNITY_EDITOR
using TapSDK.Core;
using TapSDK.Login;
using TapSDK.Compliance;
#endif
using static PotatoLife.UI;
namespace PotatoLife
{
    // Fill these in before building the TapTap package; BuildAndroid refuses to build while any is empty.
    public static class TapConfig
    {
        // TapTap 开发者中心 → 你的游戏 → 游戏服务 → 应用配置
        public const string ClientId="";
        public const string ClientToken="";
        // Shown in the privacy policy; must match the developer identity registered with TapTap.
        public const string Developer="";
        public const string Email="";
    }
    // TapTap listing gate (Android only): privacy consent, then TapTap login and the national anti-addiction check, before the title page.
    // The SDK is initialised only after consent, as the TapSDK compliance guide requires.
    public partial class PotatoGame
    {
        enum Gate{Privacy,Declined,Login,Checking,Blocked,Open}
        const string PrivacyKey="PotatoPrivacyAgreed";
        // The smoke test sets this to walk the gate in the editor, where login and the check are simulated.
        public static bool PreviewGate;
        Gate gate=Gate.Open;string gateNote="";bool gateRetry,tapReady;
        readonly object gateLock=new object();int gateCode=-1;
#if POTATO_PREVIEW
        // Look-only phone build (-potato-preview-apk): no login, never for upload.
        bool GateNeeded=>PreviewGate;
#else
        bool GateNeeded=>Application.platform==RuntimePlatform.Android||PreviewGate;
#endif

        public void RestartGate(){PlayerPrefs.DeleteKey(PrivacyKey);GateStart();Render();}
        void GateStart(){if(!GateNeeded){gate=Gate.Open;return;}if(PlayerPrefs.GetInt(PrivacyKey,0)!=1){gate=Gate.Privacy;return;}gate=Gate.Login;TapInit();TapLogin(true);}
        // Compliance results can arrive from the native side at any time, including mid-game when the allowed time runs out.
        void GateUpdate(){int code;lock(gateLock){code=gateCode;gateCode=-1;}if(code>=0)GateResult(code);}
        void GateResult(int code)
        {
            if(code==500){gate=Gate.Open;gateNote="";Render();return;}
            if(State!=null)Save.Write(State);
            if(code==1000||code==1001){gate=Gate.Login;gateNote="登录已失效，请重新登录。";}
            else if(code==9002){gate=Gate.Login;gateNote="需要完成实名认证才能进入游戏。";}
            else if(code==1030){gate=Gate.Blocked;gateRetry=false;gateNote="根据国家防沉迷规定，未成年人只能在周五、周六、周日和法定节假日的 20:00—21:00 游戏。现在不在可游戏时段，进度已经保存。";}
            else if(code==1050){gate=Gate.Blocked;gateRetry=false;gateNote="今天的游戏时间已经用完，进度已经保存。";}
            else if(code==1100){gate=Gate.Blocked;gateRetry=false;gateNote="当前账号未达到本游戏的适龄要求。";}
            else{gate=Gate.Blocked;gateRetry=true;gateNote="网络异常或配置有误（"+code+"），请检查网络后重试。";}
            CloseModal();Render();
        }
        void DrawGate()
        {
            Picture(page,home,0,0,1600,1000);var c=Card(page,330,150,940,700,new Color(1,.98f,.93f,.97f)).transform;
            if(gate==Gate.Privacy||gate==Gate.Declined)
            {
                Text(c,"个人信息保护提示",56,48,828,52,36,Ink,true);
                Text(c,gate==Gate.Declined?"不同意隐私政策，就无法进行实名认证和防沉迷校验，游戏不能继续。你可以再看一看，或者退出游戏。":"欢迎来到《土豆子的港硕生活》。游戏存档只保存在你的设备上。为了遵守国家防沉迷规定，游戏会接入 TapSDK，用于 TapTap 登录和实名认证，TapSDK 会收集网络状态、设备型号、系统版本等信息。",56,122,828,170,24,Muted);
                Text(c,"请阅读完整的《隐私政策》。点「同意并继续」表示你已阅读并同意。",56,312,828,70,24,Ink);
                Button(c,"阅读《隐私政策》",56,404,828,58,()=>PrivacyView(false),Blue,true,"GatePolicy");
                Button(c,"同意并继续",56,482,828,64,()=>{PlayerPrefs.SetInt(PrivacyKey,1);PlayerPrefs.Save();gate=Gate.Login;TapInit();Render();},Coral,true,"GateAgree");
                Button(c,gate==Gate.Declined?"退出游戏":"不同意",56,566,828,58,()=>{if(gate==Gate.Declined)Application.Quit();else{gate=Gate.Declined;Render();}},Muted,true,"GateDecline");
                return;
            }
            Text(c,gate==Gate.Blocked?"暂时不能进入":"登录 TapTap",56,48,828,52,36,Ink,true);
            Text(c,gate==Gate.Checking?"正在确认身份……":gate==Gate.Blocked?gateNote:"根据国家防沉迷规定，进入游戏前需要用 TapTap 账号登录并完成实名认证。"+(gateNote!=""?"\n"+gateNote:""),56,130,828,250,25,gate==Gate.Blocked?Coral:Muted);
            if(gate==Gate.Login)Button(c,"用 TapTap 登录",56,420,828,66,()=>TapLogin(false),Coral,true,"GateLogin");
            if(gate==Gate.Blocked&&gateRetry)Button(c,"重试",56,420,828,66,()=>{gate=Gate.Login;TapLogin(false);},Coral,true,"GateRetry");
            if(gate==Gate.Blocked)Button(c,"退出游戏",56,506,828,58,Application.Quit,Muted,true,"GateQuit");
            Button(c,"隐私政策",56,600,200,48,()=>PrivacyView(false),Muted,true,"GatePolicySmall");
        }
        // The full policy, reachable from the gate and from Settings; Settings also offers withdrawing consent.
        void PrivacyView(bool withdraw)
        {
            var p=Modal("隐私政策","《土豆子的港硕生活》",1200,900);string text=Privacy.Text();
            var probe=Text(p,text,0,0,1100,10,21,Ink);probe.ForceMeshUpdate();float full=probe.preferredHeight+40;Destroy(probe.gameObject);
            var body=UI.Scroll(p,32,140,1136,640,full);Text(body,text,0,0,1100,full,21,Ink);
            if(withdraw)Button(p,"撤回同意并退出登录",32,806,520,60,()=>{PlayerPrefs.DeleteKey(PrivacyKey);PlayerPrefs.Save();TapLogout();CloseModal();Save.Write(State);gate=Gate.Privacy;gateNote="";Render();},Muted,true,"PrivacyWithdraw");
            Button(p,"关闭",withdraw?568:32,806,withdraw?600:1136,60,CloseModal,Coral,true,"PrivacyClose");
        }
        void LogoutToGate(){CloseModal();Save.Write(State);TapLogout();gate=Gate.Login;gateNote="已退出登录。";Render();}
#if UNITY_ANDROID && !UNITY_EDITOR
        void TapInit()
        {
            if(tapReady)return;tapReady=true;
            var core=new TapTapSdkOptions{clientId=TapConfig.ClientId,clientToken=TapConfig.ClientToken,region=TapTapRegionType.CN,screenOrientation=1,enableLog=Debug.isDebugBuild};
            TapTapSDK.Init(core,new TapTapSdkBaseOptions[]{new TapTapComplianceOption{showSwitchAccount=false,useAgeRange=false},new TapTapEventOptions{enableTapTapEvent=false}});
            TapTapCompliance.RegisterComplianceCallback((code,msg)=>{lock(gateLock)gateCode=code;});
        }
        // quiet: on launch, only resume an existing TapTap login; never pop the login page unasked.
        async void TapLogin(bool quiet)
        {
            gate=Gate.Checking;gateNote="";Render();
            try
            {
                var account=await TapTapLogin.Instance.GetCurrentTapAccount();
                if(account==null){if(quiet){gate=Gate.Login;Render();return;}account=await TapTapLogin.Instance.LoginWithScopes(new[]{TapTapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE});}
                TapTapCompliance.Startup(account.openId);
            }
            catch(Exception e){gate=Gate.Login;gateNote=e is OperationCanceledException?"登录已取消。":"登录没有完成，请再试一次。";Debug.LogWarning("TapTap login: "+e.Message);Render();}
        }
        void TapLogout(){if(!tapReady)return;TapTapCompliance.Exit();TapTapLogin.Instance.Logout();}
#else
        void TapInit(){tapReady=true;}
        void TapLogin(bool quiet){if(quiet){gate=Gate.Login;return;}gate=Gate.Checking;gateNote="";Render();lock(gateLock)gateCode=500;}
        void TapLogout(){}
#endif
    }
}
