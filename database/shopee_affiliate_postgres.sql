-- Shopee Affiliate Intelligence System (PostgreSQL) - schema
-- Uses gen_random_uuid() via pgcrypto
-- Run in a fresh database. Example:
--   psql -d yourdb -f database/shopee_affiliate_postgres.sql

BEGIN;

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================
-- Types / Enums
-- =========================
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'affiliate_role') THEN
        CREATE TYPE affiliate_role AS ENUM ('affiliate', 'admin');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'conversion_status') THEN
        CREATE TYPE conversion_status AS ENUM ('pending','approved','rejected');
    END IF;
END $$;

-- =========================
-- Users / Auth
-- =========================
CREATE TABLE IF NOT EXISTS app_users (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email                   TEXT NOT NULL UNIQUE,
    password_hash          TEXT NOT NULL, -- store bcrypt/argon2 hash (do NOT store plaintext)
    role                    affiliate_role NOT NULL DEFAULT 'affiliate',
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_app_users_role_created_at
ON app_users(role, created_at);

-- =========================
-- Products (from feeds / partner API)
-- =========================
CREATE TABLE IF NOT EXISTS products (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id_on_platform TEXT NOT NULL,
    name                    TEXT NOT NULL,
    price                   NUMERIC(12,2) NOT NULL,
    original_price          NUMERIC(12,2) NOT NULL,
    commission_rate         NUMERIC(4,4) NOT NULL, -- e.g. 0.05 for 5%
    category                TEXT NOT NULL,

    review_count           INT NOT NULL DEFAULT 0,
    rating                 NUMERIC(2,1) NOT NULL DEFAULT 0,
    sales_volume          INT NULL,

    image_url               TEXT NULL,

    data_source             TEXT NOT NULL, -- 'affiliate_feed', 'partner_api'

    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_products_platform_id UNIQUE (product_id_on_platform)
);

CREATE INDEX IF NOT EXISTS idx_products_category
ON products(category);

CREATE INDEX IF NOT EXISTS idx_products_commission_rate
ON products(commission_rate);

-- =========================
-- Affiliate Links
-- =========================
CREATE TABLE IF NOT EXISTS affiliate_links (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id             UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,

    original_url           TEXT NOT NULL,

    short_code             VARCHAR(10) NOT NULL UNIQUE,
    short_url              TEXT NOT NULL,

    created_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_affiliate_links_product UNIQUE (product_id)
);

CREATE INDEX IF NOT EXISTS idx_affiliate_links_product
ON affiliate_links(product_id);

CREATE INDEX IF NOT EXISTS idx_affiliate_links_short_code
ON affiliate_links(short_code);

-- =========================
-- Clicks (tracking)
-- =========================
CREATE TABLE IF NOT EXISTS clicks (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    link_id                 UUID NOT NULL REFERENCES affiliate_links(id) ON DELETE CASCADE,
    product_id             UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,

    timestamp              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip                      INET NULL,
    user_agent              TEXT NULL,
    traffic_source         TEXT NULL, -- from UTM or referrer classification

    converted               BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_clicks_product_timestamp
ON clicks(product_id, timestamp);

CREATE INDEX IF NOT EXISTS idx_clicks_link_timestamp
ON clicks(link_id, timestamp);

CREATE INDEX IF NOT EXISTS idx_clicks_converted_timestamp
ON clicks(converted, timestamp);

-- =========================
-- Conversions (postback)
-- =========================
CREATE TABLE IF NOT EXISTS conversions (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    click_id                UUID NOT NULL REFERENCES clicks(id) ON DELETE CASCADE,

    order_id               TEXT NOT NULL,
    commission             NUMERIC(10,2) NOT NULL DEFAULT 0,
    status                  conversion_status NOT NULL DEFAULT 'pending',

    recorded_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Prevent duplicate conversion records for the same order_id
    CONSTRAINT uq_conversions_order UNIQUE (order_id)
);

CREATE INDEX IF NOT EXISTS idx_conversions_click_id
ON conversions(click_id);

CREATE INDEX IF NOT EXISTS idx_conversions_status_recorded_at
ON conversions(status, recorded_at);

-- =========================
-- Recommendations / Scoring Output
-- =========================
CREATE TABLE IF NOT EXISTS daily_recommendations (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id             UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,

    score                   DOUBLE PRECISION NOT NULL,
    recommendation_date    DATE NOT NULL,

    weight_breakdown       JSONB NULL, -- e.g. {popularity:..., deal:..., commission:..., recency:...}

    created_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_daily_recommendations UNIQUE (product_id, recommendation_date)
);

CREATE INDEX IF NOT EXISTS idx_daily_recommendations_date_score
ON daily_recommendations(recommendation_date, score DESC);

CREATE INDEX IF NOT EXISTS idx_daily_recommendations_product_date
ON daily_recommendations(product_id, recommendation_date);

-- =========================
-- Scoring Settings (admin-configurable weights)
-- =========================
CREATE TABLE IF NOT EXISTS scoring_settings (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- Store weights/breakdown in JSONB so you can evolve rule structure
    weights                 JSONB NOT NULL,

    -- Optional: which date range / schedule they apply to
    active_from            DATE NULL,
    active_to              DATE NULL,

    created_by_user_id     UUID NULL REFERENCES app_users(id) ON DELETE SET NULL,
    created_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at             TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Usually you want exactly one active set, but enforce via app logic (keeps SQL simple).

-- =========================
-- Affiliate Network Credentials (admin/user)
-- =========================
CREATE TABLE IF NOT EXISTS affiliate_credentials (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_user_id          UUID NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,

    network_name           TEXT NOT NULL, -- e.g. 'involve_asia', 'access_trade'
    api_key_encrypted      BYTEA NOT NULL, -- encrypted by application (e.g. AES-GCM or DataProtection)
    api_secret_encrypted   BYTEA NULL,

    created_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_affiliate_credentials_owner_network UNIQUE (owner_user_id, network_name)
);

CREATE INDEX IF NOT EXISTS idx_affiliate_credentials_owner
ON affiliate_credentials(owner_user_id);

-- =========================
-- Optional: Traffic Source Classification (if you want later)
-- =========================
-- CREATE TABLE IF NOT EXISTS traffic_sources ( ... );

-- =========================
-- Useful Trigger: updated_at
-- =========================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Attach to tables that have updated_at
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='products' AND column_name='updated_at') THEN
    CREATE TRIGGER trg_products_updated_at
    BEFORE UPDATE ON products
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
  END IF;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='affiliate_links' AND column_name='updated_at') THEN
    CREATE TRIGGER trg_affiliate_links_updated_at
    BEFORE UPDATE ON affiliate_links
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
  END IF;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='app_users' AND column_name='updated_at') THEN
    CREATE TRIGGER trg_app_users_updated_at
    BEFORE UPDATE ON app_users
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
  END IF;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='scoring_settings' AND column_name='updated_at') THEN
    CREATE TRIGGER trg_scoring_settings_updated_at
    BEFORE UPDATE ON scoring_settings
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
  END IF;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='affiliate_credentials' AND column_name='updated_at') THEN
    CREATE TRIGGER trg_affiliate_credentials_updated_at
    BEFORE UPDATE ON affiliate_credentials
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
  END IF;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

COMMIT;

-- End of schema
