namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class NuevaEntregaPage : ContentPage
{
    public NuevaEntregaPage(ViewModels.NuevaEntregaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModels.NuevaEntregaViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
