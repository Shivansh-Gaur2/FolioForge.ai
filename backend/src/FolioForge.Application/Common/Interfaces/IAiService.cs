using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Application.Common.Interfaces
{
    public interface IAiService
    {
        // This method must take the resume text as imput and i am expecting a string 
        // or basically json string that contains all the data 
        Task<string> GeneratePortfolioDataAsync(string resumeText);
    }
}
