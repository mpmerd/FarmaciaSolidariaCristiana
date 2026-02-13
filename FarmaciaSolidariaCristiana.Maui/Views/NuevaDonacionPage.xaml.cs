using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class NuevaDonacionPage : ContentPage
{
    private readonly NuevaDonacionViewModel _viewModel;

    public NuevaDonacionPage(NuevaDonacionViewModel viewModel)
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
