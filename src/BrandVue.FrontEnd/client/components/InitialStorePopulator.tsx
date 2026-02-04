import React from 'react';
import { useDispatch } from 'react-redux';
import { fetchVariableConfiguration } from 'client/state/variableConfigurationsSlice';
import { fetchSubsetConfigurations } from 'client/state/subsetSlice';
import { fetchAllAverages } from 'client/state/averageSlice';
import { reloadAllReports } from 'client/state/reportSlice';

const InitialStorePopulator: React.FC = () => {
    const dispatch = useDispatch();

    React.useEffect(() => {
        dispatch(fetchVariableConfiguration());
        dispatch(fetchSubsetConfigurations());
        dispatch(fetchAllAverages());
        dispatch(reloadAllReports({ forceLoad: true }));
    }, [dispatch]);

    return null;
};

export default InitialStorePopulator;