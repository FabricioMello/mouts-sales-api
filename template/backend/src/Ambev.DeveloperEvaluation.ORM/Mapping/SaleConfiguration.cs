using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(sale => sale.SaleNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(sale => sale.SaleNumber).IsUnique();

        builder.Property(sale => sale.SaleDate).IsRequired();
        builder.Property(sale => sale.CustomerId).IsRequired();
        builder.Property(sale => sale.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(sale => sale.BranchId).IsRequired();
        builder.Property(sale => sale.BranchName).IsRequired().HasMaxLength(100);
        builder.Property(sale => sale.TotalAmount).HasPrecision(18, 2);
        builder.Property(sale => sale.IsCancelled).IsRequired();
        builder.Property(sale => sale.CreatedAt).IsRequired();
        builder.Property(sale => sale.UpdatedAt);

        builder.HasIndex(sale => sale.CustomerId);
        builder.HasIndex(sale => sale.BranchId);
        builder.HasIndex(sale => sale.SaleDate).IsDescending();
        builder.HasIndex(sale => sale.IsCancelled);

        builder
            .HasMany(sale => sale.Items)
            .WithOne(item => item.Sale)
            .HasForeignKey(item => item.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(sale => sale.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
