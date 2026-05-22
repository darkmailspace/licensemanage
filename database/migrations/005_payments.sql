-- =============================================================================
-- Phase 4C — Payments, Refunds, Webhook Events
--
-- Adds the three tables backing the Razorpay + Stripe payment subsystem.
-- This file is the SQL counterpart of the EF Core migration
-- LicenseManager.Infrastructure/Migrations/20260522120000_Phase4C_Payments.cs
-- and produces an identical schema. Either path applies, never both:
--
--   *  SQL path:   psql -f 005_payments.sql
--   *  EF path:    dotnet ef database update
--                  --project src/LicenseManager.Infrastructure
--                  --startup-project src/LicenseManager.API
-- =============================================================================

-- Required for uuid_generate_v4(); idempotent if 001 already enabled it.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================================================
-- payments
-- =============================================================================
CREATE TABLE payments (
    id                       UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),

    -- Linkage
    customer_id              UUID            NOT NULL,
    license_id               UUID,

    -- Provider (1 = Stripe, 2 = Razorpay)
    provider                 INT             NOT NULL,
    provider_payment_id      VARCHAR(128),
    provider_order_id        VARCHAR(128),
    provider_customer_id     VARCHAR(128),

    -- Money
    amount                   NUMERIC(18,2)   NOT NULL,
    amount_minor             BIGINT          NOT NULL,
    currency                 VARCHAR(3)      NOT NULL,

    -- Lifecycle (1=Created, 2=Pending, 3=Authorized, 4=Captured,
    -- 5=Failed, 6=Cancelled, 7=Refunded, 8=PartiallyRefunded - see PaymentStatus enum)
    status                   INT             NOT NULL,
    authorized_at            TIMESTAMP,
    captured_at              TIMESTAMP,
    failed_at                TIMESTAMP,
    cancelled_at             TIMESTAMP,
    refunded_amount_minor    BIGINT          NOT NULL DEFAULT 0,

    -- Customer-facing handles
    client_secret            VARCHAR(512),
    checkout_url             VARCHAR(2048),
    receipt                  VARCHAR(64),
    description              VARCHAR(512),

    -- Diagnostics
    error_code               VARCHAR(128),
    error_message            VARCHAR(2048),
    raw_provider_data        TEXT,
    metadata                 JSONB,

    -- BaseEntity audit fields
    created_at               TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMP,
    created_by               VARCHAR(255),
    updated_by               VARCHAR(255),
    is_deleted               BOOLEAN         NOT NULL DEFAULT FALSE,
    deleted_at               TIMESTAMP,

    CONSTRAINT fk_payments_customer
        FOREIGN KEY (customer_id) REFERENCES license_customers(id) ON DELETE RESTRICT,
    CONSTRAINT fk_payments_license
        FOREIGN KEY (license_id)  REFERENCES licenses(id)          ON DELETE SET NULL
);

CREATE INDEX ix_payments_provider_provider_payment_id  ON payments (provider, provider_payment_id);
CREATE INDEX ix_payments_provider_provider_order_id    ON payments (provider, provider_order_id);
CREATE INDEX ix_payments_customer_id                   ON payments (customer_id);
CREATE INDEX ix_payments_license_id                    ON payments (license_id);
CREATE INDEX ix_payments_status                        ON payments (status);
CREATE INDEX ix_payments_created_at                    ON payments (created_at);

-- =============================================================================
-- refunds
-- =============================================================================
CREATE TABLE refunds (
    id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),

    payment_id          UUID          NOT NULL,
    provider_refund_id  VARCHAR(128),

    amount              NUMERIC(18,2) NOT NULL,
    amount_minor        BIGINT        NOT NULL,
    currency            VARCHAR(3)    NOT NULL,

    -- 0=Pending, 1=Succeeded, 2=Failed, 3=Cancelled (see RefundStatus enum)
    status              INT           NOT NULL,
    reason              VARCHAR(256),
    error_message       VARCHAR(2048),
    refunded_at         TIMESTAMP,
    raw_provider_data   TEXT,

    -- BaseEntity audit fields
    created_at          TIMESTAMP     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP,
    created_by          VARCHAR(255),
    updated_by          VARCHAR(255),
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,
    deleted_at          TIMESTAMP,

    CONSTRAINT fk_refunds_payment
        FOREIGN KEY (payment_id) REFERENCES payments(id) ON DELETE CASCADE
);

CREATE INDEX ix_refunds_payment_id          ON refunds (payment_id);
CREATE INDEX ix_refunds_provider_refund_id  ON refunds (provider_refund_id);
CREATE INDEX ix_refunds_status              ON refunds (status);

-- =============================================================================
-- webhook_events
-- =============================================================================
-- The (provider, provider_event_id) UNIQUE constraint is the enforcement
-- mechanism for at-most-once webhook processing. Even if both the
-- application-level dedup check and the unique index race, only one row
-- can ever be persisted, and the loser surfaces as a duplicate-key error
-- the service maps to WebhookProcessOutcome.Duplicate.
CREATE TABLE webhook_events (
    id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),

    -- 1=Stripe, 2=Razorpay (see PaymentProvider enum)
    provider            INT           NOT NULL,
    provider_event_id   VARCHAR(128)  NOT NULL,
    event_type          VARCHAR(128)  NOT NULL,

    -- 0=Received, 1=Processed, 2=Failed, 3=Ignored (see WebhookEventStatus enum)
    status              INT           NOT NULL,
    payload             TEXT          NOT NULL,
    signature           VARCHAR(2048),
    received_at         TIMESTAMP     NOT NULL DEFAULT NOW(),
    processed_at        TIMESTAMP,
    error_message       VARCHAR(2048),
    payment_id          UUID,

    -- BaseEntity audit fields
    created_at          TIMESTAMP     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP,
    created_by          VARCHAR(255),
    updated_by          VARCHAR(255),
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,
    deleted_at          TIMESTAMP
);

CREATE UNIQUE INDEX ix_webhook_events_provider_provider_event_id
    ON webhook_events (provider, provider_event_id);
CREATE INDEX ix_webhook_events_status      ON webhook_events (status);
CREATE INDEX ix_webhook_events_received_at ON webhook_events (received_at);
CREATE INDEX ix_webhook_events_payment_id  ON webhook_events (payment_id);
