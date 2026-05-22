# Kubernetes Deployment Guide

## Prerequisites

- Kubernetes cluster (v1.28+)
- kubectl configured
- Helm 3 (optional, for additional tools)
- NGINX Ingress Controller
- Cert-Manager (for SSL certificates)

## Deployment Steps

### 1. Create Namespace

```bash
kubectl apply -f namespace.yaml
```

### 2. Create Secrets

⚠️ **Important:** Update the secrets in `secrets.yaml` with your actual credentials before applying!

```bash
# Edit secrets.yaml with your actual credentials
kubectl apply -f secrets.yaml
```

### 3. Create ConfigMap

```bash
kubectl apply -f configmap.yaml
```

### 4. Deploy PostgreSQL

```bash
kubectl apply -f postgres-deployment.yaml
```

Wait for PostgreSQL to be ready:
```bash
kubectl wait --for=condition=ready pod -l app=postgres -n licensemanager --timeout=300s
```

### 5. Initialize Database

Run database migrations:
```bash
# Get PostgreSQL pod name
POSTGRES_POD=$(kubectl get pod -l app=postgres -n licensemanager -o jsonpath="{.items[0].metadata.name}")

# Copy migration files
kubectl cp ../../database/migrations ${POSTGRES_POD}:/tmp/migrations -n licensemanager

# Execute migrations
kubectl exec -it ${POSTGRES_POD} -n licensemanager -- psql -U postgres -d license_manager -f /tmp/migrations/001_initial_schema.sql
kubectl exec -it ${POSTGRES_POD} -n licensemanager -- psql -U postgres -d license_manager -f /tmp/migrations/003_functions_and_views.sql

# Execute seed data
kubectl cp ../../database/seeds ${POSTGRES_POD}:/tmp/seeds -n licensemanager
kubectl exec -it ${POSTGRES_POD} -n licensemanager -- psql -U postgres -d license_manager -f /tmp/seeds/002_seed_data.sql
```

### 6. Deploy Redis

```bash
kubectl apply -f redis-deployment.yaml
```

Wait for Redis to be ready:
```bash
kubectl wait --for=condition=ready pod -l app=redis -n licensemanager --timeout=300s
```

### 7. Deploy API

```bash
kubectl apply -f api-deployment.yaml
```

Wait for API to be ready:
```bash
kubectl wait --for=condition=ready pod -l app=licensemanager-api -n licensemanager --timeout=300s
```

### 8. Deploy Ingress (Optional)

If you have NGINX Ingress Controller and Cert-Manager installed:

```bash
kubectl apply -f ingress.yaml
```

## Verification

### Check All Resources

```bash
kubectl get all -n licensemanager
```

### Check Pod Status

```bash
kubectl get pods -n licensemanager
```

### View Logs

```bash
# API Logs
kubectl logs -f deployment/licensemanager-api -n licensemanager

# PostgreSQL Logs
kubectl logs -f deployment/postgres -n licensemanager

# Redis Logs
kubectl logs -f deployment/redis -n licensemanager
```

### Test API

```bash
# Port forward to access API locally
kubectl port-forward -n licensemanager service/licensemanager-api-service 8080:80

# Test health endpoint
curl http://localhost:8080/health

# Test API
curl http://localhost:8080/
```

## Scaling

### Manual Scaling

```bash
kubectl scale deployment licensemanager-api --replicas=5 -n licensemanager
```

### Auto-scaling

The HorizontalPodAutoscaler is already configured in `api-deployment.yaml`:
- Min replicas: 3
- Max replicas: 10
- CPU threshold: 70%
- Memory threshold: 80%

View HPA status:
```bash
kubectl get hpa -n licensemanager
```

## Monitoring

### Check Resource Usage

```bash
kubectl top pods -n licensemanager
kubectl top nodes
```

### Check Events

```bash
kubectl get events -n licensemanager --sort-by='.lastTimestamp'
```

## Troubleshooting

### Pod not starting

```bash
# Describe pod to see events
kubectl describe pod <pod-name> -n licensemanager

# Check logs
kubectl logs <pod-name> -n licensemanager

# Check previous logs if pod restarted
kubectl logs <pod-name> -n licensemanager --previous
```

### Database Connection Issues

```bash
# Test PostgreSQL connection from API pod
kubectl exec -it deployment/licensemanager-api -n licensemanager -- /bin/sh
# Then inside the pod:
# ping postgres-service
# nc -zv postgres-service 5432
```

### Redis Connection Issues

```bash
# Test Redis connection
kubectl exec -it deployment/redis -n licensemanager -- redis-cli -a <password> ping
```

## Backup and Restore

### PostgreSQL Backup

```bash
# Create backup
kubectl exec deployment/postgres -n licensemanager -- pg_dump -U postgres license_manager > backup.sql

# Copy backup out of cluster
kubectl cp licensemanager/postgres-<pod-id>:/backup.sql ./backup.sql
```

### PostgreSQL Restore

```bash
# Copy backup to pod
kubectl cp ./backup.sql licensemanager/postgres-<pod-id>:/backup.sql

# Restore
kubectl exec deployment/postgres -n licensemanager -- psql -U postgres license_manager < /backup.sql
```

## Security Considerations

1. **Update Secrets**: Change all default passwords in `secrets.yaml`
2. **Network Policies**: Consider adding NetworkPolicy resources
3. **Pod Security**: Enable Pod Security Standards
4. **RBAC**: Configure proper Role-Based Access Control
5. **Image Scanning**: Scan container images for vulnerabilities
6. **TLS/SSL**: Use cert-manager for automatic certificate management

## Cleanup

To remove all resources:

```bash
kubectl delete namespace licensemanager
```

## Production Checklist

- [ ] Update all secrets with strong passwords
- [ ] Configure proper persistent volume storage class
- [ ] Set up monitoring (Prometheus + Grafana)
- [ ] Configure logging (ELK stack or Loki)
- [ ] Set up backup automation
- [ ] Configure network policies
- [ ] Enable pod security policies
- [ ] Set up CI/CD pipeline
- [ ] Configure DNS for ingress
- [ ] Set up SSL certificates
- [ ] Configure resource limits properly
- [ ] Test disaster recovery procedures

---

**Kubernetes Version:** 1.28+  
**Last Updated:** 2026-05-22
