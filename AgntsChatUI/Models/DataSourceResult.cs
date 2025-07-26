namespace AgntsChatUI.Models
{
    using System;
    using System.Collections.Generic;

    // Unified result model for all data source searches
    public class DataSourceResult
    {
        public string? Content { get; set; }        // Full content (for documents) or relevant excerpt
        public string? Title { get; set; }          // Display title
        public string? SourceName { get; set; }     // Name of the data source
        public string? SourceType { get; set; }     // Type identifier
        public Dictionary<string, object>? Metadata { get; set; } // Additional context
        public DateTime RetrievedAt { get; set; }
    }
}