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
    }

    public partial class MainWindow : Window
    {
        private const string ServiceUrl = "http://localhost:58696/Service1.svc";

        public MainWindow()
        {
            InitializeComponent();
            LoadCurrencies();
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

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
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

                string resultText = $"{amount} {fromCurrency} = {result} {toCurrency}";

                ResultTextBlock.Text = resultText;
                HistoryListBox.Items.Insert(0, $"{DateTime.Now:g} | {resultText}");

                channel.Close();
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