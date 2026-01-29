using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class MedicamentosPage : ContentPage
{
    private readonly MedicamentosViewModel _viewModel;
    private bool _initialized;

    public MedicamentosPage(MedicamentosViewModel viewModel)
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
            await _viewModel.LoadMedicamentosCommand.ExecuteAsync(null);
        }
    }
}
