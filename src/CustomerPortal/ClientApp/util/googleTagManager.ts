import { History } from "history";
import {Sha256} from '@aws-crypto/sha256-browser';
import Gtag from "gtagjs";
import { IUserContext, ProductConfigurationResult } from "../CustomerPortalApi";

export type ActionEventName = "documentsView" |
                              "documentsUpload" |
                              "documentsDownload" |
                              "documentsDelete" |
                              "quotasView" |
                              "quotasSortAlphabetical" |
                              "quotasSortScript" |
                              "surveyGroupStatusView" |
                              "surveyGroupStatusNavigateSubSurvey" |
                              "projectsView" |
                              "projectsFilterLive" |
                              "projectsFilterClosed" |
                              "projectsFilterAll" |
                              "projectsSearch" |
                              "projectsSortLaunchDate" |
                              "projectsSortCompletion" |
                              "projectsVisibilityAll" |
                              "projectsVisibilityShared" |
                              "projectsVisibilitySavanta";

export type EventName = ActionEventName;
// Convention for EventName is {Area}{Action}{Acting On}

type EventNameToMeta = { [TKey in EventName]: {category: string, action: string, label: string | undefined} };

export interface ICommonVariables {
    activeBrand?: string;
    value?: string;
    parentComponent?: string;
    filters?: string[];
    startDate?: string;
    endDate?: string;
    highlighted?: string[];
    peer?: string[];
    comparisonPeriod?: string;
    scStart?: string;
    scEnd?: string;
    range?: string;
}

export class GoogleTagManager {
    private static readonly eventToMetaObject: EventNameToMeta = {
        documentsView: { category: "Documents Page", action: "Viewed the documents page", label: undefined },
        documentsUpload: { category: "Documents Page", action: "Uploaded a document", label: undefined },
        documentsDownload: { category: "Documents Page", action: "Downloaded a document", label: undefined },
        documentsDelete: { category: "Documents Page", action: "Deleted a document", label: undefined },
        quotasView: { category: "Quotas Page", action: "Viewed the quotas page", label: undefined },
        quotasSortAlphabetical: { category: "Quotas Page", action: "Changed quota sort order to alphabetical order", label: undefined },
        quotasSortScript: { category: "Quotas Page", action: "Changed quota sort order to script order", label: undefined },
        surveyGroupStatusView: { category: "Survey Group Status Page", action: "Viewed the survey group status page", label: undefined },
        surveyGroupStatusNavigateSubSurvey: { category: "Survey Group Status Page", action: "Navigated to view a survey inside the survey group", label: undefined },
        projectsView: { category: "Projects Page", action: "Viewed the projects page", label: undefined },
        projectsFilterLive: { category: "Projects Page", action: "Changed project filtering to Live projects", label: undefined },
        projectsFilterClosed: { category: "Projects Page", action: "Changed project filtering to Closed/Paused projects", label: undefined },
        projectsFilterAll: { category: "Projects Page", action: "Changed project filtering to All projects", label: undefined },
        projectsSearch: { category: "Projects Page", action: "Entered search text on the projects page", label: undefined },
        projectsSortLaunchDate: { category: "Projects Page", action: "Changed projects sort order to launch date", label: undefined },
        projectsSortCompletion: { category: "Projects Page", action: "Changed projects sort order to completion %", label: undefined },
        projectsVisibilityAll: { category: "Projects Page", action: "Changed projects visibility filtering to All", label: undefined },
        projectsVisibilityShared: { category: "Projects Page", action: "Changed projects visibility filtering to Shared", label: undefined },
        projectsVisibilitySavanta: { category: "Projects Page", action: "Changed projects visibility filtering to Savanta Only", label: undefined },
    }

    readonly DEMO_ORGANISATION: string = 'demo';

    history: History;
    dataLayer: any = [];
    productConfiguration: ProductConfigurationResult;
    defaultEventDataPromise: Promise<{}>;

    constructor(history: History, productConfiguration: ProductConfigurationResult) {
        this.productConfiguration = productConfiguration;

        if (productConfiguration?.googleTags.length) {
            // Start listening for history changes
            this.history = history;

            // Set up GTM data layer
            window["dataLayer"] = this.dataLayer;

            const hash = new Sha256();
            // It's technically possible to hash a particular user's database id and search for them in Google Analytics
            // We shouldn't need to do that, but if doing so, you must ensure the appropriate GDPR consent is in place for that user
            hash.update(productConfiguration.user.userId + '|1CDCA66D44034DD0B69C232885EAAE7D');
            const userOrganisation = this.getCompanyFromUserEmail(this.productConfiguration.user);
            this.defaultEventDataPromise = hash.digest().then(a => {
                const d = {
                    // In future if we have anonymous users, presumably this field will be null or empty. In that case try to avoid them all being counted as one user
                    user_id: productConfiguration.user.userId?.length > 0 ? this.bytesArrToBase64(a) : undefined,
                    companyID: this.productConfiguration.user.accountName,
                    userOrganisation: userOrganisation,
                    subdomainOrganisation: this.productConfiguration.subdomainOrganisation,
                    projectOrganisation: undefined,
                    // To help us get product feedback from our own employees, store the user id that can be looked up in the database
                    savantaInternalUserId: userOrganisation === 'savanta.com' ? this.productConfiguration.user.userId : undefined,
                    subProductId: undefined,
                };
                this.dataLayer.push(['set', d]);
                return d;
            });

            // Start the tag managers up
            productConfiguration.googleTags.map(t => this.startTagManager(`GTM-${t}`));
        }
        else {
            this.defaultEventDataPromise = Promise.resolve({});
        }
    }

    private startTagManager(gtmId: string,
        windowArg: Window = window,
        documentArg: Document = document,
        scriptArg: string = "script",
        dataLayerArg: string = "dataLayer") {

        if (gtmId.length === 0) {
            return;
        }

        // Data layer
        windowArg[dataLayerArg] = windowArg[dataLayerArg] || [];

        this.defaultEventDataPromise.then(d => {
            // GTM Start event
            windowArg[dataLayerArg].push({...d, 'gtm.start': new Date().getTime(), event: "gtm.js" });
            Gtag.gtag("set", { "productName" : this.productConfiguration.productName});
            Gtag.gtag("set", { "userOrganisation" : this.productConfiguration.productName});
            Gtag.gtag("set", { "subdomainOrganisation" : this.productConfiguration.productName});
            // Create the script tag to download the GTM script
            const existingScriptTag = documentArg.getElementsByTagName(scriptArg)[0] as HTMLScriptElement;
            const gtmScriptTag = documentArg.createElement(scriptArg) as HTMLScriptElement;
            const dl = dataLayerArg !== "dataLayer" ? "&l=" + dataLayerArg : "";
            gtmScriptTag.async = true;
            gtmScriptTag.src = "https://www.googletagmanager.com/gtm.js?id=" + gtmId + dl;
            existingScriptTag.parentNode!.insertBefore(gtmScriptTag, existingScriptTag);
        });
    }

    private getCustomerPortalApplicationState(): ICommonVariables | null {
        //customer portal doesnt have any of the BV state that gets sent here e.g. startDate activeBrand etc
        //in BV this is pageHandler.getApplicationStateForGoogleAnalytics
        return null;
    }

    public addEvent(eventName: ActionEventName, projectOrganisation?: string, subProductId?: string, values?: ICommonVariables | null): void {
        const applicationState = this.getCustomerPortalApplicationState();
        const supportingData = {
            ...applicationState,
            ...values,
            projectOrganisation: projectOrganisation,
            subProductId: subProductId
        };
        this.addEventInternal(eventName, supportingData);
    }

    private addEventInternal(eventName: EventName, supportingData?: ICommonVariables | null): void {
        const meta = GoogleTagManager.eventToMetaObject[eventName];

        this.defaultEventDataPromise.then(d => {
            const event = {
                ...d,
                event: "globalTrigger",
                category: meta.category,
                action: meta.action,
                label: meta.label,
                ...supportingData,
            };
            this.ensureTrackableCompanyIdSet(event, this.productConfiguration.user);
            this.dataLayer.push(event);
        });
    }

    private ensureTrackableCompanyIdSet(event: any, user: IUserContext) {
        //  Every customer should have a company ID,
        //  such as 'jamiesitalian'. However, users with trials that
        //  are part of the demo organisation will have the company ID 'demo'.
        //  This is no good because it means we can't attribute their
        //  activity to their real organisation for account management/sales
        //  purposes.
        //
        //  As a result we're going to yank the domain portion of their email
        //  address and set it as the companyID so that we can use this to
        //  filter on UserAccount (to which companyID maps) in Google
        //  Analytics to see how (or if) organisations are using BrandVue
        //  during their trial period. This will enable us to have more
        //  relevant conversations with them as part of the sales process.

        if (event.companyID && event.companyID !== this.DEMO_ORGANISATION) {
            return;
        }

        var userEmail = user.userName; // user names are ALWAYS emails

        //  Remotely unlikely, but we shouldn't blow up if it does happen
        //  (we are, after all, just dealing with sending events to GTM,
        //  which is not a core function of the application).
        if (!userEmail) {
            //  This means we can still distinguish between demo and non-demo
            //  users for which no company can be set (again, unlikely to be a
            //  problem, but this will highlight it in GA immediately if it
            //  ever does happen).
            if (event.companyID !== this.DEMO_ORGANISATION) {
                event.companyID = 'no user';
            }

            return;
        }

        var index = userEmail.indexOf('@');

        //  Also remotely unlikely but, again, we need to handle gracefully.
        if (index < 0) {
            event.companyID = 'no domain';
        } else if (index === userEmail.length - 1) { //  Ditto this situation.
            event.companyID = 'domain truncated';
        } else {
            event.companyID = userEmail.substring(index + 1);
        }
    }

    private getCompanyFromUserEmail(user: IUserContext): string | undefined {
        var userEmail = user.userName; // user names are ALWAYS emails

        if (userEmail) {
            var index = userEmail.indexOf('@');
            if (index < 0) {
                return 'no domain';
            } else if (index === userEmail.length - 1) {
                return 'domain truncated';
            } else {
                return userEmail.substring(index + 1);
            }
        }
        return undefined;
    }

    private bytesArrToBase64(arr) {
      const abc = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"; // base64 alphabet
      const bin = n => n.toString(2).padStart(8,0); // convert num to 8-bit binary string
      const l = arr.length
      let result = '';

      for(let i=0; i<=(l-1)/3; i++) {
        let c1 = i*3+1>=l; // case when "=" is on end
        let c2 = i*3+2>=l; // case when "=" is on end
        let chunk = bin(arr[3*i]) + bin(c1? 0:arr[3*i+1]) + bin(c2? 0:arr[3*i+2]);
        let r = chunk.match(/.{1,6}/g).map((x,j)=> j==3&&c2 ? '=' :(j==2&&c1 ? '=':abc[+('0b'+x)]));
        result += r.join('');
      }

      return result;
    }
}
