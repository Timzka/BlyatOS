using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlyatOS.Library.Configs;

internal class PasswordPolicy
{
    public uint MinLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecial { get; set; } = true;

    public bool CheckPassword(string Password)
    {
        if (Password.Length < MinLength)
            return false;
        if (RequireUppercase && !Password.Any(char.IsUpper))
            return false;
        if (RequireLowercase && !Password.Any(char.IsLower))
            return false;
        if (RequireDigit && !Password.Any(char.IsDigit))
            return false;
        if (RequireSpecial && !Password.Any(ch => !char.IsLetterOrDigit(ch)))
            return false;
        return true;
    }

    public void SetPolicy()
    {
        Console.WriteLine("How many Characters are required?");
        if (uint.TryParse(Console.ReadLine(), out uint minLen))
            MinLength = minLen;
        Console.WriteLine("Require Uppercase Letters? (y/n)");
        RequireUppercase = Console.ReadLine().ToLower() == "y";
        Console.WriteLine("Require Lowercase Letters? (y/n)");
        RequireLowercase = Console.ReadLine().ToLower() == "y";
        Console.WriteLine("Require Digits? (y/n)");
        RequireDigit = Console.ReadLine().ToLower() == "y";
        Console.WriteLine("Require Special Characters? (y/n)");
        RequireSpecial = Console.ReadLine().ToLower() == "y";
        Console.Clear();
        Console.WriteLine("New Password Policy Set:");
        Console.WriteLine($"Minimum Length: {MinLength}");
        Console.WriteLine($"Require Uppercase: {RequireUppercase}");
        Console.WriteLine($"Require Lowercase: {RequireLowercase}");
        Console.WriteLine($"Require Digits: {RequireDigit}");
        Console.WriteLine($"Require Special Characters: {RequireSpecial}");
    }

    public void WritePolicy()
    {
        Console.WriteLine($"Minimum Length: {MinLength}");
        Console.WriteLine($"Require Uppercase: {RequireUppercase}");
        Console.WriteLine($"Require Lowercase: {RequireLowercase}");
        Console.WriteLine($"Require Digits: {RequireDigit}");
        Console.WriteLine($"Require Special Characters: {RequireSpecial}");
    }
}
