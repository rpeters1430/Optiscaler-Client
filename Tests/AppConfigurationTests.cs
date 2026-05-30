using System.Text.Json;
using OptiscalerClient.Models;
using Xunit;

namespace OptiscalerClient.Tests;

public class AppConfigurationTests
{
    [Fact]
    public void DefaultsIncludePinnedLatestBeta()
    {
        var config = new AppConfiguration();

        var pinned = Assert.Single(config.PinnedOptiScalerBetaReleases);
        Assert.Equal("0.9.3-pre2", pinned.Version);
        Assert.Equal(
            "https://github.com/rpeters1430/Optiscaler-Client/releases/download/optiscaler-beta-0.9.3-pre2-20260528-007/Optiscaler_0.9.3-pre2_20260528_007.7z",
            pinned.DownloadUrl);
    }

    [Fact]
    public void SourceGeneratedJsonRoundTripsPinnedBetaReleases()
    {
        var config = new AppConfiguration
        {
            PinnedOptiScalerBetaReleases =
            [
                new PinnedOptiScalerRelease
                {
                    Version = "0.9.3-pre2",
                    DownloadUrl = "https://example.invalid/Optiscaler_0.9.3-pre2.7z",
                },
            ],
        };

        var json = JsonSerializer.Serialize(config, OptimizerContext.Default.AppConfiguration);
        var roundTripped = JsonSerializer.Deserialize(json, OptimizerContext.Default.AppConfiguration);

        Assert.NotNull(roundTripped);
        var pinned = Assert.Single(roundTripped.PinnedOptiScalerBetaReleases);
        Assert.Equal("0.9.3-pre2", pinned.Version);
        Assert.Equal("https://example.invalid/Optiscaler_0.9.3-pre2.7z", pinned.DownloadUrl);
    }
}
