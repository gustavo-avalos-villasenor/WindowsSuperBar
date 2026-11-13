using System;
using System.Collections.Generic;
using System.IO;

internal static class ManagementExtensions
{
    public const string AppDataFolderName = "WorkWindowsBar";
    private const string LegacyAppDataFolderName = "RetroBar";

    public static string InLocalAppData(this string path1, string path2 = "")
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppDataFolderName, path1, path2);
    }

    /// <summary>
    /// First run after the rename: copy settings.json and custom Themes from %LOCALAPPDATA%\RetroBar
    /// into %LOCALAPPDATA%\WorkWindowsBar. The old folder is left untouched.
    /// </summary>
    public static void MigrateLegacyAppData()
    {
        try
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string oldDir = Path.Combine(root, LegacyAppDataFolderName);
            string newDir = Path.Combine(root, AppDataFolderName);
            string newSettings = Path.Combine(newDir, "settings.json");
            string oldSettings = Path.Combine(oldDir, "settings.json");

            if (File.Exists(newSettings) || !File.Exists(oldSettings))
            {
                return;
            }

            Directory.CreateDirectory(newDir);
            File.Copy(oldSettings, newSettings);

            string oldThemes = Path.Combine(oldDir, "Themes");
            if (Directory.Exists(oldThemes))
            {
                string newThemes = Path.Combine(newDir, "Themes");
                Directory.CreateDirectory(newThemes);
                foreach (string file in Directory.GetFiles(oldThemes))
                {
                    File.Copy(file, Path.Combine(newThemes, Path.GetFileName(file)), false);
                }
            }
        }
        catch (Exception)
        {
            // Not fatal: the app falls back to default settings.
        }
    }

    public static void AddFrom(this List<string> list, string path, string extension, bool skipExisting = false)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(path, $"*.{extension}"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (skipExisting && list.Contains(fileName))
            {
                continue;
            }
            list.Add(fileName);
        }
    }
}