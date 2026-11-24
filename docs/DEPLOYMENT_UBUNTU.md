# Guía de Despliegue en Ubuntu Server (192.168.x.x)

Esta guía explica paso a paso cómo desplegar la aplicación **Farmacia Solidaria Cristiana** en tu servidor Ubuntu de la red local.

## 📋 Requisitos Previos

### En el Servidor Ubuntu (192.168.x.x3):
- Ubuntu Server (18.04 o superior)
- Acceso SSH
- Usuario con permisos sudo
- SQL Server ya instalado y configurado

---

## 🔧 Paso 1: Preparar el Servidor Ubuntu

### 1.1 Conectarse al servidor
```bash
ssh usuario@192.168.x.x
# O usando el nombre de host:
ssh usuario@MPMESCRITNOMBREPC
```

### 1.2 Actualizar el sistema
```bash
sudo apt update
sudo apt upgrade -y
```

### 1.3 Instalar .NET 9 Runtime y SDK
```bash
# Descargar el script de instalación de Microsoft
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh

# Instalar .NET 9 SDK
./dotnet-install.sh --channel 9.0

# Agregar dotnet al PATH
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> ~/.bashrc
source ~/.bashrc

# Verificar instalación
dotnet --version
```

**Alternativa (instalación desde paquetes):**
```bash
# Para Ubuntu 22.04/24.04
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-9.0 aspnetcore-runtime-9.0
```

### 1.4 Instalar Nginx (servidor web reverso)
```bash
sudo apt install -y nginx
sudo systemctl start nginx
sudo systemctl enable nginx
```

---

## 📦 Paso 2: Preparar la Aplicación en tu Mac

### 2.1 Publicar la aplicación
Desde tu Mac, en el directorio del proyecto:

```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana

# Publicar en modo Release
dotnet publish -c Release -o ./publish
```

### 2.2 Transferir archivos al servidor
```bash
# Comprimir la carpeta de publicación
cd publish
tar -czf farmacia.tar.gz *

# Transferir al servidor Ubuntu
scp farmacia.tar.gz usuario@192.168.x.x:/home/usuario/
```

**O usar rsync (más eficiente):**
```bash
cd /Users/xxxxxxxx/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana

rsync -avz --progress ./publish/ usuario@192.168.x.x:/home/usuario/farmacia/
```

---

## 🚀 Paso 3: Configurar la Aplicación en Ubuntu

### 3.1 Crear directorio de la aplicación
```bash
# En el servidor Ubuntu
sudo mkdir -p /var/www/farmacia
sudo chown -R $USER:$USER /var/www/farmacia

# Descomprimir archivos
cd /var/www/farmacia
tar -xzf ~/farmacia.tar.gz
rm ~/farmacia.tar.gz
```

### 3.2 Configurar appsettings.json
```bash
cd /var/www/farmacia
nano appsettings.json
```

Verificar la cadena de conexión (debe apuntar a localhost ya que SQL Server está en el mismo servidor):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FarmaciaDb;User Id=xxxxxxx;Password=xxx-xxx-xxx-xxx;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 3.3 Probar la aplicación manualmente
```bash
cd /var/www/farmacia
dotnet FarmaciaSolidariaCristiana.dll --urls="http://0.0.0.0:5000"
```

Desde tu Mac, abre el navegador: `http://192.168.x.x:5000`

Si funciona correctamente, presiona `Ctrl+C` para detener y continúa con el siguiente paso.

---

## ⚙️ Paso 4: Crear Servicio Systemd

### 4.1 Crear archivo de servicio
```bash
sudo nano /etc/systemd/system/farmacia.service
```

Agregar el siguiente contenido:
```ini
[Unit]
Description=Farmacia Solidaria Cristiana - ASP.NET Core App
After=network.target

[Service]
WorkingDirectory=/var/www/farmacia
ExecStart=/home/usuario/.dotnet/dotnet /var/www/farmacia/FarmaciaSolidariaCristiana.dll --urls="http://localhost:5000"
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=farmacia-app
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ROOT=/home/usuario/.dotnet

[Install]
WantedBy=multi-user.target
```

**Nota:** Ajusta `/home/usuario/.dotnet` con la ruta real donde instalaste .NET.

### 4.2 Habilitar e iniciar el servicio
```bash
# Recargar configuración de systemd
sudo systemctl daemon-reload

# Habilitar inicio automático
sudo systemctl enable farmacia.service

# Iniciar el servicio
sudo systemctl start farmacia.service

# Verificar estado
sudo systemctl status farmacia.service
```

### 4.3 Ver logs en caso de errores
```bash
# Ver logs en tiempo real
sudo journalctl -u farmacia.service -f

# Ver últimas 100 líneas
sudo journalctl -u farmacia.service -n 100
```

---

## 🌐 Paso 5: Configurar Nginx como Proxy Reverso

### 5.1 Crear configuración de Nginx
```bash
sudo nano /etc/nginx/sites-available/farmacia
```

Agregar el siguiente contenido:
```nginx
server {
    listen 80;
    listen [::]:80;
    server_name 192.168.2.113 MPMESCRITORIO farmacia.local;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Aumentar timeouts para reportes PDF
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    # Archivos estáticos (CSS, JS, imágenes)
    location ~* \.(css|js|gif|jpe?g|png|ico|svg|woff|woff2|ttf|eot)$ {
        proxy_pass http://localhost:5000;
        proxy_cache_valid 200 1d;
        expires 1d;
        add_header Cache-Control "public, immutable";
    }

    client_max_body_size 10M;
}
```

### 5.2 Habilitar el sitio
```bash
# Crear enlace simbólico
sudo ln -s /etc/nginx/sites-available/farmacia /etc/nginx/sites-enabled/

# Probar configuración
sudo nginx -t

# Si el test es exitoso, reiniciar Nginx
sudo systemctl restart nginx
```

---

## 🔥 Paso 6: Configurar Firewall (Opcional)

### 6.1 Configurar UFW
```bash
# Permitir HTTP
sudo ufw allow 80/tcp

# Permitir SSH (si no está permitido)
sudo ufw allow 22/tcp

# Habilitar firewall
sudo ufw enable

# Ver estado
sudo ufw status
```

---

## 🗄️ Paso 7: Aplicar Migraciones de Base de Datos

### 7.1 Verificar conexión a SQL Server
```bash
# Instalar herramienta sqlcmd si no está instalada
sudo apt install -y mssql-tools unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
source ~/.bashrc

# Probar conexión
sqlcmd -S localhost -U xxxxxxxxx -P 'xxxxx-xxxxx-xxxxxx' -Q "SELECT @@VERSION"
```

### 7.2 Aplicar migraciones
```bash
cd /var/www/farmacia

# Instalar herramienta EF Core (si no está instalada)
dotnet tool install --global dotnet-ef

# Agregar al PATH
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc

# Aplicar migraciones
dotnet ef database update --project /var/www/farmacia/FarmaciaSolidariaCristiana.dll
```

**Alternativa:** Aplicar migraciones desde tu Mac y conectarte remotamente a SQL Server:
```bash
# En tu Mac
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet ef database update
```

---

## 🎯 Paso 8: Acceder a la Aplicación

### 8.1 Desde cualquier dispositivo en la red local:
- **Por IP:** http://192.168.x.x
- **Por nombre:** http://NOMBREPC (si tu red tiene DNS/WINS configurado)

### 8.2 Credenciales de acceso:
- **Usuario:** admin
- **Contraseña:** xxxxxxxxxxx

---

## 🔄 Actualizaciones Futuras

### Proceso para actualizar la aplicación:

```bash
# 1. En tu Mac: Publicar nueva versión
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish

# 2. Transferir al servidor
rsync -avz --progress ./publish/ usuario@192.168.x.x:/home/usuario/farmacia-new/

# 3. En el servidor Ubuntu: Detener servicio
sudo systemctl stop farmacia.service

# 4. Hacer respaldo
sudo mv /var/www/farmacia /var/www/farmacia-backup-$(date +%Y%m%d)

# 5. Mover nueva versión
sudo mv /home/usuario/farmacia-new /var/www/farmacia

# 6. Restaurar appsettings.json si es necesario
sudo cp /var/www/farmacia-backup-*/appsettings.json /var/www/farmacia/

# 7. Aplicar nuevas migraciones (si las hay)
cd /var/www/farmacia
dotnet ef database update

# 8. Iniciar servicio
sudo systemctl start farmacia.service

# 9. Verificar estado
sudo systemctl status farmacia.service
```

---

## 🐛 Solución de Problemas

### Servicio no inicia
```bash
# Ver logs detallados
sudo journalctl -u farmacia.service -n 100 --no-pager

# Verificar permisos
ls -la /var/www/farmacia/

# Cambiar propietario si es necesario
sudo chown -R www-data:www-data /var/www/farmacia/
```

### Error de conexión a base de datos
```bash
# Verificar que SQL Server esté corriendo
sudo systemctl status mssql-server

# Probar conexión manualmente
sqlcmd -S localhost -U farmaceutico -P 'wuwbug-hAjkip-xikgy3' -d FarmaciaDb -Q "SELECT COUNT(*) FROM Medicines"
```

### Nginx muestra 502 Bad Gateway
```bash
# Verificar que la app esté escuchando en el puerto 5000
sudo netstat -tlnp | grep 5000

# Ver logs de Nginx
sudo tail -f /var/log/nginx/error.log
```

### Reiniciar todos los servicios
```bash
sudo systemctl restart farmacia.service
sudo systemctl restart nginx
```

---

## 📊 Monitoreo y Mantenimiento

### Ver uso de recursos
```bash
# CPU y memoria de la aplicación
top -p $(pgrep -f FarmaciaSolidariaCristiana)

# Espacio en disco
df -h

# Ver logs en tiempo real
sudo journalctl -u farmacia.service -f
```

### Configurar logs rotativos
```bash
sudo nano /etc/logrotate.d/farmacia
```

Agregar:
```
/var/log/farmacia/*.log {
    daily
    rotate 7
    compress
    delaycompress
    missingok
    notifempty
}
```

---

## 🔒 Seguridad Adicional (Recomendado)

### 1. Cambiar permisos restrictivos
```bash
sudo chmod 750 /var/www/farmacia
sudo chmod 640 /var/www/farmacia/appsettings.json
```

### 2. Configurar certificado SSL/TLS (opcional para red local)
Si quieres HTTPS, puedes usar un certificado auto-firmado:

```bash
# Generar certificado
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/farmacia.key \
  -out /etc/ssl/certs/farmacia.crt

# Actualizar configuración de Nginx
sudo nano /etc/nginx/sites-available/farmacia
```

Agregar:
```nginx
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name 192.168.x.x NOMBREPC;

    ssl_certificate /etc/ssl/certs/farmacia.crt;
    ssl_certificate_key /etc/ssl/private/farmacia.key;
    
    # ... resto de la configuración ...
}
```

---

## ✅ Checklist Final

- [ ] .NET 9 instalado en Ubuntu
- [ ] Nginx instalado y configurado
- [ ] Aplicación publicada y transferida
- [ ] Servicio systemd creado y en ejecución
- [ ] Nginx configurado como proxy reverso
- [ ] Base de datos migrada
- [ ] Firewall configurado
- [ ] Aplicación accesible desde la red
- [ ] Credenciales de admin funcionando
- [ ] Logs configurados

---

## 📞 Comandos Útiles de Referencia Rápida

```bash
# Estado del servicio
sudo systemctl status farmacia.service

# Reiniciar servicio
sudo systemctl restart farmacia.service

# Ver logs
sudo journalctl -u farmacia.service -f

# Reiniciar Nginx
sudo systemctl restart nginx

# Probar configuración de Nginx
sudo nginx -t

# Ver procesos .NET
ps aux | grep dotnet
```

---

## 🎓 Notas Adicionales

- La aplicación correrá en modo **Production** automáticamente
- Los datos de prueba solo se cargan si la base de datos está vacía
- Los archivos estáticos (CSS, JS, imágenes) se sirven eficientemente a través de Nginx
- Los reportes PDF pueden tardar unos segundos en generarse (los timeouts están configurados)

---

**¡Tu aplicación Farmacia Solidaria Cristiana estará lista para uso en producción!** 🚀
