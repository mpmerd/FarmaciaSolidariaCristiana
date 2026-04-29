-- Migration: AddPatientLoanBlock
-- Ejecutar contra la base de datos de producción/staging
-- Fecha: 2026-04-29

ALTER TABLE Patients ADD IsBlockedByLoan bit NOT NULL DEFAULT 0;
ALTER TABLE Patients ADD LoanBlockDate datetime2 NULL;
ALTER TABLE Patients ADD LoanBlockDescription nvarchar(500) NULL;
ALTER TABLE Patients ADD LoanUnblockDate datetime2 NULL;
ALTER TABLE Patients ADD LoanUnblockedByUserId nvarchar(450) NULL;

CREATE INDEX IX_Patients_LoanUnblockedByUserId ON Patients (LoanUnblockedByUserId);

ALTER TABLE Patients
    ADD CONSTRAINT FK_Patients_AspNetUsers_LoanUnblockedByUserId
    FOREIGN KEY (LoanUnblockedByUserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL;
