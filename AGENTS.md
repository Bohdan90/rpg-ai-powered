# Gate C — efficient development workflow

User-approved working policy: minimize token and tool overhead without weakening correctness.

## Start with bounded context

- Read `Docs/Prototype/DEVELOPMENT_CHECKPOINT.md`, inspect Git status, then read only the code, tests and specification sections relevant to the task.
- Google Drive remains authoritative for canon and working-state documents. The local checkpoint is a navigation aid and verified implementation snapshot, not a replacement for Drive. Resolve relevant conflicts before editing; follow explicit user requirements to read documents in full.
- Do not reread the whole conversation, canon pack or historical milestone reports for a small change. Expand investigation when dependencies, uncertainty or failures justify it.
- Search within the repository first; avoid broad home-directory searches. Request small, relevant output slices. Keep full logs in files and report summaries or failures.

## Implement and validate

- Prefer the smallest explicit change that fully satisfies the task. Preserve unrelated work, tuning and architecture; do not add speculative abstractions.
- `RPG.Core` remains pure C#, independent of UnityEngine and Presentation. Unity stays pinned to `6000.6.2f1` unless explicitly changed by the user.
- Run focused tests while iterating. Before a code checkpoint, run the relevant complete suites once; repeat only when subsequent changes, failures or unresolved risks warrant it. For Gate C runtime changes this normally means EditMode and PlayMode.
- Perform manual validation proportional to the integration risk, and always complete scenarios explicitly requested by the user. Distinguish automated, mouse-driven and unperformed checks honestly.
- Documentation-only changes need diff/link/content review, not Unity test runs, unless they affect executable configuration.
- Inspect the complete intended diff and Git status before committing. Never omit necessary checks merely to save tokens. Never push without explicit authorization.
- Do not delegate to additional agents unless explicitly requested or required by applicable instructions.

## Communicate and hand off

- Keep updates brief: intended change, material findings/blockers, final outcome. Do not narrate routine tool calls or duplicate long reports in chat.
- Ask only for missing decisions that materially affect correctness or scope. Continue already-authorized work without redundant confirmations, while respecting tool permission requirements.
- Final responses should normally state what changed, verification, remaining limitations and commit; link details rather than repeat them. Explicit user deliverables take precedence over brevity.
- Maintain the compact checkpoint after meaningful changes: verified baseline, current tuning, test totals, known issues and next decision. Do not embed transcripts or claim unverified results.
- Recommend a fresh chat at a task/milestone boundary, using the checkpoint and a bounded task. Do not require the user to paste the full development history.
- This policy optimizes workflow; it does not promise a particular token reduction or override explicit task requirements.
