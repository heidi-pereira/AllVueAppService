import "@Styles/main.scss";
import "@Styles/surveyDetailsPage.scss";
import React from 'react';
import { IQuota, IQuotaCell, PermissionFeaturesOptions } from '../CustomerPortalApi';
import { SurveyDetailsCard, IDetailRow } from '@Cards/SurveyDetailsCard';
import { useDataLoader } from '@Store/dataLoading';
import responses from "@Images/responses.png";
import timer from "@Images/timer.png";
import target from "@Images/target.png";
import { CircularProgressbarWithChildren } from 'react-circular-progressbar';
import { useParams } from "react-router";
import Dropdown from "@Components/Dropdown";
import Masonry, { ResponsiveMasonry } from "react-responsive-masonry"
import { ActionEventName, GoogleTagManager } from '../util/googleTagManager';
import FeatureGuard from "@Components/FeatureGuard/FeatureGuard";

enum QuotaSortOrder {
    Alphabetical,
    Script
};

const SurveyQuotasPage = (props: { googleTagManager: GoogleTagManager }) => {
    const params = useParams();
    const surveyId = parseInt(params.id);
    const [quotaSortOrder, setQuotaSortOrder] = React.useState<QuotaSortOrder>(QuotaSortOrder.Alphabetical);
    const selectedSurvey = useDataLoader((c) => c.getSurveyDetails(surveyId));

    const breakpointColumnsObj = {
        0: 1,
        768: 2
    };

    React.useEffect(() => {
        if (selectedSurvey?.subProductId) {
            props.googleTagManager.addEvent("quotasView", selectedSurvey?.organisationShortCode, selectedSurvey?.subProductId);
        }
    }, [selectedSurvey]);

    const getDetailRowFromQuotaCell = (q: IQuotaCell) => {
        return {
            name: q.name,
            complete: q.complete,
            target: q.target
        } as IDetailRow;
    }

    const renderQuota = (surveyQuota: IQuota) => {
        const quotaCells = sortQuotaCells(surveyQuota.quotaCells);
        return (
            <SurveyDetailsCard key={surveyQuota.id} name={surveyQuota.name} details={quotaCells.map(getDetailRowFromQuotaCell)} />
        );
    }

    const updateQuotaSortOrder = (sortOrder: QuotaSortOrder) => {
        const event: ActionEventName = sortOrder == QuotaSortOrder.Alphabetical ? 'quotasSortAlphabetical' : 'quotasSortScript';
        props.googleTagManager.addEvent(event, selectedSurvey?.organisationShortCode, selectedSurvey?.subProductId);
        setQuotaSortOrder(sortOrder);
    }

    const getCompletionGap = () => {
        let delta = selectedSurvey.target - selectedSurvey.complete;
        return delta >= 0 ? delta : 0;
    }

    const getSurveyTargetText = () => {
        if (selectedSurvey.target < 0) {
            return 'Not set';
        }
        return selectedSurvey.target.toLocaleString();
    }

    const sortQuotaCells = (quotaCells: IQuotaCell[] | undefined): IQuotaCell[] => {
        if (quotaCells) {
            if (quotaSortOrder === QuotaSortOrder.Alphabetical) {
                return [...quotaCells].sort((a, b) => (a.name ?? "").localeCompare(b.name ?? ""));
            } else if (quotaSortOrder === QuotaSortOrder.Script) {
                return [...quotaCells].sort((a, b) => (a.choiceSetId ?? 0) - (b.choiceSetId ?? 0));
            }
        }
        return quotaCells ?? [];
    }

    const quotaSortOrderToString = (order: QuotaSortOrder): string => {
        switch (order) {
            case QuotaSortOrder.Alphabetical: return "Alphabetical order";
            case QuotaSortOrder.Script: return "Script order";
            default: throw Error(`Unhandled sort order: ${order}`);
        }
    }

    return (
        <FeatureGuard permissions={[PermissionFeaturesOptions.QuotasAccess]}
            fallback={<div className="survey-documents"><div className="documents-none">You do not have permission to view this page.</div></div>}>
            {selectedSurvey &&
                <><div className='quota-survey-info'>
                    <div className="quota-card">
                        <div className="progress-block">
                            <CircularProgressbarWithChildren className={`survey-progress-percentage-circle ${selectedSurvey.percentComplete > 100 ? "full" : ""}`} value={selectedSurvey.percentComplete} counterClockwise={true} strokeWidth={6} >
                                <div className="progress-value">{selectedSurvey.percentComplete}%</div>
                                <label>complete</label>
                            </CircularProgressbarWithChildren>
                        </div>
                        <div className="divider"></div>
                        <div className="mobile-block">
                            <div className="response-block">
                                <img className="info-icon" src={responses}></img>
                                <div className="block-value">{selectedSurvey.complete.toLocaleString()}</div>
                                <label>responses</label>
                            </div>
                            <div className="target-block">
                                <img className="info-icon" src={target}></img>
                                <div className="block-value">{getSurveyTargetText()}</div>
                                <label>target</label>
                            </div>
                            <div className="gap-block">
                                <img className="info-icon" src={timer}></img>
                                <div className="block-value">{getCompletionGap().toLocaleString()}</div>
                                <label>remaining</label>
                            </div>
                        </div>
                    </div>
                </div>
                    {selectedSurvey.quota.length > 0 &&
                        <div className="quota-cell-sort-container">
                            <Dropdown buttonTitle={"Sort by"}
                                options={[QuotaSortOrder.Alphabetical, QuotaSortOrder.Script]}
                                selectedOption={quotaSortOrder}
                                getDisplayText={quotaSortOrderToString}
                                setSelectedOption={updateQuotaSortOrder} />
                        </div>
                    }
                    <div className="my-masonry-grid">
                        <ResponsiveMasonry columnsCountBreakPoints={breakpointColumnsObj}>
                            <Masonry gutter="30px">
                                {selectedSurvey.quota.map(q => renderQuota(q))}
                            </Masonry>
                        </ResponsiveMasonry>
                    </div>
                </>
            }
        </FeatureGuard>
    );
}

export default (SurveyQuotasPage);