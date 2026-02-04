import React from "react";
import Iframe from 'react-iframe'
import QueryString from 'query-string';
import { ProductConfiguration } from "../../ProductConfiguration";

interface IFrameContainerProps {
    url: string;
    height: number;
    productConfiguration: ProductConfiguration;
}

const IFrameContainer: React.FunctionComponent<IFrameContainerProps> = (props) => {
    const queryString = QueryString.extract(props.url);
    const urlWithAppBase = props.url + (queryString.length ? "&" : "?") + "productName=" + props.productConfiguration.productName;
    return (<Iframe url={urlWithAppBase} position={"relative"} height={props.height + "px"} />);
}

export default IFrameContainer;
