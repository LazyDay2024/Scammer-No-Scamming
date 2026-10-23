# Combat integration

Runtime wiring (2026-10-23):
- `RaidEncounter` on each Hideout holds inactive `enemyRoot`, optional `corruptPoliceRoot`, and `evidencePoint`. Child templates remain activeSelf=true. Each action clones templates; reset destroys attempt clones, so sunk corpses and rescued workers return. Only the active case is spawned.
- Evidence uses `WorldInteractable` and unlocks when every original armed guard is dead. Corrupt police spawn once at Trouble 100, chase until Seal, and do not retroactively add guards to the evidence gate.
- `CaseFlowDemo` starts `StationInvestigation.BeginCase`, retains its `AnalysisResult` and selected loadout, applies genes to health/damage/enemy sight/horse/Trouble, activates road traps only on dispatch, and completes after evidence + station Seal. Death retries just the action and restores its pre-action Trouble/innocent baseline. Innocent hits feed `EndingEvaluator`.
- `NpcAnimator` uses serialized Quaternius clip bindings through Playables. Death holds the final pose; NPC colliders/agents disable, then corpses sink. Workers use the same center-ray E interaction as evidence; mounting respects focused interactions.
- Additive APIs: `Health.RestoreFull`, `Health.SetCurrent`, `Trap.ResetForAction`, public `PlayerMount.Dismount`. Existing signatures retained.

Validation added: `RaidCombatTests` covers regeneration boundaries/no revival, reduced raw lethal damage, death/reset health, evidence all-guards gate, vanished-corpse retry, empty rooms, innocent shot events, wall occlusion/actual enemy damage, and action retry retaining analysis/genes/pre-action Trouble while resetting traps and innocent outcome.

Not yet verified by this worker: Unity compilation/test execution, build, animation appearance, practical raid balance, and world/screen tour images. Coordinator owns all Unity runs and final visual review using D:/6000.3.6f1/Editor/Unity.exe.

Review fix: Trap.ResetForAction also resets sibling activateOnSpring actors; Npc restores corpse timers, health, spawn transform, collider/agent states and AI/animation state. DisguisedScammer clears stare. Added killed-ambusher retry/corpse-timer and stare regressions. Await coordinator test run.
