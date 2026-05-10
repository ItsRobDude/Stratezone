# Stratezone Content Roadmap

This document tracks mission archetypes, map-content ideas, and content-shape decisions that make Stratezone feel rounded without turning it into uncontrolled scope.

It should capture approved direction before implementation, but it should not pretend every idea is committed. When an archetype or content toy needs runtime support, record the smallest mechanic needed and the reason it earns a place.

## Documentation Role

- **Doc role:** Content-shape planning for missions, terrain grammar, neutral structures, commander use, and scenario flavor.
- **Owns:** approved mission archetypes, campaign/demo content sequence ideas, content entry gates, and deferred content cuts.
- **Does not own:** core product identity, engine architecture, data schemas, release packaging, or implementation acceptance checks.
- **Read when:** planning new missions, deciding whether a content idea belongs in the first demo, or checking whether a proposed feature is mission-earned.
- **Do not read for:** low-level simulation contracts or store-readiness process.

## Content Principles

- Build a campaign-like sequence of authored simulation scenarios, not a story-heavy cutscene campaign.
- Keep the first enemy human and same-tech for now, with red or alternate-color presentation.
- Keep missions fresh-scenario based: each level may introduce a new problem, but not a persistent colony layer.
- Make each mission prove one main idea and one secondary pressure at most.
- Prefer resource, power, terrain, scouting, and commander-position problems before adding new unit families.
- Support and siege systems should enter only when a mission needs them, not because the content list feels thin.
- Quick RTS restart is the first-demo failure model; persistent campaign consequences are later scope.

## First Demo Mission Archetype Direction

The current target remains a first public demo with five coherent authored missions. The exact final mission count can change later, but this sequence is the working shape for planning.

### Mission 1: First Landing

Purpose: teach and prove the core RTS loop under light pressure.

Frame:

- The expedition has just landed or crash-landed in a strange new area.
- The player must secure the landing zone, survey the immediate area, neutralize any local threat, and buy time while communications with the home planet, home base, or command network are restored.
- The story framing should stay standard and practical. The mission's job is to teach Stratezone's base/power/resource/combat grammar, not to carry a heavy plot.

Gameplay shape:

- deployed Colony Hub and fragile Commander
- one Grunt, one Guardian, one Rover, and one Commander as scenario-start assets
- player trains Grunt, Cadet, and Rifleman only
- build power, pylons, Barracks, Extractor/Refinery, and defenses
- scout black fog, secure wells, cut weak enemy infrastructure, and destroy enemy forces
- Commander death remains a loss condition

Content boundary:

- no Med Hall, Logistics / Repair Pad, Artillery Battery, or Vehicle Bay requirement
- no heavy story cutscenes
- same-tech human enemy is acceptable even if represented with placeholder colors

### Mission 2: Resource Race and Chokepoints

Purpose: prove strategic base-building through terrain and scarce wells.

Frame:

- Shortly after First Landing, or weeks later, supplies are running short.
- New resource wells are detected nearby while the expedition is still cut off from home base or home planet support.
- The enemy is competing for the same wells with similar technology.

Gameplay shape:

- limited resource wells create an expansion race
- cliffs, water, ridges, or other terrain blockers create readable chokepoints
- Defense Tower walls matter because of map shape
- enemy power and extractor routes are useful strike targets
- player chooses between expanding, walling, repairing, or attacking

Content boundary:

- same-tech enemy only, reskinned red or alternate color
- no new tech layer unless the terrain/resource proof genuinely needs it
- Med Hall, Logistics / Repair Pad, and Artillery stay out unless the mission cannot work without one

### Mission 3: Armory Unlock and Anti-Armor

Purpose: prove the first deliberate tech unlock.

Frame:

- After stabilizing the outpost and securing new resources, the expedition can bring a new production module online.
- The Armory Annex becomes the first clear step from survival setup into battlefield specialization.

Gameplay shape:

- Armory Annex is a physical powered add-on adjacent to Barracks
- Guardian access is earned through mission setup, construction, restoration, or protection
- enemy armor, hardened defenses, or defense anchors justify Guardian use
- power disruption can disable the unlock path
- Guardian must remain a specialist, not a better general-purpose Rifleman

Content boundary:

- prove Armory/Guardian before broadening into multiple unlock systems
- Vehicle Bay/Rover production should stay later unless this mission's proof target changes
- at most one support/siege system may appear if the mission proves a hard need

### Commander Field Operation

Purpose: use the Commander as a real troop without turning every mission into an escort problem.

Frame:

- Commander presence can represent battlefield authorization, coordination, or command-link stability.
- Barracks, Vehicle Bay, or other production hubs may require the Commander at base or inside a command radius in some missions, but this should be used sparingly.

Gameplay shape:

- Commander remains controllable, vulnerable, and mission-relevant
- a mission may ask the Commander to inspect, activate, secure, or coordinate one important objective
- if the Commander leaves base, some production or command function can pause or degrade
- the player should usually be able to keep the Commander protected unless the mission is explicitly about a commander field task

Content boundary:

- do not make repeated commander errands a campaign crutch
- do not turn Commander into a superhero or universal ability button
- avoid stacking this with convoy/transport pressure unless the mission is specifically built around that stress

### Transport, Convoy, or Extraction

Purpose: add movement-objective variety without repeating the commander-field mission.

Frame:

- A transport needs to be escorted, intercepted, destroyed, or used for extraction.
- The mission can end with extraction after destroying an enemy transport or clearing a route.

Gameplay shape:

- temporary defenses and route control matter
- enemy pressure should be readable before it hits the transport
- the player may need to destroy an enemy transport as the offensive objective instead of only protecting a friendly one
- extraction should feel like a tactical closeout, not a long escort slog

Content boundary:

- avoid making this feel like a commander escort with different art
- pathing, targeting, and UI clarity must be strong before this becomes a first-demo requirement
- likely better after the Level 2 resource/chokepoint grammar is proven

### Siege Breaker

Purpose: prove that Stratezone is not only about direct infantry blobs.

Frame:

- The enemy has a fortified base or protected infrastructure that cannot be efficiently cracked by basic infantry alone.
- The player needs power sabotage, Rocket Towers, a forward position, or a siege tool to break the position.

Gameplay shape:

- scouting reveals why direct attack is inefficient
- infrastructure strikes soften the defense
- Artillery Battery can earn a place here if the mission specifically needs long-range base pressure
- Rocket Towers, Guardian support, or captured forward infrastructure can provide alternative routes

Content boundary:

- do not add Artillery Battery until a mission actually needs long-range siege
- keep the siege answer readable and limited; one new answer is enough
- preserve quick restart and avoid a long slow grind

### Broken Outpost Recovery

Purpose: make repair, stabilization, and rescue feel like core Stratezone verbs.

Frame:

- The player starts after damage, a raid, storm, failed landing, or sabotage.
- The immediate objective is to restore power, repair essential structures, find missing troops, and counterattack.

Gameplay shape:

- low power, damaged buildings, limited materials, and missing Grunts create early triage
- the player may discover stranded or captured friendly troops hidden in fog
- hidden friendly groups should not be preemptively killed by enemy AI before discovery
- discovered troops can become reinforcements or optional rescue objectives

Content boundary:

- hidden rescue groups need explicit mission rules: unrevealed friendly units must be reserved, hidden, or protected until the player discovers them
- avoid turning the mission into a frustrating scavenger hunt
- keep the recovery arc short enough that the RTS pace remains active

### Two-Front Defense

Purpose: create higher-stress tactical defense after controls and readability are stronger.

Frame:

- The player must protect two separated assets, such as base plus refinery, base plus commander, or base plus transport route.

Gameplay shape:

- threat timing is authored and readable
- the player has enough warning and tools to make an informed choice
- walling, power, scouting, and mobile troops all matter

Content boundary:

- this should be later or carefully authored because it can become too intense quickly
- do not use it before selection, alerts, pathing, and failure messaging are polished enough

## Current Recommendation

Working first-demo sequence:

1. First Landing
2. Resource Race and Chokepoints
3. Armory Unlock and Anti-Armor
4. Broken Outpost Recovery or Commander Field Operation
5. Siege Breaker, Transport/Extraction, or carefully authored Two-Front Defense

The fourth and fifth mission slots remain open. The current preference is to use those slots to round out Stratezone's identity without adding too many new systems: recovery/rescue, commander use, transport/extraction, or siege should each earn its place through one clean mission proof.

## Terrain Planning Transition

Terrain planning leads into neutral infrastructure because neutral structures need map context: power relays, abandoned refineries, bridge controls, ruined turrets, and sensor towers only matter when terrain, wells, and chokepoints make them worth fighting over.

## Terrain Grammar Direction

Terrain should become real map logic before it becomes final art. The first goal is to support authored mission decisions: where the player can build, where units can move, where wells sit, and where Defense Tower walls matter.

### Cliffs and Ridges

Purpose: create hard blockers, natural base edges, and attack lanes.

Recommendation:

- Use cliffs/ridges early as simple impassable regions.
- Do not add height bonuses yet.
- Use them to frame buildable clearings, wells, and tower-wall chokepoints.

### Water and Marsh

Purpose: create readable no-build/no-move boundaries and alternate route shapes.

Recommendation:

- Start with water as a hard blocker for units and buildings.
- Avoid naval systems.
- Leave marsh slowdowns for later, after passability and pathing prove stable.

### Bridges and Fords

Purpose: make crossings readable and strategically important.

Recommendation:

- Use bridges/fords as passable lanes through water or ravines.
- Keep them indestructible at first to avoid bridge-repair and bridge-state complexity.
- Use them in Level 2 only if they make the resource race clearer rather than more fragile.

### Forests and Light Woods

Purpose: add boundary texture and possible scouting friction.

Recommendation:

- Use forests mostly as visual boundary and map flavor first.
- Later, forests can become vision blockers or partial cover if scouting needs more depth.
- Do not add stealth/hiding rules until fog and visibility have enough debug support.

### Roads and Clear Lanes

Purpose: make intended routes readable.

Recommendation:

- Use roads visually first to show likely movement corridors between bases, wells, and objectives.
- Movement-speed bonuses can wait.
- Roads should help players read the map, not override the freeform base-building promise.

### Buildable Clearings

Purpose: give the player natural base spaces without showing a visible grid.

Recommendation:

- Use authored buildable regions or clearings for base areas and expansions.
- Keep hidden footprint/buffer rules active inside those clearings.
- Make clearings large enough for practical power, Barracks, extractor, and tower-wall choices.

### Resource Basins

Purpose: make wells feel like tactical map features instead of random resource dots.

Recommendation:

- Put important wells in terrain pockets with a clear tactical question: rush, wall, raid, hold, or abandon.
- Basin art can start as a decal or simple terrain patch.
- Map logic should reserve wells for Extractor/Refinery placement and keep non-extractor structures out.

### Ruins and Wrecks

Purpose: add frontier-war flavor and future neutral-object hooks.

Recommendation:

- Use ruins/wrecks first as blockers or visual interest.
- Salvage mechanics can wait.
- A ruined turret, wrecked convoy, or broken relay can later become neutral infrastructure if a mission needs it.

### Tower-Wall Chokes

Purpose: make Defense Tower walls central to Stratezone's identity.

Recommendation:

- Author some chokepoints specifically around tower-wall spacing.
- Level 2 should prove at least one defensive wall/choke route.
- The map should make walling feel like a strategic choice, not a forced tutorial step.

### Survey and Landmark Points

Purpose: give the Rover or Commander useful places to discover.

Recommendation:

- Useful later, but do not turn scouting into collectible hunting.
- Start with visible landmarks that help players orient themselves.
- Only add survey interactions after neutral infrastructure and mission objective rules justify them.

## Map Logic and Design To-Do

Maps need active design and runtime logic before the project needs a full map editor.

Milestone 3 should establish:

- a map-data shape for terrain regions such as blocked, water, cliff/ridge, buildable clearing, road/visual lane, and resource basin
- validation that required map regions and mission markers exist
- a simple preview or debug view path for map authorship, even if it is only generated output or an in-game debug overlay
- tests or smoke checks proving blocked terrain affects placement and movement
- a rule that terrain art does not become gameplay truth; simulation reads data, presentation renders it

Milestone 4 should prove:

- Level 2 uses at least one terrain blocker type for real pathing or placement consequences
- at least one resource basin creates a visible resource-race decision
- at least one chokepoint can be shaped with Defense Tower walls
- terrain boundaries are readable with greybox/prototype art before final art

A full map editor remains later scope. A dev map workbench or preview tool can be considered during Milestone 3 only if it helps prove map data and Level 2 iteration without becoming its own product.

## Neutral Infrastructure Direction

Neutral infrastructure should be authored mission content that gives Grunts, power, scouting, and terrain more tactical meaning. It should not become a generic capture-point layer or a giant new economy system.

### Interaction Rule

First-pass neutral infrastructure should use Grunts as the main interaction troop.

Rules:

- A Grunt must physically reach the neutral object.
- Capture, hack, restore, or repair actions should take time rather than completing instantly.
- The Grunt should be vulnerable while working.
- Some neutral objects should also require power before they function.
- The action should use localized command/result messaging when player-facing.

This keeps neutral infrastructure tied to the existing worker/repair identity instead of adding a new engineer/capture unit too early.

### Neutral Power Relays

Purpose: extend power into a forward area or contested expansion.

Direction:

- A Grunt can restore, activate, or hack the relay after reaching it.
- Activation should take a timed work action, not an instant click.
- The relay may require repair and/or connection to a power source before it functions.
- Once active, it can help power a forward buildable area, a resource basin, or a defensive route.

Fit:

- Strong fit for Level 2 resource-race/chokepoint play.
- Good way to make power logistics matter without adding a new faction or tech tier.

### Abandoned Refineries

Purpose: give resource-race missions a second way to fight over wells.

Direction:

- A Grunt can take over the abandoned refinery only after physically reaching it.
- It should usually require repair and may require power before it starts producing.
- It should cost time and possibly materials, so it is not just free income.
- The enemy can contest or destroy it if the mission needs pressure.

Fit:

- Good for resource shortage, recovery, or contested-well missions.
- Should not replace normal Extractor/Refinery building; it is a mission-authored opportunity.

### Repair Platforms

Purpose: provide rare mission-specific sustain without making Logistics / Repair Pad a standard early system.

Direction:

- Use rarely, probably in one specific mission.
- It may require power or Grunt activation before it repairs.
- It should not become a generic free-heal point.

Fit:

- Better after vehicles matter more.
- Could appear in a recovery or vehicle-heavy mission if the support role earns its place.

### Sensor or Radar Towers

Purpose: make scouting and power infrastructure more valuable.

Direction:

- A Grunt can activate, hack, or restore the tower through a timed action.
- The tower reveals only a small, authored area.
- It must never reveal hidden enemy plans, hidden production, or broad under-fog activity.
- It should reveal terrain, a route, a well, a local enemy position, or a limited warning lane only when that information is earned.

Fit:

- Good fit if the mission needs a controlled scouting reward.
- Must preserve the fog-of-war rule: no omniscient hidden-plan UI.

### Supply Depots

Purpose: optional material reward.

Direction:

- Lower priority for the first demo.
- If used, it should be a small mission-authored reward, not a loot economy.
- Could require a Grunt to secure or open it.

Fit:

- Acceptable but not a favorite direction.
- Keep out unless a resource-starved mission needs a small relief valve.

### Bridge Controls and Destructible Bridges

Purpose: make terrain routes and chokepoints more dynamic.

Direction:

- Bridge gameplay is a strong fit for terrain-focused missions.
- A bridge may be controlled, activated, repaired, or destroyed depending on the mission.
- Destructible bridges are appealing, but should wait until terrain/passability and path recalculation are stable enough.
- If a bridge can be destroyed, mission design must avoid soft-locking the player unless the soft lock is an intentional fail state.

Fit:

- Strong candidate after the first pass of terrain logic.
- Good for resource-race, convoy/intercept, or siege missions.

### Wrecks

Purpose: flavor, blockers, rescue hooks, or later salvage.

Direction:

- Maybe for the first demo, but not a primary system yet.
- Wrecks can start as visual blockers or mission landmarks.
- Salvage should wait unless a mission needs it.

Fit:

- Useful for Broken Outpost Recovery, transport aftermath, or battlefield flavor.
- Avoid creating a scavenging economy too early.

### Ruined Turrets

Purpose: give Grunts a tactical restoration/hacking objective.

Direction:

- A half-ruined enemy Gun Tower or similar defense can appear unpowered.
- No power is important: if powered and hostile, it would shoot the player's troops.
- A Grunt could hack, restore, or capture it through a timed action while it is unpowered.
- After capture and power restoration, it may become a player defense or forward foothold.

Fit:

- Strong fit for infrastructure-strike and recovery missions.
- Needs clear rules so the player understands why it is safe to approach and what changed after capture.

### Capturable Forward Pads

Purpose: forward staging point.

Direction:

- Not favored for now.
- Keep out of the first demo unless a later mission proves a specific need.

Fit:

- Risky because it can make Stratezone feel like a generic capture-point game.
- Revisit only after base-building, terrain, and neutral infrastructure have stronger identity.

## Neutral Infrastructure Priority

Preferred first-demo candidates:

1. Neutral Power Relay
2. Abandoned Refinery
3. Sensor/Radar Tower with small-area reveal only
4. Ruined unpowered turret
5. Bridge control or destructible bridge after terrain/pathing support exists

Possible but lower priority:

- Repair Platform
- Wrecks as blockers/landmarks
- Supply Depot

Not planned for now:

- Capturable Forward Pads

## Enemy Doctrine Direction

Enemy doctrine should make the same-tech red opponent feel authored without making the project build a skirmish-grade AI brain too early. Mission profiles should tune enemy posture, target priorities, attack timing, rebuild behavior, and resource aggression.

### Same-Tech, Different Posture

Purpose: make the first enemy readable and scoped while avoiding a pure mirror clone.

Direction:

- The first enemy uses the same basic buildings, power rules, units, and economy family as the player.
- Red or alternate-color presentation is enough for now.
- Missions can tune posture: defensive, resource-aggressive, pylon-focused, infantry-pressure, tower-heavy, or siege-minded.
- Differences should come from authored mission profile, layout, timing, and priorities before new faction tech.

Fit:

- Strong fit for the first demo.
- Keeps content production realistic while still allowing mission variety.

### Resource-Race Enemy

Purpose: contest scarce wells and make expansion feel urgent.

Direction:

- Enemy should try to claim contested wells where the mission profile allows it.
- Enemy should rebuild destroyed Extractor/Refinery structures only if it has resources and the mission profile permits rebuilding.
- Enemy should defend important wells with patrols, towers, or nearby power if the map supports it.
- Enemy pressure should be visible through scouting, attacks, and infrastructure movement, not hidden-plan alerts.

Fit:

- Core Level 2 doctrine.
- Pairs directly with terrain chokepoints, resource basins, and neutral power relays.

### Power-Strike Enemy

Purpose: make power infrastructure a battlefield target on both sides.

Direction:

- Enemy may prioritize exposed Pylons, Power Plants, Extractors, or powered add-ons over simple Colony Hub base-cracking.
- Power strikes should be inferred from visible enemy movement and attacks.
- No player-facing text should announce that the enemy secretly decided to attack power.
- The player should be able to scout, wall, repair, or punish the strike path.

Fit:

- Strong Stratezone identity.
- Should stay readable and fair, especially in early missions.

### Tower-Wall Response

Purpose: keep Defense Tower walls functional without making enemies helpless.

Direction:

- If blocked by a Defense Tower wall, enemy can attack a wall anchor, regroup, or reroute.
- The response should be simple and mission-local.
- The enemy should not magically know hidden wall plans before it encounters or scouts them.

Fit:

- Already aligned with current runtime direction.
- Expand carefully through tests and visible behavior.

### Defensive Posture

Purpose: make enemy bases feel like bases, not just unit spawn points.

Direction:

- Enemy should leave defenders at important infrastructure instead of committing every unit to attacks.
- Guard roles can cover Barracks, power, extractor, tower route, or Colony Hub.
- Defensive posture can vary by mission profile.

Fit:

- Important for resource-race and siege missions.
- Helps infrastructure strikes feel earned.

### Retreat and Regroup

Purpose: make enemy pressure feel military without creating opaque adaptation.

Direction:

- Damaged attackers may retreat toward base.
- Badly damaged returnees should not immediately recommit.
- Wiped attack groups can create an internal regroup delay.
- Retreat/regroup remains internal behavior, with no player-facing adaptation message.

Fit:

- Good if kept simple.
- Avoid making early enemies too slippery or annoying.

### Build-Order Personalities

Purpose: vary missions through profile settings instead of new factions.

Possible mission profiles:

- infantry rusher
- tower defender
- resource hoarder
- pylon striker
- turtle/siege base
- scout/harass force

Direction:

- Use these as authored mission profiles, not a generic AI personality system yet.
- Each profile should support a mission proof target.
- Avoid stacking too many profile behaviors in one mission.

Fit:

- Useful after Level 2 proves resource-race behavior.
- Good way to round out the same-tech enemy without new art/roster sprawl.

### Commander Awareness

Purpose: make exposed Commander positioning tense but fair.

Direction:

- Enemy can prioritize the Commander if he is visible and reachable.
- Enemy must not target or plan around the Commander when he is hidden under never-explored fog or otherwise unknown.
- Commander sightings can be remembered internally by rival-officer logic, but must not create player-facing adaptation text.

Fit:

- Strong fit when Commander is used as a real troop.
- Must remain fair and scout/visibility-driven.

### Under-Fog Rules

Purpose: preserve trust in fog of war.

Direction:

- Enemy can simulate production, rebuilding, and movement under fog.
- Player must not receive omniscient alerts about hidden enemy production, hidden rebuilds, or hidden attack planning.
- Sensor/radar infrastructure may reveal small authored areas only, not hidden intent.

Fit:

- Non-negotiable trust rule.
- Keeps same-tech enemy pressure from feeling like cheating.

### Enemy Rebuild Discipline

Purpose: make enemy infrastructure pressure trustworthy.

Direction:

- Enemy rebuilds only if it has resources and a mission profile permits rebuilding.
- No free rebuild cheating unless explicitly documented as a scenario exception.
- If a mission cheats for pacing, the doc and closeout should say so.

Fit:

- Strongly recommended for all first-demo missions.
- Keeps resource-race and infrastructure-strike routes honest.

## Enemy Doctrine Priority

Working doctrine sequence:

1. Level 1: tame same-tech scout/regroup/private-military pressure.
2. Level 2: resource-race enemy that contests wells and protects extractors.
3. Level 3: same-tech enemy introduces armor or hardened-defense pressure that makes Armory/Guardian useful.
4. Later: pylon striker, tower turtle, siege base, transport hunter, or scout/harass profiles.

Guardrails:

- No omniscient player-facing adaptation text.
- No broad skirmish AI before authored mission profiles prove value.
- No free enemy rebuild unless explicitly documented.
- Same-tech first enemy stays red/alternate-color until art direction proves a stronger need.

## Next Content Planning Topic

The next planning section should be support and siege entry gates: when Med Hall, Logistics / Repair Pad, Artillery Battery, Repair Platforms, and related sustain/siege ideas earn a mission slot without bloating the first demo.

## Commander Use Direction

Commander units should be real RTS troops: visible on the map, controllable, vulnerable, mission-relevant, and not abstract hero powers. Their role should support Dominion-style battlefield presence without turning every mission into an escort chore.

### Mission-Critical Troop

Purpose: make the Commander matter as a unit the player must protect.

Direction:

- Commander remains controllable.
- Commander remains fragile and pistol-only unless a later deliberate mission changes that.
- Commander death can trigger mission failure where the mission declares it.
- Keeping the Commander near home base should often be a sensible strategy.

Fit:

- Core identity, already accepted.
- Keep this as the default first-demo Commander behavior.

### Base Command Presence

Purpose: make Commander location affect production or base command.

Direction:

- Tabled for now as a direct rule.
- Do not make Barracks or Vehicle Bay globally depend on Commander proximity yet.
- Revisit only if a specific mission needs base command tension and can explain it clearly.

Fit:

- Interesting, but too risky as an always-on rule.
- Could make the Commander feel like a production totem instead of a unit if overused.

### Field Authorization

Purpose: have Commander activate, inspect, or authorize an objective in the field.

Direction:

- Tabled for now.
- If used later, it should be one important moment in a mission, not repeated errands.
- Avoid making it feel like a convoy/escort objective with a different name.

Fit:

- Possible later, but not a current first-demo commitment.

### Tactical Aura

Purpose: give nearby troops a passive benefit.

Direction:

- Tabled for now.
- Avoid hero-unit creep.
- Do not add morale, damage, speed, or broad command aura until the game proves it needs that layer.

Fit:

- Too easy to overcomplicate the Commander before the base RTS loop is mature.

### Command Link and Production Range

Purpose: tie base function, communications, and mission infrastructure together without making the Commander do constant errands.

Direction:

- Favored future angle.
- Some missions may use a command-link concept through Colony Hub, communications relay, Commander, or restored infrastructure.
- Structures might work fully only when connected to command through a clear mission rule.
- This should probably arrive after Level 2 terrain/resource grammar is proven.

Fit:

- Strong match for the "restore communications / cut off from home" framing.
- Better than making every production building depend directly on Commander proximity.

### Emergency Order

Purpose: give Commander a direct tactical ability such as fallback, rally, hold, or emergency repair priority.

Direction:

- Tabled for now.
- Avoid active ability UI until the core command bar and mission grammar are stable.

Fit:

- Could be useful later, but not needed for the first demo shape yet.

### Enemy Targeting

Purpose: make exposed Commander positioning tense and fair.

Direction:

- Enemy may prioritize Commander if he is visible and reachable.
- Enemy must not target or plan around the Commander when hidden in never-explored fog or otherwise unknown.
- Commander sightings can feed internal rival-officer memory only; they must not create player-facing adaptation text.

Fit:

- Approved and aligned with the fog-of-war trust rule.

### Commander Mission Variants

Purpose: vary Commander role across authored missions without making him the whole campaign structure.

Possible variants:

- default: stay alive and stay protected near base
- base command: Commander presence matters through a mission-specific command-link rule
- recovery: Commander survives an ambush or damaged outpost start
- extraction: Commander coordinates or survives a short end-state movement
- infrastructure: Commander interacts indirectly through command-link restoration, not repeated manual errands

Direction:

- Use only a few variants in the first demo.
- Commander should not become the repeated mission gimmick.
- Prefer command-link/infrastructure variants over direct escort repetition.

## Commander Use Priority

Approved now:

1. Mission-critical controllable troop.
2. Visible/reachable Commander can affect enemy target priority.
3. Occasional mission variants, used sparingly.
4. Command-link/production-range concept as the favored future expansion.

Tabled:

- direct Commander-at-base production dependency
- field authorization errands
- tactical aura
- emergency-order active abilities

Guardrails:

- no superhero Commander
- no repeated escort/convoy crutch
- no omniscient enemy targeting
- no global production dependency until a mission proves it is worth the complexity
