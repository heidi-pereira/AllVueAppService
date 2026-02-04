import { CustomUIIntegration, IntegrationStyle, IntegrationPosition, IntegrationReferenceType } from "../../Model/CustomUIIntegration";

const getUrlSafePageName = (pageName: string): string => {
    return pageName.toLowerCase().replace(/[\s:]/g, '-').replace(/[?#[\]@!$'()*+,;=%\\]+/g, '').replace(/&/g, 'and').replace(/\//g, 'or');
};
const getPathByPageName = (pageName: string): string => {
    return "/" + getUrlSafePageName(pageName);
};
export const listOfTabs = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
    return additionalUIWidgets.filter(x => x.style == IntegrationStyle.Tab);
};
export const lhsWidgets = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
    return additionalUIWidgets.filter(x => x.position == IntegrationPosition.Left);
};
export const rhsWidgets = (additionalUIWidgets: CustomUIIntegration[]): CustomUIIntegration[] => {
    return additionalUIWidgets.filter(x => x.position == IntegrationPosition.Right);
};
export const renderCustomTab = (integration: CustomUIIntegration, index: number) => {
    if (integration.referenceType == IntegrationReferenceType.Page || integration.referenceType == IntegrationReferenceType.ReportVue) {
        return (<a href={getPathByPageName(integration.path)} className="tab-link" key={index} title={integration.altText}>
            <i className="material-symbols-outlined">{integration.icon}</i>
            <span>{integration.name}</span>
        </a>);
    }
    else if (integration.referenceType == IntegrationReferenceType.WebLink ||
        integration.referenceType == IntegrationReferenceType.SurveyManagement) {
        return (<a href={integration.path} className="tab-link" key={index} title={integration.altText}>
            <i className="material-symbols-outlined">{integration.icon}</i>
            <span>{integration.name}</span>
        </a>);
    }
};
