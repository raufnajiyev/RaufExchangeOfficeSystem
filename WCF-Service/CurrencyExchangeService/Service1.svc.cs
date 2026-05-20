using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Security.Cryptography;
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

                    ExecuteNonQuery(connection, createDatabaseSql);
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

                    if (GetUserIdByUsername(connection, "demo") == -1)
                    {
                        string insertDemoUserSql =
                            @"INSERT INTO Users (Username, PasswordHash)
                              VALUES ('demo', @PasswordHash);";

                        using (SqlCommand command = new SqlCommand(insertDemoUserSql, connection))
                        {
                            command.Parameters.AddWithValue("@PasswordHash", HashPassword("demo"));
                            command.ExecuteNonQuery();
                        }
                    }

                    int demoUserId = GetUserIdByUsername(connection, "demo");
                    CreateDefaultBalances(connection, demoUserId);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new FaultException("Database initialization error: " + ex.Message);
            }
        }

        public bool RegisterUser(string username, string password)
        {
            ValidateUserInput(username, password);
            EnsureDatabaseCreated();

            try
            {
                using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
                {
                    connection.Open();

                    if (GetUserIdByUsername(connection, username) != -1)
                    {
                        throw new FaultException("Username already exists.");
                    }

                    string sql =
                        @"INSERT INTO Users (Username, PasswordHash)
                          VALUES (@Username, @PasswordHash);";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username.Trim());
                        command.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                        command.ExecuteNonQuery();
                    }

                    int userId = GetUserIdByUsername(connection, username);
                    CreateDefaultBalances(connection, userId);
                }

                return true;
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FaultException("Registration error: " + ex.Message);
            }
        }

        public int LoginUser(string username, string password)
        {
            ValidateUserInput(username, password);
            EnsureDatabaseCreated();

            try
            {
                using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
                {
                    connection.Open();

                    string sql =
                        @"SELECT UserId, PasswordHash
                          FROM Users
                          WHERE Username = @Username;";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username.Trim());

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["UserId"]);
                                string storedPassword = reader["PasswordHash"].ToString();
                                string hashedInput = HashPassword(password);

                                if (storedPassword == hashedInput || storedPassword == password)
                                {
                                    return userId;
                                }
                            }
                        }
                    }
                }

                return -1;
            }
            catch (Exception ex)
            {
                throw new FaultException("Login error: " + ex.Message);
            }
        }

        public string[] GetUserBalances(int userId)
        {
            EnsureDatabaseCreated();

            List<string> balances = new List<string>();

            using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
            {
                connection.Open();

                string sql =
                    @"SELECT CurrencyCode, Amount
                      FROM Balances
                      WHERE UserId = @UserId
                      ORDER BY CurrencyCode;";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            balances.Add(reader["CurrencyCode"] + ": " + reader["Amount"]);
                        }
                    }
                }
            }

            return balances.ToArray();
        }

        public bool SaveUserTransaction(int userId, string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount)
        {
            try
            {
                EnsureDatabaseCreated();

                using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
                {
                    connection.Open();

                    string sql =
                        @"INSERT INTO Transactions (UserId, FromCurrency, ToCurrency, Amount, ConvertedAmount)
                          VALUES (@UserId, @FromCurrency, @ToCurrency, @Amount, @ConvertedAmount);";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
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

        public string[] GetRecentTransactionsByUser(int userId)
        {
            EnsureDatabaseCreated();

            List<string> transactions = new List<string>();

            using (SqlConnection connection = new SqlConnection(DatabaseConnectionString))
            {
                connection.Open();

                string sql =
                    @"SELECT TOP 20 FromCurrency, ToCurrency, Amount, ConvertedAmount, TransactionDate
                      FROM Transactions
                      WHERE UserId = @UserId
                      ORDER BY TransactionDate DESC;";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

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
            }

            return transactions.ToArray();
        }

        public bool SaveTransaction(string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount)
        {
            return SaveUserTransaction(1, fromCurrency, toCurrency, amount, convertedAmount);
        }

        public string[] GetRecentTransactions()
        {
            return GetRecentTransactionsByUser(1);
        }

        private void CreateDefaultBalances(SqlConnection connection, int userId)
        {
            if (userId <= 0)
            {
                return;
            }

            string checkSql = "SELECT COUNT(*) FROM Balances WHERE UserId = @UserId;";

            using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@UserId", userId);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (count > 0)
                {
                    return;
                }
            }

            string insertSql =
                @"INSERT INTO Balances (UserId, CurrencyCode, Amount) VALUES (@UserId, 'PLN', 1000.00);
                  INSERT INTO Balances (UserId, CurrencyCode, Amount) VALUES (@UserId, 'USD', 500.00);
                  INSERT INTO Balances (UserId, CurrencyCode, Amount) VALUES (@UserId, 'EUR', 300.00);";

            using (SqlCommand command = new SqlCommand(insertSql, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.ExecuteNonQuery();
            }
        }

        private int GetUserIdByUsername(SqlConnection connection, string username)
        {
            string sql = "SELECT UserId FROM Users WHERE Username = @Username;";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Username", username.Trim());

                object result = command.ExecuteScalar();

                if (result == null)
                {
                    return -1;
                }

                return Convert.ToInt32(result);
            }
        }

        private void ValidateUserInput(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new FaultException("Username cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new FaultException("Password cannot be empty.");
            }
        }

        private void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();

                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
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