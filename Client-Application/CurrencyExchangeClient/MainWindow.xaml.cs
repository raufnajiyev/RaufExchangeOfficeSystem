using System;
using System.Globalization;
using System.ServiceModel;
using System.Windows;

namespace CurrencyExchangeClient
{
    [ServiceContract(Name = "IService1", Namespace = "http://tempuri.org/")]
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

    public partial class MainWindow : Window
    {
        private const string ServiceUrl = "http://localhost:58696/Service1.svc";

        private int currentUserId = -1;
        private string currentUsername = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            LoadCurrencies();
            InitializeDatabase();
            LoginDefaultUser();
        }

        private void LoadCurrencies()
        {
            string[] currencies =
            {
                "PLN", "USD", "EUR", "GBP", "CHF", "JPY", "CZK", "SEK", "NOK", "DKK"
            };

            FromCurrencyComboBox.ItemsSource = currencies;
            ToCurrencyComboBox.ItemsSource = currencies;
            TopUpCurrencyComboBox.ItemsSource = currencies;

            FromCurrencyComboBox.SelectedItem = "USD";
            ToCurrencyComboBox.SelectedItem = "EUR";
            TopUpCurrencyComboBox.SelectedItem = "PLN";
        }

        private IService1 CreateServiceClient()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            EndpointAddress endpoint = new EndpointAddress(ServiceUrl);

            return ChannelFactory<IService1>.CreateChannel(binding, endpoint);
        }

        private void InitializeDatabase()
        {
            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                client.EnsureDatabaseCreated();

                channel.Close();
            }
            catch
            {
                if (channel != null)
                {
                    channel.Abort();
                }
            }
        }

        private void LoginDefaultUser()
        {
            UsernameTextBox.Text = "demo";
            PasswordInputBox.Password = "demo";
            LoginUser("demo", "demo", false);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginUser(UsernameTextBox.Text, PasswordInputBox.Password, true);
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordInputBox.Password;

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                bool registered = client.RegisterUser(username, password);

                channel.Close();

                if (registered)
                {
                    MessageBox.Show("User registered successfully.");
                    LoginUser(username, password, true);
                }
            }
            catch (Exception ex)
            {
                if (channel != null)
                {
                    channel.Abort();
                }

                MessageBox.Show("Registration error: " + ex.Message);
            }
        }

        private void LoginUser(string username, string password, bool showMessage)
        {
            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                int userId = client.LoginUser(username, password);

                channel.Close();

                if (userId > 0)
                {
                    currentUserId = userId;
                    currentUsername = username;

                    AccountStatusTextBlock.Text = "Logged in as: " + currentUsername;

                    LoadTransactionHistory();
                    LoadBalances();

                    if (showMessage)
                    {
                        MessageBox.Show("Login successful.");
                    }
                }
                else
                {
                    currentUserId = -1;
                    currentUsername = string.Empty;
                    AccountStatusTextBlock.Text = "Invalid username or password.";

                    HistoryListBox.Items.Clear();
                    BalancesListBox.Items.Clear();
                    RatesListBox.Items.Clear();

                    if (showMessage)
                    {
                        MessageBox.Show("Invalid username or password.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (channel != null)
                {
                    channel.Abort();
                }

                MessageBox.Show("Login error: " + ex.Message);
            }
        }

        private void LoadTransactionHistory()
        {
            if (currentUserId <= 0)
            {
                return;
            }

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                string[] transactions = client.GetRecentTransactionsByUser(currentUserId);

                HistoryListBox.Items.Clear();

                foreach (string transaction in transactions)
                {
                    HistoryListBox.Items.Add(transaction);
                }

                channel.Close();
            }
            catch
            {
                if (channel != null)
                {
                    channel.Abort();
                }
            }
        }

        private void LoadBalances()
        {
            if (currentUserId <= 0)
            {
                return;
            }

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                string[] balances = client.GetUserBalances(currentUserId);

                BalancesListBox.Items.Clear();

                foreach (string balance in balances)
                {
                    BalancesListBox.Items.Add(balance);
                }

                channel.Close();
            }
            catch
            {
                if (channel != null)
                {
                    channel.Abort();
                }
            }
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId <= 0)
            {
                MessageBox.Show("Please login first.");
                return;
            }

            string fromCurrency = FromCurrencyComboBox.SelectedItem?.ToString();
            string toCurrency = ToCurrencyComboBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            {
                MessageBox.Show("Please select both currencies.");
                return;
            }

            if (!TryReadAmount(AmountTextBox.Text, out decimal amount))
            {
                MessageBox.Show("Please enter a valid amount.");
                return;
            }

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                decimal result = client.ExchangeUserCurrency(currentUserId, fromCurrency, toCurrency, amount);

                string resultText = amount + " " + fromCurrency + " = " + result + " " + toCurrency;
                ResultTextBlock.Text = resultText;

                channel.Close();

                LoadTransactionHistory();
                LoadBalances();
            }
            catch (Exception ex)
            {
                if (channel != null)
                {
                    channel.Abort();
                }

                MessageBox.Show("Service error: " + ex.Message);
            }
        }

        private void TopUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId <= 0)
            {
                MessageBox.Show("Please login first.");
                return;
            }

            string currencyCode = TopUpCurrencyComboBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                MessageBox.Show("Please select a currency.");
                return;
            }

            if (!TryReadAmount(TopUpAmountTextBox.Text, out decimal amount))
            {
                MessageBox.Show("Please enter a valid top-up amount.");
                return;
            }

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                client.TopUpBalance(currentUserId, currencyCode, amount);

                channel.Close();

                ResultTextBlock.Text = "Top-up completed: " + amount + " " + currencyCode;

                LoadBalances();
            }
            catch (Exception ex)
            {
                if (channel != null)
                {
                    channel.Abort();
                }

                MessageBox.Show("Top-up error: " + ex.Message);
            }
        }

        private void CheckRateButton_Click(object sender, RoutedEventArgs e)
        {
            string currencyCode = FromCurrencyComboBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                MessageBox.Show("Please select a currency.");
                return;
            }

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                decimal rate = client.GetExchangeRate(currencyCode);

                RatesListBox.Items.Insert(0, "Current rate: " + currencyCode + " = " + rate + " PLN");

                channel.Close();
            }
            catch (Exception ex)
            {
                if (channel != null)
                {
                    channel.Abort();
                }

                MessageBox.Show("Rate error: " + ex.Message);
            }
        }

        private void HistoricalRatesButton_Click(object sender, RoutedEventArgs e)
        {
            string currencyCode = FromCurrencyComboBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                MessageBox.Show("Please select a currency.");
                return;
            }

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                string[] rates = client.GetHistoricalRates(currencyCode, 7);

                RatesListBox.Items.Clear();

                foreach (string rate in rates)
                {
                    RatesListBox.Items.Add(rate);
                }

                channel.Close();
            }
            catch (Exception ex)
            {
                if (channel != null)
                {
                    channel.Abort();
                }

                MessageBox.Show("Historical rates error: " + ex.Message);
            }
        }

        private bool TryReadAmount(string input, out decimal amount)
        {
            string amountText = input.Replace(",", ".");

            if (!decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
            {
                return false;
            }

            if (amount <= 0)
            {
                return false;
            }

            return true;
        }
    }
}