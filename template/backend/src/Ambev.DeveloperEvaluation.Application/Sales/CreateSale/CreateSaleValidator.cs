using Ambev.DeveloperEvaluation.Application.Sales.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(sale => sale.SaleNumber).NotEmpty().MaximumLength(50);
        RuleFor(sale => sale.SaleDate).NotEmpty();
        RuleFor(sale => sale.CustomerId).NotEmpty();
        RuleFor(sale => sale.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(sale => sale.BranchId).NotEmpty();
        RuleFor(sale => sale.BranchName).NotEmpty().MaximumLength(100);
        RuleFor(sale => sale.Items).NotEmpty();
        RuleForEach(sale => sale.Items).SetValidator(new SaleItemCommandValidator());
    }
}
