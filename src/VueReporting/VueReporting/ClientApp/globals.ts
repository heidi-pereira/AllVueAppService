import { ReportApiClient } from "./reportingApi";
import moment = require("moment");
import {QueryStringManager} from "./QueryStringManager";

export default class Globals {
    public static ReportApiClient: ReportApiClient;
    public static QueryManager: QueryStringManager;
    public static ContextProductName: string;

    public static Initialise(siteRoot: string) {
        moment.defaultFormat = "HH:mm:ss DD/MM/YY";
        this.QueryManager = new QueryStringManager(siteRoot + "/ui");
        this.QueryManager.setInitialView();
        this.ContextProductName = this.QueryManager.getQueryParameter<string>("productName");
        this.ReportApiClient = new ReportApiClient(siteRoot);
    }
}