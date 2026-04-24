using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleValidator : AbstractValidator<CancelSaleCommand>
{
    public CancelSaleValidator()
    {
        RuleFor(sale => sale.Id).NotEmpty();
    }
}
