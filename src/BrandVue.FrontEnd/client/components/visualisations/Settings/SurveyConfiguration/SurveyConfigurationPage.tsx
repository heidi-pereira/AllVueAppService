import React from "react";
import { Factory, SwaggerException} from "../../../../BrandVueApi";
import { DataSubsetManager } from "../../../../DataSubsetManager";
import { ProductConfiguration } from "../../../../ProductConfiguration";
import Throbber from "../../../throbber/Throbber";
import { MetricSet } from "../../../../metrics/metricSet";
import CreateWaveVariableButton from "./CreateWaveVariable";
import { ApplicationConfiguration } from "../../../../ApplicationConfiguration";
import toast from "react-hot-toast";
import KimbleProposalPage from "./KimbleProposal";

interface ISurveyConfigurationPageProps {
    productConfiguration: ProductConfiguration;
    applicationConfiguration: ApplicationConfiguration;
    metrics: MetricSet;
}

const SurveyConfigurationPage = (props: ISurveyConfigurationPageProps) => {
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const allSubsets = DataSubsetManager.getAll() || [];

    React.useEffect(() => {
            setIsLoading(false);
    }, []);

    if (isLoading) {
        return <div id="ld" className="loading-container">
            <Throbber />
        </div>;
    }

    const onCreatedMetricName = (name: string) => {
        onMessage(`Created new wave '${name}'`);
        const dataCacheClient = Factory.DataCacheClient(error => error());
        dataCacheClient.forceReloadOfSurvey()
            .then(() => { })
            .catch((e: Error) => toast.error("An error occurred trying force the reload"));
    }

    const onMessage = (message: string) => {
        toast.success(message);
    }


    return (<section className='user-settings-page'>
        {props.productConfiguration.user.doesUserHaveAccessToInternalSavantaSystems &&
            <KimbleProposalPage productConfiguration={props.productConfiguration} />
        }
        {props.productConfiguration.user.isAdministrator &&
            <div><CreateWaveVariableButton allSubsets={allSubsets}
                applicationConfiguration={props.applicationConfiguration}
                onCreatedMetricName={onCreatedMetricName}
            />
            </div>
        }
       </section>
    );

}
export default SurveyConfigurationPage;