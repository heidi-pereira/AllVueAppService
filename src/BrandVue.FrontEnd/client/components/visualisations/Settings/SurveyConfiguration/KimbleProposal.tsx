import React from "react";
import { Factory, KimbleProposal} from "../../../../BrandVueApi";
import { ProductConfiguration } from "../../../../ProductConfiguration";
import Throbber from "../../../throbber/Throbber";
import style from './KimbleProposal.module.less';
import FieldRow from './FieldRow';

interface IKimbleProposal {
    productConfiguration: ProductConfiguration;
}

type KimbleFieldConfig = {
  label: string;
  value: string | undefined;
  showWarning?: boolean;
};

const KimbleProposalPage = (props: IKimbleProposal) => {
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const [kimbleProposal, setKimbleProposal] = React.useState<KimbleProposal | undefined>(undefined);
    const client = Factory.KimbleProposalClient(error => error());

    React.useEffect(() => {
        client.getKimbleProposal().then(details => {
            setKimbleProposal(details)
            setIsLoading(false);
        }).catch(() => setIsLoading(false))
    }, []);

    if (isLoading) {
        return <div id="ld" className="loading-container">
            <Throbber />
        </div>;
    }

    if (kimbleProposal == undefined) {
        return (<div className={style.kimbleInfoContainer}><div className={style.warning }>No Kimble proposal assigned</div></div>)
    }

    const fields: KimbleFieldConfig[] = [
        { label: 'Name', value: kimbleProposal.name },
        { label: 'Description', value: kimbleProposal.description },
        { label: 'Comments', value: kimbleProposal.otherComments },
        { label: 'Type', value: kimbleProposal.engagementType, showWarning: false },
        { label: 'Team', value: kimbleProposal.team, showWarning: false },
        { label: 'Business Unit', value: kimbleProposal.bUandTeam, showWarning: false },
        { label: 'Owner', value: kimbleProposal.ownerEmail, showWarning: false },
        { label: 'Audience', value: kimbleProposal.audience, showWarning: false },
    ];
    return (
        <div className={style.kimbleInfoContainer}>
            <div><h1 className="users-page-title">Kimble details</h1></div>
            <div className={style.row}>
                <div className={style.labelRow}>Id</div>
                <div><a href={`https://savanta.lightning.force.com/lightning/r/KimbleOne__Proposal__c/${kimbleProposal.kimbleProposalId}` } >{kimbleProposal.kimbleProposalId}</a></div>
            </div>
            {fields.map((field, index) => (
                <FieldRow key={index} {...field} />
            ))}
        </div>
    );

}
export default KimbleProposalPage;