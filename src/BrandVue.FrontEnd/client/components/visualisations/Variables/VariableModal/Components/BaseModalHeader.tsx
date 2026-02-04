import React from "react";
import {FeatureGuard} from "client/components/FeatureGuard/FeatureGuard";
import { PermissionFeaturesOptions} from "../../../../../BrandVueApi";
import { ProductConfigurationContext } from "../../../../../ProductConfigurationContext";

interface BaseModalHeaderProps {
    goBackHandler: () => void;
    setIsDeleteModalOpen: (isOpen: boolean) => void;
    closeHandler: () => void;
    canShowDelete?: boolean;
    isBase?: boolean;
    canGoBack?: boolean;
    title: string;
}

const BaseModalHeader: React.FC<BaseModalHeaderProps> = (props) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);

    const backButton = () => {
        return (
            <button onClick={props.goBackHandler} className="close-button">
                <i className="material-symbols-outlined" title="Back">arrow_back</i>
            </button>
        );
    }

    const rightButtons = () => {
        return (
            <div className="variable-modal-right-buttons">
                <FeatureGuard permissions={[PermissionFeaturesOptions.VariablesDelete]} 
                        customCheck={(userContext, isAuthorized) => !!((userContext.isSystemAdministrator || (productConfiguration.isSurveyVue() && isAuthorized)) && props.canShowDelete) }>
                    <button onClick={() => props.setIsDeleteModalOpen(true)} className="delete-button">
                        <i className="material-symbols-outlined" title={`Delete ${props.isBase ? "base" : "variable"}`}>delete</i>
                    </button>
                </FeatureGuard>
                <button onClick={props.closeHandler} className="close-button">
                    <i className="material-symbols-outlined" title="Close">close</i>
                </button>
            </div>
        );
    }

    return (
        <>
            <div className="top-buttons">
                {props.canGoBack && backButton()}
                {rightButtons()}
            </div>
            <div className="header">
                {props.title}
            </div>
        </>
    );
}

export default BaseModalHeader;