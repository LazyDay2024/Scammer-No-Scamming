# AI art log — UI scenes and victim portraits

วันที่สร้าง: 2026-10-23 · เกม Scammer No Scamming · ผู้ผลิต: Codex

## ขอบเขตและแนวภาพ

ใช้เป็นภาพ 2D สำหรับ UI เท่านั้น ไม่ใช่โมเดลตัวละคร 3D สุดท้าย อ่าน `docs/content/CONTENT_DRAFT.md` และเทียบภาพโลกใน `docs/pitch/art/` กับ texture เดิมใน `Assets/Art/Textures/AI/`.

ทุก prompt ใช้ stylised low-poly, flat-shaded triangular facets และ painterly cinematic light; palette orange `#D97843`, rust `#A84932`, gold `#E5B96E`, teal `#1E6C70` / `#3E9991`, charcoal `#363B3C`, bone `#E7D9B8`, near-black `#11191D`. โลกสยามภิรมย์ปี 2035 แนว Western ผสมไทย dystopia; ไม่เยาะเย้ยผู้เสียหาย. ฉากทั้งหกเว้นซ้าย 45% ให้สงบและมืดสำหรับ UI. ทุกภาพห้ามมีตัวอักษร ตัวเลข โลโก้ข้อความ หรือ watermark จึงไม่ต้องวางข้อความไทยด้วย PIL.

## ไฟล์ส่งมอบ

เครื่องมือสร้างทุกภาพ: **built-in `image_gen.imagegen`** (ไม่มี API/CLI แบบคิดค่าบริการแยก). ปรับขนาดและ encode PNG ด้วย **PowerShell / System.Drawing, HighQualityBicubic + TileFlipXY** เท่านั้น; ไม่วาดแก้เนื้อหาด้วยโค้ด. ภาพฉากต้นฉบับ 1672×941 ปรับเป็น 1920×1080 ตามข้อยกเว้นที่ผู้ใช้ระบุ; portraits ปรับเป็น power-of-two 512×512. ไม่ได้เพิ่ม alpha เพราะไม่มีรายการใดขอพื้นโปร่งใส.

| Path | ขนาด | Purpose / prompt summary | Tool |
|---|---|---|---|
| `Assets/Art/UI/AI/bg_main_menu.png` | 1920×1080 | เมนู: สารวัตรบนม้าสีน้ำตาลบนสันเขายามค่ำ เมืองวงแหวนดับไฟ หอบอสเรืองทอง; แก้หอเป็นยอดแยกและสัญลักษณ์เมฆเล็กที่ผนังให้ต่อเนื่องกับฉากอื่น | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/bg_opening.png` | 1920×1080 | เปิดเรื่อง: ถนนตึกแถวไทย รถจอดไร้น้ำมัน สายไฟและไฟถนนดับ ม้าหนึ่งตัวดื่มน้ำจากรางด้านขวา หอบอสไกลออกไป | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/bg_case_board.png` | 1920×1080 | สถานีตำรวจ: กระดานคอร์ก ภาพหลักฐานพัสดุ/อิฐ/โทรศัพท์/บ้าน/หอคอย/เมือง/สายไฟ เชื่อมด้วยด้ายแดง ตะเกียงบนโต๊ะ ไม่มีตัวหนังสือ | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/bg_lesson.png` | 1920×1080 | บทเรียน: เมืองวงแหวนยามอรุณ แสงทองนุ่ม หมอกเขียวอมฟ้า ระเบียงและต้นไม้เล็กทางขวา สงบและมีความหวัง | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/bg_ending_clean.png` | 1920×1080 | จบสะอาด: ไฟถนนและบ้านกลับมาสว่างทีละแนวในยามเช้า คนช่วยเหลือกัน ม้าสงบ หอบอสยอดแยกดับไฟ; แก้รูปทรงหอให้เข้าชุด | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/bg_ending_messy.png` | 1920×1080 | จบเละ: เมืองกลางคืน หอบอสยังเรืองทอง รถพยาบาลจอด ไฟฉุกเฉินแดง/teal กระดาษหลักฐานปลิว ไม่มีผู้บาดเจ็บหรือความรุนแรง | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/portrait_nida.png` | 512×512 | นิดา: หญิงไทยวัยทำงานร้านกาแฟ เสื้อ bone ผ้ากันเปื้อน rust ผมมวย ถืออิฐจากพัสดุ หงุดหงิดและเจ็บใจอย่างมีศักดิ์ศรี | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/portrait_phum.png` | 512×512 | ภูมิ: นักศึกษาชายไทย hoodie charcoal/teal มองโทรศัพท์ ก้มหน้าอย่างละอายและกังวล ไฟอุ่น ไม่มีข้อความบนมือถือ | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/portrait_somporn.png` | 512×512 | ลุงสมพร: ข้าราชการเกษียณ ผมเงิน แว่น เสื้อเชิ้ต bone ถือโทรศัพท์ปุ่มกดเก่า สีหน้ากังวลแต่มีศักดิ์ศรี ปุ่มไม่มีตัวเลข | image_gen.imagegen; System.Drawing resize |
| `Assets/Art/UI/AI/portrait_prae.png` | 512×512 | แพร: พนักงานบริษัทไทย blazer charcoal ท่าทีเสียใจหมดแรง ถือโทรศัพท์ที่แสดงเพียงกราฟพุ่งขึ้นเรือง teal ไม่มีข้อความหรือตัวเลข | image_gen.imagegen; System.Drawing resize |

## ต้นฉบับจากเครื่องมือและการสร้างซ้ำ

เครื่องมือบันทึกต้นฉบับอัตโนมัติไว้นอก project ที่ directory ด้านล่าง ไม่ใช่ไฟล์นำเข้าเกม. ทุกรุ่นถูกเปิดตรวจแล้ว; รุ่นที่ไม่เลือกไม่ถูกนำเข้า Assets.

Directory: `C:/Users/chanawee/.codex/generated_images/01a16896-29df-7e91-b13b-dc57985eaf95/`

| Generated file (ต่อจาก directory ข้างบน) | Purpose / prompt summary | Tool / สถานะ |
|---|---|---|
| `exec-66750da4-bb23-4e7f-8d4b-9b0c36bf7ee3.png` | เมนูรุ่นแรกตามสรุปข้างบน แต่ยอดหอเป็นรูปคล้ายเปลว/ศาลา จึงสร้างแก้เฉพาะหอ | image_gen.imagegen; ไม่เลือก — ใช้ v2 แทน |
| `exec-419a34d8-b78f-4ec9-a937-e2d1786f5a7d.png` | เปิดเรื่อง: ถนนตึกแถวไทย รถจอดไร้น้ำมัน สายไฟและไฟถนนดับ ม้าหนึ่งตัวดื่มน้ำจากรางด้านขวา หอบอสไกลออกไป | image_gen.imagegen; เลือกส่งมอบ |
| `exec-7cd99d2d-2097-4770-8f3d-3d9cebe00da5.png` | สถานีตำรวจ: กระดานคอร์ก ภาพหลักฐานพัสดุ/อิฐ/โทรศัพท์/บ้าน/หอคอย/เมือง/สายไฟ เชื่อมด้วยด้ายแดง ตะเกียงบนโต๊ะ ไม่มีตัวหนังสือ | image_gen.imagegen; เลือกส่งมอบ |
| `exec-c9ce5d96-a2f0-4293-af06-8531d72192a7.png` | บทเรียน: เมืองวงแหวนยามอรุณ แสงทองนุ่ม หมอกเขียวอมฟ้า ระเบียงและต้นไม้เล็กทางขวา สงบและมีความหวัง | image_gen.imagegen; เลือกส่งมอบ |
| `exec-fbc0375b-9163-42d0-9fc6-20b7a999ae13.png` | ฉากจบสะอาดรุ่นแรกตามสรุปข้างบน แต่ยอดหอทรงกลมคล้ายเห็ด จึงสร้างแก้เฉพาะหอ | image_gen.imagegen; ไม่เลือก — ใช้ v2 แทน |
| `exec-a796e574-67c9-466d-b884-8bb921ceb87e.png` | จบเละ: เมืองกลางคืน หอบอสยังเรืองทอง รถพยาบาลจอด ไฟฉุกเฉินแดง/teal กระดาษหลักฐานปลิว ไม่มีผู้บาดเจ็บหรือความรุนแรง | image_gen.imagegen; เลือกส่งมอบ |
| `exec-1e827e39-4376-44f4-9189-f17aad0419bd.png` | จบสะอาด: ไฟถนนและบ้านกลับมาสว่างทีละแนวในยามเช้า คนช่วยเหลือกัน ม้าสงบ หอบอสยอดแยกดับไฟ; แก้รูปทรงหอให้เข้าชุด | image_gen.imagegen; เลือกส่งมอบ |
| `exec-271aa831-6065-41e2-a854-043ea9681b95.png` | นิดา: หญิงไทยวัยทำงานร้านกาแฟ เสื้อ bone ผ้ากันเปื้อน rust ผมมวย ถืออิฐจากพัสดุ หงุดหงิดและเจ็บใจอย่างมีศักดิ์ศรี | image_gen.imagegen; เลือกส่งมอบ |
| `exec-ac3a0999-4828-4128-b8e4-b27efdf29d72.png` | ภูมิ: นักศึกษาชายไทย hoodie charcoal/teal มองโทรศัพท์ ก้มหน้าอย่างละอายและกังวล ไฟอุ่น ไม่มีข้อความบนมือถือ | image_gen.imagegen; เลือกส่งมอบ |
| `exec-970a23eb-1637-42c6-82a4-0d2be856acf5.png` | ลุงสมพร: ข้าราชการเกษียณ ผมเงิน แว่น เสื้อเชิ้ต bone ถือโทรศัพท์ปุ่มกดเก่า สีหน้ากังวลแต่มีศักดิ์ศรี ปุ่มไม่มีตัวเลข | image_gen.imagegen; เลือกส่งมอบ |
| `exec-d62f2571-e33f-49f5-876f-311ec3c5e6c6.png` | แพร: พนักงานบริษัทไทย blazer charcoal ท่าทีเสียใจหมดแรง ถือโทรศัพท์ที่แสดงเพียงกราฟพุ่งขึ้นเรือง teal ไม่มีข้อความหรือตัวเลข | image_gen.imagegen; เลือกส่งมอบ |
| `exec-50da5c00-f4b0-4544-9b49-b22716ab8da4.png` | เมนู: สารวัตรบนม้าสีน้ำตาลบนสันเขายามค่ำ เมืองวงแหวนดับไฟ หอบอสเรืองทอง; แก้หอเป็นยอดแยกและสัญลักษณ์เมฆเล็กที่ผนังให้ต่อเนื่องกับฉากอื่น | image_gen.imagegen; เลือกส่งมอบ |

## การตรวจและข้อจำกัด

- เปิดดูผลการสร้างทุกครั้ง ตรวจสี ข้อความ กายวิภาค และการอ่านภาพ; สร้างแก้หอคอยเมนูและจบสะอาดด้วย imagegen โดยใช้ฉากกลางคืนเป็น reference.
- ตรวจภาพฉากแยกโดย image-reviewer: ทั้งหกผ่านเรื่อง palette/style, ไม่มีข้อความ, พื้นที่ซ้าย 45% และ anatomy ที่ขนาดแสดงผล; ข้อเสนอแก้ยอดหอเมนูถูกนำไปแก้แล้ว.
- ผลตรวจ independent รอบสุดท้าย: เมนูที่แก้หอคอยแล้วผ่านความต่อเนื่อง; portraits ทั้งสี่ผ่านกายวิภาคใบหน้า/มือ/props, อารมณ์ที่ให้เกียรติ, ไม่มีข้อความ และอ่านตัวละครกับ props ได้ที่ 128×128. รอบนี้ reviewer ใช้ contact sheet ใน memory ไม่สร้างไฟล์เพิ่ม.
- ตรวจไฟล์ PNG และขนาดจริงด้วย System.Drawing. ไม่รัน Unity ไม่แก้ scenes/prefabs/scripts ไม่สร้าง .meta และไม่ commit; ยังไม่ได้เห็นภาพซ้อน UI จริงในเกม.
- Environment นี้เรียก `python` ไม่ได้; ใช้ System.Drawing สำหรับปรับขนาดแทน ไม่มีการเพิ่มฟอนต์หรือ dependency เข้า project.

## ไฟล์ตรวจชั่วคราวที่ reviewer สร้างและลบแล้ว

Reviewer มีข้อกำหนดให้ตรวจผ่าน contact sheet และสร้างไฟล์ชั่วคราวสองไฟล์นอก project ในรอบแรก; บันทึกเพื่อให้ครบทุกไฟล์ แม้ไม่ใช่ deliverables. ไม่มีไฟล์เหล่านี้ถูกนำเข้า Assets และ reviewer ยืนยันลบแล้ว. รอบถัดไปใช้ memory เท่านั้น.

| Path | Purpose | Tool | Prompt summary / สถานะ |
|---|---|---|---|
| `C:/Users/chanawee/AppData/Local/Temp/snc-review-sheet.png` | Contact sheet ฉากทั้งหกสำหรับ independent visual QA | PowerShell / System.Drawing | จัดภาพย่อและป้ายชื่อจากภาพเดิม ไม่มี generation prompt; ลบแล้ว |
| `C:/Users/chanawee/AppData/Local/Temp/snc-ref-sheet.png` | Contact sheet ภาพอ้างอิงโลกสำหรับ independent visual QA | PowerShell / System.Drawing | จัดภาพย่อจาก pitch references ไม่มี generation prompt; ลบแล้ว |

ไฟล์ log นี้: `docs/AI_ART_LOG_UI_SCENES.md` · Purpose: provenance, prompt summaries, iterations และ QA evidence · Tool: Codex / apply_patch · Prompt summary: บันทึกทุกไฟล์ในงาน UI scenes/portraits ตามคำสั่งผู้ใช้.

