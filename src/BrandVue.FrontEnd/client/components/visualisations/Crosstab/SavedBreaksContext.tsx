import * as BrandVueApi from "../../../BrandVueApi";
import { useEffect, useState, useContext, createContext } from "react";
import { CrossMeasure, SavedBreakCombination } from '../../../BrandVueApi';
import toast from 'react-hot-toast';
import { useMetricStateContext } from '../../../metrics/MetricStateContext';
import { MixPanel } from "client/components/mixpanel/MixPanel";
import { useSelectedBreaks } from "../../../state/entitySelectionHooks";
import { useAppDispatch } from "client/state/store";
import { getActiveBreaksFromSelection, setBreaksAndPeriod } from "../../helpers/AudienceHelper";
import { useWriteVueQueryParams } from "client/components/helpers/UrlHelper";
import { PageHandler } from "../../PageHandler";
import { useLocation, useNavigate } from "react-router-dom";

export type SavedBreaksAction =
    { type: 'SAVE_BREAKS', data: { name: string, isShared: boolean, breaks: CrossMeasure[], isSavedFromCrosstab: boolean }} |
    { type: 'UPDATE_SAVED_BREAKS', data: { savedBreaksId: number, name: string, isShared: boolean, isUpdatedFromCrosstab: boolean } } |
    { type: 'DELETE_SAVE_BREAKS', data: { savedBreaksId: number } };

interface SavedBreaksContextState {
    savedBreaks: SavedBreakCombination[];
    savedBreaksDispatch: (action: SavedBreaksAction) => Promise<void>;
}

const SavedBreaksContext = createContext<SavedBreaksContextState>({
    savedBreaks: [],
    savedBreaksDispatch: () => Promise.resolve()
});

export const useSavedBreaksContext = () => useContext(SavedBreaksContext);

interface IProps {
    children: any;
    pageHandler: PageHandler;
}

export const SavedBreaksProvider = (props: IProps) => {
    const {questionTypeLookup} = useMetricStateContext();
    const savedBreaksClient = BrandVueApi.Factory.SavedBreaksClient(error => error());
    const [savedBreaks, setSavedBreaks] = useState<SavedBreakCombination[]>([]);
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const audience = useSelectedBreaks(); 
    const dispatch = useAppDispatch();
    useEffect(() => reloadSavedBreaks(true), []);

    const toastError = (error, action?: string) => {
        const response = error.response;

        if (response) {
            const json = JSON.parse(response);
            if (json.message) {
                const resultMessage: string = json.message;
                if (!resultMessage.startsWith('Exception of type')) {
                    toast.error(resultMessage);
                    return;
                }
            }
            if (json.error && json.error.message) {
                const resultMessage: string = json.error.message;
                if (!resultMessage.startsWith('Exception of type')) {
                    toast.error(resultMessage);
                    return;
                }
            }
        }

        toast.error(`Something went wrong${action ? ` trying to ${action}` : ''}`);
    }

    const setSelectedBreaks = (id: number | undefined, savedBreaks: SavedBreakCombination[]) => {
        const breaks = getActiveBreaksFromSelection(savedBreaks.find(b => b.id === id), undefined, undefined, questionTypeLookup);
        setBreaksAndPeriod(breaks, setQueryParameter, props.pageHandler.session.activeView.curatedFilters, props.pageHandler, dispatch);
    }

    const reloadSavedBreaks = (respectSavedBreakUrl: boolean, selectedIdOverride?: number) => {
        savedBreaksClient.getSavedBreaks()
            .then(result => {
                setSavedBreaks(result.savedBreaks);
                let selectedSavedBreaksId: number | undefined = undefined;
                if(respectSavedBreakUrl) {
                    selectedSavedBreaksId = audience?.audienceId;
                    setSelectedBreaks(selectedSavedBreaksId, result.savedBreaks);
                }
                if (selectedIdOverride) {
                    setSelectedBreaks(selectedIdOverride, result.savedBreaks);
                }
            })
            .catch(error => toastError(error, "load saved breaks"));
    }

    const onSaveBreaksSuccess = (name: string, savedBreaksId: number, isSavedFromCrosstab: boolean) => {
        toast.success(`Saved ${name}`);
        isSavedFromCrosstab ? reloadSavedBreaks(isSavedFromCrosstab, savedBreaksId) : reloadSavedBreaks(isSavedFromCrosstab);
    }

    const onUpdateSavedBreaksSuccess = (name: string, isSavedFromCrosstab: boolean) => {
        toast.success(`Updated ${name}`);
        reloadSavedBreaks(isSavedFromCrosstab);
    }

    const onRemoveSavedBreaksSuccess = (savedBreaksId: number) => {
        const breaks = savedBreaks.find(b => b.id == savedBreaksId);
        toast.success(`Deleted ${breaks ? breaks.name : 'saved breaks'}`);
        const selectedSavedBreaksId = audience?.audienceId;
        if (selectedSavedBreaksId == savedBreaksId) {
            setSelectedBreaks(undefined, savedBreaks);
        }
        reloadSavedBreaks(true);
    }

    const asyncDispatch = async (action: SavedBreaksAction) => {
        switch (action.type) {
            case "SAVE_BREAKS":
                MixPanel.track("saveCrosstabBreak");
                return savedBreaksClient.saveBreaks(action.data.name,
                        action.data.isShared,
                        action.data.breaks)
                    .then((id) => onSaveBreaksSuccess(action.data.name, id, action.data.isSavedFromCrosstab))
                    .catch(error => toastError(error, "save breaks"));
            case "UPDATE_SAVED_BREAKS":
                MixPanel.track("updateCrosstabBreak");
                return savedBreaksClient.updateSaveBreaks(action.data.savedBreaksId,
                        action.data.name,
                        action.data.isShared)
                    .then(() => onUpdateSavedBreaksSuccess(action.data.name, action.data.isUpdatedFromCrosstab))
                    .catch(error => toastError(error, "update save breaks"));
            case "DELETE_SAVE_BREAKS":
                MixPanel.track("deleteCrosstabBreak");
                return savedBreaksClient.removeSavedBreaks(action.data.savedBreaksId)
                    .then(() => onRemoveSavedBreaksSuccess(action.data.savedBreaksId))
                    .catch(error => toastError(error, "delete saved breaks"));
            default:
                throw new Error(`Unsupported action type: ${action}`);
        }
    }

    return (
        <SavedBreaksContext.Provider value={{
            savedBreaks: savedBreaks,
            savedBreaksDispatch: asyncDispatch
        }}>
            {props.children}
        </SavedBreaksContext.Provider>
    );
}