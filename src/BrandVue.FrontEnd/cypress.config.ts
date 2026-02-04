import { defineConfig } from 'cypress'
import fs from "fs-extra";
import path from "path";
import JSON5 from 'json5';
import cypressMochawesomeReporter from 'cypress-mochawesome-reporter/plugin.js';

// There seems to be something special with the "environment" variable, cypress keeps it as an array of strings for some reason but does correctly assign uppercase variables
export default defineConfig({
  env: {
    // Pick: dev, beta or live. For beta/live put password in cypress.beta/live.json.
    ENVIRONMENT: 'dev',
    tags: 'AllTests',
    userEmail: 'overridenIn_CypressDotEnvDotJsonFile',
    allVueSurveyId: 'overridenIn_CypressDotEnvDotJsonFile',
    userPassword: 'overridenIn_commandLine',
    maxPagesToSample_non_audience: 50,
    maxPagesToSample_audience: 5,
  },
experimentalStudio: true,
  retries: {
    // Configure retry attempts for `cypress run`
    // Default is 0
    runMode: 2,
    // Configure retry attempts for `cypress open`
    // Default is 0
    openMode: 0
  },
  video: false,
  reporter: 'cypress-mochawesome-reporter',
  reporterOptions: {
    reporterEnabled: 'mochawesome, mocha-junit-reporter',
    mochawesomeReporterOptions: {
      charts: true,
      reportPageTitle: 'BrandVue Cypress Tests',
      embeddedScreenshots: true,
      inlineAssets: true,
      saveAllAttempts: true,
    },
    mochaJunitReporterReporterOptions: {
      mochaFile: 'cypress/reports/junit/results-[hash].xml',
    },
  },
  screenshotOnRunFailure: true,
  blockHosts: ['wchat.freshchat.com', 'syndication.twitter.com'],
  e2e: {
    setupNodeEvents(on, config) {
      cypressMochawesomeReporter(on);

      const pathToConfigFile = path.resolve(
        config.projectRoot,
        'cypress/e2e/config/',
        `cypress.${config.env.ENVIRONMENT}.json`
      );

      const configFileContent = fs.readFileSync(pathToConfigFile, 'utf8');
      let json = JSON5.parse(configFileContent);
      if (config.isTextTerminal) {
        // skip the all.cy.js spec in "cypress run" mode
        json.excludeSpecPattern = ["cypress/e2e/all.cy.ts"];
      }
      return json;
    },
    baseUrl: 'https://overridenIn_CypressDotEnvDotJsonFile',
  },
})
