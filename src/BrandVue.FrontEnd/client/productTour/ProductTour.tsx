import React from 'react';
import Joyride, { CallBackProps } from 'react-joyride';
import { Step } from 'react-joyride';
import QueryString from 'query-string';
import { DataSubsetManager } from "../DataSubsetManager";
import { ProductTourDefinitions } from './ProductTourDefinitions';
import JoyRideStyles from './JoyRideStyles';
import {useReadVueQueryParams, useWriteVueQueryParams} from '../components/helpers/UrlHelper';
import { PageHandler } from '../components/PageHandler';
import { IGoogleTagManager } from '../googleTagManager';
import { useNavigate, useLocation } from 'react-router-dom';
import {useCallback, useEffect, useState} from "react";
export interface INavStep extends Step {
    url: string
};

interface IProductTourProps {
    productName: string;
    pageHandler: PageHandler;
    googleTagManager: IGoogleTagManager;
}

export default function ProductTour(props: IProductTourProps) {
    const navigate = useNavigate();
    const location = useLocation();
    const { setQueryParameter, setQueryParameters } = useWriteVueQueryParams(navigate, location);
    const { getQueryParameter } = useReadVueQueryParams();
    
    const [state, setState] = useState({
        run: false,
        stepIndex: 0,
        steps: [] as INavStep[]
    });

    const updateUrl = useCallback((url: string) => {
        navigate(url);
    }, [navigate]);

    const finishAndResetTour = useCallback(() => {
        props.googleTagManager.addEvent("finishedTour", props.pageHandler);
        setState(prev => ({ ...prev, run: false }));
        updateUrl("/getting-started");
    }, [props.googleTagManager, props.pageHandler, updateUrl]);

    const ensureTakeOtherTour = useCallback((steps: INavStep[]) => {
        const lastStep = (steps[steps.length - 1].content as JSX.Element);

        if (lastStep.props.children &&
            lastStep.props.children[lastStep.props.children.length - 1].props.className !== "takeAnotherTour") {

            steps[steps.length - 1].content =
                <React.Fragment>
                    {steps[steps.length - 1].content}
                    <div className="takeAnotherTour">
                        <button className="btn btn-tour" onClick={finishAndResetTour} id="productTourCompleteButton">Take another tour</button>
                    </div>
                </React.Fragment>;
        }
    }, [finishAndResetTour]);

    const checkForTourStart = useCallback(() => {
        var tour = getQueryParameter<string>("Tour");

        if (tour) {
            if (props.productName.toLowerCase() === "finance") {
                //This is needed because the tour uses Brand Consideration which is only available in "All" subset of finance
                //For other subsets, Product Consideration will now be used instead
                //If the tours are changed in the future to use a metric which is present in every subset this can be removed
                //https://app.clubhouse.io/mig-global/story/36846/update-product-tour-so-it-makes-sense-for-finance
                if (DataSubsetManager.selectedSubset.id.toLowerCase() === "all") {
                    tour += "-All";
                } else {
                    tour += "-Sub";
                }
            }

            setQueryParameter("Tour", undefined);
            props.pageHandler.clearFilterState(location, setQueryParameters);
            const steps = ProductTourDefinitions.steps[tour];

            steps.forEach(s => s["disableBeacon"] = true);

            ensureTakeOtherTour(steps);

            setState({ stepIndex: 0, steps: steps, run: true });
        }
    }, [props.productName, props.pageHandler, ensureTakeOtherTour]);

    const callback = useCallback((data: CallBackProps) => {
        const { action, index, type } = data;
        const newIndex = index + (action === "prev" ? -1 : action === "next" ? 1 : 0);
        var navItem = state.steps[newIndex];
        if (action === "close") {
            setState(prev => ({ ...prev, run: false }));
            return;
        }
        if (type === "tour:status" && action === "start") {
            updateUrl(navItem.url);
            return;
        }
        if (type === "step:after") {
            props.googleTagManager.addEvent("nextStepTour", props.pageHandler, { value: `${newIndex}` });
        }
        if (type === "step:after" && state.stepIndex === index) {
            setState(prev => ({ ...prev, stepIndex: newIndex }));
            if (navItem) {
                updateUrl(navItem.url);
            }
        }
    }, [state.steps, state.stepIndex, props.googleTagManager, props.pageHandler, updateUrl]);
    useEffect(() => {
        checkForTourStart();
    }, [checkForTourStart]);
    useEffect(() => {
        checkForTourStart();
        const currentPathname = location.pathname;
        const currentSearch = location.search;
        const currentQueryParameters = QueryString.parse(currentSearch);
        const sortedUrls = state.steps.map(s => s.url).sort((a, b) => b.length - a.length);
        const navItemUrl = sortedUrls.find((navUrl) => {
            const navUrlQuery = QueryString.parse(QueryString.extract(navUrl));
            return currentPathname === navUrl.split('?')[0] &&
                Object.keys(navUrlQuery).every(k => navUrlQuery[k] === currentQueryParameters[k]);
        });

        console.debug(currentPathname, currentSearch, sortedUrls, navItemUrl);

        if (navItemUrl) {
            const navItemIndex = state.steps.findIndex(s => s.url === navItemUrl);

            if (state.stepIndex !== navItemIndex) {
                setState(prev => ({ ...prev, stepIndex: navItemIndex }));
            }
        }
    }, [location, state.steps, state.stepIndex, checkForTourStart]);
    const { steps, run, stepIndex } = state;
    return (
        <>
            {steps.length > 0 && <Joyride
                disableOverlay={true}
                steps={steps}
                run={run}
                callback={callback}
                hideBackButton={true}
                stepIndex={stepIndex}
                styles={JoyRideStyles.styles}
            />}
        </>
    );
}