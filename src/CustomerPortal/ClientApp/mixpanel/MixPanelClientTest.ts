import { IMixPanelClient } from "./IMixPanelClient";
import { VueEventProps } from "./MixPanelHelper";
import { UserProfile } from "./UserProfile";

export class MixPanelClientTest implements IMixPanelClient {
    init(projectId: string): void {
    }
    identify(userId: string): void {
    }
    track(eventName: string, props: VueEventProps): void {
    }
    reset(): void {
    }
    setPeople(userProfile: UserProfile) {
        console.log("setProfile");
    }
}