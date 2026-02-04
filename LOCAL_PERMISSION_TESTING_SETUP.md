# Local Permission Testing Setup Guide

This guide explains how to set up local permission testing between the UserManagement service and other Vue applications (BrandVue, OpenEnds, CustomerPortal) for development purposes.

## Overview

The permission system uses secure tokens to authenticate API calls between services. When testing locally, you need to:

1. Configure matching tokens in both UserManagement and your target application
2. Run both services simultaneously
3. Set up local roles and assign users to test permissions

> **Recommended Approach**: For development scenarios, use the [Local Development User IDs](#local-development-user-ids) method described at the end of this guide. This is the simplest way to test permissions without additional authentication complexity.

## Step 1: Configure User Secrets

### UserManagement Service Configuration

Navigate to the UserManagement.BackEnd project directory and set up the API token:

```bash
cd src/UserManagement.BackEnd
dotnet user-secrets set "Api:Token" "your-secure-token-here"
```

### Target Application Configuration

Choose the application you want to test and configure the matching token:

#### For BrandVue:
```bash
cd src/BrandVue.FrontEnd
dotnet user-secrets set "UserManagement:Token" "your-secure-token-here"
```

#### For OpenEnds:
```bash
cd src/OpenEnds.BackEnd
dotnet user-secrets set "UserManagement:Token" "your-secure-token-here"
```

#### For CustomerPortal:
```bash
cd src/CustomerPortal
dotnet user-secrets set "UserManagement:Token" "your-secure-token-here"
```

> **Important:** The token values must match exactly between UserManagement and your target application.

## Step 2: Run Both Applications

### Start UserManagement Service

### Start Your Target Application

## Step 3: Verify API Communication

### Test the Token Connection

The `LocalUserPermissionHttpClient` automatically runs whenever your local application starts and continuously attempts to fetch user permissions from the UserManagement API. Here's how it works:

**Automatic Behavior:**
- **Always Active**: When running locally, `LocalUserPermissionHttpClient` is always active and making permission requests
- **API Endpoint**: Calls `http://localhost:7036/usermanagement/api/internal/userfeaturepermissions/{userId}`
- **Fallback Strategy**: If UserManagement service is unreachable or returns an error, it returns an **empty permissions list**
- **Empty Permissions = Full Access**: When no permissions are returned, the application defaults to granting access to all features

**Important**: An empty permissions list means the user gets full access to everything, not restricted access. This is the default fallback behavior.

### Check for Successful API Calls

1. Monitor the UserManagement service logs for incoming requests
2. Check your target application logs for successful permission retrieval
3. If you see HTTP 401/403 errors, verify that your tokens match exactly

## Step 4: Set Up Local Roles and Users

### Access UserManagement UI

1. Navigate to `http://localhost:7036/usermanagement`
2. Log in with your credentials

### Create Custom Roles

1. Go to the **Roles** section in the UserManagement UI
2. Click **"Add Role"**
3. Configure the role with appropriate permissions:
   - **Variables**: create, edit, delete
   - **Analysis**: access
   - **Documents**: access
   - **Quotas**: access
   - **Settings**: access
   - **Data**: access
   - **Breaks**: add, edit, view, delete (for BrandVue testing)

### Assign Users to Roles

#### Option 1: Using the UserManagement UI
1. Go to the **Users** section
2. Find or create a test user
3. Assign the appropriate role for testing
4. Ensure the user belongs to the correct organization/company

#### Option 2: Direct SQL Assignment (Recommended for Development)

For faster testing, you can directly update the database using SQL Server Management Studio or your preferred SQL client:

**Step 1: Find available roles**
```sql
SELECT * FROM [BrandVueMeta].[UserFeaturePermissions].[Roles]
```

**Step 2: Check current user permissions**
```sql
SELECT TOP (1000) * 
FROM [BrandVueMeta].[UserFeaturePermissions].[UserFeaturePermissions]
```

**Step 3: Assign a role to a user**
```sql
-- Update existing permission record
UPDATE [BrandVueMeta].[UserFeaturePermissions].[UserFeaturePermissions]
SET [UserRoleId] = 1016  -- Replace with desired role ID
WHERE Id = 4             -- Replace with the permission record ID

-- OR create a new permission record if one doesn't exist
INSERT INTO [BrandVueMeta].[UserFeaturePermissions].[UserFeaturePermissions]
([UserId], [UserRoleId], [UpdatedByUserId], [UpdatedDate])
VALUES 
('your-user-id', 1016, 'admin-user-id', GETUTCDATE())
```

**Step 4: Verify the assignment**
```sql
SELECT 
    ufp.Id,
    ufp.UserId,
    ufp.UserRoleId,
    r.RoleName,
    ufp.UpdatedDate
FROM [BrandVueMeta].[UserFeaturePermissions].[UserFeaturePermissions] ufp
INNER JOIN [BrandVueMeta].[UserFeaturePermissions].[Roles] r ON ufp.UserRoleId = r.Id
WHERE ufp.UserId = 'your-user-id'
```

> **Note**: This direct SQL approach is faster for development testing but bypasses business logic validation. Use with caution and only in development environments.

## Set 5: Verification

### Test User Assignment

1. Use the test user credentials in your target application
2. The `LocalUserPermissionHttpClient` will fetch permissions for this user
3. Verify that the expected features are available/restricted based on the assigned role

---

### Local Development User IDs

When testing locally, **BrandVue** application uses specific hardcoded user IDs:

- **BrandVue**: Uses `"LocalUserId"`

**SQL Example for BrandVue Testing:**
```sql
-- Assign role to the BrandVue local development user
UPDATE [BrandVueMeta].[UserFeaturePermissions].[UserFeaturePermissions]
SET [UserRoleId] = 1016  -- Replace with your test role ID
WHERE UserId = 'LocalUserId'

-- Or create a new permission record if one doesn't exist
INSERT INTO [BrandVueMeta].[UserFeaturePermissions].[UserFeaturePermissions]
([UserId], [UserRoleId], [UpdatedByUserId], [UpdatedDate])
VALUES 
('LocalUserId', 1016, 'admin-user-id', GETUTCDATE())

-- Verify the assignment
SELECT 
    ufp.Id,
    ufp.UserId,
    ufp.UserRoleId,
    r.RoleName,
    ufp.UpdatedDate
FROM [BrandVueMeta].[UserFeaturePermissions].[UserFeaturePermissions] ufp
INNER JOIN [BrandVueMeta].[UserFeaturePermissions].[Roles] r ON ufp.UserRoleId = r.Id
WHERE ufp.UserId = 'LocalUserId'
```

This setup allows you to test permission-based feature visibility and access control in your local development environment.
