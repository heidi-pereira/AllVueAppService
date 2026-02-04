import React from 'react';
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom'; // Import jest-dom for extended matchers
import AllVueHomePageLogo from './AllVueHomePageLogo';

const renderElement = (url: string) => {
    return render(<AllVueHomePageLogo url={url} />);
}

describe('AllVueHomePageLogo Component', () => {
    it('should render a single anchor', () => {
        const {container} = renderElement("");
        const anchors = container.getElementsByTagName("a");
        expect(anchors.length).toEqual(1);
    });

    it('should render a single image', () => {
        const {container} = renderElement("");
        const images = container.getElementsByTagName("img");
        expect(images.length).toEqual(1);
    });

    it('should display the correct alt text', () => {
        const {container} = renderElement("");
        const logoText = screen.getByAltText("Logo");
        expect(logoText).toBeInTheDocument();
    });

    it('should have the correct class on image for styling', () => {
        const {container} = renderElement("");
        const logoImage = container.getElementsByTagName("img")[0];
        expect(logoImage).toHaveClass("page-logo");
    });

    it('should have the correct logo css variable on image', () => {
        const {container} = renderElement("");
        const logoImage = container.getElementsByTagName("img")[0];
        expect(logoImage.style.content).toContain('var(--header-logo)');
    });

    it('should have the correct class on anchor for styling', () => {
        const {container} = renderElement("");
        const logoImage = container.getElementsByTagName("a")[0];
        expect(logoImage).toHaveClass("logo-link-container");
    });

    it('should have the supplied URL on the a tag', () => {
        const url = "https://example.com";
        const {container} = renderElement(url);
        const anchor = container.getElementsByTagName("a")[0];
        expect(anchor).toHaveAttribute("href", url);
    });
});