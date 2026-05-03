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

Current imported runtime sprites are downscaled copies from local high-resolution working folders under `C:\Users\Rob\Pictures\stratezone`. Some source turntables were authored with the original front-facing pose as `000`; remap those source frames into the compass convention before importing them here.
