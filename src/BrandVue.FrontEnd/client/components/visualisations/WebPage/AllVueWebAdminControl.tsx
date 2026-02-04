import React from "react";
import { ProductConfiguration } from "../../../ProductConfiguration";
import { Factory, IApplicationUser, WebFileFileInformation } from "../../../BrandVueApi";
import { useDropzone } from 'react-dropzone';
import style from "./AllVueWebAdminControl.module.less"
import MultiPageModal from "../../MultiPageModal";
import ModalPage from "../../ModalPage";
import { toast } from 'react-hot-toast';
import { handleError, handleErrorWithCustomText } from 'client/components/helpers/SurveyVueUtils';
import ButtonThrobber from "../../throbber/ButtonThrobber";

interface IAllVueWebAdminControl {
    productConfiguration: ProductConfiguration;
    name: string;
    user: IApplicationUser | null;
    documents: WebFileFileInformation[]|undefined;
    masterHtml: WebFileFileInformation|undefined;
    htmlLoaded: string;
    path: string;
    onFileReload: () => void;
}

const AllVueWebAdminControl = (props: IAllVueWebAdminControl) => {
    const [isGettingData, setIsGettingData] = React.useState(false);
    const [isOpen, setIsOpen] = React.useState(false);
    const [isUploadingData, setIsUploadingData] = React.useState(false);
    const [fileName, setFileName] = React.useState("");
    const [myDocuments, setMyDocuments] = React.useState<WebFileFileInformation[] | undefined>()

    const acceptedFileTypes = ".png,.txt,.svg,.html";

    React.useEffect(() => {
        const client = Factory.AllVueWebPageClient(error => {
            setIsGettingData(false);
        });
        setIsGettingData(true);
        setIsGettingData(false);
        if (props.documents) {
            const copy = props.documents.map(obj => obj);
            setMyDocuments(copy)
        }
    }, [props.name]);

    const reloadFiles = () => {
        const client = Factory.AllVueWebPageClient(error => {
            
        });
        client.getFiles(props.name).then(documentsForPath => {
            setMyDocuments(documentsForPath);
            setIsUploadingData(false);
        });
    }

    const closeFileUpload = () => {
        setIsOpen(false);
        props.onFileReload();
    };

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
                const response = await fetch(`${(window as any).appBasePath}/api/allVueWebController/UploadFile/?path=${props.path}`,
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
                    setIsUploadingData(false);
                } else {
                    toastSuccess("File uploaded successfully");
                    reloadFiles();
                }
            }
            catch (error) {
                toastDismiss(toastId);
                const errorText = `Failed to upload file ${fileName}`;
                handleErrorWithCustomText(error, errorText);
                setIsUploadingData(false);
            }
        }
    };

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

    const getFormattedFileSize = (size: number | undefined) => {
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

    const getConvertedDate = (passedDate: Date | undefined) => {
        if (passedDate != null) {
            const date = new Date(passedDate);
            return (
                <>
                    {date.toLocaleDateString()}
                </>
            );
        }
        return null;
    };

    const onFileToDelete = (fileToDelete: WebFileFileInformation) => {

        const client = Factory.AllVueWebPageClient(error => {

        });
        client.deleteFile(fileToDelete.name).then(_ => {
            reloadFiles();
        }).catch(error => {
            handleError(error);
        });
    }

    const getPageContent = () => {
        if (isGettingData) {
            return <></>
        }
        return (<div className={style.adminContainer}>
            <div className={style.adminLeft}>

                <div className={style.adminControl}>
                    <div className={style.circularNavButton} onClick={() => setIsOpen(true)}><i className="material-symbols-outlined">star</i></div>
                </div>
            </div>
            <MultiPageModal
                isOpen={isOpen}
                setIsOpen={setIsOpen}
                header="Upload files"
            >
                <ModalPage
                    actionButtonCss="hollow-button"
                    actionButtonText="OK"
                    actionButtonHandler={closeFileUpload}
                    cancelButtonCss={style.feedbackModalCancel}
                    cancelButtonHandler={closeFileUpload}
                >
                    <table className={style.fileTable }>
                        <thead>
                            <tr className={style.tableHeader}><th></th><th></th><th>Name</th><th>Size</th><th>Modified</th></tr>
                        </thead>
                        <tbody>
                            {(myDocuments == undefined || myDocuments.length == 0) &&
                                <tr className={style.tableRow}>
                                    <td colSpan={5}>No files found</td>
                                </tr>
                            }
                            {myDocuments?.map((x, index) => {
                                return (<tr key={index} className={style.tableRow}>
                                    <td className={style.tdDelete} onClick={() => onFileToDelete(x)}><i className="material-symbols-outlined delete-icon">delete</i></td>

                                    <td>{x.url == props.masterHtml?.url &&
                                        <i title="Key page to load" className="material-symbols-outlined">star</i>
                                    }
                                    </td>
                                    <td className={style.name}><a href={x.url} target="_blank">{x.name}</a></td>
                                    <td className={style.size}>{getFormattedFileSize(x.size)}</td>
                                    <td>{getConvertedDate(x.lastModified)}</td>
                                </tr>);
                            })}
                        </tbody>
                    </table>
                    <div className={style.footer}>
                        <Dropzone />
                    </div>

                </ModalPage>
                </MultiPageModal>
            <div dangerouslySetInnerHTML={{ __html: props.htmlLoaded }} />
        </div>)
    }
    return (getPageContent());
}

export default AllVueWebAdminControl;
