# 📌 GUÍA RÁPIDA: LIMPIEZA DE DATOS

## 🎯 Para Cuando Estés Listo a Producción

Actualmente tienes **datos de prueba** en:
- ✅ Pacientes (con `TEMP` en identificación)
- ✅ Medicamentos ficticios
- ✅ Donaciones de prueba
- ✅ Entregas de prueba

## ⏰ Cuándo Ejecutar la Limpieza

Ejecuta `reset-data-keep-users.sql` cuando:
1. ✅ Hayas probado TODAS las funcionalidades
2. ✅ Estés satisfecho con el sistema
3. ✅ Vayas a empezar a registrar pacientes REALES

## 🚀 Pasos Simples

```bash
1. Ve a Somee.com → Manage my DB
2. Abre: reset-data-keep-users.sql
3. Copia TODO el contenido
4. Pega en el editor SQL de Somee
5. Ejecuta (botón "Execute")
6. Verifica que dice: "LIMPIEZA COMPLETADA EXITOSAMENTE"
```

## ✅ Después de Ejecutar

La base de datos quedará así:
- 📊 **Pacientes: 0**
- 💊 **Medicamentos: 0**
- 🎁 **Donaciones: 0**
- 📦 **Entregas: 0**
- 📄 **Documentos: 0**
- 🏢 **Patrocinadores: (los actuales)** ← ✅ Preservados
- 👥 **Usuarios: 8** ← ✅ Preservados

## 🔐 Usuarios Preservados

```
✓ admin
✓ equipo
✓ idalmis
✓ pruebamia
✓ adriano
✓ Joel
✓ perica
✓ susej
```

## 📝 Próximos Pasos Después de Limpiar

1. Inicia sesión con tu usuario
2. Ingresa medicamentos reales del inventario
3. Verifica que tus patrocinadores siguen ahí (se preservaron)
4. Empieza a registrar pacientes con carnets reales
5. Registra entregas reales

## 🛡️ Seguridad

- ✅ Los usuarios **NUNCA** se eliminan
- ✅ Los patrocinadores **NUNCA** se eliminan (son datos reales)
- ✅ El script está protegido (no se sube a GitHub)
- ✅ Puedes ejecutarlo múltiples veces sin problemas

## 📞 Dudas

Si no estás seguro, **NO lo ejecutes todavía**.  
Los datos de prueba pueden quedarse todo el tiempo que necesites.

---

**Archivos:**
- Script SQL: `reset-data-keep-users.sql`
- Documentación: `LIMPIEZA_DATOS.md`

**Por ahora:** Sigue probando con los datos actuales. Cuando estés listo, ejecutas el script.
