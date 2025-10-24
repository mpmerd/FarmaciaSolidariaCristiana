# ✅ Checklist de Migración - Preservación de Usuarios

## Antes de Empezar

- [ ] He leído `GUIA_MIGRACION_SEGURA.md` completamente
- [ ] Tengo acceso al panel de Somee.com o SQL Server Management Studio
- [ ] Tengo los archivos `backup-users-real.sql` y `restore-users-real.sql` listos
- [ ] He verificado que estos archivos NO están en Git:
  ```bash
  git status | grep -E "(backup-users|restore-users)"
  # No debe aparecer nada
  ```

## Fase 1: BACKUP (Antes de Migración)

- [ ] **Paso 1.1**: Conectar a la base de datos de PRODUCCIÓN
- [ ] **Paso 1.2**: Abrir y ejecutar `backup-users-real.sql`
- [ ] **Paso 1.3**: Copiar TODO el output de las consultas SELECT
- [ ] **Paso 1.4**: Abrir `restore-users-real.sql`
- [ ] **Paso 1.5**: Buscar la línea que dice:
  ```sql
  -- ==================== PEGA AQUÍ LOS INSERT STATEMENTS ====================
  ```
- [ ] **Paso 1.6**: Pegar el output copiado DESPUÉS de esa línea
- [ ] **Paso 1.7**: Guardar `restore-users-real.sql`
- [ ] **Paso 1.8**: Verificar que el archivo tiene contenido (debe pesar más que antes)
- [ ] **Paso 1.9**: Hacer una copia de seguridad adicional del archivo:
  ```bash
  cp restore-users-real.sql restore-users-real-$(date +%Y%m%d).sql.bak
  ```

## Fase 2: MIGRACIÓN (Aplicar Cambios)

### Opción A: Entorno Local

- [ ] **Paso 2.1**: Abrir terminal en la raíz del proyecto
- [ ] **Paso 2.2**: Crear la migración:
  ```bash
  dotnet ef migrations add AddPatientIdentificationRequired
  ```
- [ ] **Paso 2.3**: Revisar los archivos generados en `Migrations/`
- [ ] **Paso 2.4**: Aplicar la migración:
  ```bash
  dotnet ef database update
  ```
- [ ] **Paso 2.5**: Verificar que no haya errores en la consola

### Opción B: Entorno de Producción (Somee)

- [ ] **Paso 2.1**: Generar migración localmente (ver Opción A, pasos 2.2-2.3)
- [ ] **Paso 2.2**: Convertir migración a script SQL
- [ ] **Paso 2.3**: Ejecutar script SQL en panel de Somee
- [ ] **Paso 2.4**: Verificar que la tabla `Patients` tiene el nuevo esquema

## Fase 3: RESTAURACIÓN (Después de Migración)

- [ ] **Paso 3.1**: Conectar a la base de datos (ya migrada)
- [ ] **Paso 3.2**: Ejecutar `restore-users-real.sql` COMPLETO
- [ ] **Paso 3.3**: Revisar el output del script - debe mostrar:
  - Usuarios eliminados
  - Usuarios insertados
  - Roles asignados
  - Lista final de usuarios

## Fase 4: VERIFICACIÓN

- [ ] **Paso 4.1**: Verificar usuario admin existe:
  ```sql
  SELECT * FROM AspNetUsers WHERE UserName = 'admin';
  ```
- [ ] **Paso 4.2**: Verificar usuarios reales restaurados:
  ```sql
  SELECT 
      u.UserName,
      r.Name AS Rol,
      u.Email
  FROM AspNetUsers u
  LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
  LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
  ORDER BY u.UserName;
  ```
- [ ] **Paso 4.3**: Contar usuarios totales:
  ```sql
  SELECT COUNT(*) FROM AspNetUsers;
  ```
  Debe coincidir con el número de usuarios que tenías antes + admin
  
- [ ] **Paso 4.4**: Probar login con admin:
  - Usuario: `admin`
  - Contraseña: `Admin123!`
  
- [ ] **Paso 4.5**: Probar login con al menos 2 usuarios reales
- [ ] **Paso 4.6**: Verificar que cada usuario tiene su rol correcto
- [ ] **Paso 4.7**: Verificar nueva funcionalidad:
  - Campo "Carnet de Identidad o Pasaporte" es obligatorio
  - Validación de formato (11 dígitos o letra+6-7 dígitos)

## Fase 5: LIMPIEZA

- [ ] **Paso 5.1**: Compilar el proyecto:
  ```bash
  dotnet build
  ```
- [ ] **Paso 5.2**: Ejecutar la aplicación:
  ```bash
  dotnet run
  ```
- [ ] **Paso 5.3**: Probar crear un nuevo paciente con el campo de identificación
- [ ] **Paso 5.4**: Verificar que funciona la búsqueda por identificación
- [ ] **Paso 5.5**: (Opcional) Eliminar backups si todo funciona:
  ```bash
  # SOLO si estás seguro que todo funciona
  rm restore-users-real-*.sql.bak
  ```

## 🚨 En Caso de Problemas

### Si perdiste usuarios:
1. [ ] NO entres en pánico
2. [ ] Verifica que `restore-users-real.sql` tiene los INSERT statements
3. [ ] Ejecuta nuevamente `restore-users-real.sql`
4. [ ] Si no funciona, revisa el archivo .bak que creaste

### Si no puedes hacer login:
1. [ ] Intenta con usuario `admin` y contraseña `Admin123!`
2. [ ] Si no funciona, ejecuta:
   ```sql
   -- Resetear contraseña de admin
   UPDATE AspNetUsers 
   SET PasswordHash = 'AQAAAAIAAYagAAAAEBzJvP7P3...' -- Hash de Admin123!
   WHERE UserName = 'admin';
   ```

### Si hay errores de validación:
1. [ ] Verifica el formato del carnet/pasaporte
2. [ ] 11 dígitos para carnet: `12345678901`
3. [ ] Letra + 6-7 dígitos para pasaporte: `A123456` o `B1234567`

## 📝 Notas Finales

- [ ] He documentado cualquier problema encontrado
- [ ] He verificado que los archivos SQL NO están en Git
- [ ] He hecho commit de los cambios permitidos:
  ```bash
  git add .
  git status  # Verificar que backup-users-real.sql NO aparece
  git commit -m "feat: Campo de identificación obligatorio y validación para Cuba"
  ```

---

**Completado por**: _______________  
**Fecha**: _______________  
**Resultado**: ✅ Éxito / ❌ Problemas (detallar abajo)

**Notas adicionales**:
```
(Espacio para notas)
```
