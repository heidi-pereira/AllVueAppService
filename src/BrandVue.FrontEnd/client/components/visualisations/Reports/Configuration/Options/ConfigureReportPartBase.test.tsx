import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import ConfigureReportPartBase, { IConfigureReportPartBaseProps } from "./ConfigureReportPartBase";
import { BaseDefinitionType, BaseExpressionDefinition, PartDescriptor } from "../../../../../BrandVueApi";
import { generateMetric, getEntitySet, getPartDescriptor } from "../../../../../helpers/ReactTestingLibraryHelpers";
import { PartWithExtraData } from "../../ReportsPageDisplay";
import '@testing-library/jest-dom';

const mockBreaksPicker = jest.fn();

const mockCrosstabPageStateContext = {
    crosstabPageState: {
        metricBaseLookup: {
            metricName: {
                baseType: BaseDefinitionType.SawThisQuestion,
                baseVariableId: 1,
            },
        },
    },
    crosstabPageDispatch: jest.fn(),
};

jest.mock("../../Components/BaseOptionsSelector", () => ({
    __esModule: true,
    default: jest.fn((props: any) => {
        mockBreaksPicker(props)
        return <div data-testid="base-options-selector" {...props} />;
    }),
}));

let mockPart: PartDescriptor = getPartDescriptor();
mockPart.baseExpressionOverride = new BaseExpressionDefinition({
    baseType: BaseDefinitionType.SawThisQuestion,
    baseVariableId: 1,
    baseMeasureName: "metricName",
});

const mockProps: IConfigureReportPartBaseProps = {
    reportPart: {
        metric: generateMetric('metricName'),
        part: mockPart,
        ref: React.createRef<HTMLDivElement>(),
        selectedEntitySet: getEntitySet('entitySetName'),
    } as PartWithExtraData,
    reportBaseTypeOverride: undefined,
    reportBaseVariableOverride: undefined,
    savePartChanges: jest.fn(),
    canCreateNewBase: true,
};


describe("ConfigureReportPartBase", () => {
    test("renders without crashing", () => {
        render(<ConfigureReportPartBase {...mockProps} />);
        expect(screen.getByTestId("base-options-selector")).toBeInTheDocument();
    });

    test("passes correct props to BaseOptionsSelector", () => {
        render(<ConfigureReportPartBase {...mockProps} />);
        expect(mockBreaksPicker).toHaveBeenCalledWith(expect.objectContaining({
            metric: expect.objectContaining({
                displayName: "metricName",
                downIsGood: false,
                name: "metricName",
                varCode: "metricName",
            }),
            baseType: BaseDefinitionType.SawThisQuestion,
            baseVariableId: 1,
            defaultBaseType: undefined,  // Since reportBaseTypeOverride is undefined
            defaultBaseVariableId: undefined,  // Since reportBaseVariableOverride is undefined
            canCreateNewBase: true,
            selectedPart: "spec2",
        }));
    });
});
