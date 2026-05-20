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
        bool RegisterUser(string username, string password);

        [OperationContract]
        int LoginUser(string username, string password);

        [OperationContract]
        string[] GetUserBalances(int userId);

        [OperationContract]
        bool TopUpBalance(int userId, string currencyCode, decimal amount);

        [OperationContract]
        decimal ExchangeUserCurrency(int userId, string fromCurrency, string toCurrency, decimal amount);

        [OperationContract]
        bool SaveUserTransaction(int userId, string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount);

        [OperationContract]
        string[] GetRecentTransactionsByUser(int userId);

        [OperationContract]
        string[] GetHistoricalRates(string currencyCode, int days);

        [OperationContract]
        bool SaveTransaction(string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount);

        [OperationContract]
        string[] GetRecentTransactions();
    }
}