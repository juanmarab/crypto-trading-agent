namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for fetching financial news from the CryptoPanic API.
/// </summary>
public interface ICryptoPanicService
{
    Task FetchAndStoreNewsAsync(CancellationToken cancellationToken = default);
}
