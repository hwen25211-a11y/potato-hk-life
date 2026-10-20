"""Gentle feedback effects, combining original synthesis and selected Kenney CC0 foley.
Requires numpy and ffmpeg; never plays audio or modifies the supplied music.
Source recordings and licenses are tracked in Licenses/SoundEffects.json.
"""
from pathlib import Path
import numpy as np
import wave
import subprocess
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

generated={}
for name,notes in [('bubble',[84]),('coin',[79,84]),('stamp',[60,72,79]),('return',[72,76,79,84]),('company',[76,79]),('page',[57])]:
    data=np.zeros(int(SR*.65))
    for i,n in enumerate(notes):note(data,i*.075,.35,n,.24)
    if name=='page':
        rng=np.random.default_rng(18);data+=rng.normal(0,.07,len(data))*np.exp(-np.arange(len(data))/SR*15)
    generated[name]=data

def foley(name,peak=.18):
    source=Path(__file__).resolve().parent/'AudioSources'/name
    raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(source),'-ac','1','-ar',str(SR),'-af','lowpass=f=3500','-f','f32le','-'])
    data=np.frombuffer(raw,dtype='<f4').copy()
    data*=peak/max(.001,float(np.max(np.abs(data))))
    edge=min(int(SR*.008),len(data)//2)
    data[:edge]*=np.linspace(0,1,edge);data[-edge:]*=np.linspace(1,0,edge)
    data*=peak/max(.001,float(np.max(np.abs(data))))
    return data

for name in ('stamp','return','company'):generated[name]*=.55
generated['bubble']=foley('click_003.ogg',.16)
generated['page']=foley('card-slide-2.ogg',.12)
generated['purchase']=foley('pluck_001.ogg',.18)
generated['mail']=foley('open_001.ogg',.16)
clink=foley('chips-stack-3.ogg',.2)
coins=np.zeros(max(len(clink),len(generated['coin'])))
coins[:len(clink)]+=clink
coins[:len(generated['coin'])]+=generated['coin']*.55
generated['coin']=coins
for name,data in generated.items():save(name,data)
print('Created eight quiet feedback effects; no audio played; supplied music is untouched.')
