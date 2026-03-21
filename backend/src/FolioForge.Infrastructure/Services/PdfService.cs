using System.Diagnostics;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Infrastructure.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace FolioForge.Infrastructure.Services
{
    public class PdfService : IPdfService
    {
        public string ExtractText(string filePath)
        {
            // start a tracing span around PDF extraction
            using var activity = FolioForgeDiagnostics.ActivitySource.StartActivity(
                FolioForgeDiagnostics.ExtractPdf,
                ActivityKind.Internal);
            activity?.SetTag("pdf.filepath", filePath);

            if(!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var sb = new StringBuilder();

            using ( var pdf = PdfDocument.Open(filePath) )
            {
                foreach ( var page in pdf.GetPages() )
                {
                    sb.Append(page.Text);
                    sb.Append(" ");
                }
            }

            return sb.ToString().Trim();
        }
    }
}
