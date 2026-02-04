import React from 'react';
import ReportingApi = require("../reportingApi");
import Globals from "../globals";

export interface IState {
    queue: ReportingApi.ItemInfo[];
    generatedReportsFolderLink: string;
}

export class Queue extends React.Component<{}, IState> {
    _interval: number;
    constructor(props: IState) {
        super(props);

        this.state = {
            queue: props.queue,
            generatedReportsFolderLink: props.generatedReportsFolderLink
        };

        this.loadQueue = this.loadQueue.bind(this);
    }

    componentDidMount() {
        this.loadQueue();
        this._interval = setInterval(this.loadQueue, 5000);
    }

    componentWillUnmount() {
        clearInterval(this._interval);
    }

    loadQueue() {
        Globals.ReportApiClient.getQueueStatus().then(data => this.setState({ queue: data.items, generatedReportsFolderLink: data.generatedReportsFolderLink }));
    }

    renderQueueItem(item: ReportingApi.ItemInfo, index: number) {
        let statusClass;
        if (item.error) {
            statusClass = "error";
        } else if (item.completed) {
            statusClass = "completed";
        } else if (item.inProgress) {
            statusClass = "inprogress"
        }
        let className = "queue-item " + statusClass;
        return (
            <div key={index} className={className}>
                <div className="item-name">{item.name}</div>
                <div className="item-status">
                    <i className="item-spinner material-icons spin">hourglass_empty</i>
                    <i className="item-success material-icons">done</i>
                    <i className="item-error material-icons" title={item.message}>error</i>
                </div>
            </div>
        );
    }

    render() {

        return (
            <div className="queue-list">
                <div className="header">
                    <div className="queue-title">
                        <span> Queue </span>
                    </div>
                    <div className="folder-link">
                        <a href={this.state.generatedReportsFolderLink} target="_blank">Open file folder</a>
                    </div>
                </div>
                {this.state.queue && this.state.queue.map((x, index) => this.renderQueueItem(x, index))}
            </div>
        );
    }
}