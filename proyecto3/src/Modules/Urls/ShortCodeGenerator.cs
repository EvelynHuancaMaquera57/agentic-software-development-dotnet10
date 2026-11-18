using System.Security.Cryptography;

namespace Snap.Modules.Urls;

public interface IShortCodeGenerator
{
    string Generate();
}

/// <summary>Códigos base62 de 7 caracteres (~3.5 billones de combinaciones), con RNG criptográfico.</summary>
public sealed class RandomShortCodeGenerator : IShortCodeGenerator
{
    public const int Length = 7;
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
}
