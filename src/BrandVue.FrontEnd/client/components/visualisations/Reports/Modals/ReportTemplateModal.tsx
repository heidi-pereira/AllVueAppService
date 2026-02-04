import Throbber from "client/components/throbber/Throbber";
import React, { useEffect } from "react";
import { ModalBody } from "react-bootstrap";
import { Modal } from "reactstrap";
import * as BrandVueApi from "../../../../BrandVueApi";
import style from "./ReportTemplateModal.module.less";
import { ReportTemplate, ReportTemplateModel } from "client/BrandVueApi";
import toast from "react-hot-toast";
import { useAppDispatch, useAppSelector } from "client/state/store";
import { fetchTemplatesForUser } from "client/state/templatesSlice";
import { MixPanel } from "client/components/mixpanel/MixPanel";

interface IReportTemplateModalProps {
    isOpen: boolean;
    selectedReportId: number;
    selectedReportName:string;
    setIsOpen(isOpen: boolean): void;
    closeAll(): void;
}

const ReportTemplateModal = (props: IReportTemplateModalProps) => {
    const dispatch = useAppDispatch();
    const [templateName, setTemplateName] = React.useState(props.selectedReportName);
    const [templateDescription, setTemplateDescription] = React.useState("");
    const [isLoading, setIsLoading] = React.useState(false);
    const { templates: existingTemplates } = useAppSelector(state => state.templates);
    
    const reportTemplateClient = BrandVueApi.Factory.ReportTemplateClient(error => error());

    const createTemplate = async () => {
        const templateWithNameExists = existingTemplates.some(template => template.templateDisplayName === templateName);
        if(templateWithNameExists) {
            toast.error(`Template with name ${templateName} already exists`);
            return;
        }

        if(templateName.length > 256) {
            toast.error("Template name must be 256 characters or less");
            return;
        }

        if(templateDescription.length > 256) {
            toast.error("Template description must be 256 characters or less");
            return;
        }

        setIsLoading(true);
        try {
            const templateModel = new ReportTemplateModel({
                savedReportId: props.selectedReportId,
                templateDescription: templateDescription,
                templateDisplayName: templateName
            });

            const response = await reportTemplateClient.saveReportAsTemplate(templateModel)

            if (response.status === 200) {
                toast.success("Template created successfully");
                MixPanel.track("reportTemplateCreated");
                dispatch(fetchTemplatesForUser());
                props.closeAll();
            } else {
                toast.error(`Error: Unable to create template`);
            }
        } catch (error) {
            toast.error("Error: Unable to create template");
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <Modal isOpen={props.isOpen} className="report-template-modal" centered keyboard={false} autoFocus={false}>
            <ModalBody>
                <button onClick={() => props.setIsOpen(false)} className="modal-close-button" title="Close">
                    <i className="material-symbols-outlined">close</i>
                </button>
                <div className="header">
                    Create template
                </div>
                <>
                    {isLoading ? (
                        <div className={style.throbber}>
                            <Throbber />
                        </div>
                    ) : (
                        <>
                        <div className={style.savedTemplates}>
                            <div className={style.templateField}>
                                <label htmlFor="template-name">Template name</label>
                                <input type="text"
                                    id="template-name"
                                    name="template-name-input"
                                    className={style.textInput}
                                    value={templateName}
                                    onChange={(e) => setTemplateName(e.target.value)}
                                    autoFocus
                                    autoComplete="off"/>
                            </div>
                            <div className={style.templateField}>
                                <label htmlFor="template-description">Template description</label>
                                <textarea
                                    id="template-description"
                                    name="template-description-input"
                                    className={style.textArea}
                                    value={templateDescription}
                                    onChange={(e) => setTemplateDescription(e.target.value)}
                                    autoComplete="off"/>
                            </div>
                        </div>
                        <div className={style.modalButtons}>
                            <button className={style.modalButton + " secondary-button"} onClick={() => props.setIsOpen(false)}>Cancel</button>
                            <button className={style.modalButton + " primary-button"} onClick={createTemplate}>Create template</button>
                        </div>
                        </>
                    )}
                </>
            </ModalBody>
        </Modal>
    )
}

export default ReportTemplateModal;