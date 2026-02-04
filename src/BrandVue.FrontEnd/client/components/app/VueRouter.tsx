import React from 'react';
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import EntityTypesConfigurationPage from "../../pages/EntityTypesConfigurationPage";
import BrandVue from "../../BrandVue";
import { PageConfiguration } from "../../pages/PageConfiguration";
import { MetricConfigurationPage } from '../../pages/MetricConfigurationPage';
import { WeightingsConfiguration } from '../../weightings/WeightingsConfiguration';
import SubsetConfigurationPage from "../../pages/SubsetConfigurationPage";
import {
    isAverageConfigurationEnabled,
    isBrandVueAudiencesConfigEnabled,
    isColourConfigurationEnabled,
    isEntityTypeConfigEnabled,
    isPagesAndMetricsConfigEnabled,
    isSubsetConfigEnabled,
    isWeightingsConfigAccessible,
    isQuestionVariableDefinitionConfigurationEnabled,
    isFeaturesConfigEnabled,
} from "../helpers/FeaturesHelper";
import { ColourConfigurationPage } from "../../pages/ColourConfigurationPage";
import { WeightingGeneration } from '../../weightings/WeightingGeneration';
import AverageConfigurationPage from '../../pages/AverageConfiguration/AverageConfigurationPage';
import AudienceConfigurationPageWrapper from '../../pages/AudienceConfiguration/AudienceConfigurationPage';
import { ProductConfiguration } from '../../ProductConfiguration';
import ConfigurationTopNav from '../ConfigurationTopNav';
import { UserContext } from '../../GlobalContext';
import { ProductConfigurationContext } from '../../ProductConfigurationContext';
import QuestionVariableDefinitionConfigurationPage
    from '../../pages/QuestionVariableDefinitionConfiguration/QuestionVariableDefinitionConfigurationPage';
import FeaturesPage from '../visualisations/Settings/Features/Features';
import { UseAppTitle } from "../helpers/UseDocumentTitle";
import { TagManagerProvider } from "../../TagManagerContext";
import TableBuilderWrapper from '../../pages/TableBuilder/TableBuilderWrapper';

interface IProps {
    productConfiguration: ProductConfiguration;
}

const VueRouter = (props: IProps) => {
    const {productConfiguration} = props;
    const basename = `${productConfiguration.appBasePath}/ui`;
    const topNav = <ConfigurationTopNav productConfiguration={props.productConfiguration}/>;
    UseAppTitle(productConfiguration);

    return (
        <ProductConfigurationContext.Provider value={{productConfiguration: props.productConfiguration}}>
            <UserContext.Provider value={props.productConfiguration.user}>
                <BrowserRouter basename={basename}>
                    <TagManagerProvider>
                        <Routes>
                            {isSubsetConfigEnabled(props.productConfiguration) && (
                                <Route
                                    path="/subset-configuration"
                                    element={<SubsetConfigurationPage
                                        nav={topNav}
                                    />}
                                />
                            )}
                            <Route
                                path="/segment-configuration"
                                element={<Navigate to="/subset-configuration"/>}
                            />
                            {isPagesAndMetricsConfigEnabled(props.productConfiguration) && (
                                <Route
                                    path="/page-configuration"
                                    element={<PageConfiguration
                                        nav={topNav}
                                        productName={props.productConfiguration.productName}
                                    />}
                                />
                            )}
                            {isPagesAndMetricsConfigEnabled(props.productConfiguration) && (
                                <Route
                                    path="/metric-configuration"
                                    element={
                                        <MetricConfigurationPage
                                            nav={topNav}
                                            productName={props.productConfiguration.productName}
                                        />
                                    }
                                />
                            )}

                            {isWeightingsConfigAccessible(props.productConfiguration) && (
                                <Route
                                    path="/weightings-configuration"
                                    element={
                                        <WeightingsConfiguration
                                            nav={topNav}
                                        />
                                    }
                                />
                            )}

                            {isWeightingsConfigAccessible(props.productConfiguration) && (
                                <Route
                                    path="/weightings-generation"
                                    element={
                                        <WeightingGeneration
                                            nav={topNav}
                                        />
                                    }
                                />
                            )}
                            {isFeaturesConfigEnabled(props.productConfiguration) &&
                                <Route path="/features-configuration"
                                    element={<FeaturesPage nav={topNav}
                                    productConfiguration={props.productConfiguration}/>}
                                />
                            }
                            {isEntityTypeConfigEnabled(props.productConfiguration) && (
                                <Route
                                    path="/entity-type-configuration"
                                    element={
                                        <EntityTypesConfigurationPage
                                            nav={topNav}
                                        />
                                    }
                                />
                            )}

                            {isColourConfigurationEnabled(props.productConfiguration) && (
                                <Route
                                    path="/colour-configuration"
                                    element={
                                        <ColourConfigurationPage
                                            nav={topNav}
                                        />
                                    }
                                />
                            )}

                            {isAverageConfigurationEnabled(props.productConfiguration) && (
                                <Route
                                    path="/average-configuration"
                                    element={
                                        <AverageConfigurationPage
                                            nav={topNav}
                                            productConfiguration={props.productConfiguration}
                                        />
                                    }
                                />
                            )}
                            {isQuestionVariableDefinitionConfigurationEnabled(props.productConfiguration) && (
                                <Route
                                    path="/question-variable-definition-configuration"
                                    element={
                                        <QuestionVariableDefinitionConfigurationPage
                                            nav={topNav}
                                        />
                                    }
                                />
                            )}

                            {isBrandVueAudiencesConfigEnabled(props.productConfiguration) && (
                                <Route
                                    path="/audience-configuration"
                                    element={
                                        <AudienceConfigurationPageWrapper
                                            nav={topNav}
                                            isSurveyVue={props.productConfiguration.isSurveyVue()}
                                        />
                                    }
                                />
                            )}


                            {props.productConfiguration.isSurveyVue() && (
                                //we don't have user feature context here, so route then redirect if no access
                                <Route
                                    path="/table-builder"
                                    element={
                                        <TableBuilderWrapper
                                            nav={topNav}
                                            productConfiguration={props.productConfiguration}
                                        />
                                    }
                                />
                            )}

                            <Route
                                path="/results/*"
                                element={
                                    <Navigate
                                        to={window.location.pathname.replace(/results/, 'reports')}
                                    />
                                }
                            />
                            <Route
                                path="*"
                                element={
                                    <BrandVue
                                        productConfiguration={props.productConfiguration}
                                    />
                                }
                            />
                        </Routes>
                    </TagManagerProvider>
                </BrowserRouter>
            </UserContext.Provider>
        </ProductConfigurationContext.Provider>
    );
}

export default VueRouter;