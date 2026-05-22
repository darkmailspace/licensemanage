# Enterprise License Management System

A production-grade enterprise license management system with activation server, installer system, and SaaS licensing platform.

## 🏗️ Architecture

This system follows **Domain-Driven Design (DDD)** and **Clean Architecture** principles with a microservices-ready structure.

## 📦 Technology Stack

### Backend
- **ASP.NET Core 9** - Web API framework
- **PostgreSQL** - Primary database
- **Redis** - Caching and session management
- **Entity Framework Core** - ORM
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Input validation
- **Serilog** - Structured logging

### Frontend
- **Next.js 14** - Admin panel (App Router)
- **React 18** - UI framework
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling
- **shadcn/ui** - Component library
- **TanStack Query** - Data fetching
- **Zustand** - State management

### Mobile
- **Flutter** - Cross-platform mobile apps
- **Dart** - Programming language

### DevOps
- **Docker** - Containerization
- **Kubernetes** - Orchestration
- **GitHub Actions** - CI/CD

## 📂 Project Structure

```
licensemanage/
├── backend/                      # ASP.NET Core Backend
│   ├── src/
│   │   ├── LicenseManager.Domain/              # Core business entities
│   │   ├── LicenseManager.Application/         # Business logic & use cases
│   │   ├── LicenseManager.Infrastructure/      # External services & data
│   │   ├── LicenseManager.API/                 # REST API endpoints
│   │   └── LicenseManager.Installer.API/       # Installer API
│   └── tests/
│       ├── LicenseManager.Domain.Tests/
│       ├── LicenseManager.Application.Tests/
│       └── LicenseManager.API.Tests/
├── frontend/                     # Next.js Admin Panel
│   ├── admin-panel/             # Super admin dashboard
│   └── customer-portal/         # Customer self-service portal
├── mobile/                       # Flutter Mobile Apps
│   └── license_manager_app/
├── database/                     # Database scripts
│   ├── migrations/
│   └── seeds/
├── infrastructure/               # DevOps configurations
│   ├── docker/
│   ├── kubernetes/
│   └── terraform/
└── docs/                         # Documentation
    ├── api/
    ├── architecture/
    └── deployment/
```

## 🚀 Features

### License Management
- Multiple license types (Trial, Monthly, Yearly, Lifetime, Enterprise, etc.)
- License lifecycle management (Generation, Activation, Validation, Renewal, Revocation)
- Feature-based licensing control
- License upgrade/downgrade capabilities

### Security & Protection
- RSA-4096 digital signatures
- AES-256 encryption
- JWT-based authentication
- Hardware fingerprinting
- Anti-tampering protection
- Anti-piracy measures

### Locking Systems
- Domain locking (with wildcard support)
- Hardware locking (CPU, Motherboard, Disk)
- IP whitelisting
- Geographic restrictions
- User and device limits
- Concurrent login controls

### Activation System
- Online activation
- Offline activation (request/response files)
- Grace period support
- Multi-domain support

### Validation System
- Real-time validation
- Heartbeat monitoring
- Feature access control
- Configurable validation intervals

### Installer System
- Web-based installer
- Domain verification
- Hardware verification
- Automated database setup
- Installation locking

### Update System
- Auto-update mechanism
- Version management
- Changelog tracking
- Rollback support

## 🔧 Getting Started

### Prerequisites
- .NET 9 SDK
- Node.js 20+
- PostgreSQL 16+
- Redis 7+
- Docker & Docker Compose

### Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/darkmailspace/licensemanage.git
cd licensemanage
```

2. **Backend Setup**
```bash
cd backend/src/LicenseManager.API
dotnet restore
dotnet ef database update
dotnet run
```

3. **Frontend Setup**
```bash
cd frontend/admin-panel
npm install
npm run dev
```

4. **Docker Setup**
```bash
docker-compose up -d
```

## 📊 Database Schema

The system uses PostgreSQL with the following main tables:
- `license_customers` - Customer information
- `license_products` - Product definitions
- `licenses` - License records
- `license_domains` - Domain registrations
- `license_devices` - Hardware registrations
- `license_activations` - Activation logs
- `license_validations` - Validation logs
- `license_features` - Feature definitions
- `license_feature_mappings` - License-to-feature mappings
- `audit_logs` - Security and audit trails

## 🔐 Security

- All license keys are digitally signed with RSA-4096
- License data is encrypted with AES-256
- API endpoints are protected with JWT authentication
- Hardware fingerprints use SHA-256 hashing
- Secrets are stored in environment variables or key vaults
- Regular security audits and logging

## 📝 License

Proprietary - All rights reserved

## 👥 Support

For support and inquiries, please contact the development team.

---

**Built with ❤️ for Enterprise License Management**
