using FolioForge.Application.Common.Interfaces;
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
