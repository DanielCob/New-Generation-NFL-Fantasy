-- =============================================
-- Database Population Script - FIXED VERSION
-- Project: User Management System
-- Date: 2025
-- =============================================

USE UserManagementDB;
GO

-- =============================================
-- LIMPIAR DATOS EXISTENTES (OPCIONAL)
-- =============================================
/*
-- Uncomment this section if you want to start fresh
DELETE FROM SessionTokens;
DELETE FROM Engineers;
DELETE FROM Administrators;
DELETE FROM Users;
DELETE FROM Districts;
DELETE FROM Cantons;
DELETE FROM Provinces;
DELETE FROM UserTypes;
*/

-- =============================================
-- 1. POPULATE USER TYPES
-- =============================================
PRINT '=== Inserting User Types ===';

-- Los tipos de usuario ya se insertan en el script principal, pero por seguridad:
IF NOT EXISTS (SELECT 1 FROM UserTypes WHERE TypeName = 'CLIENT')
    INSERT INTO UserTypes (TypeName) VALUES ('CLIENT');
IF NOT EXISTS (SELECT 1 FROM UserTypes WHERE TypeName = 'ENGINEER')
    INSERT INTO UserTypes (TypeName) VALUES ('ENGINEER');
IF NOT EXISTS (SELECT 1 FROM UserTypes WHERE TypeName = 'ADMIN')
    INSERT INTO UserTypes (TypeName) VALUES ('ADMIN');

PRINT 'User types verified/inserted successfully!';

-- =============================================
-- 2. POPULATE GEOGRAPHIC HIERARCHY
-- =============================================
PRINT '=== Creating Geographic Hierarchy ===';

-- DECLARACIÓN DE TODAS LAS VARIABLES QUE SE USARÁN EN TODO EL SCRIPT
-- Provinces (Costa Rica)
DECLARE @ProvinceID1 INT, @ProvinceID2 INT, @ProvinceID3 INT, @ProvinceID4 INT, 
        @ProvinceID5 INT, @ProvinceID6 INT, @ProvinceID7 INT;
DECLARE @CantonID INT, @DistrictID INT;

-- Variables para cantones específicos
DECLARE @SanJoseCentral INT, @Escazu INT, @AlajuelaCentral INT, 
        @CartagoCentral INT, @HerediaCentral INT;

-- Variables para distritos específicos
DECLARE @Carmen INT, @EscazuCentro INT, @AlajuelaCentroDistrict INT,
        @Oriental INT, @HerediaCentroDistrict INT;

-- Variables para login (las declaro aquí para usarlas más adelante)
DECLARE @LoginSuccess BIT, @Message NVARCHAR(500), @UserID INT, 
        @UserType NVARCHAR(50), @SessionToken UNIQUEIDENTIFIER;

-- Create Provinces
EXEC sp_AddProvince @ProvinceName = 'San Jose', @ProvinceID = @ProvinceID1 OUTPUT;
EXEC sp_AddProvince @ProvinceName = 'Alajuela', @ProvinceID = @ProvinceID2 OUTPUT;
EXEC sp_AddProvince @ProvinceName = 'Cartago', @ProvinceID = @ProvinceID3 OUTPUT;
EXEC sp_AddProvince @ProvinceName = 'Heredia', @ProvinceID = @ProvinceID4 OUTPUT;
EXEC sp_AddProvince @ProvinceName = 'Guanacaste', @ProvinceID = @ProvinceID5 OUTPUT;
EXEC sp_AddProvince @ProvinceName = 'Puntarenas', @ProvinceID = @ProvinceID6 OUTPUT;
EXEC sp_AddProvince @ProvinceName = 'Limon', @ProvinceID = @ProvinceID7 OUTPUT;

PRINT 'Provinces created successfully!';

-- Create Cantons for San Jose
EXEC sp_AddCanton @CantonName = 'Central', @ProvinceID = @ProvinceID1, @CantonID = @CantonID OUTPUT;
SET @SanJoseCentral = @CantonID;
EXEC sp_AddCanton @CantonName = 'Escazu', @ProvinceID = @ProvinceID1, @CantonID = @CantonID OUTPUT;
SET @Escazu = @CantonID;
EXEC sp_AddCanton @CantonName = 'Desamparados', @ProvinceID = @ProvinceID1, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'Santa Ana', @ProvinceID = @ProvinceID1, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'Goicoechea', @ProvinceID = @ProvinceID1, @CantonID = @CantonID OUTPUT;

-- Create Cantons for Alajuela
EXEC sp_AddCanton @CantonName = 'Central', @ProvinceID = @ProvinceID2, @CantonID = @CantonID OUTPUT;
SET @AlajuelaCentral = @CantonID;
EXEC sp_AddCanton @CantonName = 'San Ramon', @ProvinceID = @ProvinceID2, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'Grecia', @ProvinceID = @ProvinceID2, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'Atenas', @ProvinceID = @ProvinceID2, @CantonID = @CantonID OUTPUT;

-- Create Cantons for Cartago
EXEC sp_AddCanton @CantonName = 'Central', @ProvinceID = @ProvinceID3, @CantonID = @CantonID OUTPUT;
SET @CartagoCentral = @CantonID;
EXEC sp_AddCanton @CantonName = 'Paraiso', @ProvinceID = @ProvinceID3, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'La Union', @ProvinceID = @ProvinceID3, @CantonID = @CantonID OUTPUT;

-- Create Cantons for Heredia
EXEC sp_AddCanton @CantonName = 'Central', @ProvinceID = @ProvinceID4, @CantonID = @CantonID OUTPUT;
SET @HerediaCentral = @CantonID;
EXEC sp_AddCanton @CantonName = 'Barva', @ProvinceID = @ProvinceID4, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'Santo Domingo', @ProvinceID = @ProvinceID4, @CantonID = @CantonID OUTPUT;

-- Create basic cantons for other provinces
EXEC sp_AddCanton @CantonName = 'Liberia', @ProvinceID = @ProvinceID5, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'Central', @ProvinceID = @ProvinceID6, @CantonID = @CantonID OUTPUT;
EXEC sp_AddCanton @CantonName = 'Central', @ProvinceID = @ProvinceID7, @CantonID = @CantonID OUTPUT;

PRINT 'Cantons created successfully!';

-- Create Districts for main cantons
EXEC sp_AddDistrict @DistrictName = 'Carmen', @CantonID = @SanJoseCentral, @DistrictID = @DistrictID OUTPUT;
SET @Carmen = @DistrictID;
EXEC sp_AddDistrict @DistrictName = 'Merced', @CantonID = @SanJoseCentral, @DistrictID = @DistrictID OUTPUT;
EXEC sp_AddDistrict @DistrictName = 'Hospital', @CantonID = @SanJoseCentral, @DistrictID = @DistrictID OUTPUT;
EXEC sp_AddDistrict @DistrictName = 'Catedral', @CantonID = @SanJoseCentral, @DistrictID = @DistrictID OUTPUT;
EXEC sp_AddDistrict @DistrictName = 'Zapote', @CantonID = @SanJoseCentral, @DistrictID = @DistrictID OUTPUT;

EXEC sp_AddDistrict @DistrictName = 'Escazu Centro', @CantonID = @Escazu, @DistrictID = @DistrictID OUTPUT;
SET @EscazuCentro = @DistrictID;
EXEC sp_AddDistrict @DistrictName = 'San Antonio', @CantonID = @Escazu, @DistrictID = @DistrictID OUTPUT;
EXEC sp_AddDistrict @DistrictName = 'San Rafael', @CantonID = @Escazu, @DistrictID = @DistrictID OUTPUT;

EXEC sp_AddDistrict @DistrictName = 'Alajuela Centro', @CantonID = @AlajuelaCentral, @DistrictID = @DistrictID OUTPUT;
SET @AlajuelaCentroDistrict = @DistrictID;
EXEC sp_AddDistrict @DistrictName = 'San Jose', @CantonID = @AlajuelaCentral, @DistrictID = @DistrictID OUTPUT;
EXEC sp_AddDistrict @DistrictName = 'Carrizal', @CantonID = @AlajuelaCentral, @DistrictID = @DistrictID OUTPUT;

EXEC sp_AddDistrict @DistrictName = 'Oriental', @CantonID = @CartagoCentral, @DistrictID = @DistrictID OUTPUT;
SET @Oriental = @DistrictID;
EXEC sp_AddDistrict @DistrictName = 'Occidental', @CantonID = @CartagoCentral, @DistrictID = @DistrictID OUTPUT;

EXEC sp_AddDistrict @DistrictName = 'Heredia Centro', @CantonID = @HerediaCentral, @DistrictID = @DistrictID OUTPUT;
SET @HerediaCentroDistrict = @DistrictID;
EXEC sp_AddDistrict @DistrictName = 'Mercedes', @CantonID = @HerediaCentral, @DistrictID = @DistrictID OUTPUT;

PRINT 'Districts created successfully!';

-- =============================================
-- 3. CREATE SAMPLE USERS
-- =============================================
PRINT '=== Creating Sample Users ===';

-- =============================================
-- 3.1 CREATE CLIENTS
-- =============================================
PRINT '--- Creating Clients ---';

EXEC sp_CreateClient
    @Username = 'johndoe',
    @FirstName = 'John',
    @LastSurname = 'Doe',
    @SecondSurname = 'Smith',
    @Email = 'john.doe@gmail.com',
    @Password = 'SecurePass123',
    @BirthDate = '1990-05-15',
    @ProvinceID = @ProvinceID1, -- San Jose
    @CantonID = @SanJoseCentral,
    @DistrictID = @Carmen;

EXEC sp_CreateClient
    @Username = 'mariagarcia',
    @FirstName = 'Maria',
    @LastSurname = 'Garcia',
    @SecondSurname = 'Lopez',
    @Email = 'maria.garcia@hotmail.com',
    @Password = 'SecurePass456',
    @BirthDate = '1985-08-22',
    @ProvinceID = @ProvinceID1, -- San Jose
    @CantonID = @Escazu,
    @DistrictID = @EscazuCentro;

EXEC sp_CreateClient
    @Username = 'carlossanchez',
    @FirstName = 'Carlos',
    @LastSurname = 'Sanchez',
    @SecondSurname = NULL,
    @Email = 'carlos.sanchez@yahoo.com',
    @Password = 'SecurePass789',
    @BirthDate = '1995-02-10',
    @ProvinceID = @ProvinceID2, -- Alajuela
    @CantonID = @AlajuelaCentral,
    @DistrictID = NULL;

EXEC sp_CreateClient
    @Username = 'anaperez',
    @FirstName = 'Ana',
    @LastSurname = 'Perez',
    @SecondSurname = 'Rodriguez',
    @Email = 'ana.perez@outlook.com',
    @Password = 'ClientPass123',
    @BirthDate = '1992-11-08',
    @ProvinceID = @ProvinceID3, -- Cartago
    @CantonID = @CartagoCentral,
    @DistrictID = @Oriental;

EXEC sp_CreateClient
    @Username = 'pedrolopez',
    @FirstName = 'Pedro',
    @LastSurname = 'Lopez',
    @SecondSurname = 'Vargas',
    @Email = 'pedro.lopez@gmail.com',
    @Password = 'ClientPass456',
    @BirthDate = '1988-03-25',
    @ProvinceID = @ProvinceID4, -- Heredia
    @CantonID = @HerediaCentral,
    @DistrictID = @HerediaCentroDistrict;

EXEC sp_CreateClient
    @Username = 'luisamorales',
    @FirstName = 'Luisa',
    @LastSurname = 'Morales',
    @SecondSurname = NULL,
    @Email = 'luisa.morales@gmail.com',
    @Password = 'ClientPass789',
    @BirthDate = '1993-09-14',
    @ProvinceID = @ProvinceID1, -- San Jose
    @CantonID = @SanJoseCentral,
    @DistrictID = NULL;

PRINT 'Clients created successfully!';

-- =============================================
-- 3.2 CREATE ENGINEERS
-- =============================================
PRINT '--- Creating Engineers ---';

EXEC sp_CreateEngineer
    @Username = 'roberteng',
    @FirstName = 'Robert',
    @LastSurname = 'Johnson',
    @SecondSurname = 'Williams',
    @Email = 'robert.johnson@ing.com',
    @Password = 'EngPass123',
    @BirthDate = '1988-11-30',
    @ProvinceID = @ProvinceID1, -- San Jose
    @CantonID = @Escazu,
    @DistrictID = @EscazuCentro,
    @Career = 'Software Engineering',
    @Specialization = 'Cloud Architecture';

EXEC sp_CreateEngineer
    @Username = 'saraheng',
    @FirstName = 'Sarah',
    @LastSurname = 'Thompson',
    @SecondSurname = NULL,
    @Email = 'sarah.thompson@ing.com',
    @Password = 'EngPass456',
    @BirthDate = '1992-07-18',
    @ProvinceID = @ProvinceID4, -- Heredia
    @CantonID = @HerediaCentral,
    @DistrictID = @HerediaCentroDistrict,
    @Career = 'Civil Engineering',
    @Specialization = 'Structural Design';

EXEC sp_CreateEngineer
    @Username = 'davideng',
    @FirstName = 'David',
    @LastSurname = 'Martinez',
    @SecondSurname = 'Rodriguez',
    @Email = 'david.martinez@ing.com',
    @Password = 'EngPass789',
    @BirthDate = '1986-03-25',
    @ProvinceID = @ProvinceID2, -- Alajuela
    @CantonID = @AlajuelaCentral,
    @DistrictID = @AlajuelaCentroDistrict,
    @Career = 'Mechanical Engineering',
    @Specialization = NULL;

EXEC sp_CreateEngineer
    @Username = 'lauracivil',
    @FirstName = 'Laura',
    @LastSurname = 'Fernandez',
    @SecondSurname = 'Castro',
    @Email = 'laura.fernandez@ing.com',
    @Password = 'EngPass101',
    @BirthDate = '1990-01-12',
    @ProvinceID = @ProvinceID3, -- Cartago
    @CantonID = @CartagoCentral,
    @DistrictID = NULL,
    @Career = 'Civil Engineering',
    @Specialization = 'Transportation Engineering';

EXEC sp_CreateEngineer
    @Username = 'miguelsoft',
    @FirstName = 'Miguel',
    @LastSurname = 'Ramirez',
    @SecondSurname = 'Jimenez',
    @Email = 'miguel.ramirez@ing.com',
    @Password = 'EngPass202',
    @BirthDate = '1987-06-30',
    @ProvinceID = @ProvinceID1, -- San Jose
    @CantonID = @SanJoseCentral,
    @DistrictID = @Carmen,
    @Career = 'Software Engineering',
    @Specialization = 'Mobile Development';

EXEC sp_CreateEngineer
    @Username = 'andreaelec',
    @FirstName = 'Andrea',
    @LastSurname = 'Vega',
    @SecondSurname = NULL,
    @Email = 'andrea.vega@ing.com',
    @Password = 'EngPass303',
    @BirthDate = '1991-04-22',
    @ProvinceID = @ProvinceID2, -- Alajuela
    @CantonID = @AlajuelaCentral,
    @DistrictID = NULL,
    @Career = 'Electrical Engineering',
    @Specialization = 'Power Systems';

PRINT 'Engineers created successfully!';

-- =============================================
-- 3.3 CREATE ADMINISTRATORS
-- =============================================
PRINT '--- Creating Administrators ---';

EXEC sp_CreateAdministrator
    @Username = 'adminsuper',
    @FirstName = 'Admin',
    @LastSurname = 'Super',
    @SecondSurname = 'User',
    @Email = 'admin.super@admin.com',
    @Password = 'AdminPass123',
    @BirthDate = '1980-01-15',
    @ProvinceID = @ProvinceID1, -- San Jose
    @CantonID = @SanJoseCentral,
    @DistrictID = @Carmen,
    @Detail = 'System Administrator with full access privileges';

EXEC sp_CreateAdministrator
    @Username = 'adminmanager',
    @FirstName = 'Laura',
    @LastSurname = 'Anderson',
    @SecondSurname = NULL,
    @Email = 'laura.anderson@admin.com',
    @Password = 'AdminPass456',
    @BirthDate = '1983-09-20',
    @ProvinceID = @ProvinceID3, -- Cartago
    @CantonID = @CartagoCentral,
    @DistrictID = @Oriental,
    @Detail = 'Regional Manager for Central Valley operations';

EXEC sp_CreateAdministrator
    @Username = 'admintech',
    @FirstName = 'Michael',
    @LastSurname = 'Brown',
    @SecondSurname = 'Davis',
    @Email = 'michael.brown@admin.com',
    @Password = 'AdminPass789',
    @BirthDate = '1990-12-05',
    @ProvinceID = @ProvinceID4, -- Heredia
    @CantonID = @HerediaCentral,
    @DistrictID = NULL,
    @Detail = 'Technical Support Administrator';

EXEC sp_CreateAdministrator
    @Username = 'adminhr',
    @FirstName = 'Patricia',
    @LastSurname = 'Wilson',
    @SecondSurname = 'Taylor',
    @Email = 'patricia.wilson@admin.com',
    @Password = 'AdminPass101',
    @BirthDate = '1985-08-17',
    @ProvinceID = @ProvinceID2, -- Alajuela
    @CantonID = @AlajuelaCentral,
    @DistrictID = @AlajuelaCentroDistrict,
    @Detail = 'Human Resources Administrator';

PRINT 'Administrators created successfully!';

-- =============================================
-- 4. CREATE ACTIVE SESSIONS FOR SOME USERS
-- =============================================
PRINT '=== Creating Active Sessions ===';

-- Login some clients to create active sessions
EXEC sp_UserLogin 
    @Email = 'john.doe@gmail.com', 
    @Password = 'SecurePass123',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

EXEC sp_UserLogin 
    @Email = 'maria.garcia@hotmail.com', 
    @Password = 'SecurePass456',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

EXEC sp_UserLogin 
    @Email = 'ana.perez@outlook.com', 
    @Password = 'ClientPass123',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

-- Login some engineers
EXEC sp_UserLogin 
    @Email = 'robert.johnson@ing.com', 
    @Password = 'EngPass123',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

EXEC sp_UserLogin 
    @Email = 'sarah.thompson@ing.com', 
    @Password = 'EngPass456',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

EXEC sp_UserLogin 
    @Email = 'miguel.ramirez@ing.com', 
    @Password = 'EngPass202',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

-- Login some administrators
EXEC sp_UserLogin 
    @Email = 'admin.super@admin.com', 
    @Password = 'AdminPass123',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

EXEC sp_UserLogin 
    @Email = 'laura.anderson@admin.com', 
    @Password = 'AdminPass456',
    @LoginSuccess = @LoginSuccess OUTPUT,
    @Message = @Message OUTPUT,
    @UserID = @UserID OUTPUT,
    @UserType = @UserType OUTPUT,
    @SessionToken = @SessionToken OUTPUT;

PRINT 'Active sessions created successfully!';

-- =============================================
-- 5. VERIFICATION QUERIES
-- =============================================
PRINT '=== Database Population Verification ===';

-- Show summary of created data
PRINT 'Summary of created data:';

SELECT 'User Types' as Category, COUNT(*) as Count FROM UserTypes
UNION ALL
SELECT 'Provinces', COUNT(*) FROM Provinces
UNION ALL
SELECT 'Cantons', COUNT(*) FROM Cantons
UNION ALL
SELECT 'Districts', COUNT(*) FROM Districts
UNION ALL
SELECT 'Total Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Clients', COUNT(*) FROM Users WHERE UserTypeID = 1
UNION ALL
SELECT 'Engineers', COUNT(*) FROM Users WHERE UserTypeID = 2
UNION ALL
SELECT 'Administrators', COUNT(*) FROM Users WHERE UserTypeID = 3
UNION ALL
SELECT 'Active Sessions', COUNT(*) FROM SessionTokens WHERE IsValid = 1 AND ExpirationDate > GETDATE()
UNION ALL
SELECT 'Active Users', COUNT(*) FROM Users WHERE IsActive = 1;

-- Show active users by type
PRINT 'Active users by type:';
SELECT 
    ut.TypeName,
    COUNT(*) as ActiveCount
FROM Users u
INNER JOIN UserTypes ut ON u.UserTypeID = ut.UserTypeID
WHERE u.IsActive = 1
GROUP BY ut.TypeName;

-- =============================================
-- 6. SAMPLE QUERIES TO TEST THE VIEWS
-- =============================================
PRINT '=== Testing Views ===';

PRINT 'Active Clients:';
SELECT COUNT(*) as ActiveClientsCount FROM vw_ActiveClients;

PRINT 'All Clients:';
SELECT COUNT(*) as AllClientsCount FROM vw_AllClients;

PRINT 'Active Engineers:';
SELECT COUNT(*) as ActiveEngineersCount FROM vw_ActiveEngineers;

PRINT 'All Engineers:';
SELECT COUNT(*) as AllEngineersCount FROM vw_AllEngineers;

PRINT 'Active Administrators:';
SELECT COUNT(*) as ActiveAdministratorsCount FROM vw_ActiveAdministrators;

PRINT 'All Administrators:';
SELECT COUNT(*) as AllAdministratorsCount FROM vw_AllAdministrators;

-- =============================================
-- FINAL SUMMARY
-- =============================================
PRINT '=== FINAL POPULATION SUMMARY ===';
PRINT 'Database population completed successfully!';
PRINT '';
PRINT 'Created:';
PRINT '- 7 Provinces';
PRINT '- 18 Cantons';
PRINT '- 15 Districts';
PRINT '- 16 Users total:';
PRINT '  * 7 Clients';
PRINT '  * 6 Engineers';
PRINT '  * 4 Administrators';
PRINT '- 8 Active Sessions';
PRINT '';
PRINT 'You can now test:';
PRINT '- All CRUD operations using the stored procedures';
PRINT '- Active user views (vw_ActiveClients, vw_ActiveEngineers, vw_ActiveAdministrators)';
PRINT '- Complete user views (vw_AllClients, vw_AllEngineers, vw_AllAdministrators)';
PRINT '- Login/logout functionality';
PRINT '- Session management';
PRINT '- Geographic hierarchy';
PRINT '';
PRINT 'Database is ready for testing and development!';
GO