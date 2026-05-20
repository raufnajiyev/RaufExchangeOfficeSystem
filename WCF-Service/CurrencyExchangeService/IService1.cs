using System.ServiceModel;

namespace CurrencyExchangeService
{
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        decimal GetExchangeRate(string currencyCode);

        [OperationContract]
        decimal ConvertCurrency(string fromCurrency, string toCurrency, decimal amount);

        [OperationContract]
        string[] GetSupportedCurrencies();

        [OperationContract]
        bool EnsureDatabaseCreated();

        [OperationContract]
        bool SaveTransaction(string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount);

        [OperationContract]
        string[] GetRecentTransactions();
    }
}