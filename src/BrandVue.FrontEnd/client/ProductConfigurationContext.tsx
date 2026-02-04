import React from "react";
import { ProductConfiguration } from "./ProductConfiguration";

interface IProductConfigurationContextState {
    productConfiguration: ProductConfiguration;
}

export const ProductConfigurationContext = React.createContext<IProductConfigurationContextState>({
    productConfiguration: new ProductConfiguration(),
});