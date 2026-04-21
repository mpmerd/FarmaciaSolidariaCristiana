using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class UsuariosViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<UserDto> usuarios = new();

    [ObservableProperty]
    private UserDto? selectedUser;

    [ObservableProperty]
    private List<string> rolesDisponibles = new() { "Admin", "Farmaceutico", "Viewer", "ViewerPublic" };

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string usuariosCountText = "📋 Usuarios (0)";

    private List<UserDto> _allUsuarios = new();

    partial void OnSearchTextChanged(string value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allUsuarios
            : _allUsuarios.Where(u =>
                u.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.Role.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Usuarios.Clear();
        foreach (var u in filtered)
            Usuarios.Add(u);
        UsuariosCountText = string.IsNullOrWhiteSpace(SearchText)
            ? $"📋 Usuarios ({_allUsuarios.Count})"
            : $"📋 Usuarios ({filtered.Count} de {_allUsuarios.Count})";
    }

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private bool isCreating;

    // Campos para crear/editar
    [ObservableProperty]
    private string editUserName = string.Empty;

    [ObservableProperty]
    private string editEmail = string.Empty;

    [ObservableProperty]
    private string editPassword = string.Empty;

    [ObservableProperty]
    private string editSelectedRole = string.Empty;

    [ObservableProperty]
    private string? editingUserId;

    public UsuariosViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Gestión de Usuarios";
    }

    [RelayCommand]
    private async Task LoadUsuariosAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetUsuariosAsync();

            if (response.Success && response.Data != null)
            {
                _allUsuarios = response.Data;
                AplicarFiltro();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar usuarios");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void StartCreating()
    {
        IsCreating = true;
        IsEditing = false;
        EditingUserId = null;
        EditUserName = string.Empty;
        EditEmail = string.Empty;
        EditPassword = string.Empty;
        EditSelectedRole = "ViewerPublic";
    }

    [RelayCommand]
    private void StartEditing(UserDto user)
    {
        IsEditing = true;
        IsCreating = false;
        EditingUserId = user.Id;
        EditUserName = user.UserName;
        EditEmail = user.Email;
        EditPassword = string.Empty; // No mostramos contraseña
        EditSelectedRole = user.Role;
        SelectedUser = user;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        IsCreating = false;
        EditingUserId = null;
        EditUserName = string.Empty;
        EditEmail = string.Empty;
        EditPassword = string.Empty;
        EditSelectedRole = string.Empty;
        SelectedUser = null;
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(EditUserName) || 
            string.IsNullOrWhiteSpace(EditEmail) || 
            string.IsNullOrWhiteSpace(EditSelectedRole))
        {
            await ShowErrorAsync("Por favor complete todos los campos requeridos");
            return;
        }

        if (IsCreating && string.IsNullOrWhiteSpace(EditPassword))
        {
            await ShowErrorAsync("La contraseña es requerida para nuevos usuarios");
            return;
        }

        try
        {
            IsBusy = true;

            if (IsCreating)
            {
                var request = new CreateUserRequest
                {
                    UserName = EditUserName.Trim(),
                    Email = EditEmail.Trim(),
                    Password = EditPassword,
                    Role = EditSelectedRole
                };

                var response = await ApiService.CrearUsuarioAsync(request);

                if (response.Success && response.Data != null)
                {
                    await ShowSuccessAsync($"Usuario {response.Data.UserName} creado exitosamente");
                    CancelEdit();
                    await LoadUsuariosAsync();
                }
                else
                {
                    await ShowErrorAsync(response.Message ?? "Error al crear usuario");
                }
            }
            else if (IsEditing && EditingUserId != null)
            {
                var request = new UpdateUserRequest
                {
                    UserName = EditUserName.Trim(),
                    Email = EditEmail.Trim(),
                    NewPassword = string.IsNullOrWhiteSpace(EditPassword) ? null : EditPassword,
                    NewRole = EditSelectedRole
                };

                var response = await ApiService.ActualizarUsuarioAsync(EditingUserId, request);

                if (response.Success && response.Data != null)
                {
                    await ShowSuccessAsync($"Usuario {response.Data.UserName} actualizado exitosamente");
                    CancelEdit();
                    await LoadUsuariosAsync();
                }
                else
                {
                    await ShowErrorAsync(response.Message ?? "Error al actualizar usuario");
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteUserAsync(UserDto user)
    {
        var currentUser = await AuthService.GetUserInfoAsync();
        if (currentUser?.Id == user.Id)
        {
            await ShowErrorAsync("No puedes eliminar tu propia cuenta");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Confirmar Eliminación",
            $"¿Estás seguro de eliminar al usuario {user.UserName}?\n\nEsta acción no se puede deshacer.",
            "Eliminar",
            "Cancelar");

        if (!confirm) return;

        try
        {
            IsBusy = true;

            var response = await ApiService.EliminarUsuarioAsync(user.Id);

            if (response.Success)
            {
                await ShowSuccessAsync($"Usuario {user.UserName} eliminado exitosamente");
                await LoadUsuariosAsync();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al eliminar usuario");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private new async Task ShowSuccessAsync(string message)
    {
        await Shell.Current.DisplayAlertAsync("Éxito", message, "OK");
    }

    public string GetRoleBadgeColor(string role)
    {
        return role switch
        {
            "Admin" => "#dc3545",        // Rojo
            "Farmaceutico" => "#28a745", // Verde
            "Viewer" => "#17a2b8",       // Azul info
            "ViewerPublic" => "#6c757d", // Gris
            _ => "#6c757d"
        };
    }

    public string GetRoleDisplayName(string role)
    {
        return role switch
        {
            "Admin" => "Administrador",
            "Farmaceutico" => "Farmacéutico",
            "Viewer" => "Visualizador",
            "ViewerPublic" => "Paciente",
            _ => role
        };
    }
}
