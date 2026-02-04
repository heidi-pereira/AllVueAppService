import React, { useState, useEffect } from 'react';
import moment from "moment";
import { Modal, ModalFooter, ModalHeader, ModalBody, Button } from "reactstrap";
import { Factory, DataClient, IAbstractCommonResultsInformation } from "../BrandVueApi";
import Freshchat from "../freshchat";
import { ApplicationConfiguration } from '../ApplicationConfiguration';

type TrialWarningProps = {
  applicationConfiguration: ApplicationConfiguration
};

const TrialWarning: React.FC<TrialWarningProps> = ({ applicationConfiguration }) => {
  const [showWarning, setShowWarning] = useState<boolean>(false);

  const updateCommonResults = (commonResultsInformation: IAbstractCommonResultsInformation) => {
    if (commonResultsInformation && commonResultsInformation.lowSampleSummary) {
      setShowWarning(commonResultsInformation.trialRestrictedData);
    }
  };

  useEffect(() => {
    // Register handler when component mounts
    Factory.RegisterGlobalResponseHandler(DataClient, updateCommonResults);
    return () => Factory.UnregisterGlobalResponseHandler(DataClient, updateCommonResults);
  }, []);

  const handleHide = () => {
    setShowWarning(false);
  };

  const showFreshChat = (e: React.MouseEvent) => {
    e.preventDefault();
    Freshchat.GetOrCreateWidget().show({ name: 'Talk about trial' });
  };

  return (
    <Modal isOpen={showWarning} toggle={handleHide} centered={true}>
      <ModalHeader toggle={handleHide}>Data restricted for trials</ModalHeader>
      <ModalBody>
        <p>The most recent data is not available on your trial.</p>
        <p>To view all of the data, choose a period on or before <b>{moment.utc(applicationConfiguration.dateOfLastDataPoint).format('MMMM YYYY')}</b> or <a className="link" href={window.location.href.split('?')[0]}>click here</a></p>
        <p>Please <a href="#" onClick={showFreshChat} className="link">contact us</a> to discuss any aspect of your BrandVue trial.</p>
      </ModalBody>
      <ModalFooter>
        <Button className="btn btn-tour" onClick={handleHide}>Close</Button>
      </ModalFooter>
    </Modal>
  );
};

export default TrialWarning;