# Comandos Rápidos de Despliegue

## 🚀 Desde tu Mac: Publicar y Transferir

### Publicar la aplicación
```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish
```

### Transferir al servidor Ubuntu (Primera vez)
```bash
# Transferir script de instalación
scp setup-ubuntu.sh usuario@192.168.2.113:~/

# Transferir archivos de la aplicación
rsync -avz --progress ./publish/ usuario@192.168.2.113:~/farmacia-files/
```

### Transferir actualización
```bash
# Transferir script de actualización
scp update-app.sh usuario@192.168.2.113:~/

# Transferir archivos actualizados
rsync -avz --progress ./publish/ usuario@192.168.2.113:~/farmacia-new/
```

---

## 🖥️ En el servidor Ubuntu

### Instalación inicial (solo primera vez)
```bash
ssh usuario@192.168.2.113
bash setup-ubuntu.sh
```

### Actualizar aplicación
```bash
ssh usuario@192.168.2.113
bash update-app.sh
```

### Aplicar migraciones manualmente
```bash
cd /var/www/farmacia
dotnet ef database update
```

---

## 📊 Comandos de Monitoreo

### Ver estado del servicio
```bash
sudo systemctl status farmacia.service
```

### Ver logs en tiempo real
```bash
sudo journalctl -u farmacia.service -f
```

### Ver últimos 100 logs
```bash
sudo journalctl -u farmacia.service -n 100
```

### Reiniciar servicio
```bash
sudo systemctl restart farmacia.service
```

### Reiniciar Nginx
```bash
sudo systemctl restart nginx
```

### Ver procesos .NET
```bash
ps aux | grep dotnet
```

### Ver uso de recursos
```bash
top -p $(pgrep -f FarmaciaSolidariaCristiana)
```

---

## 🗄️ Base de Datos

### Probar conexión a SQL Server
```bash
sqlcmd -S localhost -U TU_USUARIO -P 'TU_PASSWORD' -Q "SELECT @@VERSION"
```

### Ver estado de SQL Server
```bash
sudo systemctl status mssql-server
```

### Ejecutar consulta
```bash
sqlcmd -S localhost -U TU_USUARIO -P 'TU_PASSWORD' -d FarmaciaDb -Q "SELECT COUNT(*) FROM Medicines"
```

### Limpiar datos de prueba
```bash
cd /var/www/farmacia
dotnet ef database drop --force
dotnet ef database update
```

---

## 🔧 Solución de Problemas

### Servicio no inicia
```bash
# Ver logs detallados
sudo journalctl -u farmacia.service -n 100 --no-pager

# Verificar permisos
ls -la /var/www/farmacia/

# Cambiar propietario
sudo chown -R www-data:www-data /var/www/farmacia/
```

### Error 502 en Nginx
```bash
# Verificar que la app esté escuchando
sudo netstat -tlnp | grep 5000

# Ver logs de Nginx
sudo tail -f /var/log/nginx/error.log
```

### Reiniciar todo
```bash
sudo systemctl restart farmacia.service
sudo systemctl restart nginx
sudo systemctl restart mssql-server
```

---

## 🌐 Acceso a la Aplicación

- **URL por IP:** http://TU_IP_SERVIDOR
- **URL por nombre:** http://TU_NOMBRE_SERVIDOR
- **Usuario:** admin
- **Contraseña:** [Ver CONFIGURACION.md para credenciales por defecto]

---

## 📦 Estructura de Archivos

```
/var/www/farmacia/              # Aplicación
/etc/systemd/system/farmacia.service    # Servicio
/etc/nginx/sites-available/farmacia     # Config Nginx
/var/www/farmacia-backup-*/     # Respaldos
```

---

## 🔄 Workflow Completo de Actualización

```bash
# 1. En tu Mac
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
git pull
dotnet publish -c Release -o ./publish
scp update-app.sh usuario@192.168.2.113:~/
rsync -avz --progress ./publish/ usuario@192.168.2.113:~/farmacia-new/

# 2. En Ubuntu
ssh usuario@192.168.2.113
bash update-app.sh

# 3. Verificar
curl http://localhost
```
