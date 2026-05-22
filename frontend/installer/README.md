# License Manager - Installer

A web-based installation wizard for the Enterprise License Management System.

## 🎯 URL

Mounted at **`/install`** (basePath in `next.config.ts`)

## ✨ Features

- **8-step guided installation** with persistent state (Zustand + localStorage)
- **License Verification** — verify license key and activation token
- **Domain Verification** — register installation domain (with wildcard support)
- **Hardware Verification** — auto-collect device fingerprint via SHA-256
- **Database Setup** — test PostgreSQL connection and run migrations
- **Admin Account** — create super-admin with password strength meter
- **Company Setup** — organization details, GST/registration numbers
- **API Configuration** — SMTP, SMS, WhatsApp integrations (optional)
- **Installation Complete** — finalize, create lock file, redirect to admin

## 🛠️ Tech Stack

- **Next.js 15** (App Router, basePath `/install`)
- **React 19 RC**, **TypeScript 5.6**
- **Tailwind CSS 3.4** + **Shadcn UI**
- **Zustand 5** with localStorage persistence
- **Axios** for API calls
- **SubtleCrypto** for SHA-256 fingerprinting

## 🚀 Running

```bash
cd frontend/installer
cp .env.example .env.local
npm install
npm run dev
```

Open **http://localhost:3001/install**

## 🐳 Docker

```bash
docker build -t licensemanager-installer .
docker run -p 3001:3001 -e NEXT_PUBLIC_API_URL=http://api:8080 licensemanager-installer
```

## 🔒 Security

- **Lock file** (`.installation_locked`) prevents re-running after install
- **Step gating** — users can only navigate to completed or current step
- **Hardware fingerprint** locks the license to the installation server
- **Password strength validation** with 5-tier meter

## 🔌 Backend Endpoints

All under `/api/installer/*`:
- `GET /status` — check if already installed
- `POST /verify-license` — validate license key + token
- `POST /verify-domain` — register domain
- `POST /verify-hardware` — register device fingerprint
- `POST /test-database` — test PostgreSQL connection
- `POST /setup-database` — run migrations & seeds
- `POST /create-admin` — create super-admin account
- `POST /save-company` — save organization info
- `POST /configure-api` — save SMTP/SMS/WhatsApp settings
- `POST /finalize` — create lock file, complete installation
