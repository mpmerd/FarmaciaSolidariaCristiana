using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class InsumosPage : ContentPage
{
    private readonly InsumosViewModel _viewModel;
    private bool _initialized;

    public InsumosPage(InsumosViewModel viewModel)
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
            await _viewModel.LoadInsumosCommand.ExecuteAsync(null);
        }
    }
}
