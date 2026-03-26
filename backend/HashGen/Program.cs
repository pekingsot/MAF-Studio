using BCrypt.Net;

var password = "admin123";
var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
Console.WriteLine($"密码: {password}");
Console.WriteLine($"哈希: {hash}");
Console.WriteLine($"验证: {BCrypt.Net.BCrypt.Verify(password, hash)}");

password = "pekingsot123";
hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
Console.WriteLine($"密码: {password}");
Console.WriteLine($"哈希: {hash}");
Console.WriteLine($"验证: {BCrypt.Net.BCrypt.Verify(password, hash)}");
