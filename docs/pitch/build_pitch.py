"""Builds the 2-minute pitch: HTML slides -> PNG (headless Edge, for correct Thai shaping) -> silent MP4.

Run: python docs/pitch/build_pitch.py
Optional: python docs/pitch/build_pitch.py voice.m4a   (muxes a recorded narration into pitch.mp4)
Needs: opencv-python, imageio-ffmpeg, Microsoft Edge.
"""
import html
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SLIDES = os.path.join(HERE, "slides")
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

# (seconds, kicker, title, bullets, narration)
DECK = [
    (8, "Game Jam 2026", "Scammer No Scamming<br><span class='sub'>ม้าเหล็กหมดน้ำมัน</span>",
     ["ฝึกคิดในเกม สร้างภูมิต้านทานสแกมในชีวิตจริง"],
     "Scammer No Scamming ม้าเหล็กหมดน้ำมัน เกมที่ให้คุณฝึกคิด ก่อนจะโดนหลอกจริง"),
    (14, "ปัญหาจริง", "121,921 คดี · 7.48 พันล้านบาท",
     ["คดีออนไลน์ที่คนไทยแจ้งความ แค่ 4 เดือนแรกของปี 2569",
      "ใครก็โดนหลอกได้ ไม่ใช่แค่ผู้สูงอายุ",
      "<span class='src'>ที่มา: ศูนย์ต่อต้านการฉ้อโกงออนไลน์ (ACSC) 1 ม.ค.-30 เม.ย. 2569 ผ่าน Thai PBS Verify</span>"],
     "แค่สี่เดือนแรกของปีนี้ คนไทยแจ้งความคดีออนไลน์กว่าแสนสองหมื่นคดี เสียหายรวมเจ็ดพันล้านบาท"),
    (13, "Target Audience", "เล่นเพื่อใคร",
     ["วัยรุ่นและวัยทำงานที่เล่นเกม",
      "เจองานออนไลน์ ร้านค้าปลอม ลิงก์ในแชต ทุกวัน",
      "เป็นคนที่เตือนพ่อแม่และคนในบ้านได้"],
     "เกมนี้ทำมาเพื่อวัยรุ่นและวัยทำงานที่เล่นเกม ซึ่งเจอสแกมทุกวัน และเป็นคนที่เตือนพ่อแม่ได้"),
    (17, "Game Concept", "ปี 2035 · คาวบอยดิสโทเปีย",
     ["แก๊งสแกมเมอร์ดูดน้ำมันและไฟจนรถใช้ไม่ได้ ทุกคนกลับมาขี่ม้า",
      "คุณคือ \"สารวัตรเอก\" ตำรวจสายสะอาดคนสุดท้าย",
      "ไล่ปิดรังสแกมเมอร์ ถึงบอส \"เจ้าสัวคลาวด์\"",
      "เกม FPS บนหลังม้า โทนเสียดสีสังคมไทย"],
     "ปี 2035 แก๊งสแกมเมอร์ดูดน้ำมันและไฟจนรถใช้ไม่ได้ คุณคือสารวัตรเอก ขี่ม้าไล่ปิดรังสแกมเมอร์"),
    (23, "Core Gameplay", "1 คดี = 5 ขั้น",
     ["LOOP"],
     "ฟังเรื่องผู้เสียหายแล้วหารังบนแผนที่ แต่งยีนม้าที่มีข้อเสีย ขี่ม้าผ่านกับดัก บุกรังโดยห้ามยิงเหยื่อ แล้วนำหลักฐานกลับไปปิดคดี"),
    (17, "Unique Selling Point", "กับดักที่ต้องคิดก่อนลงมือ",
     ["กับดักเลียนแบบสแกมจริง: กล่องของฟรี · ลิงก์ปลอม · คนปลอมตัว · พวกซุ่ม",
      "ทุกกับดักมี \"สัญญาณ\" ให้สังเกตก่อนเสมอ: หยุดคิด = รอด, รีบ = โดน",
      "ห้ามยิงเหยื่อและลูกจ้างที่ถูกบังคับ",
      "ไม่ได้สอนวิธีในเกม แต่ฝึกให้คิด เพื่อภูมิต้านทานในชีวิตจริง"],
     "กับดักทุกอันเลียนแบบกลวิธีสแกมจริงและมีสัญญาณให้สังเกตก่อนเสมอ คนที่หยุดคิดจะรอด คนที่รีบจะโดน"),
    (13, "Player Experience", "ตื่นเต้น · ภูมิใจ · อยากเล่นอีก",
     ["ตื่นเต้นตอนควบม้าบุกรัง",
      "ภูมิใจตอนจับสัญญาณหลอกลวงได้ทัน",
      "หน้าบทเรียนเฉลยว่าสิ่งที่เพิ่งโดน ในชีวิตจริงคืออะไร",
      "คดีสุ่มทุกรอบ + 2 ฉากจบ"],
     "ผู้เล่นตื่นเต้นตอนบุก ภูมิใจตอนจับสัญญาณได้ทัน และหน้าบทเรียนบอกว่าในชีวิตจริงมันคืออะไร"),
    (15, "Progress + แผน", "ทำแล้ว และกำลังจะทำ",
     ["ทำแล้ว: ขี่ม้า · ยิงแยกจุด · เมืองวงแหวน · ยีนม้า · กับดัก 4 แบบ · เทสต์ 29 ตัวผ่าน",
      "23 ต.ค. คืน: เลือด/ตาย/เล่นซ้ำ · Trouble Meter · ขั้นวิเคราะห์",
      "24 ต.ค.: ศัตรู · คดีครบลูป · UI ไทย · ทดสอบกับคนจริง · หยุดฟีเจอร์ 22:00",
      "25 ต.ค.: build Windows บน itch.io + วิดีโอ ส่งก่อน 12:00"],
     "ตอนนี้ทำระบบขี่ม้า ยิง เมือง ยีน และกับดักแล้ว พรุ่งนี้ทำคดีให้ครบลูป แล้วส่งเกมบน itch.io วันที่ 25"),
]

LOOP = ["ฟังเรื่องผู้เสียหาย<br>หารังบนแผนที่", "แต่งยีนม้า<br>ทุกยีนมีข้อเสีย", "ขี่ม้าผ่านเมือง<br>ระวังกับดัก",
        "บุกรัง<br>ห้ามยิงเหยื่อ", "Seal หลักฐาน<br>+ หน้าบทเรียน"]

CSS = """
*{margin:0;box-sizing:border-box}
body{width:1920px;height:1080px;overflow:hidden;font-family:'Leelawadee UI','Tahoma',sans-serif;color:#E7D9B8;
 background:radial-gradient(ellipse at 80% 10%,#1E6C70 0%,#11191D 55%) #11191D;position:relative}
.band{position:absolute;left:0;top:0;width:28px;height:100%;background:linear-gradient(#D97843,#A84932)}
.wrap{position:absolute;left:150px;right:150px;top:110px}
.kicker{font-size:40px;letter-spacing:4px;color:#3E9991;font-weight:700;text-transform:uppercase}
h1{font-size:96px;line-height:1.25;color:#E5B96E;margin-top:20px;font-weight:700}
h1 .sub{font-size:64px;color:#D97843}
ul{margin-top:56px;list-style:none}
li{font-size:46px;line-height:1.5;margin-bottom:26px;padding-left:56px;position:relative}
li:before{content:'';position:absolute;left:0;top:26px;width:26px;height:26px;background:#D97843;transform:rotate(45deg)}
.src{font-size:30px;color:#C8B894;opacity:.85}
li.nob{padding-left:0} li.nob:before{display:none}
.loop{display:flex;align-items:center;margin-top:80px;gap:0}
.step{flex:1;background:#363B3C;border:3px solid #E5B96E;border-radius:18px;padding:34px 14px;text-align:center;font-size:32px;line-height:1.45;min-height:200px;display:flex;align-items:center;justify-content:center}
.step b{color:#E5B96E}
.arrow{font-size:60px;color:#D97843;padding:0 14px}
.again{margin-top:46px;font-size:36px;color:#C8B894}
.cap{position:absolute;left:0;right:0;bottom:0;background:rgba(17,25,29,.92);border-top:4px solid #A84932;
 padding:26px 150px;font-size:38px;line-height:1.45;color:#fff}
.num{position:absolute;right:60px;top:40px;font-size:30px;color:#69524A}
"""


def slide_html(i, item):
    _, kicker, title, bullets, narration = item
    if bullets == ["LOOP"]:
        steps = "<span class='arrow'>&#9654;</span>".join(
            f"<div class='step'><div><b>{n + 1}</b><br>{s}</div></div>" for n, s in enumerate(LOOP))
        body = (f"<div class='loop'>{steps}</div>"
                "<div class='again'>ตายระหว่างบุก = เล่นขั้นบุกซ้ำ · Trouble เต็ม = ตำรวจเทาบุก · ครบ 3 คดี + บอส = ฉากจบ</div>")
    else:
        cls = " class='nob'" if len(bullets) == 1 and i == 0 else ""
        body = "<ul>" + "".join(f"<li{cls}>{b}</li>" for b in bullets) + "</ul>"
    return (f"<!doctype html><html lang='th'><head><meta charset='utf-8'><style>{CSS}</style></head><body>"
            f"<div class='band'></div><div class='num'>{i + 1}/{len(DECK)}</div>"
            f"<div class='wrap'><div class='kicker'>{kicker}</div><h1>{title}</h1>{body}</div>"
            f"<div class='cap'>{html.escape(narration)}</div></body></html>")


def render():
    os.makedirs(SLIDES, exist_ok=True)
    pngs = []
    for i, item in enumerate(DECK):
        page = os.path.join(SLIDES, f"slide_{i + 1:02d}.html")
        png = os.path.join(SLIDES, f"slide_{i + 1:02d}.png")
        with open(page, "w", encoding="utf-8") as f:
            f.write(slide_html(i, item))
        subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                        f"--screenshot={png}", "--window-size=1920,1080", "file:///" + page.replace("\\", "/")],
                       check=True, capture_output=True, timeout=60)
        pngs.append(png)
    return pngs


def video(pngs, voice=None):
    import cv2
    import imageio_ffmpeg
    fps = 25
    silent = os.path.join(HERE, "pitch_silent.mp4")
    tmp = os.path.join(HERE, "_frames.mp4")
    writer = cv2.VideoWriter(tmp, cv2.VideoWriter_fourcc(*"mp4v"), fps, (1920, 1080))
    for png, item in zip(pngs, DECK):
        frame = cv2.imread(png)
        for _ in range(item[0] * fps):
            writer.write(frame)
    writer.release()
    ff = imageio_ffmpeg.get_ffmpeg_exe()
    # Re-encode to H.264 so it plays everywhere (Drive preview, phones).
    subprocess.run([ff, "-y", "-loglevel", "error", "-i", tmp, "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "23", silent],
                   check=True)
    os.remove(tmp)
    if voice:
        out = os.path.join(HERE, "pitch.mp4")
        subprocess.run([ff, "-y", "-loglevel", "error", "-i", silent, "-i", voice, "-c:v", "copy", "-c:a", "aac",
                        "-shortest", out], check=True)
        return out
    return silent


if __name__ == "__main__":
    total = sum(item[0] for item in DECK)
    assert total <= 120, total
    out = video(render(), sys.argv[1] if len(sys.argv) > 1 else None)
    print(f"{out} ({total}s)")
