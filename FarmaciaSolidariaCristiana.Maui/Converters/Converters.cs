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
