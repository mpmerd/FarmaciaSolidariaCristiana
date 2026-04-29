using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class DonacionesPage : ContentPage
{
    private readonly DonacionesViewModel _viewModel;
    private bool _initialized;

    public DonacionesPage(DonacionesViewModel viewModel)
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
            await _viewModel.LoadDonacionesCommand.ExecuteAsync(null);
        }
    }
}
