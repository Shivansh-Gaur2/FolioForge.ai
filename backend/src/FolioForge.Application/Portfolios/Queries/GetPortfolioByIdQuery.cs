using FolioForge.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Application.Portfolios.Queries
{
    public record GetPortfolioByIdQuery(Guid Id) : IRequest<PortfolioDto?>;
}
