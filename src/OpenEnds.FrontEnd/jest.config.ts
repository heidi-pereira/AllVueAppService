import type { Config } from 'jest';

// Jest configuration for TypeScript (ESM)
const config: Config = {
	preset: 'ts-jest/presets/default-esm',
	testEnvironment: 'jsdom',
	roots: ['<rootDir>/src'],
	testMatch: [
		'**/__tests__/**/*.(ts|tsx)',
		'**/*.(test|spec).(ts|tsx)'
	],
	transform: {
		'^.+\\.(ts|tsx)$': ['ts-jest', {
			tsconfig: 'tsconfig.app.json',
			useESM: true
		}]
	},
	moduleNameMapper: {
		'^@/(.*)$': '<rootDir>/src/$1',
        '^@model/(.*)$': '<rootDir>/src/Model/$1',
        "^@shared/(.*)$": "<rootDir>/../Vue.Common.FrontEnd/Components/$1",
		'\\.(css|less|scss|sass)$': 'identity-obj-proxy',
		'\\.(jpg|jpeg|png|gif|eot|otf|webp|svg|ttf|woff|woff2|mp4|webm|wav|mp3|m4a|aac|oga)$': 'jest-transform-stub'
	},
	setupFilesAfterEnv: ['<rootDir>/src/setupTests.ts'],
	testPathIgnorePatterns: [
		'<rootDir>/node_modules/',
		'<rootDir>/dist/'
	],
	collectCoverageFrom: [
		'src/**/*.{ts,tsx}',
		'!src/**/*.d.ts',
		'!src/main.tsx',
		'!src/vite-env.d.ts'
	],
	coverageDirectory: 'coverage',
	coverageReporters: [
		'text',
		'lcov',
		'html'
	],
	extensionsToTreatAsEsm: ['.ts', '.tsx'],
	globals: {
		'ts-jest': {
			useESM: true
		}
	}
};

export default config;

