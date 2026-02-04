import React from "react";
import {IApplicationUser, MainQuestionType} from "../../../../../BrandVueApi";
import * as BrandVueApi from "../../../../../BrandVueApi";
import { IGoogleTagManager } from "../../../../../googleTagManager";
import {PageHandler} from "../../../../PageHandler";
import { useMetricStateContext } from "../../../../../metrics/MetricStateContext";

interface IVariableContext {
    user: IApplicationUser | null;
    nonMapFileSurveys: BrandVueApi.SurveyRecord[];
    questionTypeLookup: {[key: string]: MainQuestionType};
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    shouldSetQueryParamOnCreate?: boolean;
    isSurveyGroup: boolean;
    selectedPart?: number;
}

interface IVariableProviderProps {
    user: IApplicationUser | null;
    nonMapFileSurveys: BrandVueApi.SurveyRecord[];
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    shouldSetQueryParamOnCreate?: boolean;
    isSurveyGroup: boolean;
    children: React.ReactNode;
}

export const VariableProvider = (props: IVariableProviderProps) => {
    const { questionTypeLookup } = useMetricStateContext();

    return (
        <VariableContext.Provider
            value={{
                user: props.user,
                nonMapFileSurveys: props.nonMapFileSurveys,
                questionTypeLookup: questionTypeLookup,
                googleTagManager: props.googleTagManager,
                pageHandler: props.pageHandler,
                shouldSetQueryParamOnCreate: props.shouldSetQueryParamOnCreate,
                isSurveyGroup: props.isSurveyGroup,
            }}
        >
            {props.children}
        </VariableContext.Provider>
    );
}

/**
 * Use the variable slice for variables, this just has random bits and pieces that aren't in redux yet.
 */
export const VariableContext = React.createContext<IVariableContext>({} as IVariableContext);