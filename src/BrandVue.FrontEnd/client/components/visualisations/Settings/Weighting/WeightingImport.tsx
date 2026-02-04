import React from 'react';
import style from './WeightingImport.module.less';
import WeightingDropZone from './WeightingDropZone';
import { WeightingImportFile, WeightingStyle, Factory, ValidationStatistics, WeightingFilterInstance } from '../../../../BrandVueApi';
import WeightingValidationModal from './Modals/WeightingValidationModal';
import { saveFile } from '../../../../helpers/FileOperations';
import WeightingUploadErrorModal from './Modals/WeightingUploadErrorModal';
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from './Controls/MaterialSymbol';
import { Renderable } from 'react-hot-toast';
import { toast } from "react-hot-toast";
import { MetricSet } from '../../../../metrics/metricSet';
import { getWavesForSurveyOrTimeBasedVariable, reloadMetrics, minWeightBelowWarning, maxWeightAboveWarning, pendingRefreshSessionStorageKey, savedWeightingNameSessionStorageKey } from './WeightingHelper';
import { useAppSelector } from 'client/state/store';
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import {useNavigate} from "react-router-dom";
import { Tooltip } from "@mui/material";
import { Metric } from 'client/metrics/metric';
import MetricDropdownMenu from 'client/components/visualisations/Variables/MetricDropdownMenu';
import { DropdownToggle } from 'reactstrap';
import { separateMetricsByGenerationType } from 'client/metrics/metricHelper';
import { handleErrorWithCustomText } from 'client/components/helpers/SurveyVueUtils';

interface IWeightingImportProps {
    subsetId: string;
    waveVariableIdentifier: string;
    waveId: number | undefined;
    metrics: MetricSet;
}

export class UploadedFileDescription {
    FileName: string;
    FileSize: string;
    SegmentName: string;
    NumberOfRows: number;
}

const WeightingImport = (props: IWeightingImportProps) => {

    const defaultTitle = 'Import Response Weights';
    const [isValidationModalOpen, setIsValidationModalOpen] = React.useState<boolean>(false);
    const [isUploadErrorModalOpen, setIsUploadErrorModalOpen] = React.useState<boolean>(false);
    const [uploadErrorMessage, setUploadErrorMessage] = React.useState<string | undefined>(undefined);
    const [validationResults, setValidationResults] = React.useState<ValidationStatistics | undefined>(undefined);
    const [isRunning, setIsRunning] = React.useState<boolean>(false);
    const [isImporting, setIsImporting] = React.useState<boolean>(false);
    const [metricsSet, setMetricsSet] = React.useState<MetricSet>(new MetricSet({ metrics: [] }));
    const [headerText, setHeaderText] = React.useState<string>(props.waveVariableIdentifier ? '' : defaultTitle);
    const [waveName, setWaveName] = React.useState<string | undefined>(undefined);
    const [importWeightingOption, setImportWeightingOption] = React.useState<string>('optionUnweighted');
    const [weightMetricToImport, setWeightMetricToImport] = React.useState<Metric | undefined>();
    const navigate = useNavigate();
    const { variables, loading: isVariablesLoading } = useAppSelector(selectHydratedVariableConfiguration);

    const navigateToWeightingSettings = () => {
        navigate("weighting");
    }

    const importWeightsFromDatabase = (): void => {
        if (weightMetricToImport) {
            setIsImporting(true);
            const client = Factory.WeightingFileClient(error => {});
            const optional: number | undefined = importWeightingOption === 'optionUnweighted' ? undefined : (importWeightingOption === 'optionWeightedToOne' ? 1 : 0);
            client.importResponseLevelDataFromResponseData(props.subsetId, weightMetricToImport.varCode, optional).
                then(result => {
                    const weightingName = waveNameRef.current ?? props.waveVariableIdentifier ?? props.subsetId ?? "";
                    window.sessionStorage.setItem(pendingRefreshSessionStorageKey, "");
                    window.sessionStorage.setItem(savedWeightingNameSessionStorageKey, weightingName);
                    navigateToWeightingSettings();
                }).catch((e: any) => {
                    const text = `Failed to import weighted response data for ${weightingFile.subsetId}`;
                    handleErrorWithCustomText(e, text);
                }).finally(() => {
                    setIsImporting(false);
                });
        }
    }

    const createContextForWeightingFile = () => {
        var context = new WeightingFilterInstance();
        context.filterMetricName = props.waveVariableIdentifier;
        context.filterInstanceId = props.waveId;
        weightingFile.context.push(context);
    }

    const getHeader = () => {
        if (metricsSet.metrics.length > 0 && props.waveVariableIdentifier?.length) {
            const metric = metricsSet.metrics.find(m => m.name === props.waveVariableIdentifier);

            if (metric == undefined) {
                toastMessage(`Failed to get name for ${props.waveVariableIdentifier}`);
                setHeaderText(defaultTitle);
            }
            else {
                getWavesForSurveyOrTimeBasedVariable(props.waveVariableIdentifier, metricsSet, variables)
                    .then((variableWaves) => {
                        const instance = variableWaves.find(wave => wave.toEntityInstanceId == props.waveId);
                        if (instance) {
                            setWaveName(instance.toEntityInstanceName);
                            const header = `${instance.toEntityInstanceName} - ${metric.varCode}`;
                            setHeaderText(header);
                        }
                    }).catch((e: any) => {
                        toastMessage(`Failed to get wave name for ${props.waveVariableIdentifier}`);
                        throw e;
                    });
            }
        }
    }

    const weightingFile = new WeightingImportFile();
    weightingFile.subsetId = props.subsetId;
    weightingFile.weightingStyle = WeightingStyle.ResponseWeighting;
    if (props.waveVariableIdentifier && props.waveVariableIdentifier.length > 0) {
        createContextForWeightingFile();
    }
    
    const toastMessage = (userFriendlyText: Renderable) => {
        return toast.error(userFriendlyText);
    };

    const loadMetrics = async () => {
        if (props.subsetId) {
            const metricSet = await reloadMetrics(props.subsetId);
            const metricSetToUse = metricSet ?? props.metrics;
            setMetricsSet(metricSetToUse);
            setWeightMetricToImport(undefined);
        }
    }

    React.useEffect(() => {
        loadMetrics();
    }, [props.subsetId]);

    React.useEffect(() => {
            if (!isVariablesLoading) {
                getHeader();
            }
        },
        [metricsSet.metrics.length, isVariablesLoading, variables, props.waveVariableIdentifier]);

    const onUploadError = (errorMessage: string) => {
        setUploadErrorMessage(errorMessage);
        setIsUploadErrorModalOpen(true);
    }

    const uploadSuccessful = async (fileNameAndSize: { fileName: string, fileSize: number }) => {
        const client = Factory.WeightingFileClient(error => error());
        await client.basicFileInformation(weightingFile)
            .then(info => {
                const fileDescription = new UploadedFileDescription();
                fileDescription.FileName = fileNameAndSize.fileName;
                fileDescription.SegmentName = props.subsetId;
                fileDescription.NumberOfRows = info.numberOfRows;
                if (info.numberOfBytes < 1024) {
                    fileDescription.FileSize = `${info.numberOfBytes} Bytes`;
                }
                else {
                    let descriptionBytes = info.numberOfBytes / 1024;
                    if (descriptionBytes < 1024) {
                        fileDescription.FileSize = `${Math.floor(descriptionBytes)} KB`;
                    }
                    else {
                        descriptionBytes /= 1024;
                        fileDescription.FileSize = `${Math.floor(descriptionBytes)} MB`;
                    }
                }
            })
            .catch((e: any) => {
                toastMessage(`Failed to validate weighting data ${weightingFile.weightingStyle} for ${weightingFile.subsetId}`);
                throw e;
            });

        validateUploadedWeights();
    }

    const downloadTemplateFile = () => {
        const client = Factory.WeightingFileClient(error => error());
        client.generateTemplateFile(weightingFile).then(file => saveFile(file, file.fileName ?? 'template.xlsx'));
    }

    const validateUploadedWeights = () => {
        setIsRunning(true);
        const client = Factory.WeightingFileClient(error => error());
        client.validate(weightingFile).then(
            results => {
                processValidationResults(results);
            }).catch((e: any) => {
                toastMessage(`Failed to validate weighting data ${weightingFile.weightingStyle} for ${weightingFile.subsetId}`);
                throw e;
            }).finally(() => {
                setIsRunning(false);
            });
    }

    const processValidationResults = (results: ValidationStatistics): void => {
        setValidationResults(results);
        const weightingOutsideThreshold = minWeightBelowWarning(results) || maxWeightAboveWarning(results);
        const invalidResults = results.errorResponsesForThisSurveyAndWave.length > 0 || results.extraResponsesInExcel.length > 0 || !results.isValid;
        if (invalidResults || weightingOutsideThreshold) {
            setIsValidationModalOpen(true);
        }
        else {
            importWeights();
        }
    }

    const waveNameRef = React.useRef(waveName);
    React.useEffect(() => {
        waveNameRef.current = waveName;
    }, [waveName]);

    const importWeights = (): void => {
        setIsImporting(true);
        const client = Factory.WeightingFileClient(error => error());
        client.pushIntoDatabase(weightingFile).
            then(result => {
                const weightingName = waveNameRef.current ?? props.waveVariableIdentifier ?? props.subsetId ?? "";
                window.sessionStorage.setItem(pendingRefreshSessionStorageKey, "");
                window.sessionStorage.setItem(savedWeightingNameSessionStorageKey, weightingName);
                navigateToWeightingSettings();
            }).catch((e: any) => {
                toastMessage(`Failed to upload weighting data ${weightingFile.weightingStyle} for ${weightingFile.subsetId}`);
                throw e;
            }).finally(() => {
                setIsImporting(false);
            });
    }

    const [customMetrics, standardMetrics, autoGeneratedNumericMetrics] = separateMetricsByGenerationType(metricsSet.metrics);
    return (
        <>
            <div className={style.page}>
                <div className={style.header}>
                    <div className={style.title}>
                        <h3>{headerText}</h3>
                    </div>
                    <div className={style.controls}>
                        <button className="secondary-button" onClick={navigateToWeightingSettings}>Cancel</button>
                    </div>
                </div>
                <div className={style.stepsContainer}>
                    <div className={style.step}>
                        <div className={style.callToAction}>Direct import of response weights from data.</div>
                            <div>If you want to import response weights, first select how to set any invalid data:</div>
                            <div className={style.controls}>
                                <div className={style.radioGroup}>
                                    <label>
                                        <input type="radio"
                                            name="dataImportOption"
                                            value="optionUnweighted"
                                            checked={importWeightingOption === 'optionUnweighted'}
                                            onChange={() => setImportWeightingOption('optionUnweighted')}
                                        />
                                        Unweighted (the response will <b>not</b> be included in weighted totals)
                                    </label>
                                    <label>
                                        <input
                                            type="radio"
                                            name="dataImportOption"
                                            value="optionWeightedToZero"
                                            checked={importWeightingOption === 'optionWeightedToZero'}
                                            onChange={() => setImportWeightingOption('optionWeightedToZero')}
                                        />
                                        A weight of 0 (the response will be included in weighted totals)
                                    </label>
                                    <label>
                                        <input type="radio" name="dataImportOption"
                                               value="optionWeightedToOne"
                                               checked={importWeightingOption === 'optionWeightedToOne'}
                                               onChange={() => setImportWeightingOption('optionWeightedToOne')}
                                        />
                                        A weight of 1
                                    </label>
                                </div>
                                <div>Select which question contains the weights:</div>
                                <div className={style.radioGroup}>
                                    <MetricDropdownMenu
                                        toggleElement={
                                            <DropdownToggle className="metric-selector-toggle toggle-button">
                                                <div className="title">{weightMetricToImport?.displayName ?? "Select a question"}</div>
                                                <i className="material-symbols-outlined">arrow_drop_down</i>
                                            </DropdownToggle>
                                        }
                                        metrics={standardMetrics}
                                        selectMetric={setWeightMetricToImport}
                                        showCreateVariableButton={false}
                                        groupCustomVariables={false}
                                    />
                                </div>
                                <div className={style.callToAction}>Click the button below to start the import:</div>
                            </div>
                        <div className={style.controlsWithSpace} >
                            <Tooltip placement="top" title={`This is only available for Ad Hoc surveys and will work as expected if every response has a value. eg Triple S Importing. (Nb. Archived responses will also be imported in case they are unarchived later)`}>
                                    <button className="hollow-button not-exported" disabled={isImporting || !weightMetricToImport} onClick={importWeightsFromDatabase}>Import direct</button>
                             </Tooltip>
                            </div>
                        <div>Once the import has completed, download the weights to verify the import.</div>

                    </div>
                    <div className={style.alternateOption}>If you are not using the direct import, follow the instructions below to upload the weighting file.</div>

                    <div className={style.step}>
                        <h3>1. Excel Template - Response Weighting Guide</h3>
                        <div className={style.templateNote}>Download our template if you're new to AllVue or need help formatting your weighting file. If your file is already in the correct format, feel free to skip this step and upload directly.</div>
                        <div>
                            <button className="hollow-button not-exported" onClick={downloadTemplateFile }>
                                <MaterialSymbol symbolType={MaterialSymbolType.download} symbolStyle={MaterialSymbolStyle.outlined} />
                                <div>Download template</div>
                            </button>
                        </div>
                    </div>
                    <div className={style.step}>
                        <h3>2. Upload Excel file</h3>
                        <div>
                            <WeightingDropZone onSuccess={uploadSuccessful} 
                                onError={onUploadError} 
                                isUploadingData={isRunning}
                                setIsUploadingData={(r) => setIsRunning(r)}
                                weightingFile={weightingFile} />
                        </div>
                    </div>
                </div>
            </div>

            <WeightingValidationModal isOpen={isValidationModalOpen} 
                onClose={() => setIsValidationModalOpen(false)} 
                onImportWeights={() => importWeights()} 
                toggle={() => setIsValidationModalOpen((isOpen) => !isOpen)} 
                validationResults={validationResults}
                isImporting={isImporting}
            />

            <WeightingUploadErrorModal isOpen={isUploadErrorModalOpen}
                setIsOpen={setIsUploadErrorModalOpen}
                toggle={() => setIsUploadErrorModalOpen((isOpen) => !isOpen ) }
                errorMessage={uploadErrorMessage ?? ''}
            />
        </>
    );
}

export default WeightingImport;