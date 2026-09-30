# Combat Variety 51 / Connected 05B — implementation in progress

Starting baseline: develop `045707f81847fb88ac34c724d5f87ce30fba5c39` (gameplay `20473ec`). Feature worktree: `../Isolated-50-51`, branch `feature/50-51-connected`. Documents 50 and 51 authorize this isolated batch; 04 player acceptance remains PENDING. No merge into develop, no push.

## Early Lab checkpoint

Three authored near-contact 23×17 entries are exposed in Gate C → Play Tactical Graybox: FireVsIce, SupportVsFire, MobileBlades. Controller selector supports Hotseat and either human side versus AI. Select Ability, target cell, explicit allied-fire confirmation if needed, Confirm. This is an early playable, not completion of 50+51.

Core additions: separate Fire/Ice HOM TI/TII, HH TI, EW TII profiles; spell commands and pure previews; Action/Exertion, source-use counters, temporary Barrier, Fire Armor retaliation, Burn, Freeze, Close Heal cleanse, dual-hit Basic and target-specific Graceful Exit. Existing profiles retain values. Integer tactical damage uses the existing final truncation policy after one Magic Power multiplication (e.g. TII Fireball 16); fixed Burn damage and shields are unscaled. Tactical replay captures new states/commands. Old replay config explicitly rejects mismatched versions. Persistence battle results retain source counters; strategic resets/save integration are still pending.

Focused validation (actual isolated Unity 6000.6.2f1 runs):
- 33 EditMode passed, 0 failed/skipped: CombatVarietyTests, BattleReplayTests, TacticalAiRetreatTests. `/private/tmp/convergence-5051/logs/EditMode-20260929-221526.xml`.
- 1 PlayMode passed, 0 failed/skipped: three Lab entries, Fireball UI→Core commit, replay, AI controller. `/private/tmp/convergence-5051/logs/PlayMode-20260929-220910.xml`.
- These are automated checks, not manual V1–V5 or full regression. Those remain pending.

Safety: original user checkout was never switched or edited. Company/product in this isolated project are `CodexPrototype/Convergence-5051-Isolated`; runner uses `CFFIXED_USER_HOME=/private/tmp/convergence-5051/preferences`, independent Library/Temp, result/replay namespaces. Initial sandbox run failed on UPM socket EPERM; approved isolated runner then succeeded. No GUI input delivered to user Unity. Original nine-file hashes recorded at `/private/tmp/convergence-5051/original-files.json`.

Remaining batch: deepen mechanics coverage/readability, 50 economy/construction/research/Forge, separate 05B integration, save/load and global Refresh budgets, final regression, honest controlled/manual evidence.
