import * as PageHandler from "../PageHandler";
import React from "react";
import FixedPeriodDatePicker  from "./FixedPeriodDatePicker";
import { IAverageDescriptor } from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { IGoogleTagManager } from "../../googleTagManager";
import { IDropdownToggleAttributes } from "../helpers/DropdownToggleAttributes";
import { useReadVueQueryParams, useWriteVueQueryParams } from "../helpers/UrlHelper";
import { useLocation, useNavigate } from "react-router-dom";

interface IFixedPeriodFilterMenusProps extends React.HTMLAttributes<HTMLDivElement> {
    activeMetrics: Metric[];
    curatedFilters: CuratedFilters;
    applicationConfiguration: ApplicationConfiguration;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler.PageHandler;
    userVisibleAverages: IAverageDescriptor[];
    showRollingAverages: boolean;
    buttonAttr?: IDropdownToggleAttributes;
}

const FixedPeriodFilterMenus: React.FunctionComponent<IFixedPeriodFilterMenusProps> = (props) => {
    const writeVueQueryParams = useWriteVueQueryParams(useNavigate(), useLocation());
    const readVueQueryParams = useReadVueQueryParams();
    const { pageHandler, userVisibleAverages, buttonAttr, activeMetrics, curatedFilters, applicationConfiguration, googleTagManager, showRollingAverages, ...rest} = props;
    return (
        <div {...rest} >
            {userVisibleAverages.length > 0 &&
                <FixedPeriodDatePicker
                    pageHandler={pageHandler}
                    activeMetrics={activeMetrics}
                    curatedFilters={curatedFilters}
                    userVisibleAverages={userVisibleAverages}
                    buttonAttr={buttonAttr}
                    writeVueQueryParams={writeVueQueryParams}
                    readVueQueryParams={readVueQueryParams}
                    applicationConfiguration={applicationConfiguration}
                    googleTagManager={googleTagManager}
                    showRollingAverages={showRollingAverages}
                />
            }
        </div>
    );
};

export default FixedPeriodFilterMenus;