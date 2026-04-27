# Stratezone Release Roadmap

This document defines the practical path from playable prototype to a build that can be sold or publicly distributed on itch.io or Steam.

It exists because "playable" and "sellable" are different targets. A sellable game needs packaging, settings, QA, storefront truth, asset rights, build versioning, and update discipline.

## Documentation Role

- **Doc role:** Active source of truth for release readiness and commercial build preparation.
- **Owns:** public-build gates, packaging expectations, store-readiness steps, release tooling, and build QA.
- **Does not own:** moment-to-moment gameplay design, engine architecture, final store copy, or price strategy.
- **Read when:** making a public demo, preparing an itch page, preparing a Steam page, packaging builds, or deciding whether a build can be sold.
- **Do not read for:** first-mission rules or low-level system behavior.

## Release Philosophy

Stratezone should reach public release through honest build gates:

1. prove the RTS loop
2. make one mission understandable
3. make a build that survives strangers playing it
4. make a public demo or itch build
5. only then prepare a Steam-facing release track

Do not promise store-page features that are not in the build. The game should never rely on a storefront description to explain missing basics.

## Tooling Ladder

These tools should appear as the project matures:

- **Godot export presets:** Windows export first, later Linux if low-friction.
- **C# build checks:** compile project and simulation tests from repeatable commands.
- **Repo scripts:** root-level commands in `tools/` or documented commands in `docs/engineering-standards.md`.
- **Content validation:** verify units, buildings, missions, factions, and balance data load before runtime.
- **Save/load checks:** validate in-mission save state before public builds.
- **Build stamping:** include version, build date, commit hash, and channel in a visible debug/about surface.
- **Smoke tests:** launch packaged build, start First Landing, win/loss once, quit cleanly.
- **Issue capture:** known issues file, playtest feedback template, crash/log location.
- **itch.io upload path:** use `butler push` once public itch distribution begins.
- **Steam upload path:** use Steamworks/SteamPipe only after the build and store page are honest enough for review.

## Public Build Requirements

Before any public build, Stratezone needs:

- a packaged Windows build that runs outside the editor
- a main menu or direct mission start that does not require developer explanation
- settings for resolution/window mode, volume, and input basics
- clear win/loss/restart flow
- a known issues list
- visible version/build info
- credits and third-party license notes
- asset provenance notes for generated, purchased, or edited art/audio
- a clean install/run test on a machine or folder outside the repo

## Milestone 6: Playtest Build

Goal: let a small private tester play without the developer narrating.

Code and tooling work:

- package a Windows build from Godot
- add basic settings and restart/quit flow
- add build version display
- add logs or a clear crash-report location
- add a playtest feedback template under `docs/` or `tools/`
- add a packaged-build smoke checklist

Exit criteria:

- a tester can launch, play, fail, restart, and quit
- one 20-30 minute session does not require editor access
- known bugs are written down instead of kept in memory

## Milestone 7: Public Demo / Itch Build

Goal: produce a public or semi-public build that can be distributed through itch.io.

Code and tooling work:

- create repeatable Windows export steps
- create a release folder layout that contains only shippable files
- add versioning for build filenames and in-game display
- prepare an itch upload command using `butler push <build-folder> <user>/<game>:windows-demo`
- test install/run from the uploaded or zipped build
- add a short public-facing known issues note

Store/page work:

- itch page draft
- screenshots from the actual build
- short description that matches the build
- install notes if needed
- minimum supported OS/hardware notes

Exit criteria:

- the Windows build can be downloaded and run
- the page does not claim features missing from the build
- feedback from strangers would be useful, not mostly blocked by setup problems

## Milestone 8: Steam Page Candidate

Goal: prepare for Steam visibility without pretending the full release is done.

Code and tooling work:

- keep a stable demo or playtest branch
- ensure the build includes every feature claimed on the page
- add a release checklist that separates store-page readiness from build readiness
- decide whether the first Steam-facing artifact is a demo, playtest, Early Access candidate, or full release candidate

Store/page work:

- capsule/key art plan
- screenshots from current build
- short trailer or gameplay capture plan
- feature list matched to actual implementation
- tags and genre positioning
- Coming Soon timing plan

Exit criteria:

- store claims are true for the current or near-current build
- missing features are described as future plans only if the chosen release type allows that expectation
- a future Steam submission pass has a checklist, not guesswork

## Milestone 9: Steam Demo or Early Access Candidate

Goal: submit a build and page that can survive platform review.

Code and tooling work:

- packaged Windows build on a release branch
- repeatable Steam build upload steps
- clean first-run flow
- settings, save/restart, credits, license notes, and support info
- crash/log capture documented
- release notes and known issues

Store/platform work:

- complete Steam store presence checklist
- complete Steam game build checklist
- confirm price/release type direction
- submit store page for review before build review
- account for review time and required Coming Soon visibility before release

Exit criteria:

- the store page and build describe the same game
- the build launches and plays outside the editor
- release blockers are platform/account/process issues, not missing game basics

## Milestone 10: Sellable Release Candidate

Goal: make a build that can reasonably be sold.

Code and tooling work:

- final release branch
- versioned build artifact
- clean install/uninstall behavior
- save/load or clearly documented mission-run expectations
- stable performance on target hardware
- final credits and license audit
- final input/settings pass
- post-launch patch process

Business/store work:

- final screenshots and trailer
- final store copy that matches the build
- price decision
- support/contact path
- launch discount decision if applicable
- release notes
- update plan for the first patch

Exit criteria:

- a buyer can install, play, understand, quit, relaunch, and get support
- the build is not just impressive to the developer; it is legible to a stranger
- the repo can produce the release build again

## External Platform Notes

Verify these against current official docs before release work:

- Steam uses separate store presence and game build checklists. Both must be completed, reviewed, and approved before release.
- Steam store presence review is usually 3-5 business days, and Valve recommends submitting at least 7 days before the intended page launch in case changes are needed.
- Steam's Coming Soon page must be live for at least 2 weeks before release.
- Steam build claims should match the features described on the store page.
- itch.io supports direct uploads, but `butler` is the preferred command-line path for repeatable build pushes.
- itch.io channel names such as `windows` or `win` help tag builds correctly.

Official references:

- Steamworks Release Process: https://partner.steamgames.com/doc/store/releasing
- itch.io docs: https://docs.itch.zone/
- butler manual: https://docs.itch.zone/butler/master/
- butler pushing builds: https://docs.itch.zone/butler/master/pushing.html
