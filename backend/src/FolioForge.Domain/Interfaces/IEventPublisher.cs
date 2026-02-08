using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Domain.Interfaces
{
    public interface IEventPublisher
    {
        // T would take events like ResumeUploadedEvent, PortfolioCreatedEvent, etc.
        // (@event): In C#, event is a reserved keyword. By adding the @ symbol, the developer can use "event" as a variable name
        Task PublishAsync<T>(T @event) where T : class;
    }
}
