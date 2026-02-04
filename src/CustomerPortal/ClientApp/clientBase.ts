export class ClientBase {

    public getBaseUrl(defaultPath: string, baseUrl?: string) {
        return baseUrl || (<any>window).appBasePath;
    }

    public async transformOptions(options: RequestInit): Promise<RequestInit> {
        return Promise.resolve<RequestInit>(options);
    }

    public async transformResult<T>(url: string, response: Response, process: (_response: Response) => Promise<T>): Promise<T> {
        try {
            let p = process(response);
            p = p.catch(e => this.runErrorHandler<T>(response, url, e));
            return p;
        }
        catch (err) {
            return this.runErrorHandler<T>(response, url, err);
        }
    }

    private runErrorHandler<T>(response: Response, url: string, err: any): Promise<T> {
        const error = this.normalizeError(err);
        error.message = "Error processing " + response.status + " from " + url + ":\n" + error.message;
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
