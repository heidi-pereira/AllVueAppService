export class RequestDebouncer {

    private static jsonEqual(a, b) {
        return JSON.stringify(a) === JSON.stringify(b);
    }

    public static for<TDataClient, TProps, TState, TRequestModel, TResponse>(reactComponent: React.Component<TProps, TState>,
        createDataClient: (handleError: ((errorLambda: () => never, error?: any) => void), baseUri?: string) => TDataClient,
        requestModelFromProps: ((props: TProps) => TRequestModel | null),
        serverMethod: (dataClient: TDataClient) => ((request: TRequestModel) => Promise<TResponse>),
        processResponse: ((r: TResponse, props?: TProps) => void)) {
        let lastRequestModelJson: string;
        const dataClient = createDataClient(err => reactComponent.setState(err));
        const requestFromServer = serverMethod(dataClient).bind(dataClient);

        return (props: TProps) => {
            const initialRequestModel = requestModelFromProps(props);
            const outerLambdaReference = reactComponent;
            const initialComponentProps = outerLambdaReference.props;
            const requestModelJson = JSON.stringify(initialRequestModel);

            if (lastRequestModelJson === requestModelJson) return false;

            lastRequestModelJson = requestModelJson;
            if (initialRequestModel != null) {
                requestFromServer(initialRequestModel).then(r => {
                    const innerLambdaReference = outerLambdaReference;
                    const modelFromCurrentProps = requestModelFromProps(innerLambdaReference.props);
                    if (initialComponentProps === innerLambdaReference.props || RequestDebouncer.jsonEqual(modelFromCurrentProps, initialRequestModel)) {
                        processResponse(r, props);
                    }
                });
            }
            return true;
        };
    }
}
