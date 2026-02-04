import React from 'react';
import Loader from "@Components/shared/Loader";
import { useIndicatorContext } from "@Store/indicatorsProvider";

const Indicators = ()=> {

    const { state, dispatchIndicator } = useIndicatorContext();

    const closeError = () => {
        dispatchIndicator({ type: "ERRORS_CLEAR" })
        throw "fdsdf";
    }

    const ErrorDisplay = (props: { errors?: string[] }) => (
        <div>{props.errors && props.errors.map((e, i) => (
            <div key={i} onClick={closeError}>
                {e.toString()}
            </div>))}
        </div>);

    return (<React.Fragment>
        <ErrorDisplay errors={state.errors} />
        <Loader show={state.loading} />
    </React.Fragment>);
}


export default Indicators;
