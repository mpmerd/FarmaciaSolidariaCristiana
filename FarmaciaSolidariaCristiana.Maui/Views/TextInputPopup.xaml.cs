namespace FarmaciaSolidariaCristiana.Maui.Views;

/// <summary>
/// Popup reutilizable para entrada de texto multilínea.
/// Alternativa a DisplayPromptAsync cuando se necesita un editor de varias líneas.
/// </summary>
public partial class TextInputPopup : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();
    private readonly bool _isRequired;

    public TextInputPopup(string title, string message, string? placeholder = null, 
        string? initialValue = null, bool isRequired = false, 
        string confirmText = "Aceptar", string cancelText = "Cancelar")
    {
        InitializeComponent();
        
        _isRequired = isRequired;
        
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        
        if (!string.IsNullOrEmpty(placeholder))
            TextEditor.Placeholder = placeholder;
        
        if (!string.IsNullOrEmpty(initialValue))
            TextEditor.Text = initialValue;
            
        ConfirmButton.Text = confirmText;
        CancelButton.Text = cancelText;
        
        UpdateCharCount();
        TextEditor.TextChanged += (s, e) => UpdateCharCount();
    }

    private void UpdateCharCount()
    {
        var length = TextEditor.Text?.Length ?? 0;
        CharCountLabel.Text = $"{length} / 1000";
    }

    /// <summary>
    /// Muestra el popup y espera el resultado.
    /// </summary>
    /// <returns>El texto ingresado, o null si se canceló</returns>
    public Task<string?> GetResultAsync() => _tcs.Task;

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        var text = TextEditor.Text?.Trim();
        
        if (_isRequired && string.IsNullOrEmpty(text))
        {
            await DisplayAlertAsync("Campo requerido", "Por favor ingrese un texto.", "OK");
            return;
        }
        
        _tcs.SetResult(text);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.SetResult(null);
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Método estático para mostrar el popup fácilmente.
    /// </summary>
    public static async Task<string?> ShowAsync(string title, string message, 
        string? placeholder = null, string? initialValue = null, 
        bool isRequired = false, string confirmText = "Aceptar", string cancelText = "Cancelar")
    {
        var popup = new TextInputPopup(title, message, placeholder, initialValue, isRequired, confirmText, cancelText);
        await Shell.Current.Navigation.PushModalAsync(popup);
        return await popup.GetResultAsync();
    }
}
