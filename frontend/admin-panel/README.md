# License Manager - Admin Panel

A modern, production-grade admin panel for the Enterprise License Management System, built with Next.js 15, TypeScript, Tailwind CSS, and Shadcn UI.

## ✨ Features

### 🔐 Authentication
- Email & password login
- Two-factor authentication (MFA) with 6-digit code
- Forgot password flow
- JWT-based session with refresh tokens
- IP whitelisting support

### 📊 Dashboard
- Real-time statistics (licenses, customers, revenue, activations)
- Revenue chart with 12-month trend
- Activation success/failure analytics
- License type distribution pie chart
- Recent activity feed

### 🔑 License Management
- **List view** with searchable, paginated table
- **Create license** wizard with full configuration
- **License details** with tabs (Overview, Domains, Devices, History)
- **Edit license** for limits, security, and settings
- **Renew license** with flexible periods (1-60 months)
- **Suspend license** with reason tracking
- **Revoke license** with permanent action confirmation
- License key generation and copying

### 👥 Customer & Product Management
- Customer list with verification status
- Product catalog with grid view
- Product version management
- Customer contact tracking

### 🌐 Domain & Device Management
- Domain registration with wildcard support
- Domain verification workflow
- Device fingerprint tracking
- Hardware identification (CPU, MAC, etc.)
- VM detection
- Geographic tracking

### 📋 Activity & Logs
- **Activation logs** with success/failure tracking
- **Validation logs** with heartbeat monitoring
- **Audit logs** with filtering by action, entity, time range
- Response time monitoring

### 📈 Reports & Analytics
- Revenue reports with multiple chart views
- License lifecycle reports
- Customer growth analytics
- Performance metrics
- Export to CSV/PDF

### ⚙️ Settings
- General application settings
- Profile management
- Security (password, MFA, IP whitelist)
- SMTP email configuration
- Notification preferences (Email, SMS, WhatsApp)
- API configuration with rate limiting
- Webhook management

## 🛠️ Tech Stack

- **Framework:** Next.js 15 (App Router)
- **Language:** TypeScript 5.6
- **UI Library:** React 19 RC
- **Styling:** Tailwind CSS 3.4
- **Components:** Shadcn UI (Radix UI primitives)
- **Forms:** React Hook Form + Zod
- **Data Fetching:** TanStack Query (React Query)
- **State:** Zustand with persistence
- **Charts:** Recharts
- **Tables:** TanStack Table
- **Icons:** Lucide React
- **HTTP:** Axios with interceptors
- **Notifications:** Sonner
- **Dates:** date-fns
- **Theme:** next-themes (dark mode support)

## 📦 Installation

### Prerequisites
- Node.js 22+
- npm or yarn or pnpm

### Setup

```bash
# Install dependencies
cd frontend/admin-panel
npm install

# Configure environment
cp .env.example .env.local
# Edit .env.local with your API URL

# Run development server
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) to view the app.

### Environment Variables

```env
NEXT_PUBLIC_API_URL=http://localhost:8080
NEXT_PUBLIC_TOKEN_COOKIE_NAME=lm_admin_token
NEXT_PUBLIC_REFRESH_TOKEN_COOKIE_NAME=lm_admin_refresh
```

## 🚀 Production Build

```bash
# Build for production
npm run build

# Start production server
npm start
```

## 🐳 Docker

```bash
# Build image
docker build -t licensemanager-admin .

# Run container
docker run -p 3000:3000 \
  -e NEXT_PUBLIC_API_URL=http://api.licensemanager.com \
  licensemanager-admin
```

## 📂 Project Structure

```
admin-panel/
├── app/                          # Next.js 15 App Router
│   ├── (auth)/                  # Authentication pages
│   │   ├── login/               # Login with credentials
│   │   ├── mfa/                 # MFA verification
│   │   ├── forgot-password/     # Password reset flow
│   │   └── layout.tsx           # Auth layout
│   ├── (dashboard)/             # Authenticated pages
│   │   ├── dashboard/           # Main dashboard
│   │   ├── licenses/            # License CRUD
│   │   │   ├── create/          # Create license
│   │   │   └── [id]/
│   │   │       ├── edit/
│   │   │       ├── renew/
│   │   │       ├── suspend/
│   │   │       └── revoke/
│   │   ├── customers/
│   │   ├── products/
│   │   ├── domains/
│   │   ├── devices/
│   │   ├── activations/
│   │   ├── validations/
│   │   ├── audit-logs/
│   │   ├── reports/
│   │   ├── settings/
│   │   └── layout.tsx           # Dashboard layout
│   ├── globals.css
│   ├── layout.tsx               # Root layout
│   ├── page.tsx
│   └── providers.tsx            # React Query + Theme
├── components/
│   ├── ui/                      # Shadcn UI components
│   ├── layout/                  # Sidebar, Header
│   ├── dashboard/               # Stat cards, charts
│   └── shared/                  # PageHeader, DataTable, etc.
├── lib/
│   ├── api.ts                   # Axios client + API services
│   ├── constants.ts             # App constants
│   └── utils.ts                 # Utility functions
├── hooks/
│   └── use-auth.ts
├── stores/
│   └── auth-store.ts            # Zustand auth store
├── types/
│   └── index.ts                 # TypeScript types
├── middleware.ts                # Next.js auth middleware
├── tailwind.config.ts
├── tsconfig.json
└── next.config.ts
```

## 🎨 UI Components

Built with [Shadcn UI](https://ui.shadcn.com) (New York style):
- Button, Input, Label, Textarea
- Card, Badge, Avatar, Skeleton
- Table, Dialog, Tooltip
- Select, Switch, Checkbox, Tabs
- Dropdown Menu, Separator
- Sonner (toast notifications)

## 🔐 Authentication Flow

1. User enters email/password at `/login`
2. API returns either:
   - `{ accessToken, user }` → user logged in
   - `{ requiresMfa: true }` → redirect to `/mfa`
3. MFA: user enters 6-digit code → API returns `{ accessToken, user }`
4. Tokens stored in cookies (HttpOnly via middleware)
5. Auto-refresh on 401 using refresh token
6. Middleware protects authenticated routes

## 🎯 Key Features

### Type-Safe API Client
```typescript
const license = await licensesApi.get("license-id");
await licensesApi.renew(id, 12); // 12 months
await licensesApi.suspend(id, "Payment overdue");
```

### Reusable Data Table
```tsx
<DataTable
  columns={columns}
  data={items}
  searchable
  pagination={{ page, pageSize, total, onPageChange }}
  rowActions={(item) => <ActionsMenu item={item} />}
/>
```

### Status Badges
```tsx
<StatusBadge status={license.status} />        // License status
<StatusBadge status={result} type="validation" />
<StatusBadge status={status} type="ticket" />
```

### Confirm Dialog
```tsx
<ConfirmDialog
  open={open}
  title="Suspend license?"
  description="This will disable the license."
  variant="destructive"
  onConfirm={handleSuspend}
/>
```

## 🌗 Dark Mode

Full dark mode support via `next-themes`. Toggle in header.

## 🔒 Security

- JWT with refresh tokens stored in secure cookies
- CSRF protection via SameSite cookies
- Auto-logout on token expiration
- Middleware-level route protection
- IP whitelisting support
- MFA enforcement
- Audit trail for all actions

## 📝 License

Proprietary - All rights reserved

---

**Version:** 1.0.0  
**Phase:** 2 (Admin Panel)
