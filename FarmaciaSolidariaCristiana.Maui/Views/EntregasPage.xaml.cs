using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class EntregasPage : ContentPage
{
    private readonly EntregasViewModel _viewModel;
    private bool _initialized;

    public EntregasPage(EntregasViewModel viewModel)
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
            await _viewModel.LoadEntregasCommand.ExecuteAsync(null);
        }
    }
}
