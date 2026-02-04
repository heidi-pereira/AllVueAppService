import React from 'react';
import userEvent from "@testing-library/user-event";
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import TabLink from './TabLink';

const mockTrack = jest.fn();

interface ITestProps {
    track: (event: string) => void;
    url: string;
    text: string;
    icon?: string;
    isActive?: boolean;
    className?: string;
    noFill?: boolean;
    eventName?: string;
}

const defaultProps: ITestProps = {
    track: mockTrack,
    url: "",
    text: "",
};

const renderComponent = (props: ITestProps) => {
    return render(<TabLink {...props} />);
};


describe('TabLink Component', () => {
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
        expect(firstChild).toHaveClass("tab-link");
    });

    it('should render a link with the correct href', () => {
        const url = "https://example.com";
        const {container} = renderComponent({...defaultProps, url});
        const firstChild = container.children[0];
        expect(firstChild).toHaveAttribute("href", url);
    });

    it('should render no icon by default', () => {
        const {container} = renderComponent(defaultProps);
        const icons = container.getElementsByTagName("i");
        expect(icons.length).toEqual(0);
    });

    it('should render the correct icon', () => {
        const icon = "donut_large";
        const {container} = renderComponent({...defaultProps, icon});
        const icons = container.getElementsByTagName("i");
        expect(icons).toHaveLength(1);
        const firstIcon = icons[0];
        expect(firstIcon).toHaveClass("material-symbols-outlined");
        expect(firstIcon).toHaveTextContent(icon);
    });

    it('should render the correct text', () => {
        const text = "Test Link";
        const {container} = renderComponent({...defaultProps, text});
        const spans = container.getElementsByTagName("span");
        expect(spans.length).toEqual(1);
        expect(spans[0]).toHaveTextContent(text);
    });

    it('should mark the link as not current by default', () => {
        const {container} = renderComponent(defaultProps);
        const firstChild = container.children[0];
        expect(firstChild).toHaveAttribute("aria-current", "false");
    });

    it('should mark the links as active and current if set to isActive', () => {
        const {container} = renderComponent({...defaultProps, isActive: true});
        const firstChild = container.children[0];
        expect(firstChild).toHaveAttribute("aria-current", "true");
        expect(firstChild).toHaveClass("active");
    });

    it('should add a custom class name if provided', () => {
        const customClass = "custom-class";
        const {container} = renderComponent({...defaultProps, className: customClass});
        const firstChild = container.children[0];
        expect(firstChild).toHaveClass(customClass);
    });

    it('should set nofill correctly on icon', () => {
        const icon = "donut_large";
        const {container} = renderComponent({...defaultProps, icon, noFill: true});
        const icons = container.getElementsByTagName("i");
        expect(icons).toHaveLength(1);
        expect(icons[0]).toHaveClass("no-symbol-fill");
    });

    it('should not call track function on click if eventName is not provided', async () => {
        const user = userEvent.setup();
        const {container} = renderComponent({...defaultProps, url: "https://example.com"});
        const firstChild = container.children[0];
        await user.click(firstChild);
        expect(mockTrack).not.toHaveBeenCalled();
    });

    it('should call track function on click if eventName is provided', async () => {
        const eventName = "test_event";
        const user = userEvent.setup();
        const {container} = renderComponent({...defaultProps, url: "https://example.com", eventName});
        const firstChild = container.children[0];
        await user.click(firstChild);
        expect(mockTrack).toHaveBeenCalledTimes(1);
        expect(mockTrack).toHaveBeenCalledWith(eventName);
    });
});