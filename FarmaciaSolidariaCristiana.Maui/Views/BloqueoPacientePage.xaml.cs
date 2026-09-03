using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class BloqueoPacientePage : ContentPage
{
    private readonly BloqueoPacienteViewModel _viewModel;
    private bool _initialized;

    public BloqueoPacientePage(BloqueoPacienteViewModel viewModel)
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
    }
}
