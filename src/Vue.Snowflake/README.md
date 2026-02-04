
## Airflow
### Testing DAGS Locally

- Install WSL
- Install UV: https://docs.astral.sh/uv/getting-started/installation/
- **If you want DB connection - log in as your .admin account to Azure : `az login`**
- Run `wsl` from the `Vue.Showflake` folder eg.`cd /mnt/c/dev/repos/Vue/src/Vue.Snowflake`
- Run `source uv-setup.sh` to configure uv
    - If this errors out and crashes your terminal, your azure cli token may need refreshing with `az logout` and `az login`
    - If you receive a permissioning error from Azure when the script attempts to access Azure Key Vault you'll need to add an Access Policy in bv-airflow-vault for yourself
    - If you have any syntax errors these may be resolved by installing dos2unix and running `dos2unix uv-setup.sh`
    - If you get an error for `Connection not found` for `test_mssql` then leave this until any other issues are fixed because it creates it at the end of the first otherwise successful run through.
- Ensure vpn connected and confirm that you can run test dag to mssql server test server:
    - `uv run airflow dags test test_azure_mssql_connection`
        - or
    - `uv run ./Airflow/dags/test_azure_mssql_connection.py`

### DAGs in AKS

#### Accessing the Cluster for the First Time
1. Connect to the VPN
2. In `az account show`, ensure you are logged in to your admin account on the Dev/Spikes subscription
3. Connect kubectl to AKS:
```pwsh
az aks get-credentials --resource-group Savanta_Tech_Test --name airflow
```
4. See if you can view the pods in the cluster and their status:
```pwsh
kubectl get pods --namespace <test|beta|live>
```
5. To access the web UI, run this command and go to localhost:8080
```pwsh
kubectl port-forward svc/airflow-api-server 8080:8080 -n <test|beta|live>
```
#### Development Cycle

| Env  | Github Branch | Port Forward Command                                               |
|:-----|:--------------|:-------------------------------------------------------------------|
| Test | `airflow-test`  | `kubectl port-forward svc/airflow-api-server 8080:8080 -n test`    |
| Beta | `airflow-beta`  | `kubectl port-forward svc/airflow-api-server 8080:8080 -n beta`    |
| Live | `airflow-live`  | `kubectl port-forward svc/airflow-api-server 8080:8080 -n live`    |

- Push your changes to the corresponding branch for the env.
- Run the port-forward command.
- Head to `localhost:8080` (or whichever port you wish to bind) to view the logs and status of the DAGs.

### DAG Development
See the following [README](.\Airflow\dags\README.md) for info related to the DAG code

### Helm Config
See the following [README](.\Airflow\helm-charts\README.md) for some specific commands used during the Helm charts setup

### Extending the Docker image

This is for adding whatever we want to the base Aiflow image, e.g. pip or apt libraries, packaging DAGs directly with the image, setting up Microsoft SSO auth etc etc

https://airflow.apache.org/docs/helm-chart/stable/production-guide.html#extending-and-customizing-airflow-image

For instance, we can upgrade our Airflow image to use the latest version supported in Helm charts (currently 3.0.2): `helm search repo airflow`

#### Using the Dockerfile
1. Login to ACR: `az acr login --name savantabvairflow`
2. Run Docker desktop
3. Build the image (change tag as required): `docker build -t savantabvairflow.azurecr.io/airflow:3.0.2 .`
4. Push to registry: `docker push savantabvairflow.azurecr.io/airflow:3.0.2`
5. With the airflow_values.yaml file attached (edit with the required tag accordingly):
    1. Helm uninstall current airflow pods: `helm uninstall airflow --namespace test`
    2. Helm install: `helm install airflow apache-airflow/airflow --namespace test --create-namespace -f airflow_values.yaml --debug`
6. Check if it's been successfully installed:
    1. Get pods: `kubectl get pods -n test`
    2. Pip list: `kubectl exec -it airflow-scheduler-64c8684bf-v2lf5 -n airflow -- pip list`

---

### Adding a Secret to Azure Key Vault and using it in the AKS cluster with Helm External-Secret

#### Step 1: Add Secret to Azure Key Vault

```bash
# Add a secret to Azure Key Vault
az keyvault secret set --vault-name bv-airflow-vault --name <secret_name> --value <secret_value>
```

#### Step 2: Create ExternalSecret
`refreshInterval` value is how frequently it checks for changes in Azure Key Vault
```yaml
# externalsecret.yaml
apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
 name: <secret_name>
 namespace: test
spec:
 refreshInterval: "1h"
 secretStoreRef:
    kind: ClusterSecretStore 
    name: azure-store
 target:
   name: <secret_name>
 data:
 - secretKey: <secret_name>
   remoteRef:
     key: <secret_name_in_key_vault>
```
`kubectl apply -f externalsecret.yaml -n test`

#### Step 3: Verify the setup
```bash
# Check if ExternalSecret is synced
kubectl get externalsecret -n test

# Check if the secret was created
kubectl get secret -n test

# Check secret content (base64 encoded)
kubectl get secret app-secret -n test -o yaml

# Check External Secrets Operator logs
kubectl logs -n external-secrets deployment/external-secrets
```

#### Step 4: Add it to the airflow_values.yaml file
```yaml
extraEnv: |
 - name: AIRFLOW__CORE__DEFAULT_TIMEZONE
   value: 'America/New_York'
 - name: SNOWFLAKE_PRIVATE_KEY
   valueFrom:
     secretKeyRef:
       name: snowflake-user-key
       key: snowflake-user-key
 - name: SNOWFLAKE_KEY_ENCRYPTION_PASSWORD
   valueFrom:
     secretKeyRef:
       name: snowflake-key-encryption-password
       key: snowflake-key-encryption-password
```

#### Step 5: update cluster
```bash
helm upgrade airflow apache-airflow/airflow --namespace test --create-namespace -f airflow_values.yaml --debug
```

#### Step 6: Use it in the DAG
```python
private_key_file_pwd = os.environ.get('SNOWFLAKE_KEY_ENCRYPTION_PASSWORD')
```

---


### SQL Server Change Tracking Setup

```sql
-- Enable change tracking at DB level
ALTER DATABASE [BrandVueMeta]  
SET CHANGE_TRACKING = ON  
(CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);

-- Enable change tracking on the desired table(s)
ALTER TABLE [BrandVueMeta].[dbo].[Features]
ENABLE CHANGE_TRACKING
WITH (TRACK_COLUMNS_UPDATED = ON);

-- Grant permissions to the user for change tracking
USE [BrandVueMeta];
GRANT VIEW CHANGE TRACKING ON SCHEMA::dbo TO airflow_user;
```

# Vue snowflake schema

There's one file per object. Dynamic tables are used wherever possible. Occasionally a stream/task combo is required to achieve the same.

## Schema relationships

Survey metadata - merged together:
raw_survey -> impl_survey -> impl_response_set_unconfigured

AllVue Config - transformed into more useable form:
raw_config -> impl_sub_product + impl_weight

Response set - the response level dataset for an AllVue subset:
impl_response_set_unconfigured + impl_sub_product + raw_survey.answers -> impl_response_set

Result - the weighted results for an AllVue subset:
impl_response_set + impl_weight -> impl_result


## Patterns

### What impl means

These schemas should only be depended upon by other parts of this database, not by an external script.
They may change at any time.
If you want to use something here, create something in the internal schema that uses this and exposes a stable enough interface for internal savanta users to code against.

### Merging

To turn the implementation of how a survey was run into a unified model for analysis a set of responses, a bunch of merging happens.

We merge metadata to minimise configuration required. e.g. When someone creates a "competitor brand set", they don't want to create it 12 times, they want to see the same competitors across the dashboard.
Each time something is merged, we get a table of the "canonical" rows (canonical for the schema it's within, e.g. response_set), and a _mappings table which for any "alternative" row can tell you the id of the canonical one.


## Dev dbs

First you will need a dev role:
```sql

set user_to_create_for = (
    select current_user()
    -- 'graham.helliwell@savanta.com' -- e.g. can hardcode
);
set owner_role_name = (
    select 'DEV_' || upper(replace(split_part($user_to_create_for, '@', 0), '.', '_')) || '__VUE__OWNER__D_ROLE'
);

set user_role_name = (
    select upper(replace(split_part(current_user(), '@', 0), '.', '_')) || '__U_ROLE'
);
create role if not exists identifier($owner_role_name);
grant usage on warehouse warehouse_xsmall to role identifier($owner_role_name);

grant role identifier($owner_role_name) to role identifier($user_role_name);
```



This is how I'm creating a reasonable looking db whilst waiting for the full db to be ready
```sql

set user_to_create_for = (
    select current_user()
    -- 'graham.helliwell@savanta.com' --e.g.
);

set old_db_name = 'LIVE__VUE'; -- Basis for the new db
set new_db_name = (
    select 'DEV_' || replace(split($user_to_create_for, '@')[0], '.', '_') || '__VUE__' || replace(current_date(), '-', '_')
);
set owner_role_name = (
    select 'DEV_' || upper(replace(split_part($user_to_create_for, '@', 0), '.', '_')) || '__VUE__OWNER__D_ROLE'
);

drop database if exists identifier($new_db_name);
create database identifier($new_db_name) clone identifier($old_db_name);
use database identifier($new_db_name); -- Be explicit even though this happens automatically

grant ownership on database identifier($new_db_name) to role identifier($owner_role_name) revoke current grants;
grant ownership on all schemas in database identifier($new_db_name) to role identifier($owner_role_name) revoke current grants;
grant all on database identifier($new_db_name) to role identifier($owner_role_name);
grant all on all schemas in database identifier($new_db_name) to role identifier($owner_role_name);
grant all on future schemas in database identifier($new_db_name) to role identifier($owner_role_name);
grant all on all tables in database identifier($new_db_name) to role identifier($owner_role_name);
grant all on future tables in database identifier($new_db_name) to role identifier($owner_role_name);
```

## Making changes

DCM is the currently integrated way to deploy changed from a deployment script
In VSCode, press ctrl+shift+p and select "Run Task" then any of the gitsnow tasks to help syncing folder and database.
It's not clear yet whether we'll lean more towards directly using gitsnow, dbt, dcm, snowddl, or something else.
We won't make this decision until we have a better idea of our requirements from actually building the pipeline.


## Backups/Snapshots

Snapshots are configured in the raw/snapshots/init.sql file.

## TODO

* Try using TVFs to dedupe logic between dynamic table and adhoc approach (check inlined sensibly)
* Create table valued proc/function that calculates weighted results for things like this by creating ad-hoc variables to give full flexibility. But may have to take a different approach to make it fast/cheap enough to run for every random user request.
* Build the old audiences page, and most loved brands report. Bring any helpful stuff back into the main views.