using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public class SaleProfile : Profile
{
    public SaleProfile()
    {
        CreateMap<Sale, SaleResult>();
        CreateMap<SaleItem, SaleItemResult>();
    }
}
