using BCrypt.Net;

Console.WriteLine("生成密码哈希...");

var passwords = new[] { "admin123", "pekingsot123" };

foreach (var password in passwords)
{
    var hash = BCrypt.HashPassword(password, 12);
    Console.WriteLine($"密码: {password}");
    Console.WriteLine($"哈希: {hash}");
    Console.WriteLine();
}
