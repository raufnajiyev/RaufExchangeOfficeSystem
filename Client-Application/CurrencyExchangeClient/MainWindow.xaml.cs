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
        bool SaveUserTransaction(int userId, string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount);

        [OperationContract]
        string[] GetRecentTransactionsByUser(int userId);

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

            FromCurrencyComboBox.SelectedItem = "USD";
            ToCurrencyComboBox.SelectedItem = "EUR";
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

            string amountText = AmountTextBox.Text.Replace(",", ".");

            if (!decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
            {
                MessageBox.Show("Please enter a valid amount.");
                return;
            }

            if (amount <= 0)
            {
                MessageBox.Show("Amount must be greater than zero.");
                return;
            }

            IService1 client = null;
            IClientChannel channel = null;

            try
            {
                client = CreateServiceClient();
                channel = (IClientChannel)client;

                decimal result = client.ConvertCurrency(fromCurrency, toCurrency, amount);

                client.SaveUserTransaction(currentUserId, fromCurrency, toCurrency, amount, result);

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
    }
}