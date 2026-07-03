namespace Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "BiblioBot";
    public string Audience { get; init; } = "BiblioBot";
    public string Secret { get; init; } = "BiblioBot_Development_Secret_Key_Change_This_For_Production_123456789";
    public int AccessTokenMinutes { get; init; } = 60;
    public int RefreshTokenDays { get; init; } = 7;
}
