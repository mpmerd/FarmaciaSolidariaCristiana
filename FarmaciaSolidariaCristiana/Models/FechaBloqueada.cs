using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// Representa una fecha bloqueada donde no se permiten turnos
    /// Usado para días festivos, emergencias, o situaciones excepcionales
    /// </summary>
    public class FechaBloqueada
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Fecha Bloqueada")]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Motivo del Bloqueo")]
        public string Motivo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bloqueado por")]
        public string UsuarioId { get; set; } = string.Empty;

        public IdentityUser? Usuario { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
