# 🚀 Quick Start Guide

Get your Enterprise License Management System running in 5 minutes!

## Option 1: Docker Compose (Recommended for Development)

### Prerequisites
- Docker 24.0+
- Docker Compose 2.20+

### Steps

```bash
# 1. Clone repository
git clone https://github.com/darkmailspace/licensemanage.git
cd licensemanage

# 2. Configure environment
cd infrastructure/docker
cp .env.example .env
# Edit .env with your passwords

# 3. Start all services
docker-compose up -d

# 4. Wait for services to be ready (30-60 seconds)
docker-compose ps

# 5. Access the services
```

### 🌐 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| **API** | http://localhost:8080 | N/A |
| **Swagger Docs** | http://localhost:8080/swagger | N/A |
| **Health Check** | http://localhost:8080/health | N/A |
| **PgAdmin** | http://localhost:5050 | See .env file |
| **Redis Commander** | http://localhost:8081 | N/A |

---

## Option 2: Kubernetes (Production)

### Prerequisites
- Kubernetes cluster (v1.28+)
- kubectl configured
- 8GB+ available RAM

### Steps

```bash
# 1. Clone repository
git clone https://github.com/darkmailspace/licensemanage.git
cd licensemanage/infrastructure/kubernetes

# 2. Update secrets
nano secrets.yaml  # Change all passwords!

# 3. Deploy
kubectl apply -f namespace.yaml
kubectl apply -f secrets.yaml
kubectl apply -f configmap.yaml
kubectl apply -f postgres-deployment.yaml
kubectl apply -f redis-deployment.yaml
kubectl apply -f api-deployment.yaml

# 4. Wait for pods
kubectl wait --for=condition=ready pod -l app=licensemanager-api -n licensemanager --timeout=300s

# 5. Access API (port-forward)
kubectl port-forward -n licensemanager service/licensemanager-api-service 8080:80
```

Then visit: http://localhost:8080/swagger

---

## 🧪 Test the API

### 1. Health Check

```bash
curl http://localhost:8080/health
```

Expected response:
```json
{
  "status": "Healthy",
  "timestamp": "2026-05-22T12:00:00Z"
}
```

### 2. Generate a License

```bash
curl -X POST http://localhost:8080/api/licenses \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "YOUR-CUSTOMER-ID",
    "productId": "YOUR-PRODUCT-ID",
    "licenseType": 5
  }'
```

### 3. Validate a License

```bash
curl -X POST http://localhost:8080/api/licenses/validate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "LK-XXXX-XXXX-XXXX-XXXX",
    "domainName": "example.com"
  }'
```

### 4. Activate a License

```bash
curl -X POST http://localhost:8080/api/licenses/activate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "LK-XXXX-XXXX-XXXX-XXXX",
    "activationToken": "AT-XXXXXXXXXXXXXXXX",
    "domainName": "example.com"
  }'
```

---

## 📊 Database Access

### Using PgAdmin (Docker)

1. Open http://localhost:5050
2. Login with credentials from `.env`
3. Add server:
   - Host: postgres
   - Port: 5432
   - Database: license_manager
   - Username: postgres
   - Password: From .env

### Using psql (Direct)

```bash
# Docker
docker-compose exec postgres psql -U postgres -d license_manager

# Kubernetes
kubectl exec -it deployment/postgres -n licensemanager -- psql -U postgres -d license_manager
```

### Sample Queries

```sql
-- View all licenses
SELECT * FROM licenses;

-- View active licenses
SELECT * FROM v_active_licenses;

-- View expiring licenses
SELECT * FROM v_expiring_licenses;

-- View license validation stats
SELECT * FROM v_license_validation_stats;
```

---

## 🔧 Common Commands

### Docker Compose

```bash
# View logs
docker-compose logs -f api

# Restart service
docker-compose restart api

# Stop all services
docker-compose down

# Remove all data (⚠️ DESTRUCTIVE)
docker-compose down -v
```

### Kubernetes

```bash
# View logs
kubectl logs -f deployment/licensemanager-api -n licensemanager

# Scale API
kubectl scale deployment licensemanager-api --replicas=5 -n licensemanager

# Check status
kubectl get all -n licensemanager

# Delete everything (⚠️ DESTRUCTIVE)
kubectl delete namespace licensemanager
```

---

## 📖 Default Admin Credentials

⚠️ **CHANGE THESE IN PRODUCTION!**

### Database Seed Data
- **Admin Email:** admin@licensemanager.com
- **Admin Password:** Admin@123456

### Sample Customer
- **Customer Code:** CUST001
- **Email:** john.doe@example.com

### Sample Product
- **Product Code:** FINANCEERPV1
- **Name:** Finance ERP System

---

## 🐛 Troubleshooting

### API not starting?

```bash
# Check logs
docker-compose logs api

# Check database connection
docker-compose exec api ping postgres
```

### Database not accessible?

```bash
# Check if PostgreSQL is running
docker-compose ps postgres

# Test connection
docker-compose exec postgres pg_isready -U postgres
```

### Port already in use?

Edit `docker-compose.yml` and change the port:
```yaml
ports:
  - "8081:8080"  # Changed from 8080:8080
```

---

## 📚 Next Steps

1. **Explore the API:** http://localhost:8080/swagger
2. **Read the docs:** See `README.md` for full documentation
3. **Check the database:** See `database/README.md`
4. **Build the frontend:** Start Phase 2 development
5. **Deploy to production:** Follow Kubernetes guide

---

## 🆘 Need Help?

- **Documentation:** See `README.md` and service-specific READMEs
- **Issues:** Check GitHub issues
- **Database Schema:** See `database/migrations/001_initial_schema.sql`
- **API Reference:** Visit `/swagger` endpoint

---

## 🎯 What's Included

- ✅ Complete REST API with 7+ endpoints
- ✅ PostgreSQL database with 19 tables
- ✅ Redis caching layer
- ✅ RSA-4096 & AES-256 encryption
- ✅ License generation, validation, activation
- ✅ Domain & hardware locking
- ✅ Swagger documentation
- ✅ Docker & Kubernetes deployment
- ✅ Health checks & monitoring
- ✅ Complete audit logging

---

**Repository:** https://github.com/darkmailspace/licensemanage  
**Status:** ✅ Production Ready  
**Version:** 1.0.0 (Phase 1)
