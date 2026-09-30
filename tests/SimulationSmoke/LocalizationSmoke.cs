using Stratezone.Localization;

internal static class LocalizationSmoke
{
    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), $"stratezone_locale_smoke_{Guid.NewGuid():N}");
        try
        {
            var i18nRoot = Path.Combine(root, "data", "i18n");
            Directory.CreateDirectory(i18nRoot);
            File.WriteAllText(
                Path.Combine(i18nRoot, "en.json"),
                """
                {
                  "strings": {
                    "ui.test": "Fallback works"
                  }
                }
                """);

            var fallback = LocalizationCatalog.LoadFromGameData(root, "es");
            SmokeTestSupport.Assert(fallback.Warnings.Count == 1, "missing optional locale records a warning");
            SmokeTestSupport.Assert(fallback.Catalog.Translate("ui.test") == "Fallback works", "missing optional locale falls back to English");

            File.Delete(Path.Combine(i18nRoot, "en.json"));
            var missingEnglishThrows = false;
            try
            {
                LocalizationCatalog.LoadFromGameData(root);
            }
            catch (FileNotFoundException)
            {
                missingEnglishThrows = true;
            }

            SmokeTestSupport.Assert(missingEnglishThrows, "missing English locale is a hard failure");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
