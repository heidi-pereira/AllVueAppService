import React from "react";
import { IPart } from "./IPart";
import { IPartDescriptor, PartDescriptor } from "../BrandVueApi";
import { IDashPartProps } from "../components/DashBoard";
import { ICardProps } from "../components/panes/Card";
import {Location} from "react-router-dom";
import { IReadVueQueryParams } from "../components/helpers/UrlHelper";
import { ReactElement } from "react";

export class BasePart implements IPart {
    descriptor: IPartDescriptor;
    constructor(part: IPartDescriptor) {
        this.descriptor = part;
    }

    getPartComponent(props: IDashPartProps): ReactElement | null {
        return null;
    }

    getCardComponent(props: ICardProps, location: Location, readVueQueryParams: IReadVueQueryParams): ReactElement | null {
        return null;
    }
}