import React, { useCallback } from 'react';
import 'react-responsive-modal/styles.css'
import '@Styles/documents.scss';
import { SurveyClient, SurveyDetails, SurveyDocument, PermissionFeaturesOptions } from '../CustomerPortalApi';
import { useDropzone } from 'react-dropzone';
import { FeatureGuard } from '../components/FeatureGuard/FeatureGuard';
import { Slide, ToastHandler } from "@Styles/ToastHandler";
import 'react-toastify/dist/ReactToastify.css';
import { ToastContainer } from 'react-toastify';
import { Modal } from 'react-responsive-modal';
import '@Styles/deleteDocumentModal.scss';
import DocumentCard from './DocumentCard';

import {useParams} from "react-router";
import { useDataLoader } from '../store/dataLoading';
import { GoogleTagManager } from '../util/googleTagManager';
import { useProductConfigurationContext } from '../store/ProductConfigurationContext';
import Loader from '../components/shared/Loader';
import { useSelectedProjectContext } from '../store/SelectedProjectProvider';

const inFlightDocumentsFetches: Map<string, Promise<SurveyDocument[]>> = new Map();

const makeFetchKey = (surveyId: number, folderPath: string[]) => `${surveyId}::${folderPath.join("\\")}`;

enum sortType{
    dateAsc,
    dateDesc,
    nameAsc,
    nameDesc
}

const sortDocuments = (tableSortState: sortType, documents: SurveyDocument[]) => {
    switch (tableSortState) {
        case sortType.dateAsc:
            return documents?.sort((a,b)=> b.lastModified.valueOf() - a.lastModified.valueOf());

        case sortType.dateDesc:
            return documents?.sort((a,b)=> b.lastModified.valueOf() - a.lastModified.valueOf()).reverse();

        case sortType.nameAsc:
            return documents?.sort((a, b) => a.name.localeCompare(b.name));

        case sortType.nameDesc:
            return documents?.sort((a, b) => a.name.localeCompare(b.name)).reverse();
    }
};

interface IBreadCrumbsProps {
    currentFolder: string[],
    setCurrentFolder: (subFolder: string[]) => void;
}

const BreadCrumbs: React.FunctionComponent<IBreadCrumbsProps> = ({ currentFolder, setCurrentFolder }) => {

    return (<div className="breadCrumbs">
        <div className="breadCrumb" key={"breadCrumb"} onClick={() => setCurrentFolder([]) }>Home</div>
        <div className="pathSeperator" key={"root"}>/</div>
        {currentFolder.map((folder, key) => {
            return <div className="pathGroup" key={key}>
                <div className="breadCrumb" onClick={() => setCurrentFolder(currentFolder.slice(0, key+1))}>{folder}</div>
                <div className="pathSeperator">/</div>
                </div>
        })}
    </div>)
}

interface IGetDocumentsProps {
    surveyDetails: SurveyDetails,
    documents: SurveyDocument[],
    googleTagManager: GoogleTagManager;
    setIsDeleteModalVisible: (boolean) => void,
    setFileToDelete: (SurveyDocument) => void,
    isLoadingDocuments: boolean,
    setCurrentFolder: (subFolder: string[]) => void,
    currentFolder : string[],
}
const GetDocuments: React.FunctionComponent<IGetDocumentsProps> = (
    {
        surveyDetails,
        documents,
        googleTagManager,
        setIsDeleteModalVisible,
        setFileToDelete,
        isLoadingDocuments,
        setCurrentFolder,
        currentFolder
    }) => {
    const [tableSortState, setTableSortState] = React.useState<sortType>(sortType.dateAsc);
    const orderedDocuments = sortDocuments(tableSortState, documents);

    if (isLoadingDocuments) {
        return (<div className="document-list"><Loader show={true}/></div>);
    }
    if ((orderedDocuments?.length === 0 || orderedDocuments == null) && currentFolder.length == 0) {
        return (<div className="document-list"><div className="documents-none">No documents</div></div>);
    }

    const sortByName = () => {
        if(tableSortState != sortType.nameAsc){
            setTableSortState(sortType.nameAsc)
        }
        else{
            setTableSortState(sortType.nameDesc);
        }
    }

    const sortByDate = () => {
        if(tableSortState != sortType.dateAsc){
            setTableSortState(sortType.dateAsc)
        }
        else{
            setTableSortState(sortType.dateDesc);
        }
    }

    return (
        <table className="document-list">
            <tbody>
                <tr className="table-header">
                    <th className="sortable-column name-header" onClick={() => sortByName()}>Name</th>
                    <th className="uploaded-by">Uploaded by</th>
                    <th className="sortable-column date" onClick={()=>sortByDate()}>Date</th>
                    <th className="file-size">File size</th>
                    <th />
                </tr>
                {orderedDocuments.map(d =>
                    <DocumentCard key={d.id}
                        currentFolder={currentFolder}
                        surveyDetails={surveyDetails}
                        document={d}
                        googleTagManager={googleTagManager}
                        setIsDeleteModalVisible={setIsDeleteModalVisible}
                        setFileToDelete={setFileToDelete}
                        setCurrentFolder={setCurrentFolder}
                    />)}
            </tbody>
        </table>
    );
}

const SurveyDocumentsPage = (props: {googleTagManager: GoogleTagManager}) => {
    const params = useParams();
    const surveyId = parseInt(params.id);
    const surveyDetails = useDataLoader((c) => c.getSurveyDetails(surveyId));
    const [currentFolder, setCurrentFolder] = React.useState<string []>([]);
    const [documents, setDocuments] = React.useState<SurveyDocument[]>([]);
    const pathToEgnite = useDataLoader((c) => c.getPathToEgnite(surveyId));
    const { productConfiguration} = useProductConfigurationContext();

    const toastHandler = new ToastHandler();
    const [isDeleteModalVisible, setIsDeleteModalVisible] = React.useState(false);
    const [isLoadingDocuments, setIsLoadingDocuments] = React.useState(false);
    const [fileToDelete, setFileToDelete] = React.useState<SurveyDocument>();
    const [documentsRefreshKey, setDocumentsRefreshKey] = React.useState(0);
    const acceptedFileTypes = ".pdf, .txt, .xlsx, .xls, .ods, .csv, .docx, .doc, .odt, .pptx, .ppt, .odp, .sav";
    const { state } = useSelectedProjectContext();

    const getSurveyDocuments = () => {
        const fetchKey = makeFetchKey(surveyId, currentFolder);
        // If there's already an in-flight promise for this key, reuse it.
        const existing = inFlightDocumentsFetches.get(fetchKey);
        if (existing) return existing;
        setIsLoadingDocuments(true);

        const client = new SurveyClient();
        const promise = client.getSurveyDocuments(surveyId, currentFolder.join("\\"))
            .catch(x => {
                toastHandler.showError(`Failed to get documents for survey ${x}`);
                throw x;
            }).finally(() => {
                inFlightDocumentsFetches.delete(fetchKey);
            });

        inFlightDocumentsFetches.set(fetchKey, promise);
        return promise;
    }

    React.useEffect(() => {
        let isCancelled = false;
        if (!isNaN(surveyId)) {
            getSurveyDocuments()
                .then(docs => {
                    if (!isCancelled){
                        setDocuments(docs);
                    }
                })
                .finally(() => {
                    if (!isCancelled) {
                        setIsLoadingDocuments(false);
                    }
                });
        }
        return () => { isCancelled = true; };
    }, [surveyId, currentFolder.length, documentsRefreshKey]);

    const triggerDocumentsReload = () => setDocumentsRefreshKey(oldKey => oldKey + 1);

    const uploadFile = async (file: File) => {
        if (!file) {
            toastHandler.showError(`This type of file can't be uploaded. Please use a supported file type (${acceptedFileTypes})`);
        } else {
            props.googleTagManager.addEvent("documentsUpload", surveyDetails?.organisationShortCode, surveyDetails?.subProductId);
            toastHandler.showProgress('Uploading file...');
            const formData = new FormData();
            formData.append('file', file);

            const response = await fetch(`${(window as any).appBasePath}/api/survey/uploadclientsurveydocument/?surveyId=${surveyId}`,
                {
                    credentials: "same-origin",
                    method: 'POST',
                    body: formData,
                    headers: {
                        "Accept": "application/json"
                    }
                });
            toastHandler.dismiss();

            if (!response.ok) {
                const result = await response.json();
                toastHandler.showError(result.detail);
            } else {
                toastHandler.showToast("File uploaded successfully");
                triggerDocumentsReload();
            }
        }
    };

    const deleteFile = async (fileName: string) => {
        props.googleTagManager.addEvent("documentsDelete", surveyDetails?.organisationShortCode, surveyDetails?.subProductId);
        toastHandler.showProgress('Deleting file...');

        const response = await fetch(`${(window as any).appBasePath}/api/survey/deleteclientsurveydocument/?surveyId=${surveyId}&name=${fileName}`,
            {
                credentials: "same-origin",
                method: 'POST',
                headers: {
                    "Accept": "application/json"
                }
            });
        toastHandler.dismiss();
        setIsDeleteModalVisible(false);

        if (!response.ok) {
            const result = await response.json();
            toastHandler.showError(result.detail);
        } else {
            toastHandler.showToast("File deleted");
            triggerDocumentsReload();
        }
    };

    function FileUploadControl() {
        const onDrop = useCallback(acceptedFiles => {
                uploadFile(acceptedFiles[0]);
            },
            []);
        const { getRootProps, getInputProps, isDragActive } =
            useDropzone({ onDrop, accept: acceptedFileTypes, multiple: false });

        const isCurrentFolderTheRoot = currentFolder.length == 0;

        const isAvailable = (canUserUploadViaWebSite() && isCurrentFolderTheRoot) || productConfiguration.user.isAuthorizedSavantaUser;
        if (!isAvailable) {
            return <></>
        }
        return (
            <div className="header">
                <div className="description">
                    <div className="text">Upload files you want to share with {surveyDetails?.name} via </div>
                    {!isCurrentFolderTheRoot && productConfiguration.user.isAuthorizedSavantaUser &&
                        <button className="hollow-button" onClick={openLinkToRelatedEnginiteFolder}>
                            <span>Open Egnyte</span>
                            <i className="material-symbols-outlined">open_in_new</i>
                        </button>
                    }
                </div>
                {isCurrentFolderTheRoot &&
                    <div className={`drop-zone ${isDragActive ? "active" : ""}`}>
                        <div {...getRootProps()} className="flex-container">
                            <div className="fileUpload">
                                <div>
                                    <input {...getInputProps()} />
                                    <button className="hollow-button">
                                        <i className="material-symbols-outlined">file_upload</i>
                                        <span>Upload document</span>
                                    </button>
                                </div>
                                <div className="drop-file-text">or drop a file here</div>
                            </div>

                            {productConfiguration.user.isAuthorizedSavantaUser &&
                                <div className="egnyte">
                                    <div className="text">or</div>
                                        <div>
                                        <button className="hollow-button" onClick={openLinkToRelatedEnginiteFolder}>
                                            <span>Open Egnyte</span>
                                            <i className="material-symbols-outlined">open_in_new</i>
                                         </button>
                                    </div>
                                </div>
                              }
                        </div>
                    </div>
                }
            </div>
        )
    }

    const openLinkToRelatedEnginiteFolder = () => {
        window.open(pathToEgnite +"/"+ currentFolder.join("/"), '_blank').focus();
    };

    const areThereSubFolders = () => {
        if (documents) {
            return documents.find(x => x.isFolder) != null;
        }
        return false;
    }

    const canUserUploadViaWebSite = () => {
        return state.selectedProject.allVueDocumentationConfiguration.isClientUploadingAllowed;
    }

    const canDisplayBreadCrumbs = () => {
        if (documents) {
            return currentFolder.length != 0 || areThereSubFolders();
        }
        return false;
    }

    return (
        <FeatureGuard permissions={[PermissionFeaturesOptions.DocumentsAccess]} fallback={<div className="survey-documents"><div className="documents-none">You do not have permission to view this page.</div></div>}>
            {documents &&
                <div className="survey-documents">
                    <FileUploadControl />

                    {canDisplayBreadCrumbs() &&
                        <BreadCrumbs currentFolder={currentFolder} setCurrentFolder={setCurrentFolder} />
                    }

                    <GetDocuments surveyDetails={surveyDetails}
                        documents={documents}
                        googleTagManager={props.googleTagManager}
                        setIsDeleteModalVisible={setIsDeleteModalVisible}
                        setFileToDelete={setFileToDelete}
                        isLoadingDocuments={isLoadingDocuments}
                        currentFolder={currentFolder}
                        setCurrentFolder={setCurrentFolder
                       }
                    />

                <div className="toast-container">
                    <ToastContainer hideProgressBar={true} toastClassName={'toast'} autoClose={2000} transition={Slide} />
                </div>
                <form>
                    <Modal open={isDeleteModalVisible}
                        onClose={() => setIsDeleteModalVisible(!isDeleteModalVisible)}
                        center
                        showCloseIcon={true}
                        closeOnOverlayClick={true}
                        classNames={{ overlay: 'custom-overlay', modal: 'deleteDocumentModal', closeButton: 'custom-close-button' }}>
                        <div className="title">Delete document?</div>
                        <div className="modal">Are you sure you want to delete <strong className="file-name">{fileToDelete?.name}?</strong></div>
                        <div className="responsive-button-wrapper">
                            <button onClick={() => setIsDeleteModalVisible(false)} className="secondary-button">Cancel</button>
                            <button onClick={() => deleteFile(fileToDelete.name)} className="primary-button destructive">Delete</button>
                        </div>
                    </Modal>
                </form>
            </div>
            }
        </FeatureGuard>
    );
}

export default SurveyDocumentsPage;