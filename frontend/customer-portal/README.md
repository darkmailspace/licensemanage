# License Manager - Customer Portal

Self-service portal for customers to manage their licenses, view invoices, and get support.

## 🎯 URL

Mounted at **`/client`** with login at **`/client/login`** (basePath in `next.config.ts`)

## ✨ Features

### Authentication
- Email/password login
- Forgot password flow
- JWT-based session via secure cookies
- Auto-redirect on 401

### Dashboard
- Quick stats (active licenses, domains, devices, open tickets)
- Renewal alerts for expiring licenses
- Quick action cards
- Active license overview

### License Management
- **List view** with status badges and quick actions
- **Detailed view** with tabs: Overview, Security, Usage
- License key copy-to-clipboard
- Days-until-expiry warnings

### Renew & Upgrade
- **Renew** — 6 preset periods (1, 3, 6, 12, 24 months) + custom
- **Upgrade** — compare 3 plans (Yearly, Enterprise, Franchise) with feature matrix
- Live expiry preview
- Recommended plan highlighting

### Domains & Devices
- View registered domains with verification status
- Wildcard and primary indicators
- Device list with OS, IP, location, VM detection
- Self-service device deactivation

### Updates
- Available updates with major/required badges
- Changelog viewing per version
- Download tracking
- Installed versions history

### Support
- Create tickets with priority selection
- Ticket list with status, priority, comment counts
- 4-priority levels (Low, Medium, High, Critical)
- Real-time relative timestamps

### Invoices
- Total paid / pending stats
- Invoice list with status badges
- One-click PDF download
- Pay Now action for pending invoices

### Profile
- Personal info management
- Company details
- Password change

## 🛠️ Tech Stack

- **Next.js 15** with App Router (basePath `/client`)
- **React 19 RC**, **TypeScript 5.6**
- **Tailwind CSS 3.4** + **Shadcn UI**
- **TanStack Query 5** for data fetching
- **Zustand 5** with persistence
- **Axios** with interceptors
- **next-themes** for dark mode

## 🚀 Running

```bash
cd frontend/customer-portal
cp .env.example .env.local
npm install
npm run dev
```

Open **http://localhost:3002/client/login**

## 🐳 Docker

```bash
docker build -t licensemanager-customer-portal .
docker run -p 3002:3002 -e NEXT_PUBLIC_API_URL=http://api:8080 licensemanager-customer-portal
```

## 🔌 Backend Endpoints

All under `/api/customer-portal/*`:

**Auth:**
- `POST /auth/login`, `POST /auth/forgot-password`, `POST /auth/logout`, `GET /auth/me`

**Dashboard & Licenses:**
- `GET /dashboard`
- `GET /licenses`, `GET /licenses/{id}`
- `POST /licenses/{id}/renew`, `POST /licenses/{id}/upgrade`

**Domains & Devices:**
- `GET /licenses/{licenseId}/domains`
- `GET /licenses/{licenseId}/devices`

**Updates:**
- `GET /updates`, `GET /updates/{versionId}/download`

**Tickets:**
- `GET /tickets`, `GET /tickets/{id}`, `POST /tickets`
- `POST /tickets/{ticketId}/comments`

**Invoices:**
- `GET /invoices`, `GET /invoices/{id}`, `GET /invoices/{id}/download`

**Profile:**
- `GET /profile`, `PUT /profile`
