import React, { useState } from 'react';
import { render, screen, fireEvent, act } from '@testing-library/react';
import "@testing-library/jest-dom";
import { UserContext } from '../../../../GlobalContext';
import { CrosstabSignificanceType, DisplaySignificanceDifferences, SigConfidenceLevel } from 'client/BrandVueApi';
import { store } from 'client/state/store';
import { Provider } from 'react-redux';
import SigDiffSelector, { ISigDiffSelectorProps } from './SigDiffSelector';
import { mockApplicationUser } from '../../../../helpers/ReactTestingLibraryHelpers';

const ControlledSigDiffSelector = (props: Partial<ISigDiffSelectorProps>) => {
    const [highlightSignificance, setHighlightSignificance] = useState(true);
    const [displaySignificanceDifferences, setDisplaySignificanceDifferences] = useState(DisplaySignificanceDifferences.ShowBoth);
    const [significanceType, setSignificanceType] = useState(CrosstabSignificanceType.CompareToTotal);
    const [significanceLevel, setSignificanceLevel] = useState(SigConfidenceLevel.NinetyFive);

    return (
        <Provider store={store}>
            <UserContext.Provider value={mockApplicationUser}>
                <SigDiffSelector
                    highlightSignificance={highlightSignificance}
                    updateHighlightSignificance={setHighlightSignificance}
                    displaySignificanceDifferences={displaySignificanceDifferences}
                    updateDisplaySignificanceDifferences={setDisplaySignificanceDifferences}
                    significanceType={significanceType}
                    setSignificanceType={setSignificanceType}
                    disableSignificanceTypeSelector={false}
                    downIsGood={true}
                    {...props}
                    significanceLevel={significanceLevel}
                    setSignificanceLevel={setSignificanceLevel}
                    isAllVue={true}
                />
            </UserContext.Provider>
        </Provider>
    );
};

describe('SigDiffSelector', () => {
    it('should render a component', () => {
        const { container } = render(<ControlledSigDiffSelector />);
        expect(container).toBeInTheDocument();
    });

    it('should disable dropdown when disableSignificanceTypeSelector is true', () => {
        render(<ControlledSigDiffSelector disableSignificanceTypeSelector={true} />);
        const dropdownToggle = screen.getByTestId('toggle-button');
        expect(dropdownToggle).toBeDisabled();
    });

    it('should disable up/down checkboxes when highlightSignificance is false', () => {
        render(<ControlledSigDiffSelector highlightSignificance={false} />);
        const upCheckbox = screen.getByTestId("upwards-checkbox");
        const downCheckbox = screen.getByTestId("downwards-checkbox");
        expect(upCheckbox).toBeDisabled();
        expect(downCheckbox).toBeDisabled();
    });

    it('should disable significance level dropdown when highlightSignificance is false', () => {
        render(<ControlledSigDiffSelector highlightSignificance={false} />);
        const dropdownToggle = screen.getByTestId('sig-level-toggle-button');
        expect(dropdownToggle).toBeDisabled();
    });

    it('should update state correctly when toggling controls', () => {
        render(<ControlledSigDiffSelector />);
        const highlightCheckbox = screen.getByTestId("significance-checkbox");
        fireEvent.click(highlightCheckbox);
        expect(highlightCheckbox).not.toBeChecked();
    });

    
    it('should disable downwards-checkbox when upwards is deselected', () => {
        render(<ControlledSigDiffSelector displaySignificanceDifferences={DisplaySignificanceDifferences.ShowDown}/>);
        const downCheckbox = screen.getByTestId("downwards-checkbox");
        expect(downCheckbox).toBeDisabled();
    });
});
