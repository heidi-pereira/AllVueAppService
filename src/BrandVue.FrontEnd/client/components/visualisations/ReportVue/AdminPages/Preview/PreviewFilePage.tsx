import React from "react";
import * as BrandVueApi from "../../../../../BrandVueApi";
import style from "./PreviewFilePage.module.less";
import PreviewReportVue from "./PreviewReportVue";


interface IPreviewFilePage {
    fileToPreview: string;
    cancelPreview: () => void;
    publishCurrentFile: (title: string) => void;
}

const PreviewFilePage = (props: IPreviewFilePage) => {

    const [url, setUrl] = React.useState("");
    const [isPreviewable, setIsPreviewable] = React.useState<boolean>(true);
    const [title, setTitle] = React.useState("");

    React.useEffect(() => {
        const client = BrandVueApi.Factory.ReportVueClient(error => error());
        client.urlToUnpublishedFile(props.fileToPreview).then(url => setUrl(url))
        setIsPreviewable(props.fileToPreview.toLowerCase().endsWith("json"));
    }, [props.fileToPreview]);

    if (isPreviewable) {
        return (
            <PreviewReportVue fileToPreview={props.fileToPreview}
                urlOfFileToPreview={url}
                publishCurrentFile={props.publishCurrentFile}
                setTitle={setTitle}
                cancelPreview={props.cancelPreview}
            />
        );
    }
    return (<div>No Preview available</div>)
}

export default PreviewFilePage;
