import { DisplaySignificanceDifferences } from "../../../../BrandVueApi";

export const getNewShownSignificanceDifferences = (toggled: DisplaySignificanceDifferences,
    currentState: DisplaySignificanceDifferences) => {

    const stateMap = {
        [DisplaySignificanceDifferences.ShowBoth]: {
          [DisplaySignificanceDifferences.ShowUp]: DisplaySignificanceDifferences.ShowDown,
          [DisplaySignificanceDifferences.ShowDown]: DisplaySignificanceDifferences.ShowUp,
        },
        [DisplaySignificanceDifferences.ShowUp]: {
          [DisplaySignificanceDifferences.ShowUp]: DisplaySignificanceDifferences.None,
          [DisplaySignificanceDifferences.ShowDown]: DisplaySignificanceDifferences.ShowBoth,
        },
        [DisplaySignificanceDifferences.ShowDown]: {
          [DisplaySignificanceDifferences.ShowDown]: DisplaySignificanceDifferences.None,
          [DisplaySignificanceDifferences.ShowUp]: DisplaySignificanceDifferences.ShowBoth,
        },
        [DisplaySignificanceDifferences.None]: {
          [DisplaySignificanceDifferences.ShowUp]: DisplaySignificanceDifferences.ShowUp,
          [DisplaySignificanceDifferences.ShowDown]: DisplaySignificanceDifferences.ShowDown,
        },
    };

    const newState = stateMap[currentState]?.[toggled];

    if (newState === undefined) {
        throw new Error(`Unexpected toggled: ${toggled}, currentState: ${currentState}`);
    }

    return newState;
}