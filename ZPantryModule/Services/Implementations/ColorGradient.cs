using System.Security.Cryptography;
using System.Text;

namespace ZPantryModule.Services.Implementations;

internal static class ColorGradient
{
    private static readonly string[] Palette =
    [
        "#ff7a1a",
        "#f97316",
        "#fb923c",
        "#f59e0b",
        "#22c55e",
        "#16a34a",
        "#0ea5e9",
        "#2563eb",
        "#a855f7",
        "#ec4899"
    ];

    public static (string From, string To) Generate(params string?[] parts)
    {
        var seed = string.Join("|", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim().ToLowerInvariant()));
        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = "zpantry";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var first = Palette[hash[0] % Palette.Length];
        var second = Palette[hash[1] % Palette.Length];

        if (first == second)
        {
            second = Palette[(hash[2] + 3) % Palette.Length];
        }

        return (first, second);
    }
}
