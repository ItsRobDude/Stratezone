using Stratezone.Localization;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

internal sealed record SmokeTestContext(
    string RepoRoot,
    string GameRoot,
    ContentCatalog Catalog,
    LocalizationCatalog Localization,
    MissionDefinition FirstLandingMission,
    MissionDefinition WellsAtTheRidgeMission,
    MapDefinition WellsAtTheRidgeMap);

internal static class SmokeTestSupport
{
    public static SmokeTestContext LoadContext()
    {
        var repoRoot = FindRepoRoot();
        var gameRoot = Path.Combine(repoRoot, "game");
        var catalog = ContentCatalog.LoadFromGameData(gameRoot);
        var localization = LocalizationCatalog.LoadFromGameData(gameRoot);
        var firstLandingMission = catalog.GetMission(ContentIds.Missions.FirstLanding);
        var wellsAtTheRidgeMission = catalog.GetMission(ContentIds.Missions.WellsAtTheRidge);
        var wellsAtTheRidgeMap = catalog.GetMap(wellsAtTheRidgeMission.MapId);

        return new SmokeTestContext(
            repoRoot,
            gameRoot,
            catalog,
            localization,
            firstLandingMission,
            wellsAtTheRidgeMission,
            wellsAtTheRidgeMap);
    }

    public static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }

    public static float DamagePerSecondAgainst(UnitDefinition attacker, UnitDefinition target)
    {
        if (!attacker.CanAttack || attacker.AttackDamage <= 0.0f || attacker.AttackCooldown <= 0.0f)
        {
            return 0.0f;
        }

        return EffectiveDamage(attacker.AttackDamage, attacker.DamageType, target.DamageResistances) / attacker.AttackCooldown;
    }

    public static float BuildingDamagePerSecondAgainst(BuildingDefinition attacker, UnitDefinition target)
    {
        if (attacker.AttackDamage <= 0.0f || attacker.AttackCooldown <= 0.0f)
        {
            return 0.0f;
        }

        return EffectiveDamage(attacker.AttackDamage, attacker.DamageType, target.DamageResistances) / attacker.AttackCooldown;
    }

    public static void TickFor(RtsSimulation simulation, float seconds)
    {
        const float step = 0.1f;
        var elapsed = 0.0f;
        while (elapsed < seconds)
        {
            simulation.Tick(MathF.Min(step, seconds - elapsed));
            elapsed += step;
        }
    }

    private static float EffectiveDamage(float rawDamage, string damageType, IReadOnlyDictionary<string, float> resistances)
    {
        var resistance = resistances.TryGetValue(damageType, out var value)
            ? value
            : 0.0f;
        return rawDamage * MathF.Max(0.0f, 1.0f - resistance);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "game")) &&
                Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Stratezone repo root.");
    }
}
