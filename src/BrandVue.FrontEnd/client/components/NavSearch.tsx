import React, {Fragment, useCallback, useState} from 'react';
import debounce from 'debounce';
import 'react-bootstrap-typeahead/css/Typeahead.css';
import {Highlighter, Menu, MenuItem, Typeahead} from "react-bootstrap-typeahead";
import {FeatureCode, LlmDiscoveryRequest, PageDescriptor} from "../BrandVueApi";
import { Location } from 'history';
import {NavigateFunction, useLocation, useNavigate} from 'react-router-dom';
import {getActiveViewUrl, getPageFromUrl, getUrlForPageName, pageListToUrl} from "./helpers/PagesHelper";
import { IGoogleTagManager } from '../googleTagManager';
import {PageHandler} from './PageHandler';
import {MetricSet} from '../metrics/metricSet';
import {MixPanel} from './mixpanel/MixPanel';
import {fetchLlmDiscoveryResult, LlmDiscoveryState} from '../state/llmDiscoverySlice';
import {useAppDispatch, useAppSelector} from "../state/store";
import { IQueryParams, useReadVueQueryParams, useWriteVueQueryParams } from "./helpers/UrlHelper";
import { isFeatureEnabled } from "./helpers/FeaturesHelper";
import { selectSubsetId } from 'client/state/subsetSlice';

type Option = string | Record<string, any>;

type ISearchItem = {
    topLevel: string;
    labelTitle?: string;
    label: string;
    url: string;
}

interface INavSearchProps {
    pages: PageDescriptor[];
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    enabledMetricSet: MetricSet;
}

const NavSearch = (props: INavSearchProps) => {
    const dispatch = useAppDispatch();
    const llmDiscovery = useAppSelector((state) => state.llmDiscovery) as LlmDiscoveryState;
    const isLlmDiscoveryEnabled = isFeatureEnabled(FeatureCode.Llm_discovery);
    const navigate = useNavigate();
    const location = useLocation();
    const { updateQueryParametersInQueryString } = useWriteVueQueryParams(navigate, location);
    const readVueQueryParams  = useReadVueQueryParams();
    const [loading, setLoading] = useState(false);
    const subsetId = useAppSelector(selectSubsetId);

    // Create a debounced version of the dispatch function
    const debouncedDispatch = useCallback(
        debounce((text: string) => {
            dispatch(fetchLlmDiscoveryResult({
                request: new LlmDiscoveryRequest({ userRequest: text, subsetId: subsetId })
            })).finally(() => setLoading(false));
        }, 500), // Wait 500ms after the user stops typing
        [] // Empty dependency array since we don't want to recreate this function
    );

    // Clean up the debounced function when component unmounts
    React.useEffect(() => {
        return () => {
            debouncedDispatch?.cancel?.();
        };
    }, []);

    const createSearchItem = (displayName: string, parentItem: PageDescriptor, current: PageDescriptor, stack: PageDescriptor[]): ISearchItem => {
        const transformed = {
            topLevel: displayName,
            label: ((parentItem.displayName !== displayName) ? parentItem.displayName + " - " : "") +
                current.displayName,
            url: pageListToUrl(stack)
        }
        return transformed;
    }
    
    const getItemsByLevel = (stack: PageDescriptor[]): ISearchItem[] => {
        const current = stack[stack.length - 1];
        return current.childPages.reduce((results, childPage) => {
            const copyOfStack = stack.slice();
            copyOfStack.push(childPage);

            if (childPage.panes && childPage.panes.length > 0) {
                results.push(createSearchItem(stack[0].displayName, current, childPage, copyOfStack));
            }
            return results.concat(getItemsByLevel(copyOfStack));
        }, ([] as ISearchItem[]));
    }
    
    const getItemsByTopLevel = (stack: PageDescriptor[]): ISearchItem[] => {
        const current = stack[stack.length - 1];
        if (current.panes && current.panes.length) {
            return getItemsByLevel(stack).concat(createSearchItem(stack[0].displayName, current, current, stack));
        }
        return getItemsByLevel(stack);
    }
    
    const items: ISearchItem[] = (props.pages.reduce((r, p) => r.concat(getItemsByTopLevel([p])), ([] as ISearchItem[])));
    const [selected, setSelected] = useState<ISearchItem[]>([]);
    const [llmSearchItems, setLlmSearchItems] = useState<ISearchItem[]>([]);

    React.useEffect(() => {
        const llmItems = (llmDiscovery.results || []).map((result): ISearchItem | undefined => {
            const url = getUrlForPageName(result.pageName, location, readVueQueryParams, { viewTypeNameOrUrl: result.partType });
            const pageFromUrl = getPageFromUrl(url);
            if (url !== "/" && pageFromUrl)
            {
                const queryParams = updateQueryParametersInQueryString(result.queryParams as IQueryParams, readVueQueryParams.getQueryParamsFromQueryString());
                const res = {
                    topLevel: pageFromUrl.pageTitle,
                    labelTitle: result.pageName,
                    label: result.messageToUser ?? '',
                    url: url + (queryParams == null ? "" : queryParams)
                };
                return res;
            }
            return undefined;
        }).filter((item): item is ISearchItem => item !== undefined);
        setLlmSearchItems(llmItems);
    }, [llmDiscovery.results]);

    const renderMenu = (results: ISearchItem[], menuProps: { text: string }) => {
        if (results.length === 0) {
            return (
                <Menu id="my-id" {...menuProps}>
                    {loading ?
                        <Menu.Header>
                            <div className="aiLabelTitleLoading" />
                            Aila is generating your results, please wait...
                        </Menu.Header> :
                        <Menu.Header>
                            <div>
                                {isLlmDiscoveryEnabled && <div className="aiLabelTitle" />}
                                <b>Your search did not match any documents</b>
                            </div>
                            <div>
                                Please check your spelling, try different keywords or explore related topics.
                            </div>
                        </Menu.Header>}
                </Menu>
            );
        }

        const groupBy = (xs, key) => xs.reduce((rv, x) => {
            (rv[x[key]] = rv[x[key]] || []).push(x);
            return rv;
        }, {});

        let idx = 0;
        const grouped = groupBy(results, "topLevel");

        const menuItems = Object.keys(grouped).map((topLevel) => {
            return [
                !!idx && <Menu.Divider key={`${topLevel}-divider`} />,
                <Menu.Header key={`${topLevel}-header`}>
                    {topLevel}
                </Menu.Header>,
                grouped[topLevel].map(page => {
                    const item =
                        <MenuItem key={idx} option={page} position={idx}>
                            {page.labelTitle &&
                                <div>
                                    <div className="aiLabelTitle" />
                                    <b>{page.labelTitle}</b>
                                </div>}
                            <Highlighter search={menuProps.text}>
                                {page.label}
                            </Highlighter>
                        </MenuItem>;
                    idx++;
                    return item;
                })
            ];
        });

        return <Menu id="my-id" {...menuProps}>{menuItems}</Menu>;
    }
    
    const handleChange = (selected: ISearchItem[], navigate: NavigateFunction, location: Location) => {
        if (selected.length) {
            setSelected([]);
            props.googleTagManager.addEvent("selectedSearchBox", props.pageHandler, { value: selected[0].label });
            MixPanel.track("searchBoxSelected");
            navigate(getActiveViewUrl(selected[0].url, location) + props.pageHandler.getPageQuery(selected[0].url, location.search, props.enabledMetricSet, readVueQueryParams));
        }
    }

    const handleInputChange = (text: string) => {
        // If text meets minimum length requirement and no matches found
        if (text.length >= 2) {
            const matches = items.filter(item =>
                item.label.toLowerCase().includes(text.toLowerCase())
            );

            if (matches.length === 0 && isLlmDiscoveryEnabled) {
                setLoading(true);
                debouncedDispatch(text);
            }
        }
        setLlmSearchItems([]);
    }

    return (
        <div className="navSearch">
            <Typeahead
                id="navSearchInputBox"
                selected={selected}
                positionFixed={true}
                align="left"
                renderMenu={(results, menuProps, state) => renderMenu(results as ISearchItem[], state ?? menuProps)}
                placeholder="Search for metrics"
                onChange={(selected) => handleChange(selected as ISearchItem[], navigate, location)}
                options={llmSearchItems.length > 0 ? llmSearchItems : items}
                minLength={2}
                className="navSearchInputBox"
                onInputChange={handleInputChange}
                filterBy={llmSearchItems.length > 0 ? () => true : ['label']}
            />
            <label htmlFor="navSearchInputBox" className="material-symbols-outlined search-icon">search</label>
        </div>
    );
}

export default NavSearch;