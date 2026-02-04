import React from 'react';
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import AllVueHeader from './AllVueHeader';
import { IDropDownMenuItem } from "./Types/IDropDownMenuItem";
import { ITabLink } from "./Types/ITabLink";
import { IExternalLink } from "./Types/IExternalLink";

const mockTrack = jest.fn();

const mockTopMenu = jest.fn();
jest.mock("./Header/TopMenu", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockTopMenu(props);
        return <div data-testid="mock-topmenu"></div>;
    })
  };
});

const mockTabs = jest.fn();
jest.mock("./Header/Tabs", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockTabs(props);
        return <div data-testid="mock-tabs"></div>;
    })
  };
});

const mockWarningBanner = jest.fn();
jest.mock("./Header/WarningBanner", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockWarningBanner(props);
        return <div data-testid="mock-warningbanner"></div>;
    })
  };
});

interface ITestProps {
    track: (eventName: string) => void;
    username?: string;
    menuItems?: Array<IDropDownMenuItem>;
    tabs?: Array<ITabLink>;
    externalLinks?: Array<IExternalLink>;
    pageTitle: string;
    homeUrl: string;
    helpUrl?: string;
    warningMessage?: string;
    warningIcon?: string;
}

const defaultProps: ITestProps = {
    track: mockTrack,
    pageTitle: "",
    homeUrl: "",
};

const renderComponent = (props: ITestProps) => {
    return render(<AllVueHeader {...props} />);
};

describe('AllVueHeader Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });
    
    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });
    
    it('should render the topmenu with the correct props', () => {
        const pageTitle = "Survey Name";
        const homeUrl = "https://example.com";
        renderComponent({ ...defaultProps, pageTitle, homeUrl });
        const topMenu = screen.queryByTestId('mock-topmenu');
        expect(topMenu).toBeInTheDocument();
        expect(mockTopMenu).toHaveBeenCalledTimes(1);
        expect(mockTopMenu).toHaveBeenCalledWith(expect.objectContaining({ track: mockTrack, pageTitle, homeUrl }));
    });

    it('should render the tabs with the correct props', () => {
        const tabs = [{ text: "Tab 1", url: "https://example.com" }];
        const externalLinks = [{ text: "External Link", url: "https://external.com" }];
        renderComponent({...defaultProps, tabs, externalLinks}); 
        const tabsComponent = screen.queryByTestId('mock-tabs');
        expect(tabsComponent).toBeInTheDocument();
        expect(mockTabs).toHaveBeenCalledTimes(1);
        expect(mockTabs).toHaveBeenCalledWith(expect.objectContaining({ tabs, externalLinks, track: mockTrack }));
    });

    it('should render a warning banner with the correct props', () => {
        const message = "This is a warning";
        const icon = "warning-icon";
        renderComponent({ ...defaultProps, warningMessage: message, warningIcon: icon });
        const warningBanner = screen.queryByTestId('mock-warningbanner');
        expect(warningBanner).toBeInTheDocument();
        expect(mockWarningBanner).toHaveBeenCalledTimes(1);
        expect(mockWarningBanner).toHaveBeenCalledWith(expect.objectContaining({ message, icon }));
    });
});