import '@testing-library/jest-dom';
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { Provider } from 'react-redux';
import { configureStore, combineReducers } from '@reduxjs/toolkit';
import templatesReducer from 'client/state/templatesSlice';
import ReportTemplateModal from "./ReportTemplateModal";
import toast from "react-hot-toast";

jest.mock("react-hot-toast", () => ({
    success: jest.fn(),
    error: jest.fn(),
}));

const rootReducer = combineReducers({
    templates: templatesReducer,
});

const mockSaveReportAsTemplate = jest.fn();
const mockGetAllTemplatesForUser = jest.fn().mockResolvedValue([
    {
        id: 101,
        templateDisplayName: "My Report",
        templateDescription: "A test template",
        savedReportId: 1,
        createdBy: "user123",
        createdDate: "2025-08-15T12:00:00Z",
    }
]);
jest.mock("../../../../BrandVueApi", () => ({
    Factory: {
        ReportTemplateClient: jest.fn(() => ({
            saveReportAsTemplate: mockSaveReportAsTemplate,
            getAllTemplatesForUser: mockGetAllTemplatesForUser,
        })),
    },
    ReportTemplateModel: class ReportTemplateModel {
        constructor(init: any) { Object.assign(this, init); }
    },
    CategorySortKey: {
        None: "None",
        BestScores: "BestScores",
        WorstScores: "WorstScores",
        OverPerforming: "OverPerforming",
        UnderPerforming: "UnderPerforming",
    }
}));

const defaultProps = {
    isOpen: true,
    selectedReportId: 1,
    selectedReportName: "My Report",
    setIsOpen: jest.fn(),
    closeAll: jest.fn(),
};

describe("ReportTemplateModal", () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockGetAllTemplatesForUser.mockResolvedValue([]);
    });

    it("shows error toast if template name is too long", async () => {
        const store = configureStore({
            reducer: rootReducer,
            preloadedState: { templates: { templates: [], isLoading: false, error: null } } as any
        });
        render(
            <Provider store={store}>
                <ReportTemplateModal {...defaultProps} />
            </Provider>
        );
        await waitFor(() => {
            expect(screen.getByLabelText("Template name")).toBeInTheDocument();
        });
        fireEvent.change(screen.getByLabelText("Template name"), { target: { value: 'a'.repeat(257) } });
        fireEvent.click(screen.getByRole("button", { name: /Create template/i }));
        expect(toast.error).toHaveBeenCalledWith("Template name must be 256 characters or less");
        expect(mockSaveReportAsTemplate).not.toHaveBeenCalled();
    });

    it("shows error toast if template description is too long", async () => {
        const store = configureStore({
            reducer: rootReducer,
            preloadedState: { templates: { templates: [], isLoading: false, error: null } } as any
        });
        render(
            <Provider store={store}>
                <ReportTemplateModal {...defaultProps} />
            </Provider>
        );
        await waitFor(() => {
            expect(screen.getByLabelText("Template description")).toBeInTheDocument();
        });
        fireEvent.change(screen.getByLabelText("Template description"), { target: { value: 'b'.repeat(257) } });
        fireEvent.click(screen.getByRole("button", { name: /Create template/i }));
        expect(toast.error).toHaveBeenCalledWith("Template description must be 256 characters or less");
        expect(mockSaveReportAsTemplate).not.toHaveBeenCalled();
    });

    it("renders modal with template fields and buttons when not loading", async () => {
                const store = configureStore({
                    reducer: rootReducer,
                    preloadedState: { templates: { templates: [], isLoading: false, error: null } } as any
                });
                render(
                    <Provider store={store}>
                        <ReportTemplateModal {...defaultProps} />
                    </Provider>
                );
        await waitFor(() => {
            expect(screen.getByLabelText("Template name")).toBeInTheDocument();
        });
        expect(screen.getByLabelText("Template description")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: /Cancel/i })).toBeInTheDocument();
        expect(screen.getByRole("button", { name: /Create template/i })).toBeInTheDocument();
    });

    it("shows error toast if template name already exists", async () => {
        mockGetAllTemplatesForUser.mockResolvedValue([
            { templateDisplayName: "My Report" }
        ]);
        const store = configureStore({
            reducer: rootReducer,
            preloadedState: ({
                templates: {
                    templates: [
                        {
                            id: 1,
                            templateDisplayName: "My Report",
                            templateDescription: "desc",
                            userId: "1",
                            baseVariable: {
                              id: 1,
                              productShortCode: "",
                              subProductId: "",
                              identifier: "",
                              displayName: "",
                              definition: {},
                              variableDependencies: [],
                              variablesDependingOnThis: [],
                              init: () => {},
                              toJSON: () => ({})
                            },
                            createdBy: "",
                            createdDate: "",
                            savedReportId: 1,
                            savedReportTemplate: {},
                            reportTemplateParts: [],
                            createdAt: "",
                            userDefinedVariableDefinitions: [],
                            averageConfiguration: {},
                            init: () => {},
                            toJSON: () => ({})
                        }
                    ],
                    isLoading: false,
                    error: null
                }
            }) as any
        });
        render(
            <Provider store={store}>
                <ReportTemplateModal {...defaultProps} />
            </Provider>
        );
        await waitFor(() => {
            expect(screen.getByLabelText("Template name")).toBeInTheDocument();
        });
        fireEvent.click(screen.getByRole("button", { name: /Create template/i }));
        expect(toast.error).toHaveBeenCalledWith("Template with name My Report already exists");
        expect(mockSaveReportAsTemplate).not.toHaveBeenCalled();
    });

    it("shows error toast on failed creation", async () => {
        mockSaveReportAsTemplate.mockResolvedValue({ status: 500 });
                const store = configureStore({
                    reducer: rootReducer,
                    preloadedState: { templates: { templates: [], isLoading: false, error: null } } as any
                });
                render(
                    <Provider store={store}>
                        <ReportTemplateModal {...defaultProps} />
                    </Provider>
                );
        await waitFor(() => {
            expect(screen.getByLabelText("Template name")).toBeInTheDocument();
        });
        fireEvent.click(screen.getByRole("button", { name: /Create template/i }));

        await waitFor(() => {
            expect(toast.error).toHaveBeenCalledWith("Error: Unable to create template");
            expect(defaultProps.closeAll).not.toHaveBeenCalled();
        });
    });

    it("shows error toast on exception", async () => {
        mockSaveReportAsTemplate.mockRejectedValue(new Error("Network error"));
                const store = configureStore({
                    reducer: rootReducer,
                    preloadedState: { templates: { templates: [], isLoading: false, error: null } } as any
                });
                render(
                    <Provider store={store}>
                        <ReportTemplateModal {...defaultProps} />
                    </Provider>
                );
        await waitFor(() => {
            expect(screen.getByLabelText("Template name")).toBeInTheDocument();
        });
        fireEvent.click(screen.getByRole("button", { name: /Create template/i }));

        await waitFor(() => {
            expect(toast.error).toHaveBeenCalledWith("Error: Unable to create template");
        });
    });
});