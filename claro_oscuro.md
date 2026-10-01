# Sistema de Tema Claro/Oscuro (App MAUI)

Este documento explica cómo está concebido el soporte de tema claro/oscuro en la app MAUI (`FarmaciaSolidariaCristiana.Maui`).

## Principio general

La app **sigue automáticamente el tema del sistema operativo** (claro/oscuro). No hay un selector manual de tema: en `App.xaml.cs` **no** se establece `UserAppTheme`, por lo que `Application.Current.UserAppTheme` queda como `Unspecified` y .NET MAUI responde al cambio de tema del SO en tiempo real (sin reiniciar la app).

El mecanismo central es la extensión de marcado **`{AppThemeBinding Light=..., Dark=...}`** en el XAML. En tiempo de ejecución MAUI elige el valor `Light` o `Dark` según el tema activo.

## Estructura de recursos

1. **`Resources/Styles/Colors.xaml`** — paleta base de la plantilla MAUI (`Primary` púrpura, escala `Gray100`–`Gray950`, brushes). Casi todos estos colores se redefinen después.
2. **`Resources/Styles/Styles.xaml`** — estilos globales (implícitos y explícitos) que ya usan `AppThemeBinding` para partes comunes: barra de navegación/tabs (`Light=White, Dark=OffBlack`), botones, cards, etc.
3. **`App.xaml`** — fusiona los dos diccionarios anteriores y **redefine la paleta propia de la app** encima (Bootstrap-like):
   - `Primary` = `#0d6efd`, `Secondary` = `#6c757d`, `Success` = `#198754`, `Danger` = `#dc3545`, `Warning` = `#ffc107`, `Info` = `#0dcaf0`
   - Escala de grises propia (`Gray100` = `#f8f9fa` ... `Gray900` = `#212529`)
   - **Ojo:** `BackgroundColor` = `#F5F5F5` es un color **fijo, no adaptativo** (ver "Errores conocidos").

Estos recursos estáticos (`{StaticResource Primary}`, `{StaticResource Danger}`, etc.) se usan en ambos temas sin cambiar: son colores de marca o tonos medios que funcionan bien sobre fondo claro y oscuro.

## Convenciones por vista (patrón de la app)

| Elemento | Claro | Oscuro |
|---|---|---|
| Fondo raíz de la página | `#f5f5f5` (o `#f8f9fa`) | `#1a1a1a` |
| Frame / tarjeta contenedora | `White` | `#2a2a2a` |
| Borde de frame | `Gray200` (`#e9ecec`) | `#404040` |
| Texto de `Entry` | `#1a1a1a` | `#e0e0e0` |
| Placeholder de `Entry` | `#999999` | `#bbbbbb` |
| Botón verde (login) | `#28a745` | `#1e7e34` |
| Separador `BoxView` | `Gray200` | `#404040` |

Ejemplo real (`Views/LoginPage.xaml`):

```xml
<ContentPage ...
             BackgroundColor="{AppThemeBinding Light=#f5f5f5, Dark=#1a1a1a}">

    <Frame BorderColor="{AppThemeBinding Light={StaticResource Gray200}, Dark=#404040}"
           BackgroundColor="{AppThemeBinding Light=White, Dark=#2a2a2a}"
           Padding="10" CornerRadius="10" HasShadow="False">
        <Entry Placeholder="Correo electrónico o usuario"
               TextColor="{AppThemeBinding Light=#1a1a1a, Dark=#e0e0e0}"
               PlaceholderColor="{AppThemeBinding Light=#999999, Dark=#bbbbbb}"/>
    </Frame>
```

## Regla práctica al crear/editar vistas

1. **Nunca** uses `{StaticResource BackgroundColor}` como fondo raíz de una página: no se adapta al tema oscuro. Usa siempre `BackgroundColor="{AppThemeBinding Light=#f5f5f5, Dark=#1a1a1a}"`.
2. Todo lo que sea "superficie" (frames, cards, popups) → `AppThemeBinding Light=White, Dark=#2a2a2a`.
3. Todo lo que sea "texto/placeholder sobre superficie" → variantes claro/oscuro (tabla arriba).
4. Colores de marca (`Primary`, `Success`, `Danger`, `Warning`) y grises medios (`Gray500`) pueden usarse como `StaticResource` en ambos temas.
5. Para lógica en C# se puede consultar el tema actual con `Application.Current?.RequestedTheme` (`Light` / `Dark`).

## Historial (2026-10)

- **Corregido:** `LoginPage.xaml` usaba `{StaticResource BackgroundColor}` (fondo claro fijo) y elementos secundarios sin adaptar (bordes, separador, footer). Migrado a `AppThemeBinding`.
- **Corregido:** las 13 vistas restantes que usaban `{StaticResource BackgroundColor}` como fondo raíz también fueron migradas a `BackgroundColor="{AppThemeBinding Light=#f5f5f5, Dark=#1a1a1a}"`:
  `RegisterPage`, `ProfilePage`, `ChangePasswordPage`, `PacientesPage`, `UsuariosPage`, `DonacionesPage`, `EntregasPage`, `InsumosPage`, `PatrocinadoresPage`, `ReportesPage`, `ReprogramarTurnosPage`, `FechasBloqueadasPage`, `BloqueoPacientePage`.
- Ya no queda ningún `{StaticResource BackgroundColor}` en los XAML de la app.

> Nota: el recurso `BackgroundColor` (#F5F5F5 fijo) sigue definido en `App.xaml` por compatibilidad, pero **no debe usarse** en nuevas vistas (ver regla 1 arriba).
