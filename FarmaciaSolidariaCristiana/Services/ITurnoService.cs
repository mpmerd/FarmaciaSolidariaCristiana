using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Servicio para gestión de turnos con lógica anti-abuso
    /// </summary>
    public interface ITurnoService
    {
        /// <summary>
        /// Valida si el usuario puede solicitar un nuevo turno (1 por mes)
        /// </summary>
        Task<(bool CanRequest, string? Reason)> CanUserRequestTurnoAsync(string userId);
        
        /// <summary>
        /// Valida si hay disponibilidad para un día específico (límite: 30 turnos por día)
        /// </summary>
        Task<(bool HasCapacity, int CurrentCount, string? Reason)> CheckDailyCapacityAsync(DateTime fecha);

        /// <summary>
        /// Obtiene el próximo slot disponible (Martes/Viernes, 1-4 PM, cada 6 minutos)
        /// Busca desde la próxima semana en adelante hasta encontrar disponibilidad
        /// </summary>
        Task<DateTime> GetNextAvailableSlotAsync();

        /// <summary>
        /// Valida si los medicamentos solicitados tienen stock disponible
        /// </summary>
        Task<Dictionary<int, (bool Available, int Stock)>> CheckMedicinesStockAsync(List<int> medicineIds);

        /// <summary>
        /// Crea una nueva solicitud de turno
        /// </summary>
        Task<Turno> CreateTurnoAsync(Turno turno, List<(int MedicineId, int Quantity)> medicamentos,
            List<(int SupplyId, int Quantity)> insumos,
            IFormFile? receta, IFormFile? tarjeton);

        /// <summary>
        /// Obtiene turnos pendientes para el dashboard del farmacéutico
        /// </summary>
        Task<List<Turno>> GetPendingTurnosAsync();

        /// <summary>
        /// Obtiene todos los turnos con filtros
        /// </summary>
        Task<List<Turno>> GetTurnosAsync(string? estado = null, DateTime? desde = null, DateTime? hasta = null);

        /// <summary>
        /// Obtiene un turno por ID con sus medicamentos
        /// </summary>
        Task<Turno?> GetTurnoByIdAsync(int id);

        /// <summary>
        /// Aprueba un turno, genera PDF y envía email
        /// </summary>
        Task<(bool Success, string? Message, string? PdfPath)> ApproveTurnoAsync(int turnoId, string farmaceuticoId, 
            Dictionary<int, int>? cantidadesAprobadas = null, string? comentarios = null);

        /// <summary>
        /// Rechaza un turno con motivo
        /// </summary>
        Task<(bool Success, string? Message)> RejectTurnoAsync(int turnoId, string farmaceuticoId, string motivo);

        /// <summary>
        /// Marca un turno como completado (entregado)
        /// </summary>
        Task<bool> CompleteTurnoAsync(int turnoId);

        /// <summary>
        /// Busca turno por documento de identidad hasheado
        /// </summary>
        Task<Turno?> FindTurnoByDocumentHashAsync(string documentHash);

        /// <summary>
        /// Busca TODOS los turnos activos por documento de identidad hasheado
        /// </summary>
        Task<List<Turno>> FindAllTurnosByDocumentHashAsync(string documentHash);

        /// <summary>
        /// Obtiene historial de turnos de un usuario
        /// </summary>
        Task<List<Turno>> GetUserTurnosAsync(string userId);

        /// <summary>
        /// Hashea un documento de identidad para almacenamiento seguro
        /// </summary>
        string HashDocument(string document);

        /// <summary>
        /// Genera el número de turno del día
        /// </summary>
        Task<int> GenerateNumeroTurnoAsync(DateTime fecha);

        /// <summary>
        /// Valida si un usuario puede cancelar su turno (debe ser Aprobado y faltar más de 7 días)
        /// </summary>
        bool CanUserCancelTurno(Turno turno);

        /// <summary>
        /// Obtiene el mensaje de razón por la cual no se puede cancelar
        /// </summary>
        string GetCancelReasonMessage(Turno turno);

        /// <summary>
        /// Cancela un turno por solicitud del usuario
        /// </summary>
        Task<bool> CancelTurnoByUserAsync(int turnoId, string userId, string motivoCancelacion);
    }
}
