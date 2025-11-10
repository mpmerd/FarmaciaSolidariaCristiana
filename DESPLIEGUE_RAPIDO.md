# 🚀 Guía Rápida de Despliegue y Actualización

## 📊 Estado Actual del Proyecto

- **Versión .NET:** 8.0
- **Servidor:** Ubuntu 24.04 (192.168.x.x/ NOMBREPC)
- **Usuario SSH:** usuario
- **Base de Datos:** SQL Server en 192.168.x.x
- **URL Aplicación:** http://192.168.x.x
- **Credenciales Admin:** admin / xxxx-xxxxx-xxxxx

---

## 🔄 Actualizar la Aplicación en el Servidor

### Paso 1: Publicar Cambios en tu Mac

```bash
cd /Users/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana

# Limpiar y publicar
rm -rf publish
dotnet publish -c Release -o ./publish
```

### Paso 2: Transferir al Servidor

```bash
cd ..

# Transferir archivos actualizados
rsync -avz --progress --delete ./FarmaciaSolidariaCristiana/publish/ usuario@192.168.x.x:~/farmacia-files/
```

### Paso 3: Actualizar en el Servidor

```bash
# Conectarse al servidor
ssh user@192.168.x.x

# Detener el servicio
sudo systemctl stop farmacia.service

# Hacer respaldo (opcional pero recomendado)
sudo cp -r /var/www/farmacia /var/www/farmacia-backup-$(date +%Y%m%d-%H%M%S)

# Copiar nuevos archivos
sudo rm -rf /var/www/farmacia/*
sudo cp -r ~/farmacia-files/* /var/www/farmacia/
sudo chown -R www-data:www-data /var/www/farmacia

# Aplicar migraciones si hay cambios en BD
cd /var/www/farmacia
dotnet ef database update

# Reiniciar servicio
sudo systemctl start farmacia.service

# Verificar que esté corriendo
sudo systemctl status farmacia.service
```

---

## 🆕 Primera Instalación en Nuevo Servidor

### Opción A: Con SSH Funcionando

```bash
# 1. En tu Mac: Publicar
cd /Users/usuario/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish
cd ..

# 2. Transferir script y archivos
scp setup-simple.sh maikeled@192.168.x.x:~/
rsync -avz --progress ./FarmaciaSolidariaCristiana/publish/ maikeled@192.168.x.x:~/farmacia-files/

# 3. Ejecutar instalación
ssh -t maikeled@192.168.x.x "bash ~/setup-simple.sh"
```

### Opción B: Sin SSH (USB)

```bash
# 1. Preparar paquete
bash prepare-usb-package.sh

# 2. Copiar a USB
cp -r usb-deployment /Volumes/TU_USB/farmacia/

# 3. En el servidor Ubuntu, seguir instrucciones en DEPLOYMENT_MANUAL_USB.md
```

---

## 🔧 Comandos Útiles del Servidor

### Ver Logs en Tiempo Real
```bash
ssh usuario@192.168.x.x
sudo journalctl -u farmacia.service -f
```

### Ver Estado del Servicio
```bash
sudo systemctl status farmacia.service
```

### Reiniciar Aplicación
```bash
sudo systemctl restart farmacia.service
```

### Reiniciar Nginx
```bash
sudo systemctl restart nginx
```

### Ver Últimos 50 Logs
```bash
sudo journalctl -u farmacia.service -n 50 --no-pager
```

### Verificar Puertos
```bash
sudo ss -tlnp | grep -E ':(80|5000)'
```

### Ver Versión de .NET
```bash
dotnet --version
dotnet --list-runtimes
```

---

## 🗄️ Gestión de Base de Datos

### Aplicar Migraciones
```bash
ssh usuario@192.168.x.x
cd /var/www/farmacia
dotnet ef database update
```

### Crear Nueva Migración (desde tu Mac)
```bash
cd /Users/maikelpusuario/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet ef migrations add NombreDeLaMigracion
```

### Ver Migraciones Aplicadas
```bash
ssh usuario@192.168.x.x
cd /var/www/farmacia
dotnet ef migrations list
```

### Limpiar Datos de Prueba
```bash
ssh usuario@192.168.x.x
cd /var/www/farmacia
dotnet ef database drop --force
dotnet ef database update
```

---

## 🐛 Solución de Problemas Comunes

### La aplicación no inicia

```bash
# Ver logs detallados
sudo journalctl -u farmacia.service -n 100 --no-pager

# Verificar permisos
ls -la /var/www/farmacia/

# Corregir permisos si es necesario
sudo chown -R www-data:www-data /var/www/farmacia/
```

### Error 502 Bad Gateway

```bash
# Verificar que la app esté corriendo en puerto 5000
sudo ss -tlnp | grep 5000

# Reiniciar ambos servicios
sudo systemctl restart farmacia.service
sudo systemctl restart nginx
```

### Puerto 80 ocupado

```bash
# Ver qué está usando el puerto 80
sudo ss -tlnp | grep :80

# Si es Apache2, detenerlo
sudo systemctl stop apache2
sudo systemctl disable apache2
```

### Error de conexión a base de datos

```bash
# Verificar SQL Server
sudo systemctl status mssql-server

# Probar conexión manualmente
/opt/mssql-tools/bin/sqlcmd -S localhost -U usuario -P 'xxxx-xxxxx-xxxx' -Q "SELECT @@VERSION"
```

---

## 📦 Scripts Disponibles

| Script | Propósito |
|--------|-----------|
| `setup-simple.sh` | Instalación inicial en servidor con .NET 8 ya instalado |
| `setup-ubuntu.sh` | Instalación completa incluyendo .NET 8 |
| `deploy-interactive.sh` | Asistente interactivo de despliegue |
| `prepare-usb-package.sh` | Crear paquete para instalación sin SSH |
| `update-app.sh` | Actualización automática con respaldo |

---

## 🔐 Información de Seguridad

### Credenciales SQL Server
- **Servidor:** 192.168.x.x,1433
- **Base de Datos:** FarmaciaDb
- **Usuario:** usuario
- **Contraseña:** xxxx-xxxx-xxxxx

### Credenciales Admin Aplicación
- **Usuario:** admin
- **Contraseña:** xxxx-xxxxxx-xxxxxxxx

### SSH
- **Usuario:** usuario
- **Contraseña:** xxxx-xxxxx-xxxxxx

**⚠️ IMPORTANTE:** Cambia estas credenciales en producción.

---

## 📁 Estructura de Archivos en el Servidor

```
/var/www/farmacia/              # Aplicación principal
/etc/systemd/system/farmacia.service    # Servicio systemd
/etc/nginx/sites-available/farmacia     # Configuración Nginx
/etc/nginx/sites-enabled/farmacia       # Link simbólico activo
/home/maikeled/farmacia-files/  # Archivos temporales de despliegue
```

---

## 🔄 Workflow Completo de Actualización

```bash
# 1. Hacer cambios en tu Mac
cd /User/Documents/Proyectos/FarmaciaSolidariaCristiana

# 2. Commit y push
git add .
git commit -m "feat: descripción de cambios"
git push origin developer

# 3. Publicar
cd FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish
cd ..

# 4. Transferir
rsync -avz --progress --delete ./FarmaciaSolidariaCristiana/publish/ maikeled@192.168.2.113:~/farmacia-files/

# 5. Actualizar en servidor
ssh maikeled@192.168.2.113 << 'EOF'
sudo systemctl stop farmacia.service
sudo cp -r /var/www/farmacia /var/www/farmacia-backup-$(date +%Y%m%d-%H%M%S)
sudo rm -rf /var/www/farmacia/*
sudo cp -r ~/farmacia-files/* /var/www/farmacia/
sudo chown -R www-data:www-data /var/www/farmacia
sudo systemctl start farmacia.service
sudo systemctl status farmacia.service
EOF

# 6. Verificar
open http://192.168.x.x
```

---

## ✅ Checklist de Verificación Post-Despliegue

- [ ] Servicio `farmacia.service` está corriendo
- [ ] Nginx está corriendo
- [ ] Aplicación responde en http://192.168.x.x
- [ ] Login funciona correctamente
- [ ] Las páginas cargan sin errores
- [ ] Los reportes PDF se generan correctamente
- [ ] Los logos se muestran en todas las páginas
- [ ] La conexión a base de datos funciona

---

## 📞 Contacto y Soporte

Para problemas o preguntas:
1. Revisar logs: `sudo journalctl -u farmacia.service -f`
2. Consultar documentación en: `DEPLOYMENT_UBUNTU.md`
3. Ver troubleshooting en: `TROUBLESHOOTING_SSH.md`

---

**Última actualización:** 20 de octubre de 2025
**Versión del documento:** 1.0
