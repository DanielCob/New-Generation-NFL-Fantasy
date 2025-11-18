using System.Text.RegularExpressions;

namespace NFL_Fantasy_API.SharedSystems.Security
{
    /// <summary>
    /// ============================================
    /// CONFIGURACIÓN DE RUTAS
    /// ============================================
    /// 
    /// Centraliza la lógica de:
    /// - Rutas públicas (no requieren autenticación)
    /// - Rutas protegidas (requieren autenticación)
    /// - Rutas administrativas (requieren rol ADMIN)
    /// 
    /// Separar esta configuración facilita el mantenimiento
    /// y hace explícitas las reglas de acceso del sistema.
    /// </summary>
    public static class RouteConfiguration
    {
        // ========================================
        // RUTAS PÚBLICAS (No requieren autenticación)
        // ========================================
        private static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/request-reset",
            "/api/auth/reset-with-token",
            "/api/seasons/current",
            "/api/reference/position-formats",
            "/api/scoring/schemas"
        };

        // Patrones regex para rutas GET públicas
        private static readonly List<Regex> PublicGetPatterns = new()
        {
            new Regex(@"^/api/reference/position-formats/\d+/slots$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^/api/scoring/schemas/\d+/rules$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^/swagger.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^/$", RegexOptions.IgnoreCase | RegexOptions.Compiled) // root
        };

        // ========================================
        // RUTAS ADMINISTRATIVAS (Requieren rol ADMIN)
        // ========================================
        private static readonly List<(Regex Pattern, Func<string, bool> MethodFilter)> AdminRoutes = new()
        {
            // Mutaciones de NFLTeam (POST/PUT/DELETE pero no GET)
            (
                new Regex(@"^/api/nflteam($|/)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                method => !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
            ),

            // Gestión de roles del sistema (cualquier método)
            (
                new Regex(@"^/api/system-roles($|/)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                method => true
            ),

            // Vistas administrativas
            (
                new Regex(@"^/api/views/.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                method => true
            ),

            // Gestión de temporadas (excepto GET /api/seasons/current que es público)
            (
                new Regex(@"^/api/seasons(?!/current$)($|/)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                method => true
            )
        };

        /// <summary>
        /// Determina si una ruta es pública (no requiere autenticación)
        /// </summary>
        public static bool IsPublicRoute(string path, string method)
        {
            // Rutas explícitamente públicas
            if (PublicRoutes.Contains(path))
                return true;

            // Patrones GET públicos (reference data, swagger, etc.)
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pattern in PublicGetPatterns)
                {
                    if (pattern.IsMatch(path))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determina si una ruta requiere rol ADMIN (autorización)
        /// </summary>
        public static bool RequiresAdminRole(string path, string method)
        {
            foreach (var (pattern, methodFilter) in AdminRoutes)
            {
                if (pattern.IsMatch(path) && methodFilter(method))
                    return true;
            }

            return false;
        }
    }
}