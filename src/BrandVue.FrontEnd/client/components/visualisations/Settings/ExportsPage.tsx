import React from "react";
import { IGoogleTagManager } from "../../../googleTagManager";
import { PageHandler } from "../../PageHandler";
import style from "./ExportsPage.module.less";
import { Factory } from "../../../BrandVueApi";
import { saveFile } from "../../../helpers/FileOperations";
import ButtonThrobber from "../../throbber/ButtonThrobber";

interface IExportsPageProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
}

const ExportsPage = (props: IExportsPageProps) => {
    const [isDownloading, setIsDownloading] = React.useState<boolean>(false);

    const DownloadResponses = () => {
        setIsDownloading(true);
        const client = Factory.DataClient(error => error());
        const excelSheetName = "export.xlsx";
        client.exportResponses().then(r => {
            saveFile(r, excelSheetName);
            setIsDownloading(false);
        });
    }

    return (
        <div className={style.exportsSettingsPage}>
            <div className={style.titleBar}>
                <div>
                    <h3>Response-Level Download</h3>
                </div>
            </div>
            <div>
                Click the button below to download the full response-level data for the survey.
                Depending on the size of the survey, the download may take a few minutes.
            </div>
            <div>
                <button className="primary-button" disabled={isDownloading} onClick={() => DownloadResponses()}>
                    {isDownloading ? <ButtonThrobber/> : < i className="material-symbols-outlined">download</i>}
                    Download
                </button>
                <div hidden={!isDownloading}>
                    Your data is being exported now.
                    When the export is complete, the download will start automatically.
                </div>
            </div>
        </div>
    );

}
export default ExportsPage;