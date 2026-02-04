import VueApiInfo from "../helpers/VueApiInfo";
import * as BrandVueApi from "../BrandVueApi";
import fetchMock from "jest-fetch-mock";

interface IMockFetchCall {
    requestUrl: string;
    body: string;
    method: string;
    headers: any;
}

export function setupApiHasLoadedData(loaded: boolean | Promise<boolean>): Promise<boolean> {
    const loadedPromise = typeof (loaded) === "boolean" ? Promise.resolve(loaded) : loaded;

    return new Promise((res, rej) => {
        BrandVueApi.ConfigClient.prototype.getApplicationConfiguration = jest.fn(() => {
            const getAppPromise = loadedPromise
                .then((isLoadedThisTime) => <BrandVueApi.ApplicationConfigurationResult>{ hasLoadedData: isLoadedThisTime });
            getAppPromise.then(() => res(true));
            return getAppPromise;
        });
    });
}

export function getMockApiCalls(): IMockFetchCall[] {
    return <IMockFetchCall[]><any>fetchMock.mock.calls.map(call => {
        return { requestUrl: call[0], ...call[1] };
    });
}

export function setupDefaultMockFetch(requestUrl?: string, returnedData: any = {}) {
    const headers = {};
    headers[VueApiInfo.serverVersionHeaderName] = "1.0.0.0";

    overrideVersionHeaders();
    fetchMock.mockResponse(JSON.stringify(returnedData), { headers: headers, url: requestUrl, status: 200});
}

function overrideVersionHeaders(): void {
    Object.defineProperty(VueApiInfo, "clientApiVersion", {
        get: () => "1.0.0.0",
        enumerable: true,
        configurable: true
    });
}