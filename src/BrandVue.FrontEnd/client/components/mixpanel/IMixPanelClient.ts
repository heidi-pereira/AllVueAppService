import { VueEventProps } from "./MixPanelHelper";
import { UserProfile } from "./UserProfile";

export interface IMixPanelClient {
    init(projectId: string): void;
    identify(userId: string): void;
    track(eventName: string, props: VueEventProps | object): void;
    reset(): void;
    setPeople(userProfile: UserProfile)
}