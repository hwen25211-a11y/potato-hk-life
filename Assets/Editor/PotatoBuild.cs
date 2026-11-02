using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.TextCore.LowLevel;
using TMPro;
namespace PotatoLife.Editor
{
    public static class PotatoBuild
    {
        public const string Version="0.2.2";
        public const string Scene="Assets/Scenes/PotatoHome.unity";
        [MenuItem("土豆子的港硕生活/准备场景与字体")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles("Assets/Resources/Art","*.png",SearchOption.AllDirectories))
            {var a=AssetImporter.GetAtPath(path) as TextureImporter;if(a==null)continue;a.textureType=TextureImporterType.Default;if(path.Contains("/Life/"))a.npotScale=TextureImporterNPOTScale.None;a.maxTextureSize=2048;a.mipmapEnabled=false;a.filterMode=FilterMode.Bilinear;a.alphaIsTransparency=true;a.isReadable=path.Contains("PotatoAtlas")||path.Contains("Costumes")||path.Contains("Items");a.textureCompression=a.isReadable?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;a.SaveAndReimport();}
            // Real photos keep their own size and aspect ratio (no power-of-two rescale), compressed, without mipmaps.
            foreach(string path in Directory.GetFiles("Assets/Resources/Photos","*.jpg"))
            {var a=AssetImporter.GetAtPath(path) as TextureImporter;if(a==null)continue;a.textureType=TextureImporterType.Default;a.npotScale=TextureImporterNPOTScale.None;a.maxTextureSize=2048;a.mipmapEnabled=false;a.filterMode=FilterMode.Bilinear;a.wrapMode=TextureWrapMode.Clamp;a.alphaSource=TextureImporterAlphaSource.None;a.isReadable=false;a.textureCompression=TextureImporterCompression.CompressedHQ;a.SaveAndReimport();}
            // The fee poster must stay readable: full size, with mipmaps for the whole-poster view.
            {var a=AssetImporter.GetAtPath("Assets/Resources/Hospital/FeePoster.jpg") as TextureImporter;if(a!=null){a.textureType=TextureImporterType.Default;a.npotScale=TextureImporterNPOTScale.None;a.maxTextureSize=8192;a.mipmapEnabled=true;a.filterMode=FilterMode.Trilinear;a.wrapMode=TextureWrapMode.Clamp;a.alphaSource=TextureImporterAlphaSource.None;a.isReadable=false;a.textureCompression=TextureImporterCompression.CompressedHQ;a.SaveAndReimport();}}
            foreach(string path in Directory.GetFiles("Assets/Resources/Audio").Where(p=>p.EndsWith(".wav")||p.EndsWith(".mp3")||p.EndsWith(".ogg")))
            {var a=AssetImporter.GetAtPath(path) as AudioImporter;if(a==null)continue;var settings=a.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;settings.loadType=path.EndsWith(".mp3")?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;a.defaultSampleSettings=settings;a.SaveAndReimport();}
            const string fontPath="Assets/Resources/Fonts/Potato SDF.asset";var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(font==null){font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/NotebookSans.ttf"),40,5,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="Potato SDF";font.isMultiAtlasTexturesEnabled=true;AssetDatabase.CreateAsset(font,fontPath);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var t in font.atlasTextures)AssetDatabase.AddObjectToAsset(t,font);}
            font.ClearFontAssetData(true);string chars=new string(string.Concat(Directory.GetFiles("Assets/Scripts","*.cs").Select(File.ReadAllText)).Where(c=>c>=32).Distinct().ToArray());string missing;font.TryAddCharacters(chars,out missing);if(!string.IsNullOrEmpty(missing))Debug.LogWarning("FONT_MISSING "+missing);foreach(var t in font.atlasTextures)if(!AssetDatabase.Contains(t))AssetDatabase.AddObjectToAsset(t,font);EditorUtility.SetDirty(font);
            if(!File.Exists(Scene)){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Potato Life Runtime");EditorSceneManager.SaveScene(scene,Scene);}EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            PlayerSettings.companyName="Potato Notebook";PlayerSettings.productName="土豆子的港硕生活";PlayerSettings.bundleVersion=Version;PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.PotatoNotebook.PotatoHKLife");
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Gamma;QualitySettings.antiAliasing=0;QualitySettings.vSyncCount=0;
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.template="PROJECT:Potato";PlayerSettings.WebGL.memorySize=512;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);AssetDatabase.SaveAssets();Debug.Log("POTATO_PREPARE_OK");
        }
        [MenuItem("土豆子的港硕生活/构建 Mac")]
        public static void BuildMac(){Prepare();Build(BuildTarget.StandaloneOSX,"Build/macOS-v"+Version+"/土豆子的港硕生活.app");}
        [MenuItem("土豆子的港硕生活/构建 WebGL")]
        public static void BuildWeb(){Prepare();Build(BuildTarget.WebGL,"Build/WebGL");}
        // TapTap package. Signing key lives outside the repo in ../PotatoHK-签名 (password on line 3 of 密码.txt).
        // Without TapConfig filled in, only a test build is allowed: -potato-test-apk, never for upload.
        [MenuItem("土豆子的港硕生活/构建 Android（TapTap）")]
        public static void BuildAndroid()
        {
            bool preview=Environment.GetCommandLineArgs().Contains("-potato-preview-apk"),test=preview||Environment.GetCommandLineArgs().Contains("-potato-test-apk");
            // The preview define skips the TapTap gate; it is set only for this build and cleared again below.
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android,preview?"POTATO_PREVIEW":"");if(preview)AssetDatabase.Refresh();
            if(!test&&(TapConfig.ClientId==""||TapConfig.ClientToken==""||TapConfig.Developer==""||TapConfig.Email==""))throw new Exception("先在 Assets/Scripts/PotatoTapGate.cs 的 TapConfig 填好 ClientId、ClientToken、开发者名称和联系邮箱");
            Prepare();var android=NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android,"com.PotatoNotebook.PotatoHKLife");PlayerSettings.Android.bundleVersionCode=202;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel23;PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)34;
            PlayerSettings.SetScriptingBackend(android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.allowedAutorotateToPortrait=PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.Android.renderOutsideSafeArea=false;PlayerSettings.Android.forceInternetPermission=true;PlayerSettings.Android.forceSDCardPermission=false;PlayerSettings.Android.preferredInstallLocation=AndroidPreferredInstallLocation.Auto;
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icon/AppIcon.png");if(icon!=null)PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
            string dir=Path.GetFullPath("../PotatoHK-签名");string store=Path.Combine(dir,"potato-hk.keystore");
            if(File.Exists(store)){string pass=File.ReadAllLines(Path.Combine(dir,"密码.txt"))[2].Split('：').Last().Trim();PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=store;PlayerSettings.Android.keystorePass=pass;PlayerSettings.Android.keyaliasName="potato";PlayerSettings.Android.keyaliasPass=pass;}
            else if(!test)throw new Exception("找不到签名密钥："+store);
            EditorUserBuildSettings.buildAppBundle=false;
            // TapSDK's native libraries come through EDM4U, which writes them into mainTemplate.gradle.
            var resolver=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GooglePlayServices.PlayServicesResolver")).FirstOrDefault(t=>t!=null);
            var resolve=resolver?.GetMethod("ResolveSync",new[]{typeof(bool)});if(resolve==null)throw new Exception("EDM4U Android Resolver 不在");if(!(bool)resolve.Invoke(null,new object[]{true}))throw new Exception("EDM4U 没能解析 TapSDK 的安卓依赖");
            try{Build(BuildTarget.Android,"Build/Android/PotatoHK-Android-v"+Version+(preview?"-preview":test?"-test":"")+".apk");}
            finally{if(preview)PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android,"");}
        }
        static void Build(BuildTarget target,string path){Directory.CreateDirectory(Path.GetDirectoryName(path));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=path,target=target,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);Debug.Log("POTATO_BUILD_OK "+target+" "+report.summary.totalSize);}
    }
}
