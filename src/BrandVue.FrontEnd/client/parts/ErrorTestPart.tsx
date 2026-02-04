import { ReactElement, useEffect, useState } from "react";
import { IPartDescriptor } from "../BrandVueApi";
import { IDashPartProps } from "../components/DashBoard";
import { ICardProps } from "../components/panes/Card";
import { BasePart } from "./BasePart";
import { Location } from "react-router-dom";
import { IReadVueQueryParams } from "../components/helpers/UrlHelper";

export class ErrorTestPart extends BasePart {
    constructor(descriptor: IPartDescriptor) {
        super(descriptor);
    }

    getPartComponent(props: IDashPartProps): ReactElement | null {
        return <ErrorTestPartComponent {...props} descriptor={this.descriptor} />;
    }

    getCardComponent(props: ICardProps, location: Location, readVueQueryParams: IReadVueQueryParams): ReactElement | null {
        return super.getCardComponent(props, location, readVueQueryParams);
    }
}

interface IErrorTestPartComponentProps extends IDashPartProps {
    descriptor: IPartDescriptor;
}

const ErrorTestPartComponent = (props: IErrorTestPartComponentProps) => {
    const [isLoading, setIsLoading] = useState(true);
    const [data, setData] = useState<string | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            setIsLoading(true);
            await new Promise(resolve => setTimeout(resolve, 1000));
            setData("Mock data loaded");
            setIsLoading(false);
        };

        fetchData();
    }, []);

    if (isLoading) {
        return (
            <div style={{ padding: "20px", textAlign: "center" }}>
                <div>Loading error test component...</div>
            </div>
        );
    }

    if (data) {
        throw new Error("Simulated rendering error");
    }

    return <div>This should never render</div>;
};
