using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class MedicamentosPage : ContentPage
{
    private readonly MedicamentosViewModel _viewModel;

    public MedicamentosPage(MedicamentosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMedicamentosCommand.ExecuteAsync(null);
    }
}
