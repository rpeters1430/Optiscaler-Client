using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OptiscalerClient.Services;

namespace OptiscalerClient.Views
{
    public partial class LogViewerWindow : Window
    {
        private readonly string _gameDir;
        private readonly string _gameName;
        private string? _currentFilePath;

        public LogViewerWindow()
        {
            InitializeComponent();
            _gameDir = "";
            _gameName = "";
        }

        public LogViewerWindow(Window owner, string gameDir, string gameName)
        {
            InitializeComponent();
            Owner = owner;
            _gameDir = gameDir;
            _gameName = gameName;
            Title = $"OptiScaler Logs - {gameName}";

            PopulateLogFiles();
        }

        private void PopulateLogFiles()
        {
            var cmbLogFile = this.FindControl<ComboBox>("CmbLogFile");
            if (cmbLogFile == null) return;

            var potentialFiles = new[]
            {
                "OptiScaler.log",
                "fakenvapi.log",
                "OptiPatcher.log",
                "OptiScaler.ini",
                "fakenvapi.ini"
            };

            var items = new List<ComboBoxItem>();
            foreach (var name in potentialFiles)
            {
                var fullPath = Path.Combine(_gameDir, name);
                if (File.Exists(fullPath))
                {
                    items.Add(new ComboBoxItem
                    {
                        Content = name,
                        Tag = fullPath
                    });
                }
            }

            cmbLogFile.ItemsSource = items;

            if (items.Count > 0)
            {
                cmbLogFile.SelectedIndex = 0;
            }
            else
            {
                var txtLogContent = this.FindControl<SelectableTextBlock>("TxtLogContent");
                if (txtLogContent != null)
                {
                    txtLogContent.Text = $"No log files (OptiScaler.log, fakenvapi.log) found in directory:\n{_gameDir}";
                }
                var txtLogStatus = this.FindControl<TextBlock>("TxtLogStatus");
                if (txtLogStatus != null)
                {
                    txtLogStatus.Text = "No log files found.";
                }
            }
        }

        private void LoadLogFile(string filePath)
        {
            _currentFilePath = filePath;
            var txtLogContent = this.FindControl<SelectableTextBlock>("TxtLogContent");
            var txtLogStatus = this.FindControl<TextBlock>("TxtLogStatus");

            if (txtLogContent == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                if (txtLogContent != null) txtLogContent.Text = "File not found or empty.";
                return;
            }

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);
                var content = reader.ReadToEnd();

                if (string.IsNullOrEmpty(content))
                {
                    txtLogContent.Text = "(Empty file)";
                }
                else
                {
                    txtLogContent.Text = content;
                }

                if (txtLogStatus != null)
                {
                    var fileInfo = new FileInfo(filePath);
                    txtLogStatus.Text = $"Loaded {Path.GetFileName(filePath)} ({fileInfo.Length} bytes) - Last Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
                }
            }
            catch (Exception ex)
            {
                txtLogContent.Text = $"Error loading file:\n{ex.Message}";
                if (txtLogStatus != null) txtLogStatus.Text = "Error reading file.";
            }
        }

        private void CmbLogFile_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem item && item.Tag is string filePath)
            {
                LoadLogFile(filePath);
            }
        }

        private void BtnRefresh_Click(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                LoadLogFile(_currentFilePath);
            }
        }

        private async void BtnCopy_Click(object? sender, RoutedEventArgs e)
        {
            var txtLogContent = this.FindControl<SelectableTextBlock>("TxtLogContent");
            if (txtLogContent != null && !string.IsNullOrEmpty(txtLogContent.Text))
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(txtLogContent.Text);
                }
            }
        }

        private void BtnOpenDir_Click(object? sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_gameDir))
            {
                try
                {
                    PlatformServiceFactory.CreateShellService().OpenFolder(_gameDir);
                }
                catch (Exception ex)
                {
                    DebugWindow.Log($"[LogViewer] Failed to open folder: {ex.Message}");
                }
            }
        }

        private void BtnClearLog_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath)) return;

            try
            {
                using (var fs = new FileStream(_currentFilePath, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite))
                {
                }
                LoadLogFile(_currentFilePath);
            }
            catch (Exception ex)
            {
                var txtLogContent = this.FindControl<SelectableTextBlock>("TxtLogContent");
                if (txtLogContent != null)
                {
                    txtLogContent.Text = $"Failed to clear file (it might be locked by the game):\n{ex.Message}";
                }
            }
        }
    }
}
