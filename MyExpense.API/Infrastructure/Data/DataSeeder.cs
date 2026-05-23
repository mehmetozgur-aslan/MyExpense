using MyExpense.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MyExpense.API.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();

        await db.Database.EnsureCreatedAsync();

        await SeedCurrentMonthBudgetAsync(db);
    }

    private static async Task SeedCurrentMonthBudgetAsync(ExpenseDbContext db)
    {
        var now = DateTime.UtcNow;

        var exists = await db.Budgets
            .AnyAsync(b => b.Month == now.Month && b.Year == now.Year);

        if (exists)
        {
            return;
        }

        db.Budgets.Add(new Budget
        {
            Month = now.Month,
            Year = now.Year,
            Limit = 5000m
        });

        await db.SaveChangesAsync();
    }
}
