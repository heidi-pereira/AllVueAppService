import React from 'react';
import { FileInformation } from "../../../../../BrandVueApi";
import { getFormatedDate } from "../../../../helpers/DateHelper";
import style from "./FoldersAndDocuments.module.less";

const getFormattedFileSize = (size: number|undefined) => {
    if (!size) {
        return "";
    }
    if (size < 1000) {
        return ("1 KB");
    }
    if (size > 1000 && size < 999999) {
        return (`${(size / 1000).toFixed()} KB`);
    }
    if (size > 1000000 && size < 999999999) {
        return (`${(size / 1000000).toFixed(2)} MB`);
    }
    if (size > 999999999) {
        return (`${(size / 1000000).toFixed(2)} GB`);
    }
    return ("-");
}

const GetUploadedByDetails = (document: FileInformation) => {
    return (
        <td className={style.uploadedBy}>Savanta</td>
    );
}

interface IDocumentCardProps {
    document: FileInformation,
    updateDocuments: () => void,
    setIsDeleteModalVisible: (isDeleteModalVisible: boolean) => void,
    setFileToDelete: (surveyDocument: FileInformation) => void,
    setFolder: (string) => void,
    setPreviewFile: (string) => void,
    urlPathToRoot: string;
}

const DocumentCard: React.FunctionComponent<IDocumentCardProps> = (props: IDocumentCardProps) => {

    const getConvertedDate = (passedDate: Date | undefined) => {
        if (passedDate != null) {
            return (
                <>
                    {getFormatedDate(passedDate)}
                </>
            );
        }
        return null;
    };

    const transformUrl = (value: string): string => {
        const transformedUrl = `${props.urlPathToRoot}/${value}`
        return (transformedUrl)
    }

    const ConfirmDelete = () => {
        props.setFileToDelete(props.document);
        props.setIsDeleteModalVisible(true);
    }
    const isDocumentAnImage = () => {
        if (props.document.isFolder)
            return false;
        if (!props.document.name.toLocaleLowerCase().endsWith(".json")) {
            return true;
        }
        return false;
    }
    const ClickOnFile = () => {
        if (props.document.isFolder)
            props.setFolder(props.document.name);
        else
            props.setPreviewFile(props.document.name);
    }

    const GetDeleteButton = (document: FileInformation) => {
        if (document.canDelete)
            return <td className={style.tdDelete} onClick={() => ConfirmDelete()}><i className="material-symbols-outlined delete-icon">delete</i></td>
        return <td></td>
    }

    const iconName = props.document.isFolder ? "topic" : props.document.displayName.endsWith(".json") ? "file_present" : "attachment"
    const isImage = isDocumentAnImage();
    return (<tr className={style.tableRow}>
        <td className={style.name} onClick={() => ClickOnFile()}><span><i className="material-symbols-outlined no-symbol-fill">{iconName}</i></span></td>
        {isImage &&
            <td className={style.name + ' ' + (props.document.isFolder ? style.folderName : style.fileName)}><a target="_blank" href={transformUrl(props.document.displayName)}>{props.document.displayName}</a></td>
        }
        {!isImage &&
            <td className={style.name + ' ' + (props.document.isFolder ? style.folderName : style.fileName)} onClick={() => ClickOnFile()}>{props.document.displayName}</td>
        }
            {GetUploadedByDetails(props.document)}
        <td className={style.date}>{getConvertedDate(props.document.lastModified)}</td>
        <td className={style.fileSize}>{getFormattedFileSize(props.document.size)}</td>
            {GetDeleteButton(props.document)}
        </tr>
    );
}

export default DocumentCard