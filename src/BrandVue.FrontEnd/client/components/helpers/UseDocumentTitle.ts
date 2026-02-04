import {ProductConfiguration} from "../../ProductConfiguration";
import {useEffect} from "react";

export const UseAppTitle = (productConfiguration: ProductConfiguration) => {
    useEffect(() => {
        const titleCaseProductName = productConfiguration.productName.replace(
            /\w\S*/g,
            txt => txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase()
        );
        const productName = productConfiguration.isSurveyVue()
            ? "AllVue"
            : "BrandVue-" + titleCaseProductName;

        const updateTitle = () => {
            const pathname = window.location.pathname;
            document.title = productName + pathname.replace(/-/g, " ").replace(/\//g, ": ");
        };

        // Initial title set
        updateTitle();

        // Listen for route changes
        window.addEventListener('popstate', updateTitle);
        return () => window.removeEventListener('popstate', updateTitle);
    }, [productConfiguration]);
};