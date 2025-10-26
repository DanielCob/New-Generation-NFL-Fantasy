export const environment = {
  production: false,
  apiUrl: 'https://localhost:58493/api',
  enableAdmin: false,

  // 🆕 NUEVAS CONFIGURACIONES
  maxImageSizeMB: 5,
  minImageDimension: 300,
  maxImageDimension: 1024,
  allowedImageTypes: ['image/jpeg', 'image/png'],

  // Paginación NFL Teams
  nflTeamsPageSize: 50,

  // Features flags
  features: {
    teamBranding: true,        // Feature 3.1
    rosterManagement: true,    // Feature 3.1
    nflTeamsCRUD: true,        // Feature 10.1
    playerBrowser: true        // Explorar jugadores
  }
};
