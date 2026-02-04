import {EntityInstance, sortEntityInstances} from "../../entity/EntityInstance";
import {EntityInstanceSelector} from "./EntityInstanceSelector";
import React from "react";

const BrandSelector = (props: { brands: EntityInstance[], disabled: boolean, changeBrand: (brandId: string) => void, chosenBrand: number }) => {
    const onChange = (selectedOption: EntityInstance | null) => {
        props.changeBrand(selectedOption ? `${selectedOption.id}` : "");
    }

    const allOptions = [new EntityInstance(EntityInstance.AllInstancesId, "For each chosen brand")].concat(props.brands.sort(sortEntityInstances));
    const isChosenBrandValid = props.chosenBrand || props.chosenBrand === 0;
    let activeValue = isChosenBrandValid ? props.brands.find(x => x.id === props.chosenBrand) || null : null;
    return (
        <span className="me-2">
            <EntityInstanceSelector
                onChange={onChange}
                activeValue={activeValue}
                optionValues={allOptions}
                title={"Brand selector"}/>
        </span>
    );
}

export default BrandSelector