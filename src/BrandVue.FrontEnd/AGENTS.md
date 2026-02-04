# Agent Instructions

Guidance for AI agents working with this repository.

## Key Technologies

Aim to use the latest version of these technologies unless you see the version referenced is different.

*   **.NET:** The backend is built with .NET. The main project file is `BrandVue.FrontEnd.Core.csproj`.
*   **React:** The frontend is built with React. Components should only focus on rendering the state.
  *   Redux slices, or react hooks if needed, should be used for state management, transformation and logic.
  *   This logic should be unit tested using jest.
*   **Redux:** Redux is used for state management. The main store configuration can be found in `store.ts`.
  *   Careful to use the typed versions of methods defined there: useAppSelector and useAppDispatch.
  *   In slices, API types must have toJSON called on them immediately, to avoid storing a class in the store (which wouldn't round trip).
  *   State should be stored in a reasonably normalized form, avoiding duplication.
*   **TypeScript:** Ensure type safety and leverage TypeScript features. Avoid using "any".
  * The BrandVueApi.ts is auto-generated using npm run build:api after a dotnet build.
*   **NUnit** with its built-in fluent assertion style.
  * Put as much shared setup as possible in the setup method, or shared helper methods if needed, so that tests very clearly show what's being tested. Or make use of TestCase.
  * Where mocks are needed, use NSubstitute
  * Be aware of `Builder` classes and other utilities in TestCommon to help construct test data.

## Important Files and Directories

*   **`/doc/Technical%20Overview.md`**: Comprehensive documentation covering application concepts, architecture, and design decisions
*   **`src/BrandVue.FrontEnd/`**: The root directory for the frontend application.
*   **`BrandVue.FrontEnd.Core.csproj`**: The main project file for the .NET backend.
*   **`package.json`**: Contains the frontend dependencies and build scripts.
*   **`webpack.common.js`**: The common webpack configuration for the frontend.
*   **`client/main.tsx`**: The entry point for the React application.
*   **`client/state/store.ts`**: Contains the Redux store configuration. Use this as context for any frontend state management changes.
*   **`client/components/`**: This directory houses the React components.
*   **`Controllers/Api/`**: This directory contains the .NET controllers that serve data to the frontend.

### Key Component Examples

*   The `DataController.cs` is a key controller that provides data to many of the frontend components.
*   The `VueApp.tsx` file is the root component of the React application. It is responsible for initializing the Redux store and rendering the main router.
*   The `Card.tsx` component is a good example of a reusable component that fetches and displays data from the backend.

## Build and Test Instructions

*   **Build the frontend and backend:**
    *   Run a dotnet build to build the backend.
    *   Run `npm install` to install the frontend dependencies.
    *   Run `npm run build:api` to generate the TypeScript API client (BrandVueApi.ts) from the backend dlls.
    *   Run `npm run build` to build the frontend.
*   **Run the application:**
    *   Run the project from Visual Studio or use the `dotnet run` command.
*   **Run tests:**
    *   Run `npm run test` to run the frontend tests.
    *   Run `dotnet test` to run the backend tests.

## General Guidelines

*   Follow existing coding conventions and style, erring on the side of best practice.
*   Write unit tests for new features and bug fixes.
*   Keep components small and focused on a single responsibility.
*   When modifying Redux state, ensure you are following best practices (e.g., immutability).
*   The frontend communicates with the backend via a RESTful API. The API controllers can be found in the `Controllers/Api/` directory.
*   **Do not use regions (`#region` / `#endregion`)** in code files. Keep code organized through proper class/method structure and meaningful naming instead.
