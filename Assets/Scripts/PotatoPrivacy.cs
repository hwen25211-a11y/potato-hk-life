namespace PotatoLife
{
    // The one copy of the privacy policy: shown in game, and Tools/privacy_html.py turns it into the page the TapTap listing links to.
    public static class Privacy
    {
        public const string Updated="2026 年 11 月 2 日";
        public static string Text()=>Body.Replace("{开发者}",TapConfig.Developer==""?"（开发者名称待填写）":TapConfig.Developer).Replace("{邮箱}",TapConfig.Email==""?"（联系邮箱待填写）":TapConfig.Email).Replace("{日期}",Updated);
        const string Body=@"《土豆子的港硕生活》隐私政策

更新日期：{日期}
生效日期：{日期}
开发者：{开发者}（以下简称「我们」）
联系邮箱：{邮箱}

《土豆子的港硕生活》是一款单机养成游戏。我们非常重视你的个人信息和隐私。请在使用前仔细阅读本政策；未成年人请在监护人陪同下阅读。

一、游戏本身保存的信息
游戏没有自己的服务器。土豆子的进度、金币、宠物、手账、声音设置等存档只保存在你的设备上，我们收不到，也不会上传。卸载游戏会删除这些存档。

二、第三方 SDK
为遵守国家关于防止未成年人沉迷网络游戏的规定，游戏接入了 TapSDK，用于 TapTap 登录、实名认证和防沉迷。只有在你同意本政策之后，游戏才会初始化 TapSDK。
· SDK 名称：TapSDK（登录、合规认证模块）
· 提供方：易玩（上海）网络科技有限公司
· 使用目的：TapTap 登录、实名认证与防沉迷
· 收集的信息：TapTap 账号标识（如 openId）、实名认证结果与年龄段、网络状态、WiFi 信息、设备版本、系统版本、设备型号、操作系统等
· 隐私政策：https://developer.taptap.cn/docs/sdk/start/agreement/
这些信息由 TapSDK 按照其隐私政策处理，我们自己不保存、不使用。游戏关闭了 TapSDK 的数据统计功能。

三、系统权限
游戏只使用网络权限（访问网络、查看网络状态），用于登录和防沉迷校验。游戏不使用位置、通讯录、相机、麦克风、存储等权限。

四、未成年人保护
根据国家规定，未成年人只能在周五、周六、周日和法定节假日的 20:00—21:00 游戏，每次 1 小时。游戏不含任何付费内容。

五、你的权利
· 你可以随时在游戏的「设置」里查看本政策、退出 TapTap 登录，或撤回对本政策的同意。撤回后，游戏会回到隐私提示页，不再使用 TapSDK。
· 卸载游戏即删除全部本地存档。
· 你的 TapTap 账号信息由 TapTap 管理，可以在 TapTap 应用内查询、更正或注销。
· 对本政策有任何疑问、意见或投诉，请发邮件联系我们，我们会在 15 个工作日内答复。

六、政策更新
本政策如有变化，我们会在游戏内提示，并再次征得你的同意。";
    }
}
