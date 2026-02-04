import React from 'react';
import { RouteComponentProps } from 'react-router';
import { Table, Button } from 'reactstrap';
import { EntitySet, ReportTemplate } from "../reportingApi";
import QueryString from 'query-string';
import Moment from 'moment';
import { ReportGenerator } from "./ReportGenerator";
import ReportingApi = require("../reportingApi");
import Globals from "../globals";
import { TemplateEditor } from "./TemplateEditor";
import { saveAs } from 'file-saver';
import { Queue } from './Queue';

export class Templates extends React.Component<{}, { reports: ReportTemplate[], showEditor: boolean, selectedReport: ReportTemplate, generating: boolean, subsetId: string, brandSets: EntitySet[] }> {
    constructor(props: RouteComponentProps<{}>) {
        super(props);

        this.state = {
            generating: false,
            reports: [],
            showEditor: false,
            selectedReport: null,
            subsetId: null,
            brandSets: []
        };

        this.exportReport = this.exportReport.bind(this);
        this.loadReports = this.loadReports.bind(this);
        this.toggleAddNew = this.toggleAddNew.bind(this);
    }

    componentDidMount() {
        this.loadReports();
        const subsetId = QueryString.parse(window.parent.location.search).Subset || null;
        Globals.ReportApiClient.getBrandSets(subsetId).then(r => this.setState({ subsetId: subsetId, brandSets: r }));
    }

    componentDidUpdate(prevProps, prevState) {
        const subsetId = QueryString.parse(window.parent.location.search).Subset || null;
        if (subsetId !== prevState.subsetId) {
            Globals.ReportApiClient.getBrandSets(subsetId)
                .then(r => this.setState({ subsetId: subsetId, brandSets: r }));
        }
    }

    loadReports() {
        Globals.ReportApiClient.getReports().then(data => this.setState({ reports: data }));
    }

    deleteReport(report: ReportTemplate) {
        if (confirm("Are you sure you want to delete this report?")) {
            Globals.ReportApiClient.deleteReport(report.id).then(this.loadReports);
        }
    }

    exportReport(report: ReportTemplate) {
        Globals.ReportApiClient.exportReport(report.id)
            .then(r => {
                const fileName = r.fileName !== undefined ? r.fileName : 'template - Private.pptx';
                saveAs(r.data, fileName);
            });
    }

    editReport(report: ReportTemplate) {
        this.setState({ showEditor: true, selectedReport: report });
    }

    toggleAddNew(reload: boolean) {
        this.setState({ showEditor: !this.state.showEditor, selectedReport: null });
        if (reload) {
            this.loadReports();
        }
    }

    getMeta(report: ReportTemplate) {
        Globals.ReportApiClient.getReportMeta(report.id)
            .then(m => console.log(m));
    }

    render() {

        return (
            <div className="clearfix">
                <Queue />
                <hr className="spaced-hr" />
                <Table>
                    <thead>
                        <tr>
                            <th>Report name</th>
                            <th>Charts</th>
                            <th>Owner</th>
                            <th>Created</th>
                            <th>Last modified</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {this.state.reports.map(r => {

                            if (this.state.showEditor && r === this.state.selectedReport) {
                                return (
                                    <tr key={r.id}><td colSpan={4}><TemplateEditor report={this.state.selectedReport} closed={this.toggleAddNew} /></td></tr>
                                );
                            } else {
                                return (
                                    <tr key={r.id}>
                                        <td>{r.name}</td>
                                        <td>{r.metaDescription}</td>
                                        <td>{r.userName}</td>
                                        <td>{Moment(r.dateCreated).format()}</td>
                                        <td>{Moment(r.dateModified).format()}</td>
                                        <td className="text-nowrap text-right">
                                            <Button title="Download template" onClick={(e) => e.ctrlKey ? this.getMeta(r) : this.exportReport(r)} className="mr-2"><i className="material-icons">file_download</i></Button>

                                            <ReportGenerator report={r} className="mr-2" brandSets={this.state.brandSets} subsetId={this.state.subsetId} />

                                            <Button title="Edit report" onClick={() => this.editReport(r)} className="mr-2"><i className="material-icons">edit</i></Button>

                                            <Button title="Delete report" onClick={() => this.deleteReport(r)}><i className="material-icons">delete</i></Button>

                                        </td>
                                    </tr>);
                            }
                        })
                        }
                    </tbody>
                </Table>
                {this.state.showEditor && this.state.selectedReport === null &&
                    <TemplateEditor report={this.state.selectedReport} closed={this.toggleAddNew} />
                }
                {!this.state.showEditor &&
                    <Button className="float-right" onClick={() => this.toggleAddNew(false)}>Add new</Button>
                }
            </div>
        );
    }
}
