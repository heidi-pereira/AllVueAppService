import React from "react";
import * as BrandVueApi from "../../../../BrandVueApi";
import {WeightingImportFile} from "../../../../BrandVueApi";
import style from "./WeightingDropZone.module.less"
import {useDropzone} from 'react-dropzone';
import ButtonThrobber from "../../../throbber/ButtonThrobber";

interface IWeightingDropZone {
    weightingFile: WeightingImportFile;
    isUploadingData: boolean;
    setIsUploadingData: (isUploading: boolean) => void;
    onSuccess?: (fileInfo: { fileName: string, fileSize: number }) => void;
    onError?: (error: string) => void;
}

const WeightingDropZone = (props: IWeightingDropZone) => {
    const [maxUploadSizeInMB, setmaxUploadSizeInMB] = React.useState<number>(10);
    const acceptedFileTypes = ".xlsx";

    const getMaxFileUpload = () => {
        {
            const client = BrandVueApi.Factory.WeightingFileClient(error => {
                error => error();
            });
            client.getMaximumUploadFileSizeInMB().then(size => {
                setmaxUploadSizeInMB(size);
            });
        }
    }

    React.useEffect(() => {
        getMaxFileUpload();
    }, []);

    const uploadFile = async (file: File, weightingImportFile: BrandVueApi.WeightingImportFile) => {
        if (!file) {
            props.onError?.(`This type of file can't be uploaded. Please use a supported file type (${acceptedFileTypes})`)
        } else {
            const formData = new FormData();
            formData.append('file', file);
            props.setIsUploadingData(true);
            try {
                const response = await fetch(`${(window as any).appBasePath}/api/ResponseWeightingFile/uploadfile/?importFileAsJson=${JSON.stringify(weightingImportFile)}`,
                    {
                        credentials: "same-origin",
                        method: 'POST',
                        body: formData,
                        headers: {
                            "Accept": "application/json"
                        }
                    });

                if (!response.ok) {
                    props.setIsUploadingData(false);
                    const result = await response.json();
                    props.onError?.(`Error uploading file: ${result.detail}`)
                } else {
                    props.onSuccess?.({ fileName: file.name, fileSize: file.size });
                }
            }
            catch (error) {
                props.setIsUploadingData(false);
                props.onError?.('Unexpected error uploading file.')
            }
        }
    };

    const onDrop = React.useCallback(acceptedFiles => {
        uploadFile(acceptedFiles[0], props.weightingFile);
    }, []);

    const { getRootProps, getInputProps, isDragActive } =
        useDropzone({ onDrop, accept: acceptedFileTypes, multiple: false }
        );

    return (
        <div className={`${style.dropZone} ${isDragActive ? style.active : ""}`}>
            <div {...getRootProps()} >
                <div className={style.flexContainer}>
                <input {...getInputProps()} />

                <button className={style.button + " hollow-button"} disabled={props.isUploadingData}>
                    <i className="material-symbols-outlined no-symbol-fill ">file_upload</i>
                    {props.isUploadingData ?
                        <>
                            <span>Uploading</span>
                            <ButtonThrobber />
                        </>
                        :
                        <span>Upload document</span>
                    }
                </button>
                    <div className={style.dropFileText}>or drop a file here</div>
                </div>
                <div className={style.dropFileMessage}>Supported file: XLS, XLSX. Max {maxUploadSizeInMB}MB</div>

            </div>
        </div>
    )
}

export default WeightingDropZone;