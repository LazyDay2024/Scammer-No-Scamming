# AI Art Log

## 2026-10-23 - Scammer No Scamming

Scope: AI-generated in-game textures/decals and reference images only. Final character models remain human-made. Read CONTEXT.md and PLAN.md; visually inspected existing tex_facade_mid.png for flat, orthographic, warm-weathered style.

Tool for all 11 PNGs: built-in image_gen (one generation per asset), followed by Python Pillow resize, text placement and PNG optimization. QR matrix: ReportLab QrCodeWidget, rendered with Pillow. No API/CLI generation fallback, no font copied into project.

Shared prompt: Thai dystopian western, year 2035; low-poly / flat-shaded broad forms; no watermark, no generated letters or numbers. Texture surfaces front-on orthographic; transparent alpha explicitly requested for decals. References are for a human artist only.

Palette: Rust #A84932, Orange #D97843, Gold #E5B96E, Umber #69524A, Charcoal #363B3C, Teal #1E6C70, PaleTeal #3E9991, Stone #C8B894, Bone #E7D9B8, Black #11191D. Generated shading/wear includes tonal variations.

All lettering, including Thai, was drawn afterward in Pillow using C:/Windows/Fonts/LeelawUI.ttf. Output was opened and visually checked after lettering.

| File | Dimensions | Bytes | What it is / prompt summary |
|---|---:|---:|---|
| `Assets/Art/Textures/AI/decal_gang_cloud.png` | 512 x 512 | 335098 | Transparent angular cloud wrapped around square-hole gold coin; charcoal/gold/rust; strong small-scale gang silhouette. |
| `Assets/Art/Textures/AI/decal_worker_tag.png` | 512 x 512 | 238274 | Transparent worn bone lanyard card, frayed teal strap, broken-chain pictogram; unarmed trafficking-victim identity, no criminal/gang imagery. PIL: แรงงานบังคับ and 0427. |
| `Assets/Art/Textures/AI/decal_police_seal.png` | 512 x 512 | 345713 | Transparent round fictional Siam Phirom police badge, empty teal ring, gold shield/scales/sun, no real official insignia. PIL: ตำรวจ / สยามภิรมย์. |
| `Assets/Art/Textures/AI/tex_supply_box_real.png` | 512 x 512 | 409417 | Orthographic clean wood crate face, orderly teal logistics stripes, blank centre and stencil panel. PIL composites the same police seal and เสบียงทางการ. |
| `Assets/Art/Textures/AI/tex_supply_box_fake.png` | 512 x 512 | 471062 | Orthographic flashy rust/orange/gold crate, crooked rough brush-painted bone sign, overpaint and wear, no official seal. PIL: ฟรี! ด่วน! / เหลือ 1 กล่อง. |
| `Assets/Art/Textures/AI/tex_scam_poster.png` | 512 x 1024 | 720163 | Portrait worn fake-job poster, rust headline banner, phone/coin icon, large empty claim and QR panel. PIL: งานออนไลน์ / รายได้วันละ / 5,000; link and QR. |
| `Assets/Art/Textures/AI/tex_server_rack.png` | 512 x 512 | 335727 | Orthographic charcoal server-rack front, broad vents, bolts, bundled teal/rust cables and teal/gold status-light dots. No generated lettering. |
| `Assets/Art/Textures/AI/tex_evidence_drive.png` | 512 x 512 | 302571 | Front-on evidence smartphone, charcoal casing, cracked corner, three empty chat bubbles, transfer icon and warning triangle. PIL: โอนก่อนเริ่มงาน / ต้องจ่ายอีก? / ด่วน! ภายใน 5 นาที. |
| `Assets/Art/Textures/AI/tex_callcenter_wall.png` | 512 x 512 | 426777 | Front-on cramped call-centre wall, stone plaster, rust wear, taped blank scripts, charcoal quota board, cable conduit. PIL adds oppressive call scripts, quota numbers, and ห้ามพัก / จนกว่าจะครบ. |
| `docs/art-reference/ref_characters.png` | 1024 x 1024 | 1120691 | Human-artist reference ONLY: four adult low-poly silhouettes side by side: rust armed scammer, pale slumped unarmed forced worker, teal crouching scared victim, charcoal/gold mounted corrupt police. PIL English labels. |
| `docs/art-reference/ref_disguised_scammer.png` | 512 x 512 | 230709 | Human-artist reference ONLY: villager-disguised scammer in ragged stone/umber clothing, fixed direct gaze, gold watch, cloud/coin tattoo on exposed right forearm. PIL reference title and tells caption. |

### Gameplay and reference notes

- Forced workers are trafficking victims: pale worn clothing, protective/slumped posture, plain number card; no weapon or gang badge. The crouching teal victim and armed rust scammer also differ in posture and silhouette. The reference sheet is not an in-game character asset.
- Disguised-scammer tells observed in the result: (1) fixed direct stare, (2) conspicuously expensive gold watch peeking from ragged clothing on the left wrist, (3) dark cloud/coin gang tattoo on the exposed right forearm. The tattoo is a concept variation, not an exact reproduction of decal_gang_cloud.
- Poster link deliberately transposes Siam to saiam: saiam-job.invalid. QR payload: https://saiam-job.invalid. The domain is a fictional non-live destination; no real scam recipient or tracking link. QR was built from a standard matrix, but not scanned using a phone.
- Server LEDs are painted static highlights; PNG contains no animation.

### Verification and limitations

- Opened the contact sheet and final Thai-lettered assets; checked wording, label placement and reference silhouettes. Adjusted seal lettering into ring, moved poster URL into its panel, moved final quota line above border, and added margins around disguised-reference captions.
- Automated checks: all 11 files are PNG; expected power-of-two dimensions; all three decals have real alpha from 0 to 255 with transparent corners; file-size budget checked.
- Not run: Unity, scene/material integration, mipmap or in-game-distance readability tests, gameplay discrimination tests. References show the intended distinctions; final human-made models still need gameplay validation.
- Local tooling note: python was absent from PATH; used existing bundled Python at C:/Users/chanawee/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe with Pillow. No installation was needed.

### Other files created

- `docs/AI_ART_LOG.md` - this provenance and verification record; tool: Python standard library; source: task requirements, actual prompts, file checks and visual observations.
- `docs/art-reference/_qa_contact.png` - temporary montage created and refreshed with Python Pillow for visual inspection; removed after review, not a deliverable.

Image deliverables total: 4936202 bytes. No Unity .meta files created. No scenes, prefabs or scripts modified. No Unity run, commit or push.
