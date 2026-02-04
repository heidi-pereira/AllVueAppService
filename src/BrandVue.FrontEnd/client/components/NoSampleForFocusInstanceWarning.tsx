import React from 'react';
import { EntityInstance } from '../entity/EntityInstance';
import WarningWithToggle from './WarningWithToggle';
import { IEntityType } from '../BrandVueApi';

const NoSampleForFocusInstanceWarning = (props: { toggleVisibility: () => void, activeEntityType: IEntityType, focusInstanceNoSample: boolean}) => {

    if (!props.focusInstanceNoSample) {
        return null;
    };

    const entityName = props.activeEntityType ? props.activeEntityType.displayNameSingular : "metric";

    return <WarningWithToggle
        text={`No sample for focus ${entityName.toLowerCase()}`}
        toggleVisibility={props.toggleVisibility} />;
}
export default NoSampleForFocusInstanceWarning;