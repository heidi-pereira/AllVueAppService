import React from "react";
import style from "./CustomUIContols.module.less"
import { CustomUIIntegration, IntegrationStyle, IntegrationPosition, IntegrationReferenceType} from "../../../../BrandVueApi";
import CustomUIEditor from "./CustomUIEditModal";
import { Button} from "reactstrap";
import { ProductConfiguration } from "../../../../ProductConfiguration";
import { getBasePathByPageName } from "../../../helpers/UrlHelper";

interface ICustomUIProp {
    productConfiguration: ProductConfiguration,
    customControl: CustomUIIntegration,
    removeItem(part: CustomUIIntegration): void;
    update: () => void;
}
const CustomUIControl = (props: ICustomUIProp) => {
    const [isEditModalVisible, setIsEditModalVisible] = React.useState(false);
    React.useEffect(() => {
    }, []);
    const targetUrl = (): string => {
        if (props.customControl.referenceType == IntegrationReferenceType.ReportVue) {
            return props.productConfiguration.appBasePath + getBasePathByPageName(props.customControl.path);
        }
        else if (props.customControl.referenceType == IntegrationReferenceType.SurveyManagement) {
            return props.productConfiguration.surveyManagementLink;
        }
        return props.customControl.path;
    }
    return (
        <div className={style.row}>
            <CustomUIEditor isOpen={isEditModalVisible} closeModal={() => { setIsEditModalVisible(false) }} original={props.customControl} update={props.update} />
            <span title={props.customControl.altText}><i className="material-symbols-outlined">{props.customControl.icon}</i></span>
            <div><a target="_blank" href={targetUrl()}>{props.customControl.name}</a></div>
            <div className={style.actionButtons}>
                <div className={style.button} onClick={e => setIsEditModalVisible(true) }  title="Edit custom integration"><i className={`material-symbols-outlined no-symbol-fill ${style.materialButton}`}>edit</i></div>
                <div className={style.button} onClick={e => props.removeItem(props.customControl)} title="Delete custom integration"><i className={`material-symbols-outlined symbol ${style.materialButton}`}>delete</i></div>
            </div>
         </div>
    );
}

interface ICustomUIProps {
    productConfiguration: ProductConfiguration;
    customControls: CustomUIIntegration[];
    addItem(part: CustomUIIntegration): void;
    removeItem(part: CustomUIIntegration): void;
    update: () => void;
}

const CustomUIControls = (props: ICustomUIProps) => {
    const getUiControl = (index: number, integration: CustomUIIntegration) => {
        return <CustomUIControl customControl={integration} removeItem={props.removeItem} key={index} update={props.update} productConfiguration={props.productConfiguration } />
    }

    const addNewExternalLinkWidget = () => {
        const newItem = new CustomUIIntegration();
        newItem.altText = "";
        newItem.icon = "Topic";
        newItem.name = "External";
        newItem.path = "https://example.com";
        newItem.style = IntegrationStyle.Tab;
        newItem.position = IntegrationPosition.Left;
        newItem.referenceType = IntegrationReferenceType.WebLink;
        props.addItem(newItem);
    }

    const addNewPageWidget = () => {
        const newItem = new CustomUIIntegration();
        newItem.altText = "";
        newItem.icon = "Topic";
        newItem.name = "Page";
        newItem.path = "Page";
        newItem.style = IntegrationStyle.Tab;
        newItem.position = IntegrationPosition.Left;
        newItem.referenceType = IntegrationReferenceType.Page;
        props.addItem(newItem);
    }

    const addNewSurveyManagementLinkWidget = () => {
        const newItem = new CustomUIIntegration();
        newItem.altText = "Survey Management";
        newItem.icon = "Topic";
        newItem.name = "Survey Management";
        newItem.path = props.productConfiguration.surveyManagementLink;
        newItem.style = IntegrationStyle.Tab;
        newItem.position = IntegrationPosition.Left;
        newItem.referenceType = IntegrationReferenceType.SurveyManagement;
        props.addItem(newItem);
    }

    const addNewConsumerDutyWidget = () => {
        const newItem = new CustomUIIntegration();
        newItem.altText = "";
        newItem.icon = "app_registration";
        newItem.name = "Dashboard";
        newItem.path = "dashboard";
        newItem.style = IntegrationStyle.Tab;
        newItem.position = IntegrationPosition.Right;
        newItem.referenceType = IntegrationReferenceType.ReportVue;
        props.addItem(newItem);
    }

    const addNewHelpWidget = () => {
        const newItem = new CustomUIIntegration();
        newItem.altText = "";
        newItem.icon = "help";
        newItem.name = "Help";
        newItem.path = "https://docs.savanta.com/allvue/Default.html";
        newItem.style = IntegrationStyle.Help;
        newItem.position = IntegrationPosition.Left;
        newItem.referenceType = IntegrationReferenceType.Page;
        props.addItem(newItem);
    }

    const hasIntegrationEnabled = (integrationType: IntegrationReferenceType) => {
        return props.customControls.filter(item => item.style == IntegrationStyle.Tab && item.referenceType == integrationType).length > 0;
    }

    const renderConsumerUserButton = (onClickEvent, text: string) => {
        return (<Button title="Required for Consumer Duty" className="secondary-button" onClick={onClickEvent}><i className={`material-symbols-outlined no-symbol-fill ${style.consumerDutyIcon}`}>feedback</i>{text}</Button>)
    }

return (<div className={style.box}>
    <div className={style.header}>Custom additions</div>
    <div className={style.content}>
        {props.customControls.length == 0 &&
            <div className={style.defaultText}>No additional controls currently defined</div>
        }
        {props.customControls.map((item, index) =>getUiControl(index, item))}
    </div>
    <div className={style.bottom}>
        <div className={style.actions}>

            <Button className="secondary-button" onClick={addNewExternalLinkWidget}>Add Web Link</Button>
            <Button className="secondary-button" onClick={addNewPageWidget}>Add Page</Button>

            {!hasIntegrationEnabled(IntegrationReferenceType.SurveyManagement) && renderConsumerUserButton(addNewSurveyManagementLinkWidget, "Enable Survey Management")}
            {!hasIntegrationEnabled(IntegrationReferenceType.ReportVue) && renderConsumerUserButton(addNewConsumerDutyWidget, "Enable ReportVue Dashboard")}

            <Button className="secondary-button" onClick={addNewHelpWidget}>Add Help</Button>
            
        </div>
    </div>
</div>);
}

export default CustomUIControls;