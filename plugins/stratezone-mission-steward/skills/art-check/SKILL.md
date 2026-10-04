---
name: art-check
description: Verify and capture Stratezone's existing Colony Hub, Rifleman, Rover and tree studies with fresh asset receipts; use for art/export review and stale-preview checks, not for creating art or approving production quality.
---

# Stratezone Art Check

Use inside the Stratezone checkout. This is the canonical body shared by Codex,
Claude Code and Grok; `.agents/skills/art-check` and `.claude/skills/art-check`
contain discovery pointers.

## Check Existing Art

Read the selected asset's README under `art/` and the relevant sections of
`docs/3d-art-direction.md` before interpreting its result. Study assets are
proposals; their bounds are not gameplay footprints or production acceptance.

From the repository root:

```powershell
python tools/art/check_art.py --asset colony_hub
python tools/art/check_art.py --asset rifleman
python tools/art/check_art.py --asset rover
python tools/art/check_art.py --asset trees
python tools/art/check_art.py --all
python tools/art/check_art.py --check-receipt path/to/receipt.json
```

The runner compiles current C# adapters, then checks existing exports and captures the real Godot renderer through
`tools/godot.ps1`. Each run writes a separate ignored `artifacts/art-check/` folder
with captures, logs and a receipt containing hashes, engine/camera settings,
results and unchecked items. Preserve tracked `previews/` until the user asks to
replace them. Check an old receipt for freshness before using it to assess the
current asset. Missing required runtime/check files or LFS pointers block a pass.
Unavailable authoring sources leave source provenance unverified and are recorded
separately, even when runtime checks pass. Do not automatically download, rebuild
or export art to make the check pass.
Stale or missing Godot import stamps block capture. Use the runner's explicit
`tools/godot.ps1 -Import` hint when an import is needed, then inspect Git status
for sidecar churn before rerunning. Verification itself does not import assets.

## Source And Export Provenance

Verification is separate from changing sources or regenerating exports. The
runner never performs those mutations. If the user requests export work, read
the asset README and inspect the saved source before choosing its helper:

- **Rifleman:** `art/units/rifleman/rifleman_study.blend` contains later fitting
  and color changes. Initial geometry builders do not reproduce those edits.
  Preserve the fitted source and shared `SZ_Infantry` skeleton. The documented
  finalizer writes the master, metrics and runtime GLBs, and removes unrelated
  scenes inside its disposable process. Run it only in a separate Blender
  background process, with the `.blend` path before `--python`; never through
  the live MCP editor. A prior live save/export attempt crashed. Do not repeat
  that live export workflow or rebuild the custom body as a recovery shortcut.
- **Hub and Rover:** saved sources include entrance/height and radar revisions.
  Inspect those revision tags before using the initial builders; rebuilding
  into their source paths can discard later edits. Use the documented targeted
  edit/export helpers on a preserved source when that is the authorized task.
- **Trees:** the builder can reproduce the documented study variants, but
  verification of a present export does not authorize rebuilding or replacing it.

## Interpret The Evidence

Compare fresh engine views with the actual directional sprite references for
silhouette, gear, proportions and palette. Inspect front, side and rear views,
team accents, default/far mission zoom and 1280x800 readability as appropriate.
Review animations for deformation, pose and foot contact when relevant.

Report structural assertion results and human visual judgments separately. A
passing export/rig/material check does not approve likeness, polish, readability,
gameplay footprint integration or Steam Deck performance. Keep any unreviewed
dimensions explicit in the closeout; the runner's receipt does not supply them.
