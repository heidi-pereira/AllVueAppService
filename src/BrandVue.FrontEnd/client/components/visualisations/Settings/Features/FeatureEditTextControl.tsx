import React from 'react';
import { useState } from 'react';
import { IFeature } from '../../../../BrandVueApi';

interface IFeatureEditTextControlProps {
    isReadOnly: boolean;
    feature: IFeature;
    initialValue: string;
    onSave(value: string): void;
}

const FeatureEditTextControl = (props: IFeatureEditTextControlProps) => {
    const [textValue, setTextValue] = useState(props.initialValue);

    const onChange = (newValue: string) => {
        if (props.initialValue != newValue) {
            props.onSave(newValue);
        }
    }

    return <input type="textbox"
            style={{ "width": "100%" }}
            disabled={props.isReadOnly}
            value={textValue}
            onChange={(e) => setTextValue(e.target.value)}
            onBlur={() => onChange(textValue)}
        />
}

export default FeatureEditTextControl;