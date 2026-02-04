module.exports = {
    "roots": [
        "<rootDir>/client"
    ],
    "transform": {
        "^.+\\.(ts|tsx)?$": "ts-jest",
        '.+\\.(css|less)$': 'jest-css-modules-transform'
    },
    "testEnvironment": "jsdom",
    "testRegex": "(/__TypescriptTests__/.*|(\\.|/)(test|spec))\\.tsx?$",
    "moduleFileExtensions": [
        "ts",
        "tsx",
        "js",
        "jsx",
        "json"
    ],
    "moduleDirectories": [
        "node_modules",
        "client"
    ],
    "automock": false,
    "setupFiles": [
        "./jestTestFixtureSetup.js",
        "jest-localstorage-mock"
    ],
    "testResultsProcessor": "jest-teamcity-reporter",
    "moduleNameMapper": {
        "\\.(jpg|jpeg|png|gif|eot|otf|webp|svg|ttf|woff|woff2|mp4|webm|wav|mp3|m4a|aac|oga)$": "<rootDir>/fileMock.js",
        "\\.(css|less)$": "identity-obj-proxy",
        "^client/(.*)$": "<rootDir>/client/$1",
        "^FeatureGuardShared/(.*)$": "<rootDir>/../Vue.Common.FrontEnd/Components/FeatureGuard/$1"
    },
    "collectCoverage": false,
    "coverageDirectory": './coverage-front-end',
    "coverageReporters": ['json', 'lcov', 'text', 'clover', 'cobertura'],
}