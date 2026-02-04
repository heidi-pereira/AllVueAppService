import React from 'react';
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import TopMenu from './TopMenu';

const mockTrack = jest.fn();

const mockPageTitle = jest.fn();
jest.mock("./PageTitle", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockPageTitle(props);
        return <a data-testid="mock-pagetitle"></a>;
    })
  };
});

const mockHelpButton = jest.fn();
jest.mock("./HelpButton", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockHelpButton(props);
        return <a data-testid="mock-helpbutton"></a>;
    })
  };
});

const mockAccountOptions = jest.fn();
jest.mock("./AccountOptions", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockAccountOptions(props);
        return <a data-testid="mock-accountoptions"></a>;
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
    runningEnvironment: string;
    runningEnvironmentDescription: string;

}

const defaultProps: ITestProps = {
    track: mockTrack,
    tabs: [],
    externalLinks: [],
    pageTitle: "",
    homeUrl: "",
};

const renderComponent = (props: ITestProps) => {
    return render(<TopMenu {...props} />);
};

describe('TopMenu Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });
    
    it('should render a nav tag', () => {
        const {container} = renderComponent(defaultProps);
        const images = container.getElementsByTagName("nav");
        expect(images.length).toEqual(1);
    });

    it('should render a page title with the correct props', () => {
        const pageTitle = "Survey Name";
        const homeUrl = "https://example.com";
        const {container} = renderComponent({...defaultProps, pageTitle, homeUrl});
        const pageTitleComponent = screen.queryByTestId('mock-pagetitle');
        expect(pageTitleComponent).toBeInTheDocument();
        expect(mockPageTitle).toHaveBeenCalledTimes(1);
        expect(mockPageTitle).toHaveBeenCalledWith(expect.objectContaining({ title: pageTitle, url: homeUrl }));
    });

    it('should render a help button if a help url is provided', () => {
        const helpUrl = "https://example.com/help";
        const {container} = renderComponent({...defaultProps, helpUrl, track: mockTrack});
        const helpButtonComponent = screen.queryByTestId('mock-helpbutton');
        expect(helpButtonComponent).toBeInTheDocument();
        expect(mockHelpButton).toHaveBeenCalledTimes(1);
        expect(mockHelpButton).toHaveBeenCalledWith(expect.objectContaining({ url: helpUrl, track: mockTrack }));
    });

    it('should render an account options component with the correct props', () => {
        const username = "testuser";
        const menuItems = [{ text: "Item 1", url: "https://example.com/1" }]
        const {container} = renderComponent({...defaultProps, track: mockTrack, username, menuItems});
        const accountOptionsComponent = screen.queryByTestId('mock-accountoptions');
        expect(accountOptionsComponent).toBeInTheDocument();
        expect(mockAccountOptions).toHaveBeenCalledTimes(1);
        expect(mockAccountOptions).toHaveBeenCalledWith(expect.objectContaining({ track: mockTrack, username, menuItems }));
    });
});