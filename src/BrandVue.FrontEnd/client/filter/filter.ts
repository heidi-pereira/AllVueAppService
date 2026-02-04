import { filterItem } from "./filterItem";
import { FilterValueTypes } from "../BrandVueApi";

export class filter {

    private delimiter_categories = "|";
    private delimiter_name = ":";
    private delimiter_value = ",";
    private delimiter_range = "-";

    public name: string;
    public displayName: string;
    public field: string; // Field name / Brand field name / Period spec
    public question: string;
    public categories: string;  // Raw text of categories
    public initialValue: string[];
    public initialDescription: string;

    public filterValueType: FilterValueTypes;

    public filterItems: filterItem[];

    // Splits categories into filter items, and creates them
    public initItems() {

        var me = this;
        // e.g. 14,15,16,17,23,24,26,28,35,36,42,50:Midwest|7,8,9,20,21,22,30,31,33,39,40,46:Northeast|1,3,4,10,11,18,19,25,32,34,37,41,43,44,47,49:South|2,5,6,12,13,27,29,38,45,48,51:West
        // e.g. 0-74999:Low|75000-500000:High
        me.filterItems = [];

        if (me.categories) {
            const categories = me.categories.split(me.delimiter_categories);
            categories.forEach((c: string) => {

                var fi = new filterItem();

                const parts = c.split(this.delimiter_name);
                const values = parts[0];
                fi.caption = parts[1];

                switch (this.filterValueType) {

                case FilterValueTypes.Category:
                    var ids = values.split(this.delimiter_value);
                    ids.forEach(id => {
                        fi.idList.push(id);
                    });
                    break;

                case FilterValueTypes.Value:
                    var ids = values.split(this.delimiter_range);
                    fi.min = +ids[0];
                    if (ids.length > 1) {
                        fi.max = +ids[1];
                    } else {
                        fi.max = fi.max;
                    }
                    break;

                }

                this.filterItems.push(fi);
            });

        }
    }
    public getDefaultValue() : string[] {
        return this.filterItems.map(fi => fi.idList.join(","));
    }
}


