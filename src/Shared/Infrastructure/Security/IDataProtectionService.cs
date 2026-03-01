namespace C4.Shared.Infrastructure.Security;

public interface IDataProtectionService
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}
