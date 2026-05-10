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
- Mission starts are authored. No player-selected loadouts for now.
- Some missions may start with a partial base, allotted irreplaceable troops, or no base at all if the mission is about keeping a force moving.
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

### Mission 2: Resource Race, Chokepoints, and First Guardian Unlock

Purpose: prove strategic base-building through terrain and scarce wells while introducing Guardian production if the mission can carry that extra lesson cleanly.

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
- Armory Annex and Guardian production can enter here as the first powered add-on unlock
- enemy armor, hardened defense, or tower anchors should justify Guardian use if the unlock enters Level 2

Content boundary:

- same-tech enemy only, reskinned red or alternate color
- no Vehicle Bay requirement
- no broad tech layer beyond Armory/Guardian if Level 2 takes that unlock
- Med Hall, Logistics / Repair Pad, and Artillery stay out unless the mission cannot work without one

### Mission 3: Vehicle Bay and Rover Unlock Candidate

Purpose: prove the first vehicle-production unlock if the demo sequence is ready for mobile utility and vehicle sustain pressure.

Frame:

- After the expedition secures enough resources and field production, a Vehicle Bay can come online as the next production module.
- This should feel like the base is becoming operational, not like the player opened a broad research tree.

Gameplay shape:

- Vehicle Bay is a physical powered add-on adjacent to Barracks
- Rover production or Rover access is earned through mission setup, construction, restoration, or protection
- map routes should make mobile vehicle play useful without making infantry irrelevant
- power disruption can disable the unlock path
- the mission can assign limited or irreplaceable starting troops if the design needs tighter control

Content boundary:

- Vehicle Bay should enter the first demo, with Mission 3 as the working candidate and Mission 4 as the fallback slot
- do not add a broad tank roster just because Vehicle Bay appears
- no player loadout selection
- at most one support/siege system may appear if the mission proves a hard need

### Commander Field Operation

Purpose: use the Commander as a real troop without turning every mission into an escort problem.

Frame:

- Commander presence can represent battlefield authorization or coordination, but command-link unlock framing is not the preferred explanation for progression.
- If the Commander affects a mission system later, it should be mission-specific and rare rather than the default production rule.

Gameplay shape:

- Commander remains controllable, vulnerable, and mission-relevant
- a mission may ask the Commander to inspect, activate, secure, or coordinate one important objective only if that moment earns its place
- if the Commander affects base operation, that rule must be explicit, local to the mission, and easy to understand
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
2. Resource Race, Chokepoints, and likely first Guardian production
3. Vehicle Bay and Rover Unlock candidate
4. Broken Outpost Recovery, Vehicle Bay fallback, or Commander Field Operation
5. Siege Breaker, Transport/Extraction, or carefully authored Two-Front Defense

The fourth and fifth mission slots remain open. The current preference is to use those slots to round out Stratezone's identity without adding too many new systems: recovery/rescue, commander use, transport/extraction, Vehicle Bay fallback, or siege should each earn its place through one clean mission proof.

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
2. Level 2: resource-race enemy that contests wells, protects extractors, and may use armor or hardened-defense pressure if Armory/Guardian production enters here.
3. Level 3 or 4: same-tech enemy creates a reason for Vehicle Bay / Rover production without making infantry obsolete.
4. Later: pylon striker, tower turtle, siege base, transport hunter, or scout/harass profiles.

Guardrails:

- No omniscient player-facing adaptation text.
- No broad skirmish AI before authored mission profiles prove value.
- No free enemy rebuild unless explicitly documented.
- Same-tech first enemy stays red/alternate-color until art direction proves a stronger need.

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

- Not a current favorite for explaining progression.
- Communications can remain story framing, but do not make "command link restored" the default reason new buildings or units become available.
- If a mission later uses command-link behavior, it should be a local objective rule rather than a broad production system.
- Keep this tabled until a specific mission proves the need.

Fit:

- Possible later as a mission-specific infrastructure problem.
- Not needed for the first unlock pacing plan.

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
4. Commander can support a specific mission objective if the scenario earns it.

Tabled:

- direct Commander-at-base production dependency
- field authorization errands
- tactical aura
- emergency-order active abilities
- command-link/production-range as a broad unlock explanation

Guardrails:

- no superhero Commander
- no repeated escort/convoy crutch
- no omniscient enemy targeting
- no global production dependency until a mission proves it is worth the complexity

## Support and Siege Entry Gates

Support and siege systems should enter when they create a clear mission verb: hold, extract, break a fortified base, recover damaged infrastructure, or escort a Grunt to restore critical equipment. They should not enter just because their prototype records already exist.

### Med Hall

Purpose: provide slow infantry sustain when a mission's attrition would otherwise make the scenario feel brittle.

Direction:

- Keep Med Hall out of Level 1 and probably Level 2.
- Best current fit is a later authored scenario, possibly a snow-map extraction or withdrawal mission.
- The snow setting should start as mission framing and readability, not an automatic new temperature-survival system.
- Med Hall should require power, heal infantry slowly, and spend resources only while healing.

Fit:

- Good later if infantry losses are the intended pressure.
- Not needed until a mission proves basic infantry attrition is the actual problem.

### Logistics / Repair Pad

Purpose: repair vehicles through infrastructure rather than turning Grunts into front-line vehicle mechanics.

Direction:

- Recommendation accepted, with one hard boundary: Grunts repair buildings only, not vehicles.
- Logistics / Repair Pad repairs mechanical units parked on it, requires power, and spends resources only while actually repairing.
- It should enter only after vehicles matter enough to justify a dedicated sustain structure.
- It should not replace Grunt building repair or make vehicle mistakes cheap everywhere on the map.

Fit:

- Better after Vehicle Bay/Rover or tank-focused missions exist.
- Keep deferred unless a vehicle-heavy mission needs it.

### Artillery Battery

Purpose: give the player a siege answer to fortified bases without making infantry blobs the best solution.

Direction:

- Recommended and approved as the strongest first-demo siege candidate.
- It should be static, powered, fragile, expensive, long-range, and unable to defend itself at close range.
- It should use explosive damage and friendly fire so positioning still matters.

Fit:

- Strong candidate for a later Siege Breaker mission.
- Should arrive only when the map has a fortified target, power vulnerability, and enemy counterplay.

### Powered Hackable Defense Equipment

Purpose: turn support/siege into a scenario objective instead of a generic capture-point layer.

Direction:

- Defense or support equipment must be powered first, then hacked or controlled by a physically present Grunt.
- The Grunt action should take time and leave the Grunt vulnerable.
- The object can be a ruined turret, defense console, neutral repair platform, or other authored mission device.
- The object should not be active and hostile while the player is expected to hack it unless the mission gives a fair way to cut power first.

Fit:

- Strong scenario hook: escort a Grunt to take over defense equipment under pressure.
- Works especially well for recovery, extraction, or fortified-position missions.

### Neutral Repair Platform

Purpose: provide rare mission-specific sustain without exposing player-built Logistics / Repair Pad too early.

Direction:

- Use only as authored mission infrastructure.
- It must be powered first, then hacked or controlled by a Grunt before it functions.
- It may repair vehicles once active, but the repair comes from the platform, not from Grunt vehicle repair.
- Keep it rare so it does not become a generic free-heal station.

Fit:

- Good for a vehicle-heavy recovery or extraction scenario.
- Lower priority than Artillery Battery and powered hackable defense equipment.

### Support as Mission Objective

Purpose: make support systems produce missions instead of passive tech bloat.

Direction:

- Preferred form: escort a Grunt to restore, hack, or control a specific piece of defense equipment.
- Success should open a defense route, stabilize an extraction, or give the player a temporary foothold.
- Avoid making this a repeated escort gimmick; one mission can use it strongly, then move on.

Fit:

- Strong candidate for Level 4 or Level 5.
- Pairs well with a damaged outpost, snow extraction, bridge route, or fortified enemy push.

## Support and Siege Priority

Approved direction:

1. First demo uses no more than one or two support/siege systems unless the roadmap is deliberately reopened.
2. Artillery Battery is the strongest siege candidate.
3. Powered hackable defense equipment is the strongest support-objective candidate.
4. Med Hall can fit a later snow/extraction scenario if infantry attrition needs support.
5. Logistics / Repair Pad waits until vehicles are central, and Grunts still cannot repair vehicles.
6. Neutral Repair Platform is rare authored infrastructure, powered first and Grunt-controlled after that.

Guardrails:

- no support system in First Landing unless playtests prove a hard need
- no Grunt vehicle repair
- no generic capture-point economy
- no support/siege layer added without a mission proof target

## Progression and Unlock Pacing

Progression should feel like an expedition becoming more capable across authored missions, not like a permanent campaign-tech tree. Each mission defines its own start, available units, available buildings, failure conditions, and proof target.

### Authored Starts

Purpose: keep mission design tight while the game is pre-demo.

Direction:

- No player-selected loadouts for now.
- Mission data owns starting units, starting buildings, starting resources, available trainable units, and available build commands.
- Some missions can start with a partially built base.
- Some missions can start with allotted or irreplaceable troops.
- Some missions can have no base at all if the point is to keep a force moving, extract, or survive a route.

Fit:

- Strongly recommended for the first demo.
- Prevents a loadout UI, balance matrix, and persistent progression layer from distracting from mission proof.

### Physical Unlocks

Purpose: make progression happen through RTS objects on the map instead of abstract menu research.

Direction:

- Armory Annex unlocks Guardian/explosive tech where the mission allows it.
- Vehicle Bay unlocks Rover/heavy-armor capacity where the mission allows it.
- Add-ons should be physical, powered, attackable, and adjacent to Barracks.
- Unlocks should be readable through mission setup, construction, restoration, or protection.

Fit:

- Strong fit for Stratezone's power-and-infrastructure identity.
- Keeps progression tied to map pressure.

### Demo Unlock Sequence

Working direction:

1. Level 1: train Grunt, Cadet, and Rifleman only; Guardian, Rover, and Commander are authored starting/scenario units.
2. Level 2: likely introduce Armory Annex and Guardian production inside the resource-race/chokepoint mission if that does not overload the lesson.
3. Level 3: preferred working slot for Vehicle Bay and Rover production.
4. Level 4: fallback Vehicle Bay slot if Level 3 needs to become recovery, no-base movement, or equipment takeover instead.
5. Level 5: support/siege payoff such as Artillery Battery, powered Grunt-hacked defense equipment, extraction, or siege breaker.

Guardrails:

- no permanent campaign tech tree in the first demo
- no player loadout screen
- no command-link restoration as the default unlock explanation
- no Vehicle Bay in Level 1
- no broad tank roster just because Vehicle Bay enters the demo

## Mission Start and Failure-Condition Patterns

Mission starts and failure conditions should create variety without turning every level into an escort mission. Each mission should have one primary failure idea, with secondary failure rules used only when they are obvious and fair.

### Full Base Start

Purpose: preserve the classic RTS spine.

Direction:

- Start with a Colony Hub, Commander, at least one Grunt, basic resources, and a path to build outward.
- Use this when the mission teaches economy, power, wells, Defense Tower walls, add-ons, or enemy-base pressure.
- Normal failure conditions are Commander death, Colony Hub destruction, or loss of another clearly declared critical structure.

Fit:

- Default pattern for First Landing and Resource Race.
- Best when the mission's main verb is build, scout, expand, defend, or attack.

### Partial Base Start

Purpose: make recovery and repair feel like real RTS pressure.

Direction:

- Start with a damaged, unpowered, or incomplete base.
- Use damaged towers, broken Pylons, offline Barracks, low resources, or a vulnerable Grunt to make recovery readable.
- Failure can come from Commander death if present, critical structure destruction, required equipment loss, or all required Grunts dying when the mission explicitly depends on Grunt work.

Fit:

- Strong candidate for Mission 4.
- Good for Broken Outpost Recovery, powered hackable defense equipment, and Grunt-value proof.

### No-Base Moving Force

Purpose: vary pacing with a fixed force and no production.

Direction:

- Start with a fixed group and no base.
- Use for a snow extraction, patrol, survivor recovery, or route-survival mission.
- Failure should be tied to a visible critical unit, transport, extraction target, or squad-survival requirement.

Fit:

- Use at most once in the five-level demo.
- Strong change of pace, but too much of it would pull Stratezone toward squad tactics instead of mission RTS.

### Allotted or Irreplaceable Troops

Purpose: create tension without adding a loadout system.

Direction:

- Mission data can grant specific units that cannot be replaced in that scenario.
- Use this for rare Guardians, Rovers before production is unlocked, a required Grunt, or a mission-critical Commander.
- Player-facing messaging must make irreplaceable units clear.

Fit:

- Good for recovery, extraction, and early vehicle showcase missions.
- Use sparingly so losses feel like tactical stakes, not hidden punishment.

### Commander Failure

Purpose: keep command presence important without making the Commander a recurring escort chore.

Direction:

- Commander death can fail missions where the Commander is declared mission-critical.
- Full-base missions can keep the Commander near home base as the sensible play.
- Avoid combining Commander failure with too many other instant-loss conditions.

Fit:

- Core to First Landing.
- Good as an anchor failure rule for some base missions, not every mission variant.

### Transport and Extraction Failure

Purpose: add movement and urgency without long escort slog.

Direction:

- Prefer short, authored transport or extraction phases.
- Strong forms include destroying an enemy transport before it escapes, holding a landing zone, reaching extraction, or protecting a transport during a brief final phase.
- Avoid long transport babysitting routes unless the map is built tightly around that one idea.

Fit:

- Good Mission 5 candidate.
- Pairs well with snow extraction, siege closeout, or no-base moving-force scenarios.

### Timer Failures

Purpose: clarify that timed mission loss is not part of the current plan.

Direction:

- Do not use timer-expiry loss conditions in the first demo unless the roadmap is explicitly reopened.
- Timers may still exist as visible pacing feedback for arrivals, extraction countdowns, attack warnings, or optional pressure, but they should not be the mission failure condition.
- Avoid hidden timers entirely.

Fit:

- This preserves experimentation and keeps failures tactical rather than schedule-driven.

### Base Destruction Failure

Purpose: match failure rules to the mission fantasy.

Direction:

- In build-and-hold missions, Colony Hub destruction usually means loss.
- In recovery or extraction missions, losing a forward outpost may not be instant failure if the objective is to evacuate or complete a route.
- Critical structures should be called out by objective text before their loss can fail the mission.

Fit:

- Strong baseline for base missions.
- Should vary only when the mission is clearly not about holding a base.

## Mission Start Priority

Working first-demo pattern:

1. Level 1: full base start; Commander and Hub failure.
2. Level 2: full base start; Commander and Hub failure; resource race plus likely Armory/Guardian.
3. Level 3: full or partial base start; Vehicle Bay/Rover unlock candidate.
4. Level 4: partial base, equipment takeover, recovery, or Vehicle Bay fallback.
5. Level 5: siege, extraction, snow extraction, or no-base moving force, with one clear alternate failure rule.

Guardrails:

- no player-selected loadouts
- no timer-expiry mission failures for the first demo
- no stacking every failure type into one mission
- no long repeated escort structure
- no hidden critical-unit punishment without clear objective messaging

## Encounter Pacing and Enemy Pressure

Enemy pressure should feel like a rival RTS force operating under authored mission rules, not like invisible wave math. The player should infer threat from scouting, visible movement, visible infrastructure, and attacks on known assets.

### Pressure Rhythm

Purpose: make missions tense without becoming constant waves.

Direction:

- Use a readable rhythm: opening grace, scout/probe, contested objective, main pressure, then mission-specific pressure beat.
- Leave breathing room after meaningful attacks so the player can repair, rebuild, scout, or counterattack.
- Early danger should come more from target choice and map route than from raw enemy count.

Fit:

- Core first-demo pacing rule.
- Keeps pressure compatible with quick RTS restart and authored mission learning.

### Enemy Group Sizes

Purpose: keep fights legible while the player is still learning the game's grammar.

Direction:

- Level 1: 1-3 attackers, mostly Cadet/Rifleman, with defenders kept at enemy base.
- Level 2: 2-5 attackers plus resource-race behavior around wells, Extractors, Pylons, and Defense Tower wall routes.
- Level 3: 3-6 attackers or one readable vehicle/armor pressure beat if Vehicle Bay/Rover or Guardian counters are being taught.
- Level 4/5: larger pressure only when the mission has already provided the tools and map shape to answer it.

Fit:

- Keeps old-school RTS readability without making early missions passive.

### Readable Targeting

Purpose: make enemy pressure teach infrastructure value.

Direction:

- Enemy should prefer meaningful visible targets before simply cracking the Colony Hub: exposed Grunts, Extractors, Pylons, Power Plants, lonely Defense Towers, forward Barracks/add-ons, mission equipment, or transports.
- Commander can be prioritized only if visible and reachable.
- Player-facing warnings should be classic known-event warnings: enemy spotted, own asset under attack, power offline, construction complete, training complete.
- Do not add alerts like "enemy is planning to attack your Pylon." Let the map show it.

Fit:

- Strong identity fit. Stratezone should make power, wells, and Grunts feel tactically exposed.

### Raid Triggers and Coalescing

Purpose: prevent early base-building milestones from stacking into unfair back-to-back raids.

Direction:

- Condition triggers are useful, but they need mission-level pacing guards.
- Building Barracks and building the first Extractor can happen back to back in normal base starts, so they must not independently fire immediate separate raids.
- Opening pressure triggers should share a grace window, cooldown, or trigger group.
- Multiple early triggers should coalesce into one planned pressure beat, increase future interest, or be ignored if a raid is already queued.
- Prefer condition triggers for authored beats, but avoid "every event fires as soon as its condition is true" behavior.

Fit:

- Required for Level 2 and later base-building missions.
- Protects fairness without removing authored reactivity.

### Enemy Rebuilding

Purpose: make pressure feel fair and raidable.

Direction:

- Enemy rebuilds only from resources and only when the mission profile allows it.
- Rebuild cadence should be slower in early missions.
- Destroying enemy Extractors, Pylons, or power should visibly slow pressure.
- Any pacing cheat must be explicitly documented as a scenario exception.

Fit:

- Strong Level 2 proof target.
- Keeps infrastructure strikes more important than pure unit trading.

### Early Armor

Purpose: introduce armor as a readable lesson, not a surprise punishment.

Direction:

- Armor or hardened defense should appear only after the player has, can unlock, or has been taught the intended answer.
- Level 2 can use hardened defenses, tower anchors, or one light armor beat if Armory/Guardian enters there.
- Level 3 can use vehicle pressure if Vehicle Bay/Rover is being taught.
- Telegraph armor through mission setup, visible enemy tech, wrecks, or scouted structures.

Fit:

- Keeps Guardian and Rover unlocks meaningful instead of arbitrary.

### Retreat and Regroup

Purpose: make the enemy feel smarter without requiring skirmish-grade AI.

Direction:

- Damaged committed attackers may retreat if the mission profile allows it.
- Wiped attack groups create a regroup delay before the next committed attack.
- Defenders should not all abandon the enemy base.
- Rival-officer memory can tune regroup timing or target weights internally, but never produces player-facing hidden-plan alerts.

Fit:

- Good first-demo AI polish once basic pressure is stable.

## Encounter Pressure Priority

Working first-demo pressure pattern:

1. Level 1: slow probes, one small committed group, visible defenders, protect Grunts and power.
2. Level 2: resource-race pressure, well contesting, Extractor/Pylon targeting, trigger coalescing, possible Guardian-justifying hardened target.
3. Level 3: route pressure and mobile/vehicle pressure that explains Vehicle Bay/Rover.
4. Level 4: recovery or equipment-takeover pressure aimed at the Grunt route or restored equipment after the objective is readable.
5. Level 5: fortified enemy, siege pressure, extraction pressure, or final counterattack after the player has the tools to respond.

Guardrails:

- no omniscient hidden-plan alerts
- no stacked opening raids from back-to-back base-building triggers
- no free enemy rebuild unless explicitly documented
- no surprise armor before the counter is available or readable
- no constant wave-spawner feel

## Next Content Planning Topic

The next planning section should be player-facing mission presentation: briefings, objective text, warning language, map callouts, and how much story flavor to use without turning the demo into a cutscene campaign.
