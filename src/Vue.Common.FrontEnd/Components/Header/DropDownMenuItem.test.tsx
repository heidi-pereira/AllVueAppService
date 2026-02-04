import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import DropDownMenuItem from './DropDownMenuItem';

const mockTrack = jest.fn();

interface IChildProps {
    track: (eventName: string) => void;
    url: string;
    title: string;
    text: string;
    eventName?: string;
}

interface ITestProps {
    track: (eventName: string) => void;
    url: string;
    title: string;
    text: string;
    showLockIcon: boolean;
    eventName?: string;
    children: Array<IChildProps>;
}

const renderComponent = (props: ITestProps) => {
    return render(<DropDownMenuItem {...props} />);
};

const defaultProps: ITestProps = {
    track: mockTrack,
    url: "",
    title: "",
    text: "",
    showLockIcon: false,
    children: []
};

describe('DropDownMenuItem Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render a li tag', () => {
        const {container} = renderComponent(defaultProps);
        const listItems = container.getElementsByTagName("li");
        expect(listItems.length).toEqual(1);
    });

    it('should include not show a lock icon by default', () => {
        const {container} = renderComponent(defaultProps);
        const icon = screen.queryByTestId("LockIcon");
        expect(icon).not.toBeInTheDocument();
    });

    it('should include a lock icon if flagged', () => {
        const {container} = renderComponent({...defaultProps, showLockIcon: true});
        const icons = screen.getAllByText(/lock/);
        expect(icons).toHaveLength(1);
    });

    it('should render a link with the correct URL', () => {
        const url = "https://example.com";
        const props = { ...defaultProps, url };
        const {container} = renderComponent(props);
        const anchors = container.getElementsByTagName("a");
        expect(anchors.length).toEqual(1);
        const anchor = anchors[0];
        expect(anchor).toHaveAttribute("href", url);
    });

    it('should render a link with the correct title', () => {
        const title = "Survey Name";
        const props = { ...defaultProps, title };
        const {container} = renderComponent(props);
        const anchors = container.getElementsByTagName("a");
        expect(anchors.length).toEqual(1);
        const anchor = anchors[0];
        expect(anchor).toHaveAttribute("title", title);
    });

    it('should render the supplied text', () => {
        const text = "Survey Name";
        const props = { ...defaultProps, text };
        const {container} = renderComponent(props);
        expect(screen.queryByText(text)).toBeInTheDocument();
    });
    
    it('should fallback to the text if no title is provided', () => {
        const text = "Survey Name";
        const props = { ...defaultProps, text };
        const {container} = renderComponent(props);
        const anchors = container.getElementsByTagName("a");
        expect(anchors.length).toEqual(1);
        const anchor = anchors[0];
        expect(anchor).toHaveAttribute("title", text);
    });

    it('should not render child list if empty', () => {
        const {container} = renderComponent(defaultProps);
        const list = container.querySelector("ul");
        expect(list).not.toBeInTheDocument();
    });

    it('should render child list if children are provided', () => {
        const childProps = [
            { url: "https://example.com/child1", title: "Child 1", text: "Child 1" },
        ];
        const {container} = renderComponent({...defaultProps, children: childProps});
        const list = container.querySelector("ul");
        expect(list).toBeInTheDocument();
    });

    it('should correct number of items in child list', () => {
        const childProps = [
            { url: "https://example.com/child1", title: "Child 1", text: "Child 1" },
            { url: "https://example.com/child2", title: "Child 2", text: "Child 2" },
            { url: "https://example.com/child3", title: "Child 3", text: "Child 3" },
        ];
        const {container} = renderComponent({...defaultProps, children: childProps});
        const list = container.querySelector("ul");
        const listItems = list?.getElementsByTagName("li");
        expect(listItems?.length).toEqual(childProps.length);
    });

    it('should not track clicks if no event name is provided', async () => {
        const user = new userEvent.setup();
        const {container} = renderComponent(defaultProps);
        const anchors = container.getElementsByTagName("a");
        expect(anchors.length).toEqual(1);
        await user.click(anchors[0]);
        expect(mockTrack).toHaveBeenCalledTimes(0);
    });

    it('should track clicks if an event name is provided', async () => {
        const user = new userEvent.setup();
        const eventName = "menuItemClicked";
        const {container} = renderComponent({ ...defaultProps, eventName });
        const anchors = container.getElementsByTagName("a");
        expect(anchors.length).toEqual(1);
        await user.click(anchors[0]);
        expect(mockTrack).toHaveBeenCalledTimes(1);
    });
});