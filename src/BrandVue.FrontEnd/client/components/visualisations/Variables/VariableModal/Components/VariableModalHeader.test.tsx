import { screen } from "@testing-library/react";
import '@testing-library/jest-dom';
import VariableModalHeader from "./VariableModalHeader";
import { ModalContent } from "../VariableContentModal";
import { PermissionFeaturesOptions} from "../../../../../BrandVueApi";
import { renderVariableModalComponent } from '../Utils/VariableTestUtils';

const props = {
            title:"Test Variable",
                content: ModalContent.Grouped,
                goBackHandler: jest.fn(),
                closeHandler: jest.fn(),
                handleError: jest.fn(),
                variableId: 123
        }

describe("VariableModalHeader FeatureGuard", () => {
    it("hides delete option when user lacks VariablesDelete permission", async () => {
        // User without VariablesDelete permission
        const user = {
            isSystemAdministrator: false,
            isAdministrator: false,
            featurePermissions: [
                { id: 2, name: "VariablesEdit", code: PermissionFeaturesOptions.VariablesEdit }
            ]
        };

        await renderVariableModalComponent(VariableModalHeader, props, undefined, user);

        // The delete button should not be present
        expect(screen.queryByRole("button", { name: /delete/i })).not.toBeInTheDocument();
    });

    it("shows delete option when user has VariablesDelete permission", async () => {
        // User with VariablesDelete permission
        const user = {
            isSystemAdministrator: false,
            isAdministrator: false,
            featurePermissions: [
                { id: 3, name: 'VariablesDelete', code: PermissionFeaturesOptions.VariablesDelete }
            ]
        };

        await renderVariableModalComponent(VariableModalHeader, props, undefined, user);

        // The delete button should be present
        expect(screen.getByRole("button", { name: /delete/i })).toBeInTheDocument();
    });
});