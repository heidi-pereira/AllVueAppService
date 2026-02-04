import React from 'react';
import userEvent from "@testing-library/user-event";
import { render, screen } from "@testing-library/react";
import {expect, jest, test, it, describe} from '@jest/globals';
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import HelpButton from './HelpButton';

const mockTrack = jest.fn();

interface ITestProps {
    track: (event: string) => void;
    url: string;
}

const renderComponent = (props: ITestProps) => {
    return render(<HelpButton {...props} />);
};

const defaultProps: ITestProps = {
    track: mockTrack,
    url: "/help",
};

describe('HelpButton Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render nothing when no URL is provided', () => {
        const {container} = renderComponent({...defaultProps, url: ""});
        expect(container).toBeEmptyDOMElement();
    });

    it('should render an a tag', () => {
        const {container} = renderComponent(defaultProps);
        const anchors = container.getElementsByTagName("a");
        expect(anchors.length).toEqual(1);
    });

    it('should use the provided URL', () => {
        const url = "https://example.com";
        const props = { ...defaultProps, url };
        const {container} = renderComponent(props);
        const anchor = container.getElementsByTagName("a")[0];
        expect(anchor).toHaveAttribute("href", url);
    });

    it('should have the correct class on anchor for styling', () => {
        const {container} = renderComponent(defaultProps);
        const anchor = container.getElementsByTagName("a")[0];
        expect(anchor).toHaveClass("circular-nav-button");
    });

    it('should use a default title attribute', () => {
        const title = "Help";
        const {container} = renderComponent(defaultProps);
        const anchor = container.getElementsByTagName("a")[0];
        expect(anchor).toHaveAttribute("title", title);
    });

    it('should open in a new tab', () => {
        const {container} = renderComponent(defaultProps);
        const anchor = container.getElementsByTagName("a")[0];
        expect(anchor).toHaveAttribute("target", "_blank");
    });

    it('should render a div for the help icon', () => {
        const {container} = renderComponent(defaultProps);
        const divs = container.getElementsByTagName("div");
        expect(divs.length).toEqual(1);
        const firstDiv = divs[0];
        expect(firstDiv).toHaveClass("circle");
    });

    it('should have a span for the label', () => {
        const {container} = renderComponent(defaultProps);
        const spans = container.getElementsByTagName("span");
        expect(spans.length).toEqual(1);
        const firstSpan = spans[0];
        expect(firstSpan).toHaveClass("text");
        expect(firstSpan).toHaveTextContent(/^Help$/);
    });

    it('should call track function when clicked', async () => {
        const user = userEvent.setup();
        const {container} = renderComponent(defaultProps);
        await user.click(screen.getByRole("link"));
        expect(mockTrack).toHaveBeenCalled();
    });
});