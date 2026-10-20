from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset
root=Path(__file__).resolve().parents[1]
p=root/'Assets/Resources/Fonts/NotoSansSC.ttf'
if not p.exists(): p=root/'Tools/NotoSansSC-source.ttf'
f=TTFont(p)
f=instantiateVariableFont(f,{'wght':500},inplace=True)
# Keep all game glyphs plus printable ASCII; rerun after adding copy.
chars=set(''.join(x.read_text() for x in (root/'Assets').rglob('*.cs')))
chars.update(chr(c) for c in f.getBestCmap() if 0x3400 <= c <= 0x9FFF);chars.update(chr(c) for c in range(32,127));chars.update('−→←≥×＋％…✓—0123456789')
opts=subset.Options();opts.name_IDs=['*'];opts.name_legacy=True;opts.name_languages=['*']
s=subset.Subsetter(options=opts);s.populate(text=''.join(chars));s.subset(f)
for record in f['name'].names:
    if record.nameID in (1,2,3,4,6,16,17):
        name='Harbour Notebook Sans' if record.nameID not in (2,17,6) else ('Regular' if record.nameID in (2,17) else 'HarbourNotebookSans')
        record.string=name.encode(record.getEncoding(),errors='replace')
out=root/'Assets/Resources/Fonts/NotebookSans.ttf';f.save(out)
missing=sorted(c for c in chars if ord(c)>31 and ord(c) not in f.getBestCmap() and c not in '\n\r\t')
print('Saved',out,'bytes',out.stat().st_size,'missing',missing)
# Original variable font is tooling source, not a shipped Resources asset.
if p!=root/'Tools/NotoSansSC-source.ttf': p.rename(root/'Tools/NotoSansSC-source.ttf')
