-- =============================================================================
-- Enterprise License Management System - Initial Database Schema
-- PostgreSQL 16+
-- =============================================================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- ADMIN USERS TABLE
-- =============================================================================
CREATE TABLE admin_users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    phone VARCHAR(50),
    role INT NOT NULL DEFAULT 4, -- 1=SuperAdmin, 2=Admin, 3=Support, 4=Viewer
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    email_verified_at TIMESTAMP,
    
    -- MFA
    mfa_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    mfa_secret VARCHAR(500),
    mfa_backup_codes TEXT, -- JSON array
    
    -- Security
    last_login_at TIMESTAMP,
    last_login_ip VARCHAR(50),
    failed_login_attempts INT NOT NULL DEFAULT 0,
    locked_until TIMESTAMP,
    ip_whitelist TEXT, -- Comma-separated
    
    -- Password Reset
    password_reset_token VARCHAR(500),
    password_reset_token_expires_at TIMESTAMP,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT chk_admin_users_role CHECK (role BETWEEN 1 AND 4)
);

CREATE INDEX idx_admin_users_email ON admin_users(email);
CREATE INDEX idx_admin_users_is_active ON admin_users(is_active);
CREATE INDEX idx_admin_users_is_deleted ON admin_users(is_deleted);

-- =============================================================================
-- LOGIN HISTORY TABLE
-- =============================================================================
CREATE TABLE login_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES admin_users(id) ON DELETE CASCADE,
    success BOOLEAN NOT NULL,
    ip_address VARCHAR(50),
    user_agent TEXT,
    country VARCHAR(100),
    city VARCHAR(100),
    failure_reason TEXT,
    login_attempt_at TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_login_history_user_id ON login_history(user_id);
CREATE INDEX idx_login_history_success ON login_history(success);
CREATE INDEX idx_login_history_login_attempt_at ON login_history(login_attempt_at DESC);

-- =============================================================================
-- CUSTOMERS TABLE
-- =============================================================================
CREATE TABLE license_customers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    phone VARCHAR(50) NOT NULL,
    company_name VARCHAR(255),
    company_registration_number VARCHAR(100),
    gst_number VARCHAR(100),
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    postal_code VARCHAR(20),
    website VARCHAR(255),
    contact_person VARCHAR(255),
    contact_person_email VARCHAR(255),
    contact_person_phone VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    verified_at TIMESTAMP,
    notes TEXT,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_license_customers_email ON license_customers(email);
CREATE INDEX idx_license_customers_customer_code ON license_customers(customer_code);
CREATE INDEX idx_license_customers_is_active ON license_customers(is_active);
CREATE INDEX idx_license_customers_is_deleted ON license_customers(is_deleted);

-- =============================================================================
-- PRODUCTS TABLE
-- =============================================================================
CREATE TABLE license_products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    version VARCHAR(50) NOT NULL DEFAULT '1.0.0',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    image_url VARCHAR(500),
    base_price DECIMAL(18, 2) NOT NULL DEFAULT 0,
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    trial_days INT NOT NULL DEFAULT 14,
    allow_trial BOOLEAN NOT NULL DEFAULT TRUE,
    max_devices_per_license INT NOT NULL DEFAULT 1,
    max_users_per_license INT NOT NULL DEFAULT 1,
    max_branches_per_license INT NOT NULL DEFAULT 1,
    require_domain_lock BOOLEAN NOT NULL DEFAULT TRUE,
    require_hardware_lock BOOLEAN NOT NULL DEFAULT FALSE,
    grace_period_days INT NOT NULL DEFAULT 7,
    validation_interval_hours INT NOT NULL DEFAULT 24,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_license_products_product_code ON license_products(product_code);
CREATE INDEX idx_license_products_is_active ON license_products(is_active);
CREATE INDEX idx_license_products_is_deleted ON license_products(is_deleted);

-- =============================================================================
-- LICENSES TABLE
-- =============================================================================
CREATE TABLE licenses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_key VARCHAR(500) NOT NULL UNIQUE,
    activation_token VARCHAR(500) NOT NULL UNIQUE,
    customer_id UUID NOT NULL REFERENCES license_customers(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES license_products(id) ON DELETE CASCADE,
    license_type INT NOT NULL, -- 1=Trial, 2=Monthly, 3=Quarterly, 4=HalfYearly, 5=Yearly, 6=MultiYear, 7=Lifetime, 8=Enterprise, 9=Franchise, 10=WhiteLabel, 11=OEM, 12=Developer, 13=Reseller
    status INT NOT NULL DEFAULT 1, -- 1=PendingActivation, 2=Active, 3=Suspended, 4=Expired, 5=Revoked, 6=GracePeriod, 7=PendingRenewal, 8=Cancelled, 9=Transferred, 10=Upgraded, 11=Downgraded
    
    -- Limits
    max_users INT NOT NULL DEFAULT 1,
    max_branches INT NOT NULL DEFAULT 1,
    max_domains INT NOT NULL DEFAULT 1,
    max_devices INT NOT NULL DEFAULT 1,
    max_concurrent_logins INT NOT NULL DEFAULT 1,
    max_api_calls BIGINT NOT NULL DEFAULT 10000,
    max_storage_gb BIGINT NOT NULL DEFAULT 10,
    max_employees INT NOT NULL DEFAULT 100,
    max_customers INT NOT NULL DEFAULT 1000,
    max_loans INT NOT NULL DEFAULT 10000,
    max_collections INT NOT NULL DEFAULT 10000,
    
    -- Dates
    start_date TIMESTAMP NOT NULL,
    expiry_date TIMESTAMP NOT NULL,
    activated_at TIMESTAMP,
    last_validated_at TIMESTAMP,
    suspended_at TIMESTAMP,
    revoked_at TIMESTAMP,
    
    -- Security
    license_signature TEXT,
    public_key TEXT,
    encrypted_payload TEXT,
    hardware_fingerprint VARCHAR(500),
    
    -- Locking
    domain_lock_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    hardware_lock_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    ip_lock_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    country_lock_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    allowed_countries TEXT, -- Comma-separated
    ip_whitelist TEXT, -- Comma-separated
    
    -- Grace Period
    in_grace_period BOOLEAN NOT NULL DEFAULT FALSE,
    grace_period_start_date TIMESTAMP,
    grace_period_days INT NOT NULL DEFAULT 7,
    
    -- Billing
    price DECIMAL(18, 2) NOT NULL DEFAULT 0,
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    auto_renewal BOOLEAN NOT NULL DEFAULT FALSE,
    payment_method VARCHAR(50),
    
    -- Additional Info
    notes TEXT,
    internal_notes TEXT,
    custom_metadata JSONB,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT chk_licenses_license_type CHECK (license_type BETWEEN 1 AND 13),
    CONSTRAINT chk_licenses_status CHECK (status BETWEEN 1 AND 11),
    CONSTRAINT chk_licenses_dates CHECK (expiry_date > start_date)
);

CREATE INDEX idx_licenses_license_key ON licenses(license_key);
CREATE INDEX idx_licenses_customer_id ON licenses(customer_id);
CREATE INDEX idx_licenses_product_id ON licenses(product_id);
CREATE INDEX idx_licenses_status ON licenses(status);
CREATE INDEX idx_licenses_expiry_date ON licenses(expiry_date);
CREATE INDEX idx_licenses_is_deleted ON licenses(is_deleted);
CREATE INDEX idx_licenses_hardware_fingerprint ON licenses(hardware_fingerprint);

-- =============================================================================
-- LICENSE DOMAINS TABLE
-- =============================================================================
CREATE TABLE license_domains (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    domain_name VARCHAR(255) NOT NULL,
    is_wildcard BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    verified_at TIMESTAMP,
    last_accessed_at TIMESTAMP,
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    verification_code VARCHAR(100),
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Transfer/Change Request
    change_requested BOOLEAN NOT NULL DEFAULT FALSE,
    requested_domain VARCHAR(255),
    change_requested_at TIMESTAMP,
    change_approved BOOLEAN NOT NULL DEFAULT FALSE,
    change_approved_at TIMESTAMP,
    approved_by VARCHAR(255),
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT uq_license_domains_license_domain UNIQUE(license_id, domain_name)
);

CREATE INDEX idx_license_domains_license_id ON license_domains(license_id);
CREATE INDEX idx_license_domains_domain_name ON license_domains(domain_name);
CREATE INDEX idx_license_domains_is_active ON license_domains(is_active);
CREATE INDEX idx_license_domains_is_deleted ON license_domains(is_deleted);

-- =============================================================================
-- LICENSE DEVICES TABLE
-- =============================================================================
CREATE TABLE license_devices (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    device_name VARCHAR(255) NOT NULL,
    device_fingerprint VARCHAR(500) NOT NULL,
    
    -- Hardware Identifiers
    cpu_id VARCHAR(255),
    motherboard_id VARCHAR(255),
    disk_serial_number VARCHAR(255),
    mac_address VARCHAR(100),
    bios_serial_number VARCHAR(255),
    
    -- System Information
    operating_system VARCHAR(100),
    os_version VARCHAR(100),
    architecture VARCHAR(50),
    is_virtual_machine BOOLEAN NOT NULL DEFAULT FALSE,
    vm_platform VARCHAR(100),
    
    -- Network Information
    ip_address VARCHAR(50),
    country VARCHAR(100),
    city VARCHAR(100),
    
    -- Device Status
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    first_activated_at TIMESTAMP,
    last_accessed_at TIMESTAMP,
    access_count INT NOT NULL DEFAULT 0,
    
    -- Deactivation
    is_deactivated BOOLEAN NOT NULL DEFAULT FALSE,
    deactivated_at TIMESTAMP,
    deactivation_reason TEXT,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT uq_license_devices_license_fingerprint UNIQUE(license_id, device_fingerprint)
);

CREATE INDEX idx_license_devices_license_id ON license_devices(license_id);
CREATE INDEX idx_license_devices_device_fingerprint ON license_devices(device_fingerprint);
CREATE INDEX idx_license_devices_is_active ON license_devices(is_active);
CREATE INDEX idx_license_devices_is_deleted ON license_devices(is_deleted);

-- =============================================================================
-- LICENSE ACTIVATIONS TABLE
-- =============================================================================
CREATE TABLE license_activations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    activation_type INT NOT NULL, -- 1=Online, 2=Offline, 3=Manual, 4=AutoActivation
    success BOOLEAN NOT NULL,
    failure_reason TEXT,
    
    -- Request Information
    domain_name VARCHAR(255),
    device_fingerprint VARCHAR(500),
    ip_address VARCHAR(50),
    country VARCHAR(100),
    user_agent TEXT,
    
    -- Offline Activation
    activation_request_file TEXT,
    activation_response_file TEXT,
    offline_activation_generated_at TIMESTAMP,
    
    -- Validation
    activation_code VARCHAR(255),
    activation_code_expires_at TIMESTAMP,
    
    -- Metadata
    request_metadata JSONB,
    response_metadata JSONB,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT chk_license_activations_activation_type CHECK (activation_type BETWEEN 1 AND 4)
);

CREATE INDEX idx_license_activations_license_id ON license_activations(license_id);
CREATE INDEX idx_license_activations_success ON license_activations(success);
CREATE INDEX idx_license_activations_created_at ON license_activations(created_at DESC);
CREATE INDEX idx_license_activations_is_deleted ON license_activations(is_deleted);

-- =============================================================================
-- LICENSE VALIDATIONS TABLE
-- =============================================================================
CREATE TABLE license_validations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    validation_result INT NOT NULL, -- 1=Valid, 2=Invalid, 3=Expired, 4=Revoked, 5=Suspended, 6=DomainMismatch, 7=HardwareMismatch, 8=MaxDevicesReached, 9=MaxUsersReached, 10=FeatureNotEnabled, 11=SignatureInvalid, 12=TamperedLicense, 13=IPRestricted, 14=CountryRestricted, 15=GracePeriodExpired
    is_valid BOOLEAN NOT NULL,
    validation_message TEXT,
    
    -- Request Information
    domain_name VARCHAR(255),
    device_fingerprint VARCHAR(500),
    ip_address VARCHAR(50),
    country VARCHAR(100),
    user_agent TEXT,
    product_version VARCHAR(50),
    
    -- Feature Validation
    requested_features TEXT, -- JSON array
    enabled_features TEXT, -- JSON array
    
    -- Heartbeat Information
    is_heartbeat BOOLEAN NOT NULL DEFAULT FALSE,
    last_heartbeat_at TIMESTAMP,
    
    -- Response Time
    response_time_ms INT NOT NULL DEFAULT 0,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT chk_license_validations_validation_result CHECK (validation_result BETWEEN 1 AND 15)
);

CREATE INDEX idx_license_validations_license_id ON license_validations(license_id);
CREATE INDEX idx_license_validations_is_valid ON license_validations(is_valid);
CREATE INDEX idx_license_validations_created_at ON license_validations(created_at DESC);
CREATE INDEX idx_license_validations_is_deleted ON license_validations(is_deleted);

-- =============================================================================
-- FEATURES TABLE
-- =============================================================================
CREATE TABLE license_features (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    feature_code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    requires_enterprise_license BOOLEAN NOT NULL DEFAULT FALSE,
    additional_cost DECIMAL(18, 2),
    display_order INT NOT NULL DEFAULT 0,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_license_features_feature_code ON license_features(feature_code);
CREATE INDEX idx_license_features_is_active ON license_features(is_active);
CREATE INDEX idx_license_features_is_deleted ON license_features(is_deleted);

-- =============================================================================
-- LICENSE FEATURE MAPPINGS TABLE
-- =============================================================================
CREATE TABLE license_feature_mappings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    feature_id UUID NOT NULL REFERENCES license_features(id) ON DELETE CASCADE,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    enabled_at TIMESTAMP,
    disabled_at TIMESTAMP,
    disabled_by VARCHAR(255),
    disabled_reason TEXT,
    
    -- Usage Limits
    usage_limit INT,
    usage_count INT NOT NULL DEFAULT 0,
    usage_limit_reset_date TIMESTAMP,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT uq_license_feature_mappings_license_feature UNIQUE(license_id, feature_id)
);

CREATE INDEX idx_license_feature_mappings_license_id ON license_feature_mappings(license_id);
CREATE INDEX idx_license_feature_mappings_feature_id ON license_feature_mappings(feature_id);
CREATE INDEX idx_license_feature_mappings_is_enabled ON license_feature_mappings(is_enabled);
CREATE INDEX idx_license_feature_mappings_is_deleted ON license_feature_mappings(is_deleted);

-- =============================================================================
-- PRODUCT FEATURES TABLE
-- =============================================================================
CREATE TABLE product_features (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES license_products(id) ON DELETE CASCADE,
    feature_id UUID NOT NULL REFERENCES license_features(id) ON DELETE CASCADE,
    is_default_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    is_optional BOOLEAN NOT NULL DEFAULT TRUE,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT uq_product_features_product_feature UNIQUE(product_id, feature_id)
);

CREATE INDEX idx_product_features_product_id ON product_features(product_id);
CREATE INDEX idx_product_features_feature_id ON product_features(feature_id);
CREATE INDEX idx_product_features_is_deleted ON product_features(is_deleted);

-- =============================================================================
-- PRODUCT VERSIONS TABLE
-- =============================================================================
CREATE TABLE product_versions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES license_products(id) ON DELETE CASCADE,
    version VARCHAR(50) NOT NULL,
    release_notes TEXT,
    changelog TEXT,
    released_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_stable BOOLEAN NOT NULL DEFAULT TRUE,
    is_beta BOOLEAN NOT NULL DEFAULT FALSE,
    is_major_update BOOLEAN NOT NULL DEFAULT FALSE,
    is_forced BOOLEAN NOT NULL DEFAULT FALSE,
    download_url VARCHAR(500),
    file_size_bytes BIGINT,
    file_checksum VARCHAR(255),
    minimum_compatible_version VARCHAR(50),
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT uq_product_versions_product_version UNIQUE(product_id, version)
);

CREATE INDEX idx_product_versions_product_id ON product_versions(product_id);
CREATE INDEX idx_product_versions_version ON product_versions(version);
CREATE INDEX idx_product_versions_released_at ON product_versions(released_at DESC);
CREATE INDEX idx_product_versions_is_deleted ON product_versions(is_deleted);

-- =============================================================================
-- UPDATE DOWNLOADS TABLE
-- =============================================================================
CREATE TABLE update_downloads (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_version_id UUID NOT NULL REFERENCES product_versions(id) ON DELETE CASCADE,
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    ip_address VARCHAR(50),
    user_agent TEXT,
    downloaded_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    completed_at TIMESTAMP,
    bytes_downloaded BIGINT,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_update_downloads_product_version_id ON update_downloads(product_version_id);
CREATE INDEX idx_update_downloads_license_id ON update_downloads(license_id);
CREATE INDEX idx_update_downloads_downloaded_at ON update_downloads(downloaded_at DESC);
CREATE INDEX idx_update_downloads_is_deleted ON update_downloads(is_deleted);

-- =============================================================================
-- LICENSE HISTORY TABLE
-- =============================================================================
CREATE TABLE license_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    action VARCHAR(100) NOT NULL,
    previous_status INT,
    new_status INT,
    description TEXT,
    performed_by VARCHAR(255),
    ip_address VARCHAR(50),
    changes JSONB,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_license_history_license_id ON license_history(license_id);
CREATE INDEX idx_license_history_created_at ON license_history(created_at DESC);
CREATE INDEX idx_license_history_is_deleted ON license_history(is_deleted);

-- =============================================================================
-- SUPPORT TICKETS TABLE
-- =============================================================================
CREATE TABLE support_tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_number VARCHAR(50) NOT NULL UNIQUE,
    customer_id UUID NOT NULL REFERENCES license_customers(id) ON DELETE CASCADE,
    license_id UUID REFERENCES licenses(id) ON DELETE SET NULL,
    subject VARCHAR(500) NOT NULL,
    description TEXT NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Open, 2=InProgress, 3=Waiting, 4=Resolved, 5=Closed, 6=Cancelled
    priority INT NOT NULL DEFAULT 2, -- 1=Low, 2=Medium, 3=High, 4=Critical
    assigned_to VARCHAR(255),
    assigned_at TIMESTAMP,
    resolved_at TIMESTAMP,
    closed_at TIMESTAMP,
    resolution TEXT,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT chk_support_tickets_status CHECK (status BETWEEN 1 AND 6),
    CONSTRAINT chk_support_tickets_priority CHECK (priority BETWEEN 1 AND 4)
);

CREATE INDEX idx_support_tickets_ticket_number ON support_tickets(ticket_number);
CREATE INDEX idx_support_tickets_customer_id ON support_tickets(customer_id);
CREATE INDEX idx_support_tickets_license_id ON support_tickets(license_id);
CREATE INDEX idx_support_tickets_status ON support_tickets(status);
CREATE INDEX idx_support_tickets_priority ON support_tickets(priority);
CREATE INDEX idx_support_tickets_is_deleted ON support_tickets(is_deleted);

-- =============================================================================
-- TICKET COMMENTS TABLE
-- =============================================================================
CREATE TABLE ticket_comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id UUID NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
    comment TEXT NOT NULL,
    commented_by VARCHAR(255),
    is_internal BOOLEAN NOT NULL DEFAULT FALSE,
    attachments TEXT, -- JSON array
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_ticket_comments_ticket_id ON ticket_comments(ticket_id);
CREATE INDEX idx_ticket_comments_created_at ON ticket_comments(created_at DESC);
CREATE INDEX idx_ticket_comments_is_deleted ON ticket_comments(is_deleted);

-- =============================================================================
-- AUDIT LOGS TABLE
-- =============================================================================
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_name VARCHAR(255) NOT NULL,
    entity_id VARCHAR(255) NOT NULL,
    action INT NOT NULL, -- 1=Create, 2=Read, 3=Update, 4=Delete, 5=Login, 6=Logout, 7=Failed, 8=Export, 9=Import
    user_id VARCHAR(255),
    user_name VARCHAR(255),
    user_email VARCHAR(255),
    ip_address VARCHAR(50),
    user_agent TEXT,
    old_values JSONB,
    new_values JSONB,
    description TEXT,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    
    CONSTRAINT chk_audit_logs_action CHECK (action BETWEEN 1 AND 9)
);

CREATE INDEX idx_audit_logs_entity_name ON audit_logs(entity_name);
CREATE INDEX idx_audit_logs_entity_id ON audit_logs(entity_id);
CREATE INDEX idx_audit_logs_action ON audit_logs(action);
CREATE INDEX idx_audit_logs_user_id ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at DESC);
CREATE INDEX idx_audit_logs_is_deleted ON audit_logs(is_deleted);

-- =============================================================================
-- API LOGS TABLE
-- =============================================================================
CREATE TABLE api_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    endpoint VARCHAR(500) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    status_code INT NOT NULL,
    response_time_ms INT NOT NULL,
    ip_address VARCHAR(50),
    user_agent TEXT,
    request_body TEXT,
    response_body TEXT,
    error_message TEXT,
    license_id UUID,
    user_id UUID,
    
    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP
);

CREATE INDEX idx_api_logs_endpoint ON api_logs(endpoint);
CREATE INDEX idx_api_logs_status_code ON api_logs(status_code);
CREATE INDEX idx_api_logs_created_at ON api_logs(created_at DESC);
CREATE INDEX idx_api_logs_license_id ON api_logs(license_id);
CREATE INDEX idx_api_logs_is_deleted ON api_logs(is_deleted);

-- =============================================================================
-- COMMENTS
-- =============================================================================
COMMENT ON TABLE admin_users IS 'Super admin and admin users for the license management system';
COMMENT ON TABLE login_history IS 'Login attempts and history for admin users';
COMMENT ON TABLE license_customers IS 'Customers who purchase licenses';
COMMENT ON TABLE license_products IS 'Products that can be licensed';
COMMENT ON TABLE licenses IS 'License records with all configurations and limits';
COMMENT ON TABLE license_domains IS 'Domains registered to licenses';
COMMENT ON TABLE license_devices IS 'Devices/hardware registered to licenses';
COMMENT ON TABLE license_activations IS 'Activation attempts and records';
COMMENT ON TABLE license_validations IS 'License validation logs and heartbeats';
COMMENT ON TABLE license_features IS 'Available features that can be enabled/disabled';
COMMENT ON TABLE license_feature_mappings IS 'Feature assignments to licenses';
COMMENT ON TABLE product_features IS 'Features available for each product';
COMMENT ON TABLE product_versions IS 'Product version releases';
COMMENT ON TABLE update_downloads IS 'Update download logs';
COMMENT ON TABLE license_history IS 'Change history for licenses';
COMMENT ON TABLE support_tickets IS 'Customer support tickets';
COMMENT ON TABLE ticket_comments IS 'Comments on support tickets';
COMMENT ON TABLE audit_logs IS 'System-wide audit trail';
COMMENT ON TABLE api_logs IS 'API request and response logs';

-- =============================================================================
-- END OF SCHEMA
-- =============================================================================
