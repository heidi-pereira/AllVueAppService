import React from 'react';
import toast, { Toaster } from 'react-hot-toast';
import { CompanyModel, Factory, IApplicationUser, SavedBreakCombination, SwaggerException } from '../../BrandVueApi';
import DeleteModal from '../../components/DeleteModal';
import Footer from '../../components/Footer';
import { filterAudiences, groupAudiencesByCategory, UNCATEGORIZED_AUDIENCE_NAME } from '../../components/helpers/AudienceHelper';
import { getMetricsValidAsBreaks } from '../../components/helpers/SurveyVueUtils';
import SearchInput from '../../components/SearchInput';
import { UserContext } from '../../GlobalContext';
import { Metric } from '../../metrics/metric';
import { MetricSet } from '../../metrics/metricSet';
import AudienceConfigurationPane from './AudienceConfigurationPane';

interface IAudienceConfigurationPageProps {
    nav: React.ReactNode;
    isSurveyVue: boolean;
}

const AudienceConfigurationPageWrapper = (props: IAudienceConfigurationPageProps) => {
    return (
        <UserContext.Consumer>
            {user =>
                <AudienceConfigurationPage nav={props.nav} user={user} />
            }
        </UserContext.Consumer>
    );
}

const AudienceConfigurationPage = (props: {nav: React.ReactNode, user: IApplicationUser | null}) => {
    const [savedAudiences, setSavedAudiences] = React.useState<SavedBreakCombination[]>([]);
    const [searchText, setSearchText] = React.useState<string>("");
    const [selectedAudience, setSelectedAudience] = React.useState<SavedBreakCombination | undefined>(undefined);
    const [isDeleteModalOpen, setDeleteModalOpen] = React.useState<boolean>(false);
    const [metrics, setMetrics] = React.useState<Metric[]>([]);
    const [authCompanies, setAuthCompanies] = React.useState<CompanyModel[]>([]);
    const validMetrics = getMetricsValidAsBreaks(metrics);
    const savedBreaksClient = Factory.SavedBreaksClient(error => error());
    const metricsClient = Factory.MetricsClient(error => error());
    const configurationClient = Factory.ConfigClient(error => error());

    const reloadAudiences = (idToSelect?: number) => {
        savedBreaksClient.getAllSavedBreaksForSubproduct()
            .then(audiences => {
                setSavedAudiences(audiences);
                if (idToSelect) {
                    const matchingAudience = audiences.find(a => a.id === idToSelect) ?? audiences[0];
                    setSelectedAudience(matchingAudience);
                }
            });
    }

    const reloadMetrics = () => {
        metricsClient.getMetricsWithDisabledForAllSubsets()
            .then(measures => {
                const metrics = MetricSet.mapMeasuresToMetrics(measures).filter(m => m.eligibleForCrosstabOrAllVue);
                metrics.sort((a,b) => a.name.localeCompare(b.name));
                setMetrics(metrics);
            })
    }

    const reloadAuthCompanies = () => {
        configurationClient.getAllAuthCompanies()
                .then(companies => setAuthCompanies(companies.sort((a,b) => a.displayName.localeCompare(b.displayName))));
    }

    React.useEffect(() => {
        reloadAudiences();
        reloadMetrics();
        reloadAuthCompanies();
    }, []);

    const startCreatingNewAudience = () => {
        setSelectedAudience(new SavedBreakCombination({
            id: 0,
            productShortCode: 'unset',
            name: '',
            isShared: true,
            createdByUserId: props.user?.userId ?? '',
            breaks: []
        }));
    }

    const saveAudience = (audience: SavedBreakCombination) => {
        const actionName = audience.id > 0 ? "update" : "create";
        toast.promise(savedBreaksClient.saveAudience(audience), {
            loading: 'Saving...',
            success: (id) => {
                reloadAudiences(id);
                return `${audience.name} saved`;
            },
            error: err => handleError(err, actionName)
        });
    }

    const deleteSelectedAudience = () => {
        if (selectedAudience) {
            const name = selectedAudience.name;
            toast.promise(savedBreaksClient.removeSavedBreaks(selectedAudience.id), {
                loading: 'Deleting...',
                success: () => {
                    reloadAudiences();
                    setSelectedAudience(undefined);
                    return `${name} deleted`;
                },
                error: err => handleError(err, 'delete')
            });
        }
        setDeleteModalOpen(false);
    }

    const handleError = (error, action: string): string => {
        if (error && SwaggerException.isSwaggerException(error)) {
            const swaggerException = error as SwaggerException;
            const responseJson = JSON.parse(swaggerException.response);
            return responseJson.message;
        }
        return `An error occurred trying to ${action} this audience`;
    }

    const getAudienceListItem = (audience: SavedBreakCombination): JSX.Element => {
        const className = 'audience-list-element' +
            (audience == selectedAudience ? ' selected' : '');
        return (
            <div key={`${audience.category ?? ''}_${audience.name}`} className={className} title={audience.name} onClick={() => setSelectedAudience(audience)}>
                <div className='audience-name'>{audience.name}</div>
            </div>
        );
    }

    const categorizedAudiences = groupAudiencesByCategory(filterAudiences(savedAudiences, searchText));
    const categoryNames = Object.keys(categorizedAudiences)
        .filter(name => name != UNCATEGORIZED_AUDIENCE_NAME)
        .sort((a,b) => a.localeCompare(b));
    const uncategorizedAudiences = categorizedAudiences[UNCATEGORIZED_AUDIENCE_NAME];
    const hasAnyAudiences = categoryNames.length > 0 || (uncategorizedAudiences && uncategorizedAudiences.group.length > 0);

    return (
        <div id="audience-configuration-page">
            {props.nav}
            <Toaster position='bottom-center' toastOptions={{duration: 5000}} />
            {selectedAudience &&
                <DeleteModal
                    isOpen={isDeleteModalOpen}
                    thingToBeDeletedName={selectedAudience.name}
                    thingToBeDeletedType='audience'
                    delete={deleteSelectedAudience}
                    closeModal={() => setDeleteModalOpen(false)}
                    affectAllUsers
                    delayClick
                />
            }
            <div className="audience-configurations">
                <div className="audience-list">
                    <h3>Configured Audiences:</h3>
                    <div className="audience-list-scroll">
                        <SearchInput id='average-search' className='flat-search' onChange={text => setSearchText(text)} text={searchText} />
                        {!hasAnyAudiences &&
                            <div className="no-audiences-message">No audiences have been configured.</div>
                        }
                        {categoryNames.map(category =>
                            <div key={category}>
                                <div className="audience-list-element-header">{category}</div>
                                {categorizedAudiences[category].group.map(audience => getAudienceListItem(audience))}
                            </div>
                        )}
                        {uncategorizedAudiences &&
                            <>
                                {categoryNames.length > 0 && <div className="audience-list-element-header">{UNCATEGORIZED_AUDIENCE_NAME}</div>}
                                {uncategorizedAudiences.group.map(audience => getAudienceListItem(audience))}
                            </>
                        }
                    </div>
                    <button className="hollow-button new-audience-button" onClick={startCreatingNewAudience}>
                        <i className="material-symbols-outlined">add</i>
                        <div className="new-audience-button-text">Configure new audience</div>
                    </button>
                </div>
                <AudienceConfigurationPane
                    selectedAudience={selectedAudience}
                    validMetrics={validMetrics}
                    authCompanies={authCompanies}
                    saveAudience={saveAudience}
                    deleteAudience={() => setDeleteModalOpen(true)}
                />
            </div>
            <Footer />
        </div>
    );
}

export default AudienceConfigurationPageWrapper;