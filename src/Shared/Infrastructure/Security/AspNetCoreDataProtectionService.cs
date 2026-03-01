using Microsoft.AspNetCore.DataProtection;

namespace C4.Shared.Infrastructure.Security;

public sealed class AspNetCoreDataProtectionService(IDataProtectionProvider provider) : IDataProtectionService
{
    private readonly IDataProtector _protector = provider.CreateProtector("C4.AzureTokens");

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText)
    {
        try
        {
            return _protector.Unprotect(protectedText);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return protectedText;
        }
    }
}
