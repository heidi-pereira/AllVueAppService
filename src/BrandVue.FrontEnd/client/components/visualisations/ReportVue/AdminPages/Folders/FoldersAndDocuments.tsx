import React from 'react'
import { FileInformation } from "../../../../../BrandVueApi";
import * as BrandVueApi from "../../../../../BrandVueApi";

import DocumentCard from "./DocumentCard";
import style from "./FoldersAndDocuments.module.less";


enum sortType {
    dateAsc,
    dateDesc,
    nameAsc,
    nameDesc
}

enum sortIndicator {
    up,
    down,
    none,
}
enum sortColumn {
    date,
    name,
}

const sortDocuments = (tableSortState: sortType, documents: FileInformation[]) => {
    switch (tableSortState) {
        case sortType.dateAsc:
            return documents?.sort((a, b) => {

                if (b.lastModified == undefined) {
                    return 1;
                }
                if (a.lastModified == undefined) {
                    return -1;
                }
                return b.lastModified.valueOf() - a.lastModified.valueOf()
            });

        case sortType.dateDesc:
            return documents?.sort((a, b) =>

            {

                if (b.lastModified == undefined) {
                    return 1;
                }
                if (a.lastModified == undefined) {
                    return -1;
                }
                return b.lastModified.valueOf() - a.lastModified.valueOf()
            }).reverse()

        case sortType.nameAsc:
            return documents?.sort((a, b) => a.name.localeCompare(b.name));

        case sortType.nameDesc:
            return documents?.sort((a, b) => a.name.localeCompare(b.name)).reverse();
    }
};

interface IDocumentsProps {
    documents: FileInformation[],
    updateDocuments: () => void,
    setIsDeleteModalVisible: (boolean) => void,
    setFileToDelete: (SurveyDocument) => void,
    setFolder: (string) => void,
    setPreviewFile: (string) => void,
    isGettingData: boolean,
}

const FoldersAndDocuments: React.FunctionComponent<IDocumentsProps> = ({ documents, updateDocuments, setIsDeleteModalVisible, setFileToDelete, setFolder, setPreviewFile, isGettingData}: IDocumentsProps) => {

    const [tableSortState, setTableSortState] = React.useState<sortType>(sortType.dateAsc);

    const [urlPathToRoot, setUrlPathToRoot] = React.useState("");

    React.useEffect(() => {
        const client = BrandVueApi.Factory.ReportVueClient(error => error());
        if (documents && documents.length > 0) {
            const folders = documents[0].name.split("\\");
            let pathToDocument = folders.slice(0, folders.length - 1).join("\\");
            if (!pathToDocument) {
                pathToDocument = ".\\";
            }
            client.urlToUnpublishedFile(pathToDocument).then(url => setUrlPathToRoot(url))
        }
    }, [documents]);


    if (isGettingData) {
        return <div className={style.waitingContainer}>
            <div className={style.waitingGroup}>
                <div className={style.waitingItem}>Getting folder contents</div>
                <div className={style.waitingItem + ' custom-spinner'}></div>
            </div>
        </div>;

    }

    const orderedDocuments = sortDocuments(tableSortState, documents);
    const sortByName = () => {
        if (tableSortState != sortType.nameAsc) {
            setTableSortState(sortType.nameAsc)
        }
        else {
            setTableSortState(sortType.nameDesc);
        }
    }

    const sortByDate = () => {
        if (tableSortState != sortType.dateAsc) {
            setTableSortState(sortType.dateAsc)
        }
        else {
            setTableSortState(sortType.dateDesc);
        }
    }

    const displayIndicator = (indicator: sortIndicator) => {
        switch (indicator) {
            case sortIndicator.up:
                return (<span><i className="material-symbols-outlined no-symbol-fill">arrow_drop_up</i></span>)
            case sortIndicator.down:
                return (<span><i className="material-symbols-outlined no-symbol-fill">arrow_drop_down</i></span>)
        }
        return (<></>);
    }
    const indicator = (column: sortColumn): sortIndicator =>
    {
        switch (tableSortState) {
            case sortType.dateAsc:
                return column == sortColumn.date ? sortIndicator.down : sortIndicator.none;
            case sortType.dateDesc:
                return column == sortColumn.date ? sortIndicator.up : sortIndicator.none;
            case sortType.nameAsc:
                return column == sortColumn.name ? sortIndicator.down : sortIndicator.none;
            case sortType.nameDesc:
                return column == sortColumn.name ? sortIndicator.up : sortIndicator.none;
        }
        return sortIndicator.none
    }

    return ( <div className={style.responseTable}>
        <table className={style.documentList}>
        <tbody>
            <tr className={style.tableHeader}>
                <th className={style.fileType} />
                <th className={style.sortableColumn + ' ' + style.name} onClick={() => sortByName()}>File name {displayIndicator(indicator(sortColumn.name))}</th>
                <th className={style.uploadedBy}>Uploaded by</th>
                <th className={style.sortableColumn + ' ' + style.date} onClick={() => sortByDate()}>Date {displayIndicator(indicator(sortColumn.date))}</th>
                <th className={style.fileSize}>File size</th>
                <th/>
                </tr>
                {orderedDocuments.length == 0 &&
                    <tr className={style.documentList}><td colSpan={5} className={style.documentsNone}>No documents or folders</td></tr>
                }
            {orderedDocuments.map((d, i) =>
                <DocumentCard key={i}
                    document={d}
                    updateDocuments={updateDocuments}
                    setIsDeleteModalVisible={setIsDeleteModalVisible}
                    setFileToDelete={setFileToDelete}
                    setFolder={setFolder}
                    setPreviewFile={setPreviewFile}
                    urlPathToRoot={urlPathToRoot }
                />)}
        </tbody>
    </table>
        </div>
    );
}
export default FoldersAndDocuments

