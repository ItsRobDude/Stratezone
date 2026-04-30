using Stratezone.Localization;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

public partial class Main
{
    private string L(string key, IReadOnlyDictionary<string, string>? args = null, string? fallback = null)
    {
        return _localization?.Translate(key, args, fallback) ?? fallback ?? $"[[{key}]]";
    }

    private string UnitName(UnitDefinition definition)
    {
        return _localization?.ContentName(definition.Id, definition.DisplayName) ?? definition.DisplayName;
    }

    private string BuildingName(BuildingDefinition definition)
    {
        return _localization?.ContentName(definition.Id, definition.DisplayName) ?? definition.DisplayName;
    }

    private string LocalizedMessage(string key, IReadOnlyDictionary<string, string>? args, string fallback)
    {
        return string.IsNullOrWhiteSpace(key)
            ? fallback
            : L(key, ResolveMessageArgs(args), fallback);
    }

    private string LocalizedPlacement(PlacementValidation validation)
    {
        return LocalizedMessage(validation.MessageKey, validation.MessageArgs, validation.Reason);
    }

    private string LocalizedPlacement(PlacementResult result)
    {
        return LocalizedMessage(result.MessageKey, result.MessageArgs, result.Message);
    }

    private string LocalizedProduction(ProductionValidation validation)
    {
        return LocalizedMessage(validation.MessageKey, validation.MessageArgs, validation.Reason);
    }

    private string LocalizedProduction(ProductionResult result)
    {
        return LocalizedMessage(result.MessageKey, result.MessageArgs, result.Message);
    }

    private string LocalizedUpgrade(UpgradeResult result)
    {
        return LocalizedMessage(result.MessageKey, result.MessageArgs, result.Message);
    }

    private string LocalizedMissionText(MissionState state)
    {
        return LocalizedMessage(state.PrimaryTextKey, state.PrimaryTextArgs, state.PrimaryText);
    }

    private IReadOnlyDictionary<string, string>? ResolveMessageArgs(IReadOnlyDictionary<string, string>? args)
    {
        if (args is null)
        {
            return null;
        }

        var resolved = new Dictionary<string, string>(args, StringComparer.Ordinal);
        foreach (var arg in args)
        {
            if (!arg.Key.EndsWith("Id", StringComparison.Ordinal))
            {
                continue;
            }

            var targetKey = arg.Key[..^2];
            resolved[targetKey] = _localization?.ContentName(arg.Value, args.TryGetValue(targetKey, out var fallback) ? fallback : arg.Value) ?? arg.Value;
        }

        return resolved;
    }
}
