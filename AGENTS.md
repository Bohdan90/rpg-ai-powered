# Repository instructions

Default AI workflow = checkpoint-first, task-local reading, targeted search, focused tests during development, one full suite before commit, concise chat output.

Follow [AI DEVELOPMENT EFFICIENCY PROTOCOL](Docs/Prototype/AI_DEVELOPMENT_EFFICIENCY_PROTOCOL.md).
Start tasks with [GATE_C_WORKING_CHECKPOINT](Docs/Prototype/GATE_C_WORKING_CHECKPOINT.md),
the current task, Git status/HEAD and relevant code/tests. The checkpoint is the single
implementation handoff; Google Drive thematic owners remain authoritative for canon.
RPG.Core must remain deterministic pure C#, independent of UnityEngine/Presentation.
Do not push or introduce additional agents without explicit authorization.
