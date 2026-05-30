using System;
using System.Collections.Generic;

namespace OptiscalerClient.Models
{
    public enum CompatibilityStatus
    {
        Working,
        NotWorking,
        Partial
    }

    public class CompatibilityEntry
    {
        public string GameName { get; set; } = "";
        public string? WikiSlug { get; set; }
        public CompatibilityStatus Status { get; set; }
        public List<string> UpscalerInputs { get; set; } = new();
        public bool OptiPatcherSupported { get; set; }
        public string Notes { get; set; } = "";
        public Dictionary<string, string> ExtractedIniSettings { get; set; } = new();
    }

    public class CompatibilityCache
    {
        public List<CompatibilityEntry> Entries { get; set; } = new();
        public DateTime FetchedAt { get; set; } = DateTime.MinValue;
    }
}
