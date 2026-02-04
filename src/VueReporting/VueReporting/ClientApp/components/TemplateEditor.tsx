import React from 'react';
import ReportingApi = require("../reportingApi");
import ReportTemplate = ReportingApi.ReportTemplate;
import FileParameter = ReportingApi.FileParameter;
import {
    Row, Col,
    Form, FormGroup, Label, Input, Button, FormText, Card, CardBody,
    CardTitle
    } from 'reactstrap';
import Globals from "../globals";

export class TemplateEditor extends React.Component<{ closed: (saved: boolean) => void; report: ReportTemplate }, { id?: number; name: string; reportTemplate: FileParameter }> {
    constructor(props) {
        super(props);

        if (this.props.report == null) {
            this.state = {
                name: "",
                reportTemplate: { data: null, fileName: null }
            }
        } else {
            this.state = {
                id: this.props.report.id,
                name: this.props.report.name,
                reportTemplate: { data: new Blob([]), fileName: null }
            }
        }

        this.uploadReport = this.uploadReport.bind(this);
        this.cancel = this.cancel.bind(this);
    }

    uploadReport(e) {
        e.preventDefault();

        Globals.ReportApiClient.saveReport(
                this.state.id,
                this.state.name,
                this.state.reportTemplate
            )
            .then(() => this.props.closed(true));

    }

    handleChange(e) {
        const change = {};
        if (e.target.files) {
            change[e.target.name] = { data: e.target.files[0], fileName: e.target.files[0].name };
        } else {
            change[e.target.name] = e.target.value;
        }
        this.setState(change);
    }

    cancel() {
        this.props.closed(false);
    }

    render() {
        const isNewReport = this.props.report === null;
        const titleText = isNewReport ? "Add new report" : "Edit existing report";
        const uploadFileText = isNewReport ? "Powerpoint template saved in OpenXML (.pptx) format."
            : "Optional, use if you want to update existing template.";
        return (
            <Card>
                <CardBody>
                    <CardTitle>{titleText}</CardTitle>
                    <Form onSubmit={this.uploadReport} className="clearfix">
                        <Row>
                            <Col>
                                <FormGroup>
                                    <Label>Report Name</Label>
                                    <Input type="text" name="name" required value={this.state.name} onChange={this.handleChange.bind(this)} />
                                    <FormText>Please enter a name which represents this report.</FormText>
                                </FormGroup>
                            </Col>
                            <Col>
                                <FormGroup>
                                    <Label>Powerpoint template</Label>
                                    <Input type="file" accept={".pptx"} name="reportTemplate" onChange={this.handleChange.bind(this)} required={isNewReport} />
                                    <FormText>{uploadFileText}</FormText>
                                </FormGroup>
                            </Col>
                        </Row>
                        <div className="float-right">
                            <Button className="mr-2">Save</Button>
                            <Button className="float-right" type="button" onClick={this.cancel}>Cancel</Button>
                        </div>
                    </Form>
                </CardBody>
            </Card>
        );
    }
}