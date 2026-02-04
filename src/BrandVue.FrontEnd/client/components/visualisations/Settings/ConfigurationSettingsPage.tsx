import React from "react";
import * as BrandVueApi from "../../../BrandVueApi";
import {CustomUIIntegration, Factory} from "../../../BrandVueApi";
import { IGoogleTagManager } from "../../../googleTagManager";
import {PageHandler} from "../../PageHandler";
import style from "./ConfigurationSettingsPage.module.less"
import {ProductConfigurationContext} from "../../../ProductConfigurationContext";
import {UserContext} from "../../../GlobalContext";
import {FormGroup} from "react-bootstrap";
import {Input, Label} from "reactstrap";
import Throbber from "../../throbber/Throbber";
import SynchDataContol from "./Controls/SynchDataContol";
import CustomUIControls from "./Controls/CustomUIControls";

interface IConfigurationSettingsProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
}

const ConfigurationSettingsPage = (props: IConfigurationSettingsProps) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const [isUpdating, setIsUpdating] = React.useState<boolean>(false);
    const [isQuotaTabAvailableChecked, setIsQuotaTabAvailableChecked] = React.useState<boolean>(productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.QuotaTabAvailable))
    const [isDocumentsTabAvailableChecked, setIsDocumentsTabAvailableChecked] = React.useState<boolean>(productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.DocumentsTabAvailable))
    const [isDataTabAvailableChecked, setIsDataTabAvailableChecked] = React.useState<boolean>(productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.DataTabAvailable))
    const [isReportTabAvailableChecked, setIsReportTabAvailableChecked] = React.useState<boolean>(productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.ReportTabAvailable))
    const [isHelpIconChecked, setIsHelpIconChecked] = React.useState<boolean>(productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.HelpIconAvailable))
    const [isClientUploadingAllowed, setIsClientUploadingAllowed] = React.useState<boolean>(productConfiguration.allVueDocumentationConfiguration.isClientUploadingAllowed)
    const [enableSecureFileDownload, setEnableSecureFileDownload] = React.useState<boolean>(productConfiguration.allVueDocumentationConfiguration.enableSecureFileDownload)
    const [additionalUIWidgets, setAdditionalUIWidgets] = React.useState<CustomUIIntegration[]>(productConfiguration.additionalUiWidgets.map(obj => new CustomUIIntegration({ ...obj })));


    const remove3rdPartyWidget = (itemToRemove: CustomUIIntegration) => {
        const items = additionalUIWidgets;
        const newList = items.filter(x => x != itemToRemove);
        setAdditionalUIWidgets(newList);
    }
    const update3rdPartyWidget = () => {
        setAdditionalUIWidgets([...additionalUIWidgets]);
    }

    const addDefault3rdPartyWidget = (newItem: CustomUIIntegration) => {
        const current = additionalUIWidgets;
        current.push(newItem);
        setAdditionalUIWidgets([...current]);
    }

    const hasValidChanges = () => {
        const hasAdditionalUIWidgetsChanged = additionalUIWidgets.length != productConfiguration.additionalUiWidgets.length ||
            JSON.stringify(additionalUIWidgets) != JSON.stringify(productConfiguration.additionalUiWidgets);
        return (
            isQuotaTabAvailableChecked != productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.QuotaTabAvailable) ||
            isDocumentsTabAvailableChecked != productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.DocumentsTabAvailable) ||
            isDataTabAvailableChecked != productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.DataTabAvailable) ||
            isReportTabAvailableChecked != productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.ReportTabAvailable) ||
            isHelpIconChecked != productConfiguration.isProductFeatureEnabled(BrandVueApi.AdditionalProductFeature.HelpIconAvailable) ||
            isClientUploadingAllowed != productConfiguration.allVueDocumentationConfiguration.isClientUploadingAllowed ||
            enableSecureFileDownload != productConfiguration.allVueDocumentationConfiguration.enableSecureFileDownload ||
            hasAdditionalUIWidgetsChanged
        );
    }

    const SaveProductConfiguration = async () => {
        const client = Factory.AllVueConfigurationClient(error => error());
        setIsUpdating(true);
        const details = await client.getProductConfiguration();

        details.isReportsTabAvailable = isReportTabAvailableChecked;
        details.isQuotaTabAvailable = isQuotaTabAvailableChecked;
        details.isDocumentsTabAvailable = isDocumentsTabAvailableChecked;
        details.isDataTabAvailable = isDataTabAvailableChecked;
        details.isHelpIconAvailable = isHelpIconChecked;
        details.additionalUiWidgets = additionalUIWidgets;
        details.allVueDocumentationConfiguration.enableSecureFileDownload = enableSecureFileDownload;
        details.allVueDocumentationConfiguration.isClientUploadingAllowed = isClientUploadingAllowed;

        await client.updateConfiguration(details);
    };

    if (isUpdating) {
        return <div id="ld" className="loading-container">
            <Throbber />
        </div>;
    }
    return (
        <div className={style.configurationSettingsPage}>
            <UserContext.Consumer>
                {(user) => {
                    if (user?.isSystemAdministrator) {
                        return <FormGroup>
                            <Label className={style.optionsTitle}><span><i className={`material-symbols-outlined`}>supervisor_account</i></span>Savanta System Administrators (only)</Label>
                            <SynchDataContol />
                            {!productConfiguration.isSurveyGroup &&
                                <>
                                    <Input id="set-quota-tab-available" type="checkbox" className="checkbox" checked={isQuotaTabAvailableChecked} onChange={() => setIsQuotaTabAvailableChecked(!isQuotaTabAvailableChecked)}></Input>
                                    <Label for="set-quota-tab-available">Enable Quotas Tab</Label>

                                    <Input id="set-documents-tab-available" type="checkbox" className="checkbox" checked={isDocumentsTabAvailableChecked} onChange={() => setIsDocumentsTabAvailableChecked(!isDocumentsTabAvailableChecked)}></Input>
                                    <Label for="set-documents-tab-available">Enable Documents Tab</Label>
                                    <div className={style.subDocumentItems}>
                                        <Input id="set-documents-client-uploadingAllowed" disabled={!isDocumentsTabAvailableChecked} type="checkbox" className="checkbox" checked={isClientUploadingAllowed} onChange={() => setIsClientUploadingAllowed(!isClientUploadingAllowed)}></Input>
                                        <Label for="set-documents-client-uploadingAllowed" disabled={!isDocumentsTabAvailableChecked}>Allow clients to upload docs</Label>

                                        <Input id="set-documents-enableSecureFileDownload" disabled={!isDocumentsTabAvailableChecked} type="checkbox" className="checkbox" checked={enableSecureFileDownload} onChange={() => setEnableSecureFileDownload(!enableSecureFileDownload)}></Input>
                                        <Label for="set-documents-enableSecureFileDownload" disabled={!isDocumentsTabAvailableChecked}>Secure downloading of files</Label>
                                    </div>
                                </>
                            }
                            <Input id="set-data-tab-available" type="checkbox" className="checkbox" checked={isDataTabAvailableChecked} onChange={() => setIsDataTabAvailableChecked(!isDataTabAvailableChecked)}></Input>
                            <Label for="set-data-tab-available">Enable Data Tab</Label>

                            <Input id="set-report-tab-available" type="checkbox" className="checkbox" checked={isReportTabAvailableChecked} onChange={() => setIsReportTabAvailableChecked(!isReportTabAvailableChecked)}></Input>
                            <Label for="set-report-tab-available">Enable Reports Tab</Label>

                            <Input id="set-help-icon-available" type="checkbox" className="checkbox" checked={isHelpIconChecked} onChange={() => setIsHelpIconChecked(!isHelpIconChecked)}></Input>
                            <Label for="set-help-icon-available">Enable Help Icon</Label>

                            <CustomUIControls
                                customControls={additionalUIWidgets}
                                addItem={addDefault3rdPartyWidget}
                                removeItem={remove3rdPartyWidget}
                                update={update3rdPartyWidget}
                                productConfiguration={productConfiguration}
                            />
                            <button id="save" type="button" disabled={!hasValidChanges()} className="primary-button" onClick={(e) => SaveProductConfiguration()}>Save</button>
                            <div className={style.infoText}>Only Savanta system administrators can modify these settings</div>
                        </FormGroup>
                    }
                }}
            </UserContext.Consumer>
        </div>
    );
}
export default ConfigurationSettingsPage;