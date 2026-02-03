using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class UsuariosPage : ContentPage
{
    private readonly UsuariosViewModel _viewModel;

    public UsuariosPage(UsuariosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadUsuariosCommand.ExecuteAsync(null);
    }
}
