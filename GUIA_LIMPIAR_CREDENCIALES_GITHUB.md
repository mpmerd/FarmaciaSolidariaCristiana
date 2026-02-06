# Guía para Eliminar Credenciales Expuestas en GitHub

## ✅ COMPLETADO - Historial Limpiado Exitosamente

**Fecha de limpieza**: 6 de febrero de 2026

### Acciones realizadas:
- ✅ `google-services.json` agregado al `.gitignore`
- ✅ Creado archivo template sin credenciales
- ✅ Archivo removido del índice de Git
- ✅ Commit creado con los cambios
- ✅ **Historial de Git completamente limpiado con BFG Repo-Cleaner**
- ✅ **Push forzado a GitHub en todas las ramas (main y developerConApi)**
- ✅ Backup creado del repositorio antes de la limpieza

### Resultado:
El archivo `google-services.json` ha sido **eliminado completamente** de:
- ✅ Rama `main` en GitHub
- ✅ Rama `developerConApi` en GitHub
- ✅ Todo el historial de commits
- ✅ El commit reportado por Google (2bdf1391) ya no existe

### Detalles técnicos de la limpieza:
```bash
# Herramienta utilizada: BFG Repo-Cleaner
# Commits procesados: 188
# Objeto eliminado: google-services.json (694 B)
# Referencias actualizadas: 4 (developerConApi, main, y sus remotos)
```

## ⚠️ PASO CRÍTICO PENDIENTE - ACCIÓN REQUERIDA

**DEBES revocar la API key expuesta INMEDIATAMENTE:**

La API key que estaba expuesta: `AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8`

Aunque el archivo fue eliminado del historial de GitHub, Google y otros servicios ya tienen registros de esta clave. **Debes revocarla para que no pueda ser usada.**

### Cómo revocar la API key:

1. Ve a [Google Cloud Console](https://console.cloud.google.com/)
2. Selecciona el proyecto: `farmaciasolidaria-11ee6`
3. Ve a **APIs & Services > Credentials**
4. Busca la API key: `AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8`
5. Haz clic en **Delete** o **Regenerate**
6. Genera una nueva API key
7. Descarga un nuevo archivo `google-services.json` desde Firebase Console

### Actualizar el archivo local:

```bash
# Copia el nuevo google-services.json descargado de Firebase
cp ~/Downloads/google-services.json FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json

# Verifica que el archivo no se subirá a GitHub (ya está en .gitignore)
git status
```

## 📋 Resumen de lo realizado

### 1. Protección del archivo
- ✅ Agregado `google-services.json` y `GoogleService-Info.plist` al [.gitignore](.gitignore)
- ✅ Creado `google-services.json.template` con valores de ejemplo
- ✅ Archivo real permanece en disco pero Git lo ignora

### 2. Limpieza del historial con BFG
```bash
# Instalado BFG Repo-Cleaner
brew install bfg

# Creado backup del repositorio
FarmaciaSolidariaCristiana-BACKUP-20260206-XXXXXX

# Ejecutado BFG para eliminar archivo del historial
bfg --delete-files google-services.json
# Resultado: 188 commits procesados, 88 objetos modificados

# Limpieza de referencias y garbage collection
git reflog expire --expire=now --all
git gc --prune=now --aggressive
```

### 3. Actualización de GitHub
```bash
# Push forzado de todas las ramas
git push origin developerConApi --force  ✅
git push origin main --force             ✅
```

### 4. Verificación
- ✅ El commit reportado por Google (2bdf1391) ya no existe
- ✅ El archivo no existe en `origin/main`
- ✅ El archivo no existe en `origin/developerConApi`
- ✅ Historial completamente limpio en todas las ramas

## 👥 Instrucciones para otros desarrolladores

Si otros desarrolladores trabajan en el proyecto, deben sincronizar el historial limpio:

```bash
# IMPORTANTE: Advertir al equipo antes de hacer esto
# Esto eliminará commits locales que no existan en el nuevo historial

# 1. Hacer backup de cambios locales importantes
git stash  # si hay cambios sin commitear

# 2. Obtener el nuevo historial limpio
git fetch origin

# 3. Resetear las ramas locales al nuevo historial
git checkout main
git reset --hard origin/main

git checkout developerConApi
git reset --hard origin/developerConApi

# 4. Crear su propio google-services.json desde el template
cp FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json.template \
   FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json

# 5. Editar el archivo con las credenciales nuevas (recibirlas de manera segura)
# Usar el nuevo google-services.json que descargaste de Firebase Console
```

## 🔒 Mejores prácticas futuras

1. **Nunca** commitear archivos con credenciales
2. Siempre usar archivos `.template` o `.example` con valores de ejemplo
3. Agregar archivos sensibles al `.gitignore` ANTES de crearlos
4. Usar variables de entorno para credenciales cuando sea posible
5. Para proyectos móviles, considerar usar diferentes configuraciones de Firebase para desarrollo y producción

## Recursos adicionales

- [Eliminar datos sensibles de un repositorio - GitHub Docs](https://docs.github.com/es/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
- [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/)
- [Firebase Security Best Practices](https://firebase.google.com/docs/projects/api-keys)
