# 🚨 Solución: SSH Connection Refused

## ❌ Problema
```
ssh: connect to host 192.168.2.113 port 22: Connection refused
```

El puerto SSH (22) está **filtrado/bloqueado** en el servidor Ubuntu.

---

## ✅ Soluciones (Elige una)

### 🔧 Solución 1: Habilitar SSH en Ubuntu (RECOMENDADO)

**Necesitas acceso físico al servidor o VNC/RDP**

1. **Conecta monitor y teclado al servidor Ubuntu** o accede por consola

2. **Ejecuta estos comandos en Ubuntu:**
   ```bash
   # Instalar OpenSSH Server
   sudo apt update
   sudo apt install -y openssh-server
   
   # Iniciar servicio SSH
   sudo systemctl start ssh
   sudo systemctl enable ssh
   
   # Permitir en firewall
   sudo ufw allow 22/tcp
   
   # Verificar que está corriendo
   sudo systemctl status ssh
   ```

3. **Prueba la conexión desde tu Mac:**
   ```bash
   ssh usuario@192.168.2.113
   ```

4. **Si funciona, ejecuta el despliegue:**
   ```bash
   cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana
   scp setup-ubuntu.sh usuario@192.168.2.113:~/
   rsync -avz --progress ./FarmaciaSolidariaCristiana/publish/ usuario@192.168.2.113:~/farmacia-files/
   ssh usuario@192.168.2.113
   bash setup-ubuntu.sh
   ```

---

### 💾 Solución 2: Despliegue por USB (Sin SSH)

**Si no puedes habilitar SSH temporalmente**

#### En tu Mac:

1. **Preparar paquete para USB:**
   ```bash
   cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana
   bash prepare-usb-package.sh
   ```

2. **Copiar a USB:**
   ```bash
   # Conecta tu USB y verifica el nombre
   ls /Volumes/
   
   # Copia el paquete (reemplaza "TU_USB" con el nombre real)
   cp -r usb-deployment /Volumes/TU_USB/farmacia/
   
   # O usa el archivo comprimido
   cp farmacia-deployment.tar.gz /Volumes/TU_USB/
   ```

3. **Expulsa USB y llévala al servidor**

#### En el servidor Ubuntu:

1. **Montar USB:**
   ```bash
   # Ver dispositivos
   lsblk
   
   # Montar (usualmente /dev/sdb1)
   sudo mkdir -p /mnt/usb
   sudo mount /dev/sdb1 /mnt/usb
   ```

2. **Copiar archivos:**
   ```bash
   cp -r /mnt/usb/farmacia/* ~/
   cd ~
   ```

3. **Seguir instrucciones en:**
   ```bash
   cat ~/DEPLOYMENT_MANUAL_USB.md
   ```

---

### 🌐 Solución 3: Compartir Carpeta de Red

**Si tu Mac y Ubuntu están en la misma red**

#### En Ubuntu:

1. **Instalar Samba:**
   ```bash
   sudo apt update
   sudo apt install -y samba
   ```

2. **Crear carpeta compartida:**
   ```bash
   mkdir -p ~/farmacia-shared
   sudo nano /etc/samba/smb.conf
   ```
   
   Agregar al final:
   ```ini
   [farmacia]
   path = /home/usuario/farmacia-shared
   browseable = yes
   writable = yes
   guest ok = yes
   ```

3. **Reiniciar Samba:**
   ```bash
   sudo systemctl restart smbd
   sudo ufw allow samba
   ```

#### En tu Mac:

1. **Conectar a la carpeta compartida:**
   ```bash
   # En Finder: Cmd+K
   # Conectar a: smb://192.168.2.113/farmacia
   ```

2. **Copiar archivos:**
   Arrastra la carpeta `usb-deployment` a la carpeta compartida

3. **En Ubuntu, continuar con instalación**

---

## 🔍 Diagnóstico Adicional

### Verificar si hay otro servicio SSH en otro puerto:

```bash
nmap -p 1-65535 192.168.2.113
```

### Ver qué puertos están abiertos:

```bash
sudo netstat -tuln | grep LISTEN
```

### Verificar firewall en Ubuntu:

```bash
sudo ufw status
```

---

## 📞 ¿Cuál usar?

| Solución | Dificultad | Requiere | Tiempo |
|----------|------------|----------|--------|
| **Habilitar SSH** | ⭐ Fácil | Acceso físico a Ubuntu | 5 min |
| **USB** | ⭐⭐ Media | USB + Acceso físico | 15 min |
| **Carpeta Red** | ⭐⭐⭐ Avanzada | Configurar Samba | 20 min |

**Recomendación:** Usa **Solución 1** (Habilitar SSH) - es la más simple y te servirá para futuras actualizaciones.

---

## 📱 Contacto

Si tienes acceso remoto al servidor (VNC, TeamViewer, etc.), puedes ejecutar los comandos remotamente sin acceso físico.

---

## ✅ Una vez resuelto

Cuando SSH funcione, ejecuta:

```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana
bash prepare-usb-package.sh  # Solo si usas USB
# O directamente:
scp setup-ubuntu.sh usuario@192.168.2.113:~/
rsync -avz --progress ./FarmaciaSolidariaCristiana/publish/ usuario@192.168.2.113:~/farmacia-files/
ssh usuario@192.168.2.113
bash setup-ubuntu.sh
```

¡Y listo! Tu aplicación estará corriendo en http://192.168.2.113 🚀
