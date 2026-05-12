# Wells at the Ridge Playtest Notes

Date: 2026-05-11

This note records the current Level 2 stabilization evidence. It is a route-proof note, not a final balance signoff.

## Rebuild Direction

- The ridge version is being treated as a disposable systems testbed rather than a demo-quality map.
- Mission 2 is rebuilt around a central island well, with player and enemy approaches controlled by destructible bridges.
- Current smoke coverage proves bridge state truth at the simulation layer: intact bridges allow crossing, collapsed bridges block crossing, Grunts can repair/rebuild the route, and enemy dispatch does not blindly route through a broken crossing.
- Neutral Repair Platform, Med Hall, and Logistics / Repair Pad remain cut from this bridge pass.

## Current Route Evidence

- Mission start pattern is `player_places_colony_hub`; smoke coverage loads the Level 2 mission and proves the player-placed Hub route can create a legal base start.
- Safe base deployment is covered by the Level 2 route smoke: Colony Hub, Power Plant, first Extractor, and Barracks can all be placed and powered from the starting clearing.
- First Extractor/Barracks route is covered by deterministic simulation checks using the current 1350 starting materials.
- Pylon chain to the island well is covered by the Level 2 route smoke; the enemy does not instantly own the central island well, and the player can establish a powered corridor to contest it.
- Bridgehead rebuild proof covers the enemy restoring useful defensive power and a Defense Tower during a breach while skipping forward expansion Pylon churn.
- Assault route pressure is represented by enemy wall/power markers and smoke checks for power-dependent route state; true hands-on wall assault feel still needs a manual pass.
- Patrol pressure is authored with first patrol at 45 seconds, 38-second patrol interval, first attack at 180 seconds, and no hidden-plan HUD warning.
- The first base-building pressure trigger watches Barracks and first Extractor, waits at least 75 seconds, coalesces for 30 seconds, cools down for 120 seconds, and fires once with a 1-unit group.

## Guardian Retrofit Evidence

- Guardian production remains gated by `barracks_upgrade_guardian_retrofit`, not Armory Annex.
- Current retrofit tuning is cost 350, duration 25 seconds, 2 required Grunts, and required Grunt range 10.
- Smoke coverage proves the player route can train the second Grunt, start the retrofit, complete it while powered, then queue and produce a Guardian.
- Smoke coverage also proves enemy Guardian production must complete the same retrofit gate before producing Guardians.
- No cost/duration/resource tuning was changed in this pass because the deterministic route is currently viable; feel tuning should wait for a hands-on Level 2 playtest.

## Readability Added

- Level 2 now declares retry, tactical-note, and sparse map-callout localization keys.
- Callouts are limited to player-useful map anchors: start well, central island well, and enemy wall power.
- These presentation fields intentionally do not announce hidden enemy plans.

## Manual Check Status

- A true human manual Level 2 run was not performed from this terminal session.
- Current confidence comes from deterministic smoke route coverage, content validation, C# build validation, and headless scene loading.
- The next hands-on pass should specifically watch whether the island well reads as a meaningful contest, whether bridge collapse/repair pacing creates tension without soft-locking the mission, and whether the player has enough time to choose between expanding, walling, attacking, repairing, or retrofitting before the enemy pressure curve collapses into one obvious answer.

## Remaining Risks

- Guardian Retrofit may still feel mandatory once enemy armor and wall pressure are played in real time.
- North/south scouting value is data-authored but not yet feel-proven by human play.
- Wall-route assault readability still depends on visual clarity and player discovery, not just route legality.
- Bridge art/readability will be greybox at first, so the first hands-on run must judge whether the crossing state is understandable before final terrain art exists.
- Restart/quit/readability UI is intentionally minimal and should be rechecked once Level 2 is played in-editor.
