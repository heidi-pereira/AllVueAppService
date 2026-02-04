import { CustomUIIntegration, IntegrationPosition, IntegrationReferenceType, IntegrationStyle} from "../CustomerPortalApi";


export const listOfHelpButtons = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
    return additionalUIWidgets.filter(x => x.style == IntegrationStyle.Help);
}

export const listOfTabs = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
    return additionalUIWidgets.filter(x => x.style == IntegrationStyle.Tab);
}

export const lhsWidgets = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
    return additionalUIWidgets.filter(x => x.position == IntegrationPosition.Left);
}
export const rhsWidgets = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
    return additionalUIWidgets.filter(x => x.position == IntegrationPosition.Right);
}
