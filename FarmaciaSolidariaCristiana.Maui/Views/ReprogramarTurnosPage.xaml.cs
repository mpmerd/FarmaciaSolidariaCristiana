using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class ReprogramarTurnosPage : ContentPage
{
    private readonly ReprogramarTurnosViewModel _viewModel;

    public ReprogramarTurnosPage(ReprogramarTurnosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}
