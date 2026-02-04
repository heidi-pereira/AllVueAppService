import { Metric } from "../../metrics/metric";
import * as BrandVueApi from "../../BrandVueApi";
import React from "react";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { EntityInstance } from "../../entity/EntityInstance";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { Table } from "reactstrap";
import DataSortOrder = BrandVueApi.DataSortOrder;
import {IEntityInstanceGroup} from "../../entity/IEntityInstanceGroup";
import { FilterOperator, SigConfidenceLevel } from "../../BrandVueApi";
import Throbber from "../throbber/Throbber";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";
import { selectTimeSelection } from "client/state/timeSelectionStateSelectors";

interface IBrandSampleProps {
    activeBrand: EntityInstance;
    keyBrands: IEntityInstanceGroup;
    height: number;
    metrics: Metric[];
    curatedFilters: CuratedFilters;
    metricDescriptions: string;
    metricGroups: string;
};

const BrandSample = (props: IBrandSampleProps) => {
    const [isLoading, setIsLoading] = React.useState(true);
    const [results, setResults] = React.useState<BrandVueApi.BrandSampleResults | null>(null);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    

    React.useEffect(() => {
        const fetchData = async () => {
            setIsLoading(true);
            const sigDiffOptions = new BrandVueApi.SigDiffOptions({
                        highlightSignificance: false,
                        sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
                        displaySignificanceDifferences: BrandVueApi.DisplaySignificanceDifferences.None,
                        significanceType: BrandVueApi.CrosstabSignificanceType.CompareToTotal,
                    });
            const model = new BrandVueApi.CuratedResultsModel({
                entityInstanceIds: props.keyBrands.getAll().map(b => b.id),
                measureName: props.metrics.map(m => m.name),
                subsetId: subsetId,
                period: new BrandVueApi.Period({
                    average: timeSelection.scorecardAverage.averageId,
                    comparisonDates: props.curatedFilters.comparisonDates(true, timeSelection)
                }),
                demographicFilter: props.curatedFilters.demographicFilter,
                activeBrandId: props.activeBrand.id,
                filterModel: new BrandVueApi.CompositeFilterModel({
                    filterOperator: FilterOperator.And,
                    filters: props.curatedFilters.measureFilters,
                    compositeFilters: []
                }),
                ordering: [],
                orderingDirection: DataSortOrder.Ascending,
                additionalMeasureFilters: [],
                includeSignificance: sigDiffOptions.highlightSignificance,
                sigConfidenceLevel: sigDiffOptions.sigConfidenceLevel,
                sigDiffOptions,
            });

            const result = await BrandVueApi.Factory.DataClient(err => err()).getBrandSampleResults(model);
            setResults(result);
            setIsLoading(false);
        };

        if (timeSelection.scorecardAverage) {
            fetchData();
        }
    }, [props.metrics, timeSelection]); // Add timeSelection as dependency

    if (!timeSelection.scorecardAverage) {
        return null;
    }

    if (isLoading || !results) {
        return (
            <div className="throbber-container-fixed">
                <Throbber />
            </div>
        );
    }

    const groups: { name: string, start: number, end: number }[] = [];

    props.metricGroups.split('|').forEach((g, i) => {
        if (g.length) groups.push({ name: g, start: i, end: i });
        else groups[groups.length - 1].end = i;
    });

    const descriptions = props.metricDescriptions.split('|');

    const getSampleSize = (metric: Metric): string => {
        const r = results.brandSampleMetricResults.find(m => m.metric === metric.name);
        return r ? String(r.weightedDailyResult.unweightedSampleSize || '-') : '-';
    }

    const currentPeriodDate = results.brandSampleMetricResults.length
        ? DateFormattingHelper.formatDateRange(
            results.monthSelectedEndDate,
            timeSelection.scorecardAverage)
        : '-';

    const getFooter = () => {
        return (
            <div className="sampleN">
                <span>{props.activeBrand.name} sample size for {currentPeriodDate}</span>
            </div>
        )
    }

    return (
        <div className="brandSample">
            <Table>
                <tbody>
                    {groups.map(g =>
                        <React.Fragment key={g.name}>
                            <tr>
                                <th>{g.name}</th>
                                <th>{currentPeriodDate}</th>
                            </tr>

                            {descriptions.slice(g.start, g.end + 1).map((d, i) =>
                                <tr key={d}>
                                    <td>{d}</td>
                                    <td>{getSampleSize(props.metrics[g.start + i])}</td>
                                </tr>
                            )}
                        </React.Fragment>
                    )}
                </tbody>
            </Table>
            {getFooter()}
        </div>
    );
}

export default BrandSample;