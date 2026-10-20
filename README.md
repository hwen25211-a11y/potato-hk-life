# 土豆子的港硕生活

一款 Unity + C# 开发的中文单机放置陪伴游戏。你来选方向，土豆子自己出门，把香港的日常带回成照片、信件和小惊喜。

**v0.1.0 可玩原型**：完整春夏秋冬、16 次外出的学年纪念、4 种温暖结局。默认一次外出 30 秒，适合首次试玩和课堂演示；可切换到现实 30—120 分钟的慢生活模式。

![土豆子的小屋](docs/screenshots/02-home-guide.png)

## 下载试玩

[发行包与运行说明](https://github.com/hwen25211-a11y/potato-hk-life/releases)

- Mac：下载 Mac 试玩 ZIP，完整解压，打开 `土豆子的港硕生活.app`。
- Windows / 浏览器：下载 Browser 试玩 ZIP，安装 Python 3 后使用包内启动器，在浏览器访问 `http://127.0.0.1:8918`。
- GitHub 自动生成的 `Source code (zip)` 是源码，不含构建结果。试玩不需要安装 Unity。

## 怎么玩

第一次只教三件事：选今天的方向 → 送土豆子出门 → 回来收好手账并打开相册。

- **四种方向**：学习、游玩、打工、交朋友。约 60% 的行程跟随偏好，其余是温和的意外。
- **离线陪伴**：连续外出可积攒最多三份收获，不会因离开游戏而丢失照片或错过考核。关闭连续外出后，每趟由玩家主动送出门。
- **照片与朋友**：12 个不同地点的照片、4 张节日纪念、3 位朋友各 3 段来信；照片保留拍摄时的服装。
- **小屋与行囊**：5 件装饰，最多摆放 4 件；便当、相机和雨伞可反复携带。金币仅在主动购买时花费，没有租金或自动扣款。
- **轻松互动**：摸摸土豆子、收任务奖励、装饰房间。体力低会在家自动恢复，没有挂科、效率惩罚或坏结局。
- **四季纪念**：每 4 次收获进入下一季；第 2/6/10/14 次解锁节日故事与衣服。16 次后翻毕业手账，按这一年的生活形成可选方向，结业后仍能继续陪伴。

主界面常驻体力、心情和金币，智慧、魅力与陪伴放在资料卡。所有时间与收益数值集中在 `Tuning` / `World`，用于当前原型试玩。

节日按春夏秋冬主题解锁，不采用现实年份日期或真实天气；没有限时错过的惩罚。

## 指定音乐

已接入用户提供的三首原始 MP3，切换时淡入淡出：

| 场景 | 曲目 | 资源 |
| --- | --- | --- |
| 小屋白天 | Mellow Lounge | `Audio/Day.mp3` |
| 外出与节日 | Winding Path | `Audio/Travel.mp3` |
| 小屋夜晚（设备时间 20:00—06:00） | Midnight Rhyme | `Audio/Night.mp3` |

背景音乐和提示音可分别调音量。原始文件名、媒体信息和 SHA-256 在 `Licenses/UserMusic.json`，来源说明在 `Licenses/Music.txt`。互动音效为独立原创合成，脚本不会覆盖指定音乐。

## 开发

用 Unity Hub 打开仓库根目录，使用 **Unity 2022.3.62f3c1**，场景为 `Assets/Scenes/PotatoHome.unity`。依赖 uGUI 1.0.0、TextMeshPro 3.0.6，中文字体已内置。

```sh
./Tools/test-rules.sh
./Tools/build.sh mac
./Tools/build.sh web
python3 Tools/package.py
```

Unity 编辑器菜单“土豆子的港硕生活”也提供构建入口。构建脚本支持 `UNITY_BIN`，规则测试支持 `UNITY_ROOT`。

| 文件 | 职责 |
| --- | --- |
| `PotatoModel.cs` | 可序列化状态、地点、物品和调节参数 |
| `PotatoRules.cs` | 可注入时间的外出、离线、收获、朋友与学年逻辑 |
| `PotatoSave.cs` | 独立版本存档、校验与备份恢复 |
| `PotatoGame.cs` / `PotatoPages.cs` | 常驻场景、局部状态刷新与各页面 |
| `CozyUI.cs` | uGUI/TMP 控件、角色图集、平滑条与轻量动画 |
| `PotatoSound.cs` | 指定配乐切换、淡入淡出、分离音量 |
| `Assets/Editor` | 构建、纯规则与界面交互检查 |

存档键和应用标识均与旧《港硕这一年》分离。旧项目没有迁移或覆盖；复用审核见 [docs/REUSE-AUDIT.txt](docs/REUSE-AUDIT.txt)，实际验证见 [docs/VALIDATION.txt](docs/VALIDATION.txt)。

## 资源与协作

新美术统一使用土豆角色与手绘场景，生成提示词见 `docs/ART-PROVENANCE.txt`。沿用的 Noto Sans SC 派生字体遵循 SIL OFL；TMP 保留自带许可。音乐版权归原权利人，不因仓库公开而变成 CC0。项目未额外指定整个游戏的开源许可证。

提交源码与 `.meta`，不提交 `Library`、`Build`、`release`、存档、凭据及 `LocalReferences`。新需求原文和旧项目的私有设计文档不进入公开仓库或发行包。协作请通过功能分支和 Pull Request，避免同时编辑同一大资源。
