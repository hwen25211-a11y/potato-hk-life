#!/usr/bin/env python3
"""Copies the owner's Hong Kong photos into ArtSource/photos/originals (kept, never edited)
and writes game-size and thumbnail JPEGs to Assets/Resources/Photos.

The whole frame is kept: no cropping, no padding. Sizes are rounded to multiples of 4
for texture compression (an aspect change well under 0.5%). Metadata is not copied.
Usage: python3 Tools/prepare_photos.py [source folder, default ~/Desktop/照片]
"""
import os, shutil, sys
from PIL import Image, ImageOps

# Fixed id -> source file name. Ids match PhotoSpot.key in PotatoModel.cs.
PHOTOS = {
    "tram-139": "1.png",
    "central-wheel": "8034fc56-c146-4c5b-ba8f-f9fdaacb763d.png",
    "chungking-mansions": "96fe6cbd-13f3-4f22-a663-201335098498.png",
    "night-arches": "微信圖片_20261024181243_1038_713.jpg",
    "back-alley": "微信圖片_20261024181243_1040_713.jpg",
    "nam-cheong-pawn": "微信圖片_20261024181243_1042_713.jpg",
    "colour-tenements": "微信圖片_20261024181243_1043_713.jpg",
    "red-taxi-arches": "微信圖片_20261024181243_1044_713.jpg",
    "monster-building": "微信圖片_20261024181243_1045_713.jpg",
    # 2026-10-24, sent in chat; only the copies in ArtSource/photos/originals exist.
    "stonecutters-harbour": "stonecutters-harbour.jpg",
    "central-across-harbour": "central-across-harbour.jpg",
    "canal-road-villain": "canal-road-villain.jpg",
}
# Retired: golden-bauhinia (id 101, stock-photo watermark) was removed at the owner's request.
GAME, THUMB = 1600, 480

def fit(img, longest):
    w, h = img.size
    k = min(1, longest / max(w, h))
    size = (max(4, round(w * k / 4) * 4), max(4, round(h * k / 4) * 4))
    return img.resize(size, Image.LANCZOS)

def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    source = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser("~/Desktop/照片")
    originals = os.path.join(root, "ArtSource", "photos", "originals")
    out = os.path.join(root, "Assets", "Resources", "Photos")
    os.makedirs(originals, exist_ok=True); os.makedirs(out, exist_ok=True)
    missing = []
    for key, name in PHOTOS.items():
        path = os.path.join(source, name)
        if not os.path.exists(path):
            kept = [os.path.join(originals, f) for f in os.listdir(originals) if os.path.splitext(f)[0] == key]
            path = kept[0] if kept else path
        if not os.path.exists(path):
            missing.append(f"{key}: {name}"); continue
        copy = os.path.join(originals, key + os.path.splitext(name)[1].lower())
        if os.path.abspath(path) != os.path.abspath(copy): shutil.copy2(path, copy)
        img = ImageOps.exif_transpose(Image.open(path)).convert("RGB")
        big, small = fit(img, GAME), fit(img, THUMB)
        big.save(os.path.join(out, key + ".jpg"), quality=86, optimize=True)
        small.save(os.path.join(out, key + "_thumb.jpg"), quality=84, optimize=True)
        print(f"{key}: {img.size[0]}x{img.size[1]} -> {big.size[0]}x{big.size[1]}, thumb {small.size[0]}x{small.size[1]}")
    if missing:
        print("MISSING (no substitute used):"); print("\n".join("  " + m for m in missing)); sys.exit(1)

if __name__ == "__main__":
    main()
