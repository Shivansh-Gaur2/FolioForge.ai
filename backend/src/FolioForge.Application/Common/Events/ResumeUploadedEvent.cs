using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Application.Common.Events
{
    public record ResumeUploadedEvent(Guid PortfolioId, string FilePath);
}
