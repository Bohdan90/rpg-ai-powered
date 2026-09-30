# Incident 04 — controlled manual starting states

Schema3, seed20260929, implementation20473ec. These are explicit artificial starting
states for rare-case validation, **not saves from natural play** and not manufactured
battle outcomes. All four passed the existing save Restore validation. Runtime/tests
are unchanged; these files are not consumed by automated suites.

Use an isolated playtest copy. Back up the existing manual slot before replacing it.
On this Mac the prototype slot is:
`~/Library/Application Support/DefaultCompany/My project/CrossroadsIncident04/manual.json`.
Copy one selected JSON there as `manual.json`, enter TacticalGraybox Play mode, then
click **Load Incident 04**. Do not paste a fixture over an unsaved player session.
For another fixture, leave Play mode and replace only the backed-up test slot.

| File | Starting state | Mouse actions and expected observations |
|---|---|---|
| `hold-reset.json` | R3, West owns/is at08, Hold1/2; A14; East16 | West **Strategic Withdrawal**. Observe08→03, Tempo100→60, Hold0/2, Gold300. End both sides and continue world phase. No reward; no inherited progress. B's staged record is deliberate fixture setup, not a claim of a natural entry trace. |
| `stranded.json` | R5 Whiteout1, East16 blocks Anchor; A Returning15/Tempo−40; B Unreleased | End both and continue world phase: R6 Closure, A remains15/StrandedReturn, Tempo60. End both again; Continue gives East a contact choice. Choose Withdrawal; finish phase. At R7 end West, move East14→08, end East, continue. At R8 Stable A is GuardingClosedAnchor16, same3 IDs; no teleport/erasure/late portal exit. |
| `grouped.json` | R3, West08, East01, A14 and B15 active/allied | Select14, Attack. All12 bodies deploy together: West6 versus A3+B3. Human West's legal North-edge Escape is sufficient to finish this fixture; return to World. Both raider commanders retain their own XP, neither force is replaced. |
| `two-battles.json` | R3, West07/Tempo100, East13, A06 and B08, not directly adjacent to one another | Select06, Attack; finish a real battle while preserving some West survivors. Return to World and record each surviving ID/HP/Armor and R3. Move back07 if needed; select08, Attack with remaining Tempo. Compare second deployment's same IDs/HP/Armor exactly with first battle end. Do not End Activation/Refresh between fights. Finish via ordinary play/Escape and return: stillR3. |

Each outcome must be read from actual UI/battle result. Merely loading these files or
asserting their Core state is not manual validation. Screenshots, state captures and
replays from the performed cases are indexed in the parent World Dynamics report.
