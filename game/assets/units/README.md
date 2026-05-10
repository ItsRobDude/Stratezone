# Unit Directional Sprites

Runtime unit art uses eight directional PNG frames named by compass-facing angle:

- `000`, `045`, `090`, `135`, `180`, `225`, `270`, `315`

`000` means facing north/up-screen, then angles rotate clockwise:

- `000`: north
- `045`: northeast
- `090`: east
- `135`: southeast
- `180`: south
- `225`: southwest
- `270`: west
- `315`: northwest

Each frame should keep a shared transparent canvas, centered body, and consistent bottom foot anchor so direction changes do not visually jump in game.

Runtime loading prefers each unit's packed atlas:

- `game/assets/units/<unit>/<unit>_directional_atlas.png`

Units with movement animation may also provide a directional run atlas:

- `game/assets/units/<unit>/animations/run_directional/<unit>_run_directional_atlas.png`
- `game/assets/units/unit_animation_settings.json`

Run atlases use eight rows in the same compass order and sixteen columns for the run-cycle frames. Keep every cell on one shared transparent canvas and preserve the same bottom-center foot anchor.

Prototype run atlases may use a smaller fixed cell than idle frames to keep texture memory reasonable. If they do, presentation code must scale that run atlas back to the same displayed size and preserve the bottom-center foot anchor.

Build a run atlas from a `N-360/1x/*.png` pose export:

```powershell
python tools\build_unit_run_atlas.py --unit rifleman --source C:\Users\Rob\Pictures\stratezone\rmrun --preview-out C:\Users\Rob\Pictures\stratezone\rmrun_review\runtime_rifleman_run_directional_preview.png
```

The default source view order is `4,3,2,1,8,7,6,5`, mapped into the game compass rows `000,045,090,135,180,225,270,315`. Override `--source-view-order` if a future turntable export starts from a different angle.

Build or refresh atlases after changing directional frames:

```powershell
python tools\build_unit_directional_atlases.py
```

Atlases use the same angle order listed above in a 4-column grid. Loose directional PNGs remain the source frames and runtime fallback.

Current imported runtime sprites are downscaled copies from local high-resolution working folders under `C:\Users\Rob\Pictures\stratezone`. Some source turntables were authored with the original front-facing pose as `000`; remap those source frames into the compass convention before importing them here.
