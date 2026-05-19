\# Currency Exchange Office System - Project Documentation



\## 1. Project Overview



This project is a network-based currency exchange office system developed for the Network Application Development course.



The system allows users to convert currencies using real exchange rates retrieved from the National Bank of Poland API. The application is divided into a WCF Web Service and a WPF client application.



\## 2. System Architecture



The project uses a client-server architecture:



WPF Client Application → WCF Web Service → NBP API



The WPF client does not communicate directly with the external API. It sends requests to the WCF service. The WCF service contains the business logic and retrieves exchange rates from the NBP API.



\## 3. WCF Web Service



The WCF service is implemented in the CurrencyExchangeService project.



Main service methods:



\- GetExchangeRate(string currencyCode)

\- ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)

\- GetSupportedCurrencies()



The service supports currencies such as PLN, USD, EUR, GBP, CHF, JPY, CZK, SEK, NOK, and DKK.



\## 4. NBP API Integration



The service retrieves real exchange rates from the National Bank of Poland API.



Example API request:



https://api.nbp.pl/api/exchangerates/rates/a/USD/?format=json



The returned JSON response is parsed in the WCF service and the exchange rate is used for currency conversion.



\## 5. Currency Conversion Logic



NBP exchange rates are based on PLN. Because of that, the conversion logic works through PLN.



Example:



USD → PLN → EUR



Formula:



amount \* fromCurrencyRate / toCurrencyRate



\## 6. WPF Client Application



The WPF client provides a graphical interface for the user.



Main features:



\- Select source currency

\- Select target currency

\- Enter amount

\- Convert currency

\- Display result

\- Show transaction history during application runtime



The WPF client communicates with the WCF service using BasicHttpBinding and ChannelFactory.



\## 7. Database Design



The database schema is included in the Database folder.



Tables:



\### Users



Stores user account data.



\### Balances



Stores user currency balances.



\### Transactions



Stores currency exchange transaction history.



The database script can be found here:



Database/schema.sql



\## 8. Technologies Used



\- C#

\- WCF

\- WPF

\- SQL Server LocalDB

\- ADO.NET / SQL script

\- NBP API

\- GitHub



\## 9. How to Run the Project



1\. Open the solution in Visual Studio.

2\. Build the solution.

3\. Start the CurrencyExchangeService project.

4\. Run the CurrencyExchangeClient project.

5\. Select currencies and enter an amount.

6\. Click Convert to get the result.



\## 10. Conclusion



The project demonstrates a network-based application using a WCF Web Service, a WPF client application, an external public API, and database schema design.

