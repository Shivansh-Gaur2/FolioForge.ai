using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Application.Common.Interfaces
{
    public interface IPdfService
    {
        string ExtractText(string filePath);
    }
}
