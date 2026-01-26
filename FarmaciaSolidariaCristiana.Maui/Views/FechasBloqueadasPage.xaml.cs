using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class FechasBloqueadasPage : ContentPage
{
    private readonly FechasBloqueadasViewModel _viewModel;

    public FechasBloqueadasPage(FechasBloqueadasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
