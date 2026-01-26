using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class TurnosPage : ContentPage
{
    private readonly TurnosViewModel _viewModel;

    public TurnosPage(TurnosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTurnosCommand.ExecuteAsync(null);
    }
}
