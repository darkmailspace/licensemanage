# License Manager API Reference

Complete REST API for the Enterprise License Management System.

**Base URL:** `http://localhost:8080/api`
**Authentication:** JWT Bearer tokens (RFC 6750)

---

## 🔐 Authentication & Authorization

### JWT Authentication
All protected endpoints require a valid JWT in the `Authorization` header:
```
Authorization: Bearer <token>
```

### Role-Based Access Control (RBAC)
Roles are numeric (lower = higher privilege):

| Role | Value | Description |
|------|-------|-------------|
| **SuperAdmin** | 1 | Full access to everything |
| **Admin** | 2 | All non-critical config |
| **Support** | 3 | Read access + ticket management |
| **Viewer** | 4 | Read-only |

### Authorization Policies

| Policy | Required Role |
|--------|---------------|
| `SuperAdmin` | Role = 1 |
| `Admin` | Role ≤ 2 |
| `AdminOrSupport` | Role ≤ 3 |
| `Support` | Role ≤ 3 |
| `Authenticated` | Any authenticated user |

---

## 📋 Endpoints

### `/api/auth` - Authentication

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/login` | Anonymous | Login with email + password |
| POST | `/mfa/verify` | Anonymous | Verify MFA code (6-digit TOTP or backup code) |
| POST | `/refresh` | Anonymous | Refresh access token |
| POST | `/forgot-password` | Anonymous | Request password reset email |
| POST | `/reset-password` | Anonymous | Reset password with token |
| POST | `/logout` | Authenticated | Logout (client discards token) |
| GET | `/me` | Authenticated | Get current user profile |
| POST | `/change-password` | Authenticated | Change own password |
| POST | `/mfa/enable` | Authenticated | Begin MFA setup (returns QR + backup codes) |
| POST | `/mfa/enable/verify` | Authenticated | Confirm MFA setup with code |
| POST | `/mfa/disable` | Authenticated | Disable MFA (requires password) |

**Login response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "abc123...",
    "expiresIn": 3600,
    "user": {
      "id": "uuid",
      "email": "admin@example.com",
      "fullName": "Admin User",
      "role": 1,
      "roleName": "SuperAdmin",
      "mfaEnabled": false
    },
    "requiresMfa": false
  }
}
```

If MFA is enabled, login returns:
```json
{ "success": true, "data": { "requiresMfa": true } }
```

---

### `/api/admin/users` - Admin User Management (SuperAdmin)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | List admin users (paginated, searchable) |
| GET | `/{id}` | Get admin user details |
| POST | `/` | Create new admin user |
| PUT | `/{id}` | Update admin user |
| DELETE | `/{id}` | Soft-delete admin user (cannot delete last super-admin) |
| GET | `/{id}/login-history` | View login history |

---

### `/api/customers` - Customer Management (AdminOrSupport read, Admin write)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List with `?search=&isActive=&isVerified=` |
| GET | `/{id}` | Support+ | Customer + stats |
| GET | `/{id}/licenses` | Support+ | Licenses for this customer |
| POST | `/` | Admin+ | Create customer |
| PUT | `/{id}` | Admin+ | Update customer |
| POST | `/{id}/verify` | Admin+ | Mark customer as verified |
| DELETE | `/{id}` | Admin+ | Soft-delete (no active licenses) |

---

### `/api/products` - Product Management (AdminOrSupport read, Admin write)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List products |
| GET | `/{id}` | Support+ | Product + features + versions |
| POST | `/` | Admin+ | Create product |
| PUT | `/{id}` | Admin+ | Update product |
| DELETE | `/{id}` | Admin+ | Soft-delete (no licenses) |
| POST | `/{id}/versions` | Admin+ | Add a new product version |

---

### `/api/features` - Feature Management

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List features (`?category=&isActive=`) |
| GET | `/{id}` | Support+ | Get feature |
| POST | `/` | Admin+ | Create feature |
| DELETE | `/{id}` | Admin+ | Delete feature (not in use) |

---

### `/api/licenses` - License Management

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List with `?status=&licenseType=&customerId=&productId=` |
| GET | `/{id}` | Support+ | License + relations |
| GET | `/{id}/history` | Support+ | License change history |
| POST | `/` | Admin+ | Generate new license |
| POST | `/validate` | Anonymous | Validate license (used by client apps) |
| POST | `/activate` | Anonymous | Activate license (used by client apps) |
| POST | `/{id}/renew` | Admin+ | Renew (`{renewalMonths}`) |
| POST | `/{id}/suspend` | Admin+ | Suspend (`{reason}`) |
| POST | `/{id}/revoke` | Admin+ | Revoke (`{reason}`) |
| POST | `/{id}/upgrade` | Admin+ | Upgrade (`{newLicenseType}`) |
| POST | `/{id}/features/{featureId}/enable` | Admin+ | Enable feature on license |
| POST | `/{id}/features/{featureId}/disable` | Admin+ | Disable feature |
| DELETE | `/{id}` | SuperAdmin | Hard delete (audit-trail-preserving) |

---

### `/api/domains` - Domain Management

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List with `?isActive=&isVerified=&changeRequested=&licenseId=` |
| GET | `/{id}` | Support+ | Domain details |
| POST | `/{id}/verify` | Admin+ | Mark domain as verified |
| POST | `/{id}/approve-change` | Admin+ | Approve/deny domain change request |
| DELETE | `/{id}` | Admin+ | Remove domain |

---

### `/api/devices` - Device Management

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List with `?isActive=&isDeactivated=&isVirtualMachine=&licenseId=` |
| GET | `/{id}` | Support+ | Device details |
| POST | `/{id}/deactivate` | Admin+ | Deactivate (`{reason}`) |
| POST | `/{id}/reactivate` | Admin+ | Reactivate |
| DELETE | `/{id}` | Admin+ | Remove device |

---

### `/api/activations` - Activation Logs (read-only)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List with `?success=&activationType=&licenseId=&from=&to=` |
| GET | `/{id}` | Support+ | Activation details |
| GET | `/stats?days=7` | Support+ | Aggregated stats |

---

### `/api/validations` - Validation Logs (read-only)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Support+ | List with filters |
| GET | `/stats?hours=24` | Support+ | Aggregated stats with avg response time |

---

### `/api/audit-logs` - Audit Trail (Admin+)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | Admin+ | List with `?entityName=&action=&userId=&from=&to=` |
| GET | `/{id}` | Admin+ | Audit log details |

---

### `/api/dashboard` - Dashboard Aggregations

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/stats` | Support+ | Overall stats (licenses, customers, revenue, activations, success rate) |
| GET | `/recent-activity?limit=20` | Support+ | Latest license history events |
| GET | `/revenue?period=12months` | Support+ | Revenue time series |
| GET | `/licenses?period=12months` | Support+ | License count time series |

---

### `/api/reports` - Reports (Support+)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/revenue?from=&to=` | Support+ | Revenue report (totals, growth, by product, by type) |
| GET | `/licenses` | Support+ | License lifecycle (status, type, monthly trends) |
| GET | `/activations?from=&to=` | Support+ | Activation success/failure with country + reason breakdown |
| GET | `/expiring?days=30` | Support+ | Licenses expiring in N days |
| GET | `/customers` | Support+ | Customer growth + geographic distribution |

---

### `/api/settings` - System Settings (SuperAdmin)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/?category=&revealSecrets=false` | SuperAdmin | List settings (secrets masked) |
| GET | `/{key}` | SuperAdmin | Get a single setting |
| PUT | `/` | SuperAdmin | Bulk update `{ settings: { key: value } }` |
| PUT | `/{key}` | SuperAdmin | Update single setting |
| POST | `/test-smtp` | SuperAdmin | Send test email |

**Setting categories:** `general`, `license`, `email`, `sms`, `whatsapp`, `security`, `api`, `webhook`

---

### `/api/installer` - Installation Wizard (Anonymous, locked after install)

See `InstallerController.cs` for full details. All endpoints disabled once `.installation_locked` exists.

---

### `/api/customer-portal` - Customer Self-Service (Customer JWT)

See `CustomerPortalController.cs` and customer-portal frontend README.

---

## 🔢 Pagination

All list endpoints accept:
- `page` (default 1, min 1)
- `pageSize` (default 20, max 200)
- `search` (free-text search)
- `sortBy` (entity-specific field name)
- `sortDir` (`asc` | `desc`, default `desc`)

Response envelope:
```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 1284,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 65,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

---

## 📦 Response Envelope

Every endpoint returns the same shape:

```json
{
  "success": true,
  "data": <T>,
  "message": "Optional success message",
  "error": "Optional error message"
}
```

For errors:
```json
{
  "success": false,
  "error": "Human-readable error message",
  "errors": ["Optional list of validation errors"]
}
```

---

## 🔧 Error Codes

| HTTP | Meaning |
|------|---------|
| 200 | Success |
| 400 | Bad request (validation error) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient role) |
| 404 | Not found |
| 500 | Internal server error |

---

## 🔐 Default Credentials (CHANGE in production)

- **Email:** `admin@licensemanager.com`
- **Password:** `Admin@123456`
- **Role:** SuperAdmin (1)

Password is hashed with BCrypt work factor 11.

---

## 🚀 Quick Test

```bash
# 1. Login
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@licensemanager.com","password":"Admin@123456"}'

# 2. Use the returned accessToken
TOKEN="<paste-here>"

# 3. List customers
curl http://localhost:8080/api/customers \
  -H "Authorization: Bearer $TOKEN"

# 4. Dashboard stats
curl http://localhost:8080/api/dashboard/stats \
  -H "Authorization: Bearer $TOKEN"
```

---

**Built with ASP.NET Core 9 · BCrypt · JWT · PostgreSQL · EF Core 9**
