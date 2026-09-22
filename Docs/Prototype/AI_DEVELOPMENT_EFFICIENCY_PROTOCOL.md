# AI DEVELOPMENT EFFICIENCY PROTOCOL

Permanent user-approved repository workflow. Goal: maximum development efficiency at
minimum token cost without reducing implementation quality, Core invariants or regression safety.
Explicit task requirements and higher-priority instructions still apply.

## 1. Source of working context

Maintain one compact authoritative implementation checkpoint:
`Docs/Prototype/GATE_C_WORKING_CHECKPOINT.md`. Include only current commit/baseline,
Gate/milestone, technical baseline, Core invariants, implemented systems, prototype tuning,
fixtures, latest test totals, known limitations, playtest findings, OPEN questions and
links to detailed reports. Never copy full canon or transcripts into it.
Update only changed facts after every meaningful commit; prepare that delta with the
implementation change and verify it after committing. Distinguish implementation baseline
from documentation-only commits. A file cannot contain its own resulting Git hash:
record the observed HEAD/baseline and use `git rev-parse --short HEAD` for the live HEAD;
do not create an endless chain of hash-only documentation commits.

## 2. Context loading

Always read the working checkpoint, concrete task prompt and relevant code/tests.
Load thematic canon only when touched: movement/attack/action economy → 01/04;
classes/weapons → 02; retreat/commander → 17/19; siege → 18/19; statuses → 12.
Do not automatically reread 00_START_HERE, all of 07/09, the full master pack, or
unrelated narrative/campaign/economy owners. If the checkpoint suffices and the task
changes no canon boundary, avoid redundant owner reads. When uncertain, search the
specific section or term first. Honor an explicit task instruction to read a file fully.

## 3. Code reading

Search symbol/test/caller → read relevant method/class → inspect immediate callers and
dependencies → change the smallest necessary area → inspect diff. Do not read large
files in full or investigate neighboring systems without a concrete dependency/reason.
Expand investigation when evidence reveals an architectural dependency.

## 4. Implementation

Prefer the smallest correct change using existing architecture. No speculative refactors,
future frameworks, unrelated cleanup, adjacent-system redesign or unsolicited scope.
Refactor only when needed for correctness, relevant duplication or consistent Core truth.

## 5. Communication

Start with a short statement of what will be checked/implemented. Further substantive
interruptions should concern canon contradictions, required decisions, unavoidable scope
expansion or serious unexpected regressions. Keep any required progress updates short.
Never send large logs, compiler dumps, code dumps or repeated context. Store useful
technical detail in a report only when warranted.

## 6. Tool output

Use targeted repository search, limited line ranges, test summaries, diff/stat and focused
error snippets. Save large logs to files. Do not output thousands of lines to inspect ten.
Batch independent reads where useful; do not perform broad filesystem discovery by default.

## 7. Testing

During development, use focused tests for the changed area (e.g. geometry/contact/LoS
or ranged/preview). Before commit, one complete successful EditMode AND PlayMode run
is mandatory. Do not substitute repeated full suites for targeted debugging. If code
changes after that run, repeat relevant focused tests and both full suites before commit.
If no code changes afterward, do not rerun without a concrete reason. This protocol
contains no documentation-only exemption to the user's mandatory pre-commit full suite.
Report failures honestly and fix them; never weaken coverage to save tokens.

## 8. Manual validation

Scale manual Play Mode checks to risk: micro tuning → relevant scenarios; geometry or
combat rule → positive, negative, preview/execution consistency and nearby regression;
milestone → broad scenario matrix. Do not rerun 15–20 unaffected scenarios for every
numeric change. Complete explicitly requested scenarios. Documentation-only work has
no new runtime behavior to manually exercise; review its contents/links and say so.
Distinguish mouse-driven checks, automated checks and unperformed checks.

## 9. Reports

Small tasks normally need only a checkpoint delta and, if useful, a short report.
Detailed reports are for milestones, architecture changes, complex bugs, comparative
experiments and handoffs. Keep details in reports and summaries in chat.

## 10. Final response

Be compact: commit hash, change, test totals, manual validation, important remaining
issue and push status. Do not repeat the task prompt; link details when needed.

## 11. Git

Before task: `git status` and `git rev-parse --short HEAD`.
After task: inspect complete intended diff, full tests, relevant manual validation,
commit and verify clean working tree. Preserve unrelated user work; do not hide it.
Never push without a separate explicit user instruction.

## 12. New chats

At a task boundary in a long chat, hand off only the working-checkpoint path, live HEAD,
specific next task and necessary thematic owners. Do not transfer the full conversation
or generate a giant handoff prompt when the checkpoint is current.

## 13. Canon boundary

The implementation checkpoint cannot override Google Drive canon. Already-approved
prototype tuning needs minimal rereading; a new gameplay rule requires the relevant
thematic owner. A potential FIXED-canon conflict requires stopping the affected work
and obtaining a design decision. Mark tuning as prototype-only, never global canon.

## 14. Decision reuse

Reuse decisions already settled in the checkpoint, current task, code/tests or relevant
owner. Ask again only when real conflict or new information changes the decision.

## 15. Non-negotiable rigor

Never economize on correctness, determinism, RPG.Core independence from UnityEngine,
Core as combat truth, regression tests, final diff review, full pre-commit suites or
user-visible behavior validation. Reduce redundant context, reasoning and output instead.
