using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Verificar estado de registro al cargar la página
        if (BindingContext is RegisterViewModel vm)
        {
            await vm.CheckRegistrationStatusCommand.ExecuteAsync(null);
        }
    }
}
