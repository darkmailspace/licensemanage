# Docker Deployment Guide

## Quick Start

### 1. Prerequisites

- Docker Engine 24.0+
- Docker Compose 2.20+
- 4GB+ RAM available
- 20GB+ disk space

### 2. Setup Environment Variables

```bash
# Copy example environment file
cp .env.example .env

# Edit .env with your credentials
nano .env
```

### 3. Start Services

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Check status
docker-compose ps
```

### 4. Initialize Database

```bash
# Wait for PostgreSQL to be ready
docker-compose exec postgres pg_isready -U postgres

# Run migrations
docker-compose exec postgres psql -U postgres -d license_manager -f /docker-entrypoint-initdb.d/001_initial_schema.sql
docker-compose exec postgres psql -U postgres -d license_manager -f /docker-entrypoint-initdb.d/003_functions_and_views.sql
```

### 5. Access Services

- **API**: http://localhost:8080
- **API Swagger**: http://localhost:8080/swagger
- **PgAdmin**: http://localhost:5050
- **Redis Commander**: http://localhost:8081

## Service Details

### License Manager API
- **Port**: 8080
- **Health Check**: http://localhost:8080/health
- **Documentation**: http://localhost:8080/swagger

### PostgreSQL Database
- **Port**: 5432
- **Database**: license_manager
- **Username**: postgres
- **Password**: Set in .env

### Redis Cache
- **Port**: 6379
- **Password**: Set in .env

### PgAdmin (Database Management)
- **Port**: 5050
- **Default Email**: Set in .env
- **Default Password**: Set in .env

### Redis Commander (Redis Management)
- **Port**: 8081

## Common Commands

### Start Services
```bash
docker-compose up -d
```

### Stop Services
```bash
docker-compose stop
```

### Restart Services
```bash
docker-compose restart
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f postgres
docker-compose logs -f redis
```

### Check Status
```bash
docker-compose ps
```

### Execute Commands in Container
```bash
# PostgreSQL
docker-compose exec postgres psql -U postgres -d license_manager

# API
docker-compose exec api /bin/bash

# Redis
docker-compose exec redis redis-cli -a <password>
```

### Rebuild and Restart
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

## Database Management

### Create Backup
```bash
docker-compose exec postgres pg_dump -U postgres license_manager > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore Backup
```bash
docker-compose exec -T postgres psql -U postgres -d license_manager < backup_20260522_120000.sql
```

### Access Database
```bash
docker-compose exec postgres psql -U postgres -d license_manager
```

## Redis Management

### Access Redis CLI
```bash
docker-compose exec redis redis-cli -a <password>
```

### Check Redis Info
```bash
docker-compose exec redis redis-cli -a <password> INFO
```

### Flush All Data (CAUTION!)
```bash
docker-compose exec redis redis-cli -a <password> FLUSHALL
```

## Monitoring

### Check Resource Usage
```bash
docker stats
```

### View Container Processes
```bash
docker-compose top
```

## Troubleshooting

### API Not Starting

1. Check logs:
```bash
docker-compose logs api
```

2. Check database connection:
```bash
docker-compose exec api ping postgres
```

3. Restart service:
```bash
docker-compose restart api
```

### Database Connection Error

1. Ensure PostgreSQL is running:
```bash
docker-compose ps postgres
```

2. Check PostgreSQL logs:
```bash
docker-compose logs postgres
```

3. Test connection:
```bash
docker-compose exec postgres pg_isready -U postgres
```

### Port Already in Use

If ports are already in use, modify them in `docker-compose.yml`:

```yaml
ports:
  - "8081:8080"  # Change 8080 to 8081
```

### Reset Everything

⚠️ **WARNING**: This will delete all data!

```bash
docker-compose down -v
docker-compose up -d
```

## Production Deployment

### Security Hardening

1. **Change Default Passwords**: Update all credentials in `.env`
2. **Disable Debug Tools**: Remove pgadmin and redis-commander services
3. **Enable HTTPS**: Configure SSL certificates
4. **Restrict Access**: Use firewall rules
5. **Regular Backups**: Set up automated backup schedule

### Performance Tuning

1. **Increase Resources**: Adjust memory and CPU limits
2. **Connection Pooling**: Configure PostgreSQL connection limits
3. **Redis Memory**: Set maxmemory policy
4. **Log Rotation**: Configure log file rotation

### Example Production docker-compose.yml Changes

```yaml
# Remove development tools
# Comment out or remove:
# - pgadmin
# - redis-commander

# Add resource limits
services:
  api:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 512M
```

## Maintenance

### Update Services

```bash
# Pull latest images
docker-compose pull

# Restart with new images
docker-compose up -d
```

### Clean Up

```bash
# Remove stopped containers
docker-compose rm -f

# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune
```

## Environment Variables Reference

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| POSTGRES_PASSWORD | PostgreSQL password | postgres | Yes |
| REDIS_PASSWORD | Redis password | redis123 | Yes |
| JWT_SECRET | JWT signing secret | (example) | Yes |
| PGADMIN_EMAIL | PgAdmin login email | admin@licensemanager.com | No |
| PGADMIN_PASSWORD | PgAdmin login password | admin | No |
| ASPNETCORE_ENVIRONMENT | ASP.NET environment | Production | No |

## Support

For issues and questions:
1. Check logs: `docker-compose logs -f`
2. Review documentation
3. Check GitHub issues
4. Contact support team

---

**Docker Version:** 24.0+  
**Docker Compose Version:** 2.20+  
**Last Updated:** 2026-05-22
