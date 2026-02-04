import React from 'react';

interface IIndicators {
    loading: boolean,
    errors: string[]
}

export type Action = { type: 'LOADING_START' }
    | { type: 'LOADING_END' }
    | { type: 'ERRORS_CLEAR' }
    | { type: 'ERRORS_ADD', data: string };

interface contextState {
    state: IIndicators,
    dispatchIndicator: React.Dispatch<Action>
}

const initialState: IIndicators = { loading: false, errors: [] };

const IndicatorContext = React.createContext<contextState>({ state: initialState, dispatchIndicator: (): void => { } });

export const useIndicatorContext = () => React.useContext(IndicatorContext);

const reducer = (state: IIndicators, action: Action): IIndicators => {

    switch (action.type) {
        case "LOADING_START":
            return { ...state, loading: true };
        case "LOADING_END":
            return { ...state, loading: false };
        case "ERRORS_CLEAR":
            return { ...state, errors: [] };
        case "ERRORS_ADD":
            return { ...state, errors: [...state.errors, action.data] };
        default:
            const exhaustiveCheck: never = action;
    }

    return state;
};

export const IndicatorProvider = ({ children }: any) => {

    const [currentState, dispatch] = React.useReducer(reducer, initialState);

    return (
        <IndicatorContext.Provider value={{ state: currentState, dispatchIndicator: dispatch }}>
            {children}
        </IndicatorContext.Provider>
    );
};