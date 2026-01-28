-- Script para sincronizar el historial de migraciones en la BD local
-- La BD ya tiene las tablas, pero EF no sabe qué migraciones se aplicaron
-- Este script inserta los registros faltantes en __EFMigrationsHistory

-- Primero verificar qué migraciones ya están registradas
SELECT * FROM [__EFMigrationsHistory] ORDER BY MigrationId;

-- Insertar las migraciones que faltan (solo si no existen)
-- Usar la versión de EF Core que estás usando (8.0.x)

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251020155103_InitialCreate')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251020155103_InitialCreate', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251021154557_AddPatientAndDocuments')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251021154557_AddPatientAndDocuments', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251021232654_AddSponsorsTable')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251021232654_AddSponsorsTable', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251023213325_AddPatientIdentificationRequired')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251023213325_AddPatientIdentificationRequired', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251023225202_AddDeliveryFieldsEnhancement')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251023225202_AddDeliveryFieldsEnhancement', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251025212114_AddCreatedAtToDeliveries')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251025212114_AddCreatedAtToDeliveries', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251027160229_AddSuppliesTable')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251027160229_AddSuppliesTable', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251027164041_AddSupplyToDeliveries')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251027164041_AddSupplyToDeliveries', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251027171452_AddSupplyToDonations')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251027171452_AddSupplyToDonations', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251031141709_AddTurnosSystem')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251031141709_AddTurnosSystem', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251031190210_MakeFechaPreferidaNullable')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251031190210_MakeFechaPreferidaNullable', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251031224145_AddTurnoInsumos')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251031224145_AddTurnoInsumos', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251103154930_AddFechasBloqueadas')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251103154930_AddFechasBloqueadas', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251104004321_AddTurnoIdToDeliveries')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251104004321_AddTurnoIdToDeliveries', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20251113150644_AddNavbarDecorations')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20251113150644_AddNavbarDecorations', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20260115043112_AddTurnoDocumentos')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion) VALUES ('20260115043112_AddTurnoDocumentos', '8.0.0');

-- Verificar el resultado
SELECT * FROM [__EFMigrationsHistory] ORDER BY MigrationId;

PRINT 'Migraciones sincronizadas correctamente';
