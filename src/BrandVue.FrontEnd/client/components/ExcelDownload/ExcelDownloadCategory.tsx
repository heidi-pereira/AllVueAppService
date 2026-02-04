import * as BrandVueApi from "../../BrandVueApi";
import {
    PageDescriptor,
    CategoryExportResultCard,
    VariableConfigurationModel
} from "../../BrandVueApi";
import { ExcelDownloadBase } from "./ExcelDownloadBase";
import { saveFile } from "../../helpers/FileOperations";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { QueryStringParamNames, useReadVueQueryParams } from "../helpers/UrlHelper";
import { StringHelper } from "../../helpers/StringHelper";
import {useState} from "react";
import { useAppSelector } from "../../state/store";
import React from "react";
import {EntitySet} from "../../entity/EntitySet";
import { PageHandler } from "../PageHandler";
import { IGoogleTagManager } from "../../googleTagManager";
import { viewBase } from "../../core/viewBase";
import { selectSubsetId } from "client/state/subsetSlice";

interface IExcelDownloadCategoryProps {
    activeDashPage: PageDescriptor;
    applicationConfiguration: ApplicationConfiguration;
    baseVariables: VariableConfigurationModel[];
    resultCards: CategoryExportResultCard[];
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    activeView: viewBase;
    entitySet: EntitySet;
}
const ExcelDownloadCategory: React.FC<IExcelDownloadCategoryProps> = (props) => {
    const [loading, setLoading] = useState(false);
    const categorySortKey = useAppSelector((state) => state.entitySelection.categorySortKey);
    const excelSheetName = "export.xlsx";
    const readVueQueryParams = useReadVueQueryParams();
    const subsetId = useAppSelector(selectSubsetId);

    const getBaseVariable1 = () =>
        props.baseVariables.find(b => b.id == readVueQueryParams.getQueryParameterInt(QueryStringParamNames.baseVariableId1));

    const getBaseVariable2 = () =>
        props.baseVariables.find(b => b.id == readVueQueryParams.getQueryParameterInt(QueryStringParamNames.baseVariableId2));

    const getVariableName = (variable: VariableConfigurationModel | undefined) => {
        if (variable !== undefined) {
            return StringHelper.formatBaseVariableName(variable.displayName);
        }
        return undefined;
    };

    const handleExcelDownloadClick = () => {
        const model = new BrandVueApi.ExcelExportCategoryModel({
            categoryResultCards: props.resultCards,
            sortKey: categorySortKey,
            pageName: props.activeDashPage.displayName,
            subsetId: subsetId,
            activeBrand: props.entitySet.getMainInstance().name,
            firstBaseVariableName: getVariableName(getBaseVariable1()),
            secondBaseVariableName: getVariableName(getBaseVariable2())
        });
        return BrandVueApi.Factory.DataClient(throwErr => setLoading(throwErr)).exportCategoriesData(model);
    };

    const handleClick = () => {
        // Call base click handler
        props.googleTagManager.addEvent("downloadExcel", props.pageHandler);

        if (props.resultCards.length > 0) {
            setLoading(true);
            handleExcelDownloadClick()
                .then(r => {
                    saveFile(r, excelSheetName);
                })
                .finally(() => {
                    setLoading(false);
                });
        }
    };

    return <ExcelDownloadBase
        {...props}
        loading={loading}
        onClick={handleClick}
    />;
};

export default ExcelDownloadCategory;