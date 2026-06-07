using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OptiscalerClient.Models;
using OptiscalerClient.Views;

namespace OptiscalerClient.Services
{
    public class CompatibilityService
    {
        private const string WikiUrl = "https://raw.githubusercontent.com/wiki/optiscaler/OptiScaler/Compatibility-List.md";

        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptiscalerClient", "compat_cache.json");

        private static List<CompatibilityEntry>? _memoryCache;
        private static DateTime _memoryCacheTime = DateTime.MinValue;

        public async Task<List<CompatibilityEntry>> GetEntriesAsync()
        {
            if (_memoryCache != null && (DateTime.Now - _memoryCacheTime).TotalMinutes < 5)
                return _memoryCache;

            if (TryLoadDiskCache(out var cached))
            {
                _memoryCache = cached!;
                _memoryCacheTime = DateTime.Now;
                return cached!;
            }

            try
            {
                var markdown = await NetworkService.GetHttpClient()
                    .GetStringAsync(WikiUrl)
                    .ConfigureAwait(false);
                var entries = ParseCompatibilityTable(markdown);
                SaveDiskCache(entries);
                _memoryCache = entries;
                _memoryCacheTime = DateTime.Now;
                return entries;
            }
            catch (Exception ex)
            {
                DebugWindow.Log($"[Compat] Fetch failed: {ex.Message}");
                if (TryLoadDiskCache(out var stale, ignoreAge: true))
                    return stale!;
                return new List<CompatibilityEntry>();
            }
        }

        public CompatibilityEntry? FindEntry(string gameName, List<CompatibilityEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(gameName) || entries.Count == 0) return null;

            var normalized = NormalizeName(gameName);
            if (normalized.Length == 0) return null;

            // Exact normalized match
            var exact = entries.FirstOrDefault(e => NormalizeName(e.GameName) == normalized);
            if (exact != null) return exact;

            // Substring match: game name contains the entry name or vice versa
            var sub = entries.FirstOrDefault(e =>
            {
                var en = NormalizeName(e.GameName);
                return en.Length >= 4 && (en.Contains(normalized) || normalized.Contains(en));
            });
            if (sub != null) return sub;

            // Fuzzy: Levenshtein with a tight threshold
            CompatibilityEntry? best = null;
            int bestDist = int.MaxValue;
            int threshold = Math.Max(2, normalized.Length / 6);

            foreach (var e in entries)
            {
                var en = NormalizeName(e.GameName);
                if (Math.Abs(en.Length - normalized.Length) > threshold) continue;
                int dist = LevenshteinDistance(normalized, en);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = e;
                }
            }

            return bestDist <= threshold ? best : null;
        }

        // ── Parsing ──────────────────────────────────────────────────────────

        internal static List<CompatibilityEntry> ParseCompatibilityTable(string markdown)
        {
            var entries = new List<CompatibilityEntry>();

            // Default column indices for the main table (fallback)
            int gameColIdx = 1;
            int statusColIdx = 2;
            int inputsColIdx = 3;
            int optiPatcherColIdx = 4;
            int notesColIdx = 5;

            string lastLine = "";

            foreach (var rawLine in markdown.Split('\n'))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("|"))
                {
                    lastLine = line;
                    continue;
                }

                if (IsSeparatorRow(line))
                {
                    // The line before this separator was the header row!
                    if (!string.IsNullOrWhiteSpace(lastLine) && lastLine.StartsWith("|"))
                    {
                        var headerCells = lastLine.Split('|');
                        for (int i = 1; i < headerCells.Length - 1; i++)
                        {
                            var header = headerCells[i].Trim().ToLowerInvariant();
                            if (header.Contains("game"))
                            {
                                gameColIdx = i;
                            }
                            else if (header.Contains("compatibility") || header.Contains("status"))
                            {
                                statusColIdx = i;
                            }
                            else if (header.Contains("input"))
                            {
                                inputsColIdx = i;
                            }
                            else if (header.Contains("optipatcher"))
                            {
                                optiPatcherColIdx = i;
                            }
                            else if (header.Contains("notes"))
                            {
                                notesColIdx = i;
                            }
                        }
                    }
                    lastLine = line;
                    continue;
                }

                var cells = line.Split('|');
                // Minimum cells length: game, status/compatibility, inputs.
                if (cells.Length < 5)
                {
                    lastLine = line;
                    continue;
                }

                // If this row is the header itself, skip it
                if (gameColIdx < cells.Length && cells[gameColIdx].Trim().Equals("Game", StringComparison.OrdinalIgnoreCase))
                {
                    lastLine = line;
                    continue;
                }

                var gameCell = gameColIdx < cells.Length ? cells[gameColIdx].Trim() : "";
                var statusCell = statusColIdx < cells.Length ? cells[statusColIdx].Trim() : "";
                var inputsCell = inputsColIdx < cells.Length ? cells[inputsColIdx].Trim() : "";
                var optiPatcherCell = optiPatcherColIdx >= 0 && optiPatcherColIdx < cells.Length ? cells[optiPatcherColIdx].Trim() : "";
                var notesCell = notesColIdx >= 0 && notesColIdx < cells.Length ? cells[notesColIdx].Trim() : "";

                // Parse game name and optional wiki slug
                string gameName;
                string? wikiSlug = null;
                var linkMatch = Regex.Match(gameCell, @"\[([^\]]+)\]\(([^)]+)\)");
                if (linkMatch.Success)
                {
                    gameName = linkMatch.Groups[1].Value.Trim();
                    wikiSlug = linkMatch.Groups[2].Value.Trim();
                }
                else
                {
                    gameName = StripMarkdown(gameCell).Trim();
                }

                if (string.IsNullOrWhiteSpace(gameName) || gameName.Equals("Game", StringComparison.OrdinalIgnoreCase))
                {
                    lastLine = line;
                    continue;
                }

                var status = statusCell.Contains("✅") || statusCell.Contains("✔️") ? CompatibilityStatus.Working
                    : statusCell.Contains("❌") ? CompatibilityStatus.NotWorking
                    : CompatibilityStatus.Partial;

                var inputs = inputsCell.Split(',')
                    .Select(s => StripMarkdown(s).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                bool optiPatcher = optiPatcherCell.Contains("✨") || optiPatcherCell.Contains("Yes") || optiPatcherCell.Contains("✅");
                var notes = StripMarkdown(notesCell).Trim();
                var iniSettings = ExtractIniSettings(notesCell);

                entries.Add(new CompatibilityEntry
                {
                    GameName = gameName,
                    WikiSlug = wikiSlug,
                    Status = status,
                    UpscalerInputs = inputs,
                    OptiPatcherSupported = optiPatcher,
                    Notes = notes,
                    ExtractedIniSettings = iniSettings
                });

                lastLine = line;
            }

            return entries;
        }

        private static bool IsSeparatorRow(string line)
        {
            var cells = line.Split('|').Skip(1).SkipLast(1);
            return cells.Any() && cells.All(c =>
            {
                var t = c.Trim();
                return t.Length > 0 && t.All(ch => ch == '-' || ch == ':' || ch == ' ');
            });
        }

        private static string StripMarkdown(string text)
        {
            // Remove markdown links [text](url)
            text = Regex.Replace(text, @"\[([^\]]*)\]\([^)]*\)", "$1");
            // Remove bold/italic
            text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "$1");
            text = Regex.Replace(text, @"\*([^*]+)\*", "$1");
            // Remove inline code
            text = Regex.Replace(text, @"`([^`]+)`", "$1");
            // Replace <br> with space
            text = Regex.Replace(text, @"<br\s*/?>", " ", RegexOptions.IgnoreCase);
            // Remove remaining HTML tags
            text = Regex.Replace(text, @"<[^>]+>", "");
            // Collapse multiple spaces
            text = Regex.Replace(text, @"\s{2,}", " ");
            return text.Trim();
        }

        internal static Dictionary<string, string> ExtractIniSettings(string rawCell)
        {
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Strip markdown emphasis/code markers but keep the text
            var stripped = Regex.Replace(rawCell, @"\*\*|`", "");
            // Match patterns like KEY = value where key starts with a letter and optional spaces/signs are allowed, avoiding trailing sentence punctuation and URL parameters
            var matches = Regex.Matches(stripped, @"(?<![A-Za-z0-9_?&/:\\])([A-Za-z][A-Za-z0-9_]*)\s*=\s*([-+]?[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)*)(?![A-Za-z0-9_])");
            foreach (Match m in matches)
            {
                var key = m.Groups[1].Value;
                var val = m.Groups[2].Value;
                // Exclude obvious URL fragments and single-char values that look like noise
                if (key.Length < 3 || val.Length == 0) continue;
                settings[key] = val;
            }
            return settings;
        }

        // ── Fuzzy matching ────────────────────────────────────────────────────

        internal static string NormalizeName(string name)
        {
            // Lowercase, strip non-alphanumeric, collapse spaces
            var result = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9 ]", " ");
            return Regex.Replace(result, @"\s+", " ").Trim();
        }

        internal static int LevenshteinDistance(string a, string b)
        {
            // Cap to avoid quadratic cost on very long strings
            if (a.Length > 60) a = a[..60];
            if (b.Length > 60) b = b[..60];

            var dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }

            return dp[a.Length, b.Length];
        }

        // ── Cache ─────────────────────────────────────────────────────────────

        private static bool TryLoadDiskCache(out List<CompatibilityEntry>? entries, bool ignoreAge = false)
        {
            entries = null;
            try
            {
                if (!File.Exists(CacheFilePath)) return false;
                var json = File.ReadAllText(CacheFilePath);
                var cache = JsonSerializer.Deserialize(json, OptimizerContext.Default.CompatibilityCache);
                if (cache?.Entries == null || cache.Entries.Count == 0) return false;
                if (!ignoreAge && (DateTime.Now - cache.FetchedAt).TotalHours > 24) return false;
                entries = cache.Entries;
                return true;
            }
            catch (Exception ex)
            {
                DebugWindow.Log($"[Compat] Cache load failed: {ex.Message}");
                return false;
            }
        }

        private static void SaveDiskCache(List<CompatibilityEntry> entries)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
                var cache = new CompatibilityCache { Entries = entries, FetchedAt = DateTime.Now };
                var json = JsonSerializer.Serialize(cache, OptimizerContext.Default.CompatibilityCache);
                File.WriteAllText(CacheFilePath, json);
            }
            catch (Exception ex)
            {
                DebugWindow.Log($"[Compat] Cache save failed: {ex.Message}");
            }
        }
    }
}
