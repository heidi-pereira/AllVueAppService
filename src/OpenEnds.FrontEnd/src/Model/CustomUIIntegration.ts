
export enum IntegrationStyle {
    Tab = "Tab",
    Help = "Help"
}

export enum IntegrationPosition {
    Left = "Left",
    Right = "Right"
}

export enum IntegrationReferenceType {
    WebLink = "WebLink",
    ReportVue = "ReportVue",
    SurveyManagement = "SurveyManagement",
    Page = "Page"
}

export interface CustomUIIntegration {
    path: string;
    name: string;
    icon: string;
    altText: string;
    style: IntegrationStyle;
    position: IntegrationPosition;
    referenceType: IntegrationReferenceType;
}
