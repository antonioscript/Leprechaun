------------------------------------------------------------
-- 1. Person (Titular)
------------------------------------------------------------
CREATE TABLE Person (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL, -- 'Antonio', 'Catarina'
    IsActive    BIT NOT NULL DEFAULT (1)
);
GO

------------------------------------------------------------
-- 2. Institution (income source: salary, donation, financing, etc.)
------------------------------------------------------------
CREATE TABLE Institution (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    Type            VARCHAR(50) NOT NULL, 
    -- 'CLT', 'Bank', 'Broker', 'PJ', 'Donation', 'Financing', etc.
    PersonId        INT NOT NULL,
    Description     NVARCHAR(500) NULL,

    StartDate       DATE NULL,
    EndDate         DATE NULL,
    IsActive        BIT NOT NULL DEFAULT (1),

    CONSTRAINT FK_Institution_Person
        FOREIGN KEY (PersonId) REFERENCES Person(Id),

    CONSTRAINT CK_Institution_Type
        CHECK (Type IN ('CLT', 'Bank', 'Broker', 'PJ', 'Donation', 'Financing')),

    CONSTRAINT CK_Institution_Start_End
        CHECK (
            EndDate IS NULL 
            OR StartDate IS NULL 
            OR EndDate >= StartDate
        )
);
GO

------------------------------------------------------------
-- 3. CostCenter (the “boxes” / envelopes)
------------------------------------------------------------
CREATE TABLE CostCenter (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(200) NOT NULL, -- 'Home Infra', 'Monastery Purchases', etc.
    PersonId    INT NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive    BIT NOT NULL DEFAULT (1),

    CONSTRAINT FK_CostCenter_Person
        FOREIGN KEY (PersonId) REFERENCES Person(Id)
);
GO

------------------------------------------------------------
-- 4. Category (optional, for reports)
------------------------------------------------------------
CREATE TABLE Category (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,   -- 'Groceries', 'Tithe', 'Rent', etc.
    Description NVARCHAR(300) NULL,
    IsActive    BIT NOT NULL DEFAULT (1)
);
GO

------------------------------------------------------------
-- 5. FinanceTransaction (ledger of everything)
------------------------------------------------------------
CREATE TABLE FinanceTransaction (
    Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
    TransactionDate     DATETIME2(0) NOT NULL,
    Amount              DECIMAL(18,2) NOT NULL,
    
    -- Movement between cost centers (boxes)
    SourceCostCenterId  INT NULL,
    TargetCostCenterId  INT NULL,

    -- Income source (required for Income)
    InstitutionId       INT NULL,

    -- 'Income', 'Transfer', 'Expense', 'Adjustment'
    TransactionType     VARCHAR(20) NOT NULL,
    
    PersonId            INT NOT NULL,
    CategoryId          INT NULL,
    Description         NVARCHAR(500) NULL,

    CONSTRAINT FK_FinanceTransaction_SourceCostCenter
        FOREIGN KEY (SourceCostCenterId) REFERENCES CostCenter(Id),

    CONSTRAINT FK_FinanceTransaction_TargetCostCenter
        FOREIGN KEY (TargetCostCenterId) REFERENCES CostCenter(Id),

    CONSTRAINT FK_FinanceTransaction_Institution
        FOREIGN KEY (InstitutionId) REFERENCES Institution(Id),

    CONSTRAINT FK_FinanceTransaction_Person
        FOREIGN KEY (PersonId) REFERENCES Person(Id),

    CONSTRAINT FK_FinanceTransaction_Category
        FOREIGN KEY (CategoryId) REFERENCES Category(Id),

    -- Amount must always be positive; direction comes from type + source/target
    CONSTRAINT CK_FinanceTransaction_Amount_Positive
        CHECK (Amount > 0),

    -- Allowed transaction types
    CONSTRAINT CK_FinanceTransaction_Type
        CHECK (TransactionType IN ('Income', 'Transfer', 'Expense', 'Adjustment')),

    -- At least some context must exist
    CONSTRAINT CK_FinanceTransaction_HasContext
        CHECK (
            InstitutionId IS NOT NULL
            OR SourceCostCenterId IS NOT NULL
            OR TargetCostCenterId IS NOT NULL
            OR TransactionType = 'Adjustment'
        ),

    -- IMPORTANT RULE:
    -- Every Income MUST have an InstitutionId (income always comes from some “source”)
    CONSTRAINT CK_FinanceTransaction_Income_HasInstitution
        CHECK (
            TransactionType <> 'Income'
            OR InstitutionId IS NOT NULL
        )
);
GO

------------------------------------------------------------
-- 6. Helpful indexes
------------------------------------------------------------

-- By date (global)
CREATE INDEX IX_FinanceTransaction_Date
    ON FinanceTransaction (TransactionDate);
GO

-- By person + date (for statements, salary-accumulated endpoints, etc.)
CREATE INDEX IX_FinanceTransaction_Person_Date
    ON FinanceTransaction (PersonId, TransactionDate);
GO

-- By cost center (source)
CREATE INDEX IX_FinanceTransaction_SourceCostCenter_Date
    ON FinanceTransaction (SourceCostCenterId, TransactionDate);
GO

-- By cost center (target)
CREATE INDEX IX_FinanceTransaction_TargetCostCenter_Date
    ON FinanceTransaction (TargetCostCenterId, TransactionDate);
GO

-- By institution (to see income per institution, etc.)
CREATE INDEX IX_FinanceTransaction_Institution_Date
    ON FinanceTransaction (InstitutionId, TransactionDate);
GO