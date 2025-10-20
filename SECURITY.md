# 🔒 Guía de Seguridad - Farmacia Solidaria Cristiana

## ⚠️ Información Sensible - NUNCA Compartir

Esta guía explica cómo manejar información sensible de forma segura.

---

## 🚫 NUNCA Incluir en Git

### ❌ Información que NUNCA debe estar en el repositorio:

1. **Contraseñas y credenciales**
   - Contraseñas de base de datos
   - Contraseñas de usuarios
   - Tokens de API
   - Claves privadas

2. **Información de servidores**
   - Direcciones IP privadas
   - Nombres de servidores internos
   - Puertos específicos
   - Rutas de archivos del sistema

3. **Configuraciones de producción**
   - Cadenas de conexión con credenciales reales
   - Configuraciones específicas de entorno
   - Secretos de aplicación

4. **Archivos sensibles**
   - `.env` (variables de entorno)
   - `appsettings.Production.json`
   - Archivos de backup con datos reales
   - Logs con información sensible

---

## ✅ Mejores Prácticas

### 1. Variables de Entorno

**Usar `.env` para configuraciones locales:**

```bash
# .env (NUNCA commitear este archivo)
DB_SERVER=192.168.2.113
DB_PASSWORD=mi_password_secreto
ADMIN_PASSWORD=otra_password_segura
```

**Usar `.env.example` como plantilla:**

```bash
# .env.example (SÍ commitear este como ejemplo)
DB_SERVER=localhost
DB_PASSWORD=tu_password_aqui
ADMIN_PASSWORD=cambiar_esto
```

### 2. Archivos de Configuración

**appsettings.json (versionado - sin secretos):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FarmaciaDb;User Id=usuario;Password=***;TrustServerCertificate=True;"
  }
}
```

**appsettings.Production.json (NO versionado - con secretos reales):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.2.113,1433;Database=FarmaciaDb;User Id=farmaceutico;Password=password_real_aqui;TrustServerCertificate=True;"
  }
}
```

### 3. User Secrets en Desarrollo

Para desarrollo local, usa User Secrets de .NET:

```bash
# Inicializar
dotnet user-secrets init

# Agregar secretos
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Password=real_password;"
dotnet user-secrets set "AdminPassword" "mi_password"

# Listar secretos
dotnet user-secrets list
```

Los secretos se guardan en:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/<id>/secrets.json`

### 4. Variables de Entorno en Producción

En el servidor Ubuntu, configura variables de entorno:

```bash
# Editar el servicio systemd
sudo nano /etc/systemd/system/farmacia.service

# Agregar variables de entorno
[Service]
Environment="ConnectionStrings__DefaultConnection=Server=...;Password=real;"
Environment="AdminPassword=secure_password"

# Recargar y reiniciar
sudo systemctl daemon-reload
sudo systemctl restart farmacia.service
```

---

## 🔐 Gestión de Contraseñas

### Requisitos de Contraseñas Seguras:

- ✅ Mínimo 12 caracteres
- ✅ Combinación de mayúsculas y minúsculas
- ✅ Números y caracteres especiales
- ✅ No usar palabras del diccionario
- ✅ No reutilizar contraseñas

### Generador de Contraseñas:

```bash
# Linux/macOS - generar contraseña aleatoria
openssl rand -base64 32

# Resultado ejemplo:
# kZ8mN2pQ5rT9vX3bC6dF1gH4jK7lM0n
```

### Cambiar Contraseñas Regularmente:

1. **Base de datos:** Cada 3-6 meses
2. **Usuario admin:** Después del primer acceso
3. **Usuarios del sistema:** Según política de la organización

---

## 📝 Checklist de Seguridad

### Antes de Commitear:

- [ ] Revisar `git status` - ¿hay archivos sensibles?
- [ ] Verificar `.gitignore` - ¿están excluidos los archivos sensibles?
- [ ] Buscar contraseñas en código: `grep -r "password" .`
- [ ] Buscar IPs privadas: `grep -r "192.168" .`
- [ ] Revisar README - ¿hay credenciales expuestas?

### Después de Clonar el Repo:

- [ ] Copiar `.env.example` a `.env`
- [ ] Configurar variables de entorno reales en `.env`
- [ ] Configurar User Secrets para desarrollo
- [ ] Nunca commitear `.env`

### En Producción:

- [ ] Usar configuraciones separadas (`appsettings.Production.json`)
- [ ] Variables de entorno en systemd o docker
- [ ] Cambiar contraseñas por defecto
- [ ] Permisos restrictivos en archivos de configuración (`chmod 600`)
- [ ] Logs sin información sensible

---

## 🚨 ¿Qué Hacer si Expones Credenciales?

### Si commiteaste credenciales por error:

1. **INMEDIATAMENTE cambiar las contraseñas expuestas**
   ```bash
   # En SQL Server
   ALTER LOGIN [usuario] WITH PASSWORD = 'nueva_password_segura';
   ```

2. **Eliminar el commit del historial (si no has pusheado)**
   ```bash
   git reset --soft HEAD~1
   git add archivo_corregido
   git commit -m "Fix: remover credenciales expuestas"
   ```

3. **Si ya hiciste push - necesitas reescribir la historia**
   ```bash
   # PELIGROSO - solo si es necesario
   git filter-branch --force --index-filter \
   "git rm --cached --ignore-unmatch archivo_con_credenciales" \
   --prune-empty --tag-name-filter cat -- --all
   
   git push origin --force --all
   ```

4. **Notificar al equipo** si es un repositorio compartido

---

## 📚 Referencias

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [GitHub Security Best Practices](https://docs.github.com/en/code-security/getting-started/best-practices-for-preventing-data-leaks-in-your-organization)

---

## 📞 Contacto de Seguridad

Si descubres una vulnerabilidad de seguridad, repórtala inmediatamente al administrador del sistema.

**NO publiques vulnerabilidades en issues públicos.**

---

*Última actualización: Octubre 2025*
