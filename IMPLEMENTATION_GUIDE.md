# Farmacia Solidaria Cristiana - Instrucciones de Implementación

## Proyecto Creado Exitosamente ✓

Este documento contiene las instrucciones para completar la implementación de las vistas restantes y ejecutar el proyecto.

## Estado Actual

### ✓ Completado:
- Proyecto ASP.NET Core 8 MVC creado
- Paquetes NuGet instalados (EF Core, Identity, iText7)
- Modelos creados (Medicine, Delivery, Donation)
- ApplicationDbContext configurado con Identity
- Program.cs configurado (sin HTTPS, con Identity y EF Core)
- appsettings.json con connection string
- DbInitializer para seed de roles y usuario admin
- Controllers: Account, Medicines, Deliveries, Donations, Reports
- Vistas principales creadas
- Layout actualizado con navegación en español

### Credenciales de Admin:
- **Usuario**: admin
- **Contraseña**: Admin123!
- **Email**: admin@example.com
- **Rol**: Admin

## Vistas Pendientes de Crear

### 1. Views/Deliveries/Index.cshtml
```cshtml
@model IEnumerable<FarmaciaSolidariaCristiana.Models.Delivery>
@{
    ViewData["Title"] = "Entregas";
}

<div class="container mt-4">
    <div class="card">
        <div class="card-header bg-success text-white d-flex justify-content-between align-items-center">
            <h4 class="mb-0">@ViewData["Title"]</h4>
            @if (User.IsInRole("Farmaceutico"))
            {
                <a asp-action="Create" class="btn btn-light">
                    <i class="bi bi-plus-circle"></i> Nueva Entrega
                </a>
            }
        </div>
        <div class="card-body">
            @if (TempData["SuccessMessage"] != null)
            {
                <div class="alert alert-success alert-dismissible fade show">
                    @TempData["SuccessMessage"]
                    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                </div>
            }
            
            <form asp-action="Index" method="get" class="row mb-3">
                <div class="col-md-4">
                    <input type="text" name="searchString" class="form-control" placeholder="Buscar medicamento..." />
                </div>
                <div class="col-md-3">
                    <input type="date" name="startDate" class="form-control" />
                </div>
                <div class="col-md-3">
                    <input type="date" name="endDate" class="form-control" />
                </div>
                <div class="col-md-2">
                    <button class="btn btn-primary w-100" type="submit">Filtrar</button>
                </div>
            </form>
            
            <div class="alert alert-info">
                <strong>Total Entregas:</strong> @ViewData["TotalDeliveries"]
            </div>
            
            <div class="table-responsive">
                <table class="table table-striped">
                    <thead class="table-dark">
                        <tr>
                            <th>Medicamento</th>
                            <th>Cantidad</th>
                            <th>Fecha</th>
                            <th>Nota Paciente</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var item in Model)
                        {
                            <tr>
                                <td>@item.Medicine?.Name</td>
                                <td>@item.Quantity @item.Medicine?.Unit</td>
                                <td>@item.DeliveryDate.ToString("dd/MM/yyyy")</td>
                                <td>@item.PatientNote</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>
```

### 2. Views/Deliveries/Create.cshtml
```cshtml
@model FarmaciaSolidariaCristiana.Models.Delivery
@{
    ViewData["Title"] = "Nueva Entrega";
}

<div class="container mt-4">
    <div class="card">
        <div class="card-header bg-success text-white">
            <h4 class="mb-0">@ViewData["Title"]</h4>
        </div>
        <div class="card-body">
            <form asp-action="Create">
                <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>
                
                <div class="form-group mb-3">
                    <label asp-for="MedicineId" class="form-label"></label>
                    <select asp-for="MedicineId" class="form-select" asp-items="ViewBag.MedicineId">
                        <option value="">-- Seleccione un Medicamento --</option>
                    </select>
                    <span asp-validation-for="MedicineId" class="text-danger"></span>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="Quantity" class="form-label"></label>
                    <input asp-for="Quantity" class="form-control" type="number" min="1" />
                    <span asp-validation-for="Quantity" class="text-danger"></span>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="DeliveryDate" class="form-label"></label>
                    <input asp-for="DeliveryDate" class="form-control" type="date" />
                    <span asp-validation-for="DeliveryDate" class="text-danger"></span>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="PatientNote" class="form-label"></label>
                    <textarea asp-for="PatientNote" class="form-control" rows="3" placeholder="Detalles de receta médica, justificante, etc."></textarea>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="Comments" class="form-label"></label>
                    <textarea asp-for="Comments" class="form-control" rows="2"></textarea>
                </div>
                
                <div class="d-grid gap-2">
                    <button type="submit" class="btn btn-success">Registrar Entrega</button>
                    <a asp-action="Index" class="btn btn-secondary">Cancelar</a>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

### 3. Views/Donations/Index.cshtml
```cshtml
@model IEnumerable<FarmaciaSolidariaCristiana.Models.Donation>
@{
    ViewData["Title"] = "Donaciones";
}

<div class="container mt-4">
    <div class="card">
        <div class="card-header bg-warning d-flex justify-content-between align-items-center">
            <h4 class="mb-0">@ViewData["Title"]</h4>
            @if (User.IsInRole("Farmaceutico"))
            {
                <a asp-action="Create" class="btn btn-dark">
                    <i class="bi bi-plus-circle"></i> Nueva Donación
                </a>
            }
        </div>
        <div class="card-body">
            @if (TempData["SuccessMessage"] != null)
            {
                <div class="alert alert-success alert-dismissible fade show">
                    @TempData["SuccessMessage"]
                    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                </div>
            }
            
            <form asp-action="Index" method="get" class="row mb-3">
                <div class="col-md-4">
                    <input type="text" name="searchString" class="form-control" placeholder="Buscar medicamento..." />
                </div>
                <div class="col-md-3">
                    <input type="date" name="startDate" class="form-control" />
                </div>
                <div class="col-md-3">
                    <input type="date" name="endDate" class="form-control" />
                </div>
                <div class="col-md-2">
                    <button class="btn btn-primary w-100" type="submit">Filtrar</button>
                </div>
            </form>
            
            <div class="alert alert-info">
                <strong>Total Donaciones:</strong> @ViewData["TotalDonations"]
            </div>
            
            <div class="table-responsive">
                <table class="table table-striped">
                    <thead class="table-dark">
                        <tr>
                            <th>Medicamento</th>
                            <th>Cantidad</th>
                            <th>Fecha</th>
                            <th>Nota Donante</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var item in Model)
                        {
                            <tr>
                                <td>@item.Medicine?.Name</td>
                                <td>@item.Quantity @item.Medicine?.Unit</td>
                                <td>@item.DonationDate.ToString("dd/MM/yyyy")</td>
                                <td>@item.DonorNote</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>
```

### 4. Views/Donations/Create.cshtml
```cshtml
@model FarmaciaSolidariaCristiana.Models.Donation
@{
    ViewData["Title"] = "Nueva Donación";
}

<div class="container mt-4">
    <div class="card">
        <div class="card-header bg-warning">
            <h4 class="mb-0">@ViewData["Title"]</h4>
        </div>
        <div class="card-body">
            <form asp-action="Create">
                <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>
                
                <div class="form-group mb-3">
                    <label asp-for="MedicineId" class="form-label"></label>
                    <select asp-for="MedicineId" class="form-select" asp-items="ViewBag.MedicineId">
                        <option value="">-- Seleccione un Medicamento --</option>
                    </select>
                    <span asp-validation-for="MedicineId" class="text-danger"></span>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="Quantity" class="form-label"></label>
                    <input asp-for="Quantity" class="form-control" type="number" min="1" />
                    <span asp-validation-for="Quantity" class="text-danger"></span>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="DonationDate" class="form-label"></label>
                    <input asp-for="DonationDate" class="form-control" type="date" />
                    <span asp-validation-for="DonationDate" class="text-danger"></span>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="DonorNote" class="form-label"></label>
                    <textarea asp-for="DonorNote" class="form-control" rows="3" placeholder="Información del donante (opcional)"></textarea>
                </div>
                
                <div class="form-group mb-3">
                    <label asp-for="Comments" class="form-label"></label>
                    <textarea asp-for="Comments" class="form-control" rows="2"></textarea>
                </div>
                
                <div class="d-grid gap-2">
                    <button type="submit" class="btn btn-warning">Registrar Donación</button>
                    <a asp-action="Index" class="btn btn-secondary">Cancelar</a>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

### 5. Views/Reports/Index.cshtml
```cshtml
@{
    ViewData["Title"] = "Reportes PDF";
}

<div class="container mt-4">
    <h2 class="mb-4"><i class="bi bi-file-pdf text-danger"></i> Generar Reportes PDF</h2>
    
    <div class="row">
        <div class="col-md-6 mb-4">
            <div class="card">
                <div class="card-header bg-success text-white">
                    <h5 class="mb-0">Reporte de Entregas</h5>
                </div>
                <div class="card-body">
                    <form asp-action="DeliveriesPDF" method="post">
                        <div class="mb-3">
                            <label class="form-label">Medicamento (Opcional)</label>
                            <select name="medicineId" class="form-select" asp-items="ViewBag.MedicineId">
                                <option value="">-- Todos --</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Fecha Inicio</label>
                            <input type="date" name="startDate" class="form-control" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Fecha Fin</label>
                            <input type="date" name="endDate" class="form-control" />
                        </div>
                        <button type="submit" class="btn btn-success w-100">
                            <i class="bi bi-download"></i> Descargar PDF
                        </button>
                    </form>
                </div>
            </div>
        </div>
        
        <div class="col-md-6 mb-4">
            <div class="card">
                <div class="card-header bg-warning">
                    <h5 class="mb-0">Reporte de Donaciones</h5>
                </div>
                <div class="card-body">
                    <form asp-action="DonationsPDF" method="post">
                        <div class="mb-3">
                            <label class="form-label">Medicamento (Opcional)</label>
                            <select name="medicineId" class="form-select" asp-items="ViewBag.MedicineId">
                                <option value="">-- Todos --</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Fecha Inicio</label>
                            <input type="date" name="startDate" class="form-control" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Fecha Fin</label>
                            <input type="date" name="endDate" class="form-control" />
                        </div>
                        <button type="submit" class="btn btn-warning w-100">
                            <i class="bi bi-download"></i> Descargar PDF
                        </button>
                    </form>
                </div>
            </div>
        </div>
    </div>
    
    <div class="row">
        <div class="col-md-12">
            <div class="card">
                <div class="card-header bg-primary text-white">
                    <h5 class="mb-0">Reporte Mensual Completo</h5>
                </div>
                <div class="card-body">
                    <form asp-action="MonthlyPDF" method="post" class="row">
                        <div class="col-md-5">
                            <label class="form-label">Año</label>
                            <input type="number" name="year" class="form-control" value="@DateTime.Now.Year" min="2020" max="2100" required />
                        </div>
                        <div class="col-md-5">
                            <label class="form-label">Mes</label>
                            <select name="month" class="form-select" required>
                                <option value="1">Enero</option>
                                <option value="2">Febrero</option>
                                <option value="3">Marzo</option>
                                <option value="4">Abril</option>
                                <option value="5">Mayo</option>
                                <option value="6">Junio</option>
                                <option value="7">Julio</option>
                                <option value="8">Agosto</option>
                                <option value="9">Septiembre</option>
                                <option value="10">Octubre</option>
                                <option value="11">Noviembre</option>
                                <option value="12">Diciembre</option>
                            </select>
                        </div>
                        <div class="col-md-2 d-flex align-items-end">
                            <button type="submit" class="btn btn-primary w-100">
                                <i class="bi bi-download"></i> Generar
                            </button>
                        </div>
                    </form>
                    <p class="text-muted mt-3 mb-0">
                        <small>Este reporte incluye: Total de donaciones, entregas y stock actual del mes seleccionado.</small>
                    </p>
                </div>
            </div>
        </div>
    </div>
</div>
```

### 6. Views/Medicines/Delete.cshtml y Details.cshtml
Crea archivos similares siguiendo el patrón de las vistas de Create/Edit.

## Pasos para Ejecutar el Proyecto

### 1. Actualizar la cadena de conexión
Edita `appsettings.json` con tu configuración de SQL Server real:
```json
"DefaultConnection": "Server=TU_SERVIDOR;Database=FarmaciaDb;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True;"
```

### 2. Crear la migración inicial
```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet ef migrations add InitialCreate
```

### 3. Aplicar la migración
```bash
dotnet ef database update
```

### 4. Ejecutar la aplicación
```bash
dotnet run
```

La aplicación estará disponible en: http://localhost:5000

## Características Implementadas

✓ Autenticación y autorización con Identity
✓ 3 roles: Admin, Farmaceutico, Viewer
✓ Gestión de usuarios (solo Admin)
✓ CRUD de medicamentos con búsqueda CIMA API
✓ Registro de entregas con validación de stock
✓ Registro de donaciones con incremento de stock
✓ Generación de reportes PDF con iText7
✓ Interfaz en español con Bootstrap 5
✓ Sin HTTPS (HTTP solo para red local)
✓ Compatible con SQL Server en Linux

## Notas de Seguridad

- Cambia la contraseña del admin después del primer login
- Configura tu SQL Server para aceptar conexiones desde la red local
- Para producción en Linux, instala SQL Server o usa Azure SQL Database

## Soporte

Sistema desarrollado para la Iglesia Metodista de Cárdenas.
