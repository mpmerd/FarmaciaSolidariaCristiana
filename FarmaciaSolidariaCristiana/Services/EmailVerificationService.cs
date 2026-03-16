using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace FarmaciaSolidariaCristiana.Services;

/// <summary>
/// Servicio para gestionar códigos de verificación de email durante el registro.
/// Usa almacenamiento en memoria con expiración automática.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Genera y almacena un código de verificación de 6 dígitos para un email.
    /// </summary>
    string GenerateCode(string email);

    /// <summary>
    /// Valida un código de verificación para un email.
    /// Si es válido, lo consume (solo se puede usar una vez).
    /// </summary>
    bool ValidateCode(string email, string code);

    /// <summary>
    /// Verifica si un email tiene un código pendiente (para rate limiting).
    /// </summary>
    bool HasPendingCode(string email);
}

public class EmailVerificationService : IEmailVerificationService
{
    private readonly ConcurrentDictionary<string, VerificationEntry> _codes = new();
    private static readonly TimeSpan CodeExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromSeconds(60);

    public string GenerateCode(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Rate limiting: no permitir reenvío antes de 60 segundos
        if (_codes.TryGetValue(normalizedEmail, out var existing) &&
            DateTime.UtcNow - existing.CreatedAt < CooldownPeriod)
        {
            return existing.Code; // Devolver el código existente
        }

        // Generar código de 6 dígitos criptográficamente seguro
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var entry = new VerificationEntry
        {
            Code = code,
            CreatedAt = DateTime.UtcNow
        };

        _codes.AddOrUpdate(normalizedEmail, entry, (_, _) => entry);

        // Limpiar códigos expirados periódicamente
        CleanupExpired();

        return code;
    }

    public bool ValidateCode(string email, string code)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!_codes.TryGetValue(normalizedEmail, out var entry))
            return false;

        // Verificar expiración
        if (DateTime.UtcNow - entry.CreatedAt > CodeExpiration)
        {
            _codes.TryRemove(normalizedEmail, out _);
            return false;
        }

        // Verificar código
        if (entry.Code != code.Trim())
            return false;

        // Consumir el código (uso único)
        _codes.TryRemove(normalizedEmail, out _);
        return true;
    }

    public bool HasPendingCode(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (_codes.TryGetValue(normalizedEmail, out var entry))
        {
            if (DateTime.UtcNow - entry.CreatedAt < CooldownPeriod)
                return true;
        }
        return false;
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _codes)
        {
            if (now - kvp.Value.CreatedAt > CodeExpiration)
                _codes.TryRemove(kvp.Key, out _);
        }
    }

    private class VerificationEntry
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
