# Configuración de Seguridad HTTPS

## Resumen
Se ha implementado una configuración completa de seguridad HTTPS en la aplicación web para garantizar que **solo se permita tráfico HTTPS** en producción.

## Cambios Implementados

### 1. Program.cs - Configuración de HSTS
- **HSTS (HTTP Strict Transport Security)** configurado con:
  - `MaxAge`: 365 días (1 año)
  - `IncludeSubDomains`: true
  - `Preload`: true
  
```csharp
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
```

### 2. Program.cs - Redirección HTTPS
- Configurada redirección permanente (308) de HTTP a HTTPS
- Puerto HTTPS: 443

```csharp
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});
```

### 3. Program.cs - JWT con RequireHttpsMetadata
- JWT configurado para requerir HTTPS en metadatos en producción
- Previene ataques man-in-the-middle en autenticación

```csharp
options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
```

### 4. Program.cs - Middleware de Seguridad
- Encabezado `Strict-Transport-Security` añadido en cada respuesta
- Solo en producción para permitir desarrollo local

```csharp
app.Use(async (context, next) =>
{
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", 
            "max-age=31536000; includeSubDomains; preload");
    }
    await next();
});
```

### 5. Program.cs - Cookies Seguras
- Cookies configuradas con nombre personalizado
- `SecurePolicy.Always` en producción (solo HTTPS)
- `HttpOnly`: true
- `SameSite`: Lax

### 6. web.config - Redirección a Nivel de IIS
- Regla de reescritura de URL para forzar HTTPS
- Redirección permanente (301) de todo el tráfico HTTP a HTTPS

```xml
<rule name="HTTPS Force" enabled="true" stopProcessing="true">
  <match url="(.*)" />
  <conditions>
    <add input="{HTTPS}" pattern="^OFF$" />
  </conditions>
  <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
</rule>
```

### 7. web.config - Encabezados de Seguridad HTTP
Encabezados adicionales de seguridad añadidos:
- `Strict-Transport-Security`: Fuerza HTTPS por 1 año
- `X-Content-Type-Options`: Previene MIME sniffing
- `X-Frame-Options`: Protege contra clickjacking
- `X-XSS-Protection`: Activa protección XSS del navegador

## Capas de Seguridad

La configuración implementa **múltiples capas de protección**:

1. **Capa de Servidor Web (IIS/web.config)**: 
   - Primera línea de defensa
   - Redirección HTTP → HTTPS antes de llegar a la aplicación

2. **Capa de Middleware (.NET)**: 
   - UseHttpsRedirection()
   - UseHsts()
   - Middleware personalizado

3. **Capa de Aplicación**:
   - Cookies seguras
   - JWT con RequireHttpsMetadata
   - Encabezados de seguridad personalizados

## Comportamiento por Entorno

### Desarrollo (ASPNETCORE_ENVIRONMENT=Development)
- Se permite HTTP para desarrollo local
- No se fuerza HTTPS
- No se añaden encabezados HSTS

### Producción (ASPNETCORE_ENVIRONMENT=Production)
- **TODO el tráfico HTTP se redirige a HTTPS**
- HSTS activado (navegadores recordarán usar HTTPS)
- Cookies solo en HTTPS
- JWT requiere HTTPS para metadatos
- Encabezados de seguridad activados

## Verificación

Para verificar que HTTPS está funcionando:

1. **Intentar acceder por HTTP**:
   ```
   http://farmaciasolidaria.somee.com
   ```
   Debe redirigir automáticamente a:
   ```
   https://farmaciasolidaria.somee.com
   ```

2. **Verificar encabezados de respuesta**:
   ```bash
   curl -I https://farmaciasolidaria.somee.com
   ```
   Debe incluir:
   ```
   Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
   X-Content-Type-Options: nosniff
   X-Frame-Options: SAMEORIGIN
   X-XSS-Protection: 1; mode=block
   ```

3. **Probar autenticación API**:
   - Tokens JWT solo deben funcionar en conexiones HTTPS
   - Cookies de sesión solo se envían por HTTPS

## Próximos Pasos Recomendados

1. **HSTS Preload**: Considerar registrar el dominio en [hstspreload.org](https://hstspreload.org/) para que los navegadores SIEMPRE usen HTTPS incluso en la primera visita

2. **Certificado SSL**: Asegurar que el certificado SSL esté actualizado y válido

3. **Content Security Policy (CSP)**: Añadir políticas CSP para mayor seguridad

4. **Monitoreo**: Configurar alertas para detectar intentos de acceso HTTP

## Archivos Modificados

- [FarmaciaSolidariaCristiana/Program.cs](FarmaciaSolidariaCristiana/Program.cs)
- [FarmaciaSolidariaCristiana/web.config](FarmaciaSolidariaCristiana/web.config) (nuevo)
- [publish/web.config](publish/web.config)

## Fecha de Implementación
4 de febrero de 2026
