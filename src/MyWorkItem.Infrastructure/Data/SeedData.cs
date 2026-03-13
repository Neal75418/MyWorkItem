using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyWorkItem.Application.Interfaces;
using MyWorkItem.Domain.Entities;
using MyWorkItem.Domain.Enums;

namespace MyWorkItem.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var hasher = serviceProvider.GetRequiredService<IPasswordHasher>();

        if (await context.Users.AnyAsync()) return;

        var admin = new User
        {
            Username = "admin",
            PasswordHash = hasher.Hash("admin123"),
            DisplayName = "Admin User",
            Role = UserRole.Admin
        };

        var user1 = new User
        {
            Username = "user1",
            PasswordHash = hasher.Hash("user123"),
            DisplayName = "Alice Wang",
            Role = UserRole.User
        };

        var user2 = new User
        {
            Username = "user2",
            PasswordHash = hasher.Hash("user123"),
            DisplayName = "Bob Chen",
            Role = UserRole.User
        };

        context.Users.AddRange(admin, user1, user2);
        await context.SaveChangesAsync();

        var workItems = new[]
        {
            new WorkItem
            {
                Title = "Review Q1 Financial Report",
                Description = "Review and verify all Q1 financial statements before the deadline.",
                CreatedBy = admin.Id
            },
            new WorkItem
            {
                Title = "Complete Safety Training",
                Description = "Annual workplace safety training module — must be completed by all employees.",
                CreatedBy = admin.Id
            },
            new WorkItem
            {
                Title = "Update Employee Handbook",
                Description = "Review and update section 3.2 regarding remote work policy changes.",
                CreatedBy = admin.Id
            },
            new WorkItem
            {
                Title = "Submit Expense Report",
                Description = "March expenses submission including travel and equipment purchases.",
                CreatedBy = admin.Id
            },
            new WorkItem
            {
                Title = "Code Review: Auth Module",
                Description = "Review PR #142 for authentication refactor — check security implications.",
                CreatedBy = admin.Id
            },
            new WorkItem
            {
                Title = "Team Retrospective Prep",
                Description = "Prepare talking points for sprint retrospective meeting.",
                CreatedBy = admin.Id
            }
        };

        context.WorkItems.AddRange(workItems);
        await context.SaveChangesAsync();
    }
}
