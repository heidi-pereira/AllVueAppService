import { IBreaksPickerProps, BreaksPicker } from "./BreaksPicker";
import { CrossMeasure, ReportType } from "../../../BrandVueApi";
import { IGoogleTagManager } from "../../../googleTagManager";
import { PageHandler } from "../../../components/PageHandler";
import { mock } from "jest-mock-extended";
import { render } from "@testing-library/react";
import { Provider } from "react-redux";
import { getCrossMeasures, getMetrics } from "../../../helpers/ReactTestingLibraryHelpers";
import "@testing-library/jest-dom";
import { setupStore } from "client/state/store";
import {MockRouter} from "../../../helpers/MockRouter";
import { BreakPickerParent } from "./BreaksDropdownHelper";
import { MockStoreBuilder } from "client/helpers/MockStore";

const getDefaultBreaksPickerState = (numberOfCrossMeasures: number) => {
    const metrics = getMetrics(3);
    const categories = getCrossMeasures(metrics, numberOfCrossMeasures);

    const defaultState: IBreaksPickerProps = {
        reportType: ReportType.Table,
        isDisabled: false,
        isPrimaryAction: false,
        selectedBreaks: categories,
        setSelectedBreaks: (breaks: CrossMeasure[]) => {console.log("setSelectedBreaks not implemented")},
        user: null,
        canSaveAndLoad: false,
        googleTagManager: mock<IGoogleTagManager>(),
        pageHandler: mock<PageHandler>(),
        groupCustomVariables: false,
        isReportSettings: false,
        isReportLevelChecked: false,
        setIsReportLevelChecked: (isReportLevelChecked: boolean) => {console.log("setIsReportLevelChecked not implemented")},
        supportMultiBreaks: true,
        displayBreakInstanceSelector: true,
        parentComponent: BreakPickerParent.Crosstab
    }

    return defaultState;
}

const getTestingComponent = (breaksPickerState: IBreaksPickerProps): JSX.Element => (
    <Provider store={setupStore(new MockStoreBuilder().build())}>
        <MockRouter>
            <BreaksPicker
                reportType={breaksPickerState.reportType}
                selectedBreaks={breaksPickerState.selectedBreaks}
                setSelectedBreaks={breaksPickerState.setSelectedBreaks}
                user={breaksPickerState.user}
                canSaveAndLoad={breaksPickerState.canSaveAndLoad}
                groupCustomVariables={breaksPickerState.groupCustomVariables}
                isPrimaryAction={breaksPickerState.isPrimaryAction}
                googleTagManager={breaksPickerState.googleTagManager}
                pageHandler={breaksPickerState.pageHandler}
                supportMultiBreaks={breaksPickerState.supportMultiBreaks}
                displayBreakInstanceSelector={breaksPickerState.displayBreakInstanceSelector}
                isDisabled={breaksPickerState.isDisabled}
                parentComponent={breaksPickerState.parentComponent}
            />
        </MockRouter>
    </Provider>
);

describe(BreaksPicker, () => {    
    it("should only display breaks dropdown when no break is present (report level modal)", () => {
        const breaksPickerState = getDefaultBreaksPickerState(0);

        //the reportsettingsmodal is currently hardcoded to false as not all charts support this, we should update it if we ever we
        //support multiple breaks globally
        breaksPickerState.supportMultiBreaks = false;

        const { rerender, container } = render(getTestingComponent(breaksPickerState));

        let addBreaksButton = container.getElementsByClassName("metric-dropdown-menu");
        expect(addBreaksButton).toBeDefined;

        breaksPickerState.selectedBreaks = getCrossMeasures(getMetrics(3), 1);
        expect(addBreaksButton).toBeUndefined;
    })

    it("should always display breaks dropdown (table reports)", () => {
        const breaksPickerState = getDefaultBreaksPickerState(0);

        const { rerender, container } = render(getTestingComponent(breaksPickerState));

        let addBreaksButton = container.getElementsByClassName("metric-dropdown-menu");
        expect(addBreaksButton).toBeDefined;

        breaksPickerState.selectedBreaks = getCrossMeasures(getMetrics(3), 1);
        expect(addBreaksButton).toBeUndefined;
    })

    it("should always display breaks dropdown (data page)", () => {
        const breaksPickerState = getDefaultBreaksPickerState(0);
        breaksPickerState.reportType = undefined;

        const { rerender, container } = render(getTestingComponent(breaksPickerState));

        let addBreaksButton = container.getElementsByClassName("metric-dropdown-menu");
        expect(addBreaksButton).toBeDefined;

        breaksPickerState.selectedBreaks = getCrossMeasures(getMetrics(3), 1);
        expect(addBreaksButton).toBeDefined;
    })

    it("should show the report level checkbox when isDisabled is false", () => {
        const breaksPickerState = getDefaultBreaksPickerState(0);

        const { rerender, container } = render(getTestingComponent(breaksPickerState));

        expect(container.getElementsByClassName("use-report-settings-label").length).toEqual(1);
    });

    it("should hide the report level checkbox when isDisabled is true", () => {
        const breaksPickerState = getDefaultBreaksPickerState(0);
        breaksPickerState.isDisabled = true;

        const { rerender, container } = render(getTestingComponent(breaksPickerState));

        expect(container.getElementsByClassName("use-report-settings-label").length).toEqual(0);
    });
})