"""Recipient-ready Mac and browser releases; private references never enter archives."""
from pathlib import Path
import zipfile,hashlib,json,stat,subprocess
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'release';OUT.mkdir(exist_ok=True)
VERSION='0.1.1'
README='''《土豆子的港硕生活》0.1.1 试玩版

默认演示模式：一次外出30秒。选方向，送土豆子出门，回来收照片和信件。
设置里可换成现实30—120分钟的慢生活。离线最多积攒3份收获。
没有挂科或坏结局；金币仅在主动购买时花费。16次收获可看完整学年纪念。

Mac包：完整解压后打开「土豆子的港硕生活.app」。无需安装Unity。
Browser包：需要Python 3；Windows双击「浏览器试玩-Windows.bat」，
Mac双击「浏览器试玩.command」，或运行 python3 Tools/preview.py。
浏览器访问 http://127.0.0.1:8918，保持启动器窗口打开；不要直接双击HTML。

游戏自动保存。继续游戏可收离线收获，浏览器存档跟随浏览器与访问地址。
六首背景音乐按场景轮播，设置里可以查看曲名、切换下一首，分别调整音乐和音效。
外出时先停止小屋音乐，再播放活动音乐，返回后恢复小屋歌单。
右侧「今日待办」集中显示任务进度；每日可选，没完成也不扣资源。
更新本版可继续现有进度，新游戏不会影响旧《港硕这一年》的存档。
Mac版未经过Apple Developer ID公证，首次打开可能需针对该App选择仍要打开。
Windows没有原生exe，使用随包的WebGL浏览器版；未做Windows实机验证。
'''
BAT='''@echo off
cd /d "%~dp0"
py -3 --version >nul 2>&1
if not errorlevel 1 (
  py -3 Tools\\preview.py
  goto finished
)
python Tools\\preview.py
:finished
pause
'''
COMMAND='''#!/bin/zsh
cd "${0:A:h}"
python3 Tools/preview.py
'''

def build(name,files,extra):
    dest=OUT/name
    with zipfile.ZipFile(dest,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for p,arc in files:z.write(p,arc)
        z.writestr('先看这里.txt',README.encode('utf-8-sig'))
        for filename,data,mode in extra:
            info=zipfile.ZipInfo(filename);info.create_system=3;info.external_attr=(stat.S_IFREG|mode)<<16;info.compress_type=zipfile.ZIP_DEFLATED;z.writestr(info,data)
    with zipfile.ZipFile(dest) as z:
        assert z.testzip() is None
        assert not any('LocalReferences' in n or n.endswith('new-game-requirements.txt') for n in z.namelist())
    result={'bytes':dest.stat().st_size,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()};print(name,result['bytes']);return result

manifest={}
licenses=[(p,'Licenses/'+p.name) for p in (ROOT/'Licenses').iterdir() if p.is_file()]
licenses.append((ROOT/'Assets/Resources/Fonts/OFL.txt','Licenses/Font-OFL.txt'))
mac=ROOT/('Build/macOS-v'+VERSION)
assert (mac/'土豆子的港硕生活.app/Contents/MacOS/土豆子的港硕生活').exists()
name='PotatoHK-Mac-v'+VERSION+'.zip'
manifest[name]=build(name,[(p,str(p.relative_to(mac))) for p in mac.rglob('*') if p.is_file() and p.name!='.DS_Store']+licenses,[])
name='PotatoHK-Browser-v'+VERSION+'.zip'
manifest[name]=build(name,[(p,str(p.relative_to(ROOT))) for p in (ROOT/'Build/WebGL').rglob('*') if p.is_file()]+licenses+[(ROOT/'Tools/preview.py','Tools/preview.py')],[('浏览器试玩-Windows.bat',BAT.replace('\n','\r\n').encode('ascii'),0o644),('浏览器试玩.command',COMMAND.encode(),0o755)])
(OUT/'checksums.json').write_text(json.dumps(manifest,indent=2)+'\n')
for name,item in manifest.items():(OUT/(name+'.sha256')).write_text(item['sha256']+'  '+name+'\n')
