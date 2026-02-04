import { CustomUIIntegration } from "../../Model/CustomUIIntegration";
import { NavigationTab } from "../../Model/Model";

export interface ISurveyContext {
    id: string;
    name: string | undefined;
    availableTabs: NavigationTab[];
    customUiIntegrations: CustomUIIntegration[];
}