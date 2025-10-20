# Scripts de Despliegue y Actualización

## 📦 Archivos Disponibles

### Scripts de Instalación Inicial
- **setup-ubuntu.sh** - Script completo de instalación (incluye instalación de .NET)
- **setup-simple.sh** - Script simplificado (para cuando .NET ya está instalado) ⭐ RECOMENDADO

### Scripts de Actualización
- **quick-update.sh** - Actualización rápida de la aplicación en el servidor
- **update-app.sh** - Script completo de actualización con respaldos

### Scripts de Prueba
- **test-cima-api.sh** - Prueba de conectividad con API CIMA
- **prepare-usb-package.sh** - Preparar paquete para instalación por USB

---

## 🚀 Proceso de Despliegue Completo

### Primera Instalación

```bash
# 1. En tu Mac: Publicar aplicación
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish

# 2. Transferir al servidor
cd ..
scp setup-simple.sh maikeled@192.168.2.113:~/
rsync -avz --progress ./FarmaciaSolidariaCristiana/publish/ maikeled@192.168.2.113:~/farmacia-files/

# 3. Instalar en el servidor
ssh maikeled@192.168.2.113
bash ~/setup-simple.sh
```

---

## 🔄 Proceso de Actualización

### Método 1: Actualización Rápida (Recomendado)

```bash
# 1. En tu Mac: Publicar nueva versión
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish

# 2. Transferir archivos actualizados
cd ..
rsync -avz --delete ./FarmaciaSolidariaCristiana/publish/ maikeled@192.168.2.113:~/farmacia-files/

# 3. Transferir y ejecutar script de actualización
scp quick-update.sh maikeled@192.168.2.113:~/
ssh -t maikeled@192.168.2.113 "bash ~/quick-update.sh"
```

### Método 2: Todo en un comando

```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana && \
dotnet publish -c Release -o ./publish && \
cd .. && \
rsync -avz --delete ./FarmaciaSolidariaCristiana/publish/ maikeled@192.168.2.113:~/farmacia-files/ && \
scp quick-update.sh maikeled@192.168.2.113:~/ && \
ssh -t maikeled@192.168.2.113 "bash ~/quick-update.sh"
```

---

## 📊 Comandos de Monitoreo

### Ver logs en tiempo real
```bash
ssh maikeled@192.168.2.113
sudo journalctl -u farmacia.service -f
```

### Ver estado del servicio
```bash
ssh maikeled@192.168.2.113
sudo systemctl status farmacia.service
```

### Reiniciar servicio
```bash
ssh -t maikeled@192.168.2.113 "sudo systemctl restart farmacia.service"
```

### Ver últimos 50 logs
```bash
ssh maikeled@192.168.2.113
sudo journalctl -u farmacia.service -n 50
```

---

## 🔧 Solución de Problemas

### Servicio no inicia

```bash
# Ver logs detallados
ssh maikeled@192.168.2.113
sudo journalctl -u farmacia.service -n 100 --no-pager

# Verificar archivos
ls -la /var/www/farmacia/

# Verificar permisos
sudo chown -R www-data:www-data /var/www/farmacia/
```

### Probar API CIMA

```bash
# Transferir y ejecutar script de prueba
scp test-cima-api.sh maikeled@192.168.2.113:~/
ssh maikeled@192.168.2.113 "bash ~/test-cima-api.sh 662883"
```

### Puerto 80 ocupado

```bash
# Ver qué está usando el puerto
ssh -t maikeled@192.168.2.113 "sudo ss -tlnp | grep :80"

# Si es Apache, detenerlo
ssh -t maikeled@192.168.2.113 "sudo systemctl stop apache2 && sudo systemctl disable apache2"
```

---

## ⚠️ Notas Importantes

### API CIMA desde Cuba
La API CIMA (https://cima.aemps.es) puede ser **muy lenta** desde Cuba debido a:
- Latencia internacional (>200ms)
- Problemas con handshake TLS/SSL (puede tardar >15 segundos)
- Limitaciones de ancho de banda

**Solución implementada:**
- Timeout aumentado a 30 segundos
- Validación de certificado SSL deshabilitada
- Mensajes de error más descriptivos
- Logging detallado para diagnóstico

**Recomendación:** Si la API CIMA no funciona desde Cuba, considera:
1. Usar entrada manual de datos
2. Implementar caché local de medicamentos comunes
3. Crear base de datos de medicamentos precargada

### Configuración de .NET
- Proyecto usa **.NET 8.0** (net8.0)
- Servidor Ubuntu tiene .NET 8.0.121 instalado
- Paquetes EF Core y Identity: versión 8.0.11

---

## 📁 Estructura de Archivos en el Servidor

```
/var/www/farmacia/              # Aplicación
/etc/systemd/system/farmacia.service    # Servicio systemd
/etc/nginx/sites-available/farmacia     # Configuración Nginx
/home/maikeled/farmacia-files/  # Archivos temporales para actualización
```

---

## 🌐 Acceso

- **URL:** http://192.168.2.113 o http://MPMESCRITORIO
- **Usuario:** admin
- **Contraseña:** doqkox-gadqud-niJho0

---

## 📝 Checklist de Actualización

- [ ] Publicar aplicación localmente (`dotnet publish`)
- [ ] Transferir archivos al servidor (`rsync`)
- [ ] Transferir script de actualización (`scp quick-update.sh`)
- [ ] Ejecutar actualización (`bash quick-update.sh`)
- [ ] Verificar servicio (`systemctl status farmacia.service`)
- [ ] Probar aplicación en navegador
- [ ] Verificar logs si hay problemas

---

**Última actualización:** 20 de Octubre de 2025
