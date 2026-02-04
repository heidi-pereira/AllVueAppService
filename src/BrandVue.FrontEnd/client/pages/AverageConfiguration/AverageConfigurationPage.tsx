import React from 'react';
import { useEffect, useState } from 'react';
import toast, { Toaster } from 'react-hot-toast';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { AverageConfiguration, AverageStrategy, CompanyModel, Factory, MakeUpTo, MultipleAverageConfigurationRequest, SwaggerException, TotalisationPeriodUnit, WeightAcross, WeightingMethod, WeightingPeriodUnit } from '../../BrandVueApi';
import Footer from '../../components/Footer';
import Throbber from '../../components/throbber/Throbber';
import { TabContent, TabPane, Nav, NavItem, NavLink } from 'reactstrap';
import AverageConfigurationPane from './AverageConfigurationPane';
import DeleteModal from '../../components/DeleteModal';
import SearchInput from '../../components/SearchInput';
import { DataSubsetManager } from '../../DataSubsetManager';
import { ProductConfiguration } from 'client/ProductConfiguration';
import AddAverageConfirmationModal, { AverageConfirmationOption } from './AddAverageConfirmationModal';

interface IAverageConfigurationPageProps {
    nav: React.ReactNode;
    productConfiguration: ProductConfiguration;
}

enum TabSelection {
    AverageConfigurations,
    FallbackAverages,
};

function orderAverages(averages: AverageConfiguration[]): AverageConfiguration[] {
    return averages.sort((a, b) => {
        if (a.order === b.order) {
            return a.displayName.localeCompare(b.displayName);
        }
        return a.order - b.order;
    })
}

function filterAverages(average: AverageConfiguration, searchText: string) {
    const loweredText = searchText.toLowerCase();
    return average.averageId.toLowerCase().includes(loweredText) ||
        average.displayName.toLowerCase().includes(loweredText)
}

const AverageConfigurationPage = (props: IAverageConfigurationPageProps) => {
    const averageConfigClient = Factory.AverageConfigurationClient(() => {});
    const configurationClient = Factory.ConfigClient(() => {});
    const [averageConfigurations, setAverageConfigurations] = useState<AverageConfiguration[]>([]);
    const [fallbackAverages, setFallbackAverages] = useState<AverageConfiguration[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [activeTab, setActiveTab] = useState<TabSelection>(TabSelection.AverageConfigurations);
    const [selectedAverage, setSelectedAverage] = useState<AverageConfiguration | undefined>();
    const [editingAverage, setEditingAverage] = useState<AverageConfiguration | undefined>();
    const [addingAverage, setAddingAverage] = useState<AverageConfiguration | undefined>();
    const [isDeleteAverageModalOpen, setDeleteAverageModalOpen] = useState<boolean>(false);
    const [searchText, setSearchText] = useState<string>("");
    const [authCompanies, setAuthCompanies] = useState<CompanyModel[]>([]);
    const [addAverageDropdownOpen, setAddAverageDropdownOpen] = useState<boolean>(false);
    const isFallbackAverage = selectedAverage != null && fallbackAverages.includes(selectedAverage);
    const isNewAverage = !isFallbackAverage && selectedAverage?.id == null || selectedAverage!.id <= 0;

    const isSurveyVue = props.productConfiguration.isSurveyVue();


    const scrollToTop = () => {
        document.querySelector('#configure-average > div')?.scrollIntoView({behavior: "smooth", block: "start", inline: "nearest"});
    }

    const selectAverage = (average: AverageConfiguration) => {
        setSelectedAverage(average);
        setEditingAverage(new AverageConfiguration({...average}));
        scrollToTop();
    }

    const reloadDataSubsetManagerSubsets = () => {
        setIsLoading(true);

        const api = Factory.MetaDataClient(error => error());
        return api.getSubsets().then(subsets => {
            DataSubsetManager.Initialize(subsets, "");
            DataSubsetManager.getAll();

        })
            .catch(error => toast.error("Couldn't get subsets configuration"))
            .finally(() => setIsLoading(false));
    }

    useEffect(() => {
        averageConfigClient.getAll()
            .then((averages) => {
                const configurations = orderAverages(averages.averageConfigurations);
                setAverageConfigurations(configurations);
                setFallbackAverages(orderAverages(averages.fallbackAverages));
                if (configurations.length > 0) {
                    selectAverage(configurations[0]);
                }
            })
            .catch(error => toast.error("Couldn't get average configurations"))
            .finally(() => reloadDataSubsetManagerSubsets())
    }, []);

    useEffect(() => {
        configurationClient.getAllAuthCompanies()
            .then(companies => setAuthCompanies(companies.sort((a,b) => a.displayName.localeCompare(b.displayName))));
    }, []);


    if (isLoading) {
         return (
            <div id="ld" className="loading-container">
                <Throbber />
            </div>
        );
    }

    const saveAverage = () => {
        if (editingAverage) {
            const action = () => isNewAverage ? averageConfigClient.create(editingAverage) : averageConfigClient.update(editingAverage);
            const actionName = isNewAverage ? 'create' : 'update';
            toast.promise(action(), {
                loading: 'Saving...',
                success: `${editingAverage.displayName} saved`,
                error: err => handleError(err, actionName)
            });
        }
    }

    const addAverage = (average: AverageConfiguration) => {
        if (isSurveyVue) {
            setAddingAverage(average);
        } else {
            toast.promise(averageConfigClient.create(average), {
                loading: 'Saving...',
                success: `${average.displayName} saved`,
                error: err => handleError(err, 'create')
            });
        }
    };

    const onAddAverageConfirmation = (options: AverageConfirmationOption[]) => {
        //in AllVue, we probably want to add both a weighted and unweighted copy of the average, one of which would be filtered out of the UI depending if data is weighted or not
        if (addingAverage) {
            const averagesToCreate: AverageConfiguration[] = [];
            if (options.includes(AverageConfirmationOption.Weighted)) {
                averagesToCreate.push(new AverageConfiguration({
                    ...addingAverage,
                    weightingMethod: WeightingMethod.QuotaCell,
                    averageId: `${addingAverage.averageId}Weighted`
                }));
            }
            if (options.includes(AverageConfirmationOption.Unweighted)) {
                averagesToCreate.push(new AverageConfiguration({
                    ...addingAverage,
                    weightingMethod: WeightingMethod.None,
                    averageId: `${addingAverage.averageId}Unweighted`
                }));
            }
            toast.promise(averageConfigClient.createMultiple(new MultipleAverageConfigurationRequest({averages: averagesToCreate})), {
                loading: 'Saving...',
                success: `${addingAverage.displayName} saved`,
                error: err => handleError(err, 'create')
            }).finally(() => {
                setAddingAverage(undefined);
            });
        }
    };

    const copyAsNewAverage = () => {
        if (selectedAverage) {
            const copy = new AverageConfiguration({
                ...selectedAverage,
                id: 0,
                averageId: `${selectedAverage.averageId}Copy`,
                displayName: `${selectedAverage.displayName} Copy`
            });
            selectAverage(copy);
        }
    }

    const createNewAverage = () => {
        const average = new AverageConfiguration({
            id: 0,
            productShortCode: '',
            subProductId: '',
            averageId: '',
            displayName: '',
            order: 0,
            group: [],
            totalisationPeriodUnit: TotalisationPeriodUnit.Day,
            numberOfPeriodsInAverage: 1,
            weightingMethod: WeightingMethod.QuotaCell,
            weightAcross: WeightAcross.SinglePeriod,
            averageStrategy: AverageStrategy.OverAllPeriods,
            makeUpTo: MakeUpTo.Day,
            weightingPeriodUnit: WeightingPeriodUnit.SameAsTotalization,
            includeResponseIds: false,
            isDefault: false,
            allowPartial: false,
            disabled: false,
            subsetIds: [],
        });
        selectAverage(average);
    }

    const openDeleteAverageModal = () => {
        if (selectedAverage?.id && selectedAverage.id > 0) {
            setDeleteAverageModalOpen(true);
        }
    }

    const deleteAverage = () => {
        if (selectedAverage?.id && !isFallbackAverage) {
            toast.promise(averageConfigClient.delete(selectedAverage.id), {
                loading: 'Deleting...',
                success: () => {
                    setDeleteAverageModalOpen(false);
                    return `${selectedAverage.displayName} deleted`
                },
                error: err => handleError(err, 'delete')
            });
        }
    }

    const handleError = (error, action: string): string => {
        if (error && SwaggerException.isSwaggerException(error)) {
            const swaggerException = error as SwaggerException;
            const responseJson = JSON.parse(swaggerException.response);
            return responseJson.message;
        }
        return `An error occurred trying to ${action} this average`;
    }

    const getAverageListElement = (average: AverageConfiguration): JSX.Element => {
        const className = 'average-list-element' +
            (average == selectedAverage ? ' selected' : '') +
            (average.disabled ? ' disabled' : '');
        return (
            <div key={average.averageId} className={className} title={average.displayName} onClick={() => selectAverage(average)}>
                <div className='average-name'>{average.averageId}</div>
                {average.isDefault &&
                    <div className='default-icon'><i className='material-symbols-outlined' title="Default average">star</i></div>
                }
            </div>
        );
    }

    const getAverageDropdownButton = () => {
        return (
            <ButtonDropdown isOpen={addAverageDropdownOpen} toggle={() => setAddAverageDropdownOpen(!addAverageDropdownOpen)} className="styled-dropdown new-average-dropdown">
                <DropdownToggle caret className="hollow-button new-average-button">
                    Add average
                </DropdownToggle>
                <DropdownMenu>
                    <DropdownItem header>Standard averages</DropdownItem>
                    <div className="scrollable-list">
                        {fallbackAverages.filter(avg => !avg.disabled).map(avg => (
                            <DropdownItem key={avg.averageId} onClick={() => addAverage(avg)}>{avg.displayName}</DropdownItem>
                        ))}
                    </div>
                    <DropdownItem divider />
                    <DropdownItem onClick={createNewAverage}><i className="material-symbols-outlined">add</i> Configure new average</DropdownItem>
                </DropdownMenu>
            </ButtonDropdown>
        );
    };

    return (
        <div id="average-configuration-page">
            {props.nav}
            <Toaster position='bottom-center' toastOptions={{duration: 5000}} />
            {selectedAverage &&
                <DeleteModal
                    isOpen={isDeleteAverageModalOpen}
                    thingToBeDeletedName={selectedAverage.displayName}
                    thingToBeDeletedType='average'
                    delete={deleteAverage}
                    closeModal={() => setDeleteAverageModalOpen(false)}
                    affectAllUsers
                    delayClick
                />
            }
            {addingAverage &&
                <AddAverageConfirmationModal
                    isOpen={addingAverage != undefined}
                    add={onAddAverageConfirmation}
                    cancel={() => setAddingAverage(undefined)}
                    average={addingAverage}/>
            }
            <div className="view-average-configurations">
                <div className='average-list'>
                    <Nav tabs>
                        <NavItem>
                            <NavLink className={activeTab === TabSelection.AverageConfigurations ? 'tab-active' : 'tab-item'}
                                onClick={() => setActiveTab(TabSelection.AverageConfigurations)}>
                                Configured Averages
                            </NavLink>
                        </NavItem>
                        <NavItem>
                            <NavLink className={activeTab === TabSelection.FallbackAverages ? 'tab-active' : 'tab-item'}
                                onClick={() => setActiveTab(TabSelection.FallbackAverages)}>
                                Fallback Averages
                            </NavLink>
                        </NavItem>
                    </Nav>
                    <SearchInput id='average-search' className='flat-search' text={searchText} onChange={text => setSearchText(text)} />
                    <TabContent activeTab={activeTab}>
                        <TabPane tabId={TabSelection.AverageConfigurations}>
                            {getAverageDropdownButton()}
                            {averageConfigurations.filter(a => filterAverages(a, searchText)).map(getAverageListElement)}
                            {averageConfigurations.length == 0 &&
                                <div className='no-averages-message'>
                                    No averages have been configured.
                                    <br />
                                    {!isSurveyVue && <>Fallback averages will be used.</>}
                                </div>
                            }
                        </TabPane>
                        <TabPane tabId={TabSelection.FallbackAverages}>
                            {fallbackAverages.filter(a => filterAverages(a, searchText)).map(getAverageListElement)}
                            {fallbackAverages.length == 0 &&
                                <div className='no-averages-message'>
                                    No fallback averages have been configured
                                </div>
                            }
                        </TabPane>
                    </TabContent>
                </div>
                <AverageConfigurationPane
                    selectedAverage={selectedAverage}
                    editingAverage={editingAverage}
                    isNewAverage={isNewAverage}
                    isFallbackAverage={isFallbackAverage}
                    authCompanies={authCompanies}
                    saveAverage={saveAverage}
                    copyAsNewAverage={copyAsNewAverage}
                    deleteAverage={openDeleteAverageModal}
                    setEditingAverage={setEditingAverage}
                    isSurveyVue={isSurveyVue}
                />
            </div>
            <Footer />
        </div>
    );
}

export default AverageConfigurationPage;