import { configureStore } from '@reduxjs/toolkit';
import { userManagementApi as apiSlice } from './rtk/apiSlice';
import { UserContext } from './orval/api/models/userContext'

const SET_USERCONTEXT = 'SET_USERCONTEXT';

export function setUserContext(user: UserContext) {
    return {
        type: SET_USERCONTEXT,
        payload: user,
    };
}
const initialState = {
    user: undefined,
};

function userDetailsReducer(state = initialState, action) {
    switch (action.type) {
    case SET_USERCONTEXT:
        return {
            ...state,
            user: action.payload,
        };
    default:
        return state;
    }
}


export const store = configureStore({
  reducer: {
    // Add the generated reducer to your store
        [apiSlice.reducerPath]: apiSlice.reducer,
        userDetailsReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(apiSlice.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
