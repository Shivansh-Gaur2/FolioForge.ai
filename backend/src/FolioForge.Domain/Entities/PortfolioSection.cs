using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FolioForge.Domain.Entities
{
    public class PortfolioSection : BaseEntity
    {
        public Guid PortfolioId { get; set; } 

        // The section type that will be used to determine the section
        // For now i am assuming major sections like "Timeline, Grid, markdown, Hero"
        public string SectionType { get; private set; } = default!;
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Display variant for customization (e.g. "default", "timeline", "card", "minimal").
        /// </summary>
        public string Variant { get; set; } = "default";

        // Store JSON content as a string for SQL Server compatibility
        // This allows flexible schema-less data storage
        public string Content { get; private set; } = default!;

        private PortfolioSection() { }
        public PortfolioSection(string sectionType, int order, string content)
        {
            SectionType = sectionType;
            SortOrder = order;
            Content = content;
        }

        // Will use Factory Pattern here because i will have to define 
        // the type, order, and data I need not to worry about how its created
        // I just need the object of that particular type so why not ?
        public static PortfolioSection Create(string type, int order, object data)
        {
            var json = JsonSerializer.Serialize(data);
            return new PortfolioSection(type, order, json);
        }

        // So as to update the Content of the object , object because it
        // would support all the classes
        public void UpdateContent(object newData)
        {
            Content = JsonSerializer.Serialize(newData);
        }

        /// <summary>
        /// Directly stores a pre-serialised JSON string (e.g. from a client PUT payload).
        /// Validates that the string is well-formed JSON before storing.
        /// </summary>
        /// <exception cref="JsonException">Thrown when <paramref name="jsonContent"/> is not valid JSON.</exception>
        public void UpdateContentRaw(string jsonContent)
        {
            // Parse-and-discard validates the JSON — throws JsonException on bad input
            using var _ = JsonDocument.Parse(jsonContent);
            Content = jsonContent;
        }

        // Helper method to deserialize content to a specific type
        public T? GetContent<T>()
        {
            return JsonSerializer.Deserialize<T>(Content);
        }

    }
}
