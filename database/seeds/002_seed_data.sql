-- =============================================================================
-- Enterprise License Management System - Seed Data
-- =============================================================================

-- =============================================================================
-- SEED ADMIN USER (Password: Admin@123456)
-- BCrypt hash with work factor 11
-- =============================================================================
INSERT INTO admin_users (
    id, email, password_hash, full_name, role, is_active, email_verified, email_verified_at, created_at
) VALUES (
    uuid_generate_v4(),
    'admin@licensemanager.com',
    '$2b$11$npJumMhrgcrIGV.jJpMHF.O6nO4oWAm7F95Ch8nynrRlxj4yvytk6', -- Admin@123456 (BCrypt, work factor 11)
    'System Administrator',
    1, -- SuperAdmin
    TRUE,
    TRUE,
    NOW(),
    NOW()
) ON CONFLICT (email) DO NOTHING;

-- =============================================================================
-- SEED LICENSE FEATURES
-- =============================================================================
INSERT INTO license_features (id, feature_code, name, description, category, is_active, requires_enterprise_license, display_order, created_at) VALUES
-- Core Modules
(uuid_generate_v4(), 'CRM_MODULE', 'CRM Module', 'Customer Relationship Management', 'Core Modules', TRUE, FALSE, 1, NOW()),
(uuid_generate_v4(), 'LOAN_MODULE', 'Loan Module', 'Loan Management System', 'Core Modules', TRUE, FALSE, 2, NOW()),
(uuid_generate_v4(), 'EMI_MODULE', 'EMI Module', 'EMI Calculation and Management', 'Core Modules', TRUE, FALSE, 3, NOW()),
(uuid_generate_v4(), 'COLLECTION_MODULE', 'Collection Module', 'Collection Management', 'Core Modules', TRUE, FALSE, 4, NOW()),
(uuid_generate_v4(), 'GPS_MODULE', 'GPS Tracking Module', 'GPS Device Tracking', 'Core Modules', TRUE, FALSE, 5, NOW()),
(uuid_generate_v4(), 'ACCOUNTING_MODULE', 'Accounting Module', 'Financial Accounting', 'Core Modules', TRUE, FALSE, 6, NOW()),
(uuid_generate_v4(), 'REPORTS_MODULE', 'Reports Module', 'Advanced Reporting', 'Core Modules', TRUE, FALSE, 7, NOW()),

-- Mobile Apps
(uuid_generate_v4(), 'MOBILE_APP_ANDROID', 'Android Mobile App', 'Mobile application for Android', 'Mobile Apps', TRUE, FALSE, 10, NOW()),
(uuid_generate_v4(), 'MOBILE_APP_IOS', 'iOS Mobile App', 'Mobile application for iOS', 'Mobile Apps', TRUE, FALSE, 11, NOW()),
(uuid_generate_v4(), 'MOBILE_APP_COLLECTION', 'Collection Mobile App', 'Mobile app for field collection agents', 'Mobile Apps', TRUE, FALSE, 12, NOW()),

-- API Access
(uuid_generate_v4(), 'API_ACCESS_BASIC', 'Basic API Access', 'REST API with basic rate limits', 'API Access', TRUE, FALSE, 20, NOW()),
(uuid_generate_v4(), 'API_ACCESS_ADVANCED', 'Advanced API Access', 'REST API with higher rate limits', 'API Access', TRUE, TRUE, 21, NOW()),
(uuid_generate_v4(), 'WEBHOOK_SUPPORT', 'Webhook Support', 'Outgoing webhook notifications', 'API Access', TRUE, TRUE, 22, NOW()),

-- White Label
(uuid_generate_v4(), 'WHITE_LABEL_BRANDING', 'White Label Branding', 'Custom branding and logos', 'White Label', TRUE, TRUE, 30, NOW()),
(uuid_generate_v4(), 'CUSTOM_DOMAIN', 'Custom Domain', 'Use custom domain name', 'White Label', TRUE, TRUE, 31, NOW()),
(uuid_generate_v4(), 'CUSTOM_EMAIL_DOMAIN', 'Custom Email Domain', 'Send emails from custom domain', 'White Label', TRUE, TRUE, 32, NOW()),

-- Advanced Features
(uuid_generate_v4(), 'MULTI_BRANCH', 'Multi Branch Support', 'Manage multiple branches', 'Advanced', TRUE, FALSE, 40, NOW()),
(uuid_generate_v4(), 'MULTI_COMPANY', 'Multi Company Support', 'Manage multiple companies', 'Advanced', TRUE, TRUE, 41, NOW()),
(uuid_generate_v4(), 'CUSTOM_FIELDS', 'Custom Fields', 'Add custom fields to entities', 'Advanced', TRUE, FALSE, 42, NOW()),
(uuid_generate_v4(), 'WORKFLOW_AUTOMATION', 'Workflow Automation', 'Automated workflows', 'Advanced', TRUE, TRUE, 43, NOW()),
(uuid_generate_v4(), 'ADVANCED_ANALYTICS', 'Advanced Analytics', 'Business intelligence and analytics', 'Advanced', TRUE, TRUE, 44, NOW()),

-- Integration
(uuid_generate_v4(), 'SMS_INTEGRATION', 'SMS Integration', 'Send SMS notifications', 'Integration', TRUE, FALSE, 50, NOW()),
(uuid_generate_v4(), 'WHATSAPP_INTEGRATION', 'WhatsApp Integration', 'Send WhatsApp messages', 'Integration', TRUE, FALSE, 51, NOW()),
(uuid_generate_v4(), 'EMAIL_INTEGRATION', 'Email Integration', 'Email notifications and campaigns', 'Integration', TRUE, FALSE, 52, NOW()),
(uuid_generate_v4(), 'PAYMENT_GATEWAY', 'Payment Gateway', 'Online payment integration', 'Integration', TRUE, FALSE, 53, NOW()),

-- Security
(uuid_generate_v4(), 'TWO_FACTOR_AUTH', 'Two-Factor Authentication', 'Enhanced security with 2FA', 'Security', TRUE, FALSE, 60, NOW()),
(uuid_generate_v4(), 'IP_WHITELIST', 'IP Whitelist', 'Restrict access by IP address', 'Security', TRUE, TRUE, 61, NOW()),
(uuid_generate_v4(), 'AUDIT_LOGS', 'Audit Logs', 'Comprehensive audit trail', 'Security', TRUE, TRUE, 62, NOW()),
(uuid_generate_v4(), 'DATA_ENCRYPTION', 'Data Encryption', 'Enhanced data encryption', 'Security', TRUE, TRUE, 63, NOW()),

-- Support
(uuid_generate_v4(), 'PRIORITY_SUPPORT', 'Priority Support', '24/7 priority customer support', 'Support', TRUE, TRUE, 70, NOW()),
(uuid_generate_v4(), 'DEDICATED_MANAGER', 'Dedicated Account Manager', 'Dedicated account manager', 'Support', TRUE, TRUE, 71, NOW());

-- =============================================================================
-- SEED SAMPLE PRODUCT
-- =============================================================================
INSERT INTO license_products (
    id, product_code, name, description, version, is_active, 
    base_price, currency, trial_days, allow_trial,
    max_devices_per_license, max_users_per_license, max_branches_per_license,
    require_domain_lock, require_hardware_lock, grace_period_days, validation_interval_hours,
    created_at
) VALUES (
    uuid_generate_v4(),
    'FINANCEERPV1',
    'Finance ERP System',
    'Complete Finance and Loan Management ERP System',
    '1.0.0',
    TRUE,
    999.00,
    'USD',
    14,
    TRUE,
    5,
    10,
    3,
    TRUE,
    FALSE,
    7,
    24,
    NOW()
);

-- =============================================================================
-- LINK FEATURES TO PRODUCT
-- =============================================================================
INSERT INTO product_features (product_id, feature_id, is_default_enabled, is_optional, created_at)
SELECT 
    p.id,
    f.id,
    CASE 
        WHEN f.category = 'Core Modules' THEN TRUE
        WHEN f.feature_code = 'API_ACCESS_BASIC' THEN TRUE
        WHEN f.feature_code = 'EMAIL_INTEGRATION' THEN TRUE
        ELSE FALSE
    END as is_default_enabled,
    CASE 
        WHEN f.category = 'Core Modules' THEN FALSE
        ELSE TRUE
    END as is_optional,
    NOW()
FROM license_products p
CROSS JOIN license_features f
WHERE p.product_code = 'FINANCEERPV1';

-- =============================================================================
-- SEED SAMPLE CUSTOMER
-- =============================================================================
INSERT INTO license_customers (
    id, customer_code, name, email, phone, company_name, 
    company_registration_number, gst_number, address, city, state, country, postal_code,
    website, contact_person, contact_person_email, contact_person_phone,
    is_active, is_verified, verified_at, created_at
) VALUES (
    uuid_generate_v4(),
    'CUST001',
    'John Doe',
    'john.doe@example.com',
    '+1234567890',
    'ABC Technologies Pvt Ltd',
    'REG123456',
    'GST123456789',
    '123 Business Street',
    'Mumbai',
    'Maharashtra',
    'India',
    '400001',
    'https://abctech.com',
    'John Doe',
    'john.doe@example.com',
    '+1234567890',
    TRUE,
    TRUE,
    NOW(),
    NOW()
);

-- =============================================================================
-- SEED SAMPLE LICENSE
-- =============================================================================
INSERT INTO licenses (
    id, license_key, activation_token, customer_id, product_id,
    license_type, status, max_users, max_branches, max_domains, max_devices,
    max_concurrent_logins, max_api_calls, max_storage_gb,
    max_employees, max_customers, max_loans, max_collections,
    start_date, expiry_date, price, currency, auto_renewal,
    domain_lock_enabled, hardware_lock_enabled, ip_lock_enabled, country_lock_enabled,
    grace_period_days, created_at
)
SELECT 
    uuid_generate_v4(),
    'LK-' || UPPER(SUBSTRING(MD5(RANDOM()::TEXT) FROM 1 FOR 8)) || '-' || 
    UPPER(SUBSTRING(MD5(RANDOM()::TEXT) FROM 1 FOR 8)) || '-' || 
    UPPER(SUBSTRING(MD5(RANDOM()::TEXT) FROM 1 FOR 8)) || '-' || 
    UPPER(SUBSTRING(MD5(RANDOM()::TEXT) FROM 1 FOR 8)),
    'AT-' || UPPER(SUBSTRING(MD5(RANDOM()::TEXT) FROM 1 FOR 16)),
    c.id,
    p.id,
    5, -- Yearly
    1, -- PendingActivation
    10, -- max_users
    3, -- max_branches
    1, -- max_domains
    5, -- max_devices
    5, -- max_concurrent_logins
    100000, -- max_api_calls
    50, -- max_storage_gb
    100, -- max_employees
    10000, -- max_customers
    50000, -- max_loans
    100000, -- max_collections
    NOW(),
    NOW() + INTERVAL '365 days',
    999.00,
    'USD',
    FALSE,
    TRUE, -- domain_lock_enabled
    FALSE, -- hardware_lock_enabled
    FALSE, -- ip_lock_enabled
    FALSE, -- country_lock_enabled
    7, -- grace_period_days
    NOW()
FROM license_customers c
CROSS JOIN license_products p
WHERE c.customer_code = 'CUST001' AND p.product_code = 'FINANCEERPV1'
LIMIT 1;

-- =============================================================================
-- ASSIGN DEFAULT FEATURES TO SAMPLE LICENSE
-- =============================================================================
INSERT INTO license_feature_mappings (license_id, feature_id, is_enabled, enabled_at, created_at)
SELECT 
    l.id,
    pf.feature_id,
    pf.is_default_enabled,
    CASE WHEN pf.is_default_enabled THEN NOW() ELSE NULL END,
    NOW()
FROM licenses l
JOIN license_products p ON l.product_id = p.id
JOIN product_features pf ON p.id = pf.product_id
WHERE p.product_code = 'FINANCEERPV1'
AND l.license_key LIKE 'LK-%'
LIMIT 1;

-- =============================================================================
-- SEED PRODUCT VERSION
-- =============================================================================
INSERT INTO product_versions (
    id, product_id, version, release_notes, changelog, released_at,
    is_stable, is_beta, is_major_update, is_forced,
    download_url, file_checksum, minimum_compatible_version, created_at
)
SELECT 
    uuid_generate_v4(),
    p.id,
    '1.0.0',
    'Initial Release of Finance ERP System',
    '- Complete Loan Management System
- EMI Calculator
- Customer Management
- Collection Module
- GPS Tracking Integration
- Reports and Analytics',
    NOW(),
    TRUE,
    FALSE,
    TRUE,
    FALSE,
    'https://downloads.licensemanager.com/financeerpv1/v1.0.0.zip',
    'SHA256:1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef',
    '1.0.0',
    NOW()
FROM license_products p
WHERE p.product_code = 'FINANCEERPV1';

-- =============================================================================
-- END OF SEED DATA
-- =============================================================================
