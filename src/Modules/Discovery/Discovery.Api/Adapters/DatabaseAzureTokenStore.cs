using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace C4.Modules.Discovery.Api.Adapters;

public sealed class DatabaseAzureTokenStore(IConfiguration configuration, IDataProtectionService dataProtection) : IAzureTokenStore
{
    private readonly string _connectionString = configuration.GetConnectionString("Discovery")
        ?? throw new InvalidOperationException("Connection string 'Discovery' is not configured.");

    public async Task StoreAsync(string externalSubscriptionId, AzureTokenInfo tokenInfo, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        string encryptedAccessToken = dataProtection.Protect(tokenInfo.AccessToken);
        string? encryptedRefreshToken = tokenInfo.RefreshToken is not null
            ? dataProtection.Protect(tokenInfo.RefreshToken)
            : null;

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO azure_tokens (external_subscription_id, access_token, refresh_token, expires_at_utc)
            VALUES (@externalSubscriptionId, @accessToken, @refreshToken, @expiresAtUtc)
            ON CONFLICT (external_subscription_id) DO UPDATE
                SET access_token   = EXCLUDED.access_token,
                    refresh_token  = EXCLUDED.refresh_token,
                    expires_at_utc = EXCLUDED.expires_at_utc
            """,
            connection);

        command.Parameters.AddWithValue("externalSubscriptionId", externalSubscriptionId);
        command.Parameters.AddWithValue("accessToken", encryptedAccessToken);
        command.Parameters.AddWithValue("refreshToken", (object?)encryptedRefreshToken ?? DBNull.Value);
        command.Parameters.AddWithValue("expiresAtUtc", DateTime.SpecifyKind(tokenInfo.ExpiresAtUtc, DateTimeKind.Utc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AzureTokenInfo?> GetAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT access_token, refresh_token, expires_at_utc
            FROM azure_tokens
            WHERE external_subscription_id = @externalSubscriptionId
            """,
            connection);

        command.Parameters.AddWithValue("externalSubscriptionId", externalSubscriptionId);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        string accessToken = dataProtection.Unprotect(reader.GetString(0));
        string? refreshToken = reader.IsDBNull(1) ? null : dataProtection.Unprotect(reader.GetString(1));
        DateTime expiresAtUtc = DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc);

        return new AzureTokenInfo(accessToken, refreshToken, expiresAtUtc);
    }
}
