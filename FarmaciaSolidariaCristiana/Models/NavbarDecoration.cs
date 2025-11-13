namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// Representa una decoración temática para el navbar de la aplicación
    /// </summary>
    public class NavbarDecoration
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Nombre de la festividad o decoración
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Tipo de decoración: Predefined o Custom
        /// </summary>
        public DecorationType Type { get; set; }
        
        /// <summary>
        /// Clave interna para decoraciones predefinidas (navidad, epifania, etc.)
        /// </summary>
        public string? PresetKey { get; set; }
        
        /// <summary>
        /// Texto corto a mostrar junto al logo (opcional)
        /// </summary>
        public string? DisplayText { get; set; }
        
        /// <summary>
        /// Color del texto (hex, ej: #FFD700)
        /// </summary>
        public string? TextColor { get; set; }
        
        /// <summary>
        /// Ruta del icono/logo personalizado (para decoraciones custom)
        /// </summary>
        public string? CustomIconPath { get; set; }
        
        /// <summary>
        /// Clase CSS para el icono predefinido (ej: fa-tree, fa-star)
        /// </summary>
        public string? IconClass { get; set; }
        
        /// <summary>
        /// Color del icono (hex)
        /// </summary>
        public string? IconColor { get; set; }
        
        /// <summary>
        /// Indica si esta decoración está actualmente activa
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Fecha de activación
        /// </summary>
        public DateTime? ActivatedAt { get; set; }
        
        /// <summary>
        /// Usuario que activó la decoración
        /// </summary>
        public string? ActivatedBy { get; set; }
        
        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
    
    public enum DecorationType
    {
        /// <summary>
        /// Decoración predefinida (Navidad, Epifanía, etc.)
        /// </summary>
        Predefined,
        
        /// <summary>
        /// Decoración personalizada con icono subido por el admin
        /// </summary>
        Custom
    }
    
    /// <summary>
    /// Decoraciones predefinidas disponibles
    /// </summary>
    public static class PresetDecorations
    {
        public static readonly Dictionary<string, DecorationPreset> Presets = new()
        {
            {
                "navidad",
                new DecorationPreset
                {
                    Key = "navidad",
                    Name = "Navidad",
                    Description = "Celebración del nacimiento de Jesús (25 de diciembre)",
                    IconClass = "fa-solid fa-tree-christmas",
                    IconColor = "#228B22",
                    DefaultText = "¡Feliz Navidad!",
                    TextColor = "#FFD700",
                    SuggestedPeriod = "Del 24 de diciembre al 6 de enero"
                }
            },
            {
                "epifania",
                new DecorationPreset
                {
                    Key = "epifania",
                    Name = "Epifanía",
                    Description = "Manifestación de Jesús a los Reyes Magos (6 de enero)",
                    IconClass = "fa-solid fa-star",
                    IconColor = "#FFD700",
                    DefaultText = "Epifanía del Señor",
                    TextColor = "#4169E1",
                    SuggestedPeriod = "6 de enero"
                }
            },
            {
                "semanasanta",
                new DecorationPreset
                {
                    Key = "semanasanta",
                    Name = "Semana Santa",
                    Description = "Pasión, muerte y resurrección de Jesucristo",
                    IconClass = "fa-solid fa-cross",
                    IconColor = "#8B4513",
                    DefaultText = "Semana Santa",
                    TextColor = "#800080",
                    SuggestedPeriod = "Domingo de Ramos hasta Domingo de Resurrección"
                }
            },
            {
                "aldersgate",
                new DecorationPreset
                {
                    Key = "aldersgate",
                    Name = "Aldersgate Day",
                    Description = "Experiencia de conversión de Juan Wesley (24 de mayo)",
                    IconClass = "fa-solid fa-heart-pulse",
                    IconColor = "#DC143C",
                    DefaultText = "Aldersgate Day",
                    TextColor = "#DC143C",
                    SuggestedPeriod = "24 de mayo (tradición wesleyana)"
                }
            },
            {
                "pentecostes",
                new DecorationPreset
                {
                    Key = "pentecostes",
                    Name = "Pentecostés",
                    Description = "Venida del Espíritu Santo sobre los apóstoles",
                    IconClass = "fa-solid fa-fire-flame-curved",
                    IconColor = "#FF4500",
                    DefaultText = "Pentecostés",
                    TextColor = "#FF4500",
                    SuggestedPeriod = "50 días después de la Pascua"
                }
            }
        };
    }
    
    public class DecorationPreset
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string IconColor { get; set; } = string.Empty;
        public string DefaultText { get; set; } = string.Empty;
        public string TextColor { get; set; } = string.Empty;
        public string SuggestedPeriod { get; set; } = string.Empty;
    }
}
