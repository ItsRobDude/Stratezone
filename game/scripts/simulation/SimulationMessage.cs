namespace Stratezone.Simulation;

public static class SimulationMessage
{
    public static IReadOnlyDictionary<string, string> Args(params (string Key, object Value)[] values)
    {
        return values.ToDictionary(
            item => item.Key,
            item => Convert.ToString(item.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            StringComparer.Ordinal);
    }
}
