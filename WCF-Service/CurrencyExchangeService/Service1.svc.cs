using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Web.Script.Serialization;

namespace CurrencyExchangeService
{
    public class Service1 : IService1
    {
        private const string MasterConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";

        private const string DatabaseConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CurrencyExchangeOffice;Integrated Security=True;";

        private static readonly string[] SupportedCurrencies =
        {
            "PLN", "USD", "EUR", "GBP", "CHF", "JPY", "CZK", "SEK", "NOK", "DKK"
        };

        public string[] GetSupportedCurrencies()
        {
            return SupportedCurrencies;
        }

        public decimal GetExchangeRate(string currencyCode)
        {
            string code = NormalizeCurrencyCode(currencyCode);

            if (code == "PLN")
            {
                return 1m;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string url = "https://api.nbp.pl/api/exchangerates/rates/a/" + code + "/?format=json";

                using (WebClient client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;

                    string json = client.DownloadString(url);

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    NbpResponse response = serializer.Deserialize<NbpResponse>(json);

                    if (response == null || response.rates == null || response.rates.Count == 0)
                    {
                        throw new FaultException("Exchange rate data was not found.");
                    }

                    return response.rates[0].mid;
                }
            }
            catch (WebException)
            {
                throw new FaultException("Currency code was not found in the NBP API.");
            }
            catch (Exception ex)
            {
                throw new FaultException("Error while retrieving exchange rate: " + ex.Message);
            }
        }

        public decimal ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)
        {
            if (amount <= 0)
            {
                throw new FaultException("Amount must be greater than zero.");
            }

            decimal fromRate = GetExchangeRate(fromCurrency);
            decimal toRate = GetExchangeRate(toCurrency);

            decimal result = amount * fromRate / toRate;

            return Math.Round(result, 2);
        }

        public bool EnsureDatabaseCreated()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(MasterConnectionString))
                {
                    connection.Open();

                    string createDatabaseSql =
                        "IF DB_ID('CurrencyExchangeOffice') IS NULL CREATE DATABASE CurrencyExchangeOffice;";

                    using (SqlCommand command = new SqlCommand(createDatabaseSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
                {
                    connection.Open();

                    string createUsersTableSql =
                        @"IF OBJECT_ID('Users', 'U') IS NULL
                          CREATE TABLE Users (
                              UserId INT IDENTITY(1,1) PRIMARY KEY,
                              Username NVARCHAR(50) NOT NULL UNIQUE,
                              PasswordHash NVARCHAR(255) NOT NULL,
                              CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                          );";

                    string createBalancesTableSql =
                        @"IF OBJECT_ID('Balances', 'U') IS NULL
                          CREATE TABLE Balances (
                              BalanceId INT IDENTITY(1,1) PRIMARY KEY,
                              UserId INT NOT NULL,
                              CurrencyCode NVARCHAR(3) NOT NULL,
                              Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                              FOREIGN KEY (UserId) REFERENCES Users(UserId)
                          );";

                    string createTransactionsTableSql =
                        @"IF OBJECT_ID('Transactions', 'U') IS NULL
                          CREATE TABLE Transactions (
                              TransactionId INT IDENTITY(1,1) PRIMARY KEY,
                              UserId INT NULL,
                              FromCurrency NVARCHAR(3) NOT NULL,
                              ToCurrency NVARCHAR(3) NOT NULL,
                              Amount DECIMAL(18,2) NOT NULL,
                              ConvertedAmount DECIMAL(18,2) NOT NULL,
                              TransactionDate DATETIME NOT NULL DEFAULT GETDATE()
                          );";

                    ExecuteNonQuery(connection, createUsersTableSql);
                    ExecuteNonQuery(connection, createBalancesTableSql);
                    ExecuteNonQuery(connection, createTransactionsTableSql);

                    string insertDemoUserSql =
                        @"IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'demo')
                          INSERT INTO Users (Username, PasswordHash)
                          VALUES ('demo', 'demo');";

                    ExecuteNonQuery(connection, insertDemoUserSql);

                    string insertDemoBalancesSql =
                        @"IF NOT EXISTS (SELECT 1 FROM Balances WHERE UserId = 1)
                          BEGIN
                              INSERT INTO Balances (UserId, CurrencyCode, Amount) VALUES (1, 'PLN', 1000.00);
                              INSERT INTO Balances (UserId, CurrencyCode, Amount) VALUES (1, 'USD', 500.00);
                              INSERT INTO Balances (UserId, CurrencyCode, Amount) VALUES (1, 'EUR', 300.00);
                          END";

                    ExecuteNonQuery(connection, insertDemoBalancesSql);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new FaultException("Database initialization error: " + ex.Message);
            }
        }

        public bool SaveTransaction(string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount)
        {
            try
            {
                EnsureDatabaseCreated();

                using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
                {
                    connection.Open();

                    string sql =
                        @"INSERT INTO Transactions (UserId, FromCurrency, ToCurrency, Amount, ConvertedAmount)
                          VALUES (1, @FromCurrency, @ToCurrency, @Amount, @ConvertedAmount);";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromCurrency", NormalizeCurrencyCode(fromCurrency));
                        command.Parameters.AddWithValue("@ToCurrency", NormalizeCurrencyCode(toCurrency));
                        command.Parameters.AddWithValue("@Amount", amount);
                        command.Parameters.AddWithValue("@ConvertedAmount", convertedAmount);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new FaultException("Transaction save error: " + ex.Message);
            }
        }

        public string[] GetRecentTransactions()
        {
            try
            {
                EnsureDatabaseCreated();

                List<string> transactions = new List<string>();

                using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
                {
                    connection.Open();

                    string sql =
                        @"SELECT TOP 20 FromCurrency, ToCurrency, Amount, ConvertedAmount, TransactionDate
                          FROM Transactions
                          ORDER BY TransactionDate DESC;";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string item =
                                Convert.ToDateTime(reader["TransactionDate"]).ToString("g") +
                                " | " +
                                reader["Amount"] + " " +
                                reader["FromCurrency"] +
                                " = " +
                                reader["ConvertedAmount"] + " " +
                                reader["ToCurrency"];

                            transactions.Add(item);
                        }
                    }
                }

                return transactions.ToArray();
            }
            catch (Exception ex)
            {
                throw new FaultException("Transaction history error: " + ex.Message);
            }
        }

        private void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private string NormalizeCurrencyCode(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                throw new FaultException("Currency code cannot be empty.");
            }

            string code = currencyCode.Trim().ToUpper();

            if (code.Length != 3)
            {
                throw new FaultException("Currency code must have exactly 3 letters.");
            }

            return code;
        }
    }

    public class NbpResponse
    {
        public string table { get; set; }
        public string currency { get; set; }
        public string code { get; set; }
        public List<NbpRate> rates { get; set; }
    }

    public class NbpRate
    {
        public string no { get; set; }
        public string effectiveDate { get; set; }
        public decimal mid { get; set; }
    }
}