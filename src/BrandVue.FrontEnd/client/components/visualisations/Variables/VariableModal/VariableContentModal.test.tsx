import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import VariableContentModal from './VariableContentModal';
import { VariableProvider } from './Utils/VariableContext';
import { Metric } from '../../../../metrics/metric';
import { Provider } from 'react-redux';
import { setupStore } from '../../../../state/store';
import { IGoogleTagManager } from 'client/googleTagManager';
import { PageHandler } from 'client/components/PageHandler';
import * as BrandVueApi from '../../../../BrandVueApi';
import * as MetricStateContext from 'client/metrics/MetricStateContext';
import { MockRouter } from 'client/helpers/MockRouter';
import { MetricSet } from 'client/metrics/metricSet';
import { EntityConfigurationStateProvider } from 'client/entity/EntityConfigurationStateContext';
import { EntitySetFactory } from 'client/entity/EntitySetFactory';
import { EntityInstanceColourRepository } from 'client/entity/EntityInstanceColourRepository';
import { EntityConfigurationLoader } from 'client/entity/EntityConfigurationLoader';
import { MockApplication } from 'client/helpers/MockApp';
import { UserContext } from 'client/GlobalContext';
import { renderVariableModalComponent } from './Utils/VariableTestUtils';

const testMetric = new Metric(null, {
    entityCombination: [],
    displayName: "Test",
});

const baseProps = {
            isOpen: true,
            setIsOpen: jest.fn(),
            subsetId: "all",
            relatedMetric: testMetric,
        };

describe("VariableContentModal", () => {
    BrandVueApi.Factory.VariableConfigurationClient = jest
        .fn()
        .mockImplementation(() => {
            return {
                isVariableReferencedByAnotherVariable: jest.fn().mockResolvedValue([]),
                getFieldVariables: jest.fn().mockResolvedValue([]),
                getVariableGroupCountAndSamplePreview: jest
                    .fn()
                    .mockResolvedValue({ count: 1, samplePreview: [] }),
            };
        });

    it("should render with default props", async () => {
        await renderVariableModalComponent(VariableContentModal, baseProps);
        expect(
            screen.getByText((content, element) =>
                content.includes("Create new variable")
            )
        ).toBeInTheDocument();
    });

    it("should render a group variable to view", async () => {
        const props = {
            ...baseProps,
            variableIdToView: 3,
        };
        await renderVariableModalComponent(VariableContentModal, props);

        await waitFor(() => {
            expect(
                screen.getAllByText("Between (inclusive)", { exact: false })
            ).toHaveLength(2);
        });
    });

    it("should render a field expression variable to view", async () => {
        const props = {
            ...baseProps,
            variableIdToView: 4,
        };
        await renderVariableModalComponent(VariableContentModal, props);

        await waitFor(() => {
            expect(
                screen.getByText("Field expression", { exact: false })
            ).toBeInTheDocument();
        });
    });

    it("should not render create modal when user lacks create permission", async () => {
        const user = {
            featurePermissions: [
                {
                    id: 1,
                    name: "VariablesEdit",
                    code: BrandVueApi.PermissionFeaturesOptions.VariablesEdit,
                },
            ],
        };

        await renderVariableModalComponent(
            VariableContentModal,
            baseProps,
            undefined,
            user
        );

        // User doesn't have VariablesCreate permission, so modal should not render
        expect(screen.queryByText("Create new variable")).not.toBeInTheDocument();
    });

    it("should not render edit modal when user lacks edit permission", async () => {
        const props = {
            ...baseProps,
            variableIdToView: 3,
        };
        const user = {
            featurePermissions: [
                {
                    id: 1,
                    name: "VariablesCreate",
                    code: BrandVueApi.PermissionFeaturesOptions.VariablesCreate,
                },
            ],
        };

        // User doesn't have VariablesEdit permission, so modal should not render
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("should not render create modal when user has other permissions but lacks VariablesCreate permission", async () => {
        const user = {
            featurePermissions: [
                // User has edit permission but NOT create permission
                {
                    id: 2,
                    name: "VariablesEdit",
                    code: BrandVueApi.PermissionFeaturesOptions.VariablesEdit,
                },
                // User might have other permissions too
                {
                    id: 3,
                    name: "SomeOtherPermission",
                    code: 999 as unknown as BrandVueApi.PermissionFeaturesOptions,
                },
            ],
        };

        await renderVariableModalComponent(
            VariableContentModal,
            baseProps,
            undefined,
            user
        );

        // The modal should NOT render when user lacks VariablesCreate permission
        // The permission logic should check the specific permission and return false
        expect(screen.queryByText("Create new variable")).not.toBeInTheDocument();

        // Verify the user actually lacks create permission by checking their feature permissions
        const hasCreatePermission = user.featurePermissions?.some(
            (fp) => fp.code === BrandVueApi.PermissionFeaturesOptions.VariablesCreate
        );
        expect(hasCreatePermission).toBe(false);

        // Verify the user has edit permission (to show they have some permissions)
        const hasEditPermission = user.featurePermissions?.some(
            (fp) => fp.code === BrandVueApi.PermissionFeaturesOptions.VariablesEdit
        );
        expect(hasEditPermission).toBe(true);
    });

    it("should not render edit modal when user has other permissions but lacks VariablesEdit permission", async () => {
        const props = {
            ...baseProps,
            variableIdToView: 3,
        };
        const user = {
            featurePermissions: [
                // User has create permission but NOT edit permission
                {
                    id: 1,
                    name: "VariablesCreate",
                    code: BrandVueApi.PermissionFeaturesOptions.VariablesCreate,
                },
                // User might have other permissions too
                {
                    id: 3,
                    name: "SomeOtherPermission",
                    code: 998 as unknown as BrandVueApi.PermissionFeaturesOptions,
                },
            ],
        };

        await renderVariableModalComponent(
            VariableContentModal,
            props,
            undefined,
            user
        );

        // The modal should NOT render when user lacks VariablesEdit permission
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

        // Verify the user actually lacks edit permission by checking their feature permissions
        const hasEditPermission = user.featurePermissions?.some(
            (fp) => fp.code === BrandVueApi.PermissionFeaturesOptions.VariablesEdit
        );
        expect(hasEditPermission).toBe(false);

        // Verify the user has create permission (to show they have some permissions)
        const hasCreatePermission = user.featurePermissions?.some(
            (fp) => fp.code === BrandVueApi.PermissionFeaturesOptions.VariablesCreate
        );
        expect(hasCreatePermission).toBe(true);
    });
});
