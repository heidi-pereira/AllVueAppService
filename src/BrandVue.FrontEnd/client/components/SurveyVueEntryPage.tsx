import { Toaster } from 'react-hot-toast';
import { dsession } from '../dsession';
import SurveyVueEntryPageHeader from './SurveyVueEntryPageHeader';
import PageLayout from './PageLayout';
import { EntitySet } from '../entity/EntitySet';
import { AverageTotalRequestModel, IEntityType } from "../BrandVueApi";
import { EntityInstance } from "../entity/EntityInstance";
import { ApplicationConfiguration } from '../ApplicationConfiguration';
import { ProductConfiguration } from '../ProductConfiguration';
import { BaseVariableContextProvider } from './visualisations/Variables/BaseVariableContext';
import { IGoogleTagManager } from '../googleTagManager';
import { CatchReportAndDisplayErrors } from "./CatchReportAndDisplayErrors";
import { MetricSet } from '../metrics/metricSet';
import { IEntityConfiguration } from '../entity/EntityConfiguration';
import EnvironmentBannerBar from './helpers/EnvironmentBannerBar';

interface ISurveyVueEntryPageProps {
    session: dsession;
    enabledMetricSet: MetricSet;
    entityConfiguration: IEntityConfiguration;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    entitySet: EntitySet | undefined;
    splitByType: IEntityType;
    availableSplitByInstances: EntityInstance[];
    updateAverageRequests(averageRequests: AverageTotalRequestModel[] | null): void;
}

const SurveyVueEntryPage = (props: ISurveyVueEntryPageProps) => {
    const activeDashPage = props.session.activeDashPage;
    const activePaneType = activeDashPage.panes[0]?.paneType;
    const activePageName = activeDashPage.panes[0]?.pageName;

    return (
        <div className="survey-vue-scroll-template" id="survey-vue-scroll-template">
            <EnvironmentBannerBar productConfiguration={props.productConfiguration} />
            <Toaster position='bottom-center' toastOptions={{duration: 5000}}/>

            <div className="page-content-disabled"></div>

            <SurveyVueEntryPageHeader activePaneType={activePaneType} activePageName={activePageName}
                session={props.session} googleTagManager={props.googleTagManager} applicationConfiguration={props.applicationConfiguration} productConfiguration={props.productConfiguration} />

            <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration} childInfo={{ "Component": "SurveyVueEntryPage" }}>
                <BaseVariableContextProvider>
                    <PageLayout
                        layout={activeDashPage.layout}
                        panesToRender={activeDashPage.panes}
                        session={props.session}
                        enabledMetricSet={props.enabledMetricSet}
                        entityConfiguration={props.entityConfiguration}
                        googleTagManager={props.googleTagManager}
                        applicationConfiguration={props.applicationConfiguration}
                        productConfiguration={props.productConfiguration}
                        getAllInstancesForType={() => []}
                        availableEntitySets={props.entitySet ? [props.entitySet] : undefined}
                        updateMetricResultsSummary={() => []}
                        updateAverageRequests={props.updateAverageRequests}
                        removeFromLowSample={() => { }}
                        updateBaseVariableNames={() => { }}
                    />
                </BaseVariableContextProvider>
            </CatchReportAndDisplayErrors>

        </div>
    );
};

export default SurveyVueEntryPage;