import React from 'react';
import { Modal, Dropdown, DropdownMenu, DropdownItem, DropdownToggle } from 'reactstrap';
import { ModalBody } from 'react-bootstrap';
import { ControlledSearchInput } from '../../../../components/SearchInput';
import { ICompanyModel, IOrganisationFeatureModel } from "client/BrandVueApi";
import { doesContainSearchText } from '../Users/UsersHelpers';
import { Toaster } from 'react-hot-toast';
import { useAppSelector } from '../../../../state/store';

interface IAddOrgsModalProps {
    isOpen: boolean;
    setIsOpen(isOpen: boolean): void;
    currentSet: IOrganisationFeatureModel[];
    addOrgs(orgs: ICompanyModel[]): void;
}

const AddOrgsModal = (props: IAddOrgsModalProps) => {
    const { allOrgs } = useAppSelector((state) => state.features);

    const [searchText, setSearchText] = React.useState("");
    const [selectedOrgs, setSelectedOrgs] = React.useState<ICompanyModel[]>([]);
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const [filteredOrgs, setFilteredOrgs] = React.useState<ICompanyModel[]>([]);

    React.useEffect(() => {
        const available = allOrgs.filter(org =>
            props.currentSet.find(orgFeature => orgFeature.organisationId == org.id) == undefined
        );
        setFilteredOrgs(available);
    }, [props.currentSet]);

    const closeModal = () => {
        props.setIsOpen(false);
        setSearchText("");
        setSelectedOrgs([]);
        setDropdownOpen(false);
    }

    const toggle = () => {
        setDropdownOpen(!dropdownOpen);
    }

    const searchTextChanged = (searchText: string) => {
        setSearchText(searchText);

        if (searchText.length > 0) {
            setDropdownOpen(true);
        }
    }

    const selectOrg = (org: ICompanyModel) => {
        let newOrgs = [...selectedOrgs];
        newOrgs.push(org);
        setSelectedOrgs(newOrgs);
        setSearchText("");
    }

    const deselectOrg = (orgToRemove: ICompanyModel) => {
        let newOrgs = selectedOrgs.filter(org => org.id !== orgToRemove.id);
        setSelectedOrgs(newOrgs);
    }

    const addOrgs = () => {
        props.addOrgs(selectedOrgs);
        setSelectedOrgs([]);
    }

    const orgContainsSearchText = (org: ICompanyModel, searchText: string) => {
        const trimmedText = searchText.trim();
        return doesContainSearchText(org.displayName, trimmedText) || doesContainSearchText(org.shortCode, trimmedText);
    }

    const matchedOrgs = filteredOrgs.filter(org => {
        return !selectedOrgs.some(o => o.id === org.id) && orgContainsSearchText(org, searchText);
    });

    const getAddButtonText = () => {
        if (selectedOrgs.length === 1) {
            return "Add organisation";
        }

        return "Add organisations";
    }

    return (
        <Modal isOpen={props.isOpen} className="add-users-modal" centered keyboard={false} autoFocus={false}>
            <ModalBody>
                <button className="modal-close-button" onClick={closeModal} title="Close">
                    <i className="material-symbols-outlined">close</i>
                </button>
                <h1 className="header">
                    Add organisations
                </h1>
                <div className="details">
                    Give people access to this feature
                </div>
                <div className="content-and-buttons">
                    <div className="content">
                        <Dropdown isOpen={dropdownOpen} className='add-user-dropdown' toggle={toggle}>
                            <DropdownToggle tag="div" className="question-search-container">
                                <ControlledSearchInput 
                                    id="user-search"
                                    text={searchText}
                                    onChange={(text) => searchTextChanged(text)}
                                    className="question-search-input-group"
                                    autoFocus={true}
                                    placeholder={"Search for an organisation"}
                                    ariaLabel="Organisations able to be added to project"
                                />
                            </DropdownToggle>
                            <DropdownMenu>
                                {matchedOrgs.map(org => {
                                    return <DropdownItem key={org.id} onClick={() => selectOrg(org)}>
                                        <AddOrgRow org={org} />
                                    </DropdownItem>
                                })}
                            </DropdownMenu>
                        </Dropdown>
                        <ul className='add-user-list'>
                            {selectedOrgs.map(org => {
                                return <AddOrgRow key={org.id} org={org} deselectOrg={deselectOrg} />
                            })}
                        </ul>
                    </div>
                    <div className="modal-buttons">
                        <button className="modal-button secondary-button" onClick={closeModal}>Cancel</button>
                        <button className="modal-button primary-button" onClick={addOrgs} disabled={selectedOrgs.length === 0}>{getAddButtonText()}</button>
                    </div>
                </div>
            </ModalBody>
            <Toaster position='bottom-center' toastOptions={{duration: 5000}} />
        </Modal>
    );
};

interface IAddOrgRowProps {
    org: ICompanyModel;
    deselectOrg?: (org: ICompanyModel) => void;
}

const AddOrgRow = ({org: org, deselectOrg}: IAddOrgRowProps) => {
    return (
        <li className='add-user-row'>
            <div className='user-dropdown-title'>
                <div className='full-name'>
                    {org.displayName}
                </div>
            </div>
            {deselectOrg &&
                <button className="deselect-user-button" onClick={() => deselectOrg(org)} title="Deselect user">
                    <i className="material-symbols-outlined">close</i>
                </button>
            }
        </li>
    )
}

export default AddOrgsModal;