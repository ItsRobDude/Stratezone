using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

internal static class CombatResolver
{
    public static bool ResolveUnitAttack(
        UnitState attacker,
        UnitState directTarget,
        IReadOnlyList<UnitState> units,
        IReadOnlyList<BuildingState> buildings)
    {
        if (!CanAttack(attacker.Definition.AttackDamage, attacker.AttackCooldownRemaining, directTarget.IsDestroyed))
        {
            return false;
        }

        var destroyedBuilding = ResolveDamage(
            attacker.EntityId,
            attacker.FactionId,
            attacker.Position,
            attacker.Definition.AttackDamage,
            attacker.Definition.DamageType,
            attacker.Definition.AreaRadius,
            attacker.Definition.FriendlyFire,
            attacker.Definition.TargetFilters,
            directTarget.Position,
            directTarget,
            null,
            units,
            buildings);
        attacker.RegisterOutgoingAttack(directTarget.Position);
        attacker.AttackCooldownRemaining = MathF.Max(0.05f, attacker.Definition.AttackCooldown);
        return destroyedBuilding;
    }

    public static bool ResolveUnitAttack(
        UnitState attacker,
        BuildingState directTarget,
        IReadOnlyList<UnitState> units,
        IReadOnlyList<BuildingState> buildings)
    {
        if (!CanAttack(attacker.Definition.AttackDamage, attacker.AttackCooldownRemaining, directTarget.IsDestroyed))
        {
            return false;
        }

        var destroyedBuilding = ResolveDamage(
            attacker.EntityId,
            attacker.FactionId,
            attacker.Position,
            attacker.Definition.AttackDamage,
            attacker.Definition.DamageType,
            attacker.Definition.AreaRadius,
            attacker.Definition.FriendlyFire,
            attacker.Definition.TargetFilters,
            directTarget.Position,
            null,
            directTarget,
            units,
            buildings);
        attacker.RegisterOutgoingAttack(directTarget.Position);
        attacker.AttackCooldownRemaining = MathF.Max(0.05f, attacker.Definition.AttackCooldown);
        return destroyedBuilding;
    }

    public static bool ResolveBuildingAttack(
        BuildingState attacker,
        UnitState directTarget,
        IReadOnlyList<UnitState> units,
        IReadOnlyList<BuildingState> buildings)
    {
        if (!CanAttack(attacker.Definition.AttackDamage, attacker.AttackCooldownRemaining, directTarget.IsDestroyed))
        {
            return false;
        }

        var destroyedBuilding = ResolveDamage(
            attacker.EntityId,
            attacker.FactionId,
            attacker.Position,
            attacker.Definition.AttackDamage,
            attacker.Definition.DamageType,
            attacker.Definition.AreaRadius,
            attacker.Definition.FriendlyFire,
            attacker.Definition.TargetFilters,
            directTarget.Position,
            directTarget,
            null,
            units,
            buildings);
        attacker.AttackCooldownRemaining = MathF.Max(0.05f, attacker.Definition.AttackCooldown);
        return destroyedBuilding;
    }

    public static bool ResolveBuildingAttack(
        BuildingState attacker,
        BuildingState directTarget,
        IReadOnlyList<UnitState> units,
        IReadOnlyList<BuildingState> buildings)
    {
        if (!CanAttack(attacker.Definition.AttackDamage, attacker.AttackCooldownRemaining, directTarget.IsDestroyed))
        {
            return false;
        }

        var destroyedBuilding = ResolveDamage(
            attacker.EntityId,
            attacker.FactionId,
            attacker.Position,
            attacker.Definition.AttackDamage,
            attacker.Definition.DamageType,
            attacker.Definition.AreaRadius,
            attacker.Definition.FriendlyFire,
            attacker.Definition.TargetFilters,
            directTarget.Position,
            null,
            directTarget,
            units,
            buildings);
        attacker.AttackCooldownRemaining = MathF.Max(0.05f, attacker.Definition.AttackCooldown);
        return destroyedBuilding;
    }

    public static bool TryCrushInfantry(UnitState crusher, SimVector2 start, SimVector2 end, IReadOnlyList<UnitState> units)
    {
        if (!crusher.Definition.CanRunOverInfantry || crusher.Definition.RunOverDamage <= 0.0f)
        {
            return false;
        }

        var didCrush = false;
        foreach (var target in units)
        {
            if (target.EntityId == crusher.EntityId ||
                target.FactionId == crusher.FactionId ||
                target.IsDestroyed ||
                !target.Definition.Tags.Contains("infantry", StringComparer.Ordinal))
            {
                continue;
            }

            if (DistancePointToSegment(target.Position, start, end) > 20.0f)
            {
                continue;
            }

            target.ApplyDamage(crusher.Definition.RunOverDamage, crusher.Definition.RunOverDamageType);
            didCrush = true;
        }

        return didCrush;
    }

    private static bool ResolveDamage(
        int sourceEntityId,
        string sourceFactionId,
        SimVector2 sourcePosition,
        float damage,
        string damageType,
        float areaRadius,
        bool friendlyFire,
        IReadOnlyList<string> targetFilters,
        SimVector2 center,
        UnitState? directUnit,
        BuildingState? directBuilding,
        IReadOnlyList<UnitState> units,
        IReadOnlyList<BuildingState> buildings)
    {
        var radius = areaRadius > 0.0f ? RtsSimulation.ToWorldRadius(areaRadius) : 0.0f;

        if (areaRadius <= 0.0f)
        {
            if (directUnit is not null &&
                directUnit.EntityId != sourceEntityId &&
                !directUnit.IsDestroyed &&
                CanDamageFaction(sourceFactionId, directUnit.FactionId, friendlyFire) &&
                MatchesUnitFilters(directUnit.Definition, targetFilters))
            {
                directUnit.ApplyDamage(damage, damageType);
                directUnit.RegisterIncomingAttack(sourcePosition);
            }

            if (directBuilding is not null &&
                directBuilding.EntityId != sourceEntityId &&
                !directBuilding.IsDestroyed &&
                CanDamageFaction(sourceFactionId, directBuilding.FactionId, friendlyFire) &&
                targetFilters.Contains("building", StringComparer.Ordinal))
            {
                var destroyed = directBuilding.ApplyDamage(damage, damageType);
                directBuilding.RegisterIncomingAttack(sourcePosition);
                return destroyed;
            }

            return false;
        }

        var destroyedBuilding = false;

        foreach (var unit in units)
        {
            if (unit.EntityId == sourceEntityId ||
                unit.IsDestroyed ||
                !CanDamageFaction(sourceFactionId, unit.FactionId, friendlyFire) ||
                !MatchesUnitFilters(unit.Definition, targetFilters) ||
                !IsInDamageArea(unit.Position, center, radius))
            {
                continue;
            }

            unit.ApplyDamage(damage, damageType);
            unit.RegisterIncomingAttack(sourcePosition);
        }

        foreach (var building in buildings)
        {
            if (building.EntityId == sourceEntityId ||
                building.IsDestroyed ||
                !CanDamageFaction(sourceFactionId, building.FactionId, friendlyFire) ||
                !targetFilters.Contains("building", StringComparer.Ordinal) ||
                !IsInDamageArea(building.Position, center, radius + building.FootprintWorldRadius))
            {
                continue;
            }

            destroyedBuilding |= building.ApplyDamage(damage, damageType);
            building.RegisterIncomingAttack(sourcePosition);
        }

        return destroyedBuilding;
    }

    private static bool CanAttack(float damage, float cooldown, bool targetDestroyed)
    {
        return cooldown <= 0.0f && !targetDestroyed && damage > 0.0f;
    }

    private static bool CanDamageFaction(string sourceFactionId, string targetFactionId, bool friendlyFire)
    {
        return friendlyFire || sourceFactionId != targetFactionId;
    }

    private static bool MatchesUnitFilters(UnitDefinition unit, IReadOnlyList<string> targetFilters)
    {
        return targetFilters.Count == 0 ||
            unit.Tags.Any(tag => targetFilters.Contains(tag, StringComparer.Ordinal));
    }

    private static bool IsInDamageArea(SimVector2 position, SimVector2 center, float radius)
    {
        return position.DistanceTo(center) <= radius;
    }

    private static float DistancePointToSegment(SimVector2 point, SimVector2 start, SimVector2 end)
    {
        var segment = end - start;
        var lengthSquared = (segment.X * segment.X) + (segment.Y * segment.Y);
        if (lengthSquared <= 0.0001f)
        {
            return point.DistanceTo(start);
        }

        var pointOffset = point - start;
        var t = ((pointOffset.X * segment.X) + (pointOffset.Y * segment.Y)) / lengthSquared;
        t = Math.Clamp(t, 0.0f, 1.0f);
        var projection = start + (segment * t);
        return point.DistanceTo(projection);
    }
}
