import React from "react";
import style from "./CustomUIContols.module.less"
import { CustomUIIntegration, IntegrationStyle, IntegrationReferenceType, IntegrationPosition } from "../../../../BrandVueApi";
import { Modal, ModalFooter, ModalHeader, ModalBody, Button, ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";
import { IndentStyle } from "typescript/lib/tsserverlibrary";
interface ICustomEditorProp {
    original: CustomUIIntegration;
    isOpen: boolean;
    closeModal: () => void;
    update: () => void;

}
const CustomUIEditor = (props: ICustomEditorProp) => {

    const [nameToUpdate, setNameToUpdate] = React.useState<string>("");
    const [altTextToUpdate, setAltTextToUpdate] = React.useState<string>("");
    const [iconToUpdate, setIconToUpdate] = React.useState<string>("");
    const [pathToUpdate, setPathlToUpdate] = React.useState<string>("");
    const [styleToUpdate, setStyleToUpdate] = React.useState<IntegrationStyle>(IntegrationStyle.Tab);
    const [referenceTypeToUpdate, setReferenceTypeToUpdate] = React.useState<IntegrationReferenceType>(IntegrationReferenceType.WebLink);
    const [positionToUpdate, setPositionToUpdate] = React.useState<IntegrationPosition>(IntegrationPosition.Left);

    const [isStyleDropdownOpen, setIsStyleDropdownOpen] = React.useState<boolean>(false);
    const [isPositionDropdownOpen, setIsPositionDropdownOpen] = React.useState<boolean>(false);
    const [isReferenceTypeDropdownOpen, setIsReferenceTypeDropdownOpen] = React.useState<boolean>(false);

    React.useEffect(() => {
        if (props.isOpen) {
            setNameToUpdate(props.original.name);
            setAltTextToUpdate(props.original.altText);
            setIconToUpdate(props.original.icon);
            setPathlToUpdate(props.original.path);
            setStyleToUpdate(props.original.style);
            setReferenceTypeToUpdate(props.original.referenceType);
            setPositionToUpdate(props.original.position);
        }
    }, [props.isOpen]);

    const save = () => {

        props.original.name = nameToUpdate;
        props.original.altText = altTextToUpdate;
        props.original.icon = iconToUpdate;
        props.original.path = pathToUpdate;
        props.original.style = styleToUpdate;
        props.original.referenceType = referenceTypeToUpdate;
        props.original.position = positionToUpdate;
        props.update();
        props.closeModal();
    }

    const renderPosition = () => {
        return (<ButtonDropdown isOpen={isPositionDropdownOpen} toggle={() => setIsPositionDropdownOpen(!isPositionDropdownOpen)} className="dropdown">
            <DropdownToggle className="metric-selector-toggle toggle-button">
                <span>{positionToUpdate}</span>
                <i className={`${style.symbol} material-symbols-outlined`}>arrow_drop_down</i>
            </DropdownToggle>
            <DropdownMenu>
                <DropdownItem key={IntegrationPosition.Left} onClick={() => setPositionToUpdate(IntegrationPosition.Left)}>
                    <div className="name-container"><span className='title'>{IntegrationPosition.Left}</span></div>
                </DropdownItem>
                <DropdownItem key={IntegrationPosition.Right} onClick={() => setPositionToUpdate(IntegrationPosition.Right)}>
                    <div className="name-container"><span className='title'>{IntegrationPosition.Right}</span></div>
                </DropdownItem>
            </DropdownMenu>
        </ButtonDropdown>);
    }

    const renderType = () => {
        return (<ButtonDropdown isOpen={isReferenceTypeDropdownOpen} toggle={() => setIsReferenceTypeDropdownOpen(!isReferenceTypeDropdownOpen)} className="dropdown">
            <DropdownToggle className="metric-selector-toggle toggle-button">
                <span>{referenceTypeToUpdate}</span>
                <i className={`${style.symbol} material-symbols-outlined`}>arrow_drop_down</i>
            </DropdownToggle>
            <DropdownMenu>
                {referenceTypeToUpdate == IntegrationReferenceType.ReportVue &&
                    <DropdownItem key={IntegrationReferenceType.ReportVue} onClick={() => setReferenceTypeToUpdate(IntegrationReferenceType.ReportVue)}>
                        <div className="name-container"><span className='title'>{IntegrationReferenceType.ReportVue}</span></div>
                    </DropdownItem>
                }
                {referenceTypeToUpdate == IntegrationReferenceType.WebLink &&
                    <DropdownItem key={IntegrationReferenceType.WebLink} onClick={() => setReferenceTypeToUpdate(IntegrationReferenceType.WebLink)}>
                        <div className="name-container"><span className='title'>{IntegrationReferenceType.WebLink}</span></div>
                    </DropdownItem>
                }
                {referenceTypeToUpdate == IntegrationReferenceType.SurveyManagement &&
                    <DropdownItem key={IntegrationReferenceType.SurveyManagement} onClick={() => setReferenceTypeToUpdate(IntegrationReferenceType.SurveyManagement)}>
                        <div className="name-container"><span className='title'>{IntegrationReferenceType.SurveyManagement}</span></div>
                    </DropdownItem>
                }
            </DropdownMenu>
        </ButtonDropdown>);
    }
    const dialogTitle = () => {
        switch (styleToUpdate) {
            case IntegrationStyle.Help:
                return "Help Icon"
        }
        switch (referenceTypeToUpdate) {
            case IntegrationReferenceType.ReportVue:
                return "Report Vue";
            case IntegrationReferenceType.SurveyManagement:
                return "Survey Management";
            case IntegrationReferenceType.WebLink:
                return "Web Link";
            default:
                return referenceTypeToUpdate;
        }
    }
    return (<Modal isOpen={props.isOpen} toggle={() => props.closeModal()} centered={true} className={`modal-dialog-centered content-modal modal-copy edit-3rdPartyIntegrations-modal`}>
        <ModalHeader>
            <div className="settings-modal-header">
                <div className="close-icon">
                    <button type="button" className="btn btn-close" onClick={() => props.closeModal()}>
                       
                    </button>
                </div>
                <div className="set-name">Edit Custom Integration ({dialogTitle() })</div>
            </div>
        </ModalHeader>
        <ModalBody >
            <div className="input-container">
                <label htmlFor="folder-name-input">Position:</label>
                {renderPosition()}
            </div>
            {styleToUpdate == IntegrationStyle.Tab && referenceTypeToUpdate != IntegrationReferenceType.ReportVue && referenceTypeToUpdate != IntegrationReferenceType.SurveyManagement && 
                <div className="input-container">
                    <label htmlFor="folder-name-input">Type:</label>
                    {renderType()}
                </div>
            }

            <div className="input-container">
                <label htmlFor="folder-name-input">Name:</label>
                <input className="folder-name-input"
                    id="folder-name-input"
                    type="text"
                    autoFocus={true}
                    autoComplete="off"
                    value={nameToUpdate}
                    onChange={(e) => setNameToUpdate(e.target.value)} />
            </div>

            <div className="input-container">
                <label htmlFor="folder-name-input">Icon:<span title="Icon preview"><i className="material-symbols-outlined">{iconToUpdate}</i></span></label>
                <input className="folder-name-input"
                    id="folder-name-input"
                    type="text"
                    autoFocus={true}
                    autoComplete="off"
                    value={iconToUpdate}
                    onChange={(e) => setIconToUpdate(e.target.value)} />
            </div>

            <div className="input-container">
                <label htmlFor="folder-name-input">Url:</label>
                <input className="folder-name-input"
                    id="folder-name-input"
                    type="text"
                    disabled={referenceTypeToUpdate == IntegrationReferenceType.SurveyManagement }
                    autoFocus={true}
                    autoComplete="off"
                    value={pathToUpdate}
                    onChange={(e) => setPathlToUpdate(e.target.value)} />
            </div>
            
            <div className="input-container">
                <label htmlFor="folder-name-input">Alt text:</label>
                <input className="folder-name-input"
                    id="folder-name-input"
                    type="text"
                    autoFocus={true}
                    autoComplete="off"
                    value={altTextToUpdate}
                    onChange={(e) => setAltTextToUpdate(e.target.value)} />
            </div>

        </ModalBody>
        <ModalFooter>
            <Button className="secondary-button" onClick={() => props.closeModal()}>Cancel</Button>
            <Button className="primary-button destructive" onClick={() => save()} >Update</Button>
        </ModalFooter>
    </Modal>
    );
}
export default CustomUIEditor