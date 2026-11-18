using Microsoft.Data.Sqlite;
using Snap.Data;

namespace Snap.Modules.Auth;

/// <summary>Único punto con SQL del módulo Auth. Consultas parametrizadas, sin ORM.</summary>
public sealed class UsersRepository(SqliteConnectionFactory connections)
{
    private const int SqliteConstraintUnique = 2067;

    /// <returns>El id del usuario creado, o <c>null</c> si el email ya existe.</returns>
    public async Task<long?> TryInsertAsync(
        string email, string passwordHash, string name, DateTimeOffset createdAt, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (email, password_hash, name, created_at)
            VALUES ($email, $passwordHash, $name, $createdAt)
            RETURNING id;
            """;
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$passwordHash", passwordHash);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$createdAt", SqliteTime.Format(createdAt));

        try
        {
            return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqliteConstraintUnique)
        {
            return null;
        }
    }

    public async Task<UserRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, email, password_hash, name, created_at FROM users WHERE email = $email;";
        command.Parameters.AddWithValue("$email", email);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UserRecord(
            Id: reader.GetInt64(0),
            Email: reader.GetString(1),
            PasswordHash: reader.GetString(2),
            Name: reader.GetString(3),
            CreatedAt: SqliteTime.Parse(reader.GetString(4)));
    }

    public async Task UpdatePasswordHashAsync(long userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE users SET password_hash = $passwordHash WHERE id = $id;";
        command.Parameters.AddWithValue("$passwordHash", passwordHash);
        command.Parameters.AddWithValue("$id", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
