# ส่งต่องานให้ Codex: ทำ Scammer No Scamming ให้เป็น "เกมเต็ม"

เขียนโดย Claude (หลัก) 2026-10-23 ~20:45 · repo `D:\AI\scammer-no-scamming` · remote `LazyDay2024/Scammer-No-Scamming`

## 1. สถานการณ์และสิ่งที่เจ้าของงาน (chanawee) ตัดสินแล้ว

- ส่งเกม **25 ต.ค. 2026 12:00** (ลิงก์ itch.io แบบดาวน์โหลด Windows ต้องเล่นได้จริง + วิดีโอ 2 นาที) · นำเสนอ 19:00
- คลิปนำเสนอไอเดียส่ง **คืนนี้ 23 ต.ค. 23:00** (ทำเสร็จแล้ว อยู่ที่ `D:\AI\Scammer_No_Scamming_Pitch_Package.zip` ทีมเป็นคนอัปขึ้น Drive เอง ไม่ต้องทำต่อ)
- chanawee ลองเล่น build ล่าสุดแล้วบอกว่า **"ดูห่วยแตกมากและไม่ใช่เกมเลย"** อยากได้ **ภาพแนว Red Dead Redemption 2** และ **เกมเพลย์แบบ 3D interact ไม่ใช่ UI ง่อยๆ**
- **อนุมัติแล้ว**:
  - ทำเป็น "เกมเต็ม" ทั้งหมดเอง รวมแกนต่อสู้ (ข้อ 1-7 ที่เดิมเป็นของเพื่อน Nithis) เพื่อนจะมาปรับทีหลัง
  - ใช้/ดาวน์โหลดโมเดล **CC0** (ต้องบอกชื่อไฟล์ แหล่ง ขนาด เมื่อโหลดเพิ่ม)
  - ย้ายเข้าโลก 3D ทั้ง 3 ขั้น: **วิเคราะห์คดีในสถานี** (คุยผู้เสียหาย, การ์ดเบาะแสบนบอร์ด, เลือกคำตอบด้วยการเล็ง+E, ปักหมุดรังบนโต๊ะแผนที่ 3D), **แต่งยีนที่คอกม้า** (ขวดยีน 4 ขวดบนชั้น เล็ง+E ฉีดให้ม้า ม้าเรืองแสง), **บทเรียนเป็นแฟ้มคดี** วางบนโต๊ะให้เดินไปอ่าน
- เมนูหลัก/พักเกม/ฉากจบ ยังเป็น UI ได้ แต่ต้องสวย

## 2. ของที่มีแล้ว (อยู่บน main ยกเว้นที่บอก)

| ส่วน | ไฟล์ |
|---|---|
| ผู้เล่น เดิน/ขี่ม้า/ยิง hitscan, HitZone หัว/ตัว/เท้า (ของเพื่อน ห้ามรื้อ แต่ต่อยอดได้) | `Assets/Scripts/Player`, `Horse`, `Combat` |
| เมืองวงแหวนสร้างจากโค้ด (ของเพื่อน) | `Assets/Scripts/City/CityBuilder.cs` |
| ยีน (`GeneLoadout`, `RunModifiers`, `HorseGenes`) | `Assets/Scripts/Genes` |
| กับดัก 5 แบบ + `TrapLog` + `WeaponLock` | `Assets/Scripts/Traps` |
| ข้อมูลคดี JSON 3 คดี + บอส (ไทยเกลาแล้ว), `CaseDatabase`, `AnalysisSession`, `LessonPage`, `EndingEvaluator` | `Assets/Resources/Content/game_content.json`, `Assets/Scripts/Data`, `Analysis` |
| รัง `Hideout` + `CityLayoutPoints` (โซน/ทิศ) | `Assets/Scripts/World` |
| หน้าจอ IMGUI ภาษาไทย (`GameScreen`, `ThaiGui`, Analysis/Gene/Lesson/MainMenu/Pause/Story) | `Assets/Scripts/Screens` |
| ตัวเดินเรื่องชั่วคราว `CaseFlowDemo` + ฉาก `Assets/Scenes/CaseFlowDemo.unity` (สร้างด้วย `CaseFlowSceneBuilder`) | `Assets/Scripts/Dev`, `Assets/Editor` |
| อาร์ต AI (Codex): ภาพหน้าจอ/ภาพผู้เสียหาย/ไอคอน `Assets/Resources/UI`, เท็กซ์เจอร์ `Assets/Art/Textures/AI` (log: `docs/AI_ART_LOG*.md`) | |
| **branch `feature/visual-polish` (ยังไม่ merge)**: `WorldTour`, โมเดล CC0 Quaternius `Assets/ThirdParty/Quaternius` (ตัวละคร 17, ม้า, ของในบ้าน, ปืน/วิทยุ/โทรศัพท์) + `Assets/Editor/QuaterniusImport.cs` | |
| **worktree `D:\AI\snc-ai` branch `feature/npc-ai` (Codex exec กำลังเขียน/อาจเสร็จแล้ว ยังไม่ commit)**: AI ศัตรู/คนบริสุทธิ์ตามสเปก `Assets/Scripts/Npc/*` ดูสเปกเต็มที่หัวข้อ 6 | |

เทสต์ปัจจุบัน: EditMode 25/25, PlayMode 19/19 · Windows build ผ่าน (116 MB)

## 3. ปัญหาที่เห็นจากภาพในเกมจริง (WorldTour)

1. ลำแสงเหนือรังเป็น **สีชมพู** ใน build: `Hideout.CreatePillar` ใช้ default material (built-in shader ไม่มีใน URP) → ต้องใช้ material asset URP Unlit/Lit ที่สร้างใน editor
2. ประตูรังกลายเป็น **เสาบาง** (หมุนผิด) และรังไม่มีอาคาร/ห้องให้บุกจริง
3. ม้าเป็นกล่อง คนเป็นแคปซูล ป้าย/ตู้โทรศัพท์เป็นกล่องแปะภาพทุกด้าน
4. แสงแบน ไม่มี post-processing สถานีเป็นห้องโล่งผนังสีครีม
5. ไม่มีศัตรู/การยิงสู้/เลือดผู้เล่น/ความตาย = ไม่รู้สึกเป็นเกม

## 4. ข้อมูลโมเดล CC0 (ตรวจแล้ว)

- ตัวละคร Quaternius (Generic rig) ขนาดจริงในไฟล์สูง ~3.25 หน่วย → **scale ~0.55** ให้สูง ~1.8 ม. คลิปที่มี (ชื่อ `CharacterArmature|...`): `Idle`(loop), `Walk`(loop), `Shoot_OneHanded`, `RecieveHit`, `Defeat` (ใช้เป็นตาย), `Victory` (ยกแขน ใช้เป็นยกมือยอมแพ้), `SitDown` (ใช้เป็นหมอบกลัว), `StandUp`, `PickUp`, `Punch`, `Walk_Carry` · **ไม่มี Run** → ใช้ Walk เร่ง speed
- ม้า `Horse.fbx` bounds ~ (3.9, 7.9, 10.2) → **scale ~0.27** คลิป `Armature|Idle`, `Walk`, `WalkSlow`, `Run`, `Jump`, `Death`
- แนะนำบทบาท: สแกมเมอร์ติดอาวุธ = `Suit_Male`/`Casual3_Male`, ตำรวจเทา = `Soldier_Male`, ตำรวจจริง (สถานี) = `BlueSoldier_Male`, ลูกจ้างที่ถูกบังคับ = `Worker_Male/Female`, เหยื่อ/ชาวบ้าน = `Casual*`, นิดา = `Chef_Female`, ภูมิ = `Casual2_Male`, ลุงสมพร = `OldClassy_Male`, แพร = `Suit_Female`, คนปลอมตัว = `Soldier_Male` ย้อมสีเพี้ยน
- เล่นคลิปด้วย Playables (AnimationMixerPlayable crossfade) หรือสร้าง AnimatorController ใน editor script ก็ได้

## 5. แผนงานตามลำดับความสำคัญ (ทำให้รู้สึกเป็นเกมก่อน)

1. **แกนต่อสู้**: รวม `feature/npc-ai` (ตรวจ/แก้ให้ compile) + `NpcAnimator` (implements `INpcAnimation` ด้วยคลิปข้างบน) + `PlayerVitals` เลือด 100 ตาย → เล่นขั้นบุกซ้ำ (คงยีน/คำตอบไว้ ตามกติกาใน CONTEXT.md) + `HitFeedback` (hitmarker, เสียงปืน) + ศัตรูล้มแล้วจม/หาย
2. **รังที่บุกได้จริง**: ใน `CaseFlowSceneBuilder` สร้างคอมพาวด์ที่แต่ละ `Hideout` (ลบอาคารเมืองที่ทับในรัศมี ~18 ม.) เป็นห้องคอลเซ็นเตอร์มีผนัง ประตู โต๊ะ จอ (`tex_server_rack`, `tex_callcenter_wall`), ศัตรู 3-5 ตัว (บอสมากกว่า), ลูกจ้างยกมือ 2-3 คน (ห้ามยิง กด E ปล่อยตัว), หลักฐาน (`tex_evidence_drive` / โมเดล Battery_Big/Radio) ให้กด E เก็บ → กลับสถานี Seal · Bake NavMesh ด้วย `NavMeshSurface` (มีแพ็กเกจ AI Navigation แล้ว)
3. **ภาพแนว RDR2**: URP Volume (Tonemapping ACES, Bloom, Color Adjustments ส้มทอง/คอนทราสต์, White Balance อุ่น, Lift-Gamma-Gain เงาอมเขียวคราม, Vignette, Film Grain), กล้องเปิด postProcessing, ดวงอาทิตย์ต่ำ golden hour เงานุ่ม, หมอก exponential สีฝุ่น, อนุภาคฝุ่นลอย, เปลี่ยนม้าเป็นโมเดล Quaternius (ซ่อน renderer กล่องเดิม ผูก Animator กับ `HorseController.Speed`) และ NPC/กับดักเป็นโมเดลจริง (ป้าย = แผ่นบนเสา แปะภาพด้านหน้าเท่านั้น, ตู้โทรศัพท์ = กล่อง + quad ด้านหน้า), แก้ลำแสงสีชมพู
4. **3D interact**: ระบบเล็ง+E กลาง (raycast จากกล้อง, prompt เล็กๆ, ใช้ `Trap.IsOnFootInteract` กันชนกับปุ่มขึ้นม้า) แล้วทำ
   - สถานี: ตกแต่งด้วยโมเดล Interior (โต๊ะ ชั้น ไฟ) ผู้เสียหายยืนรอ คุยแล้วซับเรื่องขึ้นทีละประโยค, บอร์ดการ์ดเบาะแส 4 ใบ (TextMesh ฟอนต์ OS ไทย หรือ quad+ภาพที่เรนเดอร์ข้อความผ่าน Edge), คำถามเป็นป้ายคำตอบ 3 แผ่นให้ยิง/เล็ง+E, ข้อรังใช้โต๊ะแผนที่เมือง 3D มีหมุดตามโซน/ทิศ
   - คอกม้าหน้าสถานี: ชั้นขวดยีน 4 ขวด (สีตามไอคอน) เล็ง+E = ใส่/ถอดยีน ม้าเปลี่ยนออร่าสี, ป้ายบอกโควตา
   - หลัง Seal: แฟ้มคดีบนโต๊ะ เปิดอ่านบทเรียน (จะใช้ panel ใกล้กล้องในโลก 3D ก็ได้)
   - ใช้ตรรกะเดิม (`AnalysisSession`, `GeneLoadout`, `LessonPage`) ห้ามเขียนกติกาใหม่ซ้ำ
5. ศัตรูตำรวจเทาบุกเมื่อ Trouble เต็ม, HUD Trouble/เลือดแบบมินิมอล, เสียงประกอบ (สร้างในโค้ดได้)
6. ชื่อเกมใน Player Settings ยังเป็น "My project" → เปลี่ยนเป็น Scammer No Scamming (ได้รับอนุญาตให้ทำทั้งเกมแล้ว)

## 6. สเปก AI ที่ส่งให้ Codex exec ไปแล้ว (worktree `D:\AI\snc-ai`)

`INpcAnimation` (SetSpeed, Aim, Shoot, Die, HandsUp, Cower, Hit) + `NullNpcAnimation`; `Npc` (Faction, Health, static Shot/Killed, ศพจมหายใน 8 วิ); `ArmedEnemy` (Idle/Alert/Attack/Search/Dead, ได้ยินเสียงปืน, มองเห็นด้วย line of sight, ยิงด้วย HitscanWeapon ความแม่นเพิ่มใน 2 วิแรก, ดาเมต 8, รักษาระยะ 8-18 ม., `SightMultiplier` สำหรับยีนซ่อนตัว, ปลุกเพื่อนในรัศมี 25 ม.); `Bystander` (Victim หมอบ/หนี, ForcedWorker ยกมือ กด E ปล่อยตัว, event Freed); `RaidRoom` (Cleared); `PlayerVitals` (เลือด 100 ฟื้น 5/วิ หลัง 6 วิ จอแดง ตัวชี้ทิศโดนยิง event Died); `HitFeedback` (hitmarker + เสียงปืนสร้างในโค้ด) — สเปกเต็ม: `C:\Users\chanawee\AppData\Local\Temp\claude\D--AI\0bb9c3e8-ec83-4131-92fb-ba4bbee524cc\scratchpad\codex_ai.md` log: `codex_ai.log` ในโฟลเดอร์เดียวกัน

## 7. วิธีตรวจผล (ใช้ทุกครั้ง อย่าอ้างว่าเสร็จจากแค่ compile)

- Unity: **`D:\6000.3.6f1\Editor\Unity.exe` เท่านั้น** (อย่าเปิดด้วย 6000.3.12f1) · ถ้า Editor เปิดโปรเจกต์อยู่ ห้ามรัน batchmode ซ้อน
- เทสต์: `Unity.exe -batchmode -nographics -projectPath D:/AI/scammer-no-scamming -runTests -testPlatform EditMode|PlayMode -testResults <xml> -logFile <log>`
- สร้างฉาก: `-batchmode -quit -executeMethod CaseFlowSceneBuilder.Build` · build: `-executeMethod WindowsBuild.Build` → `Builds/Windows/`
- **ดูภาพจริง**: `Builds/Windows/ScammerNoScamming.exe -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -screentour <dir>` (11 หน้าจอ UI) และ `-worldtour <dir>` (ถ่ายทุกกับดัก/รัง/สถานี) แล้วเปิดภาพดูเองทุกครั้ง
- หลัง batchmode Unity มักแก้ `ProjectSettings/*.asset`, `Assets/Settings/*` เอง → `git checkout --` ไฟล์ที่ไม่ได้ตั้งใจแก้ก่อน commit

## 8. กฎ/ข้อควรระวัง

- ข้อความไทยทุกอย่างที่คนอ่าน ต้องฟังเป็นภาษาคนเล่าเรื่องต่อเนื่อง (เจ้าของงานเคยติว่า "ไม่ลื่น ไม่เหมือนภาษามนุษย์") ให้ใช้ข้อความใน `game_content.json` ที่เกลาแล้ว ถ้าเขียนใหม่ให้เกลาและตรวจกันก่อน
- ไทยในภาพ: PIL ในเครื่องไม่มี raqm และ Python ใน sandbox ของ Codex ถูกบล็อก → วาดไทยผ่าน headless Edge (`msedge --headless=new --screenshot --default-background-color=00000000`) แบบใน `docs/pitch/build_pitch.py`; ในเกมใช้ `ThaiGui.Font` (ฟอนต์ OS Leelawadee UI) ซึ่งแสดงผลถูกแล้วใน build
- อาร์ต AI ใช้ได้กับ 2D (เท็กซ์เจอร์/UI/อ้างอิง) และจด log; โมเดลตัวละคร 3D ใช้ CC0 ของ Quaternius (ใส่เครดิต)
- commit บ่อย push branch ของตัวเอง แล้ว merge เข้า main หลังเทสต์ผ่าน + ดูภาพจาก tour แล้ว (chanawee อนุญาตให้ push/merge main) ห้าม force-push
- ตัวเลขทางการที่ใช้ได้มีแค่: 121,921 คดี / 7.48 พันล้านบาท (ACSC ม.ค.-เม.ย. 2569 ผ่าน Thai PBS Verify), สายด่วน 1441, thaipoliceonline.go.th
