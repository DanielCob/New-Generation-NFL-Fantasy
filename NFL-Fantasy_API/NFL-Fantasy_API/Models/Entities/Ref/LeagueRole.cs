using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Ref
{
    /// <summary>
    /// Entidad que refleja la tabla ref.LeagueRole
    /// Roles disponibles en ligas: COMMISSIONER, CO_COMMISSIONER, MANAGER, SPECTATOR
    /// </summary>
    [Table("LeagueRole", Schema = "ref")]
    public class LeagueRole
    {
        [Key]
        [MaxLength(20)]
        public string RoleCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Display { get; set; } = string.Empty;
    }
}