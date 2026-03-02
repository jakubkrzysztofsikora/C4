namespace C4.Shared.Kernel.Security;

public interface IDataProtectionService
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}
