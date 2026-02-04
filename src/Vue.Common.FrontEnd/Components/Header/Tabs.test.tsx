import React from 'react';
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import Tabs from './Tabs';
import { ITabLink } from '../Types/ITabLink';
import { IExternalLink } from '../Types/IExternalLink';

const mockTrack = jest.fn();

const mockTabLink = jest.fn();
jest.mock("./TabLink", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockTabLink(props);
        return <a data-testid="mock-tablink"></a>;
    })
  };
});

const mockExternalLink = jest.fn();
jest.mock("./ExternalLink", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockExternalLink(props);
        return <a data-testid="mock-externallink"></a>;
    })
  };
});

interface ITestProps {
    track: (event: string) => void;
    tabs?: Array<ITabLink>;
    externalLinks?: Array<IExternalLink>;
}

const defaultProps: ITestProps = {
    track: mockTrack,
    tabs: [],
    externalLinks: [],
};

const renderComponent = (props: ITestProps) => {
    return render(<Tabs {...props} />);
};

describe('Tabs Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render nothing when there are no tabs or external links', () => {
        const {container} = renderComponent({});
        expect(container).toBeEmptyDOMElement();
    });

    it('should render a div tag with the correct class', () => {
        const {container} = renderComponent(defaultProps);
        const divs = container.children;
        expect(divs).toHaveLength(1);
        const firstDiv = divs[0];
        expect(firstDiv.tagName).toEqual("DIV");
        expect(firstDiv).toHaveClass("tabs");
    });

    it('should render 2 link container divs', () => {
        const {container} = renderComponent(defaultProps);
        const topDiv = container.children[0];
        const linkContainers = topDiv.children;
        expect(linkContainers).toHaveLength(2);

        const firstDiv = linkContainers[0];
        expect(firstDiv.tagName).toEqual("DIV");
        expect(firstDiv).toHaveClass("tab-link-container");
        const secondDiv = linkContainers[1];
        expect(secondDiv.tagName).toEqual("DIV");
        expect(secondDiv).toHaveClass("links-to-savanta");
    });

    it('should render no TabLink components by default', () => {
        const {container} = renderComponent(defaultProps);
        const tabLinks = screen.queryAllByTestId("mock-tablink");
        expect(tabLinks).toHaveLength(0);
    });

    it('should render the correct number of TabLink components', () => {
        const tabs = [
            { url: "https://example.com", text: "Tab 1" },
            { url: "https://example.com", text: "Tab 2" },
            { url: "https://example.com", text: "Tab 3" },
        ];
        const {container} = renderComponent({...defaultProps, tabs});
        const tabLinks = screen.queryAllByTestId("mock-tablink");
        expect(tabLinks).toHaveLength(tabs.length);
        expect(mockTabLink).toHaveBeenCalledTimes(tabs.length);
        tabs.forEach((tab, index) => {
            expect(mockTabLink).toHaveBeenNthCalledWith(index + 1, expect.objectContaining({
                track: mockTrack,
                url: tab.url,
                text: tab.text,
            }));
        });
    });

    it('should render no external links by default', () => {
        const {container} = renderComponent(defaultProps);
        const links = screen.queryAllByTestId("mock-externallink");
        expect(links).toHaveLength(0);
    });

    it('should render the correct number of external links', () => {
        const externalLinks = [
            { url: "https://example.com", text: "Tab 1" },
            { url: "https://example.com", text: "Tab 2" },
            { url: "https://example.com", text: "Tab 3" },
        ];
        const {container} = renderComponent({...defaultProps, externalLinks});
        const links = screen.queryAllByTestId("mock-externallink");
        expect(links).toHaveLength(externalLinks.length);
        expect(mockExternalLink).toHaveBeenCalledTimes(externalLinks.length);
        externalLinks.forEach((externalLink, index) => {
            expect(mockExternalLink).toHaveBeenNthCalledWith(index + 1, expect.objectContaining({
                track: mockTrack,
                url: externalLink.url,
                text: externalLink.text,
            }));
        });
    });
});