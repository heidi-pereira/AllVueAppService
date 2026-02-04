import React from "react";
import { DropdownToggle, ButtonDropdown, DropdownMenu, DropdownItem} from "reactstrap";
import * as BrandVueApi from "../BrandVueApi";
import { ProductConfigurationContext } from "../ProductConfigurationContext";
import { saveFile } from "../../client/helpers/FileOperations";
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from "../components/visualisations/Settings/Weighting/Controls/MaterialSymbol";
import { AverageIds } from "client/components/helpers/PeriodHelper";

interface IWeightingsExport {
    displayError(message: string): void;
}

export const WeightingsExport: React.FunctionComponent<IWeightingsExport> = ((props: IWeightingsExport) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const [averages, setAverages] = React.useState<BrandVueApi.IAverageDescriptor[]>([]);
    const [subsets, setSubsets] = React.useState<BrandVueApi.Subset[]>([]);
    const [subset, setSubset] = React.useState<BrandVueApi.Subset>();
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const [subsetDropdownOpen, setSubsetDropdownOpen] = React.useState<boolean>(false);

    const onExportResponseWeightings = (e: React.MouseEvent, subsetDisplayName: string, averageId: string) => {
        const weightingAlgorithmsClient = BrandVueApi.Factory.WeightingAlgorithmsClient(error => error());
        e.stopPropagation();
        let preFixForFileName = productConfiguration.productName;
        if (productConfiguration.subProductId) {
            preFixForFileName += ` - ${productConfiguration.subProductId} `;
        }
        weightingAlgorithmsClient.exportRespondentWeights(
            new BrandVueApi.ExportRespondentWeightsRequest(
                { subsetIds: [subset!.id], averageId: averageId }))
            .then(r => saveFile(r, `${preFixForFileName} Weightings- ${subsetDisplayName} (${averageId})- Private.csv`))
            .catch(error => {
                props.displayError("Export failed");
            });
        return;
    }

    React.useEffect(() => {
        BrandVueApi.Factory.MetaDataClient(throwErr => throwErr())
            .getSubsets()
            .then(subsets => {
                subsets= subsets.filter(subset => !subset.disabled);
                subsets.sort((subsetA, subsetB) => subsetA.order - subsetB.order);
                setSubsets(subsets);
                setSubset(subsets[0]);
            });
    }, []);

    React.useEffect(() => {
        if (subset != null) {
            BrandVueApi.Factory.MetaDataClient(throwErr => throwErr())
                .getAverages(subset.id)
                .then(r => {
                    r.sort((a, b) => a.order - b.order);
                    setAverages(r);
                });
        }
    }, [subset]);

    const toggleSubset = (e: React.MouseEvent) => {
        e.stopPropagation();
        setSubsetDropdownOpen(!subsetDropdownOpen);
    }
    
    const toggle = (e: React.MouseEvent) => {
        e.stopPropagation();
        setDropdownOpen(!dropdownOpen);
    }

    const customPeriodAverage = averages.find(x =>
        !x.disabled &&
        x.averageId === AverageIds.CustomPeriod
    );
    const otherAverages = averages.filter(x =>
        !x.disabled &&
        x.averageId !== AverageIds.CustomPeriod
    );
    const defaultAverage = customPeriodAverage ?? otherAverages.find(a => a.isDefault) ?? otherAverages[0];

    if (subset !== undefined && (productConfiguration.user?.isSystemAdministrator??false)) {
        const subsetDisplayName = subset.displayName;
        return <div>
            <ButtonDropdown isOpen={subsetDropdownOpen} toggle={toggleSubset} id="dropdown-button-drop-up" drop="start" key="subsets" className={`subsetSelector`} >
                <DropdownToggle caret className="btn-menu styled-dropnone" aria-label="Select a subset">
                    <div className="circular-nav-button-normal">
                        <div className="circle">
                            <MaterialSymbol symbolType={MaterialSymbolType.public} symbolStyle={MaterialSymbolStyle.outlined} />
                        </div>
                        <div className="text">{subsetDisplayName}</div>
                    </div>
                </DropdownToggle>

                <div className="selectSubset">
                    <DropdownMenu>
                        {subsets.map((v, i) => <DropdownItem key={`dropdown_${i}`} onClick={(e) =>
                            setSubset(v)}>
                            {v.displayName}</DropdownItem>)}
                    </DropdownMenu>
                </div>
            </ButtonDropdown >

            <ButtonDropdown isOpen={dropdownOpen} toggle={toggle} id="dropdown-button-drop-up" drop="start" key="exports" className={`averageSelector`} >
                <DropdownToggle caret className="btn-menu styled-dropnone" aria-label="Select an average to export">
                    Export <MaterialSymbol symbolType={MaterialSymbolType.download} symbolStyle={MaterialSymbolStyle.outlined} />
                </DropdownToggle>

                <div className="exportWeightsDropdown">
                    <DropdownMenu>
                        <DropdownItem header>Export weights for</DropdownItem>
                        {defaultAverage &&
                            <DropdownItem key={-1} onClick={(e) => onExportResponseWeightings(e, subsetDisplayName, defaultAverage.averageId)}>
                                <i className={`material-symbols-outlined menu-icon default`}>
                                    {defaultAverage.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell ? 'Weight': '360'}
                                </i>{`Default (${subsetDisplayName})`}
                            </DropdownItem>
                        }
                        {otherAverages.map((v, i) =>
                            <DropdownItem key={i} onClick={(e) => onExportResponseWeightings(e, subsetDisplayName, v.averageId)}>
                                <i className={`material-symbols-outlined menu-icon`}>
                                    {v.weightingMethod == BrandVueApi.WeightingMethod.QuotaCell ? 'Weight': '360'}
                                </i>{v.displayName}
                            </DropdownItem>
                        )}
                    </DropdownMenu>
                </div>
            </ButtonDropdown >
        </div>
    }
    return<></>;
});
