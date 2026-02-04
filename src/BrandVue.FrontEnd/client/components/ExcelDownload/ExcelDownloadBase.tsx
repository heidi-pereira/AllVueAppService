import React from "react";
import ExcelDownloadButton from "../buttons/ExcelDownloadButton";
import { UserContext } from "../../GlobalContext";
import * as Constants from "../../Constants";
import { IGoogleTagManager } from "../../googleTagManager";
import { PageHandler } from "../PageHandler";

export interface IExcelBaseDownloadProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
}

export const ExcelDownloadBase: React.FC<IExcelBaseDownloadProps & { loading?: boolean; onClick: () => void }> = (props) => {
    return (
        <UserContext.Consumer>
            {(user) => {
                const isExportForbidden = user?.isTrialUser ?? true;
                const toolTipMessage = isExportForbidden ? Constants.dataExportForbiddenMessage : undefined;
                return (
                    <ExcelDownloadButton
                        onClick={props.onClick}
                        loading={props.loading || false}
                        disabled={isExportForbidden}
                        tooltipContent={toolTipMessage}
                    />
                );
            }}
        </UserContext.Consumer>
    );
};