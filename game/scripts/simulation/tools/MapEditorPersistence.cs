using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stratezone.Simulation.Tools;

public sealed record MapEditorSavePreview(
    string MissionSourcePath,
    string MapSourcePath,
    string OriginalMissionText,
    string ProposedMissionText,
    string OriginalMapText,
    string ProposedMapText,
    string DiffText,
    int SessionRevision)
{
    public bool MissionChanged => !string.Equals(OriginalMissionText, ProposedMissionText, StringComparison.Ordinal);
    public bool MapChanged => !string.Equals(OriginalMapText, ProposedMapText, StringComparison.Ordinal);
    public bool HasChanges => MissionChanged || MapChanged;
}

public sealed record MapEditorSaveResult(
    bool Success,
    bool WroteMission,
    bool WroteMap,
    string Message);

public static class MapEditorPersistence
{
    public static MapEditorSavePreview CreatePreview(string gameRoot, MapEditorSession session)
    {
        if (string.IsNullOrWhiteSpace(gameRoot))
        {
            throw new ArgumentException("Game root is required for map editor save.", nameof(gameRoot));
        }

        if (string.IsNullOrWhiteSpace(session.MissionId))
        {
            throw new InvalidOperationException("Map editor cannot save because no mission is loaded.");
        }

        if (string.IsNullOrWhiteSpace(session.MapId))
        {
            throw new InvalidOperationException("Map editor cannot save because no map is loaded.");
        }

        var missionPath = FindMissionSourcePath(gameRoot, session.MissionId);
        var mapPath = Path.Combine(gameRoot, "data", "maps", "maps.json");
        var missionText = File.ReadAllText(missionPath);
        var mapText = File.ReadAllText(mapPath);

        var proposedMissionText = ReplaceArrayPropertyValue(
            missionText,
            session.MissionId,
            "mission_markers",
            FormatArray(session.Markers, MapEditorSession.FormatMissionMarker));
        var proposedMapText = ReplaceArrayPropertyValue(
            mapText,
            session.MapId,
            "terrain_regions",
            FormatArray(session.Regions, MapEditorSession.FormatTerrainRegion));
        proposedMapText = ReplaceArrayPropertyValue(
            proposedMapText,
            session.MapId,
            "map_objects",
            FormatArray(session.Objects, MapEditorSession.FormatMapObject));

        ValidateJson(missionPath, proposedMissionText);
        ValidateJson(mapPath, proposedMapText);

        var diff = new StringBuilder();
        diff.Append(BuildDiff(missionPath, missionText, proposedMissionText));
        diff.AppendLine();
        diff.Append(BuildDiff(mapPath, mapText, proposedMapText));

        return new MapEditorSavePreview(
            missionPath,
            mapPath,
            missionText,
            proposedMissionText,
            mapText,
            proposedMapText,
            diff.ToString(),
            session.Revision);
    }

    public static MapEditorSaveResult WritePreview(MapEditorSavePreview preview)
    {
        if (!preview.HasChanges)
        {
            return new MapEditorSaveResult(true, false, false, "No map editor changes to save.");
        }

        var wroteMission = false;
        var wroteMap = false;
        if (preview.MissionChanged)
        {
            WriteAtomicWithBackup(preview.MissionSourcePath, preview.ProposedMissionText);
            wroteMission = true;
        }

        if (preview.MapChanged)
        {
            WriteAtomicWithBackup(preview.MapSourcePath, preview.ProposedMapText);
            wroteMap = true;
        }

        var saved = new List<string>();
        if (wroteMission)
        {
            saved.Add(Path.GetFileName(preview.MissionSourcePath));
        }

        if (wroteMap)
        {
            saved.Add(Path.GetFileName(preview.MapSourcePath));
        }

        return new MapEditorSaveResult(true, wroteMission, wroteMap, $"Saved {string.Join(", ", saved)}.");
    }

    private static string FindMissionSourcePath(string gameRoot, string missionId)
    {
        var missionDirectory = Path.Combine(gameRoot, "data", "missions");
        foreach (var path in Directory.EnumerateFiles(missionDirectory, "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("records", out var records) ||
                records.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var record in records.EnumerateArray())
            {
                if (record.TryGetProperty("id", out var id) &&
                    string.Equals(id.GetString(), missionId, StringComparison.Ordinal))
                {
                    return path;
                }
            }
        }

        throw new FileNotFoundException($"No mission source JSON contains mission '{missionId}'.", missionDirectory);
    }

    private static string ReplaceArrayPropertyValue(string source, string recordId, string propertyName, string arrayJson)
    {
        var recordSpan = FindRecordSpan(source, recordId);
        var propertyPattern = new Regex($"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\\[", RegexOptions.CultureInvariant);
        var match = propertyPattern.Match(source, recordSpan.Start, recordSpan.Length);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Record '{recordId}' does not contain array property '{propertyName}'.");
        }

        var arrayStart = source.IndexOf('[', match.Index, match.Length);
        if (arrayStart < 0)
        {
            throw new InvalidOperationException($"Record '{recordId}' property '{propertyName}' is not an array.");
        }

        var arrayEnd = FindMatchingDelimiter(source, arrayStart, '[', ']');
        return source[..arrayStart] + arrayJson + source[(arrayEnd + 1)..];
    }

    private static TextSpan FindRecordSpan(string source, string recordId)
    {
        var idPattern = new Regex($"\"id\"\\s*:\\s*\"{Regex.Escape(recordId)}\"", RegexOptions.CultureInvariant);
        var match = idPattern.Match(source);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find record id '{recordId}'.");
        }

        var objectStart = FindObjectStartBefore(source, match.Index);
        var objectEnd = FindMatchingDelimiter(source, objectStart, '{', '}');
        return new TextSpan(objectStart, objectEnd - objectStart + 1);
    }

    private static int FindObjectStartBefore(string source, int index)
    {
        var inString = false;
        var escaped = false;
        var lastObjectStart = -1;
        for (var i = 0; i < index; i++)
        {
            var current = source[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
            }
            else if (current == '{')
            {
                lastObjectStart = i;
            }
        }

        if (lastObjectStart < 0)
        {
            throw new InvalidOperationException("Could not find JSON object start.");
        }

        return lastObjectStart;
    }

    private static int FindMatchingDelimiter(string source, int start, char open, char close)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var i = start; i < source.Length; i++)
        {
            var current = source[i];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            if (current == open)
            {
                depth++;
            }
            else if (current == close)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        throw new InvalidOperationException($"Could not find matching '{close}' for '{open}' at index {start}.");
    }

    private static string FormatArray<T>(IReadOnlyList<T> items, Func<T, string> formatter)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[");
        for (var index = 0; index < items.Count; index++)
        {
            builder.Append(Indent(formatter(items[index]), 8));
            builder.AppendLine(index == items.Count - 1 ? string.Empty : ",");
        }

        builder.Append("      ]");
        return builder.ToString();
    }

    private static string Indent(string value, int spaces)
    {
        var prefix = new string(' ', spaces);
        return string.Join(
            Environment.NewLine,
            value.Split(Environment.NewLine).Select(line => line.Length == 0 ? line : prefix + line));
    }

    private static void ValidateJson(string path, string text)
    {
        try
        {
            using var _ = JsonDocument.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Map editor generated invalid JSON for '{path}': {exception.Message}", exception);
        }
    }

    private static string BuildDiff(string path, string before, string after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            return $"--- {path}{Environment.NewLine}+++ {path}{Environment.NewLine} no changes{Environment.NewLine}";
        }

        var beforeLines = NormalizeLines(before);
        var afterLines = NormalizeLines(after);
        var prefix = 0;
        while (prefix < beforeLines.Length &&
            prefix < afterLines.Length &&
            string.Equals(beforeLines[prefix], afterLines[prefix], StringComparison.Ordinal))
        {
            prefix++;
        }

        var suffix = 0;
        while (suffix < beforeLines.Length - prefix &&
            suffix < afterLines.Length - prefix &&
            string.Equals(
                beforeLines[beforeLines.Length - suffix - 1],
                afterLines[afterLines.Length - suffix - 1],
                StringComparison.Ordinal))
        {
            suffix++;
        }

        var contextBefore = Math.Max(0, prefix - 3);
        var beforeChangedEnd = beforeLines.Length - suffix;
        var afterChangedEnd = afterLines.Length - suffix;
        var contextAfterBefore = Math.Min(beforeLines.Length, beforeChangedEnd + 3);
        var contextAfterAfter = Math.Min(afterLines.Length, afterChangedEnd + 3);

        var builder = new StringBuilder();
        builder.AppendLine($"--- {path}");
        builder.AppendLine($"+++ {path}");
        builder.AppendLine($"@@ line {contextBefore + 1} @@");
        for (var index = contextBefore; index < prefix; index++)
        {
            builder.AppendLine($" {beforeLines[index]}");
        }

        for (var index = prefix; index < beforeChangedEnd; index++)
        {
            builder.AppendLine($"-{beforeLines[index]}");
        }

        for (var index = prefix; index < afterChangedEnd; index++)
        {
            builder.AppendLine($"+{afterLines[index]}");
        }

        for (var beforeIndex = beforeChangedEnd; beforeIndex < contextAfterBefore; beforeIndex++)
        {
            var afterIndex = afterChangedEnd + (beforeIndex - beforeChangedEnd);
            var line = afterIndex < contextAfterAfter ? afterLines[afterIndex] : beforeLines[beforeIndex];
            builder.AppendLine($" {line}");
        }

        return builder.ToString();
    }

    private static string[] NormalizeLines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n');
    }

    private static void WriteAtomicWithBackup(string path, string text)
    {
        var tempPath = path + ".tmp";
        var backupPath = path + ".bak";
        File.WriteAllText(tempPath, text, Encoding.UTF8);
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        try
        {
            File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private readonly record struct TextSpan(int Start, int Length);
}
