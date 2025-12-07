-- ============================================================================
-- Setup tSQLt Framework para SQL Server Unit Testing
-- ============================================================================

-- Verificar si tSQLt ya está instalado
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'tSQLt')
BEGIN
    PRINT 'tSQLt no está instalado. Por favor instalar desde: https://tsqlt.org/downloads/'
    PRINT 'Instrucciones:'
    PRINT '1. Descargar tSQLt.zip'
    PRINT '2. Ejecutar tSQLt.class.sql en tu base de datos'
    PRINT '3. Ejecutar EXEC tSQLt.EnableExternalAccess'
    RETURN;
END

-- Crear esquema de tests si no existe
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'PlayerNewsTests')
BEGIN
    EXEC tSQLt.NewTestClass 'PlayerNewsTests';
    PRINT '✅ Test class PlayerNewsTests creado'
END

GO

-- Crear tablas de datos de prueba
IF OBJECT_ID('PlayerNewsTests.TestData_Users', 'U') IS NULL
BEGIN
    CREATE TABLE PlayerNewsTests.TestData_Users (
        UserID INT PRIMARY KEY,
        Name NVARCHAR(100),
        Email NVARCHAR(255),
        SystemRoleCode NVARCHAR(20)
    );
END

IF OBJECT_ID('PlayerNewsTests.TestData_Teams', 'U') IS NULL
BEGIN
    CREATE TABLE PlayerNewsTests.TestData_Teams (
        NFLTeamID INT PRIMARY KEY,
        TeamName NVARCHAR(100),
        IsActive BIT
    );
END

IF OBJECT_ID('PlayerNewsTests.TestData_Players', 'U') IS NULL
BEGIN
    CREATE TABLE PlayerNewsTests.TestData_Players (
        NFLPlayerID INT PRIMARY KEY,
        FirstName NVARCHAR(50),
        LastName NVARCHAR(50),
        Position NVARCHAR(10),
        NFLTeamID INT,
        IsActive BIT,
        CurrentDesignationID TINYINT NULL,
        InjuryStatus NVARCHAR(50) NULL,
        InjuryDescription NVARCHAR(200) NULL
    );
END

IF OBJECT_ID('PlayerNewsTests.TestData_Designations', 'U') IS NULL
BEGIN
    CREATE TABLE PlayerNewsTests.TestData_Designations (
        DesignationID TINYINT PRIMARY KEY,
        DesignationCode NVARCHAR(10),
        DesignationName NVARCHAR(50),
        Description NVARCHAR(200)
    );
END

GO

PRINT '✅ Framework de testing configurado correctamente'
GO