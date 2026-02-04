# UserManagement Project

## Overview

The UserManagement project is a web application built with ASP.NET Core 9.0 backend and a modern frontend using Vite, React, and Material-UI. It provides user management capabilities including authentication, authorization, feature permissions, and data permissions management within the Vue platform ecosystem.

## Architecture

The project follows a clean architecture pattern with the following structure:

### Backend (`UserManagement.BackEnd`)
- **Framework**: ASP.NET Core 9.0
- **Authentication**: OpenID Connect with OAuth2
- **API Documentation**: OpenAPI/Scalar integration
- **Database**: Entity Framework Core with SQL Server
- **Patterns**: CQRS with MediatR, Repository pattern
- **Architecture Layers**:
  - `Application/` - Business logic and CQRS handlers
  - `Domain/` - Domain entities and business rules
  - `Infrastructure/` - Data access and external services
  - `WebApi/` - Controllers and middleware
  - `Library/` - Shared utilities and settings

### Frontend (`UserManagement.FrontEnd`)
- **Framework**: React with TypeScript
- **Build Tool**: Vite
- **UI Library**: Material-UI (MUI)
- **State Management**: RTK Query
- **Testing**: Jest
- **Code Generation**: API clients auto-generated from OpenAPI specs

## Default Configuration

### **Important: Default Port Information**

The UserManagement application runs on **`http://localhost:7036`** by default. This port is critical for the Vue platform ecosystem as:

- **BrandVue (BV) and other applications depend on this port**
- The port is configured in `launchSettings.json` and `launch.json` for vscode and should not be changed without coordinating with other Vue platform services
- Frontend development server runs on `http://localhost:7037` and proxies API calls to the backend

**⚠️ Warning**: Changing the default port (7036) may break integration with other Vue platform applications including BrandVue.

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Node.js 20.x
- SQL Server (for metadata storage)
- Visual Studio 2022 or VS Code

### Development Setup

1. **Clone and Navigate**
   ```bash
   cd src/UserManagement.BackEnd
   ```

2. **Configure User Secrets** (Development)
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Settings:MetadataConnectionString" "your-connection-string"
   dotnet user-secrets set "Settings:AuthClientSecret" "your-auth-secret"
   ```

3. **Install Frontend Dependencies**
   ```bash
   cd ../UserManagement.FrontEnd
   npm install
   ```

4. **Run the Application**
   
   **Option A: Using Visual Studio**
   - Open `UserManagement.sln`
   - Set `UserManagement.BackEnd` as startup project
   - Press F5 to run

   **Option B: Using VS Code Debug Configuration**
   - Open `UserManagement.sln`
   - Go to Run and Debug (Ctrl+Shift+D)
   - Select ".NET UserManagement API with Frontend" from the dropdown
   - Press F5 to start debugging
   - This will automatically build, run the backend on port 7036, and open the frontend at http://localhost:7037/usermanagement

   **Option C: Using VS Code Tasks**
   - Open workspace in VS Code
   - Run task: "watch UserManagement" (Ctrl+Shift+P > Tasks: Run Task)

   **Option D: Command Line**
   ```bash
   # Backend
   cd src/UserManagement.BackEnd
   dotnet run

   # Frontend (in separate terminal)
   cd src/UserManagement.FrontEnd
   npm run dev
   ```

### Access Points

- **Application**: http://localhost:7037/usermanagement/
- **Backend API**: http://localhost:7036
- **API Documentation**: http://localhost:7036/scalar/v1
- **OpenAPI Spec**: http://localhost:7036/usermanagement/openapi/v1.json

## Key Features

### Authentication & Authorization
- OpenID Connect integration with Vue Auth Server
- Multi-tenant support via `acr_values`
- Role-based access control
- Custom claims transformation for development

### User & Permission Management
- User data permissions management
- Feature permissions and roles
- White-labeling support
- Permission options and rules configuration

### API Features
- RESTful API with OpenAPI documentation
- Auto-generated TypeScript client for frontend
- CORS enabled for development
- Exception handling middleware
- Internal token validation

## Configuration

### Key Settings (appsettings.json)
```json
{
  "Settings": {
    "ApplicationBasePath": "usermanagement",
    "MetadataConnectionString": "...",
    "AuthAuthority": "auth-server-url",
    "AuthClientId": "client-id",
    "AuthClientSecret": "client-secret"
  }
}
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES`: For SPA proxy in development

## Development Guidelines

### Building
```bash
# Build entire solution
dotnet build src/UserManagement.sln

# Build specific project
dotnet build src/UserManagement.BackEnd/UserManagement.BackEnd.csproj
```

### Testing
```bash
# Backend tests
dotnet test

# Frontend tests
cd src/UserManagement.FrontEnd
npm test
```

### Code Generation
Frontend API clients are automatically generated from the backend OpenAPI specification:
```bash
cd src/UserManagement.FrontEnd
npm run generate:api
```

## Deployment

The project includes Azure DevOps pipeline configuration (`UserManagement.yaml`) for automated builds and deployments.

### Build Pipeline
- Triggers on changes to `src/UserManagement*`, `src/Vue.Common.FrontEnd*`, `src/Vue.Common*`
- Uses .NET 9.0 and Node.js 20.x
- Builds and tests both backend and frontend
- Packages for Octopus Deploy on master/hotfix branches

### Deployment process (Blue/Green)
- Octopus Deploy is used for deployment from created packages, with an automatic deployment to Test
- Blue/Green deployment is available for all environments, but usually only used for live
   - requires 2 slots for each environment, ie testblue & test, defined in the Octopus Project Variables:
      - **BlueDeploymentSlot** - the slot the package is deployed to for the environment
      - **TargetDeploymentSlot** - the slot serving requests to users for the environment
   - An Octopus Deploy step will attempt to spin up the blue slot, and when confirmed running will swap the 2 slots which should result in zero downtime
      - if the 2 variables are the same for an environment then the swap is skipped, thus this step is not restricted to a specific environment

## Integration with Vue Platform

This UserManagement service is part of the larger Vue platform ecosystem and integrates with:

- **BrandVue**: Primary consumer of user management services
- **Vue.Common**: Shared libraries and authentication
- **AllVue**: Comprehensive analytics platform
- **Auth Server**: Centralized authentication service

The default port configuration (`http://localhost:7036`) is standardised across the platform for seamless service communication.

## Running with auth server local development
Note: When running the UserManagement service with a local instance of the Auth Server, ensure that the `AuthAuthority` in your user secrets or `appsettings.json` points to the local Auth Server URL (e.g., `http://localhost:44378` for development).
you WILL have issues with cookies and use different browsers and clear down auth cookies between runs.

## Troubleshooting

### Common Issues

1. **Port 7036 already in use**
   - Check if another UserManagement instance is running
   - Verify no other Vue platform services are using this port
   - **Do not change the port** - coordinate with other services instead

2. **Authentication failures**
   - Verify user secrets are configured
   - Check Auth Server connectivity
   - Ensure correct client ID/secret configuration

3. **Database connection issues**
   - Verify SQL Server is running
   - Check connection string in user secrets
   - Ensure database exists and migrations are applied

4. **Frontend API generation fails**
   - Ensure backend is running on correct port
   - Check OpenAPI endpoint accessibility
   - Verify Node.js and npm dependencies

## Support

For issues related to:
- **Authentication**: Check Auth Server documentation
- **Database**: Verify metadata connection
- **Integration**: Coordinate with BrandVue and AllVue teams regarding port dependencies
