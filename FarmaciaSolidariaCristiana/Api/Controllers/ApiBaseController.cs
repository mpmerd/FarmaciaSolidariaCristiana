using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// Controlador base para todos los endpoints de la API.
    /// Usa autenticación JWT por defecto para todos los endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public abstract class ApiBaseController : ControllerBase
    {
        /// <summary>
        /// Retorna una respuesta de éxito estandarizada
        /// </summary>
        protected IActionResult ApiOk<T>(T data, string? message = null)
        {
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Operación exitosa",
                Data = data
            });
        }

        /// <summary>
        /// Retorna una respuesta de error estandarizada
        /// </summary>
        protected IActionResult ApiError(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null
            });
        }

        /// <summary>
        /// Retorna una respuesta de error con detalles de validación
        /// </summary>
        protected IActionResult ApiValidationError(Dictionary<string, string[]> errors)
        {
            return BadRequest(new ApiResponse<Dictionary<string, string[]>>
            {
                Success = false,
                Message = "Error de validación",
                Data = errors
            });
        }
    }

    /// <summary>
    /// Respuesta estandarizada de la API
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
