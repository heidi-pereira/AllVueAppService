import QueryString from "query-string";
import { createBrowserHistory, History } from 'history';

export class QueryStringManager {
    constructor(baseUrl: string) {
        this.history = createBrowserHistory({ basename: baseUrl });
    }

    history: History;
    
    setInitialView(initialView?: string) {
        const s = this.history.location.search;
        if (initialView) {
            this.history.push(initialView + s);
        } else {
            this.history.push('/templates' + s);
        }
    }

    public getQueryParameter<T>(name: string, defaultValue?: T): T {
        const parsed = QueryString.parse(this.history.location.search);

        let value = parsed[name.replace(/\+/g, " ")];

        if (value != undefined) {
            value = value.replace(/\+/g, " ");

            // Array
            if (value.indexOf(".") >= 0) {
                value = value.split(".");
            }
        } else {
            return defaultValue!;
        }

        return value;
    }

    public setQueryParameters(params: { name: string, value: string | number | string[] | number[] | undefined }[]) {
        let currentSearch = this.history.location.search;
        if (!currentSearch.startsWith("?")) {
            currentSearch = "?" + currentSearch;
        }
        const parsed = QueryString.parse(currentSearch);

        for (let i = 0; i < params.length; i++) {
            const name = params[i].name;
            const value = params[i].value;

            // No need to replace " " with "+" as they that is already done
            parsed[name] = Array.isArray(value) ? value.join(".") : value;

            if (parsed[name] === "") {
                delete parsed[name];
            }
        }

        const newSearch = `?${QueryString.stringify(parsed, { encode: false }).replace(/ /g, "+")}`;

        if (currentSearch !== newSearch) {
            this.history.replace(newSearch);
        }
    }
}