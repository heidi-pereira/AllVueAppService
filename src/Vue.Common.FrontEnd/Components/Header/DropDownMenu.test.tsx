import React from 'react';
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import DropDownMenu from './DropDownMenu';

const mockTrack = jest.fn();

const mockChild = jest.fn();
jest.mock("./DropDownMenuItem", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockChild(props);
        return <li data-testid="mock-child"></li>;
    })
  };
});

interface IMenuItem {
    track: (eventName: string) => void;
    text: string;
    url: string;
}

interface ITestProps {
    username: string;
    menuItems?: Array<IMenuItem>;
}

const renderComponent = (props: ITestProps) => {
    return render(<DropDownMenu {...props} />);
};

const defaultProps: ITestProps = {
    track: mockTrack,
    username: "",
};

describe('DropDownMenu Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render a ul for the dropdown menu with correct class', () => {
        const {container} = renderComponent(defaultProps);
        const divs = container.children;
        expect(divs.length).toEqual(1);
        const firstDiv = divs[0];
        expect(firstDiv.tagName).toEqual("UL");
        expect(firstDiv).toHaveClass("account-menu");
    });

    it('should render the username as the first item in the menu', () => {
        const username = "testuser";
        const {container} = renderComponent({...defaultProps, username: username});
        expect(mockChild).toHaveBeenCalledWith(expect.objectContaining({ text: username }));
    });

    it('should render a separator after the username', () => {
        const username = "testuser";
        const {container} = renderComponent({...defaultProps, username: username});
        const ul = container.getElementsByTagName("ul")[0];
        const separator = ul.getElementsByTagName("div")[0];
        expect(separator).toBeInTheDocument();
        expect(separator).toHaveClass("separator");
    });

    it('should render menu items if provided', () => {
        const menuItems = [
            { text: "Item 1", url: "https://example.com/1" },
            { text: "Item 2", url: "https://example.com/2" },
        ];
        const {container} = renderComponent({...defaultProps, menuItems});
        expect(mockChild).toHaveBeenCalledTimes(menuItems.length + 1); // +1 for the username
        menuItems.forEach((item, index) => {
            expect(mockChild).toHaveBeenCalledWith(expect.objectContaining({ text: item.text, url: item.url, track: mockTrack }));
        });
    });
});