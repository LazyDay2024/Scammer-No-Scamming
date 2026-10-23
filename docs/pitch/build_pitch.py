"""Builds the 2-minute idea pitch video.

Each slide = full-bleed key art (docs/pitch/art/NN_*.png) + a transparent text overlay rendered by headless Edge
(so Thai shaping is correct). The video slowly zooms/pans each picture, fades the text in and cross-fades slides.

Run:   python docs/pitch/build_pitch.py            -> slides/slide_NN.png + pitch_silent.mp4
       python docs/pitch/build_pitch.py voice.m4a  -> also pitch.mp4 with narration
Needs: opencv-python, numpy, imageio-ffmpeg, Microsoft Edge.
"""
import glob
import html
import os
import subprocess
import sys

import cv2
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "art")
SLIDES = os.path.join(HERE, "slides")
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
W, H, FPS = 1920, 1080, 30
FADE = 0.6  # seconds of cross-fade between slides

# seconds, kicker, headline, lines, caption, layout
DECK = [
    (12, "2035 · Siam Phirom", "Scammer<br>No Scamming", ["ม้าเหล็กหมดน้ำมัน", "ปี 2035 เมืองทั้งเมืองดับสนิท", "เหลือตำรวจสะอาดคนเดียว กับม้าหนึ่งตัว"],
     "แก๊งสแกมดูดทั้งน้ำมัน ไฟ และเงินของผู้คน จนเมืองนี้ตาย", "title"),
    (15, "The Real Damage", "121,921 คดี<br><span class='sm'>ใน 4 เดือน</span>", ["เสียหาย 7.48 พันล้านบาท"],
     "นี่ไม่ใช่ตัวเลขในเกม นี่คือความเสียหายจริงของคนไทย", "stat"),
    (13, "Target Audience", "ใครก็โดนหลอกได้", ["วัยรุ่น และวัยทำงาน", "คนที่มือถือไม่เคยห่างมือ"],
     "ถ้าคุณเคยซื้อของออนไลน์ หรือกดลิงก์จากแชต เกมนี้ทำมาเพื่อคุณ", "text"),
    (14, "Game Concept", "คาวบอย<br>ไล่ปิดรังสแกม", ["เมืองวงแหวน สถานีตำรวจอยู่ใจกลาง", "ตึกสูง → ตึกแถว → บ้าน · ถนนหลัก 4 ทิศ"],
     "อ่านเบาะแส ปักหมุดรังให้ถูกโซน ถูกทิศ แล้วออกล่า", "text"),
    (18, "Core Gameplay", "1 คดี 6 ขั้น", ["STEPS"],
     "ทุกคดีเริ่มจากเรื่องของคนที่โดนหลอก และจบที่หลักฐานถูก Seal", "steps"),
    (17, "Unique Selling Point", "หยุดคิด = รอด<br><span class='hot'>รีบ = โดน</span>", ["กับดักทุกแบบเลียนแบบสแกมจริง", "มีสัญญาณให้สังเกตก่อนเสมอ"],
     "กล่องไหนจริง กล่องไหนปลอม? คนที่จ้องคุณอยู่ ไว้ใจได้ไหม?", "text"),
    (17, "Player Experience", "จับทัน = ภูมิใจ", ["ตื่นเต้น แต่ต้องแยกให้ออกว่าใครคือเหยื่อ", "หน้าบทเรียนเฉลยสแกมของจริง", "คดีสุ่ม · ฉากจบ 2 แบบ"],
     "เราไม่ได้สอนวิธีในเกม แต่ฝึกให้คิดก่อน เพื่อภูมิต้านทานในชีวิตจริง", "text"),
    (14, "Development Plan", "อีก 2 วันได้เล่น", ["PLAN"],
     "เขียนเทสต์ไว้แล้ว 32 ตัว เจอกันบน itch.io วันที่ 25 ต.ค.", "plan"),
]

STEPS = ["ฟังผู้เสียหาย", "หารังบนแผนที่", "แต่งยีนม้า", "ขี่ผ่านกับดัก", "บุกรัง <span class='hot'>ห้ามยิงเหยื่อ</span>", "Seal หลักฐาน"]
PLAN = [("เสร็จแล้ว", "ขี่ม้า · ยิง · เมือง · ยีน · กับดัก 4 แบบ"),
        ("24 ต.ค.", "ศัตรู · คดีครบลูป · UI ไทย · ทดสอบกับคนจริง"),
        ("25 ต.ค.", "ส่งขึ้น itch.io")]

CSS = """
*{margin:0;box-sizing:border-box}
html,body{background:transparent}
body{width:1920px;height:1080px;overflow:hidden;font-family:'Leelawadee UI','Tahoma',sans-serif;color:#F3E9D2;position:relative}
.shade{position:absolute;inset:0;background:linear-gradient(90deg,rgba(9,14,17,.92) 0%,rgba(9,14,17,.78) 34%,rgba(9,14,17,.15) 62%,rgba(9,14,17,0) 75%)}
.shade.title{background:linear-gradient(90deg,rgba(9,14,17,.85) 0%,rgba(9,14,17,.45) 45%,rgba(9,14,17,0) 70%),linear-gradient(0deg,rgba(9,14,17,.7),rgba(9,14,17,0) 40%)}
.wrap{position:absolute;left:130px;top:140px;width:1040px}
.kicker{display:inline-block;font-size:28px;letter-spacing:6px;font-weight:700;color:#11191D;background:#E5B96E;padding:6px 18px 4px;text-transform:uppercase}
h1{font-size:108px;line-height:1.08;margin-top:34px;font-weight:700;color:#FFF4DC;text-shadow:0 6px 30px rgba(0,0,0,.6)}
.lay-title h1{font-size:128px;letter-spacing:-2px}
.lay-title .tag{font-size:60px;color:#E5B96E;margin-top:18px;font-weight:700}
.lay-title .line{font-size:40px}
.lay-title{top:100px}
.bar{width:120px;height:10px;background:#D97843;margin:38px 0 30px}
.line{font-size:46px;line-height:1.45;color:#EDE2C8;text-shadow:0 2px 12px rgba(0,0,0,.7)}
.lay-stat h1{font-size:170px;color:#E5B96E}
h1 .sm{font-size:70px;color:#FFF4DC}
.hot{color:#E8794A}
.lay-stat .line:first-of-type{font-size:64px;color:#FFF4DC;font-weight:700}
.src{position:absolute;left:130px;bottom:170px;font-size:24px;color:#C8B894}
.steps{display:flex;flex-direction:column;gap:18px;margin-top:10px}
.step{display:flex;align-items:center;gap:24px;font-size:42px}
.step b{flex:none;width:60px;height:60px;border-radius:50%;background:#D97843;color:#11191D;display:flex;align-items:center;justify-content:center;font-size:36px}
.plan{display:flex;flex-direction:column;gap:22px}
.plan div{align-self:flex-start;font-size:36px;line-height:1.4;border-left:8px solid #D97843;padding:6px 0 6px 22px;background:rgba(17,25,29,.55)}
.plan div span{display:block;color:#E5B96E;font-weight:700;font-size:30px;letter-spacing:2px}
.cap{position:absolute;left:0;right:0;bottom:56px;text-align:center}
.cap span{display:inline-block;font-size:38px;line-height:1.4;padding:12px 30px;background:rgba(9,14,17,.78);border-radius:10px;color:#fff}
.num{position:absolute;right:56px;top:46px;font-size:26px;letter-spacing:3px;color:rgba(243,233,210,.6)}
"""


def overlay_html(i, item):
    _, kicker, title, lines, caption, layout = item
    if layout == "title":
        inner = (f"<h1>{title}</h1><div class='tag'>{lines[0]}</div><div class='bar'></div>" +
                 "".join(f"<div class='line'>{l}</div>" for l in lines[1:]))
    elif layout == "steps":
        inner = (f"<h1>{title}</h1><div class='bar'></div><div class='steps'>" +
                 "".join(f"<div class='step'><b>{n + 1}</b>{s}</div>" for n, s in enumerate(STEPS)) + "</div>")
    elif layout == "plan":
        inner = (f"<h1>{title}</h1><div class='bar'></div><div class='plan'>" +
                 "".join(f"<div><span>{h}</span>{t}</div>" for h, t in PLAN) + "</div>")
    else:
        inner = f"<h1>{title}</h1><div class='bar'></div>" + "".join(f"<div class='line'>{l}</div>" for l in lines)
    extra = ("<div class='src'>ม.ค.-เม.ย. 2569 · ที่มา: ศูนย์ต่อต้านการฉ้อโกงออนไลน์ (ACSC) ผ่าน Thai PBS Verify</div>"
             if layout == "stat" else "")
    shade = "shade title" if layout == "title" else "shade"
    return (f"<!doctype html><html lang='th'><head><meta charset='utf-8'><style>{CSS}</style></head>"
            f"<body class='b-{layout}'><div class='{shade}'></div><div class='num'>{i + 1:02d} / {len(DECK):02d}</div>"
            f"<div class='wrap lay-{layout}'><div class='kicker'>{kicker}</div>{inner}</div>{extra}"
            f"<div class='cap'><span>{html.escape(caption)}</span></div></body></html>")


def wait_for_file(path, timeout=20.0):
    """Edge can return before the screenshot is flushed; wait until the file exists and stops growing."""
    import time
    end, last = time.time() + timeout, -1
    while time.time() < end:
        size = os.path.getsize(path) if os.path.exists(path) else -1
        if size > 0 and size == last:
            return
        last = size
        time.sleep(0.3)
    raise RuntimeError(f"screenshot not written: {path}")


def render_overlays():
    os.makedirs(SLIDES, exist_ok=True)
    paths = []
    for i, item in enumerate(DECK):
        page = os.path.join(SLIDES, f"overlay_{i + 1:02d}.html")
        png = os.path.join(SLIDES, f"overlay_{i + 1:02d}.png")
        with open(page, "w", encoding="utf-8") as f:
            f.write(overlay_html(i, item))
        if os.path.exists(png):
            os.remove(png)
        subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                        "--default-background-color=00000000", f"--screenshot={png}", f"--window-size={W},{H}",
                        "file:///" + page.replace("\\", "/")], check=True, capture_output=True, timeout=60)
        wait_for_file(png)
        paths.append(png)
    return paths


def load_art(i):
    found = sorted(glob.glob(os.path.join(ART, f"{i + 1:02d}_*.png")))
    if found:
        img = cv2.imread(found[0], cv2.IMREAD_COLOR)
        return cv2.resize(img, (W, H), interpolation=cv2.INTER_AREA)
    return np.full((H, W, 3), (29, 25, 17), np.uint8)  # fallback: near-black


def ken_burns(img, t, i):
    """Slow zoom 1.0 -> 1.08 with a gentle pan; direction alternates per slide."""
    z = 1.0 + 0.08 * t
    cw, ch = int(W / z), int(H / z)
    dx = (W - cw) * (t if i % 2 == 0 else 1 - t)
    dy = (H - ch) * 0.5
    crop = img[int(dy):int(dy) + ch, int(dx):int(dx) + cw]
    return cv2.resize(crop, (W, H), interpolation=cv2.INTER_LINEAR)


def composite(base, overlay_rgba, alpha_scale):
    a = overlay_rgba[:, :, 3:4].astype(np.float32) / 255.0 * alpha_scale
    return (base.astype(np.float32) * (1 - a) + overlay_rgba[:, :, :3].astype(np.float32) * a).astype(np.uint8)


def frame_of(i, local_t, arts, overlays):
    dur = DECK[i][0]
    base = ken_burns(arts[i], local_t / dur, i)
    text_alpha = min(1.0, max(0.0, (local_t - 0.3) / 0.7))
    return composite(base, overlays[i], text_alpha)


def video(overlay_paths, voice=None):
    import imageio_ffmpeg
    arts = [load_art(i) for i in range(len(DECK))]
    overlays = [cv2.imread(p, cv2.IMREAD_UNCHANGED) for p in overlay_paths]
    for i, ov in enumerate(overlays):
        if ov.shape[2] == 3:  # no alpha came back; treat dark as transparent is wrong, so fail loudly
            raise RuntimeError(f"overlay {i + 1} has no alpha channel")
        cv2.imwrite(os.path.join(SLIDES, f"slide_{i + 1:02d}.png"), composite(ken_burns(arts[i], 0.5, i), ov, 1.0))

    ff = imageio_ffmpeg.get_ffmpeg_exe()
    silent = os.path.join(HERE, "pitch_silent.mp4")
    proc = subprocess.Popen([ff, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "bgr24", "-s", f"{W}x{H}",
                             "-r", str(FPS), "-i", "-", "-c:v", "libx264", "-preset", "medium", "-crf", "20",
                             "-pix_fmt", "yuv420p", "-movflags", "+faststart", silent], stdin=subprocess.PIPE)
    starts = np.cumsum([0] + [d[0] for d in DECK])
    total = starts[-1]
    for f in range(int(total * FPS)):
        t = f / FPS
        i = int(np.searchsorted(starts, t, side="right") - 1)
        frame = frame_of(i, t - starts[i], arts, overlays)
        if i + 1 < len(DECK) and starts[i + 1] - t < FADE:  # cross-fade into the next slide
            k = 1 - (starts[i + 1] - t) / FADE
            nxt = composite(ken_burns(arts[i + 1], 0.0, i + 1), overlays[i + 1], 0.0)
            frame = cv2.addWeighted(frame, 1 - k, nxt, k, 0)
        if t < 0.8:
            frame = (frame * (t / 0.8)).astype(np.uint8)
        if total - t < 0.8:
            frame = (frame * ((total - t) / 0.8)).astype(np.uint8)
        proc.stdin.write(frame.tobytes())
    proc.stdin.close()
    proc.wait()
    if voice:
        out = os.path.join(HERE, "pitch.mp4")
        subprocess.run([ff, "-y", "-loglevel", "error", "-i", silent, "-i", voice, "-c:v", "copy", "-c:a", "aac",
                        "-shortest", out], check=True)
        return out
    return silent


if __name__ == "__main__":
    total = sum(item[0] for item in DECK)
    assert total <= 120, total
    out = video(render_overlays(), sys.argv[1] if len(sys.argv) > 1 else None)
    print(f"{out} ({total}s)")
