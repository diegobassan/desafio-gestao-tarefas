CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Tarefas" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Tarefas" PRIMARY KEY,
    "Titulo" TEXT NOT NULL,
    "Descricao" TEXT NULL,
    "Situacao" INTEGER NOT NULL,
    "DataVencimento" TEXT NOT NULL,
    CONSTRAINT "CK_Tarefas_Titulo_Tamanho" CHECK (length("Titulo") BETWEEN 1 AND 150),
    CONSTRAINT "CK_Tarefas_Descricao_Tamanho" CHECK ("Descricao" IS NULL OR length("Descricao") <= 2000)
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261116104610_Inicial', '10.0.15');

COMMIT;

