using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSolidariaCristiana.Models.ViewModels;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in: {Username}", model.Username);
                    
                    // Si hay una URL de retorno válida, usarla
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    
                    // Siempre redirigir a la página de inicio
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Intento de inicio de sesión inválido.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult ManageUsers()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to selected role
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    _logger.LogInformation("Admin created new user: {Username} with role: {Role}", model.Username, model.Role);
                    TempData["SuccessMessage"] = $"Usuario {model.Username} creado exitosamente.";
                    return RedirectToAction(nameof(ManageUsers));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                CurrentRole = userRoles.FirstOrDefault() ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                // Update basic info
                user.UserName = model.Username;
                user.NormalizedUserName = model.Username.ToUpper();
                user.Email = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }

                // Update role
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                
                if (!string.IsNullOrEmpty(model.NewRole))
                {
                    await _userManager.AddToRoleAsync(user, model.NewRole);
                }

                _logger.LogInformation("Admin edited user: {Username}", model.Username);
                TempData["SuccessMessage"] = $"Usuario {model.Username} actualizado exitosamente.";
                return RedirectToAction(nameof(ManageUsers));
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent admin from deleting themselves
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == user.Id)
            {
                TempData["ErrorMessage"] = "No puedes eliminar tu propia cuenta.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin deleted user: {Username}", user.UserName);
                TempData["SuccessMessage"] = $"Usuario {user.UserName} eliminado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al eliminar el usuario.";
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Public Registration
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            var registrationEnabled = _configuration.GetValue<bool>("AppSettings:EnablePublicRegistration");
            if (!registrationEnabled)
            {
                TempData["ErrorMessage"] = "El registro público está actualmente deshabilitado.";
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var registrationEnabled = _configuration.GetValue<bool>("AppSettings:EnablePublicRegistration");
            if (!registrationEnabled)
            {
                TempData["ErrorMessage"] = "El registro público está actualmente deshabilitado.";
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to ViewerPublic role (restricted public registration)
                    await _userManager.AddToRoleAsync(user, "ViewerPublic");

                    _logger.LogInformation("New user registered: {Username} with ViewerPublic role", model.UserName);

                    // Send welcome email
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(model.Email, model.UserName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending welcome email to {Email}", model.Email);
                    }

                    TempData["SuccessMessage"] = "¡Registro exitoso! Ya puedes iniciar sesión.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Forgot Password
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction("ForgotPasswordConfirmation");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Generate callback URL - use configured SiteUrl for production
                var siteUrl = _configuration.GetValue<string>("AppSettings:SiteUrl") ?? $"{Request.Scheme}://{Request.Host}";
                var callbackUrl = $"{siteUrl}/Account/ResetPassword?userId={user.Id}&token={Uri.EscapeDataString(token)}";

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, callbackUrl!);
                    _logger.LogInformation("Password reset email sent to user {UserName} at {Email}", model.UserName, user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending password reset email to user {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "Error al enviar el correo. Por favor, intenta nuevamente.");
                    return View(model);
                }

                return RedirectToAction("ForgotPasswordConfirmation");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for user {UserId}", model.UserId);
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
