-- =============================================
-- Database Creation Script
-- Project: User Management System
-- Date: 2025
-- =============================================

-- Create Database
CREATE DATABASE UserManagementDB;
GO

USE UserManagementDB;
GO

-- =============================================
-- LOCATION TABLES (Must be created first)
-- =============================================

-- Provinces Table
CREATE TABLE Provinces (
    ProvinceID INT PRIMARY KEY IDENTITY(1,1),
    ProvinceName NVARCHAR(100) NOT NULL UNIQUE,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Cantons Table
CREATE TABLE Cantons (
    CantonID INT PRIMARY KEY IDENTITY(1,1),
    ProvinceID INT NOT NULL,
    CantonName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ProvinceID) REFERENCES Provinces(ProvinceID),
    -- Ensure canton names are unique within a province
    CONSTRAINT UQ_Canton_Province UNIQUE(CantonName, ProvinceID)
);

-- Districts Table
CREATE TABLE Districts (
    DistrictID INT PRIMARY KEY IDENTITY(1,1),
    CantonID INT NOT NULL,
    DistrictName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CantonID) REFERENCES Cantons(CantonID),
    -- Ensure district names are unique within a canton
    CONSTRAINT UQ_District_Canton UNIQUE(DistrictName, CantonID)
);

-- =============================================
-- USER RELATED TABLES
-- =============================================

-- User Types Table
CREATE TABLE UserTypes (
    UserTypeID INT PRIMARY KEY IDENTITY(1,1),
    TypeName NVARCHAR(50) NOT NULL UNIQUE -- 'CLIENT', 'ENGINEER', 'ADMIN' 
);

-- Main Users Table
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) UNIQUE NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastSurname NVARCHAR(100) NOT NULL,
    SecondSurname NVARCHAR(100),
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    BirthDate DATE NOT NULL,
    UserTypeID INT NOT NULL,
    ProvinceID INT NOT NULL,
    CantonID INT NOT NULL,
    DistrictID INT NULL,
    IsActive BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserTypeID) REFERENCES UserTypes(UserTypeID),
    FOREIGN KEY (ProvinceID) REFERENCES Provinces(ProvinceID),
    FOREIGN KEY (CantonID) REFERENCES Cantons(CantonID),
    FOREIGN KEY (DistrictID) REFERENCES Districts(DistrictID)
);

-- Engineers Table
CREATE TABLE Engineers (
    EngineerID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT UNIQUE NOT NULL,
    Career NVARCHAR(200) NOT NULL,
    Specialization NVARCHAR(200) NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- Administrators Table
CREATE TABLE Administrators (
    AdminID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT UNIQUE NOT NULL,
    Detail NVARCHAR(500) NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);

-- Crear tabla para tokens de sesión
CREATE TABLE SessionTokens (
    TokenID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    Token UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    ExpirationDate DATETIME NOT NULL,
    IsValid BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- =============================================
-- CONSTRAINTS
-- =============================================

-- Check constraint for email validation based on user type
ALTER TABLE Users
ADD CONSTRAINT CK_Email_UserType CHECK (
    (UserTypeID = 1) OR -- Client (any email)
    (UserTypeID = 2 AND Email LIKE '%@ing.com') OR -- Engineer
    (UserTypeID = 3 AND Email LIKE '%@admin.com') -- Admin
);
GO

-- Check constraint for birth date (must be at least 18 years old)
ALTER TABLE Users
ADD CONSTRAINT CK_BirthDate CHECK (
    DATEDIFF(YEAR, BirthDate, GETDATE()) >= 18
);
GO

-- =============================================
-- SP para Login
-- =============================================

-- SP para Login
CREATE PROCEDURE sp_UserLogin
    @Email NVARCHAR(255),
    @Password NVARCHAR(255),
    @LoginSuccess BIT OUTPUT,
    @Message NVARCHAR(500) OUTPUT,
    @UserID INT OUTPUT,
    @UserType NVARCHAR(50) OUTPUT,
    @SessionToken UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @LoginSuccess = 0;
    SET @UserID = NULL;
    SET @UserType = NULL;
    SET @SessionToken = NULL;
    
    DECLARE @StoredPasswordHash NVARCHAR(255);
    DECLARE @UserTypeID INT;
    
    -- Verificar si el usuario existe (sin importar IsActive)
    SELECT @UserID = u.UserID, 
           @StoredPasswordHash = u.PasswordHash,
           @UserTypeID = u.UserTypeID,
           @UserType = ut.TypeName
    FROM Users u
    INNER JOIN UserTypes ut ON u.UserTypeID = ut.UserTypeID
    WHERE u.Email = @Email;
    
    IF @UserID IS NULL
    BEGIN
        SET @Message = 'Error: Correo electrónico no registrado.';
        RETURN;
    END
    
    -- Verificar la contraseña
    DECLARE @InputPasswordHash NVARCHAR(255);
    SET @InputPasswordHash = CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password + @Email), 2);
    
    IF @StoredPasswordHash != @InputPasswordHash
    BEGIN
        SET @Message = 'Error: Contraseña incorrecta.';
        SET @UserID = NULL;
        SET @UserType = NULL;
        RETURN;
    END
    
    -- Login exitoso - Crear token de sesión
    SET @SessionToken = NEWID();
    INSERT INTO SessionTokens (UserID, Token, ExpirationDate)
    VALUES (@UserID, @SessionToken, DATEADD(HOUR, 24, GETDATE()));
    
    -- NUEVO: Activar la cuenta automáticamente al hacer login
    UPDATE Users
    SET IsActive = 1,
        UpdatedAt = GETDATE()
    WHERE UserID = @UserID;
    
    SET @LoginSuccess = 1;
    SET @Message = 'Login exitoso.';
END;
GO

-- =============================================
-- SPs Separados para Crear Cada Tipo de Usuario
-- =============================================

-- Primero, insertar los tipos de usuario si no existen
IF NOT EXISTS (SELECT 1 FROM UserTypes WHERE TypeName = 'CLIENT')
    INSERT INTO UserTypes (TypeName) VALUES ('CLIENT');
IF NOT EXISTS (SELECT 1 FROM UserTypes WHERE TypeName = 'ENGINEER')
    INSERT INTO UserTypes (TypeName) VALUES ('ENGINEER');
IF NOT EXISTS (SELECT 1 FROM UserTypes WHERE TypeName = 'ADMIN')
    INSERT INTO UserTypes (TypeName) VALUES ('ADMIN');
GO

-- SP para crear Cliente
CREATE PROCEDURE sp_CreateClient
    @Username NVARCHAR(50),
    @FirstName NVARCHAR(100),
    @LastSurname NVARCHAR(100),
    @SecondSurname NVARCHAR(100) = NULL,
    @Email NVARCHAR(255),
    @Password NVARCHAR(255),
    @BirthDate DATE,
    @ProvinceID INT,
    @CantonID INT,
    @DistrictID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validar que el email NO termine en @ing.com o @admin.com
        IF @Email LIKE '%@ing.com' OR @Email LIKE '%@admin.com'
        BEGIN
            RAISERROR('Error: Los clientes no pueden usar correos @ing.com o @admin.com', 16, 1);
            RETURN;
        END
        
        DECLARE @UserID INT;
        DECLARE @PasswordHash NVARCHAR(255);
        
        -- Validar jerarquía de ubicación
        IF dbo.fn_ValidateLocationHierarchy(@ProvinceID, @CantonID, @DistrictID) = 0
        BEGIN
            RAISERROR('Jerarquía de ubicación inválida.', 16, 1);
            RETURN;
        END
        
        -- Hash de la contraseña
        SET @PasswordHash = CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password + @Email), 2);
        
        -- Insertar en Users
        INSERT INTO Users (Username, FirstName, LastSurname, SecondSurname, 
                          Email, PasswordHash, BirthDate, UserTypeID, 
                          ProvinceID, CantonID, DistrictID)
        VALUES (@Username, @FirstName, @LastSurname, @SecondSurname, 
                @Email, @PasswordHash, @BirthDate, 1, -- UserTypeID = 1 para CLIENT
                @ProvinceID, @CantonID, @DistrictID);
        
        SET @UserID = SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
        
        SELECT @UserID AS NewUserID, 'Cliente creado exitosamente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP para crear Ingeniero
CREATE PROCEDURE sp_CreateEngineer
    @Username NVARCHAR(50),
    @FirstName NVARCHAR(100),
    @LastSurname NVARCHAR(100),
    @SecondSurname NVARCHAR(100) = NULL,
    @Email NVARCHAR(255),
    @Password NVARCHAR(255),
    @BirthDate DATE,
    @ProvinceID INT,
    @CantonID INT,
    @DistrictID INT = NULL,
    @Career NVARCHAR(200),
    @Specialization NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validar que el email termine en @ing.com
        IF @Email NOT LIKE '%@ing.com'
        BEGIN
            RAISERROR('Error: Los ingenieros deben usar correo @ing.com', 16, 1);
            RETURN;
        END
        
        DECLARE @UserID INT;
        DECLARE @PasswordHash NVARCHAR(255);
        
        -- Validar jerarquía de ubicación
        IF dbo.fn_ValidateLocationHierarchy(@ProvinceID, @CantonID, @DistrictID) = 0
        BEGIN
            RAISERROR('Jerarquía de ubicación inválida.', 16, 1);
            RETURN;
        END
        
        -- Hash de la contraseña
        SET @PasswordHash = CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password + @Email), 2);
        
        -- Insertar en Users
        INSERT INTO Users (Username, FirstName, LastSurname, SecondSurname, 
                          Email, PasswordHash, BirthDate, UserTypeID, 
                          ProvinceID, CantonID, DistrictID)
        VALUES (@Username, @FirstName, @LastSurname, @SecondSurname, 
                @Email, @PasswordHash, @BirthDate, 2, -- UserTypeID = 2 para ENGINEER
                @ProvinceID, @CantonID, @DistrictID);
        
        SET @UserID = SCOPE_IDENTITY();
        
        -- Insertar en Engineers
        INSERT INTO Engineers (UserID, Career, Specialization)
        VALUES (@UserID, @Career, @Specialization);
        
        COMMIT TRANSACTION;
        
        SELECT @UserID AS NewUserID, 'Ingeniero creado exitosamente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP para crear Administrador
CREATE PROCEDURE sp_CreateAdministrator
    @Username NVARCHAR(50),
    @FirstName NVARCHAR(100),
    @LastSurname NVARCHAR(100),
    @SecondSurname NVARCHAR(100) = NULL,
    @Email NVARCHAR(255),
    @Password NVARCHAR(255),
    @BirthDate DATE,
    @ProvinceID INT,
    @CantonID INT,
    @DistrictID INT = NULL,
    @Detail NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Validar que el email termine en @admin.com
        IF @Email NOT LIKE '%@admin.com'
        BEGIN
            RAISERROR('Error: Los administradores deben usar correo @admin.com', 16, 1);
            RETURN;
        END
        
        DECLARE @UserID INT;
        DECLARE @PasswordHash NVARCHAR(255);
        
        -- Validar jerarquía de ubicación
        IF dbo.fn_ValidateLocationHierarchy(@ProvinceID, @CantonID, @DistrictID) = 0
        BEGIN
            RAISERROR('Jerarquía de ubicación inválida.', 16, 1);
            RETURN;
        END
        
        -- Hash de la contraseña
        SET @PasswordHash = CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password + @Email), 2);
        
        -- Insertar en Users
        INSERT INTO Users (Username, FirstName, LastSurname, SecondSurname, 
                          Email, PasswordHash, BirthDate, UserTypeID, 
                          ProvinceID, CantonID, DistrictID)
        VALUES (@Username, @FirstName, @LastSurname, @SecondSurname, 
                @Email, @PasswordHash, @BirthDate, 3, -- UserTypeID = 3 para ADMIN
                @ProvinceID, @CantonID, @DistrictID);
        
        SET @UserID = SCOPE_IDENTITY();
        
        -- Insertar en Administrators
        INSERT INTO Administrators (UserID, Detail)
        VALUES (@UserID, @Detail);
        
        COMMIT TRANSACTION;
        
        SELECT @UserID AS NewUserID, 'Administrador creado exitosamente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- =============================================
-- Views para Visualizar Usuarios Activos por Tipo
-- =============================================

-- View para Clientes con sesiones activas
CREATE VIEW vw_ActiveClients AS
SELECT DISTINCT
    u.UserID,
    u.Username,
    u.FirstName,
    u.LastSurname,
    u.SecondSurname,
    u.Email,
    u.BirthDate,
    DATEDIFF(YEAR, u.BirthDate, GETDATE()) - 
    CASE 
        WHEN DATEADD(YEAR, DATEDIFF(YEAR, u.BirthDate, GETDATE()), u.BirthDate) > GETDATE() 
        THEN 1 
        ELSE 0 
    END AS Age,
    p.ProvinceName,
    c.CantonName,
    d.DistrictName,
    u.CreatedAt,
    u.UpdatedAt,
    u.IsActive
FROM Users u
INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
INNER JOIN Cantons c ON u.CantonID = c.CantonID
LEFT JOIN Districts d ON u.DistrictID = d.DistrictID
WHERE u.UserTypeID = 1 
AND EXISTS (
    SELECT 1 FROM SessionTokens st 
    WHERE st.UserID = u.UserID 
    AND st.IsValid = 1 
    AND st.ExpirationDate > GETDATE()
);
GO

-- View para Ingenieros con sesiones activas
CREATE VIEW vw_ActiveEngineers AS
SELECT DISTINCT
    u.UserID,
    u.Username,
    u.FirstName,
    u.LastSurname,
    u.SecondSurname,
    u.Email,
    u.BirthDate,
    DATEDIFF(YEAR, u.BirthDate, GETDATE()) - 
    CASE 
        WHEN DATEADD(YEAR, DATEDIFF(YEAR, u.BirthDate, GETDATE()), u.BirthDate) > GETDATE() 
        THEN 1 
        ELSE 0 
    END AS Age,
    e.Career,
    e.Specialization,
    p.ProvinceName,
    c.CantonName,
    d.DistrictName,
    u.CreatedAt,
    u.UpdatedAt,
    u.IsActive
FROM Users u
INNER JOIN Engineers e ON u.UserID = e.UserID
INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
INNER JOIN Cantons c ON u.CantonID = c.CantonID
LEFT JOIN Districts d ON u.DistrictID = d.DistrictID
WHERE u.UserTypeID = 2 
AND EXISTS (
    SELECT 1 FROM SessionTokens st 
    WHERE st.UserID = u.UserID 
    AND st.IsValid = 1 
    AND st.ExpirationDate > GETDATE()
);
GO

-- View para Administradores con sesiones activas
CREATE VIEW vw_ActiveAdministrators AS
SELECT DISTINCT
    u.UserID,
    u.Username,
    u.FirstName,
    u.LastSurname,
    u.SecondSurname,
    u.Email,
    u.BirthDate,
    DATEDIFF(YEAR, u.BirthDate, GETDATE()) - 
    CASE 
        WHEN DATEADD(YEAR, DATEDIFF(YEAR, u.BirthDate, GETDATE()), u.BirthDate) > GETDATE() 
        THEN 1 
        ELSE 0 
    END AS Age,
    a.Detail,
    p.ProvinceName,
    c.CantonName,
    d.DistrictName,
    u.CreatedAt,
    u.UpdatedAt,
    u.IsActive
FROM Users u
INNER JOIN Administrators a ON u.UserID = a.UserID
INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
INNER JOIN Cantons c ON u.CantonID = c.CantonID
LEFT JOIN Districts d ON u.DistrictID = d.DistrictID
WHERE u.UserTypeID = 3 
AND EXISTS (
    SELECT 1 FROM SessionTokens st 
    WHERE st.UserID = u.UserID 
    AND st.IsValid = 1 
    AND st.ExpirationDate > GETDATE()
);
GO

-- =============================================
-- Views para Visualizar TODOS los Usuarios por Tipo
-- =============================================

-- View para TODOS los Clientes (sin restricciones de sesión)
CREATE VIEW vw_AllClients AS
SELECT DISTINCT
    u.UserID,
    u.Username,
    u.FirstName,
    u.LastSurname,
    u.SecondSurname,
    u.Email,
    u.BirthDate,
    DATEDIFF(YEAR, u.BirthDate, GETDATE()) - 
    CASE 
        WHEN DATEADD(YEAR, DATEDIFF(YEAR, u.BirthDate, GETDATE()), u.BirthDate) > GETDATE() 
        THEN 1 
        ELSE 0 
    END AS Age,
    p.ProvinceName,
    c.CantonName,
    d.DistrictName,
    u.CreatedAt,
    u.UpdatedAt,
    u.IsActive,
    -- Indicador si tiene sesión activa
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM SessionTokens st 
            WHERE st.UserID = u.UserID 
            AND st.IsValid = 1 
            AND st.ExpirationDate > GETDATE()
        ) THEN 1
        ELSE 0
    END AS HasActiveSession
FROM Users u
INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
INNER JOIN Cantons c ON u.CantonID = c.CantonID
LEFT JOIN Districts d ON u.DistrictID = d.DistrictID
WHERE u.UserTypeID = 1;
GO

-- View para TODOS los Ingenieros (sin restricciones de sesión)
CREATE VIEW vw_AllEngineers AS
SELECT DISTINCT
    u.UserID,
    u.Username,
    u.FirstName,
    u.LastSurname,
    u.SecondSurname,
    u.Email,
    u.BirthDate,
    DATEDIFF(YEAR, u.BirthDate, GETDATE()) - 
    CASE 
        WHEN DATEADD(YEAR, DATEDIFF(YEAR, u.BirthDate, GETDATE()), u.BirthDate) > GETDATE() 
        THEN 1 
        ELSE 0 
    END AS Age,
    e.Career,
    e.Specialization,
    p.ProvinceName,
    c.CantonName,
    d.DistrictName,
    u.CreatedAt,
    u.UpdatedAt,
    u.IsActive,
    -- Indicador si tiene sesión activa
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM SessionTokens st 
            WHERE st.UserID = u.UserID 
            AND st.IsValid = 1 
            AND st.ExpirationDate > GETDATE()
        ) THEN 1
        ELSE 0
    END AS HasActiveSession
FROM Users u
INNER JOIN Engineers e ON u.UserID = e.UserID
INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
INNER JOIN Cantons c ON u.CantonID = c.CantonID
LEFT JOIN Districts d ON u.DistrictID = d.DistrictID
WHERE u.UserTypeID = 2;
GO

-- View para TODOS los Administradores (sin restricciones de sesión)
CREATE VIEW vw_AllAdministrators AS
SELECT DISTINCT
    u.UserID,
    u.Username,
    u.FirstName,
    u.LastSurname,
    u.SecondSurname,
    u.Email,
    u.BirthDate,
    DATEDIFF(YEAR, u.BirthDate, GETDATE()) - 
    CASE 
        WHEN DATEADD(YEAR, DATEDIFF(YEAR, u.BirthDate, GETDATE()), u.BirthDate) > GETDATE() 
        THEN 1 
        ELSE 0 
    END AS Age,
    a.Detail,
    p.ProvinceName,
    c.CantonName,
    d.DistrictName,
    u.CreatedAt,
    u.UpdatedAt,
    u.IsActive,
    -- Indicador si tiene sesión activa
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM SessionTokens st 
            WHERE st.UserID = u.UserID 
            AND st.IsValid = 1 
            AND st.ExpirationDate > GETDATE()
        ) THEN 1
        ELSE 0
    END AS HasActiveSession
FROM Users u
INNER JOIN Administrators a ON u.UserID = a.UserID
INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
INNER JOIN Cantons c ON u.CantonID = c.CantonID
LEFT JOIN Districts d ON u.DistrictID = d.DistrictID
WHERE u.UserTypeID = 3;
GO

-- =============================================
-- SPs para Actualizar Cada Tipo de Usuario
-- =============================================

-- SP para actualizar Cliente
CREATE PROCEDURE sp_UpdateClient
    @UserID INT,
    @Username NVARCHAR(50) = NULL,
    @FirstName NVARCHAR(100) = NULL,
    @LastSurname NVARCHAR(100) = NULL,
    @SecondSurname NVARCHAR(100) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Password NVARCHAR(255) = NULL,
    @BirthDate DATE = NULL,
    @ProvinceID INT = NULL,
    @CantonID INT = NULL,
    @DistrictID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar que el usuario existe y es cliente
        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserID = @UserID AND UserTypeID = 1)
        BEGIN
            RAISERROR('El usuario no existe o no es un cliente.', 16, 1);
            RETURN;
        END
        
        -- Si se proporciona email, validar que no sea de ingeniero o admin
        IF @Email IS NOT NULL AND (@Email LIKE '%@ing.com' OR @Email LIKE '%@admin.com')
        BEGIN
            RAISERROR('Los clientes no pueden usar correos @ing.com o @admin.com', 16, 1);
            RETURN;
        END
        
        -- Si se proporcionan ubicaciones, validar jerarquía
        IF @ProvinceID IS NOT NULL AND @CantonID IS NOT NULL
        BEGIN
            IF dbo.fn_ValidateLocationHierarchy(@ProvinceID, @CantonID, @DistrictID) = 0
            BEGIN
                RAISERROR('Jerarquía de ubicación inválida.', 16, 1);
                RETURN;
            END
        END
        
        -- Actualizar solo los campos proporcionados
        UPDATE Users
        SET Username = ISNULL(@Username, Username),
            FirstName = ISNULL(@FirstName, FirstName),
            LastSurname = ISNULL(@LastSurname, LastSurname),
            SecondSurname = CASE WHEN @SecondSurname IS NULL THEN SecondSurname ELSE @SecondSurname END,
            Email = ISNULL(@Email, Email),
            PasswordHash = CASE 
                WHEN @Password IS NOT NULL 
                THEN CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password + ISNULL(@Email, Email)), 2)
                ELSE PasswordHash 
            END,
            BirthDate = ISNULL(@BirthDate, BirthDate),
            ProvinceID = ISNULL(@ProvinceID, ProvinceID),
            CantonID = ISNULL(@CantonID, CantonID),
            DistrictID = CASE WHEN @DistrictID IS NULL THEN DistrictID ELSE @DistrictID END,
            UpdatedAt = GETDATE()
        WHERE UserID = @UserID;
        
        COMMIT TRANSACTION;
        
        SELECT 'Cliente actualizado exitosamente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP para actualizar Ingeniero
CREATE PROCEDURE sp_UpdateEngineer
    @UserID INT,
    @Username NVARCHAR(50) = NULL,
    @FirstName NVARCHAR(100) = NULL,
    @LastSurname NVARCHAR(100) = NULL,
    @SecondSurname NVARCHAR(100) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Password NVARCHAR(255) = NULL,
    @BirthDate DATE = NULL,
    @ProvinceID INT = NULL,
    @CantonID INT = NULL,
    @DistrictID INT = NULL,
    @Career NVARCHAR(200) = NULL,
    @Specialization NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar que el usuario existe y es ingeniero
        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserID = @UserID AND UserTypeID = 2)
        BEGIN
            RAISERROR('El usuario no existe o no es un ingeniero.', 16, 1);
            RETURN;
        END
        
        -- Si se proporciona email, validar que sea @ing.com
        IF @Email IS NOT NULL AND @Email NOT LIKE '%@ing.com'
        BEGIN
            RAISERROR('Los ingenieros deben usar correo @ing.com', 16, 1);
            RETURN;
        END
        
        -- Si se proporcionan ubicaciones, validar jerarquía
        IF @ProvinceID IS NOT NULL AND @CantonID IS NOT NULL
        BEGIN
            IF dbo.fn_ValidateLocationHierarchy(@ProvinceID, @CantonID, @DistrictID) = 0
            BEGIN
                RAISERROR('Jerarquía de ubicación inválida.', 16, 1);
                RETURN;
            END
        END
        
        -- Actualizar tabla Users
        UPDATE Users
        SET Username = ISNULL(@Username, Username),
            FirstName = ISNULL(@FirstName, FirstName),
            LastSurname = ISNULL(@LastSurname, LastSurname),
            SecondSurname = CASE WHEN @SecondSurname IS NULL THEN SecondSurname ELSE @SecondSurname END,
            Email = ISNULL(@Email, Email),
            PasswordHash = CASE 
                WHEN @Password IS NOT NULL 
                THEN CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password + ISNULL(@Email, Email)), 2)
                ELSE PasswordHash 
            END,
            BirthDate = ISNULL(@BirthDate, BirthDate),
            ProvinceID = ISNULL(@ProvinceID, ProvinceID),
            CantonID = ISNULL(@CantonID, CantonID),
            DistrictID = CASE WHEN @DistrictID IS NULL THEN DistrictID ELSE @DistrictID END,
            UpdatedAt = GETDATE()
        WHERE UserID = @UserID;
        
        -- Actualizar tabla Engineers
        UPDATE Engineers
        SET Career = ISNULL(@Career, Career),
            Specialization = CASE WHEN @Specialization IS NULL THEN Specialization ELSE @Specialization END
        WHERE UserID = @UserID;
        
        COMMIT TRANSACTION;
        
        SELECT 'Ingeniero actualizado exitosamente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP para actualizar Administrador
CREATE PROCEDURE sp_UpdateAdministrator
    @UserID INT,
    @Username NVARCHAR(50) = NULL,
    @FirstName NVARCHAR(100) = NULL,
    @LastSurname NVARCHAR(100) = NULL,
    @SecondSurname NVARCHAR(100) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Password NVARCHAR(255) = NULL,
    @BirthDate DATE = NULL,
    @ProvinceID INT = NULL,
    @CantonID INT = NULL,
    @DistrictID INT = NULL,
    @Detail NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar que el usuario existe y es administrador
        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserID = @UserID AND UserTypeID = 3)
        BEGIN
            RAISERROR('El usuario no existe o no es un administrador.', 16, 1);
            RETURN;
        END
        
        -- Si se proporciona email, validar que sea @admin.com
        IF @Email IS NOT NULL AND @Email NOT LIKE '%@admin.com'
        BEGIN
            RAISERROR('Los administradores deben usar correo @admin.com', 16, 1);
            RETURN;
        END
        
        -- Si se proporcionan ubicaciones, validar jerarquía
        IF @ProvinceID IS NOT NULL AND @CantonID IS NOT NULL
        BEGIN
            IF dbo.fn_ValidateLocationHierarchy(@ProvinceID, @CantonID, @DistrictID) = 0
            BEGIN
                RAISERROR('Jerarquía de ubicación inválida.', 16, 1);
                RETURN;
            END
        END
        
        -- Actualizar tabla Users
        UPDATE Users
        SET Username = ISNULL(@Username, Username),
            FirstName = ISNULL(@FirstName, FirstName),
            LastSurname = ISNULL(@LastSurname, LastSurname),
            SecondSurname = CASE WHEN @SecondSurname IS NULL THEN SecondSurname ELSE @SecondSurname END,
            Email = ISNULL(@Email, Email),
            PasswordHash = CASE 
                WHEN @Password IS NOT NULL 
                THEN CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @Password + ISNULL(@Email, Email)), 2)
                ELSE PasswordHash 
            END,
            BirthDate = ISNULL(@BirthDate, BirthDate),
            ProvinceID = ISNULL(@ProvinceID, ProvinceID),
            CantonID = ISNULL(@CantonID, CantonID),
            DistrictID = CASE WHEN @DistrictID IS NULL THEN DistrictID ELSE @DistrictID END,
            UpdatedAt = GETDATE()
        WHERE UserID = @UserID;
        
        -- Actualizar tabla Administrators
        UPDATE Administrators
        SET Detail = CASE WHEN @Detail IS NULL THEN Detail ELSE @Detail END
        WHERE UserID = @UserID;
        
        COMMIT TRANSACTION;
        
        SELECT 'Administrador actualizado exitosamente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- =============================================
-- SP para Eliminar Permanentemente Usuarios
-- =============================================

-- SP para eliminar Cliente permanentemente
CREATE PROCEDURE sp_DeleteClient
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar que el usuario existe y es cliente
        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserID = @UserID AND UserTypeID = 1)
        BEGIN
            RAISERROR('El usuario no existe o no es un cliente.', 16, 1);
            RETURN;
        END
        
        -- Eliminar registros relacionados
        DELETE FROM SessionTokens WHERE UserID = @UserID;
        DELETE FROM Users WHERE UserID = @UserID;
        
        COMMIT TRANSACTION;
        
        SELECT 'Cliente eliminado permanentemente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP para eliminar Ingeniero permanentemente
CREATE PROCEDURE sp_DeleteEngineer
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar que el usuario existe y es ingeniero
        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserID = @UserID AND UserTypeID = 2)
        BEGIN
            RAISERROR('El usuario no existe o no es un ingeniero.', 16, 1);
            RETURN;
        END
        
        -- Eliminar registros relacionados (Engineers se elimina por CASCADE)
        DELETE FROM SessionTokens WHERE UserID = @UserID;
        DELETE FROM Users WHERE UserID = @UserID;
        
        COMMIT TRANSACTION;
        
        SELECT 'Ingeniero eliminado permanentemente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- SP para eliminar Administrador permanentemente
CREATE PROCEDURE sp_DeleteAdministrator
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar que el usuario existe y es administrador
        IF NOT EXISTS (SELECT 1 FROM Users WHERE UserID = @UserID AND UserTypeID = 3)
        BEGIN
            RAISERROR('El usuario no existe o no es un administrador.', 16, 1);
            RETURN;
        END
        
        -- Eliminar registros relacionados (Administrators se elimina por CASCADE)
        DELETE FROM SessionTokens WHERE UserID = @UserID;
        DELETE FROM Users WHERE UserID = @UserID;
        
        COMMIT TRANSACTION;
        
        SELECT 'Administrador eliminado permanentemente' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- =============================================
-- PROCEDIMIENTOS DE UTILIDAD ADICIONALES
-- =============================================

-- SP para validar token de sesión
CREATE PROCEDURE sp_ValidateSessionToken
    @Token UNIQUEIDENTIFIER,
    @IsValid BIT OUTPUT,
    @UserID INT OUTPUT,
    @UserType NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @IsValid = 0;
    SET @UserID = NULL;
    SET @UserType = NULL;
    
    SELECT @UserID = st.UserID,
           @UserType = ut.TypeName,
           @IsValid = CASE 
                        WHEN st.ExpirationDate > GETDATE() AND st.IsValid = 1 
                        THEN 1 
                        ELSE 0 
                      END
    FROM SessionTokens st
    INNER JOIN Users u ON st.UserID = u.UserID
    INNER JOIN UserTypes ut ON u.UserTypeID = ut.UserTypeID
    WHERE st.Token = @Token;
    
    -- Si el token expiró, marcarlo como inválido
    IF @IsValid = 0 AND @UserID IS NOT NULL
    BEGIN
        UPDATE SessionTokens
        SET IsValid = 0
        WHERE Token = @Token;
        
        -- NUEVO: Verificar si quedan tokens activos para actualizar IsActive
        IF NOT EXISTS (
            SELECT 1 FROM SessionTokens 
            WHERE UserID = @UserID 
            AND IsValid = 1 
            AND ExpirationDate > GETDATE()
        )
        BEGIN
            UPDATE Users
            SET IsActive = 0,
                UpdatedAt = GETDATE()
            WHERE UserID = @UserID;
        END
    END
END;
GO

-- SP para cerrar sesión
CREATE PROCEDURE sp_UserLogout
    @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UserID INT;
    
    -- Obtener el UserID del token
    SELECT @UserID = UserID
    FROM SessionTokens
    WHERE Token = @Token AND IsValid = 1;
    
    IF @UserID IS NOT NULL
    BEGIN
        -- Invalidar el token
        UPDATE SessionTokens
        SET IsValid = 0
        WHERE Token = @Token;
        
        -- NUEVO: Verificar si quedan tokens activos para este usuario
        IF NOT EXISTS (
            SELECT 1 FROM SessionTokens 
            WHERE UserID = @UserID 
            AND IsValid = 1 
            AND ExpirationDate > GETDATE()
        )
        BEGIN
            -- Si no hay más tokens activos, desactivar la cuenta
            UPDATE Users
            SET IsActive = 0,
                UpdatedAt = GETDATE()
            WHERE UserID = @UserID;
        END
    END
    
    SELECT 'Sesión cerrada exitosamente' AS Message;
END;
GO

-- =============================================
-- PROCEDIMIENTOS PARA GESTIONAR UBICACIONES
-- =============================================

-- SP para agregar Provincia
CREATE PROCEDURE sp_AddProvince
    @ProvinceName NVARCHAR(100),
    @ProvinceID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Validar que el nombre no esté vacío
        IF @ProvinceName IS NULL OR LTRIM(RTRIM(@ProvinceName)) = ''
        BEGIN
            RAISERROR('El nombre de la provincia no puede estar vacío.', 16, 1);
            RETURN;
        END
        
        -- Validar que no exista ya una provincia con ese nombre
        IF EXISTS (SELECT 1 FROM Provinces WHERE ProvinceName = @ProvinceName)
        BEGIN
            RAISERROR('Ya existe una provincia con ese nombre.', 16, 1);
            RETURN;
        END
        
        -- Insertar la nueva provincia
        INSERT INTO Provinces (ProvinceName) 
        VALUES (@ProvinceName);
        
        SET @ProvinceID = SCOPE_IDENTITY();
        
        SELECT @ProvinceID AS NewProvinceID, 
               'Provincia creada exitosamente' AS Message;
               
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

-- SP para agregar Cantón
CREATE PROCEDURE sp_AddCanton
    @CantonName NVARCHAR(100),
    @ProvinceID INT,
    @CantonID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Validar que el nombre no esté vacío
        IF @CantonName IS NULL OR LTRIM(RTRIM(@CantonName)) = ''
        BEGIN
            RAISERROR('El nombre del cantón no puede estar vacío.', 16, 1);
            RETURN;
        END
        
        -- Validar que la provincia existe
        IF NOT EXISTS (SELECT 1 FROM Provinces WHERE ProvinceID = @ProvinceID)
        BEGIN
            RAISERROR('La provincia especificada no existe.', 16, 1);
            RETURN;
        END
        
        -- Validar que no exista ya un cantón con ese nombre en la misma provincia
        IF EXISTS (SELECT 1 FROM Cantons WHERE CantonName = @CantonName AND ProvinceID = @ProvinceID)
        BEGIN
            RAISERROR('Ya existe un cantón con ese nombre en esta provincia.', 16, 1);
            RETURN;
        END
        
        -- Insertar el nuevo cantón
        INSERT INTO Cantons (CantonName, ProvinceID) 
        VALUES (@CantonName, @ProvinceID);
        
        SET @CantonID = SCOPE_IDENTITY();
        
        SELECT @CantonID AS NewCantonID,
               'Cantón creado exitosamente' AS Message;
               
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

-- SP para agregar Distrito
CREATE PROCEDURE sp_AddDistrict
    @DistrictName NVARCHAR(100),
    @CantonID INT,
    @DistrictID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Validar que el nombre no esté vacío
        IF @DistrictName IS NULL OR LTRIM(RTRIM(@DistrictName)) = ''
        BEGIN
            RAISERROR('El nombre del distrito no puede estar vacío.', 16, 1);
            RETURN;
        END
        
        -- Validar que el cantón existe
        IF NOT EXISTS (SELECT 1 FROM Cantons WHERE CantonID = @CantonID)
        BEGIN
            RAISERROR('El cantón especificado no existe.', 16, 1);
            RETURN;
        END
        
        -- Validar que no exista ya un distrito con ese nombre en el mismo cantón
        IF EXISTS (SELECT 1 FROM Districts WHERE DistrictName = @DistrictName AND CantonID = @CantonID)
        BEGIN
            RAISERROR('Ya existe un distrito con ese nombre en este cantón.', 16, 1);
            RETURN;
        END
        
        -- Insertar el nuevo distrito
        INSERT INTO Districts (DistrictName, CantonID) 
        VALUES (@DistrictName, @CantonID);
        
        SET @DistrictID = SCOPE_IDENTITY();
        
        SELECT @DistrictID AS NewDistrictID,
               'Distrito creado exitosamente' AS Message;
               
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

-- =============================================
-- PROCEDIMIENTOS DE CONSULTA DE UBICACIONES
-- =============================================

-- SP para obtener todas las provincias
CREATE PROCEDURE sp_GetProvinces
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT ProvinceID, ProvinceName, CreatedAt
    FROM Provinces
    ORDER BY ProvinceName;
END;
GO

-- SP para obtener cantones por provincia
CREATE PROCEDURE sp_GetCantonsByProvince
    @ProvinceID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT c.CantonID, c.CantonName, c.ProvinceID, p.ProvinceName, c.CreatedAt
    FROM Cantons c
    INNER JOIN Provinces p ON c.ProvinceID = p.ProvinceID
    WHERE c.ProvinceID = @ProvinceID
    ORDER BY c.CantonName;
END;
GO

-- SP para obtener distritos por cantón
CREATE PROCEDURE sp_GetDistrictsByCanton
    @CantonID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT d.DistrictID, d.DistrictName, d.CantonID, c.CantonName, 
           c.ProvinceID, p.ProvinceName, d.CreatedAt
    FROM Districts d
    INNER JOIN Cantons c ON d.CantonID = c.CantonID
    INNER JOIN Provinces p ON c.ProvinceID = p.ProvinceID
    WHERE d.CantonID = @CantonID
    ORDER BY d.DistrictName;
END;
GO

-- =============================================
-- PROCEDIMIENTO PARA CAMBIO DE CONTRASEÑA
-- =============================================

CREATE PROCEDURE sp_ChangePassword
    @UserID INT,
    @OldPassword NVARCHAR(255),
    @NewPassword NVARCHAR(255),
    @Success BIT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Success = 0;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @Email NVARCHAR(255);
        DECLARE @StoredPasswordHash NVARCHAR(255);
        
        -- Verificar que el usuario existe y obtener su información
        SELECT @Email = Email, 
               @StoredPasswordHash = PasswordHash
        FROM Users
        WHERE UserID = @UserID;
        
        IF @Email IS NULL
        BEGIN
            SET @Message = 'El usuario no existe.';
            RETURN;
        END
        
        -- Validar la contraseña actual
        DECLARE @OldPasswordHash NVARCHAR(255);
        SET @OldPasswordHash = CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @OldPassword + @Email), 2);
        
        IF @StoredPasswordHash != @OldPasswordHash
        BEGIN
            SET @Message = 'La contraseña actual es incorrecta.';
            RETURN;
        END
        
        -- Validar que la nueva contraseña no sea igual a la anterior
        IF @OldPassword = @NewPassword
        BEGIN
            SET @Message = 'La nueva contraseña debe ser diferente a la anterior.';
            RETURN;
        END
        
        -- Validar que la nueva contraseña no esté vacía
        IF @NewPassword IS NULL OR LEN(@NewPassword) < 6
        BEGIN
            SET @Message = 'La nueva contraseña debe tener al menos 6 caracteres.';
            RETURN;
        END
        
        -- Actualizar la contraseña
        DECLARE @NewPasswordHash NVARCHAR(255);
        SET @NewPasswordHash = CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @NewPassword + @Email), 2);
        
        UPDATE Users
        SET PasswordHash = @NewPasswordHash,
            UpdatedAt = GETDATE()
        WHERE UserID = @UserID;
        
        -- Invalidar todos los tokens de sesión existentes del usuario (seguridad adicional)
        UPDATE SessionTokens
        SET IsValid = 0
        WHERE UserID = @UserID AND IsValid = 1;
        
        -- Desactivar la cuenta ya que no tiene tokens activos
        UPDATE Users
        SET IsActive = 0,
            UpdatedAt = GETDATE()
        WHERE UserID = @UserID;
        
        COMMIT TRANSACTION;
        
        SET @Success = 1;
        SET @Message = 'Contraseña actualizada exitosamente. Por favor, inicie sesión nuevamente.';
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END;
GO

-- =============================================
-- PROCEDIMIENTO ADICIONAL: RESETEAR CONTRASEÑA (ADMIN)
-- =============================================

CREATE PROCEDURE sp_ResetPasswordByAdmin
    @AdminUserID INT,
    @TargetUserID INT,
    @NewPassword NVARCHAR(255),
    @Success BIT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Success = 0;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar que el usuario que ejecuta es un administrador con sesión activa
        IF NOT EXISTS (
            SELECT 1 FROM Users u 
            INNER JOIN UserTypes ut ON u.UserTypeID = ut.UserTypeID
            WHERE u.UserID = @AdminUserID 
            AND ut.TypeName = 'ADMIN'
            AND EXISTS (
                SELECT 1 FROM SessionTokens st
                WHERE st.UserID = u.UserID
                AND st.IsValid = 1
                AND st.ExpirationDate > GETDATE()
            )
        )
        BEGIN
            SET @Message = 'Solo los administradores con sesión activa pueden resetear contraseñas.';
            RETURN;
        END
        
        -- Verificar que el usuario objetivo existe
        DECLARE @TargetEmail NVARCHAR(255);
        
        SELECT @TargetEmail = Email
        FROM Users
        WHERE UserID = @TargetUserID;
        
        IF @TargetEmail IS NULL
        BEGIN
            SET @Message = 'El usuario objetivo no existe.';
            RETURN;
        END
        
        -- Validar que la nueva contraseña cumpla requisitos mínimos
        IF @NewPassword IS NULL OR LEN(@NewPassword) < 6
        BEGIN
            SET @Message = 'La nueva contraseña debe tener al menos 6 caracteres.';
            RETURN;
        END
        
        -- Actualizar la contraseña
        DECLARE @NewPasswordHash NVARCHAR(255);
        SET @NewPasswordHash = CONVERT(NVARCHAR(255), HASHBYTES('SHA2_256', @NewPassword + @TargetEmail), 2);
        
        UPDATE Users
        SET PasswordHash = @NewPasswordHash,
            UpdatedAt = GETDATE()
        WHERE UserID = @TargetUserID;
        
        -- Invalidar todos los tokens de sesión del usuario
        UPDATE SessionTokens
        SET IsValid = 0
        WHERE UserID = @TargetUserID AND IsValid = 1;
        
        -- Desactivar la cuenta del usuario objetivo ya que no tiene tokens activos
        UPDATE Users
        SET IsActive = 0,
            UpdatedAt = GETDATE()
        WHERE UserID = @TargetUserID;
        
        COMMIT TRANSACTION;
        
        SET @Success = 1;
        SET @Message = 'Contraseña reseteada exitosamente. El usuario deberá iniciar sesión con la nueva contraseña.';
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE sp_CleanExpiredTokens
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Crear tabla temporal con usuarios afectados
        CREATE TABLE #AffectedUsers (UserID INT);
        
        -- Identificar usuarios cuyos tokens van a ser eliminados
        INSERT INTO #AffectedUsers (UserID)
        SELECT DISTINCT UserID 
        FROM SessionTokens 
        WHERE ExpirationDate < GETDATE() OR IsValid = 0;
        
        -- Eliminar tokens expirados o inválidos
        DELETE FROM SessionTokens 
        WHERE ExpirationDate < GETDATE() OR IsValid = 0;
        
        -- Actualizar IsActive para usuarios sin tokens activos
        UPDATE Users
        SET IsActive = 0,
            UpdatedAt = GETDATE()
        WHERE UserID IN (SELECT UserID FROM #AffectedUsers)
        AND NOT EXISTS (
            SELECT 1 FROM SessionTokens st
            WHERE st.UserID = Users.UserID
            AND st.IsValid = 1
            AND st.ExpirationDate > GETDATE()
        );
        
        DROP TABLE #AffectedUsers;
        
        COMMIT TRANSACTION;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- =============================================
-- Sincronizar IsActive con tokens activos
-- =============================================
CREATE PROCEDURE sp_SyncIsActiveWithTokens
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Activar usuarios con tokens válidos
        UPDATE Users
        SET IsActive = 1,
            UpdatedAt = GETDATE()
        WHERE EXISTS (
            SELECT 1 FROM SessionTokens st
            WHERE st.UserID = Users.UserID
            AND st.IsValid = 1
            AND st.ExpirationDate > GETDATE()
        )
        AND IsActive = 0;
        
        -- Desactivar usuarios sin tokens válidos
        UPDATE Users
        SET IsActive = 0,
            UpdatedAt = GETDATE()
        WHERE NOT EXISTS (
            SELECT 1 FROM SessionTokens st
            WHERE st.UserID = Users.UserID
            AND st.IsValid = 1
            AND st.ExpirationDate > GETDATE()
        )
        AND IsActive = 1;
        
        COMMIT TRANSACTION;
        
        SELECT 'Sincronización de IsActive completada' AS Message;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

PRINT 'Database creation completed successfully!';
GO
-- =============================================