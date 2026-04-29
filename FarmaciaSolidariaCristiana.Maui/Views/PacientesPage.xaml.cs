using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class PacientesPage : ContentPage
{
    private readonly PacientesViewModel _viewModel;
    private bool _initialized;

    public PacientesPage(PacientesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_initialized)
        {
            _initialized = true;
            await _viewModel.InitializeAsync();
        }
        else
        {
            await _viewModel.LoadPacientesCommand.ExecuteAsync(null);
        }
    }
}
