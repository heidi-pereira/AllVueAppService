import * as BrandVueApi from "./BrandVueApi";
import FreshchatConfig = BrandVueApi.FreshchatConfig;
import Factory = BrandVueApi.Factory;

export default class Freshchat {
    private static _instance: Freshchat;
    fcWidget: any;
    static isEnabled: boolean;

    private constructor() {
    }

    public CompletedLoadingJavaScript() {

        this.fcWidget = (window as any).fcWidget;

        if (this.fcWidget) {
            this.init();
        } else {
            //  This is possibly excessively defensive but the point
            //  is that if Freshchat has failed to load we want to
            //  absolutely ensure that Freshchat related functionality
            //  is disabled.
            Freshchat.isEnabled = false;

            //  Checking because compatibility on some mobile browsers
            //  is unknown according to both:
            //
            //  https://developer.mozilla.org/en-US/docs/Web/API/Console/warn
            //
            //  and
            //
            //  https://developer.mozilla.org/en-US/docs/Web/API/Console/log
            //
            //  (I know: I was a bit shocked to see Chrome for Android and
            //  Safari for iOS on these lists too so I'm taking no chances.)
            if (console && console.warn) {
                console.warn(
                    "Freshchat widget is unavailable - Freshchat may have failed to load or otherwise be down. Please contact support@freshchat.com for more information.");
            }
            this.sendErrorMessage("Freshchat widget is unavailable");
        }
    }

    public static GetOrCreateWidget(): Freshchat {
        return this._instance || (this._instance = new this());
    }

    private sendErrorMessage(message: string) {
        const handleLoggingError = (err: () => never) => window.console.warn("Unable to log error");

        const serverError = Factory.ClientErrorClient(handleLoggingError);
        let errorDetails = new BrandVueApi.ErrorDetails();
        errorDetails.errorLevel = BrandVueApi.ErrorLevel.Error;
        errorDetails.message = message;
        errorDetails.url = window.location.href;
        serverError.logError(errorDetails);
    }

    public show(payload: {}): void {
        if (this.fcWidget && !this.fcWidget.isOpen()) {
            this.fcWidget.open(payload);
        }
    }

    private getUserPayload(config: FreshchatConfig) {
        return {
            firstName: config.firstName,
            lastName: config.lastName,
            email: config.userId,
            meta: {
                "role": config.role,
                "trialActive": config.isTrial,
                "environment": config.environment,
                "company": config.company,
                "product": config.productName
            }
        };
    }

    private initWidget(config: FreshchatConfig) {
        if (this.fcWidget) {
            this.fcWidget.init({
                token: config.apiToken,
                host: "https://wchat.freshchat.com",
                externalId: config.userId,
                restoreId: config.restoreId,
            });
        }
    }

    private updateUser(response: any, config: FreshchatConfig) {
        var status = response && response.status;
        if (status === 200 && this.fcWidget) {
            this.fcWidget.user.update(this.getUserPayload(config));
        }
    }

    private createUser(response: any, config: FreshchatConfig) {
        if (!this.fcWidget) {
            return;
        }

        var status = response && response.status;

        if ([401, 403, 404].some(s => s === status)) {

            this.fcWidget.user.create(this.getUserPayload(config)).then(resp => {

                var user = resp.data;
                if (user.restoreId) {

                    BrandVueApi.Factory.MetaDataClient(throwErr => throwErr())
                        .saveFreshchatConversationId(config.userId, user.restoreId);
                }

            });
        }
    }

    private init() {
        const params = new URLSearchParams(window.location.search);
        let appMode = params.get("appMode");
        appMode = appMode !== null ? appMode : "normal";
        BrandVueApi.Factory.MetaDataClient(throwErr => throwErr())
            .getFreshchatConfig(appMode)
            .then(config => {
                //  Again, arguably excessively defensive, but I'd like this code
                //  not to be fragile in the face of future modification.
                Freshchat.isEnabled = config.enabled && this.fcWidget;

                if (config.enabled) {
                    this.initWidget(config);

                    this.fcWidget.user.get()
                        .then(response => this.updateUser(response, config))
                        .catch(response => this.createUser(response, config));
                }
            });
    }
}