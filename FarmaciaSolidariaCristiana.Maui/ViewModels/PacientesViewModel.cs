using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.Views;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class PacientesViewModel : BaseViewModel
{
    // Constantes de límite de tamaño de archivos
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;  // 5MB para imágenes
    private const int MaxPdfSizeBytes = 3 * 1024 * 1024;    // 3MB para PDFs (más restrictivo por red)
    
    private readonly IImageCompressionService _imageCompressionService;
    
    [ObservableProperty]
    private ObservableCollection<Patient> pacientes = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool isRefreshing;

    private List<Patient> _allPacientes = new();

    public PacientesViewModel(IApiService apiService, IAuthService authService, IImageCompressionService imageCompressionService)
        : base(authService, apiService)
    {
        _imageCompressionService = imageCompressionService;
        Title = "Pacientes";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        CanEdit = userInfo?.Role == Constants.RoleAdmin || 
                  userInfo?.Role == Constants.RoleFarmaceutico;
        await LoadPacientesAsync();
    }

    [RelayCommand]
    private async Task LoadPacientesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetPacientesAsync();
            if (response.Success && response.Data != null)
            {
                _allPacientes = response.Data;
                ApplyFilter();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar pacientes");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadPacientesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Pacientes = new ObservableCollection<Patient>(_allPacientes);
        }
        else
        {
            var filtered = _allPacientes
                .Where(p => p.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           (p.IdentificationDocument?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (p.Phone?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            Pacientes = new ObservableCollection<Patient>(filtered);
        }
    }

    [RelayCommand]
    private async Task SelectPacienteAsync(Patient paciente)
    {
        if (paciente == null) return;
        
        var options = CanEdit 
            ? new[] { "Ver detalles", "Editar ficha", "Ver documentos médicos", "Eliminar" } 
            : new[] { "Ver detalles", "Ver documentos médicos" };
        
        var action = await Shell.Current.DisplayActionSheetAsync(
            paciente.FullName,
            "Cancelar",
            null,
            options);

        switch (action)
        {
            case "Ver detalles":
                await ViewDetailsAsync(paciente);
                break;
            case "Editar ficha":
                await EditPacienteAsync(paciente);
                break;
            case "Ver documentos médicos":
                await VerDocumentosMedicosAsync(paciente);
                break;
            case "Eliminar":
                await DeletePacienteAsync(paciente);
                break;
        }
    }

    [RelayCommand]
    private async Task AddPacienteAsync()
    {
        if (!CanEdit)
        {
            await ShowErrorAsync("No tiene permisos para agregar pacientes.");
            return;
        }

        var nuevoPaciente = new Patient { IsActive = true };
        
        // 1. DATOS PERSONALES OBLIGATORIOS
        
        // Carnet de Identidad
        var cedula = await Shell.Current.DisplayPromptAsync(
            "1. Datos del Paciente",
            "Carnet de Identidad o Pasaporte:",
            maxLength: 20,
            keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(cedula)) return;
        nuevoPaciente.IdentificationDocument = cedula;

        // Nombre completo
        var nombre = await Shell.Current.DisplayPromptAsync(
            "1. Datos del Paciente",
            "Nombre completo:",
            maxLength: 200);
        if (string.IsNullOrWhiteSpace(nombre)) return;
        nuevoPaciente.FullName = nombre;

        // Edad
        var edadStr = await Shell.Current.DisplayPromptAsync(
            "1. Datos del Paciente",
            "Edad:",
            keyboard: Keyboard.Numeric);
        if (string.IsNullOrEmpty(edadStr) || !int.TryParse(edadStr, out var edad)) return;
        nuevoPaciente.Age = edad;

        // Género
        var sexo = await Shell.Current.DisplayActionSheetAsync(
            "Género del paciente",
            "Cancelar",
            null,
            "Masculino", "Femenino");
        if (sexo == "Cancelar" || sexo == null) return;
        nuevoPaciente.Gender = sexo == "Masculino" ? "M" : "F";

        // 2. DATOS DE CONTACTO (opcionales)
        
        var telefono = await Shell.Current.DisplayPromptAsync(
            "1. Datos del Paciente",
            "Teléfono/Contacto (opcional):",
            maxLength: 50);
        if (telefono == null) return;
        nuevoPaciente.Phone = string.IsNullOrWhiteSpace(telefono) ? null : telefono;

        var direccion = await Shell.Current.DisplayPromptAsync(
            "1. Datos del Paciente",
            "Dirección (opcional):",
            maxLength: 500);
        if (direccion == null) return;
        nuevoPaciente.Address = string.IsNullOrWhiteSpace(direccion) ? null : direccion;

        var municipio = await Shell.Current.DisplayPromptAsync(
            "1. Datos del Paciente",
            "Municipio (opcional):",
            maxLength: 100);
        if (municipio == null) return;
        nuevoPaciente.Municipality = string.IsNullOrWhiteSpace(municipio) ? null : municipio;

        var provincia = await Shell.Current.DisplayPromptAsync(
            "1. Datos del Paciente",
            "Provincia (opcional):",
            maxLength: 100);
        if (provincia == null) return;
        nuevoPaciente.Province = string.IsNullOrWhiteSpace(provincia) ? null : provincia;

        // 3. DATOS CLÍNICOS
        
        var diagnostico = await Shell.Current.DisplayPromptAsync(
            "2. Datos Clínicos",
            "Diagnóstico principal (opcional):",
            maxLength: 500);
        if (diagnostico == null) return;
        nuevoPaciente.MainDiagnosis = string.IsNullOrWhiteSpace(diagnostico) ? null : diagnostico;

        var patologias = await Shell.Current.DisplayPromptAsync(
            "2. Datos Clínicos",
            "Patologías asociadas (opcional):",
            maxLength: 1000);
        if (patologias == null) return;
        nuevoPaciente.AssociatedPathologies = string.IsNullOrWhiteSpace(patologias) ? null : patologias;

        var alergias = await Shell.Current.DisplayPromptAsync(
            "2. Datos Clínicos",
            "Alergias conocidas (opcional):",
            maxLength: 500);
        if (alergias == null) return;
        nuevoPaciente.KnownAllergies = string.IsNullOrWhiteSpace(alergias) ? null : alergias;

        var tratamientos = await Shell.Current.DisplayPromptAsync(
            "2. Datos Clínicos",
            "Tratamientos actuales (opcional):",
            maxLength: 1000);
        if (tratamientos == null) return;
        nuevoPaciente.CurrentTreatments = string.IsNullOrWhiteSpace(tratamientos) ? null : tratamientos;

        // 4. DATOS VITALES (opcional - preguntamos si quiere registrarlos)
        var registrarVitales = await Shell.Current.DisplayAlertAsync(
            "3. Datos Vitales",
            "¿Desea registrar datos vitales (peso, altura, presión arterial)?",
            "Sí", "No");

        if (registrarVitales)
        {
            var pesoStr = await Shell.Current.DisplayPromptAsync(
                "3. Datos Vitales",
                "Peso en kg (ej: 70.5):",
                keyboard: Keyboard.Numeric);
            if (pesoStr != null && decimal.TryParse(pesoStr, out var peso))
                nuevoPaciente.Weight = peso;

            var alturaStr = await Shell.Current.DisplayPromptAsync(
                "3. Datos Vitales",
                "Altura en cm (ej: 170):",
                keyboard: Keyboard.Numeric);
            if (alturaStr != null && decimal.TryParse(alturaStr, out var altura))
                nuevoPaciente.Height = altura;

            var sistolicaStr = await Shell.Current.DisplayPromptAsync(
                "3. Datos Vitales",
                "Presión arterial sistólica (ej: 120):",
                keyboard: Keyboard.Numeric);
            if (sistolicaStr != null && int.TryParse(sistolicaStr, out var sistolica))
                nuevoPaciente.BloodPressureSystolic = sistolica;

            var diastolicaStr = await Shell.Current.DisplayPromptAsync(
                "3. Datos Vitales",
                "Presión arterial diastólica (ej: 80):",
                keyboard: Keyboard.Numeric);
            if (diastolicaStr != null && int.TryParse(diastolicaStr, out var diastolica))
                nuevoPaciente.BloodPressureDiastolic = diastolica;
        }

        // 5. OBSERVACIONES
        var observaciones = await Shell.Current.DisplayPromptAsync(
            "Observaciones",
            "Notas adicionales (opcional):",
            maxLength: 2000);
        if (observaciones == null) return;
        nuevoPaciente.Observations = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones;

        // CREAR PACIENTE
        try
        {
            IsBusy = true;
            var result = await ApiService.CrearPacienteAsync(nuevoPaciente);

            if (result.Success && result.Data != null)
            {
                await Shell.Current.DisplayAlertAsync("Éxito", "Paciente creado correctamente", "OK");
                
                // Buscar documentos de turnos para este paciente
                await OfrecerImportarDocumentosTurnoAsync(result.Data);
                
                // Preguntar si desea agregar documentos adicionales
                var agregarDocs = await Shell.Current.DisplayAlertAsync(
                    "Documentos Médicos",
                    "¿Desea agregar más documentos médicos?",
                    "Sí", "No");

                if (agregarDocs)
                {
                    await AgregarDocumentoAsync(result.Data);
                }

                // Recargar lista
                var reloadResult = await ApiService.GetPacientesAsync();
                if (reloadResult.Success && reloadResult.Data != null)
                {
                    _allPacientes = reloadResult.Data;
                    ApplyFilter();
                }
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al crear paciente");
            }
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
    private async Task ViewDetailsAsync(Patient paciente)
    {
        if (paciente == null) return;
        
        var details = $"═══ DATOS PERSONALES ═══\n" +
                     $"Nombre: {paciente.FullName}\n" +
                     $"Carnet/Pasaporte: {paciente.IdentificationDocument}\n" +
                     $"Edad: {paciente.Age} años\n" +
                     $"Género: {(paciente.Gender == "M" ? "Masculino" : "Femenino")}\n" +
                     $"Teléfono: {paciente.Phone ?? "N/A"}\n" +
                     $"Dirección: {paciente.Address ?? "N/A"}\n" +
                     $"Municipio: {paciente.Municipality ?? "N/A"}\n" +
                     $"Provincia: {paciente.Province ?? "N/A"}\n\n" +
                     $"═══ DATOS CLÍNICOS ═══\n" +
                     $"Diagnóstico: {paciente.MainDiagnosis ?? "N/A"}\n" +
                     $"Patologías: {paciente.AssociatedPathologies ?? "N/A"}\n" +
                     $"Alergias: {paciente.KnownAllergies ?? "N/A"}\n" +
                     $"Tratamientos: {paciente.CurrentTreatments ?? "N/A"}\n\n" +
                     $"═══ DATOS VITALES ═══\n" +
                     $"Peso: {(paciente.Weight.HasValue ? $"{paciente.Weight} kg" : "N/A")}\n" +
                     $"Altura: {(paciente.Height.HasValue ? $"{paciente.Height} cm" : "N/A")}\n" +
                     $"Presión arterial: {paciente.BloodPressureDisplay}\n\n" +
                     $"═══ OTROS ═══\n" +
                     $"Observaciones: {paciente.Observations ?? "N/A"}\n" +
                     $"Entregas realizadas: {paciente.DeliveriesCount}\n" +
                     $"Estado: {(paciente.IsActive ? "Activo" : "Inactivo")}";
                     
        await Shell.Current.DisplayAlertAsync("Ficha del Paciente", details, "Cerrar");
    }

    [RelayCommand]
    private async Task EditPacienteAsync(Patient paciente)
    {
        if (!CanEdit || paciente == null)
        {
            await ShowErrorAsync("No tiene permisos para editar pacientes.");
            return;
        }

        // Elegir qué sección editar
        var seccion = await Shell.Current.DisplayActionSheetAsync(
            "¿Qué datos desea editar?",
            "Cancelar",
            null,
            "Datos personales",
            "Datos de contacto",
            "Datos clínicos",
            "Datos vitales",
            "Observaciones",
            "📎 Agregar documento",
            "📥 Importar de turnos");

        if (seccion == "Cancelar" || seccion == null) return;

        // Acciones especiales que no requieren guardar cambios del paciente
        if (seccion == "📎 Agregar documento")
        {
            await AgregarDocumentoAsync(paciente);
            return;
        }
        
        if (seccion == "📥 Importar de turnos")
        {
            await OfrecerImportarDocumentosTurnoAsync(paciente);
            return;
        }

        bool cambiosRealizados = false;

        switch (seccion)
        {
            case "Datos personales":
                cambiosRealizados = await EditarDatosPersonalesAsync(paciente);
                break;
            case "Datos de contacto":
                cambiosRealizados = await EditarDatosContactoAsync(paciente);
                break;
            case "Datos clínicos":
                cambiosRealizados = await EditarDatosClinicosAsync(paciente);
                break;
            case "Datos vitales":
                cambiosRealizados = await EditarDatosVitalesAsync(paciente);
                break;
            case "Observaciones":
                cambiosRealizados = await EditarObservacionesAsync(paciente);
                break;
        }

        if (cambiosRealizados)
        {
            try
            {
                IsBusy = true;
                var result = await ApiService.ActualizarPacienteAsync(paciente);

                if (result.Success)
                {
                    await Shell.Current.DisplayAlertAsync("Éxito", "Paciente actualizado correctamente", "OK");
                    
                    // Recargar lista
                    var reloadResult = await ApiService.GetPacientesAsync();
                    if (reloadResult.Success && reloadResult.Data != null)
                    {
                        _allPacientes = reloadResult.Data;
                        ApplyFilter();
                    }
                }
                else
                {
                    await ShowErrorAsync(result.Message ?? "Error al actualizar paciente");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task<bool> EditarDatosPersonalesAsync(Patient paciente)
    {
        var nuevoNombre = await Shell.Current.DisplayPromptAsync(
            "Datos Personales",
            "Nombre completo:",
            initialValue: paciente.FullName,
            maxLength: 200);
        if (string.IsNullOrEmpty(nuevoNombre)) return false;

        var edadStr = await Shell.Current.DisplayPromptAsync(
            "Datos Personales",
            "Edad:",
            initialValue: paciente.Age.ToString(),
            keyboard: Keyboard.Numeric);
        if (string.IsNullOrEmpty(edadStr) || !int.TryParse(edadStr, out var edad)) return false;

        paciente.FullName = nuevoNombre;
        paciente.Age = edad;
        return true;
    }

    private async Task<bool> EditarDatosContactoAsync(Patient paciente)
    {
        var telefono = await Shell.Current.DisplayPromptAsync(
            "Datos de Contacto",
            "Teléfono:",
            initialValue: paciente.Phone ?? "",
            maxLength: 50);
        if (telefono == null) return false;

        var direccion = await Shell.Current.DisplayPromptAsync(
            "Datos de Contacto",
            "Dirección:",
            initialValue: paciente.Address ?? "",
            maxLength: 500);
        if (direccion == null) return false;

        var municipio = await Shell.Current.DisplayPromptAsync(
            "Datos de Contacto",
            "Municipio:",
            initialValue: paciente.Municipality ?? "",
            maxLength: 100);
        if (municipio == null) return false;

        var provincia = await Shell.Current.DisplayPromptAsync(
            "Datos de Contacto",
            "Provincia:",
            initialValue: paciente.Province ?? "",
            maxLength: 100);
        if (provincia == null) return false;

        paciente.Phone = string.IsNullOrWhiteSpace(telefono) ? null : telefono;
        paciente.Address = string.IsNullOrWhiteSpace(direccion) ? null : direccion;
        paciente.Municipality = string.IsNullOrWhiteSpace(municipio) ? null : municipio;
        paciente.Province = string.IsNullOrWhiteSpace(provincia) ? null : provincia;
        return true;
    }

    private async Task<bool> EditarDatosClinicosAsync(Patient paciente)
    {
        var diagnostico = await Shell.Current.DisplayPromptAsync(
            "Datos Clínicos",
            "Diagnóstico principal:",
            initialValue: paciente.MainDiagnosis ?? "",
            maxLength: 500);
        if (diagnostico == null) return false;

        var patologias = await Shell.Current.DisplayPromptAsync(
            "Datos Clínicos",
            "Patologías asociadas:",
            initialValue: paciente.AssociatedPathologies ?? "",
            maxLength: 1000);
        if (patologias == null) return false;

        var alergias = await Shell.Current.DisplayPromptAsync(
            "Datos Clínicos",
            "Alergias conocidas:",
            initialValue: paciente.KnownAllergies ?? "",
            maxLength: 500);
        if (alergias == null) return false;

        var tratamientos = await Shell.Current.DisplayPromptAsync(
            "Datos Clínicos",
            "Tratamientos actuales:",
            initialValue: paciente.CurrentTreatments ?? "",
            maxLength: 1000);
        if (tratamientos == null) return false;

        paciente.MainDiagnosis = string.IsNullOrWhiteSpace(diagnostico) ? null : diagnostico;
        paciente.AssociatedPathologies = string.IsNullOrWhiteSpace(patologias) ? null : patologias;
        paciente.KnownAllergies = string.IsNullOrWhiteSpace(alergias) ? null : alergias;
        paciente.CurrentTreatments = string.IsNullOrWhiteSpace(tratamientos) ? null : tratamientos;
        return true;
    }

    private async Task<bool> EditarDatosVitalesAsync(Patient paciente)
    {
        var pesoStr = await Shell.Current.DisplayPromptAsync(
            "Datos Vitales",
            "Peso en kg:",
            initialValue: paciente.Weight?.ToString() ?? "",
            keyboard: Keyboard.Numeric);
        if (pesoStr == null) return false;

        var alturaStr = await Shell.Current.DisplayPromptAsync(
            "Datos Vitales",
            "Altura en cm:",
            initialValue: paciente.Height?.ToString() ?? "",
            keyboard: Keyboard.Numeric);
        if (alturaStr == null) return false;

        var sistolicaStr = await Shell.Current.DisplayPromptAsync(
            "Datos Vitales",
            "Presión sistólica:",
            initialValue: paciente.BloodPressureSystolic?.ToString() ?? "",
            keyboard: Keyboard.Numeric);
        if (sistolicaStr == null) return false;

        var diastolicaStr = await Shell.Current.DisplayPromptAsync(
            "Datos Vitales",
            "Presión diastólica:",
            initialValue: paciente.BloodPressureDiastolic?.ToString() ?? "",
            keyboard: Keyboard.Numeric);
        if (diastolicaStr == null) return false;

        paciente.Weight = decimal.TryParse(pesoStr, out var peso) ? peso : null;
        paciente.Height = decimal.TryParse(alturaStr, out var altura) ? altura : null;
        paciente.BloodPressureSystolic = int.TryParse(sistolicaStr, out var sist) ? sist : null;
        paciente.BloodPressureDiastolic = int.TryParse(diastolicaStr, out var diast) ? diast : null;
        return true;
    }

    private async Task<bool> EditarObservacionesAsync(Patient paciente)
    {
        var observaciones = await Shell.Current.DisplayPromptAsync(
            "Observaciones",
            "Notas adicionales:",
            initialValue: paciente.Observations ?? "",
            maxLength: 2000);
        if (observaciones == null) return false;

        paciente.Observations = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones;
        return true;
    }

    private async Task VerDocumentosMedicosAsync(Patient paciente)
    {
        try
        {
            IsBusy = true;
            var response = await ApiService.GetDocumentosPacienteAsync(paciente.Id);
            
            if (response.Success && response.Data != null && response.Data.Count > 0)
            {
                var docs = response.Data;
                // Incluir fecha de subida para distinguir documentos con mismo nombre
                var opciones = docs.Select((d, i) => $"📄 {d.DocumentType}: {d.FileName} ({d.UploadedAt:dd/MM HH:mm})").ToArray();
                
                var seleccion = await Shell.Current.DisplayActionSheetAsync(
                    $"Documentos de {paciente.FullName} ({docs.Count})",
                    "Cerrar",
                    null,
                    opciones);
                
                if (seleccion != "Cerrar" && seleccion != null)
                {
                    var index = Array.IndexOf(opciones, seleccion);
                    if (index >= 0)
                    {
                        var doc = docs[index];
                        await VerDocumentoAsync(doc);
                    }
                }
            }
            else
            {
                var agregarDoc = await Shell.Current.DisplayAlertAsync(
                    "Sin Documentos",
                    $"No hay documentos médicos para {paciente.FullName}.\n\n¿Desea agregar uno?",
                    "Agregar", "Cerrar");

                if (agregarDoc && CanEdit)
                {
                    await AgregarDocumentoAsync(paciente);
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al cargar documentos: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task VerDocumentoAsync(PatientDocument doc)
    {
        if (string.IsNullOrEmpty(doc.FilePath))
        {
            await Shell.Current.DisplayAlertAsync(
                doc.DocumentType,
                $"Archivo: {doc.FileName}\nSubido: {doc.UploadedAt:dd/MM/yyyy}\nNotas: {doc.Notes ?? "Sin notas"}\n\n⚠️ Ruta del documento no disponible",
                "OK");
            return;
        }

        var accion = await Shell.Current.DisplayActionSheetAsync(
            doc.FileName,
            "Cerrar",
            null,
            "📄 Ver en la app",
            "ℹ️ Ver información");

        switch (accion)
        {
            case "📄 Ver en la app":
                await VerDocumentoEnAppAsync(doc);
                break;
            case "ℹ️ Ver información":
                await Shell.Current.DisplayAlertAsync(
                    doc.DocumentType,
                    $"Archivo: {doc.FileName}\nSubido: {doc.UploadedAt:dd/MM/yyyy}\nNotas: {doc.Notes ?? "Sin notas"}",
                    "OK");
                break;
        }
    }

    private async Task VerDocumentoEnAppAsync(PatientDocument doc)
    {
        try
        {
            IsBusy = true;
            
            // Descargar el documento usando la API
            var fileBytes = await ApiService.DownloadPatientDocumentAsync(doc.PatientId, doc.Id);
            
            if (fileBytes == null || fileBytes.Length == 0)
            {
                await ShowErrorAsync("No se pudo descargar el documento");
                return;
            }
            
            // Determinar tipo de documento
            var isPdf = doc.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            var isImage = doc.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                         doc.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                         doc.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                         doc.FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                         doc.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
            
            if (isPdf)
            {
                // Usar PdfViewerPage para PDFs
                var pdfViewer = new PdfViewerPage(fileBytes, doc.FileName);
                await Shell.Current.Navigation.PushAsync(pdfViewer);
            }
            else if (isImage)
            {
                // Usar DocumentoViewerPage con bytes pre-descargados para imágenes
                var docViewer = new DocumentoViewerPage(fileBytes, doc.FileName, doc.DocumentType);
                await Shell.Current.Navigation.PushAsync(docViewer);
            }
            else
            {
                // Tipo desconocido - intentar abrir con el visor de PDF
                var pdfViewer = new PdfViewerPage(fileBytes, doc.FileName);
                await Shell.Current.Navigation.PushAsync(pdfViewer);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al abrir el documento: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Busca documentos de turnos aprobados y ofrece importarlos
    /// </summary>
    private async Task OfrecerImportarDocumentosTurnoAsync(Patient paciente)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Pacientes] Buscando documentos de turnos para: {paciente.IdentificationDocument}");
            
            if (string.IsNullOrWhiteSpace(paciente.IdentificationDocument))
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "El paciente no tiene número de identificación registrado.", "OK");
                return;
            }
            
            IsBusy = true;
            
            // Buscar documentos de turnos para este número de identificación
            var searchResult = await ApiService.GetTurnoDocumentsByIdentificationAsync(paciente.IdentificationDocument);
            
            System.Diagnostics.Debug.WriteLine($"[Pacientes] Resultado búsqueda: Success={searchResult.Success}, Data={searchResult.Data != null}, Message={searchResult.Message}");
            
            if (!searchResult.Success)
            {
                await Shell.Current.DisplayAlertAsync("Error", searchResult.Message ?? "Error al buscar documentos de turnos", "OK");
                return;
            }
            
            if (searchResult.Data == null || !searchResult.Data.Found || searchResult.Data.Documents.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Sin Documentos", 
                    "No se encontraron documentos en turnos aprobados para este número de identificación.", "OK");
                return;
            }
            
            var docs = searchResult.Data.Documents;
            System.Diagnostics.Debug.WriteLine($"[Pacientes] Encontrados {docs.Count} documentos");
            
            var importar = await Shell.Current.DisplayAlertAsync(
                "📋 Documentos de Turnos Encontrados",
                $"Se encontraron {docs.Count} documento(s) en turnos aprobados para este paciente.\n\n¿Desea importarlos a la ficha del paciente?",
                "Sí, importar",
                "No, gracias");
            
            if (!importar) return;
            
            // Mostrar lista para seleccionar
            var opciones = docs.Select(d => $"📄 {d.DocumentType} - Turno #{d.NumeroTurno} ({d.FechaSolicitud:dd/MM/yy})").ToArray();
            var todosSeleccionados = new List<TurnoDocumentItem>(docs);
            
            var seleccionarTodos = await Shell.Current.DisplayAlertAsync(
                "Seleccionar Documentos",
                $"¿Importar todos los {docs.Count} documentos?",
                "Sí, todos",
                "Seleccionar manualmente");
            
            if (!seleccionarTodos)
            {
                // Selección manual
                todosSeleccionados.Clear();
                foreach (var doc in docs)
                {
                    var incluir = await Shell.Current.DisplayAlertAsync(
                        "Importar Documento",
                        $"📄 {doc.DocumentType}\n📁 {doc.FileName}\n📅 Turno #{doc.NumeroTurno} ({doc.FechaSolicitud:dd/MM/yyyy})\n\n¿Incluir este documento?",
                        "Sí", "No");
                    if (incluir)
                    {
                        todosSeleccionados.Add(doc);
                    }
                }
            }
            
            if (todosSeleccionados.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Info", "No se seleccionaron documentos para importar.", "OK");
                return;
            }
            
            // Importar los documentos seleccionados
            IsBusy = true;
            
            var itemsToImport = todosSeleccionados.Select(d => new TurnoDocumentImportItem
            {
                TurnoId = d.TurnoId,
                NumeroTurno = d.NumeroTurno,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                FilePath = d.FilePath,
                FechaSolicitud = d.FechaSolicitud
            }).ToList();
            
            var importResult = await ApiService.ImportTurnoDocumentsAsync(paciente.Id, itemsToImport);
            
            if (importResult.Success && importResult.Data != null)
            {
                var msg = importResult.Data.ImportedCount > 0
                    ? $"✅ Se importaron {importResult.Data.ImportedCount} documento(s) correctamente."
                    : "No se pudo importar ningún documento.";
                
                if (importResult.Data.Errors.Count > 0)
                {
                    msg += $"\n\n⚠️ Errores:\n{string.Join("\n", importResult.Data.Errors)}";
                }
                
                await Shell.Current.DisplayAlertAsync("Importación Completada", msg, "OK");
            }
            else
            {
                await ShowErrorAsync(importResult.Message ?? "Error al importar documentos");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Pacientes] Error al buscar documentos de turnos: {ex.Message}");
            // No mostramos error al usuario porque no es crítico
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AgregarDocumentoAsync(Patient paciente)
    {
        if (!CanEdit)
        {
            await ShowErrorAsync("No tiene permisos para agregar documentos.");
            return;;
        }

        var opcion = await Shell.Current.DisplayActionSheetAsync(
            "Agregar documento médico",
            "Cancelar",
            null,
            "📷 Tomar foto",
            "🖼️ Seleccionar de galería",
            "📁 Seleccionar archivo PDF");

        if (opcion == "Cancelar" || opcion == null) return;

        FileResult? archivo = null;

        try
        {
            switch (opcion)
            {
                case "📷 Tomar foto":
                    if (!MediaPicker.Default.IsCaptureSupported)
                    {
                        await ShowErrorAsync("La cámara no está disponible en este dispositivo.");
                        return;
                    }
                    archivo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                    {
                        Title = "Tomar foto del documento"
                    });
                    break;

                case "🖼️ Seleccionar de galería":
                    var photoResults = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
                    {
                        Title = "Seleccionar imagen"
                    });
                    archivo = photoResults?.FirstOrDefault();
                    break;

                case "📁 Seleccionar archivo PDF":
                    var pdfResult = await FilePicker.Default.PickAsync(new PickOptions
                    {
                        PickerTitle = "Seleccionar PDF",
                        FileTypes = FilePickerFileType.Pdf
                    });
                    // FilePickerResult ya es un FileResult - usarlo directamente
                    // para que OpenReadAsync() funcione en Android con URIs content://
                    archivo = pdfResult;
                    break;
            }

            if (archivo == null) return;

            // Leer archivo y validar tamaño antes de continuar
            using var stream = await archivo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            
            // Validar tamaño según tipo de archivo
            bool isPdf = archivo.FileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true ||
                        archivo.ContentType == "application/pdf";
            
            if (isPdf)
            {
                if (fileBytes.Length > MaxPdfSizeBytes)
                {
                    await ShowErrorAsync(
                        $"El PDF es demasiado grande ({FormatFileSize(fileBytes.Length)}).\n" +
                        $"El tamaño máximo permitido es {FormatFileSize(MaxPdfSizeBytes)}.\n\n" +
                        "Sugerencia: Use un PDF más pequeño o tome una foto del documento.");
                    return;
                }
            }
            else
            {
                if (fileBytes.Length > MaxImageSizeBytes)
                {
                    await ShowErrorAsync(
                        $"La imagen es demasiado grande ({FormatFileSize(fileBytes.Length)}).\n" +
                        $"El tamaño máximo permitido es {FormatFileSize(MaxImageSizeBytes)}.");
                    return;
                }
            }

            // Tipo de documento
            var tipoDoc = await Shell.Current.DisplayActionSheetAsync(
                "Tipo de documento",
                "Cancelar",
                null,
                "Receta médica",
                "Resultado de laboratorio",
                "Informe médico",
                "Certificado médico",
                "Otro");

            if (tipoDoc == "Cancelar" || tipoDoc == null) return;

            // Notas opcionales
            var notas = await Shell.Current.DisplayPromptAsync(
                "Notas",
                "Descripción o notas (opcional):",
                maxLength: 500);
            if (notas == null) return;

            // Subir documento
            IsBusy = true;

            // Comprimir si es imagen (detectar por ContentType o extensión de archivo)
            bool isImage = archivo.ContentType?.StartsWith("image/") == true ||
                          (!isPdf && (archivo.FileName?.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) == true ||
                                     archivo.FileName?.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) == true ||
                                     archivo.FileName?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == true ||
                                     archivo.FileName?.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) == true));
            
            // Nombre del archivo a enviar
            var fileName = archivo.FileName ?? "documento";
            
            if (isImage && fileBytes.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Pacientes] Comprimiendo imagen: {fileBytes.Length} bytes");
                fileBytes = await ComprimirImagenAsync(fileBytes);
                System.Diagnostics.Debug.WriteLine($"[Pacientes] Después de compresión: {fileBytes.Length} bytes");
                
                // Después de comprimir, la imagen siempre es JPEG
                fileName = Path.ChangeExtension(fileName, ".jpg");
            }

            // Validar que tenemos datos antes de enviar
            if (fileBytes == null || fileBytes.Length == 0)
            {
                await ShowErrorAsync("Error: No se pudieron leer los datos del archivo.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Pacientes] Subiendo documento: {fileName}, {fileBytes.Length} bytes");

            var result = await ApiService.SubirDocumentoPacienteAsync(
                paciente.Id,
                fileName,
                tipoDoc,
                fileBytes,
                notas);

            if (result.Success)
            {
                await Shell.Current.DisplayAlertAsync("Éxito", "Documento subido correctamente", "OK");
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al subir el documento");
            }
        }
        catch (PermissionException)
        {
            await ShowErrorAsync("Se requieren permisos para acceder a la cámara o galería.");
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

    private async Task<byte[]> ComprimirImagenAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            System.Diagnostics.Debug.WriteLine("[Pacientes] ComprimirImagenAsync: bytes de entrada vacíos");
            return imageBytes ?? Array.Empty<byte>();
        }
        
        try
        {
            // Usar el servicio de compresión de imágenes inyectado
            using var stream = new MemoryStream(imageBytes);
            var compressed = await _imageCompressionService.CompressImageAsync(stream, "image/jpeg", 1920, 1080, 80);
            
            // Verificar que la compresión no devolvió bytes vacíos
            if (compressed == null || compressed.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Pacientes] Compresión devolvió vacío, usando original");
                return imageBytes;
            }
            
            System.Diagnostics.Debug.WriteLine($"[Pacientes] Imagen comprimida: {imageBytes.Length / 1024}KB -> {compressed.Length / 1024}KB");
            return compressed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Pacientes] Error comprimiendo: {ex.Message}");
            return imageBytes;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    [RelayCommand]
    private async Task DeletePacienteAsync(Patient paciente)
    {
        if (!CanEdit || paciente == null) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Eliminar Paciente",
            $"¿Estás seguro de eliminar a '{paciente.FullName}'?\n\nEsta acción no se puede deshacer.",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await ApiService.DeletePacienteAsync(paciente.Id);
                
                if (response.Success)
                {
                    _allPacientes.Remove(paciente);
                    ApplyFilter();
                    await Shell.Current.DisplayAlertAsync("Éxito", "Paciente eliminado correctamente", "OK");
                }
                else
                {
                    await ShowErrorAsync(response.Message ?? "Error al eliminar");
                }
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
    }
}
