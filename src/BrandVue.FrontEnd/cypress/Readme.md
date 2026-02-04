# Cypress Tests
This folder contains Cypress end to end tests of Vue.

## Usage
Pick an environment (e.g beta) and put it in cypress.config.ts
Put a non-ad, non-mfa username and password in e.g. cypress.beta.json and cypress.config.ts respectively
Run `npm run cy` to open Cypress Test Runner. Alternatively, you can run `npx cypress open`.
Run `npm run cy:run` to run Cypress tests without the runner. This command is used in CI pipelines.
Change your configuration in cypress.json file in the main project folder or by passing configuration parameters into cypress.
For details google Cypress io documentation.

## Guidelines
We decided to keep Cypress tests relatively configuration (map file) independent.
Try to test stable parts of the product so that the tests don't break without a reason.
