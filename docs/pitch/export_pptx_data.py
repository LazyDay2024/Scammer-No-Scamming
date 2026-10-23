"""Writes the backgrounds and slide data that make_pptx.ps1 turns into an editable PowerPoint.

python docs/pitch/export_pptx_data.py   then   powershell -File docs/pitch/make_pptx.ps1
"""
import json
import os
import re

import cv2
import numpy as np

import build_pitch as b

OUT = os.path.join(b.HERE, "pptx_bg")


def shade(img, title):
    """Same left-to-right darkening as the video overlay, so text stays readable."""
    x = np.linspace(0, 1, b.W)
    if title:
        a = np.interp(x, [0, .45, .70, 1], [.85, .45, 0, 0])
    else:
        a = np.interp(x, [0, .34, .62, .75, 1], [.92, .78, .15, 0, 0])
    a = a[None, :, None]
    dark = np.array([17, 14, 9], np.float32)  # #090E11 in BGR
    out = img.astype(np.float32) * (1 - a) + dark * a
    # darken the bottom for the caption bar
    y = np.interp(np.linspace(0, 1, b.H), [0, .7, 1], [0, 0, .55])[:, None, None]
    return (out * (1 - y) + dark * y).astype(np.uint8)


def plain(html_text):
    t = re.sub(r"<br\s*/?>", "\n", html_text)
    return re.sub(r"<[^>]+>", "", t)


def main():
    os.makedirs(OUT, exist_ok=True)
    slides = []
    for i, (sec, kicker, title, lines, caption, layout) in enumerate(b.DECK):
        bg = os.path.join(OUT, f"bg_{i + 1:02d}.png")
        cv2.imwrite(bg, shade(b.ken_burns(b.load_art(i), 0.5, i), layout == "title"))
        if layout == "steps":
            body = [f"{n + 1}. {plain(s)}" for n, s in enumerate(b.STEPS)]
        elif layout == "plan":
            body = [f"{h}: {t}" for h, t in b.PLAN]
        else:
            body = [plain(l) for l in lines]
        slides.append({"seconds": sec, "kicker": kicker.upper(), "title": plain(title), "lines": body,
                       "caption": caption, "layout": layout, "background": bg,
                       "source": ("ม.ค.-เม.ย. 2569 · ที่มา: ศูนย์ต่อต้านการฉ้อโกงออนไลน์ (ACSC) ผ่าน Thai PBS Verify"
                                  if layout == "stat" else "")})
    with open(os.path.join(b.HERE, "pptx_slides.json"), "w", encoding="utf-8") as f:
        json.dump(slides, f, ensure_ascii=False, indent=1)
    print(len(slides), "slides")


if __name__ == "__main__":
    main()
