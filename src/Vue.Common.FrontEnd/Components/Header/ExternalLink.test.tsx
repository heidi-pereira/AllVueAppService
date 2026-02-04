import React from 'react';
import userEvent from "@testing-library/user-event";
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import ExternalLink from './ExternalLink';

const mockTrack = jest.fn();

interface ITestProps {
    track: (event: string) => void;
    url: string;
    text: string;
    eventName?: string;
}

const defaultProps: ITestProps = {
    track: mockTrack,
    url: "",
    text: "",
};

const renderComponent = (props: ITestProps) => {
    return render(<ExternalLink {...props} />);
};


describe('ExternalLink Component', () => {
    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render an a tag with the correct class', () => {
        const {container} = renderComponent(defaultProps);
        const children = container.children;
        expect(children.length).toEqual(1);
        const firstChild = children[0];
        expect(firstChild.tagName).toEqual("A");
        expect(firstChild).toHaveClass("open-survey-link");
    });

    it('should render an a tag with the correct target and rel', () => {
        const {container} = renderComponent(defaultProps);
        const children = container.children;
        expect(children.length).toEqual(1);
        const firstChild = children[0];
        expect(firstChild.tagName).toEqual("A");
        expect(firstChild).toHaveAttribute("target", "_blank");
        expect(firstChild).toHaveAttribute("rel", "noopener noreferrer");
    });

    it('should render an a tag with the correct href', () => {
        const url = "https://www.savanta.com/";
        const {container} = renderComponent({...defaultProps, url});
        const children = container.children;
        expect(children.length).toEqual(1);
        const firstChild = children[0];
        expect(firstChild.tagName).toEqual("A");
        expect(firstChild).toHaveAttribute("href", url);
    });

    it('should render the text in a span with the correct class', () => {
        const text = "Open Survey";
        const {container} = renderComponent({...defaultProps, text});
        const children = container.children;
        expect(children.length).toEqual(1);
        const firstChild = children[0];
        expect(firstChild.tagName).toEqual("A");
        const span = firstChild.getElementsByTagName("span");
        expect(span.length).toEqual(1);
        expect(span[0]).toHaveClass("link-text");
        expect(span[0]).toHaveTextContent(text);
    });

    it('should render an icon with the correct class', () => {
        const icon = "open_in_new";
        const {container} = renderComponent({...defaultProps});
        const children = container.children;
        expect(children.length).toEqual(1);
        const firstChild = children[0];
        expect(firstChild.tagName).toEqual("A");
        const icons = firstChild.getElementsByTagName("i");
        expect(icons.length).toEqual(1);
        expect(icons[0]).toHaveClass("material-symbols-outlined");
        expect(icons[0]).toHaveTextContent(icon);
    });

    it('should not call track function on click if eventName is not supplied', async () => {
        const user = userEvent.setup();
        const {container} = renderComponent(defaultProps);
        const firstChild = container.children[0];
        await user.click(firstChild);
        expect(mockTrack).not.toHaveBeenCalled();
    });

    it('should call track function on click if eventName is supplied', async () => {
        const user = userEvent.setup();
        const eventName = "external_link_clicked";
        const {container} = renderComponent({...defaultProps, eventName});
        const firstChild = container.children[0];
        await user.click(firstChild);
        expect(mockTrack).toHaveBeenCalledTimes(1);
        expect(mockTrack).toHaveBeenCalledWith(eventName);
    });
});