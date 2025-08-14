using System.Text.RegularExpressions;

namespace TicketSystem.Domain.ValueObjects;

public class PhoneNumber
{
    private static readonly Regex PhoneRegex = new(
        @"^[\+]?[0-9\s\-\(\)]{10,15}$",
        RegexOptions.Compiled);

    public string Value { get; private set; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty", nameof(value));

        var cleanValue = Regex.Replace(value, @"[^\d\+]", "");

        if (!PhoneRegex.IsMatch(value))
            throw new ArgumentException("Invalid phone number format", nameof(value));

        Value = value;
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;
    public static implicit operator PhoneNumber(string phone) => new(phone);

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is PhoneNumber other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}