namespace TokenShield.ProviderAdapters;

public interface IProviderAdapterFactory
{
    IProviderAdapter GetAdapter(string providerName);
}
