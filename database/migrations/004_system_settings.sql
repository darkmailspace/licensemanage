-- =============================================================================
-- System Settings Table
-- Stores key-value configuration accessible via the SettingsController.
-- =============================================================================

CREATE TABLE IF NOT EXISTS system_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(255) NOT NULL UNIQUE,
    value TEXT,
    category VARCHAR(100),
    description TEXT,
    is_secret BOOLEAN NOT NULL DEFAULT FALSE,

    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_system_settings_key ON system_settings(key);
CREATE INDEX IF NOT EXISTS idx_system_settings_category ON system_settings(category);
CREATE INDEX IF NOT EXISTS idx_system_settings_is_deleted ON system_settings(is_deleted);

CREATE TRIGGER update_system_settings_updated_at
BEFORE UPDATE ON system_settings
FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- Default settings (idempotent inserts)
-- =============================================================================

INSERT INTO system_settings (key, value, category, description, is_secret) VALUES
    ('app.name', 'License Manager', 'general', 'Application display name', FALSE),
    ('app.support_email', 'support@licensemanager.com', 'general', 'Support contact email', FALSE),
    ('app.timezone', 'UTC', 'general', 'Default timezone', FALSE),
    ('license.default_grace_period_days', '7', 'license', 'Default grace period in days', FALSE),
    ('license.default_trial_days', '14', 'license', 'Default trial duration in days', FALSE),
    ('license.validation_interval_hours', '24', 'license', 'How often clients must validate', FALSE),
    ('smtp.host', '', 'email', 'SMTP server hostname', FALSE),
    ('smtp.port', '587', 'email', 'SMTP server port', FALSE),
    ('smtp.username', '', 'email', 'SMTP username', FALSE),
    ('smtp.password', '', 'email', 'SMTP password', TRUE),
    ('smtp.from_email', 'noreply@licensemanager.com', 'email', 'From email address', FALSE),
    ('smtp.from_name', 'License Manager', 'email', 'From display name', FALSE),
    ('smtp.use_tls', 'true', 'email', 'Use TLS for SMTP', FALSE),
    ('sms.api_key', '', 'sms', 'SMS provider API key', TRUE),
    ('whatsapp.api_key', '', 'whatsapp', 'WhatsApp Business API key', TRUE),
    ('security.mfa_required_for_admins', 'false', 'security', 'Require MFA for all admin users', FALSE),
    ('security.session_timeout_minutes', '60', 'security', 'JWT access token TTL', FALSE),
    ('security.max_failed_login_attempts', '5', 'security', 'Lockout threshold', FALSE),
    ('security.lockout_duration_minutes', '15', 'security', 'Account lockout duration', FALSE),
    ('api.rate_limit_per_minute', '100', 'api', 'API rate limit per minute', FALSE),
    ('api.enable_logging', 'true', 'api', 'Log all API requests/responses', FALSE),
    ('webhook.url', '', 'webhook', 'Outgoing webhook URL', FALSE)
ON CONFLICT (key) DO NOTHING;
