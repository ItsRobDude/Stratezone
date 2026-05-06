# Building Directional Sprites

Runtime building art uses eight directional PNG frames named by compass-facing angle:

- `000`, `045`, `090`, `135`, `180`, `225`, `270`, `315`

`000` means facing north/up-screen, then angles rotate clockwise. Static buildings currently render the `180` south/front frame in game because that is the most readable default view.

Each building frame should keep a shared transparent canvas, centered body, and consistent scale across the full turntable set.

Runtime loading prefers each building's packed atlas:

- `game/assets/buildings/<building>/<building>_directional_atlas.png`

Build or refresh atlases after changing directional frames:

```powershell
python tools\build_unit_directional_atlases.py --assets-root game\assets\buildings
```

Atlases use the same angle order listed above in a 4-column grid. Loose directional PNGs remain the source frames and runtime fallback.
