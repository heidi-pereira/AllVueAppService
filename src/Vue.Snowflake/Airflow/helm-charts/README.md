# Helm Setup

## Create namespaces
```bash
kubectl create namespace <env> --dry-run=client --output yaml | kubectl apply -f -
kubectl create namespace external-secrets-system --dry-run=client --output yaml | kubectl apply -f -
```

## Create service accounts
```bash
kubectl apply -f ./<env>/serviceaccount.yaml
```

## Install external-secrets operator
```bash
helm install external-secrets external-secrets/external-secrets --namespace external-secrets-system --create-namespace --set installCRDs=true
```

## Create cluster secret store
```bash
kubectl apply -f cluster-secret-store.yaml
```

## Create external secrets
```bash
kubectl apply -f ./<env>/externalsecret_airflow-aks-azure-logs-secrets.yaml
kubectl apply -f ./<env>/externalsecret_snowflake-key-encryption-password.yaml
kubectl apply -f ./<env>/externalsecret_snowflake-user-key.yaml
kubectl apply -f ./<env>/externalsecret_fernet-key.yaml
```

## Create federated credentials for Azure identity
```bash
az identity federated-credential create --name external-secret-operator --identity-name my-airflow-identity --resource-group Savanta_Tech_Test --issuer https://uksouth.oic.prod-aks.azure.com/c880139e-fd32-4303-a6c4-3c775406e4c5/a8e9d65c-7887-4889-b63e-a1befad28c39/ --subject system:serviceaccount:<env>:airflow --audiences api://AzureADTokenExchange --output table
```

## Create persistent volumes and claims
```bash
kubectl apply -f ./<env>/pv.yaml
kubectl apply -f ./<env>/pvc.yaml
```

## Install airflow helm repo
```bash
helm repo add apache-airflow https://airflow.apache.org
helm repo update
helm search repo airflow
```

## Install airflow chart
```bash
helm install airflow apache-airflow/airflow --namespace <env> --create-namespace -f ./<env>/airflow_values.yaml --debug
```

## Verify installation
```bash
kubectl get pods -o wide -n <env>
```

## Port-forward to verify airflow webserver UI
```bash
kubectl port-forward svc/airflow-webserver 8080:8080 -n <env>
```

