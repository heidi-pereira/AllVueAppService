import React from 'react';
import { Form, FormGroup, Button, Modal, ModalHeader, ModalBody, ModalFooter, Label, Input } from 'reactstrap';
import ReportingApi = require("../reportingApi");
import ReportTemplate = ReportingApi.ReportTemplate;
import Globals from "../globals";
import moment from 'moment';
import { saveAs } from 'file-saver';
import { EntitySet } from "../reportingApi";

export class ReportGenerator extends React.Component<
    { report: ReportTemplate, className: string, brandSets: EntitySet[], subsetId: string },
    { modal: boolean, selectedBrandSets: string[], selectedOrganisation: string, generating: boolean, currentBrands: boolean, originalBrands: boolean, reportDate: string }> {
    
    
    constructor(props) {
        super(props);

        this.months = Array.from(Array(12).keys()).map(m => moment().subtract(m + 1, 'months').format('MMM YYYY'));
        this.organisations = this.props.brandSets.map(set => set.organisation).filter(this.onlyUnique);
        
        this.state = {
            modal: false,
            selectedBrandSets: [],
            generating: false,
            currentBrands: false,
            originalBrands: false,
            reportDate: this.months[0],
            selectedOrganisation: this.organisations[0],
        };

        this.toggle = this.toggle.bind(this);
        this.generate = this.generate.bind(this);
    }

    componentDidUpdate(prevProps, prevState) {
        if (prevProps.brandSets.length !== this.props.brandSets.length) {
            this.organisations = this.props.brandSets.map(set => set.organisation).filter(this.onlyUnique);
            this.setState({selectedOrganisation: this.organisations[0]});
        }
    }

    savantaOrgName: string = "savanta";
    months: string[];
    organisations: string[];

    generate() {
        this.setState({generating: true});

        const reportDate = moment(this.state.reportDate + " UTC", "MMM YYYY").endOf('month').toDate();
        Globals.ReportApiClient.generateAndSaveReportForBrandSets(this.props.report.id, this.state.selectedBrandSets, this.state.currentBrands, this.state.originalBrands, reportDate, this.props.subsetId)
            .then(() => {
                this.toggle();
            })
            .catch(r => {
                console.log(r);
                alert('An error has occurred, please try again. If the error continues, please contact support.');
                this.toggle();
            });
    }
    
    toggle() {
        this.setState({ modal: !this.state.modal, generating: false, selectedBrandSets: [], reportDate: this.months[0]});
    }

    selectBrandSet(brandSetName: string, add: boolean) {
        var selectedBrandSets = this.state.selectedBrandSets;
        if (add) {
            selectedBrandSets.push(brandSetName);
        } else {
            selectedBrandSets.splice(selectedBrandSets.indexOf(brandSetName, 0), 1);
        }
        this.setState({selectedBrandSets: selectedBrandSets});
    }

    onlyUnique(value, index, self) {
        return value && self.indexOf(value) === index;
    }
    
    getAvailableBrandSets() {
        if (this.state.selectedOrganisation === this.savantaOrgName) {
            return this.props.brandSets.filter(set => set.organisation == this.state.selectedOrganisation || !set.organisation);
        }
        
        return this.props.brandSets.filter(set => set.organisation == this.state.selectedOrganisation);
    }
    
    render() {
        const availableBrandSets = this.getAvailableBrandSets();
        
        const brandSetsRender = this.props.brandSets.length > 0
            ? availableBrandSets.map(b => {
                const brandSetName = b.name;
                return (
                    <FormGroup key={brandSetName} check className="mr-3" inline>
                        <Label check>
                            <Input type="checkbox" checked={this.state.selectedBrandSets.indexOf(brandSetName) >= 0} onChange={(e) =>
                                this.selectBrandSet(brandSetName, (e.target as HTMLInputElement).checked)}/>{' '}
                            {brandSetName}
                        </Label>
                    </FormGroup>
                );
            })
            : <div>Loading...</div>;

        return (
            <span>
                <Button title="Generate reports" className={this.props.className} onClick={this.toggle}><i className="material-icons">playlist_play</i></Button>
                <Modal isOpen={this.state.modal} toggle={this.toggle} className={"brandSetChooserModal"}>
                    <ModalHeader toggle={this.toggle}>Generate report from <b>{this.props.report.name}</b></ModalHeader>
                    <ModalBody>
                        <Form>
                            <fieldset className="form-inline mb-3">
                                <FormGroup inline>
                                    <Label>
                                        Report month: 
                                        <Input type="select" onChange={(e) => this.setState({ reportDate: e.target.value })}>
                                            {this.months.map(m => <option key={m}>{m}</option>)}
                                        </Input>
                                    </Label>
                                </FormGroup>
                            </fieldset>
                            <fieldset className="mb-3">
                                <FormGroup check>
                                    <Label check>
                                        <Input type="checkbox" checked={this.state.currentBrands} onChange={(e) => this.setState({ currentBrands: (e.target as HTMLInputElement).checked })} /> Currently selected brands
                                    </Label>
                                </FormGroup>
                                <FormGroup check className="mb-2">
                                    <Label check>
                                        <Input type="checkbox" checked={this.state.originalBrands} onChange={(e) => this.setState({ originalBrands: (e.target as HTMLInputElement).checked })} /> Originally exported brands
                                    </Label>
                                </FormGroup>
                            </fieldset>
                            <div className="mb-3">
                                <div>Selected sets:</div>
                                {this.state.selectedBrandSets.join(", ")}
                            </div>
                            <fieldset className="mb-3">
                                <FormGroup>
                                    <Label>
                                        Organisation:
                                        <Input type="select" onChange={(e) => this.setState({selectedOrganisation: e.target.value})}>
                                            {this.organisations.map(org => <option key={org}>{org}</option>)}
                                        </Input>
                                    </Label>
                                </FormGroup>
                            </fieldset>
                            <fieldset className="mb-2">
                                {brandSetsRender}
                            </fieldset>
                        </Form>
                    </ModalBody>
                    <ModalFooter>
                        <Button color="primary" onClick={this.generate} disabled={this.state.generating}>{this.state.generating ? "Generating..." : "Generate"}</Button>{' '}
                        <Button color="secondary" onClick={this.toggle} disabled={this.state.generating}>Cancel</Button>
                    </ModalFooter>
                </Modal>
            </span>
        );
    }
}
