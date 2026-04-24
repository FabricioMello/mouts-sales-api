using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

public class SaleProfile : Profile
{
    public SaleProfile()
    {
        CreateMap<SaleItemRequest, SaleItemCommand>();
        CreateMap<CreateSale.CreateSaleRequest, CreateSaleCommand>();
        CreateMap<UpdateSale.UpdateSaleRequest, UpdateSaleCommand>();
        CreateMap<SaleResult, SaleResponse>();
        CreateMap<SaleItemResult, SaleItemResponse>();
    }
}
