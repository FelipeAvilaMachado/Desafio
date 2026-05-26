using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace Desafio.Infrastructure.Azure;

/// <summary>
/// Reads secrets from Azure Key Vault using DefaultAzureCredential.
/// In development, falls back gracefully when no vault URI is configured.
/// </summary>
public sealed class AzureKeyVaultSecretProvider(
    SecretClient secretClient,
    ILogger<AzureKeyVaultSecretProvider> logger)
{
    public async Task<string?> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        try
        {
            var secret = await secretClient.GetSecretAsync(secretName, cancellationToken: ct);
            logger.LogInformation("Retrieved secret '{SecretName}' from Key Vault", secretName);
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve secret '{SecretName}' from Key Vault", secretName);
            return null;
        }
    }
}
