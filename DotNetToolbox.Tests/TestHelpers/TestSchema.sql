IF OBJECT_ID(N'dbo.TestDimTable', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TestDimTable
    (
        Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CustomerId    NVARCHAR(50)      NOT NULL,
        Name          NVARCHAR(200)     NULL,
        IsActive      BIT               NULL,
        Amount        DECIMAL(18,4)     NULL,
        CreatedOn     DATETIME2(7)      NULL,
        UpdatedOffset DATETIMEOFFSET(7) NULL,
        SomeInt       INT               NULL,
        SomeBigInt    BIGINT            NULL,
        SomeDate      DATE              NULL,
        SomeFloat     FLOAT             NULL,
        RowGuid       UNIQUEIDENTIFIER  NULL,
        Notes         VARCHAR(100)      NULL
    );
END

