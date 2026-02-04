import { filter } from "./filter";
import { CuratedFilters } from "./CuratedFilters";
import * as BrandVueApi from "../BrandVueApi";


export class filterSet {

    public filters: filter[];
    public filterLookup: any;
    public curatedFilters: CuratedFilters;

    public load(selectedSubsetId: string) {
        this.filters = [];
        this.filterLookup = {};


        let metaDataClient = BrandVueApi.Factory.MetaDataClient(throwErr => throwErr());

        return metaDataClient.getFilters(selectedSubsetId).then(filters => {
            filters.map(thisfilter => {
                const f: filter = new filter();

                f.name = thisfilter.name;
                f.displayName = thisfilter.displayName;
                f.field = thisfilter.field;
                f.filterValueType = thisfilter.filterValueType;
                f.categories = thisfilter.categories;
                f.initItems();

                // Add to arrays
                this.filterLookup[f.name] = f;
                this.filters.push(f);

            });

        }
        );

    }
}


