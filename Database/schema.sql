CREATE DATABASE CurrencyExchangeOffice;
GO

USE CurrencyExchangeOffice;
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Balances (
    BalanceId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    CurrencyCode NVARCHAR(3) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

CREATE TABLE Transactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    FromCurrency NVARCHAR(3) NOT NULL,
    ToCurrency NVARCHAR(3) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ConvertedAmount DECIMAL(18,2) NOT NULL,
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

INSERT INTO Users (Username, PasswordHash)
VALUES ('demo', 'demo');

INSERT INTO Balances (UserId, CurrencyCode, Amount)
VALUES
(1, 'PLN', 1000.00),
(1, 'USD', 500.00),
(1, 'EUR', 300.00);
GO