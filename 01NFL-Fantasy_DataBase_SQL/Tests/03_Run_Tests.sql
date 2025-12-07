-- ============================================================================
-- Ejecutar TODOS los tests de NFLPlayerNews
-- ============================================================================

-- Limpiar resultados previos
EXEC tSQLt.SetVerbose;
GO

-- Ejecutar todos los tests de la clase PlayerNewsTests
EXEC tSQLt.Run 'PlayerNewsTests';
GO

-- Ver resultados detallados
SELECT 
    Class,
    TestCase,
    Result,
    Msg
FROM tSQLt.TestResult
WHERE Class = 'PlayerNewsTests'
ORDER BY Id DESC;
GO

-- Resumen de resultados
SELECT 
    Result,
    COUNT(*) AS Count
FROM tSQLt.TestResult
WHERE Class = 'PlayerNewsTests'
GROUP BY Result;
GO