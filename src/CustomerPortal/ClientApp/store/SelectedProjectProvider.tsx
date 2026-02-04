import React from 'react';
import { IProject } from '../CustomerPortalApi';

interface IState {
    selectedProject: IProject
}

type Action = { type: 'SET_SELECTEDSEARCH', data: IProject }

const defaultState: IState = { selectedProject: null };

interface contextState {
    state: IState,
    dispatch: React.Dispatch<Action>
}

const Context = React.createContext<contextState>({ state: defaultState, dispatch: (): void => { } });

export const useSelectedProjectContext = () => React.useContext(Context);

export const SelectedProjectProvider = ({ children }: any) => {

    const reducer = (state: IState, action: Action): IState => {
        switch (action.type) {
            case 'SET_SELECTEDSEARCH':
                return { ...state, selectedProject: action.data };
        }

        return state;
    };

    const [state, dispatch] = React.useReducer(reducer, defaultState);

    return (
        <Context.Provider value={{ state, dispatch }}>
            {children}
        </Context.Provider>
    );
};