import React from 'react';
import style from "./WeightingValidationModal.module.less"
import { Modal, ModalHeader, ModalBody } from 'reactstrap';
import { InvalidResponseWeight, InvalidResponseReason, ExtraResponseReason, ExtraResponseWeight, Factory, ValidationStatistics } from '../../../../../BrandVueApi';
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from '../Controls/MaterialSymbol';
import { saveFile } from '../../../../../helpers/FileOperations';
import { maxWeightAboveWarning, minWeightBelowWarning } from '../WeightingHelper';

interface IWeightingValidationModalProps {
    isOpen: boolean;
    onClose: () => void;
    toggle: () => void;
    onImportWeights: () => void;
    validationResults: ValidationStatistics | undefined;
    isImporting: boolean;
}

const WeightingValidationModal = (props: IWeightingValidationModalProps) => {
    const downloadErrors = () => {
        const client = Factory.WeightingFileClient(error => error());
        client.downloadErrors(props.validationResults!)
            .then(file => saveFile(file, file.fileName ?? 'template.xlsx'));
    }

    const errors = props.validationResults?.errorResponsesForThisSurveyAndWave;
    const warnings = props.validationResults?.extraResponsesInExcel.filter(x => x.reason != ExtraResponseReason.ID_WeightTooLarge && x.reason != ExtraResponseReason.ID_WeightTooSmall);
    const totalIssues = (errors?.length ?? 0) + (warnings?.length ?? 0);
    const showMaxWeightWarning = maxWeightAboveWarning(props.validationResults);
    const showMinWeightWarning = minWeightBelowWarning(props.validationResults);

    function groupByReason(items: ExtraResponseWeight[]): Record<ExtraResponseReason, ExtraResponseWeight[]> {
        return items.reduce((groups, item) => {
          const group = groups[item.reason] || [];
          group.push(item);
          groups[item.reason] = group;
          return groups;
        }, {} as Record<ExtraResponseReason, ExtraResponseWeight[]>);
    }

    const getResponseMismatchedErrorDescription = (mismatchedItem: ExtraResponseWeight): string => {
        switch (mismatchedItem.reason) {
            case ExtraResponseReason.ID_Archived:
                return "ID archived"
            case ExtraResponseReason.ID_FoundInAlternativeWave:
                return "ID found in a different wave"
            case ExtraResponseReason.ID_NonExistent:
                return "ID does not exist"
            case ExtraResponseReason.ID_NotFoundInSurvey:
                return "ID not found in this survey"
            case ExtraResponseReason.ID_WeightTooSmall:
                return "Weight is too small";
            case ExtraResponseReason.ID_WeightTooLarge:
                return "Weight is too large";
        }

        return '';
    }

    function groupByInvalidReason(items: InvalidResponseWeight[]): Record<InvalidResponseReason, InvalidResponseWeight[]> {
        return items.reduce((groups, item) => {
            const group = groups[item.reason] || [];
            group.push(item);
            groups[item.reason] = group;
            return groups;
        }, {} as Record<InvalidResponseReason, InvalidResponseWeight[]>);
    }

    const getResponseErrorDescription = (mismatchedItem: InvalidResponseWeight): string => {
        switch (mismatchedItem.reason) {
            case InvalidResponseReason.ID_InvalidWeight:
                return "Invalid weight";

            case InvalidResponseReason.ID_ExtraResponsesInDatabaseForThisSurveyAndWave:
                return "Missing from import";

        }

        return '';
    }

    const displayErrors = () => {
        if (!errors || errors.length == 0) {
            return;
        }
        const groupedErrors = groupByInvalidReason(errors);
        return (
            <>
                <div className={style.validationMessagesContainer}>
                    <div className={style.redMaterialIcon}>
                        <MaterialSymbol symbolType={MaterialSymbolType.error} symbolStyle={MaterialSymbolStyle.outlined} noFill />
                    </div>
                    <div className={style.validationHeader}>
                        {errors?.length} Errors
                    </div>
                </div>
                <div className={style.warningMessage}>
                    Amend incorrect response IDs
                </div>
                <div className={style.weightingsMismatchTableContainer}>
                    <div className={style.tableScrollContainer}>
                        <table className={style.weightingsTable}>
                            <thead><tr><th>Errors</th><th>Count</th></tr></thead>
                            <tbody>
                                {Object.values(groupedErrors).map(g => {
                                    return (
                                        <tr key={ExtraResponseReason[g[0].reason]}>
                                            <td>{getResponseErrorDescription(g[0])}</td>
                                            <td>{g.length}</td>
                                        </tr>);
                                    })
                                }
                            </tbody>
                        </table>
                    </div>
                </div>
            </>
        )
    }

    const displayWarnings = () => {
        if(!warnings || warnings.length == 0) {
            return;
        }

        const groupedErrors = groupByReason(warnings!);
        return (
            <>
                <div className={style.validationMessagesContainer}>
                    <div className={style.warningMaterialIcon}>
                        <MaterialSymbol symbolType={MaterialSymbolType.warning} symbolStyle={MaterialSymbolStyle.outlined} />
                    </div>
                    <div className={style.validationHeader}>
                        {warnings?.length} Warnings
                    </div>
                </div>
                <div className={style.warningMessage}>
                    This will not affect your data but will not be applied to your overall weighting
                </div>
                <div className={style.weightingsMismatchTableContainer}>
                    <div className={style.tableScrollContainer}>
                        <table className={style.weightingsTable}>
                            <thead><tr><th>Warnings</th><th>Count</th></tr></thead>
                            <tbody>
                                {Object.values(groupedErrors).map(g => {
                                    return (
                                        <tr key={ExtraResponseReason[g[0].reason]}>
                                            <td>{getResponseMismatchedErrorDescription(g[0])}</td>
                                            <td>{g.length}</td>
                                        </tr>
                                    )
                                })}
                            </tbody>
                        </table>
                    </div>
                </div>
            </>
        )
    }

    const getWarningOrOkIcon = (showWarning: boolean) => {
        if (showWarning) {
            return (
                <div className={style.warningMaterialIcon}>
                    <MaterialSymbol symbolType={MaterialSymbolType.warning} symbolStyle={MaterialSymbolStyle.outlined} />
                </div>
            )
        }
        return (
            <div className={style.blueMaterialIcon}>
                <MaterialSymbol symbolType={MaterialSymbolType.check_circle} symbolStyle={MaterialSymbolStyle.outlined} noFill />
            </div>
        )
    }

    const getMinMaxWarningMessage = () => {
        if (showMaxWeightWarning && showMinWeightWarning) {
            return "Min weighting score is too low and Max weighting score is too high.";
        }

        if (showMaxWeightWarning) {
            return "Max weighting score is too high.";
        }

        if (showMinWeightWarning) {
            return "Min weighting score is too low."
        }
    }

    const getMinMaxWarningAndAdvisory = () => {
        return (
            <>
                <div>{getMinMaxWarningMessage()}</div>
                <div>Advisory Min. is 0.2 (a 20% weighting), and Advisory Max. is 5.0 (a 500% weighting).</div>
            </>
        )
    }

    const getMinMaxDetails = () => {

        const numberOfResponsesMatchedInExcelUpload = props.validationResults?.numberOfResponsesMatched;
        const numberOfResponsesInDatabaseForThisSurveyAndWave = props.validationResults?.numberOfResponsesInDatabaseForThisSurveyAndWave;
        const excelUploadMatchesAllRespondentsForSurveyAndWave = numberOfResponsesInDatabaseForThisSurveyAndWave == numberOfResponsesMatchedInExcelUpload;
        return (
            <div className={style.validationContent}>
                <div className={style.validationHeader}>
                    {excelUploadMatchesAllRespondentsForSurveyAndWave &&
                        <div className={style.blueMaterialIcon}>
                            <MaterialSymbol symbolType={MaterialSymbolType.check_circle} symbolStyle={MaterialSymbolStyle.outlined} noFill={true} />
                        </div>
                    }
                    {!excelUploadMatchesAllRespondentsForSurveyAndWave &&
                        <div className={style.redMaterialIcon}>
                            <MaterialSymbol symbolType={MaterialSymbolType.error} symbolStyle={MaterialSymbolStyle.outlined} noFill={true} />
                        </div>
                    }
                    <div className={style.message}>
                        <b>{`${numberOfResponsesMatchedInExcelUpload}/${numberOfResponsesInDatabaseForThisSurveyAndWave}`}</b> response IDs were matched successfully
                    </div>
                </div>
                <div className={style.validationBody}>
                    <div className={style.minMax}>
                        {getWarningOrOkIcon(showMinWeightWarning)}
                        <b>Min: {props.validationResults?.minWeight}</b>
                    </div>
                    <div className={style.minMax}>
                        {getWarningOrOkIcon(showMaxWeightWarning)}
                        <b>Max: {props.validationResults?.maxWeight}</b>
                    </div>
                </div>
                <div className={style.advisory}>
                    {getMinMaxWarningAndAdvisory()}
                </div>
            </div>
        )
    }

    const getHeaderText = () => {
        if(totalIssues == 0) {
            return "Response IDs matched";
        }
        return `${ totalIssues } Flagged Response ID${ totalIssues > 1 ? "s" : "" }`;
    }

    const getContent = () => {
        if (totalIssues > 0) {
            return (
                <>
                    {displayErrors()}
                    {displayWarnings()}
                    {getMinMaxDetails()}
                </>
            )
        };

        return getMinMaxDetails();
    }

    const getModalButtons = () => {
        if (totalIssues == 0) {
            return (
                <>
                    <button className={`modal-button secondary-button ${style.autoWidth}`} onClick={props.toggle}>
                        <div>Cancel</div>
                    </button>
                    <button className={`modal-button primary-button ${style.autoWidth}`} onClick={props.onImportWeights}>Apply</button>
                </>
            )
        }

        return (
            <>
                <button className={`modal-button hollow-button ${style.autoWidth}`} onClick={downloadErrors}>
                    <i className="material-symbols-outlined">file_download</i>
                    {(!errors || errors?.length == 0) &&
                        <div>Download IDs to view warnings</div>
                    }
                    {(errors && errors.length > 0) &&
                        <div>Download IDs with actions</div>
                    }
                </button>
                {
                    (!errors || errors?.length == 0) &&
                    <button className={`modal-button primary-button ${style.autoWidth}`} onClick={props.onImportWeights}>Apply weights</button>
                }
            </>
        )
    }

    return (
        <Modal isOpen={props.isOpen} modalTransition={{ timeout: 50 }} toggle={props.toggle} className="variable-content-modal modal-dialog-centered content-modal settings-create">
            <ModalHeader style={{ width: "100%" }}>
                <div className="settings-modal-header">
                    <div className="close-icon">
                        <button type="button" className="btn btn-close" onClick={props.onClose}>
                        </button>
                    </div>
                    <div className="set-name">{getHeaderText()}</div>
                </div>
            </ModalHeader>
            <ModalBody>
                <div>
                    {getContent()}
                </div>
                <div className="modal-buttons">
                    {getModalButtons()}
                </div>
            </ModalBody>
        </Modal>
    )
}

export default WeightingValidationModal;