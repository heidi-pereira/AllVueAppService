import React from 'react';
import userEvent from "@testing-library/user-event";
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import WarningBanner from './WarningBanner';

interface ITestProps {
    message?: string;
    icon?: string;
}

const defaultProps: ITestProps = {
    message: "This is a warning message",
    icon: "warning-icon",
};

const renderComponent = (props: ITestProps) => {
    return render(<WarningBanner {...props} />);
};

describe('WarningBanner Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render nothing when there is no message', () => {
        const {container} = renderComponent({});
        expect(container).toBeEmptyDOMElement();
    });

    it('should render a banner div when there is a message', () => {
        const {container} = renderComponent(defaultProps);
        const bannerDiv = container.getElementsByClassName("warning-banner");
        expect(bannerDiv).toHaveLength(1);
    });

    it('should display an icon if one is provided', () => {
        const {container} = renderComponent(defaultProps);
        const bannerDiv = container.getElementsByClassName("warning-banner");
        const icons = container.getElementsByTagName("i")
        expect(icons.length).toBeGreaterThanOrEqual(1);
        expect(icons[0]).toHaveTextContent(defaultProps.icon!);
    });

    it('should display the message text', () => {
        const {container} = renderComponent(defaultProps);
        const messageDivs = container.getElementsByClassName("message");
        expect(messageDivs).toHaveLength(1);
        expect(messageDivs[0]).toHaveTextContent(defaultProps.message!);
    });

    it('should show a close button', () => {
        const {container} = renderComponent(defaultProps);
        const closeButtons = container.getElementsByClassName("remove-button");
        expect(closeButtons).toHaveLength(1);
        const icons = closeButtons[0].getElementsByTagName("i");
        expect(icons).toHaveLength(1);
        expect(icons[0]).toHaveTextContent("close");
    });

    it('should remove the banner when the close button is clicked', async () => {
        const {container} = renderComponent(defaultProps);
        const user = userEvent.setup();
        const closeButtons = container.getElementsByClassName("remove-button");
        expect(closeButtons).toHaveLength(1);
        await user.click(closeButtons[0]);
        expect(container).toBeEmptyDOMElement();
    });
});