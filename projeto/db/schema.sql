-- ============================================================================
-- Schema do banco `catalogo` (MySQL 8) — equivalente ao modelo EF Core
-- (CatalogoDbContext). A API cria/migra o schema automaticamente ao subir;
-- este arquivo serve para documentação e para recriar o banco manualmente.
-- ============================================================================
CREATE DATABASE IF NOT EXISTS `catalogo` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `catalogo`;

CREATE TABLE IF NOT EXISTS `categorias` (
  `Id`        INT NOT NULL AUTO_INCREMENT,
  `Nome`      VARCHAR(120) NOT NULL,
  `Slug`      VARCHAR(80)  NOT NULL,
  `Icone`     VARCHAR(80)  NULL,
  `MoodleId`  INT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_categorias_Slug` (`Slug`),
  UNIQUE KEY `IX_categorias_MoodleId` (`MoodleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `modalidades` (
  `Id`    INT NOT NULL AUTO_INCREMENT,
  `Nome`  VARCHAR(80) NOT NULL,
  `Slug`  VARCHAR(40) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_modalidades_Slug` (`Slug`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `status` (
  `Id`    INT NOT NULL AUTO_INCREMENT,
  `Nome`  VARCHAR(80) NOT NULL,
  `Slug`  VARCHAR(40) NOT NULL,
  `Cor`   VARCHAR(9)  NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_status_Slug` (`Slug`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `cursos` (
  `Id`                   INT NOT NULL AUTO_INCREMENT,
  `NomeCompleto`         VARCHAR(200) NOT NULL,
  `NomeBreve`            VARCHAR(60)  NOT NULL,
  `Descricao`            TEXT         NOT NULL,
  `CategoriaId`          INT NOT NULL,
  `ModalidadeId`         INT NOT NULL,
  `StatusId`             INT NOT NULL,
  `CargaHorariaHoras`    INT NOT NULL,
  `PublicoAlvo`          VARCHAR(300) NOT NULL,
  `ImagemUrl`            VARCHAR(500) NOT NULL,
  `DataDisponibilizacao` DATE NULL,
  `MoodleId`             INT NULL,
  `MoodleUrl`            VARCHAR(500) NULL,
  `CriadoEm`             DATETIME(6) NOT NULL,
  `AtualizadoEm`         DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_cursos_NomeBreve` (`NomeBreve`),
  UNIQUE KEY `IX_cursos_MoodleId` (`MoodleId`),
  KEY `IX_cursos_CategoriaId_ModalidadeId_StatusId` (`CategoriaId`,`ModalidadeId`,`StatusId`),
  KEY `IX_cursos_ModalidadeId` (`ModalidadeId`),
  KEY `IX_cursos_StatusId` (`StatusId`),
  CONSTRAINT `FK_cursos_categorias_CategoriaId`   FOREIGN KEY (`CategoriaId`)  REFERENCES `categorias`  (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_cursos_modalidades_ModalidadeId` FOREIGN KEY (`ModalidadeId`) REFERENCES `modalidades` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_cursos_status_StatusId`          FOREIGN KEY (`StatusId`)     REFERENCES `status`      (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `modulos` (
  `Id`                INT NOT NULL AUTO_INCREMENT,
  `CursoId`           INT NOT NULL,
  `Ordem`             INT NOT NULL,
  `Titulo`            VARCHAR(200) NOT NULL,
  `Descricao`         TEXT NOT NULL,
  `CargaHorariaHoras` INT NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_modulos_CursoId_Ordem` (`CursoId`,`Ordem`),
  CONSTRAINT `FK_modulos_cursos_CursoId` FOREIGN KEY (`CursoId`) REFERENCES `cursos` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `depoimentos` (
  `Id`      INT NOT NULL AUTO_INCREMENT,
  `Nome`    VARCHAR(120) NOT NULL,
  `Cargo`   VARCHAR(160) NOT NULL,
  `Texto`   VARCHAR(1000) NOT NULL,
  `Nota`    INT NOT NULL,
  `CursoId` INT NULL,
  `Ordem`   INT NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_depoimentos_CursoId_Ordem` (`CursoId`,`Ordem`),
  CONSTRAINT `FK_depoimentos_cursos_CursoId` FOREIGN KEY (`CursoId`) REFERENCES `cursos` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `indicadores` (
  `Id`     INT NOT NULL AUTO_INCREMENT,
  `Valor`  VARCHAR(40)  NOT NULL,
  `Rotulo` VARCHAR(120) NOT NULL,
  `Ordem`  INT NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Dados fictícios: inseridos automaticamente pela API (Data/DbSeeder.cs)
-- na primeira execução, quando a tabela `cursos` está vazia.
