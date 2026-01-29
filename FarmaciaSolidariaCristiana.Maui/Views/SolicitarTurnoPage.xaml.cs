using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class SolicitarTurnoPage : ContentPage
{
    private readonly SolicitarTurnoViewModel _viewModel;
    private bool _initialized;

    public SolicitarTurnoPage(SolicitarTurnoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
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
