import { MixPanelProps, VueEventProps, VueEventName, MixPanelModel } from './MixPanelHelper';
import { IMixPanelClient } from './IMixPanelClient';
import { UserProfile } from './UserProfile';
import { UserContext } from 'CustomerPortalApi';

export class MixPanel {
    public static readonly Props: MixPanelProps = {
        pageLoaded: new VueEventProps("Performance", "Non Event", "Profiling"),
    };
    private static client: IMixPanelClient;
    private static productName: string;

    public static init(model: MixPanelModel) {
        this.client = model.client;
        this.client.init(model.projectId)
        this.client.identify(model.userId);
        this.productName = model.productName;
    }

    public static logout() {
        const userLoggedOut = "userLoggedOut";
        let props = MixPanel.Props[userLoggedOut];
        this.client.track(this.camelCaseToTitle(userLoggedOut), props);
    }

    public static track(eventName: VueEventName) {
        let props = MixPanel.Props[eventName];
        let propsObj = this.addProps(props as object);
        this.client.track(this.camelCaseToTitle(eventName), propsObj);
    }

    public static trackPageLoadTime(time: any, page: string) {
        const eventName : VueEventName = "pageLoaded";
        let props = MixPanel.Props[eventName] as object;
        props["Page Load Time"] = time;
        props = this.addProps(props);
        this.client.track(`${page} ${this.camelCaseToTitle(eventName)}`, props);
    }

    public static trackPage(page: string) {
        const pageSelected = "pageSelected";
        let props = MixPanel.Props[pageSelected];
        props.Page = page;
        let propsObj = this.addProps(props as object);
        this.client.track(this.camelCaseToTitle(pageSelected), propsObj);
    }

    public static setPeople(user: UserContext) {
        if(user) {
            let userProfile = this.getUserProfileFromUser(user);
            this.client.setPeople(userProfile)
        }
    }

    private static getUserProfileFromUser(user: UserContext) {
        let result = new UserProfile();
        result.Roles = this.getRoleFromUser(user);
        result.$name = user.firstName + " " + user.lastName;
        result.$email = user.userName;
        result.$organisation = this.getCompanyFromUserEmail(user);
        
        return result;
    }

    private static getCompanyFromUserEmail(user: UserContext): string {
        const userEmail = user.userName; // user names are ALWAYS emails

        if (userEmail) {
            const index = userEmail.indexOf('@');
            if (index < 0) {
                return 'no domain';
            } else if (index === userEmail.length - 1) {
                return 'domain truncated';
            } else {
                return userEmail.substring(index + 1);
            }
        }

        return "";
    }

    private static getRoleFromUser(user: UserContext): string[] {
        const result: string[] = [];
        if (user.isAdministrator) {
            result.push("Administrator");
        }
        if (user.isReportViewer) {
            result.push("Report Viewer");
        }
        if (user.isSystemAdministrator) {
            result.push("System Administrator");
        }
        if (user.isTrialUser) {
            result.push("Trial User");
        }

        return result;
    }

    private static camelCaseToTitle(camelCase: string): string {
        // Split the string at each uppercase letter and capitalize the first letter of each word
        const words = camelCase.split(/(?=[A-Z])/).map(word => {
            return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
        });

        // Join the words with spaces
        return words.join(' ');
    }

    private static addProps(props: object) : object{
        props["Subset"] = "All";
        props["Product"] = this.productName;
        return props;
    }
}