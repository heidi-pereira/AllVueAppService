import { createSelector } from "reselect";
import { RootState } from "./store";
import { ITimeSelectionOptions } from "./ITimeSelectionOptions";
import { throwIfNullish } from "../components/helpers/ThrowHelper";

export const selectTimeSelection = createSelector(
    [(state: RootState) => state.average.allAverages, (state: RootState) => state.timeSelection.scorecardPeriod],
    (allAverages, scorecardPeriod): ITimeSelectionOptions => ({
        scorecardAverage: throwIfNullish(allAverages.find((a) => a.averageId === scorecardPeriod) ??
            allAverages.find(a => a.isDefault) ??
            allAverages[0]),
    })
);

