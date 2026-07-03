using System;
using System.IO;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthenticationModule.Repositories.Entities.Migrations;

[DbContext(typeof(ZpantryDbContext))]
[Migration("20260703000100_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Database",
            "Migrations",
            "20260628000100_InitialSkeleton.sql");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Database migration script was not found.", scriptPath);
        }

        var sql = File.ReadAllText(scriptPath);
        migrationBuilder.Sql(sql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "meal_recommendation_items");
        migrationBuilder.DropTable(name: "pantry_usage_logs");
        migrationBuilder.DropTable(name: "recommendation_feedbacks");
        migrationBuilder.DropTable(name: "user_pantry_items");
        migrationBuilder.DropTable(name: "media_assets");
        migrationBuilder.DropTable(name: "cooking_logs");
        migrationBuilder.DropTable(name: "ingredient_aliases");
        migrationBuilder.DropTable(name: "meal_recommendations");
        migrationBuilder.DropTable(name: "today_menu_items");
        migrationBuilder.DropTable(name: "ingredients");
        migrationBuilder.DropTable(name: "recipes");
        migrationBuilder.DropTable(name: "users");
    }
}
