import React from 'react';
import { useState } from 'react';
import {Navigate, useLocation} from "react-router-dom";
import DropdownSelector from "./dropdown/DropdownSelector";
import { ViewType, ViewTypeEnum } from "./helpers/ViewTypeHelper";
import { IPageContext } from "./helpers/PagesHelper";
import { MixPanel } from './mixpanel/MixPanel';
import { To } from 'history';
interface IViewSelectorProps {
    pageContext: IPageContext;
}

const ViewSelector: React.FunctionComponent<IViewSelectorProps> = (props: IViewSelectorProps) => {

    if (props.pageContext.activeViews.length <= 1) return null;
    const selectedView = props.pageContext.viewMenuItem ?? props.pageContext.activeViews[0]
    const [redirect, setRedirect] = useState<To | undefined>();
    const location = useLocation();
    MixPanel.track("chartLoaded", { Part: props.pageContext.pagePart, ChartType: selectedView.name});

    const onSelectedItem = (view: ViewType): void => {
        MixPanel.track("chartTypeChanged", { Part: props.pageContext.pagePart, ChartType: selectedView.name });
        setRedirect({ pathname: props.pageContext.pagePart + view.url, search: location.search  })
    };

    return (
        <>
            <DropdownSelector<ViewType>
                label="Chart type"
                items={props.pageContext.activeViews}
                selectedItem={selectedView}
                onSelected={onSelectedItem}
                itemKey={view => ViewTypeEnum[view.id]}
                renderItem={view => <span><i className='material-symbols-outlined'>{view.icon}</i> <span>{view.name}</span></span>}
                itemDisplayText={view => view.name}
                asButton={false}
                showLabel={true}
            />
            {redirect && <Navigate to={redirect} replace/>}
        </>
    );

};

export default ViewSelector;