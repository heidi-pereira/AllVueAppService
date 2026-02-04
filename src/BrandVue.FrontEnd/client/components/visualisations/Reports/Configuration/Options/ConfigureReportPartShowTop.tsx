import React from 'react';
import { PartDescriptor, ReportOrder } from "../../../../../BrandVueApi";
import { PartType } from '../../../../panes/PartType';

interface IConfigureReportPartDisplayNameProps {
    part: PartDescriptor;
    entityInstanceCount: number;
    savePartChanges(newPart: PartDescriptor): void;
}

const defaultNumberOfItemsToShowForPartType = (partType: PartType) => {
    switch (partType) {
        case PartType.ReportsCardDoughnut:
            return 7;
        default:
            return 3;
    }
}

const ConfigureReportPartShowTop = (props: IConfigureReportPartDisplayNameProps) => {
    const [rangeInputValue, setRangeInputValue] = React.useState<number>(defaultNumberOfItemsToShowForPartType(PartType[props.part.partType]));
    const supportedShowTopNPartTypes: string[] = [PartType.ReportsCardDoughnut,
            PartType.ReportsCardChart,
            PartType.ReportsCardMultiEntityMultipleChoice,
            PartType.ReportsTable,
        ];

    const canPickShowTop = supportedShowTopNPartTypes.includes(props.part.partType);
    const showTopEnabled = props.part.showTop != undefined;

    React.useEffect(() => {
        const { part: { showTop } } = props;
        const value = showTop ? showTop : defaultNumberOfItemsToShowForPartType(PartType[props.part.partType]);
        setRangeInputValue(value);
    }, [props.part.showTop, props.part.partType])

    React.useEffect(() => {
        if (props.part.showTop == undefined && props.part.partType === PartType.ReportsCardDoughnut) {
            const defaultDoughnutShowTop = defaultNumberOfItemsToShowForPartType(PartType.ReportsCardDoughnut);
            if (props.entityInstanceCount > defaultDoughnutShowTop) {
                saveShowTop(defaultDoughnutShowTop);
            }
        }
    }, [props.part.partType])

    const saveShowTop = (newShowTopValue: number | undefined) => {
        const modifiedPart = new PartDescriptor(props.part);
        modifiedPart.showTop = newShowTopValue;

        if (newShowTopValue) {
            modifiedPart.reportOrder = ReportOrder.ResultOrderDesc;
        } else {
            modifiedPart.reportOrder = ReportOrder.ScriptOrderDesc;
        }

        props.savePartChanges(modifiedPart);
    }

    const toggleEnableShowTop = (enableShowTop: boolean) => {
        let newShowTopValue: number | undefined = undefined;
        if (enableShowTop) {
            newShowTopValue = props.part.showTop ?? rangeInputValue;
        }
        saveShowTop(newShowTopValue);
    }

    const updateShowTop = (newValue: number) => {
        saveShowTop(newValue);
    }

    if (canPickShowTop) {
        return (
            <>
                <label className="category-label">Answers</label>
                <div className="show-top-x">
                    <input id="enable-top-x-input"
                        className="checkbox"
                        type="checkbox"
                        checked={showTopEnabled} 
                        onChange={(e) => toggleEnableShowTop(e.target.checked)}
                        data-testid="enable-top-x-checkbox" 
                    />
                    <label htmlFor="enable-top-x-input">
                        Show top
                    </label>
                    <input type="number"
                        className="range-input"
                        autoComplete="off"
                        min="1"
                        step="1"
                        value={rangeInputValue}
                        onChange={(e) => updateShowTop(+e.target.value)}
                        disabled={!showTopEnabled}
                        data-testid="topn-range-input"
                    />
                    only
                </div>
            </>
        )
    }

    return null;
}

export default ConfigureReportPartShowTop;