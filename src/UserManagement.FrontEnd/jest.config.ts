import type { Config } from 'jest';

const config: Config = {
  verbose: true,
  moduleDirectories: [
    'node_modules',
    '<rootDir>/node_modules',
  ],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  rootDir: "./", // or wherever your main project is
  roots: [
    "<rootDir>",
    "<rootDir>/../Vue.Common.FrontEnd"
  ],
  testEnvironment: 'jsdom',
  testMatch: [
    "<rootDir>/src/**/*.test.{js,jsx,ts,tsx}",
    "<rootDir>/../Vue.Common.FrontEnd/**/*.test.{js,jsx,ts,tsx}"
  ],
  transform: {
    "^.+\\.(ts|tsx|js|jsx)$": "babel-jest",
  },
  moduleNameMapper: {
    "^@shared/(.*)$": "<rootDir>/../Vue.Common.FrontEnd/Components/$1",
    "^@/(.*)$": "<rootDir>/src/$1",
    "\\.(jpg|jpeg|png|gif|eot|otf|webp|svg|ttf|woff|woff2|mp4|webm|wav|mp3|m4a|aac|oga)$": "<rootDir>/fileMock.js",
    "\\.(css|less|scss)$": "identity-obj-proxy",
  },
};

export default config;