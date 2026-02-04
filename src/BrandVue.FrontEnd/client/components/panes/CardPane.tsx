import React from "react";
import { dsession } from "../../dsession";
import { IPaneDescriptor, IAverageDescriptor, CategorySortKey } from "../../BrandVueApi";
import { EntitySet } from "../../entity/EntitySet";
import { CatchReportAndDisplayErrors } from "../CatchReportAndDisplayErrors";
import Card from "./Card";
import { getBasePathByPageName, QueryStringParamNames, useReadVueQueryParams } from "../helpers/UrlHelper";
import { getStartPage } from "../helpers/PagesHelper";
import DropdownSelector from "../dropdown/DropdownSelector";
import { StringHelper } from "../../helpers/StringHelper";
import Tooltip from "../Tooltip";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { getTypedPart } from "../../parts/IPart";
import { ProductConfigurationContext } from "../../ProductConfigurationContext";
import { MetricSet } from "../../metrics/metricSet";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { PaneType } from "./PaneType";
import {setActiveEntitySet} from "../../state/entitySelectionSlice";
import {useDispatch} from "react-redux";

interface IProps {
    paneIndex: number;
    layout: "PartGrid" | "PartColumn";
    session: dsession;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    applicationConfiguration: ApplicationConfiguration;
    pane: IPaneDescriptor;
    entitySet: EntitySet;
    averages: IAverageDescriptor[];
    availableEntitySets: EntitySet[];
    updateBaseVariableNames: (firstName: string | undefined, secondName: string | undefined) => void;
}

const CardPane: React.FunctionComponent<IProps> = (props: IProps) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const { getQueryParameterInt } = useReadVueQueryParams();
    const dispatch = useDispatch();
    
    const onChange = React.useCallback((brandSet: EntitySet | null): void => {
        if (brandSet && brandSet.type.isBrand) {
            dispatch(setActiveEntitySet({entitySet: brandSet}));
        } else {
            throw Error("Not brand");
        }
    }, [dispatch]);

    const getTitle = () => {
        const getTitleWithBrandSelector = (title: string) => {
            const entitySetList = props.availableEntitySets.filter(e => !e.isSectorSet);

            const getBrandGroupToolTip = () => {
                return <div className="brandvue-tooltip">
                    <div className="tooltip-header">Competitors</div>
                    <div className="tooltip-label">{props.entitySet.getInstances()
                        .getAll()
                        .filter(i => i.id !== props.entitySet.mainInstance?.id)
                        .map(i => { return (<p key={i.id}>{i.name}</p>) })}</div>
                </div>;
            }

            if (entitySetList.length > 1) {
                const brandDropdown =

                    <span className="inline">
                        <Tooltip placement="bottom" title={getBrandGroupToolTip()}>
                            <div className="tooltip-area">{props.entitySet.name}</div>
                        </Tooltip>
                        <DropdownSelector<EntitySet>
                            label=""
                            items={entitySetList}
                            selectedItem={props.entitySet}
                            onSelected={onChange}
                            itemDisplayText={e => e.name}
                            asButton={false}
                            showLabel={true}
                            itemKey={e => e.name}
                        />
                    </span>
                    ;

                return StringHelper.replaceJSX(title, "{{brandSetSelector}}", brandDropdown);
            }

            return (<>{title.replace("{{brandSetSelector}}", props.entitySet.name)}</>);
        }

        const applyDynamicUser = (title: string): string => { return title.replace("{{user}}", productConfiguration.user.name);}
        const applyDynamicInstance = (title: string): string => { return title.replace("{{instance}}", props.entitySet.getMainInstance().name);}
        const applyDynamicBrandSet = (title: string): string => { return title.replace("{{brandSet}}", props.entitySet.name);}
        const applyDynamicBrandSetSelector = (title: string) => { return getTitleWithBrandSelector(title)};

        return applyDynamicBrandSetSelector(applyDynamicBrandSet(applyDynamicInstance(applyDynamicUser(props.pane.spec))));
      };

    const startPage = getStartPage();
    const startPageUrl = productConfiguration.appBasePath + getBasePathByPageName(startPage.name);
    const dateOfLastDataPoint = props.applicationConfiguration.dateOfLastDataPoint;
    const dateOfFirstDataPoint = props.applicationConfiguration.dateOfFirstDataPoint;

    const headerContent = () => {
        const iconPath = props.pane.spec2 ? productConfiguration.calculateCdnPath(`/assets/img/${props.pane.spec2}`) : undefined;
        return <>
            {iconPath && <img src={iconPath} alt={props.pane.spec2} className="pane-icon" />}
            {getTitle()}
        </>;
    }

    const base1 = getQueryParameterInt(QueryStringParamNames.baseVariableId1) ?? -1;
    const base2 = getQueryParameterInt(QueryStringParamNames.baseVariableId2) ?? -1;

    return (
        <div className={props.layout}>
            {
                props.pane.paneType == PaneType.audienceProfile
                    ? <div className="pane-header">{headerContent()}</div>
                    : <h2>{headerContent()}</h2>
            }
            <div className="part-container">
                {props.pane.parts.map((p, i) =>
                    <CatchReportAndDisplayErrors key={i} applicationConfiguration={props.applicationConfiguration}
                                                 childInfo={{
                                                    "Part": p.partType,
                                                    "Spec1": p.spec1,
                                                    "Spec2": p.spec2,
                                                    "Spec3": p.spec3
                        }}
                                                 startPagePath={startPageUrl}
                                                 startPageName={startPage.displayName}>

                        <Card
                            selectedSubsetId={props.session.selectedSubsetId}
                            partConfig={getTypedPart(p)}
                            metrics={props.enabledMetricSet.getMetrics(p.spec1)}
                            averages={props.averages}
                            pageHandler={props.session.pageHandler}
                            entitySet={props.entitySet}
                            entityConfiguration={props.entityConfiguration}
                            dateOfLastDataPoint={dateOfLastDataPoint}
                            getAverageById={(averageId: string) => props.session.getAverageByIdOrDefault(averageId)}
                            dateOfFirstDataPoint={dateOfFirstDataPoint}
                            baseVariableId1={base1}
                            baseVariableId2={base2}
                            title={props.pane.spec}
                            paneIndex={props.paneIndex}
                            updateBaseVariableNames={props.updateBaseVariableNames}
                        />
                    </CatchReportAndDisplayErrors>
                )}
            </div>
        </div>
    );
}

export default CardPane;