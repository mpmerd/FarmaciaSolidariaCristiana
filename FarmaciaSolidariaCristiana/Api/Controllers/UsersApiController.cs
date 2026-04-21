using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Api.Models;
using FarmaciaSolidariaCristiana.Data;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de usuarios (solo Admin)
    /// </summary>
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersApiController : ApiBaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UsersApiController> _logger;
        private readonly ApplicationDbContext _context;

        public UsersApiController(
            UserManager<IdentityUser> userManager,
            ILogger<UsersApiController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los usuarios con sus roles
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<UserManagementDto>>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userManager.Users.ToListAsync();

            // Una sola query JOIN en lugar de N+1 llamadas a GetRolesAsync
            var roleMap = await _context.UserRoles
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(x => x.Name).FirstOrDefault() ?? "Sin rol");

            var userDtos = users
                .Select(user => new UserManagementDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    Role = roleMap.TryGetValue(user.Id, out var role) ? role : "Sin rol",
                    EmailConfirmed = user.EmailConfirmed
                })
                .OrderBy(u => GetRolePriority(u.Role))
                .ThenBy(u => u.UserName)
                .ToList();

            return ApiOk(userDtos);
        }

        /// <summary>
        /// Obtiene un usuario por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), 200)]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return ApiError("Usuario no encontrado", 404);

            var roles = await _userManager.GetRolesAsync(user);
            var dto = new UserManagementDto
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "Sin rol",
                EmailConfirmed = user.EmailConfirmed
            };

            return ApiOk(dto);
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateUserApiRequest request)
        {
            if (!ModelState.IsValid)
                return ApiError("Datos inválidos");

            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
                return ApiError("El nombre de usuario ya existe");

            var user = new IdentityUser
            {
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ApiError($"Error al crear usuario: {errors}");
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                await _userManager.AddToRoleAsync(user, request.Role);
            }

            _logger.LogInformation("Admin creó nuevo usuario: {Username} con rol: {Role}", request.UserName, request.Role);

            var dto = new UserManagementDto
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                Role = request.Role ?? "Sin rol",
                EmailConfirmed = true
            };

            return ApiOk(dto, "Usuario creado exitosamente");
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserApiRequest request)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return ApiError("Usuario no encontrado", 404);

            // Update basic info
            user.UserName = request.UserName;
            user.NormalizedUserName = request.UserName.ToUpper();
            user.Email = request.Email;
            user.NormalizedEmail = request.Email.ToUpper();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return ApiError($"Error al actualizar usuario: {errors}");
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                    return ApiError($"Error al cambiar contraseña: {errors}");
                }
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            if (!string.IsNullOrEmpty(request.NewRole))
            {
                await _userManager.AddToRoleAsync(user, request.NewRole);
            }

            _logger.LogInformation("Admin actualizó usuario: {Username}", request.UserName);

            var dto = new UserManagementDto
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                Role = request.NewRole ?? "Sin rol",
                EmailConfirmed = user.EmailConfirmed
            };

            return ApiOk(dto, "Usuario actualizado exitosamente");
        }

        /// <summary>
        /// Elimina un usuario
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return ApiError("Usuario no encontrado", 404);

            // Get current user to prevent self-deletion
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id)
                return ApiError("No puedes eliminar tu propia cuenta");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ApiError($"Error al eliminar usuario: {errors}");
            }

            _logger.LogInformation("Admin eliminó usuario: {Username}", user.UserName);

            return ApiOk(new { message = $"Usuario {user.UserName} eliminado exitosamente" });
        }

        /// <summary>
        /// Obtiene los roles disponibles
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
        public IActionResult GetRoles()
        {
            var roles = new List<string> { "Admin", "Farmaceutico", "Viewer", "ViewerPublic" };
            return ApiOk(roles);
        }

        private static int GetRolePriority(string role)
        {
            return role switch
            {
                "Admin" => 1,
                "Farmaceutico" => 2,
                "Viewer" => 3,
                "ViewerPublic" => 4,
                _ => 5
            };
        }
    }

    // DTOs para gestión de usuarios
    public class UserManagementDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
    }

    public class CreateUserApiRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido")]
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserApiRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        public string NewRole { get; set; } = string.Empty;
    }
}
