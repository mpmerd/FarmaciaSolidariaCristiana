-- ============================================================
-- FIX: Paciente con carnet 68041401061
-- Ejecutar DESPUES de revisar el resultado del diagnostico.
-- Logica segura:
--   * Si ya existe un paciente ACTIVO con ese carnet: NO hace nada.
--   * Si existe un solo paciente INACTIVO (eliminado) con ese carnet:
--     lo reactiva para que aparezca en la app y en la web.
--   * Si hay varias filas con ese carnet (duplicados): NO hace nada
--     automaticamente, avisa para decidir manualmente.
-- ============================================================

DECLARE @Carnet varchar(20) = '68041401061';
DECLARE @Total int, @Activos int;

SELECT @Total = COUNT(*),
       @Activos = SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END)
FROM Patients
WHERE IdentificationDocument = @Carnet;

PRINT 'Filas con ese carnet: ' + CAST(@Total AS varchar(10));
PRINT 'Filas activas: ' + CAST(ISNULL(@Activos, 0) AS varchar(10));

IF @Total > 1
BEGIN
    PRINT 'ATENCION: hay DUPLICADOS con ese carnet. NO se corrige automaticamente.';
    PRINT 'Revise los Ids y datos a continuacion y decida cual fila conservar.';
    SELECT Id, IdentificationDocument, FullName, IsActive,
           CAST(RegistrationDate AS varchar(30)) AS RegistrationDate
    FROM Patients
    WHERE IdentificationDocument = @Carnet
    ORDER BY IsActive DESC, Id;
    RETURN;
END

IF @Activos = 1
BEGIN
    PRINT 'El paciente ya esta ACTIVO. No se necesita correccion de datos.';
    RETURN;
END

IF @Total = 1 AND ISNULL(@Activos, 0) = 0
BEGIN
    PRINT 'Paciente inactivo encontrado. REACTIVANDO...';
    UPDATE Patients
    SET IsActive = 1,
        IsBlockedByLoan = 0,
        LoanBlockDescription = NULL
    WHERE IdentificationDocument = @Carnet AND IsActive = 0;

    PRINT 'Paciente reactivado. Verificar:';
    SELECT Id, IdentificationDocument, FullName, IsActive
    FROM Patients
    WHERE IdentificationDocument = @Carnet;
    RETURN;
END

PRINT 'No se encontro ninguna fila con ese carnet. No se hizo ningun cambio.';
