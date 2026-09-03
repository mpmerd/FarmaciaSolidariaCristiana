namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// ViewModel para el reporte diario de turnos aprobados
    /// </summary>
    public class ReporteTurnosDiaViewModel
    {
        public DateTime? Fecha { get; set; }
        public List<TurnoReporteItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Información de un turno dentro del reporte diario
    /// </summary>
    public class TurnoReporteItem
    {
        /// <summary>ID global del turno (clave primaria en la BD)</summary>
        public int TurnoId { get; set; }

        /// <summary>Número secuencial del turno dentro del día (ej. 3 de 30)</summary>
        public int? NumeroTurnoDia { get; set; }

        /// <summary>Fecha y hora asignada del turno</summary>
        public DateTime? FechaPreferida { get; set; }

        /// <summary>
        /// Número de documento en texto plano.
        /// Solo disponible si el paciente está registrado en la ficha de pacientes.
        /// </summary>
        public string? DocumentoIdentidad { get; set; }

        /// <summary>
        /// Nombre completo del paciente.
        /// Solo disponible si el paciente está registrado en la ficha de pacientes.
        /// </summary>
        public string? NombrePaciente { get; set; }

        /// <summary>True si el paciente existe en la tabla de pacientes</summary>
        public bool PacienteEncontrado { get; set; }

        public List<ItemReporte> Medicamentos { get; set; } = new();
        public List<ItemReporte> Insumos { get; set; } = new();

        /// <summary>
        /// Ruta relativa (desde wwwroot) de la imagen de receta médica.
        /// Solo tiene valor cuando PacienteEncontrado == false y la receta es imagen (.jpg/.png).
        /// </summary>
        public string? RecetaMedicaImagePath { get; set; }

        /// <summary>True si la receta es PDF (no embebible como imagen)</summary>
        public bool RecetaMedicaEsPdf { get; set; }
    }

    /// <summary>
    /// Medicamento o insumo dentro del reporte
    /// </summary>
    public class ItemReporte
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string Unidad { get; set; } = string.Empty;
    }
}
