from pathlib import Path
import urllib.request
out=Path(__file__).resolve().parents[1]/'Assets/Resources/Fonts'
out.mkdir(parents=True,exist_ok=True)
for name,url in [ ('NotoSansSC.ttf','https://raw.githubusercontent.com/google/fonts/main/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf'), ('OFL.txt','https://raw.githubusercontent.com/google/fonts/main/ofl/notosanssc/OFL.txt') ]:
    if not (out/name).exists():
        urllib.request.urlretrieve(url,out/name)
        print(name,(out/name).stat().st_size)
