using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlyatOS.Library.Helpers;

namespace BlyatOS.Library.Configs;

public class PasswordPolicy
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
        ConsoleHelpers.WriteLine("How many Characters are required?");
        if (uint.TryParse(ConsoleHelpers.ReadLine(), out uint minLen))
            MinLength = minLen;
        ConsoleHelpers.WriteLine("Require Uppercase Letters? (y/n)");
        RequireUppercase = ConsoleHelpers.ReadLine().ToLower() == "y";
        ConsoleHelpers.WriteLine("Require Lowercase Letters? (y/n)");
        RequireLowercase = ConsoleHelpers.ReadLine().ToLower() == "y";
        ConsoleHelpers.WriteLine("Require Digits? (y/n)");
        RequireDigit = ConsoleHelpers.ReadLine().ToLower() == "y";
        ConsoleHelpers.WriteLine("Require Special Characters? (y/n)");
        RequireSpecial = ConsoleHelpers.ReadLine().ToLower() == "y";
        ConsoleHelpers.ClearConsole();
        ConsoleHelpers.WriteLine("New Password Policy Set:");
        ConsoleHelpers.WriteLine($"Minimum Length: {MinLength}");
        ConsoleHelpers.WriteLine($"Require Uppercase: {RequireUppercase}");
        ConsoleHelpers.WriteLine($"Require Lowercase: {RequireLowercase}");
        ConsoleHelpers.WriteLine($"Require Digits: {RequireDigit}");
        ConsoleHelpers.WriteLine($"Require Special Characters: {RequireSpecial}");
    }

    public void WritePolicy()
    {
        ConsoleHelpers.WriteLine($"Minimum Length: {MinLength}");
        ConsoleHelpers.WriteLine($"Require Uppercase: {RequireUppercase}");
        ConsoleHelpers.WriteLine($"Require Lowercase: {RequireLowercase}");
        ConsoleHelpers.WriteLine($"Require Digits: {RequireDigit}");
        ConsoleHelpers.WriteLine($"Require Special Characters: {RequireSpecial}");
    }
}
