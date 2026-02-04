import {Modal, ModalBody, ModalHeader} from 'reactstrap';
import { IFeatureModel } from '../../../../BrandVueApi';
import FeatureEditUrlControl from './FeatureEditUrlControl';
import FeatureEditTextControl from './FeatureEditTextControl';
import Tooltip from '../../../Tooltip';
import style from './Features.module.less';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { fetchFeatures, updateFeature } from '../../../../state/featuresSlice';

interface IEditFeatureModalProps {
    isOpen: boolean;
    featureToEdit: number;
    isReadOnly: boolean;
    setIsOpen: (isOpen: boolean) => void;
}

const EditFeatureModal = (props: IEditFeatureModalProps) => {
    const dispatch = useAppDispatch();
    const { features } = useAppSelector((state) => state.features);

    const featureToEdit = features.find(f => f.id === props.featureToEdit);

    const onChangeDocumentationURL = (newValue: string) => {
        if (featureToEdit != undefined) {
            dispatch(updateFeature({ ...featureToEdit, documentationUrl: newValue }));
        }
    }

    const onChangeName = (newValue: string) => {
        if (featureToEdit != undefined) {
            dispatch(updateFeature({ ...featureToEdit, name: newValue }));
        }
    }

    const toggle = () => {
        props.setIsOpen(!props.isOpen);
        dispatch(fetchFeatures());
    };
    
    return (
    <Modal isOpen={props.isOpen} toggle={toggle}>
        <ModalHeader toggle={toggle}>{`Edit feature '${featureToEdit?.name}'`}</ModalHeader>
        {featureToEdit != undefined &&
        <ModalBody>
            <div className={style.editFeatureSection}>
                Name
                <FeatureEditTextControl isReadOnly={props.isReadOnly} feature={featureToEdit} initialValue={featureToEdit.name} onSave={onChangeName} />
            </div>
            <div className={style.editFeatureSection}>
                Code
                {!featureToEdit.isInDatabase &&
                    <Tooltip placement="top" title={`This is a new feature that is not in the database yet`} >
                        <span>{featureToEdit.featureCode}</span>
                    </Tooltip>
                }
                {!featureToEdit.isInEnum &&
                    <Tooltip placement="top" title={`This is an old feature that is no longer in the code so can probably be deleted`} >
                        <span>{featureToEdit.featureCode}</span>
                    </Tooltip>
                }
                {featureToEdit.isInDatabase && featureToEdit.isInEnum &&
                    <p>{featureToEdit.featureCode}</p>
                }
            </div>
            <div className={style.editFeatureSection}>
                Documentation URL
                <FeatureEditUrlControl isReadOnly={props.isReadOnly} feature={featureToEdit} onSaveDocumentationURL={onChangeDocumentationURL} />
            </div>
        </ModalBody>
        }
    </Modal>
    );
}
export default EditFeatureModal;