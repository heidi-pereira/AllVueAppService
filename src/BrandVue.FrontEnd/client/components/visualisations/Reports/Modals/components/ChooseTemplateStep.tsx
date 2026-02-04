import React, { useEffect } from 'react';
import Throbber from '../../../../throbber/Throbber';
import { ApplicationConfiguration } from '../../../../../ApplicationConfiguration';
import style from './ChooseTemplateStep.module.less';
import { ReportTemplate } from 'client/BrandVueApi';
import { useAppDispatch, useAppSelector } from 'client/state/store';
import { deleteTemplateById } from 'client/state/templatesSlice';
import FuzzyDate from 'client/components/helpers/FuzzyDate';
import TemplateListItem from './TemplateListItem';
import DeleteModal from 'client/components/DeleteModal';
import toast from 'react-hot-toast';
import { MixPanel } from 'client/components/mixpanel/MixPanel';

interface IChooseTemplateProps {
    isCreatingReport: boolean;
    applicationConfiguration: ApplicationConfiguration;
    reportName: string;
    onBack: () => void;
    onCreate: (selectedTemplateId: number) => void;
}

const ChooseTemplateStep: React.FC<IChooseTemplateProps> = (props) => { 
    const dispatch = useAppDispatch();
    const { templates: existingTemplates, isLoading } = useAppSelector(state => state.templates);
    const [selectedId, setSelectedId] = React.useState<number>(0);
    const selectedTemplate: ReportTemplate | undefined = existingTemplates.find(t => t.id === selectedId) ?? existingTemplates[0] ?? undefined;
    const [deleteConfirmationModalVisible, setDeleteConfirmationModalVisible] = React.useState(false);

    useEffect(() => {
        if (existingTemplates.length > 0) {
            setSelectedId(existingTemplates[0].id);
        }
    }, [existingTemplates]);

    const handleDeleteTemplate = () => {
        if (!selectedTemplate) {
            toast.error("Error: No template selected to delete");
            return;
        }
        dispatch(deleteTemplateById(selectedTemplate.id)).then(() => {
            setDeleteConfirmationModalVisible(false);
            MixPanel.track("reportTemplateDeleted");
            toast.success("Template deleted successfully");
        })
        .catch(() => {
            toast.error("Error: Unable to delete template");
        });
    };

    if (isLoading || props.isCreatingReport) {
        return (
            <div className={style.container}>
                <div className={style.loading}>
                    <Throbber />
                </div>
            </div>
        );
    }

    const toTime = (d: any): number => {
        if (d == null) return 0;
        if (typeof d === 'number') return d;
        if (d instanceof Date) return d.getTime();
        if (typeof d === 'string') {
            const t = Date.parse(d);
            return Number.isNaN(t) ? 0 : t;
        }
        return 0;
    };

    const sortedTemplatesByDate = [...existingTemplates].sort((a, b) => toTime(b.createdAt) - toTime(a.createdAt));

    return (
        <>
            {deleteConfirmationModalVisible && (
                <DeleteModal
                    isOpen={deleteConfirmationModalVisible && !!selectedTemplate}
                    thingToBeDeletedName={selectedTemplate?.templateDisplayName ?? ''}
                    thingToBeDeletedType={"template"}
                    delete={handleDeleteTemplate}
                    closeModal={() => setDeleteConfirmationModalVisible(false)}
                    affectAllUsers={false}
                />
            )}
            <div className="details">2 of 2: Select template</div>
            <div className={style.container}>
                <div className={style.header}>
                    Start with a pre-built template and we’ll automatically match your survey questions
                </div>
                <div className="content-and-buttons">
                    <div className={style.body}>
                        <div className={style.listPane}>
                            <ul className={style.templateList}>
                                {sortedTemplatesByDate.map(t => (
                                    <TemplateListItem
                                        key={t.id}
                                        template={t}
                                        selected={t.id === selectedId}
                                        onSelect={() => setSelectedId(t.id)}
                                        setDeleteConfirmationModalVisible={() => setDeleteConfirmationModalVisible(true)}
                                    />
                                ))}
                            </ul>
                        </div>
                        <div className={style.detailPane}>
                            <div className={style.detailCard}>
                                <div className={style.detailTitle}>
                                    Details
                                </div>
                                <div className={style.detailRow}>
                                    <span>Items</span>
                                    <span className={style.detailValue}>
                                        {selectedTemplate?.reportTemplateParts ? selectedTemplate.reportTemplateParts.length : 0}
                                    </span>
                                </div>
                                <div className={style.detailRow}>
                                    <span>Created on</span>
                                        <span className={style.detailValue}>
                                        <FuzzyDate date={selectedTemplate?.createdAt} lowerCase includePastFuture/>
                                    </span>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="modal-buttons">
                        <button className="modal-button secondary-button" onClick={props.onBack}>Back</button>
                        <button className="modal-button primary-button" onClick={() => props.onCreate(selectedTemplate.id)}>Next</button>
                    </div>
                </div>
            </div>
        </>
    );
};

export default ChooseTemplateStep;