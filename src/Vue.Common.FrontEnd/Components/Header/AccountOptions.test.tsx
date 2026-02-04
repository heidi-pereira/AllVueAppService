import React from 'react';
import userEvent from "@testing-library/user-event";
import { render, screen, fireEvent } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import AccountOptions from './AccountOptions';
import { IDropDownMenuItem } from '../Types/IDropDownMenuItem';

const mockTrack = jest.fn();

const mockChild = jest.fn();
jest.mock("gravatar", () => {
  return {
    url: jest.fn((email: string, options: any, bit: boolean) => {
        return mockChild(email);
    })
  };
});

const mockDropDownMenu = jest.fn();
jest.mock("./DropDownMenu", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockDropDownMenu(props);
        return <ul data-testid="mock-dropdownmenu"></ul>;
    })
  };
});

interface ITestProps {
    track: (eventName: string) => void;
    username: string;
    menuItems?: Array<IDropDownMenuItem>;
}

const renderComponent = (props: ITestProps) => {
    return render(<AccountOptions {...props} />);
};

const defaultProps = {
    track: mockTrack,
    username: "",
};

describe('AccountOptions Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render a div for the account options with correct class', () => {
        const {container} = renderComponent(defaultProps);
        const divs = container.children;
        expect(divs.length).toEqual(1);
        const firstDiv = divs[0];
        expect(firstDiv.tagName).toEqual("DIV");
        expect(firstDiv).toHaveClass("account-options");
    });

    it('should render 2 levels of child divs', () => {
        const {container} = renderComponent(defaultProps);
        const divs = container.children[0].children;
        expect(divs.length).toEqual(1);
        const firstDiv = divs[0];
        expect(firstDiv.tagName).toEqual("DIV");
        expect(firstDiv).toHaveClass("dropdown");
        expect(firstDiv.children.length).toEqual(1);
    });

    it('should render a button for the dropdown with correct attributes', () => {
        const {container} = renderComponent(defaultProps);
        const buttons = container.getElementsByTagName("button");
        expect(buttons.length).toEqual(1);
        const button = buttons[0];
        expect(button).toHaveClass("nav-button");
        expect(button).toHaveAttribute("type", "button");
        expect(button).toHaveAttribute("aria-haspopup", "true");
        expect(button).toHaveAttribute("aria-expanded", "false");
        expect(button).toHaveAttribute("title", "My Account");
    });

    it('should render a button with divs in', () => {
        const {container} = renderComponent(defaultProps);
        const buttons = container.getElementsByTagName("button");
        const button = buttons[0];
        const divs = button.getElementsByTagName("div");
        expect(divs.length).toEqual(3);
        const firstDiv = divs[0];
        const secondDiv = divs[1];
        const thirdDiv = divs[2];
        expect(firstDiv).toHaveClass("circular-nav-button");
        expect(secondDiv).toHaveClass("circle");
        expect(thirdDiv).toHaveClass("text");
        expect(thirdDiv).toHaveTextContent(/My Account/);
    });

    it('should show an account icon by default', () => {
        const {container} = renderComponent(defaultProps);
        const icons = screen.getAllByText(/account_circle/);
        expect(icons).toHaveLength(1);
    });

    it('should try to display a gravatar image if a username is provided', () => {
        const username = "tech@savanta.com";
        const {container} = renderComponent({ ...defaultProps, username });
        expect(mockChild).toHaveBeenCalledWith(username);
    });

    it('should hide the dropdown menu by default', () => {
        const {container} = renderComponent(defaultProps);
        const dropdowns = container.getElementsByClassName("dropdownContainer");
        expect(dropdowns.length).toEqual(0);
    });

    it('should show the dropdown menu when the button is clicked', async () => {
        const {container} = renderComponent(defaultProps);
        const user = userEvent.setup();
        expect(screen.getByRole("button")).toBeInTheDocument();
        await user.click(screen.getByRole("button"));
        expect(mockDropDownMenu).toHaveBeenCalled();
    });

    it('should pass the correct props to the dropdown menu', async () => {
        const user = userEvent.setup();
        const menuItems = [
            {
                text: "My Account",
                title: "My Account",
                url: "/my-account",
                showLockIcon: false,
                children: [],
            },
            {
                text: "Settings",
                title: "Settings",
                url: "/settings",
                showLockIcon: true,
                children: [],
            },
        ];
        const {container} = renderComponent({...defaultProps, menuItems: menuItems});
        expect(screen.getByRole("button")).toBeInTheDocument();
        await user.click(screen.getByRole("button"));
        expect(mockDropDownMenu).toHaveBeenCalledWith(expect.objectContaining({ menuItems: menuItems, track: mockTrack}));
    });
});