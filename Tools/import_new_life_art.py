#!/usr/bin/env python3
"""Turns the approved preview art (ArtSource/newlife/webp) into Assets/Resources/Art/Life PNGs.
Sizes are rounded to multiples of 4 for texture compression. The fee poster goes to Resources/Hospital."""
import os
from PIL import Image
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC=os.path.join(ROOT,'ArtSource','newlife','webp');OUT=os.path.join(ROOT,'Assets','Resources','Art','Life')
NAMES={'potato':'Potato','act':'Actions','eath':'EatHand','eatp':'EatChopsticks','food':'Food','bench':'Bench','table':'Table','stop':'MinibusStop','places':'Places','street':'Street','cafe':'Cafe','clinic':'Clinic','mall':'Mall','mtr':'Mtr','park':'Park','store':'Store'}
for b in 'corgi westie shiba poodle tong ginger bsh ragdoll ash siamese'.split():NAMES['b_'+b]='Pet_'+b
os.makedirs(OUT,exist_ok=True)
def four(i):
    w,h=i.size;s=(round(w/4)*4,round(h/4)*4)
    return i if s==i.size else i.resize(s,Image.LANCZOS)
for k,n in NAMES.items():
    i=Image.open(os.path.join(SRC,'a_'+k+'.webp'));four(i).save(os.path.join(OUT,n+'.png'),optimize=True);print(n,i.size)
# The pouring frame carries a sliver of the neighbouring cell's tail on its left edge.
act=Image.open(os.path.join(OUT,'Actions.png')).convert('RGBA');act.paste((0,0,0,0),(600,300,660,600));act.save(os.path.join(OUT,'Actions.png'),optimize=True)
# The minibus is the owner's drawn replacement for the earlier photo: trimmed to its alpha and kept at 1200 px wide.
bus=Image.open(os.path.join(ROOT,'ArtSource','newlife','minibus-drawn.png')).convert('RGBA');bus=bus.crop(bus.getchannel('A').getbbox())
four(bus.resize((1200,round(1200*bus.height/bus.width)),Image.LANCZOS)).save(os.path.join(OUT,'Minibus.png'),optimize=True)
hosp=os.path.join(ROOT,'Assets','Resources','Hospital');os.makedirs(hosp,exist_ok=True)
four(Image.open(os.path.join(SRC,'a_poster.webp')).convert('RGB')).save(os.path.join(hosp,'FeePoster.jpg'),quality=92,subsampling=0,optimize=True)
