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
        public Guid PortfolioId { get; private set; }

        // The section type that will be used to determine the section
        // For now i am assuming major sections like "Timeline, Grid, markdown, Hero"
        public string SectionType { get; private set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;

        // Store JSON content as a string for SQL Server compatibility
        // This allows flexible schema-less data storage
        public string Content { get; private set; }

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

        // Helper method to deserialize content to a specific type
        public T? GetContent<T>()
        {
            return JsonSerializer.Deserialize<T>(Content);
        }

    }
}
