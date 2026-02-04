
require("whatwg-fetch");
const { TextEncoder, TextDecoder } = require('util');
global.fetch = require("jest-fetch-mock");

jest.mock("@react-hook/resize-observer", () => jest.fn());
jest.mock("highcharts/highcharts-more", () => jest.fn());
jest.mock("highcharts-grouped-categories", () => jest.fn());


global.TextEncoder = TextEncoder;
global.TextDecoder = TextDecoder;

global.matchMedia = global.matchMedia || function () {
    return {
        matches: false,
        addListener: function () { },
        removeListener: function () { }
    }
}