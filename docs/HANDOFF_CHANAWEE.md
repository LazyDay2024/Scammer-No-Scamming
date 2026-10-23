# ส่งต่องานฝั่ง chanawee (ข้อ 9-13, 15 + อาร์ต)

ปรับล่าสุด: 2026-10-23 · อ่านคู่กับ [PLAN.md](../PLAN.md) และ [CLASS_DIAGRAM.md](CLASS_DIAGRAM.md)

## ลองเล่นได้ทันที

1. เปิดโปรเจกต์ด้วย Unity **6000.3.6f1**
2. เมนู **Tools > Scammer No Scamming > Build Case Flow Demo Scene** จะสร้าง `Assets/Scenes/CaseFlowDemo.unity`
3. เปิดฉากนั้นแล้วกด Play: เมนู → เปิดเรื่อง → วิเคราะห์คดี → แต่งยีน → ขี่ม้าไปรัง (ลำแสงสีทอง) → กด E ค้างที่ประตูรัง → กลับสถานี กด E ค้างที่โต๊ะ Seal → บทเรียน → คดีถัดไป → ฉากจบ · Esc = พักเกม

ฉากนี้เป็นของ chanawee ไม่แตะ CitySandbox / PlayerSandbox

## มีอะไรแล้ว

| ข้อ | สิ่งที่ทำ | ไฟล์หลัก |
|---|---|---|
| 9 วิเคราะห์ | เรื่องผู้เสียหาย + การ์ดเบาะแส + คำถาม 3 ข้อ ตอบผิด Trouble เพิ่ม ข้อสุดท้ายตอบถูก = เปิดตำแหน่งรัง | `Analysis/AnalysisSession.cs`, `Screens/AnalysisScreen.cs` |
| 10 แผนที่ | รัง 4 แห่งตามโซน/ทิศในเนื้อหาคดี ตอบถูกแล้วมีลำแสงเหนือรัง | `World/Hideout.cs`, `World/HideoutPlacer.cs` (`CityLayoutPoints`) |
| 11 ยีน | 4 ยีน ปกติ 2 สูงสุด 4 เกินแล้ว Trouble เพิ่ม หน้าเลือกยีน | `Genes/*`, `Screens/GeneSelectionScreen.cs` |
| 12 กับดัก | ครบ 5 แบบ ทุกอันมีสัญญาณให้สังเกต | `Traps/*` |
| 13 บทเรียน | ประเภทสแกม สัญญาณเตือน วิธีรับมือ + อ้างถึงกับดักที่เพิ่งโดน + สายด่วน | `Analysis/LessonPage.cs`, `Screens/LessonScreen.cs` |
| 15 เมนู/ฉากจบ | เมนูหลัก (ความไวเมาส์), พักเกม, เปิดเรื่อง, ฉากจบ 2 แบบ | `Screens/*` |
| เนื้อหา | 3 คดี + บอส, ข้อความทั้งหมดในเกม ภาษาไทยเกลาแล้ว | `Resources/Content/game_content.json` |
| อาร์ต | เท็กซ์เจอร์/ไอคอน/ภาพหน้าจอ ทำโดย AI ตามกฎทีม (บันทึกใน `docs/AI_ART_LOG*.md`) ภาพหน้าจออยู่ใน `Resources/UI` | `Assets/Art/*/AI`, `Assets/Resources/UI` |

เทสต์: EditMode 25/25 และ PlayMode 19/19 ผ่านใน batchmode · Windows build ผ่าน (116 MB) · ตรวจหน้าจอจริงจาก build ด้วยโหมดทัวร์ (ภาษาไทยแสดงถูก) **ยังไม่มีคนลองเล่นจนจบด้วยมือ**

## Build Windows / ตรวจหน้าจอ

- เมนู **Tools > Scammer No Scamming > Build Windows** หรือ batch: `Unity.exe -batchmode -quit -projectPath . -executeMethod WindowsBuild.Build` ได้ `Builds/Windows/` (zip ทั้งโฟลเดอร์ขึ้น itch.io)
- ตรวจทุกหน้าจอโดยไม่ต้องกดเอง: `Builds/Windows/ScammerNoScamming.exe -screen-fullscreen 0 -screentour <โฟลเดอร์>` จะจับภาพ 11 หน้าจอแล้วปิดเอง
- `CaseFlowDemo.unity` ถูกตั้งเป็นฉากแรกใน Build Settings แล้ว ถ้าเพื่อนมีฉากหลักของตัวเอง ย้ายลำดับได้
- ชื่อเกมใน Player Settings ยังเป็น "My project" (หน้าต่างเกมขึ้นชื่อนี้) ควรเปลี่ยนเป็น Scammer No Scamming ก่อนส่ง ฉันไม่แก้ ProjectSettings ของเพื่อนเอง

## จุดเชื่อมกับงานเพื่อน (ข้อ 1-7, 14)

`Dev/CaseFlowDemo.cs` คือตัวแทนชั่วคราวของ `GameManager` ให้เล่นได้ก่อน เมื่อของจริงมา ให้ย้ายการเรียกเหล่านี้ไปไว้ใน phase ของเพื่อน แล้วลบ demo ทิ้งได้:

| เพื่อนทำ | เรียกของฝั่ง chanawee |
|---|---|
| `GameManager.StartRun` | `CaseDatabase.LoadDefault()` แล้ว `db.PickRun(3, seed)` (คดีสอนเล่นมาก่อน บอสอยู่ท้าย) |
| `AnalysisPhase` | `analysisScreen.Show(case, troublePerWrong, result => ...)` แล้วถ้า `result.IsUnlocked(IntelKind.HideoutLocation)` ให้ `Hideout.Find(case.HideoutId).Reveal()` · `result.TroublePenalty` เข้า `TroubleMeter.SetBase` |
| `PlanningPhase` | `geneScreen.Show(GeneCatalog.CreateDefault(), loadout, l => ...)` → `l.BuildModifiers()` ใส่ `HorseGenes.Apply`, `PlayerCharacter` (`PlayerMaxHealth`, `DamageTaken`), ศัตรู (`EnemySightRange`), Trouble (`TroubleGain`) · `l.OverBudgetTrouble` เข้า Trouble ฐาน |
| `ActionPhase` เริ่ม | `TrapLog.Reset()` · ฟัง `Trap.Sprung` ถ้าทีมตกลงให้โดนกับดักแล้ว Trouble ขึ้น · `SupplyCrate.Collected` = เติมเลือด |
| Seal หลักฐานเสร็จ (ข้อ 7) | `lessonScreen.Show(LessonPage.Build(case, db.Text, TrapLog.SprungKinds, TrapLog.DefusedKinds), next)` |
| `EndingPhase` | `EndingEvaluator.Evaluate(run.InnocentsHit, run.TotalTrouble)` แล้วใช้ `StoryScreen` กับ `db.Text.EndingClean/EndingMessy` |

หมายเหตุ:

- หน้าจอเป็น IMGUI ใช้ฟอนต์ Leelawadee UI ของ Windows (ไม่ต้องแนบไฟล์ฟอนต์ เพราะเกมส่งเป็น Windows build) ตั้งชื่อฐานว่า `GameScreen` เพื่อไม่ชนกับ `UIScreen` ของข้อ 14 ถ้าเพื่อนทำ uGUI เสร็จแล้วอยากย้าย ข้อมูลและตรรกะแยกจากหน้าจอแล้ว เปลี่ยนแค่ส่วนวาด
- ระหว่างหน้าจอเปิด เกมหยุด (`Time.timeScale = 0`) ปิด `PlayerControls` และปลดเมาส์
- กับดัก/ยีน/วิเคราะห์ไม่เรียก Trouble/UI/ศัตรูตรงๆ ตามกฎ dependency ในเอกสาร
- ค่าที่ยังไม่ตัดสินใจเป็นพารามิเตอร์: Trouble ต่อคำตอบผิด (10), ต่อยีนที่เกิน (20), เกณฑ์ฉากจบเละ (100), ดาเมตกับดัก
- การเก็บหลักฐานกับ Seal ใน demo เป็นของชั่วคราว (กด E ค้าง) ข้อ 7 ของเพื่อนแทนที่ได้เลย
