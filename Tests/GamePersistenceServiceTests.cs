using System;
using System.Collections.Generic;
using System.IO;
using OptiscalerClient.Models;
using OptiscalerClient.Services;
using Xunit;

namespace OptiscalerClient.Tests
{
    public class GamePersistenceServiceTests : IDisposable
    {
        private readonly string _tempFile;

        public GamePersistenceServiceTests()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), "OptiscalerClientTests", $"{Guid.NewGuid()}.json");
        }

        public void Dispose()
        {
            if (File.Exists(_tempFile))
            {
                try { File.Delete(_tempFile); } catch { }
            }
            var dir = Path.GetDirectoryName(_tempFile);
            if (dir != null && Directory.Exists(dir))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void LoadGames_ReturnsEmptyList_WhenFileDoesNotExist()
        {
            var service = new GamePersistenceService(_tempFile);
            var games = service.LoadGames();

            Assert.NotNull(games);
            Assert.Empty(games);
        }

        [Fact]
        public void SaveAndLoadGames_RoundtripsSuccessfully()
        {
            var service = new GamePersistenceService(_tempFile);

            var originalGames = new List<Game>
            {
                new()
                {
                    Name = "Cyberpunk 2077",
                    InstallPath = "C:\\Games\\Cyberpunk 2077",
                    Platform = GamePlatform.Steam,
                    AppId = "1091500",
                    ExecutablePath = "bin\\x64\\Cyberpunk2077.exe",
                    DlssVersion = "3.5.0",
                    DlssPath = "bin\\x64\\nvngx_dlss.dll",
                    IsOptiscalerInstalled = true,
                    OptiscalerVersion = "0.9.2"
                },
                new()
                {
                    Name = "Portal 2",
                    InstallPath = "C:\\Games\\Portal 2",
                    Platform = GamePlatform.Manual,
                    AppId = "portal2",
                    IsHidden = true,
                    DisplayOrder = 10
                }
            };

            service.SaveGames(originalGames);
            Assert.True(File.Exists(_tempFile));

            var loadedGames = service.LoadGames();
            Assert.NotNull(loadedGames);
            Assert.Equal(2, loadedGames.Count);

            // Game 1
            Assert.Equal("Cyberpunk 2077", loadedGames[0].Name);
            Assert.Equal("C:\\Games\\Cyberpunk 2077", loadedGames[0].InstallPath);
            Assert.Equal(GamePlatform.Steam, loadedGames[0].Platform);
            Assert.Equal("1091500", loadedGames[0].AppId);
            Assert.Equal("bin\\x64\\Cyberpunk2077.exe", loadedGames[0].ExecutablePath);
            Assert.Equal("3.5.0", loadedGames[0].DlssVersion);
            Assert.Equal("bin\\x64\\nvngx_dlss.dll", loadedGames[0].DlssPath);
            Assert.True(loadedGames[0].IsOptiscalerInstalled);
            Assert.Equal("0.9.2", loadedGames[0].OptiscalerVersion);

            // Game 2
            Assert.Equal("Portal 2", loadedGames[1].Name);
            Assert.Equal(GamePlatform.Manual, loadedGames[1].Platform);
            Assert.Equal("portal2", loadedGames[1].AppId);
            Assert.True(loadedGames[1].IsHidden);
            Assert.Equal(10, loadedGames[1].DisplayOrder);
        }

        [Fact]
        public void LoadGames_ReturnsEmptyList_WhenFileContainsCorruptJson()
        {
            var service = new GamePersistenceService(_tempFile);
            var dir = Path.GetDirectoryName(_tempFile);
            if (dir != null) Directory.CreateDirectory(dir);

            File.WriteAllText(_tempFile, "{ invalid json }");

            var games = service.LoadGames();
            Assert.NotNull(games);
            Assert.Empty(games);
        }
    }
}
