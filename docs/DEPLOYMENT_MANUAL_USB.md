# Despliegue Manual Usando USB (Sin SSH)

## 📋 Requisitos
- USB de al menos 2 GB
- Acceso físico al servidor Ubuntu

## 🔧 Paso 1: Preparar Archivos en tu Mac

### 1.1 Publicar la aplicación
```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish
```

### 1.2 Crear paquete para USB
```bash
cd ..
mkdir usb-deployment
cp -r FarmaciaSolidariaCristiana/publish usb-deployment/farmacia-app
cp setup-ubuntu.sh usb-deployment/
cp update-app.sh usb-deployment/
cp DEPLOYMENT_UBUNTU.md usb-deployment/
```

### 1.3 Copiar a USB
```bash
# Identificar tu USB (generalmente /Volumes/TU_USB)
ls /Volumes/

# Copiar archivos
cp -r usb-deployment/* /Volumes/TU_USB/farmacia/
```

---

## 🖥️ Paso 2: En el Servidor Ubuntu

### 2.1 Montar USB
```bash
# Ver dispositivos conectados
lsblk

# Montar USB (usualmente /dev/sdb1 o /dev/sdc1)
sudo mkdir -p /mnt/usb
sudo mount /dev/sdb1 /mnt/usb
```

### 2.2 Copiar archivos
```bash
cp -r /mnt/usb/farmacia/* ~/
cd ~
```

### 2.3 Ejecutar instalación
```bash
# Copiar archivos a directorio de aplicación
sudo mkdir -p /var/www/farmacia
sudo cp -r ~/farmacia-app/* /var/www/farmacia/

# Dar permisos
sudo chown -R www-data:www-data /var/www/farmacia
```

### 2.4 Instalar .NET 9
```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-9.0 aspnetcore-runtime-9.0
```

### 2.5 Instalar Nginx
```bash
sudo apt install -y nginx
sudo systemctl start nginx
sudo systemctl enable nginx
```

### 2.6 Crear servicio systemd
```bash
sudo nano /etc/systemd/system/farmacia.service
```

Pegar:
```ini
[Unit]
Description=Farmacia Solidaria Cristiana
After=network.target

[Service]
WorkingDirectory=/var/www/farmacia
ExecStart=/usr/bin/dotnet /var/www/farmacia/FarmaciaSolidariaCristiana.dll --urls="http://localhost:5000"
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=farmacia-app
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Guardar con `Ctrl+O`, `Enter`, `Ctrl+X`

### 2.7 Configurar Nginx
```bash
sudo nano /etc/nginx/sites-available/farmacia
```

Pegar:
```nginx
server {
    listen 80;
    server_name 192.168.2.113 MPMESCRITORIO;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    client_max_body_size 10M;
}
```

Habilitar sitio:
```bash
sudo ln -s /etc/nginx/sites-available/farmacia /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl restart nginx
```

### 2.8 Iniciar aplicación
```bash
sudo systemctl daemon-reload
sudo systemctl enable farmacia.service
sudo systemctl start farmacia.service
sudo systemctl status farmacia.service
```

### 2.9 Configurar firewall
```bash
sudo ufw allow 80/tcp
sudo ufw allow 22/tcp
```

---

## ✅ Verificar

Desde tu Mac, abre el navegador:
- http://192.168.2.113

---

## 🔄 Futuras Actualizaciones con USB

1. **En tu Mac:**
```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish
cp -r ./publish/* /Volumes/TU_USB/farmacia-update/
```

2. **En Ubuntu:**
```bash
# Montar USB
sudo mount /dev/sdb1 /mnt/usb

# Detener servicio
sudo systemctl stop farmacia.service

# Hacer respaldo
sudo cp -r /var/www/farmacia /var/www/farmacia-backup-$(date +%Y%m%d)

# Actualizar archivos
sudo rm -rf /var/www/farmacia/*
sudo cp -r /mnt/usb/farmacia-update/* /var/www/farmacia/
sudo chown -R www-data:www-data /var/www/farmacia

# Reiniciar servicio
sudo systemctl start farmacia.service
```

---

## 🔓 Habilitar SSH para Futuros Despliegues

**Mientras estés en Ubuntu:**

```bash
# Instalar SSH
sudo apt update
sudo apt install -y openssh-server

# Iniciar SSH
sudo systemctl start ssh
sudo systemctl enable ssh

# Permitir en firewall
sudo ufw allow 22/tcp

# Verificar
sudo systemctl status ssh
```

Después podrás usar `scp` y `rsync` desde tu Mac.
