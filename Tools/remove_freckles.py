"""Remove the small freckle dots from the potato's golden skin.

Usage: python3 Tools/remove_freckles.py <in.png> <out.png> [--mask <mask.png>]

Freckles are small, compact, slightly darker golden-brown blobs. Outlines and
arm creases are long and thin, eyes are near-black and the mouth and cheeks are
red, so they are kept. Each freckle is filled from the surrounding skin colour.
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

def hsv(rgb):
    r, g, b = [rgb[..., i] for i in range(3)]
    mx = rgb.max(-1); mn = rgb.min(-1); d = mx - mn + 1e-6
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    return h, d / (mx + 1e-6), mx

def main(src, dst, mask_out=None):
    im = np.asarray(Image.open(src).convert("RGBA")).astype(np.float32)
    rgb, a = im[..., :3] / 255, im[..., 3]
    h, s, v = hsv(rgb)
    # Golden potato skin and its darker freckles share one warm hue band.
    warm = (a > 200) & (h > 22) & (h < 48) & (s > .25) & (v > .35)
    skin = warm & (v > .62)
    # Local skin brightness, measured only over skin pixels.
    k = 25
    vs = ndimage.uniform_filter(np.where(skin, v, 0), k)
    ws = ndimage.uniform_filter(skin.astype(np.float32), k)
    local = vs / np.maximum(ws, 1e-3)
    # Stay clear of the outline, the alpha edge and the sprout so strokes are never touched.
    line = (a < 200) | (v < .45) | ~warm & (a > 200)
    away = ndimage.distance_transform_edt(~line) > 5
    cand = warm & (v < local - .028) & (ws > .4) & (v > .38) & away
    cand = ndimage.binary_opening(cand, iterations=1)
    lab, n = ndimage.label(cand)
    keep = np.zeros_like(cand)
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        comp = lab[sl] == i
        area = comp.sum(); hh = sl[0].stop - sl[0].start; ww = sl[1].stop - sl[1].start
        fill = area / (hh * ww)
        # Compact dots only: never long outline strokes or creases.
        if 6 <= area <= 260 and max(hh, ww) <= 24 and max(hh, ww) <= 2.4 * min(hh, ww) and fill > .45:
            keep[sl] |= comp
    keep = ndimage.binary_dilation(keep, iterations=2) & (a > 200)
    # Fill from nearby skin colour.
    out = im.copy()
    src_mask = skin & ~keep
    for c in range(3):
        ch = im[..., c]
        num = ndimage.gaussian_filter(np.where(src_mask, ch, 0), 6)
        den = ndimage.gaussian_filter(src_mask.astype(np.float32), 6)
        fillc = num / np.maximum(den, 1e-3)
        soft = ndimage.gaussian_filter(keep.astype(np.float32), 1.2)
        out[..., c] = ch * (1 - soft) + fillc * soft
    out[..., 3] = im[..., 3]
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA").save(dst, optimize=True)
    if mask_out:
        Image.fromarray((keep * 255).astype(np.uint8)).save(mask_out)
    print(src, "freckle px", int(keep.sum()))

if __name__ == "__main__":
    args = sys.argv[1:]
    m = None
    if "--mask" in args:
        i = args.index("--mask"); m = args[i + 1]; del args[i:i + 2]
    main(args[0], args[1], m)
