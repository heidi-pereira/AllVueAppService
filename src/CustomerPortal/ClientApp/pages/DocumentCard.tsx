import { SurveyDocument } from "CustomerPortalApi";
import React from "react";
import Moment from "moment/moment";
import { ActionEventName, GoogleTagManager } from "../util/googleTagManager";
import { SurveyDetails } from "../CustomerPortalApi";
import { useSelectedProjectContext } from "../store/SelectedProjectProvider";

const getConvertedDate = (passedDate: Date) => {
    if (passedDate != null) {
        const date = new Date(passedDate);
        return (
            <>
                {Moment(date).format("DD MMM YYYY")}
            </>
        );
    }
    return null;
};

const getFormattedFileSize = (size: number) => {
    if(size < 1000){
        return ("1 KB");
    }
    if(size > 1000 && size < 999999){
        return (`${(size/1000).toFixed()} KB`);
    }
    if(size > 1000000 && size < 999999999){
        return (`${(size/1000000).toFixed(2)} MB`);
    }
    if(size > 999999999){
        return (`${(size/1000000).toFixed(2)} GB`);
    }
    return ("-");
}

const GetUploadedByDetails = (document: SurveyDocument) => {
    if(document.isClientDocument){
        return(
        <td className="uploaded-by">{document.clientName}</td>
        );
    }
    return(
        <td className="uploaded-by">Savanta</td>
    );
}

interface IDocumentCardProps {
    currentFolder: string[],
    document: SurveyDocument,
    surveyDetails: SurveyDetails;
    googleTagManager: GoogleTagManager;
    setIsDeleteModalVisible: (isDeleteModalVisible: boolean) => void,
    setFileToDelete: (surveyDocument: SurveyDocument) => void,
    setCurrentFolder: (subFolder: string[]) => void,
}

const DocumentCard = (props: IDocumentCardProps) => {
    const { state } = useSelectedProjectContext();

    const ConfirmDelete = () => {
        props.setFileToDelete(props.document);
        props.setIsDeleteModalVisible(true);
    }

    const GetDeleteButton = (document: SurveyDocument) => {
        if (document.isClientDocument && state.selectedProject.allVueDocumentationConfiguration.isClientUploadingAllowed) {
            return <td className="td-delete" onClick={() => ConfirmDelete()}><i className="material-symbols-outlined delete-icon">delete</i></td>
        }
        return <td></td>
    }

    const addEvent = (event: ActionEventName) => {
        props.googleTagManager.addEvent(event, props.surveyDetails.organisationShortCode, props.surveyDetails.subProductId);
    }

    return (
        <tr>
            <td className="name">
                <div className="filefolder">
                    {props.document.isFolder &&
                        <>
                        <i className='material-symbols-outlined folder'>folder</i>
                        <a onClick={() => {
                            const copy = props.currentFolder.slice(0);
                                copy.push(props.document.name);
                            props.setCurrentFolder(copy);
                        }}>{props.document.name}</a>
                        </>
                }
                    {!props.document.isFolder &&
                    <>
                        <i className='material-symbols-outlined no-symbol-fill file'>article</i>
                        <a href={props.document.downloadUrl} onClick={() => addEvent("documentsDownload")}>{props.document.name}</a>
                    </>
                }

                 
                </div>
            </td>
            {GetUploadedByDetails(props.document)}
            <td className="date">{getConvertedDate(props.document.lastModified)}</td>
            <td className="file-size">{props.document.isFolder? '':getFormattedFileSize(props.document.size)}</td>
            {GetDeleteButton(props.document)}
        </tr>
    );
}

export default (DocumentCard)