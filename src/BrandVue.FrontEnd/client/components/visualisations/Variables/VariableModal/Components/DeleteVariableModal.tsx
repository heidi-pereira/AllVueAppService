import { useCrosstabPageStateContext } from "../../../Crosstab/CrosstabPageStateContext";
import * as BrandVueApi from "../../../../../BrandVueApi";
import { IGoogleTagManager } from "../../../../../googleTagManager";
import DeleteModal from "../../../../DeleteModal";
import { PageHandler } from "../../../../PageHandler";
import { useContext } from "react";
import { useMetricStateContext } from "../../../../../metrics/MetricStateContext";
import { useEntityConfigurationStateContext } from "../../../../../entity/EntityConfigurationStateContext";
import { BaseVariableContext } from "../../BaseVariableContext";
import {QueryStringParamNames, useWriteVueQueryParams} from "../../../../../components/helpers/UrlHelper";
import {useLocation, useNavigate} from "react-router-dom";
import { fetchVariableConfiguration } from "client/state/variableConfigurationsSlice";
import { useAppDispatch } from "client/state/store";
import { handleError } from "client/components/helpers/SurveyVueUtils";

interface IProps {
    isOpen: boolean;
    variableId?: number;
    variableName: string
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    closeModal: (deleted: boolean) => void;
    isBase?: boolean
    closeAllModals: () => void;
}

const DeleteVariableModal = (props: IProps) => {
    const { crosstabPageDispatch } = useCrosstabPageStateContext();
    const dispatch = useAppDispatch();
    const { entityConfigurationDispatch } = useEntityConfigurationStateContext();
    const { crosstabPageMetrics, metricsDispatch } = useMetricStateContext();
    const { baseVariableDispatch: baseVariablesDispatch } = useContext(BaseVariableContext);
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const refreshPage = async () => {
        setQueryParameter(QueryStringParamNames.urlSafeMetricName, crosstabPageMetrics.filter(m => m.variableConfigurationId !== props.variableId)[0].urlSafeName);
        await metricsDispatch({type: "RELOAD_METRICS"});
        await dispatch(fetchVariableConfiguration()).unwrap();
        await entityConfigurationDispatch({type: "RELOAD_ENTITYCONFIGURATION"});
    }

    const refreshBases = async () => {
        crosstabPageDispatch({type: 'REMOVE_METRIC_BASE', data: {variableId: props.variableId!}});
        await baseVariablesDispatch({type: 'RELOAD_BASE_VARIABLES'});
        await metricsDispatch({type: "RELOAD_METRICS"});
        await dispatch(fetchVariableConfiguration()).unwrap();
        await entityConfigurationDispatch({type: "RELOAD_ENTITYCONFIGURATION"});
    }

    const deleteVariable = async () => {
        try {
            if(props.variableId) {
                if (props.isBase) {
                    props.googleTagManager.addEvent("baseVariablesDelete", props.pageHandler);
                    await BrandVueApi.Factory.VariableConfigurationClient((_, error) => handleError(error)).deleteBaseVariableById(props.variableId)
                    await refreshBases();
                } else {
                    props.googleTagManager.addEvent("variablesDelete", props.pageHandler);
                    await BrandVueApi.Factory.VariableConfigurationClient((_, error) => handleError(error)).deleteVariableById(props.variableId);  
                    await refreshPage();
                }
                props.closeAllModals();
            }
        }
        catch(error) {
            handleError(error);
        }
    }

    return (
        <DeleteModal
            isOpen={props.isOpen}
            thingToBeDeletedName={props.variableName}
            thingToBeDeletedType= {props.isBase ? "base" : "variable"}
            delete={deleteVariable}
            closeModal={() => props.closeModal(false)}
            affectAllUsers
            delayClick
        />
    );
}
export default DeleteVariableModal;