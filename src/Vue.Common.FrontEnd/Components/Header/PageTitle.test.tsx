import React from 'react';
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import PageTitle from './PageTitle';

const mockChild = jest.fn();
jest.mock("./AllVueHomePageLogo", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
        mockChild(props);
        return <div data-testid="mock-child"></div>;
    })
  };
});

interface ITestProps {
    title: string;
    url: string;
};

const renderComponent = (props: ITestProps) => {
    return render(<PageTitle title={props.title} url={props.url} />);
};

const defaultProps = {
    title: "Survey Name",
    url: "https://example.com"
}

describe('PageTitle Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });
    
    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should contain a logo component', () => {
        renderComponent(defaultProps);
        expect(screen.getByTestId('mock-child')).toBeInTheDocument();
    });
        
    const pageTitles = [
        ["Survey Name"],
        ["Organisation Name"],
        ["Savanta"],
    ];

    test.each(pageTitles)("should output the title supplied: %s", async (title: string) => {
        const props = { ...defaultProps, title };
        renderComponent(props);
        expect(screen.getByText(title)).toBeInTheDocument();
    });

    it('should set the url prop of the AllVuePageLogo component', () => {
        const url = "https://example.com";
        const props = { ...defaultProps, url };
        renderComponent(props);
        expect(mockChild).toHaveBeenCalledWith(expect.objectContaining({ url: url }));
    });

    it('should use the correct class for the page title span', () => {
        const {container} = renderComponent(defaultProps);
        const pageTitleSpan = container.getElementsByTagName("span")[0];
        expect(pageTitleSpan).toHaveClass("page-title");
    });

    it('should use the correct class for the container div', () => {
        const {container} = renderComponent(defaultProps);
        const containerDiv = container.getElementsByTagName("div")[0];
        expect(containerDiv).toHaveClass("page-title-container");
    });
});