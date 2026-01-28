// Script para verificar/crear UserDeviceTokens
using Microsoft.Data.SqlClient;

var connectionString = Environment.GetEnvironmentVariable("SYNC_MIGRATIONS_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing connection string. Set SYNC_MIGRATIONS_CONNECTION_STRING (example: Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;)");
    Environment.Exit(1);
}

using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("Verificando UserDeviceTokens para OneSignal...\n");

// Verificar si la tabla existe
var checkTableSql = "SELECT OBJECT_ID(N'[UserDeviceTokens]')";
using var checkCmd = new SqlCommand(checkTableSql, connection);
var result = await checkCmd.ExecuteScalarAsync();

if (result == DBNull.Value || result == null)
{
    Console.WriteLine("Tabla UserDeviceTokens no existe, creándola...");
    
    var createTableSql = @"
        CREATE TABLE [UserDeviceTokens] (
            [Id] int NOT NULL IDENTITY,
            [UserId] nvarchar(450) NOT NULL,
            [PlayerId] nvarchar(512) NOT NULL,
            [DeviceType] nvarchar(50) NOT NULL,
            [DeviceName] nvarchar(256) NULL,
            [AppVersion] nvarchar(50) NULL,
            [IsActive] bit NOT NULL DEFAULT 1,
            [CreatedAt] datetime2 NOT NULL,
            [LastUsedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_UserDeviceTokens] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_UserDeviceTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
        );
        
        CREATE INDEX [IX_UserDeviceTokens_UserId] ON [UserDeviceTokens] ([UserId]);
        CREATE INDEX [IX_UserDeviceTokens_PlayerId] ON [UserDeviceTokens] ([PlayerId]);
        CREATE UNIQUE INDEX [IX_UserDeviceTokens_UserId_PlayerId] ON [UserDeviceTokens] ([UserId], [PlayerId]);
    ";
    
    using var createCmd = new SqlCommand(createTableSql, connection);
    await createCmd.ExecuteNonQueryAsync();
    Console.WriteLine("✓ Tabla UserDeviceTokens creada exitosamente!");
}
else
{
    Console.WriteLine("✓ Tabla UserDeviceTokens ya existe");
}

Console.WriteLine("\n✓ Operación completada!");
