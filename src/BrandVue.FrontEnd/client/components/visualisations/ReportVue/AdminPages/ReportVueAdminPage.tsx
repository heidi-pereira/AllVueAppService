import React from "react";
import { useDropzone } from 'react-dropzone';
import { toast } from 'react-hot-toast';
import { Modal, ModalFooter, ModalHeader, ModalBody, Button } from "reactstrap";
import * as BrandVueApi from "../../../../BrandVueApi";
import { FileInformation } from "../../../../BrandVueApi";
import style from "./ReportVueAdminPage.module.less";
import PreviewFilePage from "./Preview/PreviewFilePage";
import AddFolderModal from "./Folders/AddFolderModal";
import FoldersAndDocuments from "./Folders/FoldersAndDocuments";
import docStyle from "./Folders/FoldersAndDocuments.module.less";
import ButtonThrobber from "../../../throbber/ButtonThrobber";
import Throbber from "../../../throbber/Throbber";
import { handleErrorWithCustomText} from "client/components/helpers/SurveyVueUtils";

const ReportVueAdminPage = () => {
    const rootPath = ".\\";
    const [dataFetchId, setDataFetchId] = React.useState(1);
    const [fileName, setFileName] = React.useState("");
    const [path, setPath] = React.useState(rootPath);
    const [documents, setDocuments] = React.useState<FileInformation[]>();
    const [previewFile, setPreviewFile] = React.useState("");
    const [isDeleteModalVisible, setIsDeleteModalVisible] = React.useState(false);
    const [isCreateFolderModalVisible, setIsCreateFolderModalVisible] = React.useState(false);
    const [fileToDelete, setFileToDelete] = React.useState<FileInformation>();
    const [isPublishing, setIsPublishing] = React.useState<boolean>(false);
    const [displayPublishing, setDisplayPublishing] = React.useState<boolean>(false);
    const [fileToPublish, setFileToPublish] = React.useState<string>("");
    const [publishTitle, setPublishTitle] = React.useState<string>("");
    const [isGettingData, setIsGettingData] = React.useState(false);
    const [isUploadingData, setIsUploadingData] = React.useState(false);
    const [activeCurrentRelease, setActiveCurrentRelease] = React.useState<BrandVueApi.ProjectReleaseDetails | null | undefined>(null);
    const [currentBuildParameters, setCurrentBuildParameters] = React.useState<BrandVueApi.DashboardBuildParameters | undefined>(undefined);

    const acceptedFileTypes = ".png,.txt,.json,.svg,.zip,.dashboard";
    const defaultReportName = "report.json";

    React.useEffect(() => {
        const client = BrandVueApi.Factory.ReportVueClient(error => {
            toastError(`Failed to get files for '${path}'`);
            setPath(rootPath);
            setIsGettingData(false);
        });
        setIsGettingData(true);
        client.getFiles(path).then(documentsForPath => {
            setIsGettingData(false);
            setDocuments(documentsForPath);
        });
    }, [path, dataFetchId]);

    const toastError = (userFriendlyText: string) => {
        toast.error(userFriendlyText, { duration: 10000 });
    };
    const toastSuccess = (userFriendlyText: string): string => {
        const toastId = toast.success(userFriendlyText);
        return toastId;
    };
    const toastDismiss = (toastId: string | undefined) => {
        toast.dismiss(toastId);
    };
    const toastProgress = (userFriendlyText: string): string => {
        return toast.loading(userFriendlyText);
    };

    const updateDocuments = () => {
        //force state change to refresh
        setDataFetchId(dataFetchId * -1);
    }

    const setFolder = (folder: string) => {
        if (folder == "..") {
            const parts = path.split("\\");
            if (parts.length == 1) {
                folder = rootPath;
            }
            else {
                parts.splice(parts.length - 1);
                folder = parts.join("\\");
            }
        }
        setPath(folder);
    }

    const uploadFile = async (file: File) => {
        if (!file) {
            toastError(`This type of file can't be uploaded. Please use a supported file type (${acceptedFileTypes})`);
        } else {
            const toastId = toastProgress('Uploading file...');
            const formData = new FormData();
            formData.append('file', file);
            setFileName(file.name);
            setIsUploadingData(true);
            try {
                const response = await fetch(`${(window as any).appBasePath}/api/reportVue/uploaddocument/?path=${path}`,
                    {
                        credentials: "same-origin",
                        method: 'POST',
                        body: formData,
                        headers: {
                            "Accept": "application/json"
                        }
                    });
                toastDismiss(toastId);

                if (!response.ok) {
                    const result = await response.json();
                    toastError(result.detail);
                } else {
                    toastSuccess("File uploaded successfully");
                    updateDocuments();
                }
                setIsUploadingData(false);
            }
            catch (error) {
                toastDismiss(toastId);
                const errorText = `Failed to upload file ${fileName}.`;
                handleErrorWithCustomText(error, errorText);
                setIsUploadingData(false);
            }
        }
    };


    const createFolder = async (folderToCreate: string) => {
        const fullName = path.endsWith("\\") ? path + folderToCreate : path + "\\" + folderToCreate;
        const toastId = toastProgress(`Creating folder '${fullName}'...`);

        const client = BrandVueApi.Factory.ReportVueClient(error => error());
        client.createFolder(fullName).then(
            () => {
                toastDismiss(toastId);
                setIsCreateFolderModalVisible(false);
                toastSuccess(`Created folder '${fullName}'`);
                updateDocuments();
            }
        ).catch(() => {
            toastDismiss(toastId);
            setIsCreateFolderModalVisible(false);
            toastError("Failed");
        });
    };

    const deleteFileOrFolder = async (name: string, isFolder: boolean) => {
        const objectToDelete = isFolder ? "folder" : "file";
        const toastId = toastProgress(`Deleting ${objectToDelete} '${name}''...`);

        const client = BrandVueApi.Factory.ReportVueClient(error => error());
        client.deleteDocument(name).then(
            () => {
                toastDismiss(toastId);
                setIsDeleteModalVisible(false);
                toastSuccess(`${objectToDelete} '${name}'' deleted`);
                updateDocuments();
            }
        ).catch(() => {
            toastDismiss(toastId);
            setIsDeleteModalVisible(false);
            toastError("Failed");
        });
    };

    function DeleteModal() {
        if (fileToDelete) {
            return (<Modal isOpen={isDeleteModalVisible} toggle={() => setIsDeleteModalVisible(!isDeleteModalVisible)} centered={true} className={`modal-dialog-centered content-modal add-folder-modal  ${docStyle.deleteDocumentModal}`}>
                <ModalHeader>
                    <div className="settings-modal-header">
                        <div className="close-icon">
                            <button type="button" className="btn btn-close" onClick={() => setIsDeleteModalVisible(!isDeleteModalVisible)}>
                              
                            </button>
                        </div>
                        <div className="set-name">Delete {fileToDelete?.isFolder ? " folder" : "document"}</div>
                    </div>
                </ModalHeader>
                <ModalBody >
                    <div>Are you sure you want to delete <strong className={style.fileName}>{fileToDelete?.name}?</strong></div>

                </ModalBody>
                <ModalFooter>
                    <Button className="secondary-button" onClick={() => setIsDeleteModalVisible(!isDeleteModalVisible)}>Cancel</Button>
                    <Button className="primary-button destructive" onClick={() => deleteFileOrFolder(fileToDelete.name, fileToDelete.isFolder)} >Delete</Button>

                </ModalFooter>
            </Modal>
            );
        }
        return <></>
    }

    function publish(fileToPublish: string, warning: string|null) {
        const client = BrandVueApi.Factory.ReportVueClient(error => error());
        client.publishDocument(fileToPublish, warning == null ? "Standard release procedure": warning).then(
            () => {
                setPreviewFile("");
                setIsPublishing(false);
                setPath(rootPath);
                toastSuccess(`Successfully published '${publishTitle}' (${fileToPublish})`);
            }
        ).catch(() => {
            setPreviewFile("");
            setIsPublishing(false);
            setPath(rootPath);
            toastError(`Failed to publish' '${publishTitle}' (${fileToPublish})`);
        });
    }

    function publishFile(title: string, myFileToPublish: string) {
        setPublishTitle(title);
        setIsPublishing(true);
        setDisplayPublishing(true);
        setFileToPublish(myFileToPublish);
        setCurrentBuildParameters(undefined);
        const client = BrandVueApi.Factory.ReportVueClient(error => error());
        client.publishDocumentStats(myFileToPublish).then(
            (result) => {
                const release = result?.projects.find(x => x.isActive);
                setActiveCurrentRelease(release);
                setCurrentBuildParameters(result.buildParameters);
                if (release) {
                    setPublishTitle(release.project.name);
                }
                else {
                    setDisplayPublishing(false);
                    publish(myFileToPublish, null);
                }
            }).catch(() => {
                setDisplayPublishing(false);
                publish(myFileToPublish, null);
            });

    }

    function BreadCrumbs() {
        let paths = path == ".\\" ? [] : path.split("\\");
        return (<div className={style.paths}>
            <span onClick={() => setFolder(".\\")} className={style.pathText}>Root directory</span>
            {paths.map((path, index) => {
                var pathBuilt = paths.slice(0, index + 1).join("\\");
                return <span key={ `breadCrumbs${index}`}>
                            <span className={style.pathSeperator}>/</span>
                            <span title={pathBuilt} onClick={() => setFolder(pathBuilt)} className={style.pathText}>{path}</span>
                        </span>
            })}
        </div>);
    }

    function TitleBar() {
        const jsonFile = documents?.find(x => x.displayName == defaultReportName)
        const folderName = jsonFile?.name.substr(0, jsonFile?.name.length - defaultReportName.length - 1) ?? "";
        return (<div className={style.titleBar}>
            {BreadCrumbs()}
            {jsonFile &&
                <>
                    <div className={style.action}>
                        <button className="hollow-button" onClick={() => setPreviewFile(jsonFile.name)} >
                            Preview
                        </button>
                    </div>
                    <div className={style.action}>
                        <button className="primary-button" onClick={() => publishFile(folderName, jsonFile.name)} >
                            Publish
                        </button>
                    </div>
                </>
            }
        </div>
        );
    }

    function Dropzone() {
        const onDrop = React.useCallback(acceptedFiles => {
            uploadFile(acceptedFiles[0]);
        },
            []);
        const { getRootProps, getInputProps, isDragActive } =
            useDropzone({ onDrop, accept: acceptedFileTypes, multiple: false });

        return (
            <div className={`${style.dropZone} ${isDragActive ? style.active : ""}`}>
                <div {...getRootProps()} className={style.flexContainer}>
                    <input {...getInputProps()} />

                    <button className={style.button + " hollow-button"} onClick={(e) => { e.stopPropagation(); setIsCreateFolderModalVisible(true); }} >
                        <i className="material-symbols-outlined no-symbol-fill ">create_new_folder</i>
                        <span>New Folder</span>
                    </button>
                    <button className={style.button + " hollow-button"} disabled={isUploadingData}>
                        <i className="material-symbols-outlined no-symbol-fill ">file_upload</i>
                        {!isUploadingData &&
                            <span>Upload document</span>
                        }
                        {isUploadingData &&
                            <>
                                <span>Uploading</span>
                                <ButtonThrobber />
                            </>
                        }
                    </button>
                    <div className={style.dropFileText}>or drop a file here</div>
                </div>
            </div>
        )
    }

    function compareVersionNumbers(version1: string, version2: string): number {
        if (version1 == version2) {
            return 0;
        }
        const splitVersion1 = version1.split('.').map(Number);
        const splitVersion2 = version2.split('.').map(Number);

        const maxLength = Math.max(splitVersion1.length, splitVersion2.length);

        for (let i = 0; i < maxLength; i++) {
            const num1 = splitVersion1[i] || 0;
            const num2 = splitVersion2[i] || 0;

            if (num1 > num2) {
                return 1;
            } else if (num1 < num2) {
                return -1;
            }
        }

        return 0;
    }


    if (displayPublishing) {
        if (!currentBuildParameters ) {
            return (<div className={style.publishContainer}><Throbber/></div>)
        }
        const olderVersionOfDesktopTools = compareVersionNumbers(currentBuildParameters?.desktopToolsVersion ?? "", activeCurrentRelease?.dashboardBuildParameters.desktopToolsVersion ?? "") < 0;
        const warningMessage = olderVersionOfDesktopTools ? "Warning old desktop tools" : null;
        return (<div className={style.publishContainer}>
            <div className={style.publishGroup}>
                <div className={style.publishTitle}>Publishing {publishTitle}</div>

                {olderVersionOfDesktopTools &&
                    <div className={style.publishError}>
                        <div> <i className="material-symbols-outlined no-symbol-fill ">warning</i> !!!WARNING!!!!</div>
                        <div>Old version of Desktop tools used to generate this dashboard. Consider upgrading your desktop tools and regenerating the dashboard</div>
                    </div>
                }
                <div className={style.publishNotes}>
                    <div>Dashboard to publish</div>
                    <div>Desktop tools Version: {currentBuildParameters?.desktopToolsVersion ?? ""}</div>
                </div>
                <div className={style.publishItem}>This will overwrite the existing dashboard with the same name.</div>
                <div className={style.publishHelpItem}>To enter a new dashboard title instead, go to ReportVue after referring to the <a href="https://docs.savanta.com/internal/Content/Report_Builder/Setting_the_Report_Paths.html" target="_blank">help</a>.</div>
                <div className={style.publishNotes}>
                    <div>Current Dashboard</div>
                    <div>Released by: {activeCurrentRelease?.userName ?? ""}</div>
                    <div>Release Date: {activeCurrentRelease?.releaseDate.toLocaleDateString() ?? ""} {activeCurrentRelease?.releaseDate.toLocaleTimeString() ?? ""}</div>
                    <div>Version: {activeCurrentRelease?.versionOfRelease ?? ""}</div>
                    <div>Process: {activeCurrentRelease?.reasonForRelease ?? ""}</div>
                    <div>Desktop tools Version: {activeCurrentRelease?.dashboardBuildParameters.desktopToolsVersion ?? ""}</div>
                </div>

                <div className={style.publishItem}>Are you sure you wish to proceed?</div>

                <div className={style.publishButtons}>
                    <button className={olderVersionOfDesktopTools ? "hollow-button": "primary-button"} onClick={(e) => { e.stopPropagation(); setDisplayPublishing(false); publish(fileToPublish, warningMessage); }} >
                    <i className="material-symbols-outlined no-symbol-fill ">publish</i>
                    <span>Yes</span>
                </button>
                    <button className={style.button +  ((!olderVersionOfDesktopTools) ? " hollow-button": " primary-button") } onClick={(e) => { e.stopPropagation(); setDisplayPublishing(false); setIsPublishing(false) }} >
                    <span>No</span>
                    </button>
                </div>

            </div>
        </div>);
        }

    if (isPublishing) {
        return (<div className={style.publishContainer}>
            <div className={style.publishGroup}>
                <div className={style.publishItem}>Publishing {publishTitle }</div>
                <div className={style.publishItem + ' custom-spinner'}></div>
            </div>
        </div>);
    }
    if (previewFile && previewFile.length) {
        return (<PreviewFilePage
            fileToPreview={previewFile}
            cancelPreview={() => setPreviewFile("")}
            publishCurrentFile={(title) => publishFile(title, previewFile)}
        />);
    }
    return (<div className={style.surveyDocuments}>

        <DeleteModal />
        <AddFolderModal isOpen={isCreateFolderModalVisible} closeModal={() => { setIsCreateFolderModalVisible(false) }} path={path} createFolder={(folder: string) =>
        { createFolder(folder) }} />
        <div className={style.header}>
            <TitleBar />
        </div>
        <FoldersAndDocuments
            documents={documents ? documents : []}
            updateDocuments={updateDocuments}
            setIsDeleteModalVisible={setIsDeleteModalVisible}
            setFileToDelete={setFileToDelete}
            setFolder={setFolder}
            setPreviewFile={setPreviewFile}
            isGettingData={isGettingData}
        />

        <div className={style.footer}>
            <Dropzone />
        </div>
    </div>);
}

export default ReportVueAdminPage;
