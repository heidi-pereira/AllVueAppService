import * as React from "react";
import { ProductConfiguration } from "../ProductConfiguration";
import { IGoogleTagManager } from "../googleTagManager";
import * as PageHandler from "./PageHandler";
import * as moment from "moment/moment";
import $ from "jquery";

export class HtmlView extends React.Component<{
    productConfiguration: ProductConfiguration,
    googleTagManager: IGoogleTagManager,
    pageHandler: PageHandler.PageHandler,
    fileName: string,
    setQueryParameter: (name: string, value: (string | number | string[] | number[] | undefined)) => void;
}, {}> {
    constructor(props) {
        super(props);
        this.startTour = this.startTour.bind(this);
        this.postLoad = this.postLoad.bind(this);
    }

    el;

    componentDidMount() {
        const url = this.props.productConfiguration.calculateCdnPath(`/pages/${this.props.fileName}?id=${moment.utc().valueOf()}`);
        $(this.el).load(url, this.postLoad);
    }

    postLoad() {
        $(this.el).find("*[data-tour]").click(this.startTour);
        
        $("#video-container").click((e) => {
            var title = $(e.target).find("#video-title")?.text?.toString();
            this.props.googleTagManager
                .addEvent("tutorialVideoClicked", this.props.pageHandler, {"title": title});
        })
    }

    startTour(e) {
        const tourName = $(e.target).data("tour");
        this.props.googleTagManager
            .addEvent("startedTour", this.props.pageHandler, {value: tourName});
        this.props.setQueryParameter("Tour", tourName);
        return false;
    }

    render() {
        return (<div className="col-sm-12" ref={el => this.el = el}/>);
    }
}