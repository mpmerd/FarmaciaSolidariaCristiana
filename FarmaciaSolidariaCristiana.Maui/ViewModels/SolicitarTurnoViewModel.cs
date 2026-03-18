using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para solicitar un nuevo turno
/// </summary>
public partial class SolicitarTurnoViewModel : BaseViewModel
{
    private readonly IImageCompressionService _imageCompressionService;
    
    // Límites de tamaño de archivos
    private const int MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB para imágenes
    private const int MaxPdfSizeBytes = 3 * 1024 * 1024;   // 3MB para PDFs (más restrictivo)
    
    #region Observable Properties

    [ObservableProperty]
    private string _documentoIdentidad = string.Empty;

    [ObservableProperty]
    private string _documentoError = string.Empty;

    [ObservableProperty]
    private bool _isDocumentoValid;

    [ObservableProperty]
    private string _tipoSolicitud = "Medicamento";

    [ObservableProperty]
    private bool _isMedicamento = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ItemBusqueda> _resultadosBusqueda = new();

    [ObservableProperty]
    private bool _showSearchResults;

    [ObservableProperty]
    private ObservableCollection<ItemSeleccionado> _itemsSeleccionados = new();

    [ObservableProperty]
    private ObservableCollection<DocumentoTurno> _documentosAdjuntos = new();

    [ObservableProperty]
    private string _notas = string.Empty;

    [ObservableProperty]
    private bool _canSubmit;

    #endregion

    private List<Medicine> _allMedicamentos = new();
    private List<Supply> _allInsumos = new();
    private HashSet<int> _restrictedMedicineIds = new();
    private string _lastCheckedDocument = string.Empty;

    // Tipos de documento permitidos
    public List<string> TiposDocumento { get; } = new()
    {
        "Receta Médica",
        "Tarjetón Sanitario",
        "Informe Médico",
        "Tratamiento",
        "Otro"
    };

    public SolicitarTurnoViewModel(
        IAuthService authService, 
        IApiService apiService,
        IImageCompressionService imageCompressionService)
        : base(authService, apiService)
    {
        _imageCompressionService = imageCompressionService;
        Title = "Solicitar Turno";
    }

    #region Property Changed Handlers

    partial void OnDocumentoIdentidadChanged(string value)
    {
        ValidateDocumento();
        UpdateCanSubmit();
        _ = LoadRestrictedMedicinesAsync();
    }

    partial void OnIsMedicamentoChanged(bool value)
    {
        TipoSolicitud = value ? "Medicamento" : "Insumo";
        // Limpiar selección al cambiar tipo
        ItemsSeleccionados.Clear();
        ResultadosBusqueda.Clear();
        SearchText = string.Empty;
        ShowSearchResults = false;
        UpdateCanSubmit();
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterItems();
    }

    #endregion

    #region Validation

    private void ValidateDocumento()
    {
        if (string.IsNullOrWhiteSpace(DocumentoIdentidad))
        {
            DocumentoError = "El documento es requerido";
            IsDocumentoValid = false;
            return;
        }

        var doc = DocumentoIdentidad.Trim().ToUpper();

        // Validar Carnet de Identidad (11 dígitos numéricos)
        if (Regex.IsMatch(doc, @"^\d{11}$"))
        {
            DocumentoError = string.Empty;
            IsDocumentoValid = true;
            return;
        }

        // Validar Pasaporte (1-3 letras + 6-7 dígitos)
        if (Regex.IsMatch(doc, @"^[A-Z]{1,3}\d{6,7}$"))
        {
            DocumentoError = string.Empty;
            IsDocumentoValid = true;
            return;
        }

        DocumentoError = "Documento inválido";
        IsDocumentoValid = false;
    }

    private void UpdateCanSubmit()
    {
        CanSubmit = IsDocumentoValid && ItemsSeleccionados.Count > 0;
    }

    private async Task LoadRestrictedMedicinesAsync()
    {
        if (!IsDocumentoValid)
        {
            _restrictedMedicineIds.Clear();
            _lastCheckedDocument = string.Empty;
            return;
        }

        var doc = DocumentoIdentidad.Trim().ToUpper();
        if (doc == _lastCheckedDocument) return;
        _lastCheckedDocument = doc;

        try
        {
            var result = await ApiService.GetRestrictedMedicinesAsync(doc);
            if (result.Success && result.Data != null)
            {
                _restrictedMedicineIds = new HashSet<int>(result.Data);
                // Remover items ya seleccionados que estén restringidos
                var itemsToRemove = ItemsSeleccionados
                    .Where(i => _restrictedMedicineIds.Contains(i.Id) && IsMedicamento)
                    .ToList();
                foreach (var item in itemsToRemove)
                    ItemsSeleccionados.Remove(item);
                if (itemsToRemove.Any())
                {
                    UpdateCanSubmit();
                    await ShowErrorAsync(
                        "Se removieron medicamentos que este paciente ya solicitó este mes. " +
                        "Un mismo medicamento no puede retirarse en más de un turno por mes.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SolicitarTurno] Error loading restricted medicines: {ex.Message}");
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void SetMedicamento()
    {
        IsMedicamento = true;
    }

    [RelayCommand]
    private void SetInsumo()
    {
        IsMedicamento = false;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            // Cargar medicamentos e insumos disponibles
            var medTask = ApiService.GetMedicamentosAsync();
            var insTask = ApiService.GetInsumosAsync();

            await Task.WhenAll(medTask, insTask);

            var medResult = await medTask;
            var insResult = await insTask;

            if (medResult.Success && medResult.Data != null)
            {
                _allMedicamentos = medResult.Data.Where(m => m.StockQuantity > 0).ToList();
            }

            if (insResult.Success && insResult.Data != null)
            {
                _allInsumos = insResult.Data.Where(s => s.StockQuantity > 0).ToList();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterItems()
    {
        if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2)
        {
            ResultadosBusqueda.Clear();
            ShowSearchResults = false;
            return;
        }

        var search = SearchText.ToLower();
        var results = new List<ItemBusqueda>();

        if (IsMedicamento)
        {
            results = _allMedicamentos
                .Where(m => m.Name.ToLower().Contains(search))
                .Take(10)
                .Select(m => new ItemBusqueda
                {
                    Id = m.Id,
                    Name = _restrictedMedicineIds.Contains(m.Id) 
                        ? $"{m.Name} (ya solicitado este mes)" 
                        : m.Name,
                    Stock = m.StockQuantity,
                    Unit = m.Unit,
                    Tipo = "Medicamento"
                })
                .ToList();
        }
        else
        {
            results = _allInsumos
                .Where(s => s.Name.ToLower().Contains(search))
                .Take(10)
                .Select(s => new ItemBusqueda
                {
                    Id = s.Id,
                    Name = s.Name,
                    Stock = s.StockQuantity,
                    Unit = s.Unit,
                    Tipo = "Insumo"
                })
                .ToList();
        }

        // Excluir items ya seleccionados y medicamentos restringidos
        var idsSeleccionados = ItemsSeleccionados.Select(i => i.Id).ToHashSet();
        results = results.Where(r => !idsSeleccionados.Contains(r.Id)).ToList();
        if (IsMedicamento)
        {
            results = results.Where(r => !_restrictedMedicineIds.Contains(r.Id)).ToList();
        }

        ResultadosBusqueda = new ObservableCollection<ItemBusqueda>(results);
        ShowSearchResults = results.Count > 0;
    }

    [RelayCommand]
    private async Task SelectItem(ItemBusqueda item)
    {
        if (item == null) return;

        // Verificar restricción de medicamento por mes
        if (IsMedicamento && _restrictedMedicineIds.Contains(item.Id))
        {
            await ShowErrorAsync(
                $"El medicamento \"{item.Name}\" ya fue solicitado por este paciente este mes. " +
                "Un mismo paciente no puede retirar el mismo medicamento en más de un turno por mes natural.");
            return;
        }

        var seleccionado = new ItemSeleccionado
        {
            Id = item.Id,
            Name = item.Name,
            Stock = item.Stock,
            Unit = item.Unit,
            Cantidad = 0
        };

        ItemsSeleccionados.Add(seleccionado);

        // Limpiar búsqueda
        SearchText = string.Empty;
        ResultadosBusqueda.Clear();
        ShowSearchResults = false;

        UpdateCanSubmit();
    }

    [RelayCommand]
    private void RemoveItem(ItemSeleccionado item)
    {
        if (item == null) return;
        ItemsSeleccionados.Remove(item);
        UpdateCanSubmit();
    }

    [RelayCommand]
    private async Task AddDocumentAsync()
    {
        try
        {
            var action = await Shell.Current.DisplayActionSheetAsync(
                "Agregar Documento",
                "Cancelar",
                null,
                "📷 Tomar Foto",
                "🖼️ Seleccionar de Galería",
                "📄 Seleccionar PDF");

            if (string.IsNullOrEmpty(action) || action == "Cancelar")
                return;

            // Pedir tipo de documento
            var tipoDoc = await Shell.Current.DisplayActionSheetAsync(
                "Tipo de Documento",
                "Cancelar",
                null,
                TiposDocumento.ToArray());

            if (string.IsNullOrEmpty(tipoDoc) || tipoDoc == "Cancelar")
                return;

            FileResult? result = null;
            byte[]? fileBytes = null;
            string fileName = string.Empty;
            string contentType = string.Empty;

            if (action == "📷 Tomar Foto")
            {
                // Verificar permisos de cámara
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        await ShowErrorAsync("Se necesita permiso de cámara para tomar fotos");
                        return;
                    }
                }

                result = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Tomar foto del documento"
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    fileBytes = await _imageCompressionService.CompressImageAsync(
                        stream, result.ContentType, 1920, 1080, 80);
                    
                    // Validar que la compresión devolvió datos
                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        await ShowErrorAsync("Error al procesar la imagen de la cámara.");
                        return;
                    }
                    
                    fileName = Path.ChangeExtension(result.FileName, ".jpg");
                    contentType = "image/jpeg"; // Siempre JPEG después de compresión
                    
                    Console.WriteLine($"[SolicitarTurno] Imagen de cámara comprimida: {FormatFileSize(fileBytes.Length)}");
                }
            }
            else if (action == "🖼️ Seleccionar de Galería")
            {
                var photoResults = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
                {
                    Title = "Seleccionar foto del documento"
                });
                result = photoResults?.FirstOrDefault();

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    fileBytes = await _imageCompressionService.CompressImageAsync(
                        stream, result.ContentType, 1920, 1080, 80);
                    
                    // Validar que la compresión devolvió datos
                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        await ShowErrorAsync("Error al procesar la imagen seleccionada.");
                        return;
                    }
                    
                    fileName = Path.ChangeExtension(result.FileName, ".jpg");
                    contentType = "image/jpeg";
                    
                    Console.WriteLine($"[SolicitarTurno] Imagen de galería comprimida: {FormatFileSize(fileBytes.Length)}");
                }
            }
            else if (action == "📄 Seleccionar PDF")
            {
                var pdfResult = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Seleccionar documento PDF",
                    FileTypes = FilePickerFileType.Pdf
                });

                if (pdfResult != null)
                {
                    using var stream = await pdfResult.OpenReadAsync();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                    
                    // Validar tamaño de PDF (más restrictivo)
                    if (fileBytes.Length > MaxPdfSizeBytes)
                    {
                        await ShowErrorAsync($"El PDF es demasiado grande ({FormatFileSize(fileBytes.Length)}).\n" +
                            $"El tamaño máximo permitido es {FormatFileSize(MaxPdfSizeBytes)}.\n\n" +
                            "Sugerencia: Use un PDF más pequeño o tome una foto del documento.");
                        return;
                    }
                    
                    fileName = pdfResult.FileName;
                    contentType = "application/pdf";
                    Console.WriteLine($"[SolicitarTurno] PDF seleccionado: {FormatFileSize(fileBytes.Length)}");
                }
            }

            if (fileBytes != null && fileBytes.Length > 0)
            {
                // Validar tamaño máximo (5MB para imágenes, PDFs ya validados)
                if (fileBytes.Length > MaxImageSizeBytes)
                {
                    await ShowErrorAsync($"El archivo es demasiado grande ({FormatFileSize(fileBytes.Length)}).\n" +
                        $"Tamaño máximo: {FormatFileSize(MaxImageSizeBytes)}");
                    return;
                }

                var documento = new DocumentoTurno
                {
                    TempId = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    DocumentType = tipoDoc,
                    ContentType = contentType,
                    FileBytes = fileBytes,
                    FileSizeDisplay = FormatFileSize(fileBytes.Length)
                };

                DocumentosAdjuntos.Add(documento);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al agregar documento: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveDocument(DocumentoTurno doc)
    {
        if (doc == null) return;
        DocumentosAdjuntos.Remove(doc);
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        Console.WriteLine($"[SolicitarTurno] SubmitAsync called. CanSubmit={CanSubmit}, IsDocumentoValid={IsDocumentoValid}, ItemsCount={ItemsSeleccionados.Count}, IsBusy={IsBusy}");
        
        if (!CanSubmit)
        {
            var msg = !IsDocumentoValid 
                ? "Ingrese un documento de identidad válido" 
                : ItemsSeleccionados.Count == 0 
                    ? "Debe seleccionar al menos un item" 
                    : "Complete todos los campos requeridos";
            await ShowErrorAsync(msg);
            return;
        }

        // Validar que todos los items tengan cantidad mayor a 0
        var itemsSinCantidad = ItemsSeleccionados.Where(i => i.Cantidad <= 0).ToList();
        if (itemsSinCantidad.Any())
        {
            var nombres = string.Join(", ", itemsSinCantidad.Select(i => i.Name));
            await ShowErrorAsync($"Por favor ingrese la cantidad a solicitar para:\n{nombres}\n\nLa cantidad debe ser mayor que 0.");
            return;
        }

        if (IsBusy) return;

        var confirm = await ShowConfirmAsync(
            "Confirmar Solicitud",
            "¿Está seguro de enviar esta solicitud de turno?");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            // 1. Crear el turno
            var request = new CrearTurnoMobileRequest
            {
                DocumentoIdentidad = DocumentoIdentidad.Trim().ToUpper(),
                TipoSolicitud = TipoSolicitud,
                Notas = Notas,
                Items = ItemsSeleccionados.Select(i => new TurnoItemRequest
                {
                    Id = i.Id,
                    Cantidad = i.Cantidad
                }).ToList()
            };

            var result = await ApiService.CrearTurnoMobileAsync(request);

            if (!result.Success || result.Data == null)
            {
                await ShowErrorAsync(result.Message ?? "Error al crear la solicitud");
                return;
            }

            var turnoId = result.Data.Id;

            // 2. Subir documentos uno por uno
            int docSubidos = 0;
            int docFallidos = 0;
            var erroresDocumentos = new List<string>();
            
            foreach (var doc in DocumentosAdjuntos)
            {
                try
                {
                    var docResult = await ApiService.SubirDocumentoTurnoAsync(
                        turnoId,
                        doc.FileName,
                        doc.DocumentType,
                        doc.FileBytes,
                        null);

                    if (docResult.Success)
                    {
                        docSubidos++;
                    }
                    else
                    {
                        docFallidos++;
                        erroresDocumentos.Add($"{doc.FileName}: {docResult.Message ?? "Error desconocido"}");
                        Console.WriteLine($"[SolicitarTurno] Error subiendo {doc.FileName}: {docResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    docFallidos++;
                    erroresDocumentos.Add($"{doc.FileName}: {ex.Message}");
                    Console.WriteLine($"[SolicitarTurno] Error subiendo documento: {ex.Message}");
                }
            }

            // 3. Verificar resultado de documentos
            if (DocumentosAdjuntos.Count > 0 && docSubidos == 0)
            {
                // Todos los documentos fallaron - ofrecer cancelar el turno
                var cancelarTurno = await Shell.Current.DisplayAlertAsync(
                    "Error al Subir Documentos",
                    $"El turno fue creado pero NO se pudieron subir los documentos.\n\n" +
                    $"Errores:\n{string.Join("\n", erroresDocumentos.Take(3))}\n\n" +
                    "¿Desea CANCELAR el turno e intentar de nuevo?",
                    "Sí, cancelar turno",
                    "No, mantener turno");
                    
                if (cancelarTurno)
                {
                    // Intentar cancelar el turno
                    try
                    {
                        var cancelResult = await ApiService.CancelarTurnoAsync(turnoId, "Cancelado por error al subir documentos");
                        if (cancelResult.Success)
                        {
                            await Shell.Current.DisplayAlertAsync(
                                "Turno Cancelado",
                                "El turno fue cancelado. Por favor, intente crear la solicitud de nuevo.",
                                "OK");
                            // No navegar, permitir que el usuario intente de nuevo
                            return;
                        }
                        else
                        {
                            await Shell.Current.DisplayAlertAsync(
                                "Advertencia",
                                $"No se pudo cancelar el turno automáticamente.\n\n" +
                                "Por favor, contacte a la farmacia para cancelar manualmente.",
                                "Entendido");
                        }
                    }
                    catch
                    {
                        await Shell.Current.DisplayAlertAsync(
                            "Advertencia",
                            "No se pudo cancelar el turno automáticamente.\n\n" +
                            "Por favor, contacte a la farmacia.",
                            "Entendido");
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Turno Mantenido",
                        "El turno quedó creado sin documentos.\n\n" +
                        "Puede intentar subir los documentos desde la vista de turnos.",
                        "Entendido");
                }
            }
            else if (docFallidos > 0)
            {
                // Algunos documentos fallaron
                await ShowSuccessAsync(
                    $"¡Turno solicitado!\n\n" +
                    $"Se adjuntaron {docSubidos} de {DocumentosAdjuntos.Count} documentos.\n" +
                    $"({docFallidos} documento(s) no se pudieron subir)\n\n" +
                    "Recibirás una notificación cuando sea revisado.");
            }
            else
            {
                await ShowSuccessAsync(
                    $"¡Turno solicitado exitosamente!\n\n" +
                    (DocumentosAdjuntos.Count > 0 
                        ? $"Se adjuntaron {docSubidos} documento(s).\n\n" 
                        : "") +
                    "Recibirás una notificación cuando sea revisado.");
            }

            // Navegar de vuelta
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (ItemsSeleccionados.Count > 0 || DocumentosAdjuntos.Count > 0)
        {
            var confirm = await ShowConfirmAsync(
                "Cancelar Solicitud",
                "¿Está seguro? Se perderán los datos ingresados.");

            if (!confirm) return;
        }

        await Shell.Current.GoToAsync("..");
    }

    #endregion

    #region Helpers

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    #endregion
}

#region Models

/// <summary>
/// Item de búsqueda (medicamento o insumo)
/// </summary>
public class ItemBusqueda
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;

    public string StockDisplay => $"Stock: {Stock} {Unit}";
    public Color StockColor => Stock > 10 ? Colors.Green : (Stock > 5 ? Colors.Orange : Colors.Red);
}

/// <summary>
/// Item seleccionado para el turno
/// </summary>
public class ItemSeleccionado
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 0;

    public string Display => $"{Name} ({Stock} {Unit} disponibles)";
}

/// <summary>
/// Documento adjunto al turno
/// </summary>
public class DocumentoTurno
{
    public string TempId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileSizeDisplay { get; set; } = string.Empty;

    public string Icon => ContentType.Contains("pdf") ? "📄" : "🖼️";
    public string Display => $"{Icon} {DocumentType}: {FileName}";
}

/// <summary>
/// Request para crear turno desde móvil
/// </summary>
public class CrearTurnoMobileRequest
{
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public string TipoSolicitud { get; set; } = "Medicamento";
    public string? Notas { get; set; }
    public List<TurnoItemRequest> Items { get; set; } = new();
}

#endregion
