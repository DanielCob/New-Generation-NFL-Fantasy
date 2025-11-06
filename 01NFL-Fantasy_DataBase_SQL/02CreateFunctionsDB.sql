USE XNFLFantasyDB;
GO

/* ================================================
   FUNCIÓN: Genera un INT aleatorio único para IDs
   ================================================ */
USE XNFLFantasyDB;
GO

CREATE OR ALTER FUNCTION dbo.fn_GenerateRandomInt()
RETURNS INT
AS
BEGIN
    -- Normaliza a NVARCHAR todo lo que no sea NVARCHAR (especialmente sql_variant)
    DECLARE @client NVARCHAR(128) = CONVERT(NVARCHAR(128), CONNECTIONPROPERTY('client_net_address'));
    DECLARE @nonce  NVARCHAR(256) = CONVERT(NVARCHAR(256), SESSION_CONTEXT(N'nonce'));

    DECLARE @mix NVARCHAR(400) = CONCAT(
        CONVERT(NVARCHAR(33), SYSUTCDATETIME(), 126), N';',
        CONVERT(NVARCHAR(10), @@SPID),             N';',
        CONVERT(NVARCHAR(10), @@CPU_BUSY),         N';',
        CONVERT(NVARCHAR(10), @@IO_BUSY),          N';',
        CONVERT(NVARCHAR(10), @@PACKET_ERRORS),    N';',
        COALESCE(@client, N''),                    N';',
        COALESCE(@nonce,  N'')
    );

    DECLARE @hash VARBINARY(32) = HASHBYTES('SHA2_256', @mix);

    -- Mapear a [100_000_000, 999_999_999]
    RETURN ((CAST(SUBSTRING(@hash, 1, 4) AS INT) & 2147483647) % 900000000) + 100000000;
END
GO

