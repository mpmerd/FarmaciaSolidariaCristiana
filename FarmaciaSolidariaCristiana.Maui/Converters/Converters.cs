using System.Globalization;

namespace FarmaciaSolidariaCristiana.Maui.Converters;

/// <summary>
/// Convierte null a bool (true si no es null)
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte bool a visibilidad inversa
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

/// <summary>
/// Convierte estado "Pendiente" a true para mostrar botones de acción
/// </summary>
public class StatusToPendingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
            return status == "Pendiente";
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte bool a icono de password (ojo abierto/cerrado)
/// </summary>
public class BoolToPasswordIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible)
            return isVisible ? "eye_off.png" : "eye.png";
        return "eye.png";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte estado a color
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Pendiente" => Colors.Orange,
                "Aprobado" => Colors.Green,
                "Rechazado" => Colors.Red,
                "Completado" => Colors.Blue,
                "Cancelado" => Colors.Gray,
                _ => Colors.Black
            };
        }
        return Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte stock a color (rojo si bajo, amarillo si medio, verde si alto)
/// </summary>
public class StockToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int stock)
        {
            return stock switch
            {
                0 => Color.FromArgb("#dc3545"),      // Danger - sin stock
                <= 10 => Color.FromArgb("#ffc107"),  // Warning - stock bajo
                _ => Color.FromArgb("#198754")       // Success - stock normal
            };
        }
        return Color.FromArgb("#6c757d"); // Gray
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte bool Activo a color
/// </summary>
public class BoolToActiveColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool activo)
        {
            return activo ? Color.FromArgb("#198754") : Color.FromArgb("#dc3545");
        }
        return Color.FromArgb("#6c757d");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte bool Activo a texto
/// </summary>
public class BoolToActiveTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool activo)
        {
            return activo ? "Activo" : "Inactivo";
        }
        return "N/A";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string to a boolean (true if not null/empty)
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value?.ToString());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts month index (1-12) to picker index (0-11) and vice versa
/// </summary>
public class MonthIndexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int month)
        {
            return month - 1; // 1-12 to 0-11
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index + 1; // 0-11 to 1-12
        }
        return 1;
    }
}

/// <summary>
/// Converts bool to color based on parameter
/// Parameter format: "TrueColor|FalseColor" (e.g., "Primary|LightGray")
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string param)
        {
            var colors = param.Split('|');
            if (colors.Length == 2)
            {
                var colorName = boolValue ? colors[0] : colors[1];
                return GetColorFromName(colorName);
            }
        }
        return Colors.Gray;
    }

    private static Color GetColorFromName(string name) => name switch
    {
        "Primary" => Color.FromArgb("#0d6efd"),
        "Secondary" => Color.FromArgb("#6c757d"),
        "Success" => Color.FromArgb("#198754"),
        "Danger" => Color.FromArgb("#dc3545"),
        "Warning" => Color.FromArgb("#ffc107"),
        "Info" => Color.FromArgb("#0dcaf0"),
        "White" => Colors.White,
        "Black" => Colors.Black,
        "LightGray" => Color.FromArgb("#e9ecef"),
        _ => Colors.Gray
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to color (inverse) based on parameter
/// Parameter format: "TrueColor|FalseColor"
/// </summary>
public class BoolToColorInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string param)
        {
            var colors = param.Split('|');
            if (colors.Length == 2)
            {
                // Inverted: use second color when true
                var colorName = !boolValue ? colors[0] : colors[1];
                return GetColorFromName(colorName);
            }
        }
        return Colors.Gray;
    }

    private static Color GetColorFromName(string name) => name switch
    {
        "Primary" => Color.FromArgb("#0d6efd"),
        "Secondary" => Color.FromArgb("#6c757d"),
        "Success" => Color.FromArgb("#198754"),
        "Danger" => Color.FromArgb("#dc3545"),
        "Warning" => Color.FromArgb("#ffc107"),
        "Info" => Color.FromArgb("#0dcaf0"),
        "White" => Colors.White,
        "Black" => Colors.Black,
        "LightGray" => Color.FromArgb("#e9ecef"),
        _ => Colors.Gray
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to text based on parameter
/// Parameter format: "TrueText|FalseText"
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string param)
        {
            var texts = param.Split('|');
            if (texts.Length == 2)
            {
                return boolValue ? texts[0] : texts[1];
            }
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts int to bool (true if > 0)
/// </summary>
public class IntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if value is not null
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if value is null
/// </summary>
public class IsNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte rol de usuario a color
/// </summary>
public class RoleToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string role)
        {
            return role switch
            {
                "Admin" => Color.FromArgb("#dc3545"),        // Rojo
                "Farmaceutico" => Color.FromArgb("#28a745"), // Verde
                "Viewer" => Color.FromArgb("#17a2b8"),       // Azul info
                "ViewerPublic" => Color.FromArgb("#6c757d"), // Gris
                _ => Color.FromArgb("#6c757d")
            };
        }
        return Color.FromArgb("#6c757d");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
