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
# seconds, kicker, headline, lines, captions [(start second within the scene, text)], layout
# Copy: second Claude account, in the voice of the team's own refined pitch (2026-10-23).
DECK = [
    (12, "Scammer No Scamming", "เคยเกือบกดลิงก์<br>ที่ไม่ควรกดไหม?", ["ม้าเหล็กหมดน้ำมัน"],
     [(0, "ถ้าเคย… ไม่ต้องอาย เมืองนี้กดกันไปหมดแล้ว"),
      (5, "ปี 2035 แก๊งสแกมดูดน้ำมันกับไฟจนเกลี้ยง เหลือตำรวจมือสะอาดคนเดียว")], "title"),
    (15, "The Real Damage", "121,921 คดี", ["เสียหายรวม 7.48 พันล้านบาท"],
     [(0, "ฟังเหมือนนิยายใช่ไหม"),
      (6, "แต่นี่ไม่ใช่ฉากในเกม… นี่คือ 4 เดือนแรกของปีนี้ ในโลกจริง")], "stat"),
    (13, "Target Audience", "แล้วใครโดน?", ["วัยรุ่น · วัยทำงาน", "คนที่มือถือไม่เคยห่างมือ"],
     [(0, "ไม่ใช่คนโง่… คือคนแบบเรานี่แหละ"),
      (5, "นั่งกินข้าว เลื่อนมือถือ แล้วเผลอกดไปทีเดียว")], "text"),
    (14, "Game Concept", "ถ้าให้นึกภาพ…", ["ขี่ม้าแบบ Red Dead Redemption", "แต่ต้องจับผิดแบบ Papers, Please"],
     [(0, "คุณคือสารวัตรเอก ขี่ม้าไล่ปิดรังสแกมในเมืองวงแหวน"),
      (7, "สถานีอยู่ตรงกลาง รังซ่อนอยู่ในตึกสูง ตึกแถว และบ้าน")], "text"),
    (18, "Core Gameplay", "1 คดี เริ่มจาก 1 คน", ["STEPS"],
     [(0, "ฟังเขาเล่าให้จบ เบาะแสซ่อนอยู่ในเรื่อง"),
      (8, "เดาให้ถูกว่ารังอยู่โซนไหน ทิศไหน… แล้วควบไปเลย")], "steps"),
    (17, "Unique Selling Point", "ต่างจากเกมยิงทั่วไป<br>3 อย่าง",
     ["1  กับดักทุกอันลอกมาจากสแกมจริง", "2  มีพิรุธให้เห็นก่อนเสมอ", "3  โดนแล้ว ได้เฉลยของจริง"],
     [(0, "กล่องไหนปลอม? คนนั้นจ้องเราทำไม?"),
      (8, "หยุดดูก็รอด… รีบเมื่อไหร่ก็โดน")], "text"),
    (17, "Player Experience", "ไม่ใช่ทุกคน<br>คือคนร้าย", ["ห้ามยิงเหยื่อ ห้ามยิงคนที่โดนบังคับมา", "คดีสุ่มทุกรอบ · ฉากจบ 2 แบบ"],
     [(0, "ตื่นเต้นตอนบุก แต่ภูมิใจที่สุดตอนจับพิรุธได้ทัน"),
      (9, "ยิงก่อนคิดเมื่อไหร่… ฉากจบเละเมื่อนั้น")], "text"),
    (14, "Development Plan", "รังสุดท้าย<br>ไม่ได้อยู่ในเกม", ["PLAN"],
     [(0, "มันอยู่ในมือถือของเราทุกคน"),
      (6, "เราไม่ได้สอนให้บุก แค่ให้ซ้อมคิดก่อนเจอจริง · 25 ต.ค. มาลองเอง")], "plan"),
]

STEPS = ["ฟังผู้เสียหาย", "หารัง", "แต่งยีนม้า", "ขี่ผ่านกับดัก", "บุกรัง", "Seal หลักฐาน"]
PLAN = [("เสร็จแล้ว", "ม้า · ยิง · เมือง · ยีน · กับดัก 5 แบบ · หน้าวิเคราะห์ · บทเรียน · เทสต์ 44 ตัว"),
        ("24 ต.ค.", "ศัตรู · ปิดลูปคดี · UI ไทย · ให้คนจริงลองเล่น"),
        ("25 ต.ค.", "ขึ้น itch.io")]

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


def overlay_html(i, item, caption):
    _, kicker, title, lines, _captions, layout = item
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
      beats = []
      for k, (_, caption) in enumerate(item[4]):
        page = os.path.join(SLIDES, f"overlay_{i + 1:02d}_{k + 1}.html")
        png = os.path.join(SLIDES, f"overlay_{i + 1:02d}_{k + 1}.png")
        with open(page, "w", encoding="utf-8") as f:
            f.write(overlay_html(i, item, caption))
        if os.path.exists(png):
            os.remove(png)
        subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                        "--default-background-color=00000000", f"--screenshot={png}", f"--window-size={W},{H}",
                        "file:///" + page.replace("\\", "/")], check=True, capture_output=True, timeout=60)
        wait_for_file(png)
        beats.append(png)
      paths.append(beats)
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


def beat_of(i, local_t):
    k = 0
    for n, (start, _) in enumerate(DECK[i][4]):
        if local_t >= start:
            k = n
    return k


def frame_of(i, local_t, arts, overlays):
    dur = DECK[i][0]
    base = ken_burns(arts[i], local_t / dur, i)
    text_alpha = min(1.0, max(0.0, (local_t - 0.3) / 0.7))
    return composite(base, overlays[i][beat_of(i, local_t)], text_alpha)


def video(overlay_paths, voice=None):
    import imageio_ffmpeg
    arts = [load_art(i) for i in range(len(DECK))]
    overlays = [[cv2.imread(p, cv2.IMREAD_UNCHANGED) for p in beats] for beats in overlay_paths]
    for i, beats in enumerate(overlays):
        for ov in beats:
            if ov is None or ov.shape[2] != 4:
                raise RuntimeError(f"overlay {i + 1} has no alpha channel")
        cv2.imwrite(os.path.join(SLIDES, f"slide_{i + 1:02d}.png"), composite(ken_burns(arts[i], 0.5, i), beats[-1], 1.0))

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
            nxt = ken_burns(arts[i + 1], 0.0, i + 1)
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
