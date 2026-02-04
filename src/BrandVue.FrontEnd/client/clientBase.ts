import { AccessTokenManager } from "./core/AccessTokenManager";
import { parse } from "query-string";
import lzstring from "lz-string/libs/lz-string";
import { DataLoadInProgressError } from "./DataLoadInProgressError";
import { NoDataError } from "./NoDataError";
import VueApiInfo from "./helpers/VueApiInfo";
import {PageLoadIndication} from "./PageLoadIndication";

export class ClientBase {
    private static isOutOfDate: boolean = false;
    private tokenManager = new AccessTokenManager(this.getBaseUrl(""));
    public handleError: ((errorLambda: () => never, error?: any) => void);
    public responseHandlers?: ((response) => void)[];
    public getBaseUrl(defaultPath: string, baseUrl?: string) {
        return baseUrl || (<any>window).appBasePath;
    }

    public async transformOptions(options: RequestInit): Promise<RequestInit> {
        PageLoadIndication.instance.handleStart();

        if (ClientBase.isOutOfDate) {
            //Suspend requests if client code out of date to avoid errors and UI flicker while waiting for the location reload.
            await new Promise(resolve => window.setTimeout(resolve, 10000));
        }

        options = options || <RequestInit>{};
        options.headers = options.headers || <HeadersInit>{};

        //const accessToken = this.tokenManager.getAccessToken();
        //options.headers["Authorization"] = "Bearer " + <string>accessToken;

        options.headers[VueApiInfo.clientVersionHeaderName] = VueApiInfo.clientApiVersion;

        const bvReportingQuery = parse(window.location.search)["BVReporting"];
        if (bvReportingQuery) {
            options.headers["X-BVReporting"] = bvReportingQuery;
        }
        const bvReportingOrganizationQuery = parse(window.location.search)["BVOrg"];
        if (bvReportingOrganizationQuery) {
            options.headers["X-BVOrg"] = bvReportingOrganizationQuery;
        }

        return Promise.resolve<RequestInit>(options);
    }

    public async transformResult<T>(url: string, response: Response, process: (_response: Response) => Promise<T>): Promise<T> {
        PageLoadIndication.instance.handleEnd();
        if (!this.handleError) throw new Error("handleError is not configured, please use the static factory method instead of the constructor");

        const serverApiVersionFromHeader = response.headers.get(VueApiInfo.serverVersionHeaderName);
        if (serverApiVersionFromHeader) {
            if (VueApiInfo.clientApiVersion !== serverApiVersionFromHeader) {
                ClientBase.isOutOfDate = true;
                setTimeout(() => (location as any).reload(true), 1000);
            }
        }

        if (response.status === 401) {
            this.tokenManager.requestNewAccessToken();
            console.log("Aborting request in order to retrieve new access token");
            await new Promise(resolve => window.setTimeout(resolve, 10000));
            // We're hoping the requestNewAccessToken method does a redirect,
            // so users shouldn't see things here unless that goes wrong and hasn't kicked in after 10 seconds
            return this.runErrorHandler<T>(response, url, new Error("Request aborted, log out and in again"));
        } else if (response.status === 503) {
            let error = new DataLoadInProgressError();
            this.handleError(() => { throw error });
            return Promise.reject(error);
        } else if (response.redirected && this.allowRedirect(response.url)) {
            window.location.href = response.url;
            return Promise.reject(new Error("redirected"));
        } else if (response.status === 403) {
            return Promise.reject(new Error(response.statusText));
        } else if (response.status === 204) {
            let error = new NoDataError();
            this.handleError(() => { throw error }, error);
            return Promise.reject(error);
        } else {
            try {
                let p = process(response);

                if (this.responseHandlers) {
                    this.responseHandlers.map(f => p.then(r => {
                        f(r);
                        return r;
                    }));
                }

                p = p.catch(e => this.runErrorHandler<T>(response, url, e));
                return p;
            }
            catch (err) {
                return this.runErrorHandler<T>(response, url, err);
            }
        }
    }

    private allowRedirect(url): boolean {
        try {
            const parsedUrl = new URL(url);
            return parsedUrl.origin === window.location.origin;
        } catch (error) { }
        return false;
    }

    private runErrorHandler<T>(response: Response, url: string, err: any): Promise<T> {
        const error = this.normalizeError(err);
        error.message = "Error processing " + response.status + " from " + url + ":\n" + error.message;
        this.handleError(() => { throw error; });
        return Promise.reject(error);
    }

    // https://stackoverflow.com/a/43643569
    private normalizeError(e: any): Error {
        if (e instanceof Error) {
            return e;
        }
        return new Error(typeof e === "string" ? e : e.toString());
    }
}

export class Factory {
    private compress: boolean;

    public constructor(compress: boolean) {
        this.compress = compress;
    }

    public fetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
        if (init != null && init.body != null) {
            if (!this.compress) {
                init.method = "POST";
            }
            else {
                //https://stackoverflow.com/questions/417142/what-is-the-maximum-length-of-a-url-in-different-browsers
                const maxUrlLength = 1600;

                //Take the data from the body of a Http GET and
                //place it on the URL compressed.
                let parameter = lzstring.compressToBase64(init.body);
                parameter = "model=" + encodeURIComponent(parameter);
                parameter = parameter.replace(/[?&]$/, "");
                if (parameter.length < maxUrlLength) {
                    url = url + "?" + parameter;
                    init.body = null;
                    init.method = "GET";
                } else {
                    init.method = "POST";
                }
            }
        }
        return window.fetch(url, init);
    }

    /**
      * e.g. In a react component use `err => this.setState(err)`
      */
    public static AilaClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new AilaClient();
        client.handleError = handleError;
        return client;
    }

    public static DataClientWithNoHandler(handleError: ((errorLambda: () => never, error?: any) => void), baseUri?: string) {
        const client = new DataClient(baseUri, new Factory(true));
        client.handleError = handleError;
        return client;
    }

    public static UserFeaturesClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new UserFeaturesClient();
        client.handleError = handleError;
        return client;
    }

    public static OrganisationFeaturesClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new OrganisationFeaturesClient();
        client.handleError = handleError;
        return client;
    }

    public static FeaturesClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new FeaturesClient();
        client.handleError = handleError;
        return client;
    }

    /**
     * e.g. In a react component use `err => this.setState(err)`
     */
    public static DataClient(handleError: ((errorLambda: () => never, error?: any) => void), baseUri?: string) {
        const client = new DataClient(baseUri, new Factory(true));
        client.responseHandlers = Factory.globalResponseHandlers[client.constructor.name];
        client.handleError = handleError;
        return client;
    }
    
    
    public static LlmInsightsClient(handleError: ((errorLambda: () => never, error?: any) => void),baseUri?: string) {
        const client = new LlmInsightsClient(baseUri, new Factory(false));
        client.responseHandlers = Factory.globalResponseHandlers[client.constructor.name];
        client.handleError = handleError;
        return client;
    }

    public static LlmDiscoveryClient(handleError: ((errorLambda: () => never, error?: any) => void), baseUri?: string) {
        const client = new LlmDiscoveryClient(baseUri, new Factory(false));
        client.responseHandlers = Factory.globalResponseHandlers[client.constructor.name];
        client.handleError = handleError;
        return client;
    }


    private static globalResponseHandlers: { [index: string] : ((r)=>void)[] } = {};

    public static RegisterGlobalResponseHandler<T extends object>(a: { new(): T }, responseHandler: (r: any) => void) {
        const responseHandlers = Factory.getResponseHandlers(a);
        responseHandlers.push(responseHandler);
    }

    public static UnregisterGlobalResponseHandler<T extends object>(a: { new(): T }, responseHandler: (r: any) => void) {
        const responseHandlers = Factory.getResponseHandlers(a);
        const index = responseHandlers.indexOf(responseHandler);
        if (index !== -1) {
            responseHandlers.splice(index, 1);
        }
    }

    private static getResponseHandlers<T extends object>(a: { new(): T }) {
        const clientTypeName = a.name;
        let responseHandlers = Factory.globalResponseHandlers[clientTypeName];
        if (!responseHandlers) {
            responseHandlers = Factory.globalResponseHandlers[clientTypeName] = new Array<(r: any) => void>();
        }
        return responseHandlers;
    }

   /**
     * e.g. In a react component use `err => this.setState(err)`
     */
    public static MetaDataClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new MetaDataClient();
        client.handleError = handleError;
        return client;
    }

    public static ConfigClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new ConfigClient();
        client.handleError = handleError;
        return client;
    }

    public static ClientErrorClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new ClientErrorClient();
        client.handleError = handleError;
        return client;
    }

    public static MetricsClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new MetricsClient();
        client.handleError = handleError;
        return client;
    }

    public static EntitiesClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new EntitiesClient();
        client.handleError = handleError;
        return client;
    }

    public static SubsetsClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new SubsetsClient();
        client.handleError = handleError;
        return client;
    }

     public static ConfigureMetricClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new ConfigureMetricClient();
        client.handleError = handleError;
        return client;
    }

     public static PagesClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new PagesClient();
        client.handleError = handleError;
        return client;
    }

    public static WeightingFileClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new WeightingFileClient();
        client.handleError = handleError;
        return client;
    }

     public static WeightingAlgorithmsClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
         const client = new WeightingAlgorithmsClient();
         client.handleError = handleError;
         return client;
     }

     public static WeightingPlansClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
         const client = new WeightingPlansClient();
         client.handleError = handleError;
         return client;
     }

     public static SavedBreaksClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new SavedBreaksClient();
        client.handleError = handleError;
        return client;
    }

    public static VariableConfigurationClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new VariableConfigurationClient();
        client.handleError = handleError;
        return client;
    }

    public static SavedReportClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new SavedReportClient();
        client.handleError = handleError;
        return client;
    }

    public static ReportTemplateClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new ReportTemplateClient();
        client.handleError = handleError;
        return client;
    }

    public static AverageConfigurationClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new AverageConfigurationClient();
        client.handleError = handleError;
        return client;
    }

    public static UsersClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new UsersClient();
        client.handleError = handleError;
        return client;
    }

    public static AllVueConfigurationClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new AllVueConfigurationClient();
        client.handleError = handleError;
        return client;
    }

    public static ReportVueClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new ReportVueClient();
        client.handleError = handleError;
        return client;
    }

    public static AllVueWebPageClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new AllVueWebPageClient();
        client.handleError = handleError;
        return client;
    }

    public static SyncedDataClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new SyncedDataClient();
        client.handleError = handleError;
        return client;
    }

    public static DataCacheClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new DataCacheClient();
        client.handleError = handleError;
        return client;
    }

    public static KimbleProposalClient(handleError: ((errorLambda: () => never, error?: any) => void)) {
        const client = new KimbleProposalClient();
        client.handleError = handleError;
        return client;
    }
}