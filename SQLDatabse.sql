CREATE TABLE Wallet
(
    AccountID INT PRIMARY KEY,
    AccountBalance INT NOT NULL,
    AccountHolderName VARCHAR(100) NOT NULL,
    AccountType VARCHAR(50) NOT NULL,
    DateOpened DATETIME NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Transacting
(
    TransactionID INT PRIMARY KEY,
    Amount INT NOT NULL,
    Direction VARCHAR(10) NOT NULL,
    AccountID INT NOT NULL,
    Timestamp DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Transacting_Wallet FOREIGN KEY (AccountID) REFERENCES Wallet(AccountID)
);
