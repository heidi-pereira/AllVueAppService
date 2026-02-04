import { IFeature } from '../../../../BrandVueApi';
import FeatureEditTextControl from './FeatureEditTextControl';

interface IFeatureEditUrlControlProps {
    isReadOnly: boolean;
    feature: IFeature;
    onSaveDocumentationURL(url: string): void;
}

const FeatureEditUrlControl = (props: IFeatureEditUrlControlProps) => {
    const onChangeDocumentationURL = (newValue: string) => {
        props.onSaveDocumentationURL(newValue);
    }

    return <div style={{ "display": "flex" }}>
        {props.feature.documentationUrl &&
            <a style={{ color: "#1376cd" }} href={props.feature.documentationUrl} target="_blank">
                <i className="material-symbols-outlined">help</i>
            </a>
        }
        {!props.feature.documentationUrl &&
            <i className="material-symbols-outlined disabled">help</i>
        }

        <FeatureEditTextControl isReadOnly={props.isReadOnly} feature={props.feature} initialValue={props.feature.documentationUrl} onSave={onChangeDocumentationURL} />

    </div>
}

export default FeatureEditUrlControl;