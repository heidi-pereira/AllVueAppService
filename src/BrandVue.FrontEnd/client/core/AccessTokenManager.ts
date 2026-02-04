import {TokenData} from "./tokenData";

export class AccessTokenManager {

    public constructor(private appBasePath: string)
    { }

    private storageKey: string = "access_token";

    public storeAccessToken(): void {
        // attempt to read access token from fragment
        var data = this.parseHashFragment();

        // if this is an access_token, store it
        if (data && data.access_token) {
            sessionStorage.setItem(this.storageKey, data.access_token);
            // set hash back to what it was before
            var originalHash = data.state || "";
            history.pushState({}, "", window.location.href.split("#")[0] + originalHash);
        }
    }

    public getAccessToken(): string | null {
        return sessionStorage.getItem(this.storageKey);
    }

    public requestNewAccessToken(): void {
        // send the browser to the authorize endpoint to get a token
        // (user will be prompted to login if necessary)
        (location as any).reload(true);
    }

    private getFragment(): string | null {
        if (window.location.hash.indexOf("#") === 0) {
            return window.location.hash.substr(1);
        } else {
            return null;
        }
    }

    private parseHashFragment(): TokenData | null {
        var fragment = this.getFragment();

        var data: TokenData = new TokenData();
        if (fragment === null) {
            return null;
        }

        var pairs: string[] = fragment.split("&");
        for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i];
            var key, value: string;

            var separatorIndex = pair.indexOf("=");
            if (separatorIndex === -1) {
                key = pair;
                value = "";
            } else {
                key = pair.substr(0, separatorIndex);
                value = pair.substr(separatorIndex + 1);
            }

            key = decodeURIComponent(key);
            value = decodeURIComponent(value);
            data[key] = value;
        }
        return data;
    }
}
