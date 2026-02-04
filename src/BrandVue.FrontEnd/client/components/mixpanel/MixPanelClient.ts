import mixpanel from "mixpanel-browser";
import { IMixPanelClient } from "./IMixPanelClient";
import { VueEventProps } from "./MixPanelHelper";
import { UserProfile } from "./UserProfile";

export class MixPanelClient implements IMixPanelClient {
    init(projectId: string): void {
        mixpanel.init(projectId, {});
    }
    identify(userId: string): void {
        mixpanel.identify(userId);
    }
    track(eventName: string, props: VueEventProps): void {
        mixpanel.track(eventName, props);
    }
    reset(): void {
        mixpanel.reset();
    }
    setPeople(userProfile: UserProfile) {
        mixpanel.people.set(userProfile);
    }
}