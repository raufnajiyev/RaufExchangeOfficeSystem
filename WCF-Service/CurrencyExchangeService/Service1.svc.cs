using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Web.Script.Serialization;

namespace CurrencyExchangeService
{
    public class Service1 : IService1
    {
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