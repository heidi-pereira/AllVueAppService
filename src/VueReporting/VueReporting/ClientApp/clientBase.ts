import Globals from "./globals";

export class ClientBase {
    public transformOptions(options: RequestInit): Promise<RequestInit> {
        options.credentials = 'include';
        options.headers["ProductName"] = Globals.ContextProductName;
        options.headers["ProductFilter"] = window.parent.location.search;
        return Promise.resolve<RequestInit>(options);
    }
}