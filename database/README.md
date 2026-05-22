# Database Setup Guide

## PostgreSQL Database for License Management System

### Prerequisites
- PostgreSQL 16 or higher
- Database user with CREATE DATABASE privileges

### Quick Setup

#### 1. Create Database
```bash
createdb license_manager
```

Or using psql:
```sql
CREATE DATABASE license_manager;
```

#### 2. Run Migrations

```bash
# Connect to database
psql -d license_manager

# Run migrations in order
\i migrations/001_initial_schema.sql
\i seeds/002_seed_data.sql
\i migrations/003_functions_and_views.sql
```

#### 3. Verify Installation

```sql
-- Check tables
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Check views
SELECT table_name FROM information_schema.views 
WHERE table_schema = 'public';

-- Check functions
SELECT routine_name FROM information_schema.routines 
WHERE routine_schema = 'public' AND routine_type = 'FUNCTION';

-- Verify seed data
SELECT * FROM admin_users;
SELECT * FROM license_features;
SELECT * FROM license_products;
```

### Database Schema

#### Core Tables
- **admin_users** - System administrators
- **license_customers** - Customers who purchase licenses
- **license_products** - Products available for licensing
- **licenses** - License records with configurations
- **license_domains** - Domain registrations
- **license_devices** - Hardware/device registrations
- **license_activations** - Activation attempts and logs
- **license_validations** - Validation requests and heartbeats
- **license_features** - Available features
- **license_feature_mappings** - Feature assignments to licenses

#### Supporting Tables
- **product_features** - Features available per product
- **product_versions** - Product releases and updates
- **update_downloads** - Update download logs
- **license_history** - License change history
- **support_tickets** - Customer support tickets
- **ticket_comments** - Ticket comments
- **audit_logs** - System-wide audit trail
- **api_logs** - API request/response logs
- **login_history** - Admin login attempts

### Useful Views

```sql
-- Active licenses
SELECT * FROM v_active_licenses;

-- Licenses expiring in next 30 days
SELECT * FROM v_expiring_licenses;

-- Validation statistics
SELECT * FROM v_license_validation_stats;

-- Customer summary
SELECT * FROM v_customer_license_summary;

-- Product revenue
SELECT * FROM v_product_revenue_summary;

-- Daily activation stats
SELECT * FROM v_daily_activation_stats;

-- Device registration summary
SELECT * FROM v_device_registration_summary;

-- Support ticket stats
SELECT * FROM v_support_ticket_stats;

-- Feature usage
SELECT * FROM v_feature_usage_stats;
```

### Useful Functions

```sql
-- Generate license key
SELECT generate_license_key();

-- Generate activation token
SELECT generate_activation_token();

-- Check if license expired
SELECT check_license_expired('license-id-uuid');

-- Check if in grace period
SELECT check_license_in_grace_period('license-id-uuid');

-- Days until expiry
SELECT get_license_days_until_expiry('license-id-uuid');

-- Can activate
SELECT can_license_activate('license-id-uuid');

-- Device limit reached
SELECT check_device_limit_reached('license-id-uuid');

-- Domain limit reached
SELECT check_domain_limit_reached('license-id-uuid');
```

### Default Admin Credentials

**Email:** admin@licensemanager.com  
**Password:** Admin@123456

⚠️ **Important:** Change the default password immediately after first login!

### Connection String Format

```
Host=localhost;Port=5432;Database=license_manager;Username=postgres;Password=yourpassword
```

### Environment Variables

```bash
DATABASE_HOST=localhost
DATABASE_PORT=5432
DATABASE_NAME=license_manager
DATABASE_USER=postgres
DATABASE_PASSWORD=yourpassword
DATABASE_SSL_MODE=Prefer
```

### Backup and Restore

#### Backup
```bash
pg_dump -U postgres license_manager > backup_$(date +%Y%m%d_%H%M%S).sql
```

#### Restore
```bash
psql -U postgres license_manager < backup_20260522_120000.sql
```

### Performance Optimization

All critical columns are indexed:
- Primary keys (UUID)
- Foreign keys
- Frequently queried columns (email, license_key, status, dates)
- Soft delete flags (is_deleted)

### Data Retention

- **audit_logs**: Recommend archiving after 1 year
- **api_logs**: Recommend archiving after 6 months
- **license_validations**: Recommend archiving after 6 months
- **login_history**: Recommend archiving after 1 year

### Security Considerations

1. All tables support soft deletes (is_deleted flag)
2. Audit fields on every table (created_at, updated_at, created_by, updated_by)
3. Automatic timestamp updates via triggers
4. Password hashing with bcrypt (strength 11)
5. MFA support for admin users
6. IP whitelisting capabilities
7. Comprehensive audit logging

### Maintenance

```sql
-- Analyze tables for query optimization
ANALYZE;

-- Vacuum to reclaim space
VACUUM ANALYZE;

-- Reindex for performance
REINDEX DATABASE license_manager;
```

### Monitoring Queries

```sql
-- Database size
SELECT pg_size_pretty(pg_database_size('license_manager'));

-- Table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Active connections
SELECT count(*) FROM pg_stat_activity WHERE datname = 'license_manager';

-- Long running queries
SELECT pid, now() - query_start AS duration, query
FROM pg_stat_activity
WHERE state = 'active' AND now() - query_start > interval '5 minutes';
```

---

**Database Version:** 1.0.0  
**Last Updated:** 2026-05-22
