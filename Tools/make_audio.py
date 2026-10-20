"""Original interaction effects. Background recordings are user-supplied.
Background AudioClips: Resources/Audio/Day, Night, Travel.
"""
from pathlib import Path
import numpy as np
import wave
ROOT=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio'
ROOT.mkdir(parents=True,exist_ok=True)
SR=22050

def save(name,data):
    peak=max(.9,float(np.max(np.abs(data))))
    data=np.clip(data/peak*.77,-1,1)
    with wave.open(str(ROOT/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR);f.writeframes((data*32767).astype('<i2').tobytes())

def note(buf,at,duration,midi,amp=0.1,kind='keys'):
    start=int(at*SR);n=min(int(duration*SR),len(buf)-start)
    if n<=0:return
    t=np.arange(n)/SR;freq=440*2**((midi-69)/12)
    if kind=='bass':tone=np.sin(2*np.pi*freq*t)+.18*np.sin(4*np.pi*freq*t);env=(1-np.exp(-t*90))*np.exp(-t*3)
    else:tone=np.sin(2*np.pi*freq*t)+.22*np.sin(4*np.pi*freq*t)*np.exp(-t*7)+.08*np.sin(8*np.pi*freq*t)*np.exp(-t*10);env=(1-np.exp(-t*180))*np.exp(-t*2.6)
    env*=np.minimum(1,(n/SR-t)*35)
    buf[start:start+n]+=amp*tone*env

for name,notes in [('bubble',[84]),('coin',[79,84]),('stamp',[60,72,79]),('return',[72,76,79,84]),('company',[76,79]),('page',[57])]:
    data=np.zeros(int(SR*.65))
    for i,n in enumerate(notes):note(data,i*.075,.35,n,.24)
    if name=='page':
        rng=np.random.default_rng(18);data+=rng.normal(0,.07,len(data))*np.exp(-np.arange(len(data))/SR*15)
    save(name,data)
print('Created six original feedback sounds; supplied music is untouched.')
