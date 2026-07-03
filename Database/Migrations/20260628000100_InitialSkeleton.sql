-- ZPantry skeleton migration for PostgreSQL.
-- This file is intentionally lightweight and mirrors the SSOT entity set.
-- TODO: Generate EF Core migrations once the domain stabilizes.

CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    full_name varchar(150) NULL,
    email varchar(200) NOT NULL UNIQUE,
    avatar_url varchar(500) NULL,
    password_hashed varchar(500) NOT NULL,
    otp_code varchar(6) NULL,
    otp_expired_at timestamptz NULL,
    otp_retry_count integer NOT NULL DEFAULT 0,
    is_email_confirmed boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT false,
    role varchar(50) NOT NULL DEFAULT 'user',
    refresh_token_hash varchar(128) NULL,
    refresh_token_expires_at timestamptz NULL
);

CREATE TABLE IF NOT EXISTS ingredients (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    name varchar(200) NOT NULL,
    normalized_name varchar(200) NOT NULL,
    category varchar(100) NULL,
    unit varchar(50) NULL,
    calories_per_unit numeric(18, 4) NULL,
    protein_per_unit numeric(18, 4) NULL,
    fat_per_unit numeric(18, 4) NULL,
    carb_per_unit numeric(18, 4) NULL,
    image_url varchar(500) NULL,
    embedding real[] NULL
);

CREATE TABLE IF NOT EXISTS ingredient_aliases (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    ingredient_id uuid NOT NULL,
    alias_name varchar(200) NOT NULL,
    normalized_alias_name varchar(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS recipes (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    name varchar(200) NOT NULL,
    description text NULL,
    cooking_time_minutes integer NULL,
    difficulty varchar(50) NULL,
    serving_size integer NULL,
    instruction_text text NULL,
    image_url varchar(500) NULL,
    source_type varchar(100) NULL,
    embedding real[] NULL
);

CREATE TABLE IF NOT EXISTS recipe_ingredients (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    recipe_id uuid NOT NULL,
    ingredient_id uuid NOT NULL,
    quantity numeric(18, 4) NULL,
    unit varchar(50) NULL,
    is_required boolean NOT NULL DEFAULT true,
    note text NULL
);

CREATE TABLE IF NOT EXISTS user_pantry_items (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    user_id uuid NOT NULL,
    ingredient_id uuid NOT NULL,
    quantity numeric(18, 4) NULL,
    unit varchar(50) NULL,
    expired_at timestamptz NULL,
    storage_location varchar(100) NULL,
    note text NULL
);

CREATE TABLE IF NOT EXISTS meal_recommendations (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    user_id uuid NOT NULL,
    request_text text NULL,
    input_ingredient_text text NULL,
    recommendation_type varchar(100) NULL,
    status varchar(100) NULL,
    completed_at timestamptz NULL
);

CREATE TABLE IF NOT EXISTS meal_recommendation_items (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    meal_recommendation_id uuid NOT NULL,
    recipe_id uuid NOT NULL,
    match_score numeric(18, 4) NULL,
    missing_ingredient_count integer NOT NULL DEFAULT 0,
    missing_ingredient_names text NULL,
    reason text NULL,
    rank integer NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS recommendation_feedbacks (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    user_id uuid NOT NULL,
    meal_recommendation_id uuid NOT NULL,
    recipe_id uuid NOT NULL,
    rating integer NULL,
    feedback_type varchar(100) NULL,
    comment text NULL
);

CREATE TABLE IF NOT EXISTS media_assets (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    recipe_id uuid NULL,
    ingredient_id uuid NULL,
    public_id varchar(200) NOT NULL,
    url varchar(500) NOT NULL,
    secure_url varchar(500) NOT NULL,
    resource_type varchar(50) NULL,
    format varchar(50) NULL,
    width integer NULL,
    height integer NULL
);

CREATE TABLE IF NOT EXISTS today_menu_items (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    user_id uuid NOT NULL,
    meal_id uuid NULL,
    recipe_id uuid NULL,
    meal_name varchar(200) NOT NULL,
    meal_type varchar(100) NULL,
    serving_size integer NULL,
    planned_date date NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Planned',
    note text NULL,
    cooked_at timestamptz NULL,
    image_url varchar(500) NULL,
    image_public_id varchar(200) NULL
);

CREATE TABLE IF NOT EXISTS cooking_logs (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    user_id uuid NOT NULL,
    today_menu_item_id uuid NOT NULL,
    meal_id uuid NULL,
    recipe_id uuid NULL,
    meal_name varchar(200) NOT NULL,
    image_url varchar(500) NULL,
    image_public_id varchar(200) NULL,
    cooked_at timestamptz NOT NULL,
    rating integer NULL,
    note text NULL
);

CREATE TABLE IF NOT EXISTS pantry_usage_logs (
    id uuid PRIMARY KEY,
    created_at timestamptz NOT NULL,
    created_by uuid NULL,
    updated_at timestamptz NULL,
    updated_by uuid NULL,
    deleted_at timestamptz NULL,
    deleted_by uuid NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    user_id uuid NOT NULL,
    today_menu_item_id uuid NOT NULL,
    cooking_log_id uuid NOT NULL,
    ingredient_id uuid NOT NULL,
    ingredient_name varchar(200) NOT NULL,
    quantity_used numeric(18, 4) NULL,
    unit varchar(50) NULL,
    action_type varchar(50) NOT NULL DEFAULT 'consumed',
    warning text NULL
);
