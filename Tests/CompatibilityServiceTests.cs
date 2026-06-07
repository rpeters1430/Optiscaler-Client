using System.Collections.Generic;
using OptiscalerClient.Models;
using OptiscalerClient.Services;
using Xunit;

namespace OptiscalerClient.Tests
{
    public class CompatibilityServiceTests
    {
        [Theory]
        [InlineData("Cyberpunk 2077", "cyberpunk 2077")]
        [InlineData("Star Wars Jedi: Survivor!", "star wars jedi survivor")]
        [InlineData("  Portal    2  ", "portal 2")]
        [InlineData("GOTHIC-II", "gothic ii")]
        public void NormalizeName_NormalizesCorrectly(string input, string expected)
        {
            var result = CompatibilityService.NormalizeName(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("cyberpunk", "cyberpunk", 0)]
        [InlineData("cyberpunk", "cybeprunk", 2)] // two character swaps
        [InlineData("cyberpunk 2077", "cyberpunk 2078", 1)]
        [InlineData("abc", "def", 3)]
        public void LevenshteinDistance_ComputesCorrectly(string a, string b, int expectedDistance)
        {
            var result = CompatibilityService.LevenshteinDistance(a, b);
            Assert.Equal(expectedDistance, result);
        }

        [Fact]
        public void ExtractIniSettings_ParsesCorrectly()
        {
            // Direct matches without space
            var res1 = CompatibilityService.ExtractIniSettings("FakeNvapi=1");
            Assert.True(res1.ContainsKey("FakeNvapi"));
            Assert.Equal("1", res1["FakeNvapi"]);

            // Direct matches with space (the new feature!)
            var res2 = CompatibilityService.ExtractIniSettings("FakeNvapi = 1");
            Assert.True(res2.ContainsKey("FakeNvapi"));
            Assert.Equal("1", res2["FakeNvapi"]);

            // Multiple values embedded in a sentence with styling
            var notes = "Use **FakeNvapi = 1** and `KeepNvidiaFeatures = 0`. Also DepthToLinear=1.0.";
            var res3 = CompatibilityService.ExtractIniSettings(notes);
            Assert.Equal(3, res3.Count);
            Assert.Equal("1", res3["FakeNvapi"]);
            Assert.Equal("0", res3["KeepNvidiaFeatures"]);
            Assert.Equal("1.0", res3["DepthToLinear"]);

            // Excludes url fragments
            var urlNotes = "See http://example.com/api?param=1 for details.";
            var res4 = CompatibilityService.ExtractIniSettings(urlNotes);
            Assert.Empty(res4);
        }

        [Fact]
        public void ParseCompatibilityTable_ParsesMarkdownCorrectly()
        {
            var markdown = @"
# Compatibility List
Some introduction text.

| Game | Status | Inputs | OptiPatcher | Notes |
| :--- | :--- | :--- | :--- | :--- |
| [Cyberpunk 2077](cyberpunk-2077) | ✅ Working | dxgi, nvngx | ✨ Yes | Use FakeNvapi=1 |
| Portal 2 | ❌ Not Working | - | - | Broken |
| [Half-Life 2](hl-2) | ⚠️ Partial | dxgi | - | DepthToLinear = 1.0 |
";

            var entries = CompatibilityService.ParseCompatibilityTable(markdown);
            Assert.Equal(3, entries.Count);

            // Entry 1
            Assert.Equal("Cyberpunk 2077", entries[0].GameName);
            Assert.Equal("cyberpunk-2077", entries[0].WikiSlug);
            Assert.Equal(CompatibilityStatus.Working, entries[0].Status);
            Assert.Equal(new List<string> { "dxgi", "nvngx" }, entries[0].UpscalerInputs);
            Assert.True(entries[0].OptiPatcherSupported);
            Assert.Equal("Use FakeNvapi=1", entries[0].Notes);
            Assert.Equal("1", entries[0].ExtractedIniSettings["FakeNvapi"]);

            // Entry 2
            Assert.Equal("Portal 2", entries[1].GameName);
            Assert.Null(entries[1].WikiSlug);
            Assert.Equal(CompatibilityStatus.NotWorking, entries[1].Status);
            Assert.Equal(new List<string> { "-" }, entries[1].UpscalerInputs);
            Assert.False(entries[1].OptiPatcherSupported);
            Assert.Equal("Broken", entries[1].Notes);
            Assert.Empty(entries[1].ExtractedIniSettings);

            // Entry 3
            Assert.Equal("Half-Life 2", entries[2].GameName);
            Assert.Equal("hl-2", entries[2].WikiSlug);
            Assert.Equal(CompatibilityStatus.Partial, entries[2].Status);
            Assert.Equal(new List<string> { "dxgi" }, entries[2].UpscalerInputs);
            Assert.False(entries[2].OptiPatcherSupported);
            Assert.Equal("DepthToLinear = 1.0", entries[2].Notes);
            Assert.Equal("1.0", entries[2].ExtractedIniSettings["DepthToLinear"]);
        }

        [Fact]
        public void FindEntry_FindsCorrectMatch()
        {
            var service = new CompatibilityService();
            var entries = new List<CompatibilityEntry>
            {
                new() { GameName = "Cyberpunk 2077" },
                new() { GameName = "Portal 2" },
                new() { GameName = "Star Wars Jedi: Survivor" }
            };

            // Exact match
            var exact = service.FindEntry("Cyberpunk 2077", entries);
            Assert.NotNull(exact);
            Assert.Equal("Cyberpunk 2077", exact.GameName);

            // Case and whitespace normalized match
            var normalized = service.FindEntry("  portal   2 ", entries);
            Assert.NotNull(normalized);
            Assert.Equal("Portal 2", normalized.GameName);

            // Substring match
            var substring = service.FindEntry("Star Wars Jedi Survivor", entries);
            Assert.NotNull(substring);
            Assert.Equal("Star Wars Jedi: Survivor", substring.GameName);

            // Fuzzy match
            var fuzzy = service.FindEntry("Cyberpuk 2077", entries); // typo in cyberpunk
            Assert.NotNull(fuzzy);
            Assert.Equal("Cyberpunk 2077", fuzzy.GameName);

            // No match
            var none = service.FindEntry("Super Mario Bros", entries);
            Assert.Null(none);
        }

        [Fact]
        public void ParseLumaTable_ParsesMarkdownCorrectly()
        {
            var markdown = @"
## Luma Unreal Engine
| Game | Compatibility | Upscaler <br>Inputs | Notes | Images |
| ---- | :-----------: | :-----------------: | ------ | :----: |
| [Aliens: Fireteam Elite](Luma-Unreal-Engine-Luma-UE) | ✔️ Working | DLSS | Use DontUseNTShared=true | [1](url) |
";

            var entries = CompatibilityService.ParseCompatibilityTable(markdown);
            Assert.Single(entries);

            var entry = entries[0];
            Assert.Equal("Aliens: Fireteam Elite", entry.GameName);
            Assert.Equal("Luma-Unreal-Engine-Luma-UE", entry.WikiSlug);
            Assert.Equal(CompatibilityStatus.Working, entry.Status);
            Assert.Equal(new List<string> { "DLSS" }, entry.UpscalerInputs);
            Assert.False(entry.OptiPatcherSupported); // No OptiPatcher column
            Assert.Equal("Use DontUseNTShared=true", entry.Notes);
            Assert.True(entry.ExtractedIniSettings.ContainsKey("DontUseNTShared"));
            Assert.Equal("true", entry.ExtractedIniSettings["DontUseNTShared"]);
        }
    }
}
