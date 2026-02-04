import React from 'react';
import {CompanyModel } from '../../../../BrandVueApi';

interface ITitleProps {
    projectCompany: CompanyModel|undefined;
}

const FeaturesTitleControl = (props: ITitleProps) => {
    if (props.projectCompany != undefined) {
        return (<div>
            Only users assocated to company <b>{props.projectCompany.displayName}</b> 
                {props.projectCompany.parentCompanyDisplayName && (props.projectCompany.parentCompanyDisplayName.length > 0) &&
                    <>
                &nbsp;or <b>{props.projectCompany.parentCompanyDisplayName}</b>
                    </>
                }
                &nbsp;will be visible.
            </div>
        );
    }
    return <></>
}

export default FeaturesTitleControl;