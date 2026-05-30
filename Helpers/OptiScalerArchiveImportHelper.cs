using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using OptiscalerClient.Services;
using OptiscalerClient.Views;

namespace OptiscalerClient.Helpers
{
    internal static class OptiScalerArchiveImportHelper
    {
        public static async Task<string?> PromptAndImportAsync(
            Window owner,
            ComponentManagementService componentService,
            string requestedVersion,
            string failureMessage)
        {
            var useLocalArchive = await new ConfirmDialog(
                owner,
                "OptiScaler Download Unavailable",
                $"OptiScaler v{requestedVersion} could not be downloaded.\n\n{failureMessage}\n\nSelect a local OptiScaler archive (.7z, .zip, or .rar) to continue with a custom version?")
                .ShowDialog<bool>(owner);

            if (!useLocalArchive)
                return null;

            var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select OptiScaler Archive",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("OptiScaler archives")
                    {
                        Patterns = new[] { "*.7z", "*.zip", "*.rar" },
                    },
                },
            });

            if (files == null || files.Count == 0)
                return null;

            var filePath = files[0].Path.IsAbsoluteUri
                ? files[0].Path.LocalPath
                : files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            try
            {
                return await componentService.ImportCustomOptiScalerVersionAsync(filePath);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $"\n{ex.InnerException.Message}" : "";
                await new ConfirmDialog(
                    owner,
                    "Import Error",
                    $"Failed to import custom OptiScaler archive:\n{ex.Message}{innerMsg}",
                    isAlert: true).ShowDialog<bool>(owner);
                return null;
            }
        }
    }
}
