import React from 'react';
import { SurveyClient } from '../CustomerPortalApi';
import { useIndicatorContext } from "@Store/indicatorsProvider";
import { NoProjectAccessError } from '../util/NoProjectAccessError';
import { useNavigate } from 'react-router';
import { NoProjectAccessQueryParam } from '../utils';
import { ProjectNotFoundError } from '../util/ProjectNotFoundError';

// Call this if you want errors to be caught by the React error boundary
const useAsyncError = () => {
    const [_, setError] = React.useState();
    const navigate = useNavigate();

    return React.useCallback(
        e => {
            if (e?.typeDiscriminator === NoProjectAccessError.typeDiscriminator || e?.typeDiscriminator === ProjectNotFoundError.typeDiscriminator) {
                navigate(`/?${NoProjectAccessQueryParam}=true`, { replace: true });
            } else {
                setError(() => {
                    throw e;
                });
            }
        },
        [setError],
    );
};

// Data loader with spinny and optional trigger to force reloads
export const useDataLoader = <T extends unknown>(
        loader: (c: SurveyClient) => Promise<T>, defaultValue?: T, setter?: (data: T) => void, trigger?: number): T => {
    const { dispatchIndicator } = useIndicatorContext();
    const throwError = useAsyncError();

    const [result, setResult] = React.useState(defaultValue ?? null);

    React.useEffect(() => {

        dispatchIndicator({ type: "LOADING_START" });

        loader(new SurveyClient())

            .then(r => {
                setter ? setter(r) : setResult(r);
                dispatchIndicator({ type: 'LOADING_END' });
            })

            .catch(e => {
                const errorResult = JSON.parse(e.response);

                dispatchIndicator({ type: 'LOADING_END' });
                throwError(errorResult)
            });

    }, [trigger]);

    return result;
};