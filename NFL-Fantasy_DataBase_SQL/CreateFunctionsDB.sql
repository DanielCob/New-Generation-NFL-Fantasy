-- =============================================
-- FUNCTION TO VALIDATE LOCATION HIERARCHY
-- =============================================
CREATE FUNCTION fn_ValidateLocationHierarchy
(
    @ProvinceID INT,
    @CantonID INT,
    @DistrictID INT = NULL
)
RETURNS BIT
AS
BEGIN
    DECLARE @IsValid BIT = 0;
    
    -- Check if canton belongs to the province
    IF EXISTS (SELECT 1 FROM Cantons WHERE CantonID = @CantonID AND ProvinceID = @ProvinceID)
    BEGIN
        -- If no district specified, location is valid
        IF @DistrictID IS NULL
            SET @IsValid = 1;
        -- If district specified, check if it belongs to the canton
        ELSE IF EXISTS (SELECT 1 FROM Districts WHERE DistrictID = @DistrictID AND CantonID = @CantonID)
            SET @IsValid = 1;
    END
    
    RETURN @IsValid;
END;
GO