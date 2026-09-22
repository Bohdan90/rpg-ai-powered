# Gate C — local telemetry and deterministic replay

Local debug workflow only. No analytics server, production replay UI or event-sourcing
architecture. Core BattleJournal wraps the existing resolver; Presentation serializes
explicit DTOs via Unity JsonUtility and writes local files. Core remains Unity-free.

## Use

In Play Mode, scroll HUD to **LOCAL REPLAY / TELEMETRY**:
1. **Export battle + session** writes a unique `.jsonl` replay and `.session.json` metrics
   file to `Application.persistentDataPath/GateC/Replays`; the full path appears in HUD.
2. **Load / verify replay file** reads the path field (which can be replaced with another
   local export), re-runs Core commands and reports success or first divergent sequence.
   Verification does not replace/mutate the current live battle or advance its RNG.
3. Restart Same Seed starts a new journal/session. Existing exported files remain.

## Format v1

Header: format/config/build version, fixture ID, seed, initial controllers, complete
initial snapshot and SHA-256 hash. Snapshot includes deployment, profile IDs, board
solids/dimensions/retreat geometry, pools/status/facing, action/movement/OA/Defending,
activation priority/tie keys/index, outcome and explicit initial RNG state. This allows
exact reconstruction of a supplied mid-battle snapshot as well as normal fresh starts.

Command lines: attempt sequence and separate successful sequence (zero for invalid),
round/active actor, command actor/target/kind/path/final-facing/friendly confirmation,
controller at submission, AI explanation where applicable, applied/error, RNG before/
after, state hash, all units' before/after resource snapshots and HP/Armor deltas, plus
structured Core events. Normal attack preview chances are explicit; OA chances/rolls,
Guard, facing, damage, Defend, death, escape and battle outcome are in event diagnostics.
Initial controller labels stay historical; command controller labels capture mid-session
Hotseat/AI switches. Current play mode is West player/East AI when enabled.

Footer: attempt/success counts, final hash and final snapshot (including active/dead/
escaped sets and remaining pools). A missing footer, unsupported version, malformed
file, reordered command, legality mismatch or state/RNG mismatch is reported, never
silently accepted. Divergence sequence means attempted-command sequence; initial/header
mismatch is sequence 0, footer/truncation after N records is N+1.

Hash: canonical little-endian BinaryWriter integer/boolean representation, fixed field
order, sorted solids/unit IDs, ordered priority list, current profile tuning values;
SHA-256 encoded as Base64. No GetHashCode, timestamps or locale-sensitive numeric text.
Bump config/format version when replay semantics/schema changes. Profile tuning changes
also invalidate initial state hashes. This is a diagnostic format, not long-term save
compatibility or a tamper-proof archive.

Replay truth is the explicit initial state + seed + ordered successful commands.
Invalid attempts are revalidated for diagnosis but change neither state nor RNG nor
successful sequence. Combat rolls are regenerated; logged rolls/events/resource copies
are diagnostic output, not replay inputs. State hashes and regenerated RNG are verified;
arbitrary edits to diagnostic strings/roll fields are not authenticated by the state hash.

## Separate session metrics

Current round number, activations started since recording, contact attempts (normal +
OA), actual ZoC exits/OA triggers, Defend, escapes, deaths, invalid attempts and explicit
preview cancellations. `attacksBeforeFirstHpLoss` counts preceding contact attempts,
excluding the attack which first caused HP loss; `firstHpLossObserved` distinguishes
ongoing no-HP-loss sessions. Round number is not a count of completed rounds.
Optional player decision-time measurement is intentionally omitted; no background-tab
timing is reported. Metrics make no automatic claim about fun, depth or balance.

## Validation

Nine Core replay cases cover normal elimination, physical withdrawal, OA death before
a step, preview/invalid purity, replay of successful commands alone, first divergence,
truncation, mid-state resources, ignored diagnostic rolls, and AI sequences on both 9v9
maps. Two PlayMode cases test real JSONL/session disk round-trips via Presenter for
field/OA/Escape/AI and rejection of malformed/incomplete files.

Final full-suite and physical mouse export/verify evidence are in the closure report.
