using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class UsuariosPage : ContentPage
{
    private readonly UsuariosViewModel _viewModel;
    private bool _initialized;

    public UsuariosPage(UsuariosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_initialized)
        {
            _initialized = true;
            await _viewModel.LoadUsuariosCommand.ExecuteAsync(null);
        }
        else
        {
            await _viewModel.LoadUsuariosCommand.ExecuteAsync(null);
        }
    }
}
