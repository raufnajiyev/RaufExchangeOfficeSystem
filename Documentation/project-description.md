\# Currency Exchange Office System - Project Documentation



\## 1. Project Overview



This project is a network-based Currency Exchange Office System developed for the \*\*Network Application Development\*\* course.



The system simulates an online currency exchange office. Users can log in, register a new account, convert currencies using real exchange rates, and view their transaction history. The application uses a distributed architecture based on a \*\*WCF Web Service\*\*, a \*\*WPF client application\*\*, and a \*\*SQL Server LocalDB database\*\*.



The main goal of the project is to demonstrate communication between a client application and a web service, integration with an external public API, and persistent data storage in a database.



\---



\## 2. System Architecture



The system is divided into three main parts:



```text

WPF Client Application → WCF Web Service → NBP API

&#x20;                                 ↓

&#x20;                          SQL Server LocalDB

```



\### WPF Client Application



The WPF client is the graphical user interface of the system. It allows the user to:



\- Log in using username and password

\- Register a new account

\- Select source and target currencies

\- Enter an amount

\- Convert currency

\- View transaction history

\- View account balances



The client does not communicate directly with the external API or database. Instead, it sends all requests to the WCF service.



\### WCF Web Service



The WCF service contains the main business logic of the project. It is responsible for:



\- Retrieving real exchange rates from the National Bank of Poland API

\- Performing currency conversion calculations

\- Creating and managing the database

\- Registering and logging in users

\- Saving user transactions

\- Returning transaction history and balances to the client



\### SQL Server LocalDB



The database is used to store application data persistently. It stores users, currency balances, and transaction history.



\---



\## 3. WCF Service Functionality



The WCF service provides the following main methods:



```text

GetExchangeRate(string currencyCode)

ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)

GetSupportedCurrencies()

EnsureDatabaseCreated()

RegisterUser(string username, string password)

LoginUser(string username, string password)

GetUserBalances(int userId)

SaveUserTransaction(int userId, string fromCurrency, string toCurrency, decimal amount, decimal convertedAmount)

GetRecentTransactionsByUser(int userId)

```



\### GetExchangeRate



This method receives a currency code such as `USD`, `EUR`, or `GBP` and retrieves the current exchange rate from the National Bank of Poland API.



\### ConvertCurrency



This method converts an amount from one currency to another. Since NBP exchange rates are based on PLN, conversion between two foreign currencies is calculated through PLN.



Formula:



```text

amount \* fromCurrencyRate / toCurrencyRate

```



Example:



```text

USD → PLN → EUR

```



\### RegisterUser and LoginUser



The system supports basic user account functionality. A user can register with a username and password. Passwords are hashed before being stored in the database.



The default demo account is:



```text

Username: demo

Password: demo

```



\### SaveUserTransaction



After a successful currency conversion, the transaction is saved in the database with the related user ID.



\### GetRecentTransactionsByUser



This method returns recent transactions only for the currently logged-in user.



\---



\## 4. NBP API Integration



The project uses the official National Bank of Poland API to retrieve real currency exchange rates.



Example API request:



```text

https://api.nbp.pl/api/exchangerates/rates/a/USD/?format=json

```



The WCF service sends a request to the API, receives a JSON response, parses it, and extracts the exchange rate value.



The WPF client does not call the NBP API directly. This is important because the system follows a proper client-server architecture:



```text

Client → Service → External API

```



\---



\## 5. Database Design



The database is created automatically by the WCF service if it does not already exist.



Database name:



```text

CurrencyExchangeOffice

```



The database schema is also included in the repository:



```text

Database/schema.sql

```



\### Users Table



Stores user account information.



Main fields:



```text

UserId

Username

PasswordHash

CreatedAt

```



\### Balances Table



Stores user currency balances.



Main fields:



```text

BalanceId

UserId

CurrencyCode

Amount

```



\### Transactions Table



Stores currency exchange transactions.



Main fields:



```text

TransactionId

UserId

FromCurrency

ToCurrency

Amount

ConvertedAmount

TransactionDate

```



\---



\## 6. WPF Client Application



The WPF client provides a desktop interface for interacting with the system.



Main interface features:



\- Username and password input

\- Login button

\- Register button

\- Currency selection fields

\- Amount input field

\- Convert button

\- Result display

\- Transaction history list

\- Account balance list



The WPF client communicates with the WCF service using:



```text

BasicHttpBinding

ChannelFactory

```



This means the client manually creates a WCF communication channel and calls service methods through the service contract.



\---



\## 7. Transaction Flow



The basic transaction process works as follows:



1\. User logs in or registers.

2\. User selects source currency.

3\. User selects target currency.

4\. User enters amount.

5\. Client sends conversion request to WCF service.

6\. WCF service retrieves rates from NBP API.

7\. WCF service calculates converted amount.

8\. WCF service saves transaction in SQL Server LocalDB.

9\. Client displays result and updates transaction history.



Flow diagram:



```text

User Input

&#x20;  ↓

WPF Client

&#x20;  ↓

WCF Service

&#x20;  ↓

NBP API

&#x20;  ↓

Conversion Result

&#x20;  ↓

SQL Server LocalDB

&#x20;  ↓

Updated Transaction History

```



\---



\## 8. Technologies Used



The project uses the following technologies:



\- C#

\- WCF

\- WPF

\- SQL Server LocalDB

\- ADO.NET

\- NBP API

\- Visual Studio

\- GitHub



\---



\## 9. Repository Structure



```text

RaufExchangeOfficeSystem

│

├── WCF-Service

│   └── CurrencyExchangeService

│

├── Client-Application

│   └── CurrencyExchangeClient

│

├── Database

│   └── schema.sql

│

├── Documentation

│   ├── project-description.md

│   └── Screenshots

│

└── README.md

```



\---



\## 10. How to Run the Project



1\. Open the solution in Visual Studio.

2\. Build the solution.

3\. Start the `CurrencyExchangeService` project.

4\. Start the `CurrencyExchangeClient` project.

5\. Log in using the demo account or register a new user.

6\. Select source and target currencies.

7\. Enter an amount.

8\. Click `Convert`.

9\. The result will be displayed in the WPF client.

10\. The transaction will be saved in the SQL Server LocalDB database.



Demo login:



```text

Username: demo

Password: demo

```



\---



\## 11. Implemented Requirements



The project implements the main course requirements:



\- WCF Web Service

\- NBP API integration

\- Currency exchange functionality

\- WPF client application

\- User registration and login

\- Transaction history

\- SQL Server database integration

\- Database schema and scripts

\- Public GitHub repository

\- Multiple commits

\- README and documentation



\---



\## 12. Conclusion



This project demonstrates a complete network-based application using a WCF Web Service and WPF client application. The system retrieves real exchange rates from an external API, performs currency conversion, stores user transactions in a SQL Server database, and provides a usable desktop interface.



The application follows a client-server architecture and separates user interface, business logic, external API communication, and database operations into clear components.

