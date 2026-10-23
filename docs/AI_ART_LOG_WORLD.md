# AI art log — world textures

Date: 2026-10-23. Project: Scammer No Scamming / fictional Siam Phirom, 2035.

All assets are **2D textures only**. Final 3D character models remain human-made. Reference inputs: `docs/content/CONTENT_DRAFT.md`, `docs/pitch/art/04_concept.png`, `docs/pitch/art/06_trap.png`, existing `tex_supply_box_fake.png`, `decal_gang_cloud.png`, and `decal_police_seal.png`.

Shared prompt: front orthographic, stylised low-poly flat-shaded facets, painterly worn surfaces and cinematic orange/teal lighting; orange #D97843, rust #A84932, gold #E5B96E, teal #1E6C70/#3E9991, charcoal #363B3C, bone #E7D9B8, near-black #11191D. Generate without text. No real-world insignia, people, character models, watermark or pseudo-writing.

## Project files created

Tool: built-in `image_gen.imagegen` for every art asset; PowerShell / .NET System.Drawing for exact-size high-quality bicubic PNG export. Python 3.11 executable exists, but sandbox returned `Access is denied`; no API fallback, package install or sandbox bypass attempted. Thai typography is pending the user's answer on using .NET with the supplied Windows font instead of Pillow.

| Path | Size | Purpose and prompt summary |
|---|---|---|
| `Assets/Art/Textures/AI/tex_phone_booth.png` | 1024×1024 | Old public phone front; cracked glass, payphone, anonymous glowing caller screen. |
| `Assets/Art/Textures/AI/tex_sign_rent_horse.png` | 1024×512 | Hand-painted teal/bone alley sign with horseshoe and coins; three blank bands reserved for exact Thai lure. |
| `Assets/Art/Textures/AI/tex_sign_phishing.png` | 1024×512 | Scam ad panel with warning triangle, blank dark warning screen and bone URL bar; revised cyan glow to muted teal. |
| `Assets/Art/Textures/AI/tex_money_bag.png` | 512×512 | Full-bleed faceted canvas sack, rust drawstring, coin bulges and square-hole coin emblem. |
| `Assets/Art/Textures/AI/tex_uniform_real_police.png` | 1024×1024 | Trustworthy dark teal uniform fabric, tidy seams, gold scales badge and name plate. |
| `Assets/Art/Textures/AI/tex_uniform_fake_police.png` | 1024×1024 | Matching imitation uniform: wrong grey-green cloth, cheap peeling crooked badge, uneven scales, deliberately blank name tag. |
| `Assets/Art/Textures/AI/tex_shirt_forced_worker.png` | 1024×1024 | Plain grey faceted worn cotton with blank number-tag area; revised from coarse sack weave to shirt cotton; tileable. |
| `Assets/Art/Textures/AI/tex_outfit_scammer.png` | 1024×1024 | Rust/black tactical chest cloth, zipper and pockets, small curling cloud and square-hole gold coin gang patch. |
| `Assets/Art/Textures/AI/tex_clothes_victim.png` | 1024×1024 | Ordinary teal/orange/bone cotton plaid; no gang or official markings; tileable. |
| `Assets/Art/Textures/AI/tex_hideout_door.png` | 1024×1024 | Reinforced teal/rust shophouse double door, heavy lock, stolen-power cables, small camera. |
| `Assets/Art/Textures/AI/tex_evidence_seal_table.png` | 1024×1024 | Orthographic station counter work surface, empty evidence mat, rust tamper tape, scales seal and empty stamp circle. |
| `Assets/Art/Textures/AI/tex_wanted_board.png` | 1024×1024 | Station wall board, exactly four empty bone wanted-poster frames, small scales symbol, no text. |
| `docs/AI_ART_LOG_WORLD.md` | Markdown | Provenance, prompts, processing, review evidence and limitations. Tool: apply_patch; manually authored inventory, no generation prompt. |

## Built-in generator source files

These PNGs were created automatically by the image tool in its default output folder, outside the repository. They are recorded here as provenance; no extra intermediate project files were created.

| Source PNG path | Destination / status |
|---|---|
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-eba235f7-1a3c-41e4-9622-cf2fd09f3983.png` | `tex_phone_booth.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-9af10516-512b-4168-bc16-ed65f079850d.png` | `tex_sign_rent_horse.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-5201a58d-4bb1-4562-a0d0-d38fe202a3ca.png` | `tex_sign_phishing.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-43003cae-9d11-454e-bc6e-c76b74063e31.png` | `tex_money_bag.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-8489e17a-e2be-4820-abd6-7f1f26c5628f.png` | `tex_uniform_real_police.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-87b54de0-e0fa-457d-aea9-3d23690b5009.png` | `tex_uniform_fake_police.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-0e02ec6a-6ac0-4c5f-9b7f-60a9f6805650.png` | `tex_shirt_forced_worker.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-a693fca6-24ef-4bd4-9a09-4d295a7bc133.png` | `tex_outfit_scammer.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-f7a5c962-01c8-465d-b5bf-3c005f771b24.png` | `tex_clothes_victim.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-767847fe-cf2c-4fa8-ac35-7d5c4ef41a75.png` | `tex_hideout_door.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-4acefa78-acbd-48f4-aa1b-3fe03ce0f819.png` | `tex_evidence_seal_table.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-aa789e48-3b27-4eb7-a4c5-0d33d17ddb4c.png` | `tex_wanted_board.png` selected |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-48c89e19-d3f2-4fb9-b825-648468fc1595.png` | Rejected initial phishing panel: cyan rim too saturated; regenerated muted teal. |
| `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-aaf6a1da-bf1e-4a43-b620-3abdbc9139e1.png` | Rejected initial worker cloth: coarse sack-like weave; regenerated cotton with broad flat facets. |

## Review and current state

Phone refinement source: `C:/Users/chanawee/.codex/generated_images/01a16896-29fe-7262-80e6-b47ff47a2fbc/exec-91c7cff6-dab3-48a4-a497-f98035703884.png` — built-in image_gen edit: retain all booth features, add fine cracks directly on amber caller-ID glass, keep type area empty; selected over initial phone image and exported to the same requested PNG. The initial phone source above remains recorded as a superseded first pass.

- Every generated image was displayed and visually inspected. No character anatomy occurs in these material assets.
- Final exported files were also viewed together in an in-memory 4×3 contact sheet: consistent palette and facet style, clear real/fake uniform contrast, readable props and four empty poster slots. No preview/contact-sheet files were written.
- Phone: clear cracked glass, anonymous glowing screen and hanging receiver. Pending exact caller text.
- Uniforms: shared layout, real deep teal vs fake faded charcoal/green; fake badge cheap and scales unbalanced; fake name tag intentionally blank. Real tag pending name.
- Wanted board: visually counted exactly four blank poster frames, no generated text.
- Exact final sizes exported as PNG. The 1024×512 signs and 512×512 money bag match requested exceptions; others 1024×1024.
- Repeat review: observed both worker and civilian fabrics tiled 2×2 in an in-memory preview, without writing extra files. No strong seam was apparent at preview scale. Mean RGB difference across opposite edges (8-bit levels): worker horizontal 6.77 / vertical 7.52, vs internal neighbour 5.28; civilian horizontal 5.60 / vertical 6.04, vs internal neighbour 4.07. These are natural textured boundaries, not pixel-identical borders.
- PNG header checks: all 12 deliverables have exact requested dimensions, 8-bit RGB (PNG color type 2). No transparency was requested for this file set.
- Pending: typeset exact Thai lure and phishing warning / URL, caller-ID label and real officer name; inspect Thai marks. The blank type areas are intentional placeholders until the typography-tool constraint is resolved.
- Unity was not run. No .meta, scenes, prefabs, scripts or commits were created or changed.

