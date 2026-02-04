import {PageHandler} from "./components/PageHandler";
import {ApplicationUser} from "./BrandVueApi";
import {Sha256} from '@aws-crypto/sha256-browser';
import {ProductConfiguration} from "./ProductConfiguration";
import Gtag from "gtagjs";
import {Location, useLocation} from "react-router-dom";
import {
    IReadVueQueryParams,
} from "./components/helpers/UrlHelper";
import { useMemo } from "react";
import React from "react";
import {ProductConfigurationContext} from "./ProductConfigurationContext";
export type ConfigurationEventName = "pageConfigureCreate" |
    "pageConfigureUpdate" |
    "pageConfigureDelete" |
    "metricConfigureCreate" |
    "metricConfigureUpdate" |
    "metricConfigureDelete" |
    "weightingsConfigureCreate" |
    "weightingsConfigureCopy" |
    "weightingsConfigureUpdate" |
    "weightingsConfigureDelete" |
    "weightingsConfigureDeleteTarget" |
    "subsetConfigureCreate" |
    "subsetConfigureUpdate" |
    "subsetConfigureDelete";

export type ActionEventName = "downloadExcel" |
    "downloadChart" |
    "openFilterDialog" |
    "closeFilterDialog" |
    "applyFilter" |
    "removeFilter" |
    "clearFilter" |
    "removeAllFilters" |
    "openBrandChooser" |
    "closeBrandChooser" |
    "changeFocusBrand" |
    "changeBrandSet" |
    "addBrand" |
    "removeBrand" |
    "applyBrandChooser" |
    "toggleOnLegend" |
    "toggleOffLegend" |
    "selectedSearchBox" |
    "startedTour" |
    "nextStepTour" |
    "finishedTour" |
    "changeAverage" |
    "changeDate" |
    "changeComparisonPeriod" |
    "changeCrosstabMetric" |
    "changeCrosstabActiveBrand" |
    "addCrosstabBreak" |
    "removeCrosstabBreak" |
    "exportCrosstabData" |
    "showCrosstabCounts" |
    "hideCrosstabCounts" |
    "weightingEnabled" |
    "weightingDisabled" |
    "showCrosstabLowSample" |
    "hideCrosstabLowSample" |
    "showCrosstabSignificance" |
    "hideCrosstabSignificance" |
    "orderCrosstabResultOrderAsc" |
    "orderCrosstabResultOrderDesc" |
    "orderCrosstabScriptOrderAsc" |
    "orderCrosstabScriptOrderDesc" |
    "changeCrosstabBaseType" |
    "showCrosstabDontKnows" |
    "excludeCrosstabDontKnows" |
    "surveyVueTestSurvey" |
    "surveyVueDisableMetric" |
    "surveyVueDisableMetricFailed" |
    "surveyVueEnableMetric" |
    "surveyVueEnableMetricFailed" |
    "surveyVueEditHelptext" |
    "surveyVueEditVariableName" |
    "surveyVueEditVarCode" |
    "surveyVueEditVarCodeFailed" |
    "surveyVueEditHelptextFailed" |
    "surveyVueEditVariableNameFailed" |
    "surveyVueSwapMetricSplitBy" |
    "surveyVueSwapMetricSplitByFailed" |
    "reportsPageViewReport" |
    "reportsPageViewChart" |
    "reportsPageViewText" |
    "reportsPageBackFromChart" |
    "reportsPageGoToCrosstab" |
    "reportsPageCopyTextToClipboard" |
    "reportsPageCopyTextToClipboardFailed" |
    "reportsPageCreateNewChart" |
    "reportsPageCreateNewTable" |
    "reportsPageCreateNewFailed" |
    "reportsPageDelete" |
    "reportsPageViewReportSettings" |
    "reportsPageUpdateReportSettings" |
    "reportsPageAddParts" |
    "reportsPageRemovePart" |
    "reportsPageReorderParts" |
    "reportsPageModifyPart" |
    "variablesCreate" |
    "variablesCreateWaves" |
    "variablesUpdate" |
    "variablesUpdateWaves" |
    "variablesDelete" |
    "baseVariablesCreate" |
    "baseVariablesUpdate" |
    "baseVariablesDelete" |
    "calculationTypeConverted" |
    "audiencesSelectAudience" |
    "audiencesSelectNone" |
    "audiencesToggleInstances" |
    "reportsPageToggleEntityInstance" |
    "createdNet" |
    "removedNet" |
    "userSettingsAddUsers" |
    "userSettingsRemoveUser" |
    "allVueChartAverageMeanAdded" |
    "allVueChartAverageMedianAdded" |
    "allVueChartAverageMentionsAdded" |
    "allVueChartAverageMeanRemoved" |
    "allVueChartAverageMedianRemoved" |
    "allVueChartAverageMentionsRemoved" |
    "allVueTableAverageMeanAdded" |
    "allVueTableAverageMedianAdded" |
    "allVueTableAverageMentionsAdded" |
    "allVueTableAverageMeanRemoved" |
    "allVueTableAverageMedianRemoved" |
    "allVueTableAverageMentionsRemoved" |
    "xhrFinish" |
    "tutorialVideoClicked";


export type EventName = ConfigurationEventName | ActionEventName;
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
    entitiesLoaded?: number;
    averageId?: string;
    numberOfPeriodsInAverage?: number;
    page?: string;
    panes?: string[];
    pageLoadTiming?: number;
    title?: string;
}



export function useGoogleTagManager(): IGoogleTagManager {
    const productConfiguration = React.useContext(ProductConfigurationContext);
    const location = useLocation();
    return useMemo(() => {
        var innerGtm = new GoogleTagManager(productConfiguration.productConfiguration);
        return {
            addEvent: (eventName: ActionEventName, pageHandler: PageHandler, values?: ICommonVariables | null) => {
                innerGtm.addEvent(eventName, pageHandler, location, values);
            },
            addConfigurationEvent: (eventName: ConfigurationEventName) => {
                innerGtm.addConfigurationEvent(eventName);
            }
        }
    }, [location, productConfiguration.productConfiguration])
}

export interface IGoogleTagManager {
    addEvent: (eventName: ActionEventName, pageHandler: PageHandler, values?: ICommonVariables | null) => void;
    addConfigurationEvent: (eventName: ConfigurationEventName) => void;
}

export class GoogleTagManager {
    private static readonly eventToMetaObject: EventNameToMeta = {
        downloadExcel: { category: "Downloads", action: "Excel Download", label: undefined },
        downloadChart: { category: "Downloads", action: "Chart Download", label: undefined },

        openFilterDialog: { category: "Filters", action: "Open Filter Dialog", label: undefined },
        closeFilterDialog: { category: "Filters", action: "Close Filter Dialog", label: undefined },
        applyFilter: { category: "Filters", action: "Filter Applied", label: undefined },
        removeFilter: { category: "Filters", action: "Filter Removed", label: undefined },
        clearFilter: { category: "Filters", action: "Filter Cleared", label: undefined },
        removeAllFilters: { category: "Filters", action: "All Filters Removed", label: undefined },
        openBrandChooser: { category: "Brand Chooser", action: "Open Brand Chooser", label: undefined },
        closeBrandChooser: { category: "Brand Chooser", action: "Close Brand Chooser", label: undefined },
        changeFocusBrand: { category: "Brand Chooser", action: "Focus Brand Changed", label: undefined },
        changeBrandSet: { category: "Brand Chooser", action: "Brand Set Changed", label: undefined },
        addBrand: { category: "Brand Chooser", action: "Add Brand", label: undefined },
        removeBrand: { category: "Brand Chooser", action: "Remove Brand", label: undefined },
        applyBrandChooser: { category: "Brand Chooser", action: "Brand Chooser Applied", label: undefined },

        toggleOnLegend: { category: "Legend Toggle", action: "Legend Toggle On", label: undefined },
        toggleOffLegend: { category: "Legend Toggle", action: "Legend Toggle Off", label: undefined },

        selectedSearchBox: { category: "Search Box", action: "Search Box Selected", label: undefined },

        startedTour: { category: "Tours", action: "Tour Started", label: undefined },
        nextStepTour: { category: "Tours", action: "Tour Next Step", label: undefined },
        finishedTour: { category: "Tours", action: "Tour Finished", label: undefined },
        tutorialVideoClicked: {category: "Tours", action: "Clicked Tutorial Video", label: undefined},
        
        changeAverage: { category: "Periods", action: "Average Changed", label: undefined },
        changeDate: { category: "Periods", action: "Date Range Changed", label: undefined },

        changeComparisonPeriod: { category: "Comparisons", action: "Comparison Period Changed", label: undefined },

        surveyVueTestSurvey: { category: "SurveyVue", action: "Clicked Open Test Survey", label: undefined },

        surveyVueDisableMetric: { category: "SurveyVue Configuration", action: "Disabled Metric", label: undefined },
        surveyVueDisableMetricFailed: { category: "SurveyVue Configuration", action: "Failed To Disable Metric", label: undefined },
        surveyVueEnableMetric: { category: "SurveyVue Configuration", action: "Enabled Metric", label: undefined },
        surveyVueEnableMetricFailed: { category: "SurveyVue Configuration", action: "Failed To Enable Metric", label: undefined },
        surveyVueEditHelptext: { category: "SurveyVue Configuration", action: "Edited Helptext", label: undefined },
        surveyVueEditHelptextFailed: { category: "SurveyVue Configuration", action: "Failed To Edit Helptext", label: undefined },
        surveyVueEditVariableName: { category: "SurveyVue Configuration", action: "Edited variable name", label: undefined },
        surveyVueEditVariableNameFailed: { category: "SurveyVue Configuration", action: "Failed To Edit variable name", label: undefined },
        surveyVueEditVarCode: { category: "SurveyVue Configuration", action: "Edited Varcode", label: undefined },
        surveyVueEditVarCodeFailed: { category: "SurveyVue Configuration", action: "Failed To Edit Varcode", label: undefined },
        surveyVueSwapMetricSplitBy: {category: "SurveyVue Configuration", action: "Swapped Metric Split By", label: undefined },
        surveyVueSwapMetricSplitByFailed: {category: "SurveyVue Configuration", action: "Failed To Swap Metric Split By", label: undefined },
        calculationTypeConverted: { category: "Configuration", action: "Converted a metric calculation type", label: undefined},

        reportsPageViewReport: { category: "ReportsPage", action: "Clicked to view a report", label: undefined },
        reportsPageViewChart: { category: "ReportsPage", action: "Clicked View Chart", label: undefined },
        reportsPageViewText: { category: "ReportsPage", action: "Clicked View Text Modal", label: undefined },
        reportsPageBackFromChart: { category: "ReportsPage", action: "Returned From Chart To All Results", label: undefined },
        reportsPageGoToCrosstab: { category: "ReportsPage", action: "Went From Chart To Crosstab", label: undefined },
        reportsPageCopyTextToClipboard: { category: "ReportsPage", action: "Copied All Text Responses To Clipboard", label: undefined },
        reportsPageCopyTextToClipboardFailed: { category: "ReportsPage", action: "Failed To Copy All Text Responses To Clipboard", label: undefined },
        reportsPageCreateNewChart: { category: "ReportsPage", action: "Created new chart report", label: undefined },
        reportsPageCreateNewTable: { category: "ReportsPage", action: "Created new table report", label: undefined },
        reportsPageCreateNewFailed: { category: "ReportsPage", action: "Failed to create new report", label: undefined },
        reportsPageDelete: { category: "ReportsPage", action: "Deleted a report", label: undefined },
        reportsPageViewReportSettings: { category: "ReportsPage", action: "Viewed report settings", label: undefined },
        reportsPageUpdateReportSettings: { category: "ReportsPage", action: "Updated report settings", label: undefined },
        reportsPageAddParts: { category: "ReportsPage", action: "Added charts or tables to report", label: undefined },
        reportsPageRemovePart: { category: "ReportsPage", action: "Removed chart or table from report", label: undefined },
        reportsPageReorderParts: { category: "ReportsPage", action: "Reordered charts or tables in report", label: undefined },
        reportsPageModifyPart: { category: "ReportsPage", action: "Modified settings for a chart or table in report", label: undefined },

        changeCrosstabMetric: { category: "Crosstab", action: "Crosstab Metric Changed", label: undefined },
        changeCrosstabActiveBrand: { category: "Crosstab", action: "Changed Crosstab Active Brand", label: undefined },
        addCrosstabBreak: { category: "Crosstab", action: "Added Crosstab Break", label: undefined },
        removeCrosstabBreak: { category: "Crosstab", action: "Removed Crosstab Break", label: undefined },
        exportCrosstabData: { category: "Crosstab", action: "Exported Crosstab Data", label: undefined },
        showCrosstabCounts: { category: "Crosstab", action: "Enabled Include Counts", label: undefined },
        hideCrosstabCounts: { category: "Crosstab", action: "Disabled Include Counts", label: undefined },
        weightingEnabled: { category: "Crosstab", action: "Show data weighted", label: undefined },
        weightingDisabled: { category: "Crosstab", action: "Show data unweighted", label: undefined },
        showCrosstabLowSample: { category: "Crosstab", action: "Enabled Highlight Low Sample", label: undefined },
        hideCrosstabLowSample: { category: "Crosstab", action: "Disabled Highlight Low Sample", label: undefined },
        showCrosstabSignificance: { category: "Crosstab", action: "Enabled highlight significant values", label: undefined },
        hideCrosstabSignificance: { category: "Crosstab", action: "Disabled highlight significant values", label: undefined },
        changeCrosstabBaseType: { category: "Crosstab", action: "Changed base type override", label: undefined },
        showCrosstabDontKnows: { category: "Crosstab", action: "Disabled exclude don't know responses", label: undefined },
        excludeCrosstabDontKnows: {category: "Crosstab", action: "Enabled exclude don't know responses", label: undefined },
        orderCrosstabResultOrderAsc: { category: "Crosstab", action: "Crosstab in results order (ascending)", label: undefined },
        orderCrosstabResultOrderDesc: { category: "Crosstab", action: "Crosstab in results order (descending)", label: undefined },
        orderCrosstabScriptOrderAsc: { category: "Crosstab", action: "Crosstab in script order (ascending)", label: undefined },
        orderCrosstabScriptOrderDesc: { category: "Crosstab", action: "Crosstab in script order (descending)", label: undefined },

        pageConfigureCreate: { category: "Page Configuration", action: "Created New Page", label: undefined },
        pageConfigureUpdate: { category: "Page Configuration", action: "Updated Page Configuration", label: undefined },
        pageConfigureDelete: { category: "Page Configuration", action: "Deleted A Page", label: undefined },

        metricConfigureCreate: { category: "Metric Configuration", action: "Created New Metric", label: undefined },
        metricConfigureUpdate: { category: "Metric Configuration", action: "Updated Metric Configuration", label: undefined },
        metricConfigureDelete: { category: "Metric Configuration", action: "Deleted A Metric", label: undefined },

        weightingsConfigureCreate: { category: "Weighting Configuration", action: "Created Weightings Configuration", label: undefined },
        weightingsConfigureCopy: { category: "Weighting Configuration", action: "Copied Weightings Configuration To Siblings", label: undefined },
        weightingsConfigureUpdate: { category: "Weighting Configuration", action: "Updated Weightings Configuration", label: undefined },
        weightingsConfigureDelete: { category: "Weighting Configuration", action: "Deleted Weightings Configuration", label: undefined },
        weightingsConfigureDeleteTarget: { category: "Weighting Configuration", action: "Deleted Weightings Plan Target", label: undefined },

        subsetConfigureCreate: { category: "Subset Configuration", action: "Created New Subset", label: undefined },
        subsetConfigureUpdate: { category: "Subset Configuration", action: "Updated Subset Configuration", label: undefined },
        subsetConfigureDelete: { category: "Subset Configuration", action: "Deleted A Subset", label: undefined },

        variablesCreate: { category: "Variables", action: "Created a variable", label: undefined},
        variablesUpdate: { category: "Variables", action: "Updated a variable", label: undefined},
        variablesCreateWaves: { category: "Variables", action: "Created a wave variable", label: undefined},
        variablesUpdateWaves: { category: "Variables", action: "Updated a wave variable", label: undefined},
        variablesDelete: { category: "Variables", action: "Deleted a variable", label: undefined},
        baseVariablesCreate: { category: "Variables", action: "Created a base variable", label: undefined},
        baseVariablesUpdate: { category: "Variables", action: "Updated a base variable", label: undefined},
        baseVariablesDelete: { category: "Variables", action: "Deleted a base variable", label: undefined},

        audiencesSelectAudience: { category: "Audiences", action: "Selected an audience to use as breaks", label: undefined },
        audiencesSelectNone: { category: "Audiences", action: "Selected to show everyone (no audience breaks)", label: undefined },
        audiencesToggleInstances: { category: "Audiences", action: "Toggled some audience break instances", label: undefined },

        reportsPageToggleEntityInstance: {category: "Netting", action: "Toggled entity visibility", label: undefined},
        createdNet: {category: "Netting", action: "Created net", label: undefined},
        removedNet: {category: "Netting", action: "Removed net", label: undefined},
        userSettingsAddUsers: {category: "User settings", action: "Add user to project", label: undefined},
        userSettingsRemoveUser: {category: "User settings", action: "Remove user from project", label: undefined},

        allVueChartAverageMeanAdded: {category: "AllVue Averages", action: "Added a mean average to a chart", label: undefined},
        allVueChartAverageMedianAdded: {category: "AllVue Averages", action: "Added a median average to a chart", label: undefined},
        allVueChartAverageMentionsAdded: {category: "AllVue Averages", action: "Added average mentions to a chart", label: undefined},
        allVueChartAverageMeanRemoved: {category: "AllVue Averages", action: "Removed a mean average from a chart", label: undefined},
        allVueChartAverageMedianRemoved: {category: "AllVue Averages", action: "Removed a median average from a chart", label: undefined},
        allVueChartAverageMentionsRemoved: {category: "AllVue Averages", action: "Removed average mentions from a chart", label: undefined},
        allVueTableAverageMeanAdded: {category: "AllVue Averages", action: "Added a mean average to a table", label: undefined},
        allVueTableAverageMedianAdded: {category: "AllVue Averages", action: "Added a median average to a table", label: undefined},
        allVueTableAverageMentionsAdded: {category: "AllVue Averages", action: "Added average mentions to a table", label: undefined},
        allVueTableAverageMeanRemoved: {category: "AllVue Averages", action: "Removed a mean average from a table", label: undefined},
        allVueTableAverageMedianRemoved: {category: "AllVue Averages", action: "Removed a median average from a table", label: undefined},
        allVueTableAverageMentionsRemoved: {category: "AllVue Averages", action: "Removed average mentions from a table", label: undefined},

        xhrFinish: {category: "Performance", action: "Finished Xhr for Page Load", label: undefined}, 
    }

    readonly DEMO_ORGANISATION: string = 'demo';
    
    dataLayer: any = [];
    productConfiguration: ProductConfiguration;
    defaultEventDataPromise: Promise<{}>;
    constructor(productConfiguration: ProductConfiguration) {
        this.productConfiguration = productConfiguration;
        if (productConfiguration?.googleTags.length || productConfiguration?.gaTags.length) {
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
                    projectOrganisation: this.productConfiguration.projectOrganisation,
                    // To help us get product feedback from our own employees, store the user id that can be looked up in the database
                    savantaInternalUserId: userOrganisation === 'savanta.com' ? this.productConfiguration.user.userId : undefined,
                    subProductId: this.productConfiguration.subProductId,
                };
                this.dataLayer.push(['set', d]);
                return d;
            });

            // Start the tag managers up
            productConfiguration.googleTags.map(t => this.startTagManager(`GTM-${t}`));//remove when gaTags is correctly configured
            productConfiguration.gaTags.map(t => this.startTagManager(t));
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
            Gtag.gtag("set", { "subProductId" : this.productConfiguration.subProductId})
            // Create the script tag to download the GTM script
            const existingScriptTag = documentArg.getElementsByTagName(scriptArg)[0] as HTMLScriptElement;
            const dl = dataLayerArg !== "dataLayer" ? "&l=" + dataLayerArg : "";
            const gtmScriptTag = documentArg.createElement(scriptArg) as HTMLScriptElement;   
            gtmScriptTag.async = true;
            gtmScriptTag.src = "https://www.googletagmanager.com/gtm.js?id=" + gtmId + dl;
            existingScriptTag.parentNode!.insertBefore(gtmScriptTag, existingScriptTag);
        });
    }

    public addEvent(eventName: ActionEventName, pageHandler: PageHandler, location: Location, values?: ICommonVariables | null): void {
        const applicationState = pageHandler.getApplicationStateForGoogleAnalytics(location);
        const supportingData = { ...applicationState, ...values };
        this.addEventInternal(eventName, supportingData);
    }

    public addConfigurationEvent(eventName: ConfigurationEventName): void {
        this.addEventInternal(eventName);
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

    private ensureTrackableCompanyIdSet(event: any, user: ApplicationUser) {
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

    private getCompanyFromUserEmail(user: ApplicationUser): string | undefined {
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
