import React from 'react';
import {
    useGetApiUsersGetcompaniesQuery,
} from '../../rtk/apiSlice';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import Tooltip from '@mui/material/Tooltip';
import AutocompleteDropDown from '@shared/AutocompleteDropDown/AutocompleteDropDown';
const ALL_ITEMS = "*";

interface CompanyDropDownProps {
    selectedCompany?: string;
    onChange: (value: string) => void;
}

const CompanyDropDown: React.FC<CompanyDropDownProps> = ({ selectedCompany, onChange }) => {
    const { data: companies, isLoading: companiesLoading, error: companiesError } = useGetApiUsersGetcompaniesQuery();

    const handleCompanyChange = (value: string) => {
        if (value === ALL_ITEMS) {
            onChange('');
        }
        else {
            onChange(value);
        }
    }

    if (companiesError) {
        return (
            <div className="warning">
                <Tooltip title={
                    <>
                        There was a problem loading the list of companies.<br />
                        Status: {companiesError.status}<br />
                        Message: {companiesError.data?.error || 'Unknown error'}
                    </>
                }>
                    <WarningAmberIcon fontSize="small" />
                </Tooltip>
                Error loading companies
            </div>);
    }
    if (!companies || companies.length <= 1) return null;
    const companiesItems = [
        { value: ALL_ITEMS, label: 'All companies' },
        ...companies.map(company => ({ value: company.id, label: company.displayName }))
    ];
    return (
            <AutocompleteDropDown
                id="company-select-label"
                label="Company"
                value={selectedCompany && selectedCompany.length > 1 ? selectedCompany : ALL_ITEMS}
                onChange={handleCompanyChange}
                items={companiesItems}
                loading={companiesLoading}
                sx={{ 
                    mr: 1, 
                    textTransform: 'none', 
                    minWidth: {
                        xs: '100px',
                        md: '140px',
                        lg: '200px'
                    }
                }}
                size="small"
            />
    );
}

export default CompanyDropDown;