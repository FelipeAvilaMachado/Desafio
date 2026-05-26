using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;

namespace Desafio.Infrastructure.Aws;

/// <summary>
/// Reads secrets from AWS Secrets Manager.
/// </summary>
public sealed class SecretsManagerProvider(
    IAmazonSecretsManager client,
    ILogger<SecretsManagerProvider> logger)
{
    public async Task<string?> GetSecretAsync(string secretId, CancellationToken ct = default)
    {
        try
        {
            var request = new GetSecretValueRequest { SecretId = secretId };
            var response = await client.GetSecretValueAsync(request, ct);
            logger.LogInformation("Retrieved secret '{SecretId}' from Secrets Manager", secretId);
            return response.SecretString;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve secret '{SecretId}' from Secrets Manager", secretId);
            return null;
        }
    }
}
