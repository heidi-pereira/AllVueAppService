module.exports = {
    "roots": [
        "<rootDir>/ClientApp"
    ],
    transform: {
        "^.+\\.tsx?$": "ts-jest",
    },
    testEnvironment: 'jest-environment-jsdom',
    testRegex: "(/__tests__/.*|(\\.|/)(test|spec))\\.(jsx?|tsx?)$",
    testPathIgnorePatterns: ["/lib/", "/node_modules/"],
    moduleFileExtensions: ["ts", "tsx", "js", "jsx", "json"],
    "automock": false,
    moduleNameMapper: {
        "\\.(jpg|jpeg|png|gif|eot|otf|webp|svg|ttf|woff|woff2|mp4|webm|wav|mp3|m4a|aac|oga)$": "<rootDir>/fileMock.js",
        "\\.(css|less|scss)$": "<rootDir>/styleMock.js",

        "^@Store(.*)$": "<rootDir>/ClientApp/store$1",
        "^@Cards(.*)$": "<rootDir>/ClientApp/cards$1",
        "^@Utils(.*)$": "<rootDir>/ClientApp/utils$1",
        "^@Layouts(.*)$": "<rootDir>/ClientApp/layouts$1",
        "^@Components(.*)$": "<rootDir>/ClientApp/components$1",
        "^@Globals(.*)$": "<rootDir>/ClientApp/globals$1",
        "^@Services(.*)$": "<rootDir>/ClientApp/services$1",
        "^@Styles(.*)$": "<rootDir>/ClientApp/styles$1",
        "^@Pages(.*)$": "<rootDir>/ClientApp/pages$1",
        "^@Docs(.*)$": "<rootDir>/ClientApp/docs$1"
    }
};