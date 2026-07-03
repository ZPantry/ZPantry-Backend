-- =========================================================================================
-- PostgreSQL Database & Table Script Converted from MSSQL (Db.sql)
-- Database: ZPantryDb
-- Table: public."users"
-- =========================================================================================

-- 1. DATABASE CREATION
-- Note: In PostgreSQL, CREATE DATABASE cannot be executed inside a transaction block.
-- When running via psql or query tool, execute this section separately if ZPantryDb does not exist.
-- DROP DATABASE IF EXISTS "ZPantryDb";

CREATE DATABASE "ZPantryDb"
    WITH 
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    CONNECTION LIMIT = -1;

-- Connect to the ZPantryDb database (psql command)
-- If running inside pgAdmin / DBeaver, select the ZPantryDb connection before running below commands.
\c "ZPantryDb"

-- Set standard database runtime parameters
ALTER DATABASE "ZPantryDb" SET timezone TO 'UTC';
ALTER DATABASE "ZPantryDb" SET client_encoding TO 'UTF8';
ALTER DATABASE "ZPantryDb" SET standard_conforming_strings TO on;

-- Enable extension for UUID generation functions
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS vector;

-- =========================================================================================
-- 2. TABLE CREATION: public."users"
-- =========================================================================================

CREATE TABLE IF NOT EXISTS public."users" (
    "Id" UUID NOT NULL,
    "FullName" VARCHAR(150) NULL,
    "Email" VARCHAR(200) NOT NULL,
    "PasswordHashed" VARCHAR(500) NOT NULL,
    "CreatedAt" TIMESTAMP(6) WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "OtpCode" VARCHAR(6) NULL,
    "OtpExpiredAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "OtpRetryCount" INTEGER NOT NULL,
    "IsEmailConfirmed" BOOLEAN NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "Role" VARCHAR(50) NOT NULL,
    "RefreshTokenHash" VARCHAR(128) NULL,
    "RefreshTokenExpiresAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_users_Email" UNIQUE ("Email")
);

-- =========================================================================================
-- 3. DEFAULT VALUE CONSTRAINTS
-- =========================================================================================

-- Equivalent to SQL Server: DEFAULT (newsequentialid()) FOR [Id]
ALTER TABLE public."users" ALTER COLUMN "Id" SET DEFAULT gen_random_uuid();

-- Equivalent to SQL Server: DEFAULT ((0)) FOR [OtpRetryCount]
ALTER TABLE public."users" ALTER COLUMN "OtpRetryCount" SET DEFAULT 0;

-- Equivalent to SQL Server: DEFAULT ((0)) FOR [IsEmailConfirmed]
ALTER TABLE public."users" ALTER COLUMN "IsEmailConfirmed" SET DEFAULT false;

-- Equivalent to SQL Server: DEFAULT ((0)) FOR [IsActive]
ALTER TABLE public."users" ALTER COLUMN "IsActive" SET DEFAULT false;

-- Equivalent to SQL Server: CONSTRAINT [DF_users_Role] DEFAULT ('user') FOR [Role]
ALTER TABLE public."users" ALTER COLUMN "Role" SET DEFAULT 'user';

-- Set database access to read/write explicitly
ALTER DATABASE "ZPantryDb" SET default_transaction_read_only = off;

-- =========================================================================================
-- 4. TABLE CREATION: today menu, cooking logs, pantry usage logs
-- =========================================================================================

CREATE TABLE IF NOT EXISTS public."today_menu_items" (
    "Id" UUID NOT NULL,
    "CreatedAt" TIMESTAMP(6) WITH TIME ZONE NOT NULL,
    "CreatedBy" UUID NULL,
    "UpdatedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "UpdatedBy" UUID NULL,
    "DeletedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "DeletedBy" UUID NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "UserId" UUID NOT NULL,
    "MealId" UUID NULL,
    "RecipeId" UUID NULL,
    "MealName" VARCHAR(200) NOT NULL,
    "MealType" VARCHAR(100) NULL,
    "ServingSize" INTEGER NULL,
    "PlannedDate" DATE NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Planned',
    "Note" TEXT NULL,
    "CookedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "ImageUrl" VARCHAR(500) NULL,
    "ImagePublicId" VARCHAR(200) NULL,
    CONSTRAINT "PK_today_menu_items" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS public."cooking_logs" (
    "Id" UUID NOT NULL,
    "CreatedAt" TIMESTAMP(6) WITH TIME ZONE NOT NULL,
    "CreatedBy" UUID NULL,
    "UpdatedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "UpdatedBy" UUID NULL,
    "DeletedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "DeletedBy" UUID NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "UserId" UUID NOT NULL,
    "TodayMenuItemId" UUID NOT NULL,
    "MealId" UUID NULL,
    "RecipeId" UUID NULL,
    "MealName" VARCHAR(200) NOT NULL,
    "ImageUrl" VARCHAR(500) NULL,
    "ImagePublicId" VARCHAR(200) NULL,
    "CookedAt" TIMESTAMP(6) WITH TIME ZONE NOT NULL,
    "Rating" INTEGER NULL,
    "Note" TEXT NULL,
    CONSTRAINT "PK_cooking_logs" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS public."pantry_usage_logs" (
    "Id" UUID NOT NULL,
    "CreatedAt" TIMESTAMP(6) WITH TIME ZONE NOT NULL,
    "CreatedBy" UUID NULL,
    "UpdatedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "UpdatedBy" UUID NULL,
    "DeletedAt" TIMESTAMP(6) WITH TIME ZONE NULL,
    "DeletedBy" UUID NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "UserId" UUID NOT NULL,
    "TodayMenuItemId" UUID NOT NULL,
    "CookingLogId" UUID NOT NULL,
    "IngredientId" UUID NOT NULL,
    "IngredientName" VARCHAR(200) NOT NULL,
    "QuantityUsed" NUMERIC(18, 4) NULL,
    "Unit" VARCHAR(50) NULL,
    "ActionType" VARCHAR(50) NOT NULL DEFAULT 'consumed',
    "Warning" TEXT NULL,
    CONSTRAINT "PK_pantry_usage_logs" PRIMARY KEY ("Id")
);
