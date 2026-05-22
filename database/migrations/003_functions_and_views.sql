-- =============================================================================
-- Enterprise License Management System - Functions and Views
-- =============================================================================

-- =============================================================================
-- FUNCTION: Generate License Key
-- =============================================================================
CREATE OR REPLACE FUNCTION generate_license_key()
RETURNS VARCHAR(500) AS $$
DECLARE
    key_part1 VARCHAR(8);
    key_part2 VARCHAR(8);
    key_part3 VARCHAR(8);
    key_part4 VARCHAR(8);
BEGIN
    key_part1 := UPPER(SUBSTRING(MD5(RANDOM()::TEXT || NOW()::TEXT) FROM 1 FOR 8));
    key_part2 := UPPER(SUBSTRING(MD5(RANDOM()::TEXT || NOW()::TEXT) FROM 1 FOR 8));
    key_part3 := UPPER(SUBSTRING(MD5(RANDOM()::TEXT || NOW()::TEXT) FROM 1 FOR 8));
    key_part4 := UPPER(SUBSTRING(MD5(RANDOM()::TEXT || NOW()::TEXT) FROM 1 FOR 8));
    
    RETURN 'LK-' || key_part1 || '-' || key_part2 || '-' || key_part3 || '-' || key_part4;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: Generate Activation Token
-- =============================================================================
CREATE OR REPLACE FUNCTION generate_activation_token()
RETURNS VARCHAR(500) AS $$
BEGIN
    RETURN 'AT-' || UPPER(SUBSTRING(MD5(RANDOM()::TEXT || NOW()::TEXT) FROM 1 FOR 32));
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: Check License Expiry
-- =============================================================================
CREATE OR REPLACE FUNCTION check_license_expired(p_license_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    v_expiry_date TIMESTAMP;
BEGIN
    SELECT expiry_date INTO v_expiry_date
    FROM licenses
    WHERE id = p_license_id;
    
    RETURN NOW() > v_expiry_date;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: Check if License is in Grace Period
-- =============================================================================
CREATE OR REPLACE FUNCTION check_license_in_grace_period(p_license_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    v_in_grace_period BOOLEAN;
    v_grace_period_start_date TIMESTAMP;
    v_grace_period_days INT;
BEGIN
    SELECT in_grace_period, grace_period_start_date, grace_period_days
    INTO v_in_grace_period, v_grace_period_start_date, v_grace_period_days
    FROM licenses
    WHERE id = p_license_id;
    
    IF NOT v_in_grace_period OR v_grace_period_start_date IS NULL THEN
        RETURN FALSE;
    END IF;
    
    RETURN NOW() <= (v_grace_period_start_date + (v_grace_period_days || ' days')::INTERVAL);
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: Get License Days Until Expiry
-- =============================================================================
CREATE OR REPLACE FUNCTION get_license_days_until_expiry(p_license_id UUID)
RETURNS INT AS $$
DECLARE
    v_expiry_date TIMESTAMP;
BEGIN
    SELECT expiry_date INTO v_expiry_date
    FROM licenses
    WHERE id = p_license_id;
    
    RETURN EXTRACT(DAY FROM (v_expiry_date - NOW()));
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: Check if License Can Activate
-- =============================================================================
CREATE OR REPLACE FUNCTION can_license_activate(p_license_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    v_status INT;
    v_expiry_date TIMESTAMP;
BEGIN
    SELECT status, expiry_date INTO v_status, v_expiry_date
    FROM licenses
    WHERE id = p_license_id;
    
    RETURN v_status = 1 AND NOW() <= v_expiry_date; -- PendingActivation and not expired
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: Check Device Limit
-- =============================================================================
CREATE OR REPLACE FUNCTION check_device_limit_reached(p_license_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    v_max_devices INT;
    v_current_devices INT;
BEGIN
    SELECT max_devices INTO v_max_devices
    FROM licenses
    WHERE id = p_license_id;
    
    SELECT COUNT(*) INTO v_current_devices
    FROM license_devices
    WHERE license_id = p_license_id 
    AND is_active = TRUE 
    AND is_deactivated = FALSE
    AND is_deleted = FALSE;
    
    RETURN v_current_devices >= v_max_devices;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: Check Domain Limit
-- =============================================================================
CREATE OR REPLACE FUNCTION check_domain_limit_reached(p_license_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    v_max_domains INT;
    v_current_domains INT;
BEGIN
    SELECT max_domains INTO v_max_domains
    FROM licenses
    WHERE id = p_license_id;
    
    SELECT COUNT(*) INTO v_current_domains
    FROM license_domains
    WHERE license_id = p_license_id 
    AND is_active = TRUE 
    AND is_deleted = FALSE;
    
    RETURN v_current_domains >= v_max_domains;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- VIEW: Active Licenses Summary
-- =============================================================================
CREATE OR REPLACE VIEW v_active_licenses AS
SELECT 
    l.id,
    l.license_key,
    l.status,
    l.license_type,
    l.start_date,
    l.expiry_date,
    EXTRACT(DAY FROM (l.expiry_date - NOW()))::INT as days_until_expiry,
    c.customer_code,
    c.name as customer_name,
    c.email as customer_email,
    c.company_name,
    p.product_code,
    p.name as product_name,
    p.version as product_version,
    l.max_users,
    l.max_branches,
    l.max_devices,
    l.max_domains,
    (SELECT COUNT(*) FROM license_devices WHERE license_id = l.id AND is_active = TRUE AND is_deleted = FALSE) as active_devices,
    (SELECT COUNT(*) FROM license_domains WHERE license_id = l.id AND is_active = TRUE AND is_deleted = FALSE) as active_domains,
    l.created_at,
    l.activated_at,
    l.last_validated_at
FROM licenses l
JOIN license_customers c ON l.customer_id = c.id
JOIN license_products p ON l.product_id = p.id
WHERE l.is_deleted = FALSE
AND l.status = 2; -- Active

-- =============================================================================
-- VIEW: Expiring Licenses (Within 30 Days)
-- =============================================================================
CREATE OR REPLACE VIEW v_expiring_licenses AS
SELECT 
    l.id,
    l.license_key,
    l.expiry_date,
    EXTRACT(DAY FROM (l.expiry_date - NOW()))::INT as days_until_expiry,
    c.customer_code,
    c.name as customer_name,
    c.email as customer_email,
    c.phone as customer_phone,
    p.product_code,
    p.name as product_name,
    l.price,
    l.currency,
    l.auto_renewal
FROM licenses l
JOIN license_customers c ON l.customer_id = c.id
JOIN license_products p ON l.product_id = p.id
WHERE l.is_deleted = FALSE
AND l.status = 2 -- Active
AND l.expiry_date BETWEEN NOW() AND (NOW() + INTERVAL '30 days')
ORDER BY l.expiry_date ASC;

-- =============================================================================
-- VIEW: License Validation Statistics
-- =============================================================================
CREATE OR REPLACE VIEW v_license_validation_stats AS
SELECT 
    l.id as license_id,
    l.license_key,
    c.name as customer_name,
    p.name as product_name,
    COUNT(lv.id) as total_validations,
    COUNT(CASE WHEN lv.is_valid = TRUE THEN 1 END) as successful_validations,
    COUNT(CASE WHEN lv.is_valid = FALSE THEN 1 END) as failed_validations,
    MAX(lv.created_at) as last_validation_at,
    AVG(lv.response_time_ms)::INT as avg_response_time_ms
FROM licenses l
JOIN license_customers c ON l.customer_id = c.id
JOIN license_products p ON l.product_id = p.id
LEFT JOIN license_validations lv ON l.id = lv.license_id AND lv.is_deleted = FALSE
WHERE l.is_deleted = FALSE
GROUP BY l.id, l.license_key, c.name, p.name;

-- =============================================================================
-- VIEW: Customer License Summary
-- =============================================================================
CREATE OR REPLACE VIEW v_customer_license_summary AS
SELECT 
    c.id as customer_id,
    c.customer_code,
    c.name as customer_name,
    c.email,
    c.company_name,
    COUNT(l.id) as total_licenses,
    COUNT(CASE WHEN l.status = 2 THEN 1 END) as active_licenses,
    COUNT(CASE WHEN l.status = 4 THEN 1 END) as expired_licenses,
    COUNT(CASE WHEN l.status = 3 THEN 1 END) as suspended_licenses,
    SUM(l.price) as total_revenue,
    MAX(l.expiry_date) as latest_expiry_date
FROM license_customers c
LEFT JOIN licenses l ON c.id = l.customer_id AND l.is_deleted = FALSE
WHERE c.is_deleted = FALSE
GROUP BY c.id, c.customer_code, c.name, c.email, c.company_name;

-- =============================================================================
-- VIEW: Product Revenue Summary
-- =============================================================================
CREATE OR REPLACE VIEW v_product_revenue_summary AS
SELECT 
    p.id as product_id,
    p.product_code,
    p.name as product_name,
    p.version,
    COUNT(l.id) as total_licenses_sold,
    COUNT(CASE WHEN l.status = 2 THEN 1 END) as active_licenses,
    SUM(l.price) as total_revenue,
    AVG(l.price) as avg_license_price,
    p.currency
FROM license_products p
LEFT JOIN licenses l ON p.id = l.product_id AND l.is_deleted = FALSE
WHERE p.is_deleted = FALSE
GROUP BY p.id, p.product_code, p.name, p.version, p.currency;

-- =============================================================================
-- VIEW: Daily Activation Statistics
-- =============================================================================
CREATE OR REPLACE VIEW v_daily_activation_stats AS
SELECT 
    DATE(created_at) as activation_date,
    COUNT(*) as total_attempts,
    COUNT(CASE WHEN success = TRUE THEN 1 END) as successful_activations,
    COUNT(CASE WHEN success = FALSE THEN 1 END) as failed_activations,
    ROUND(COUNT(CASE WHEN success = TRUE THEN 1 END)::NUMERIC / COUNT(*)::NUMERIC * 100, 2) as success_rate
FROM license_activations
WHERE is_deleted = FALSE
GROUP BY DATE(created_at)
ORDER BY activation_date DESC;

-- =============================================================================
-- VIEW: Device Registration Summary
-- =============================================================================
CREATE OR REPLACE VIEW v_device_registration_summary AS
SELECT 
    l.id as license_id,
    l.license_key,
    c.name as customer_name,
    p.name as product_name,
    l.max_devices,
    COUNT(ld.id) as registered_devices,
    COUNT(CASE WHEN ld.is_active = TRUE AND ld.is_deactivated = FALSE THEN 1 END) as active_devices,
    l.max_devices - COUNT(CASE WHEN ld.is_active = TRUE AND ld.is_deactivated = FALSE THEN 1 END) as available_slots
FROM licenses l
JOIN license_customers c ON l.customer_id = c.id
JOIN license_products p ON l.product_id = p.id
LEFT JOIN license_devices ld ON l.id = ld.license_id AND ld.is_deleted = FALSE
WHERE l.is_deleted = FALSE
GROUP BY l.id, l.license_key, c.name, p.name, l.max_devices;

-- =============================================================================
-- VIEW: Support Ticket Statistics
-- =============================================================================
CREATE OR REPLACE VIEW v_support_ticket_stats AS
SELECT 
    DATE(created_at) as ticket_date,
    COUNT(*) as total_tickets,
    COUNT(CASE WHEN status = 1 THEN 1 END) as open_tickets,
    COUNT(CASE WHEN status = 2 THEN 1 END) as in_progress_tickets,
    COUNT(CASE WHEN status = 4 THEN 1 END) as resolved_tickets,
    COUNT(CASE WHEN status = 5 THEN 1 END) as closed_tickets,
    COUNT(CASE WHEN priority = 4 THEN 1 END) as critical_priority,
    COUNT(CASE WHEN priority = 3 THEN 1 END) as high_priority
FROM support_tickets
WHERE is_deleted = FALSE
GROUP BY DATE(created_at)
ORDER BY ticket_date DESC;

-- =============================================================================
-- VIEW: Feature Usage Statistics
-- =============================================================================
CREATE OR REPLACE VIEW v_feature_usage_stats AS
SELECT 
    f.feature_code,
    f.name as feature_name,
    f.category,
    COUNT(DISTINCT lfm.license_id) as total_licenses_using,
    COUNT(CASE WHEN lfm.is_enabled = TRUE THEN 1 END) as enabled_count,
    COUNT(CASE WHEN lfm.is_enabled = FALSE THEN 1 END) as disabled_count
FROM license_features f
LEFT JOIN license_feature_mappings lfm ON f.id = lfm.feature_id AND lfm.is_deleted = FALSE
WHERE f.is_deleted = FALSE
GROUP BY f.id, f.feature_code, f.name, f.category
ORDER BY total_licenses_using DESC;

-- =============================================================================
-- TRIGGER: Auto-update updated_at timestamp
-- =============================================================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to all tables
CREATE TRIGGER update_admin_users_updated_at BEFORE UPDATE ON admin_users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_license_customers_updated_at BEFORE UPDATE ON license_customers FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_license_products_updated_at BEFORE UPDATE ON license_products FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_licenses_updated_at BEFORE UPDATE ON licenses FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_license_domains_updated_at BEFORE UPDATE ON license_domains FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_license_devices_updated_at BEFORE UPDATE ON license_devices FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_license_features_updated_at BEFORE UPDATE ON license_features FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_support_tickets_updated_at BEFORE UPDATE ON support_tickets FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- END OF FUNCTIONS AND VIEWS
-- =============================================================================
