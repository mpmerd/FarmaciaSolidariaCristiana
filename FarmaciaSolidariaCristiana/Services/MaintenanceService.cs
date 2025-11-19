namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Implementación del servicio de modo de mantenimiento
    /// Usa un archivo en disco para persistir el estado entre reinicios
    /// </summary>
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MaintenanceService> _logger;
        private readonly string _maintenanceFilePath;
        private static readonly object _lock = new object();

        public MaintenanceService(
            IWebHostEnvironment environment,
            ILogger<MaintenanceService> logger)
        {
            _environment = environment;
            _logger = logger;
            _maintenanceFilePath = Path.Combine(_environment.ContentRootPath, "maintenance.json");
        }

        public bool IsMaintenanceMode()
        {
            lock (_lock)
            {
                if (!File.Exists(_maintenanceFilePath))
                    return false;

                try
                {
                    var json = File.ReadAllText(_maintenanceFilePath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<MaintenanceData>(json);
                    return data?.IsActive ?? false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error leyendo estado de mantenimiento");
                    return false;
                }
            }
        }

        public void EnableMaintenanceMode(string reason)
        {
            lock (_lock)
            {
                try
                {
                    var data = new MaintenanceData
                    {
                        IsActive = true,
                        Reason = reason,
                        ActivatedAt = DateTime.Now
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(_maintenanceFilePath, json);
                    _logger.LogInformation("Modo de mantenimiento activado: {Reason}", reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error activando modo de mantenimiento");
                    throw;
                }
            }
        }

        public void DisableMaintenanceMode()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_maintenanceFilePath))
                    {
                        File.Delete(_maintenanceFilePath);
                        _logger.LogInformation("Modo de mantenimiento desactivado");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error desactivando modo de mantenimiento");
                    throw;
                }
            }
        }

        public string? GetMaintenanceReason()
        {
            lock (_lock)
            {
                if (!File.Exists(_maintenanceFilePath))
                    return null;

                try
                {
                    var json = File.ReadAllText(_maintenanceFilePath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<MaintenanceData>(json);
                    return data?.Reason;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error leyendo motivo de mantenimiento");
                    return null;
                }
            }
        }

        private class MaintenanceData
        {
            public bool IsActive { get; set; }
            public string Reason { get; set; } = string.Empty;
            public DateTime ActivatedAt { get; set; }
        }
    }
}
