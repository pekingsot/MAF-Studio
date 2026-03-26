namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("users")]
public class User : BaseEntityWithUpdate
{
    [Dapper.Contrib.Extensions.Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "user";

    public string? Avatar { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
