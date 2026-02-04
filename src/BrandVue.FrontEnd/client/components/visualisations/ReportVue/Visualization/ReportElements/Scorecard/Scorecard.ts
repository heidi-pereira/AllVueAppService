import { ScorecardRow, ScorecardRowTypes } from "./ScorecardRow";
import { ScorecardColumn } from "./ScorecardColumn";
import { ReportStructure } from "../ReportStructure";
import { ReportPage } from "../ReportPage";
import { FilterTemplate } from "../FilterTemplate";
import { DropDownMenu } from "../../Nav/DropDownMenu";
import { Filter } from "../Filter";
import { ScorecardCell } from "./ScorecardCell";
import { MixPanel } from "../../../../../mixpanel/MixPanel";

export class Scorecard {
    private static DefaultInvalidColumnWdith(): number { return 5 };
    private _resetButton: HTMLButtonElement;
    public Rows: ScorecardRow[];
    public Columns: ScorecardColumn[];

    public CellBorderOptions: string;
    public NavDiv: HTMLDivElement;
    public Table: HTMLTableElement;
    public ReportStructure: ReportStructure;
    public ReportPage: ReportPage;
    public SortOrderDescending: boolean = true;
    private cachedColumns: ScorecardColumn[];
    private cachedSortedPosition: number[] | null;

    public static async LoadScorecardFromJson(url: string): Promise<Scorecard | null> {
        var scorecard: Scorecard | null = null;
        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            scorecard = await new Scorecard(await response.json());
        } catch (error) {
            console.error("Error fetching JSON:", error);
        }
        return scorecard;
    }

    constructor(json: any) {
        this.Populate(json);
    }

    private Populate(json: any) {
        this.Columns = ScorecardColumn.GetScorecardColumns(this, json.Columns);
        this.Rows = ScorecardRow.GetScorecardRows(this, json.Rows);
    }

    public Render(div: HTMLElement, reSize: boolean) {

        var me = this;
        me.NavDiv = document.createElement("div");
        div.appendChild(me.NavDiv);
        me.AddNav();
        var innerDiv = document.createElement("div");
        innerDiv.style.overflow = "auto";
        innerDiv.style.height = "auto";
        if (reSize) {
            innerDiv.style.overflow = "auto";
        }
        div.appendChild(innerDiv);

        me.AddCopyTableButton(me.NavDiv);
        if (me.DropDowns && me.DropDowns.length > 0) {
            me.AddResetButton(me.NavDiv);
        }
        me.Table = document.createElement("table");
        innerDiv.appendChild(me.Table);
        
        this.AssociateColumnsWithBrands();
        var filterTemplate: FilterTemplate = me.ReportStructure.FilterTemplateLookup[me.ReportPage.FilterTemplateName];
        if (filterTemplate && filterTemplate.CustomFieldHeadings) {
            me.UpdateScorecard();
        } else {
            me.AddContentToTable(me.Columns, null, null);
        }
    }

    private AddCopyTableButton(div: HTMLDivElement) {
        var me = this;
        var copyButton = document.createElement("button");
        copyButton.title = "Copy to Excel with preserved table format";
        copyButton.className = "rv-button";

        const html_i = document.createElement("i");
        html_i.className = "material-symbols-outlined";
        html_i.textContent = "content_copy";

        const text = document.createElement("span");
        text.textContent = "Copy table";

        copyButton.appendChild(html_i);
        copyButton.appendChild(text);

        div.appendChild(copyButton);

        copyButton.addEventListener("click", function () {
            MixPanel.trackWithContext("dashboardCopyToClipboard", "ScoreCard");
            if (!navigator.clipboard) {
                const range = document.createRange();
                range.selectNode(me.Table);

                const selection = window.getSelection();
                selection?.removeAllRanges();
                selection?.addRange(range);
                try {
                    document.execCommand("copy");
                } catch (err) {
                    alert("Error copying table to the clipboard");
                }
                selection?.removeAllRanges();
            } else {
                const text = me.Table.outerHTML.replace(/font-size: \d.px;/g, "");
                const text_noLink = text.replace(/<a [^>]*?>/g, "").replace(/<\/a>/g, "");
                const textHtmlBlob = new Blob([text_noLink], { type: 'text/html' });
                const textPlainBlob = new Blob([me.DataToTabDelmited(me.cachedColumns, me.cachedSortedPosition)], { type: 'text/plain' });

                const clipboardItem = new ClipboardItem(
                    {
                        ['text/html']: textHtmlBlob,
                        ['text/plain']: textPlainBlob,
                    });

                navigator.clipboard.write([clipboardItem])
                    .then()
                    .catch(error => { 
                        alert("Error copying table to the clipboard");
                    });
            } 
        });
    }

    private AddResetButton(div: HTMLDivElement) {
        var me = this;
        this._resetButton = document.createElement("button");
        this._resetButton.className = "rv-button";
        this._resetButton.title = "Reset the all the filters to defaults ";
        const html_i = document.createElement("i");
        html_i.className = "material-symbols-outlined";
        html_i.textContent = "autorenew";
        this._resetButton.appendChild(html_i);
        me._resetButton.disabled = true;

        div.appendChild(this._resetButton);

        this._resetButton.addEventListener("click", function () {
            me.DropDowns.forEach(d => {
                d.SetActive(me.FilterForAll, false);
            });
            MixPanel.trackWithContext("dashboardFiltersCleared", "ScoreCard");
            me.SortDropDown.SetActive("None", false);
            const filterTemplate: FilterTemplate = me.ReportStructure.FilterTemplateLookup[me.ReportPage.FilterTemplateName];
            me.UpdateNavContent(filterTemplate);
            me.UpdateScorecard();
            me._resetButton.disabled = true;
        });
    }

    private gapRow(rowIndex: number, index: number): ScorecardCell {

        while (rowIndex >= 0 && this.Rows[rowIndex].RowType == ScorecardRowTypes.Gap) {
            rowIndex--;
        }
        const rowToClone = this.Rows[rowIndex];
        return rowToClone.Cells[index]?.CloneForGap();
    }

    private AddTextAsBrandLink(document: Document, column: ScorecardColumn, scorecardCell: ScorecardCell, htmlTableCell: HTMLTableCellElement) {
        const link = document.createElement("a");
        link.target = "_blank";
        if (column.AssociatedBrandRecord.DisplayName) {
            link.title = column.AssociatedBrandRecord.DisplayName;
        }
        link.href = `${window.location.pathname}?item=${column.AssociatedBrandRecord.BrandName}`
        scorecardCell.ApplyStyle(link);

        link.innerText = scorecardCell.DisplayText;
        link.style.color = scorecardCell.FontColor;
        link.addEventListener("mouseover", function () {
            link.style.color = column.AssociatedBrandRecord.SolidFillColor;
        });
        link.addEventListener("mouseout", function () {
            link.style.color = scorecardCell.FontColor;
        });

        link.style.fontSize = "inherit";
        htmlTableCell.appendChild(link);
    }

    private DataToTabDelmited(columns: ScorecardColumn[], sortedColPositionMap: number[] | null): string {
        var rowsOfText: string[] = this.Rows.map((row, rowIndex) => {
            const rowOfData = columns.map((col, columnIndex) => {
                const span = row.GetSpan(columnIndex);
                if (span > 0) {
                    const mappedColumnIndex = sortedColPositionMap ? sortedColPositionMap[columnIndex] : columnIndex;
                    const cellToAdd = (row.RowType === ScorecardRowTypes.Gap && row.Cells.length == 0) ? this.gapRow(rowIndex, mappedColumnIndex) : row.Cells[mappedColumnIndex];
                    if (cellToAdd) {
                        return cellToAdd.DisplayText && cellToAdd.DisplayText.trim().length > 0 ? cellToAdd.DisplayText : "";
                    }
                }
                return "";
            });
            return rowOfData.join("\t")
        });
        return rowsOfText.join("\r\n");
    }

    private AddContentToTable(columns: ScorecardColumn[], sortRow: ScorecardRow | null, sortedColPositionMap: number[] | null) {
        let me = this;
        me.cachedColumns = columns;
        me.cachedSortedPosition = sortedColPositionMap;
        me.Table.style.borderCollapse = "collapse";
        me.Table.style.tableLayout = "fixed";
        let totalWidth = me.AppendColumnSettingsAndReturnTotalWidth(columns);
        me.Table.style.width = totalWidth + "px";
        let fixTop = true;
        let head = document.createElement("thead");
        let body = document.createElement("tbody");

        const isSectionBrandRelated = !me.ReportPage.Section.IsUnrelatedToBrand;
        const columnsToBeSticky = isSectionBrandRelated ? 2 : 1;
        me.Rows.forEach((row, rowIndex) => {
            let rowInHTML = document.createElement("tr");
            let columnIndex = 0;
            let cumulativeWidth = 0;
            columns.forEach(column => {
                const span = row.GetSpan(columnIndex);
                if (span > 0) {
                    const mappedColumnIndex = sortedColPositionMap ? sortedColPositionMap[columnIndex] : columnIndex;
                    const cellToAdd = (row.RowType === ScorecardRowTypes.Gap && row.Cells.length == 0) ? this.gapRow(rowIndex, mappedColumnIndex) : row.Cells[mappedColumnIndex];
                    if (!cellToAdd) {
                        console.log(`Error skipping ColSpan:${columnIndex}:${span}:${mappedColumnIndex} using ${sortedColPositionMap}`);
                    }
                    else {
                        const cell = document.createElement("td");
                        cellToAdd.ApplyStyle(cell);
                        if (columnIndex < columnsToBeSticky) {
                            cell.style.left = cumulativeWidth + "px";
                            cell.style.zIndex = "1";
                            cell.style.position = "sticky";
                        }
                        if (columnIndex == 1) {
                            if (isSectionBrandRelated) {
                                cell.style.boxShadow = "inset 2px 0 0 0 #808080, inset -2px 0 0 0 #808080"
                            }
                            else {
                                cell.style.boxShadow = "inset 2px 0 0 0 #808080"
                            }
                        }
                        if (cellToAdd.DisplayText) {
                            if (row.IsTitle() && fixTop && column.AssociatedBrandRecord && rowIndex == 0) {
                                this.AddTextAsBrandLink(document, column, cellToAdd, cell);
                            }
                            else {
                                cell.innerText = cellToAdd.DisplayText;
                            }
                        }
                        if (sortRow === row && columnIndex == 0) {

                            const sortIndicator = document.createElement("i");
                            sortIndicator.className = "material-symbols-outlined";
                            sortIndicator.id = "scorecardSort";
                            sortIndicator.textContent = "arrow_right";
                            cell.appendChild(sortIndicator);
                        }
                        if (span > 1) {
                            cell.colSpan = span;
                        }
                        rowInHTML.appendChild(cell);
                    }
                }
                cumulativeWidth += column?.Width ?? Scorecard.DefaultInvalidColumnWdith();
                columnIndex++;
            });
            if (fixTop && !row.IsTitle()) {
                fixTop = false;
            }
            if (fixTop) {
                head.appendChild(rowInHTML);
            } else {
                body.appendChild(rowInHTML);
            }
        });
        head.style.zIndex = "2";
        head.style.position = "sticky";
        head.style.top = "0px";
        me.Table.appendChild(head);
        me.Table.appendChild(body);
    }

    private RenderCustom(filters: Filter[], hasFilterBeenApplied: boolean) {
        if (filters.length == 0)
            return;

        var me = this;
        const isBrandRelated = !me.ReportPage.Section.IsUnrelatedToBrand;
        me.Table.innerHTML = "";

        let allColumns: ScorecardColumn[] = [];
        // Unfiltered columns
        let unfilteredColumns = me.Columns.filter(c => !c.FilterId || (c.FilterId == 0 && isBrandRelated));
        unfilteredColumns.forEach(c => {
            allColumns.push(c);
        });
        // Mapped columns
        let columnMap = me.Columns.reduce((acc, c) => ({ ...acc, [c.FilterId]: c }), {});
        filters.forEach(f => {
            var c = columnMap[f.Id];
            allColumns.push(c);
        });
        
        me._resetButton.disabled = !hasFilterBeenApplied;
        let colPositionMap = allColumns.map(c => me.Columns.indexOf(c));
        let sortRow: ScorecardRow = me.SortDropDown?.SelectedObject();
        let indices = Array.from({ length: allColumns.length }, (_, i) => i);

        if (sortRow?.Cells) {
            indices.sort((a, b) => me.CompareCells(sortRow.Cells[colPositionMap[a]], sortRow.Cells[colPositionMap[b]], me.IsColumnLocked(a, b, isBrandRelated)));
        }
        let sortedColPositionMap = indices.map(index => colPositionMap[index]);
        me.AddContentToTable(allColumns, sortRow, sortedColPositionMap);
    }

    private IsColumnLocked(cellIndexLeft: number, cellIndexRight: number, isBrandRelated: boolean) {
        if ((cellIndexLeft == 0) || (cellIndexRight == 0)) {
            return true;
        }
        if (isBrandRelated && (cellIndexLeft == 1 || cellIndexRight == 1)) {
            return true;
        }
        return false;
    }

    private CompareCells(leftHandCell: ScorecardCell, rightHandCell: ScorecardCell, isLocked: boolean): number {
        if (isLocked) {
            return 0;
        }
        const leftValue = this.SortOrderDescending ? leftHandCell : rightHandCell;
        const rightValue = this.SortOrderDescending ? rightHandCell : leftHandCell;

        if (leftValue.Value == undefined && rightValue.Value == undefined) {
            const leftString = leftValue.DisplayText ?? '';
            const rightString = rightValue.DisplayText ?? '';
            return (leftString.localeCompare(rightString));
        }

        const leftValToCompare = leftValue.Value == undefined ? 0 : leftValue.Value;
        const rightValToCompare = rightValue.Value == undefined ? 0 : rightValue.Value;

        return rightValToCompare - leftValToCompare;
    }

    private AppendColumnSettingsAndReturnTotalWidth(columns: ScorecardColumn[]): number {
        let totalWidth = 0;
        let colGroup = document.createElement("colgroup");
        columns.forEach((c, index) => {
            if (c) {
                var colElement = document.createElement("col");
                var useWidth = c.IsGap() ? c.Width / 3 : c.Width;
                colElement.style.width = useWidth + "px";
                totalWidth += c.Width;
                colGroup.appendChild(colElement);
            }
            else {
                console.log(`Warning column ${index} is null`)
                var colElement = document.createElement("col");
                var useWidth = Scorecard.DefaultInvalidColumnWdith();
                colElement.style.width = useWidth + "px";
                totalWidth += useWidth;
                colGroup.appendChild(colElement);

            }
        });
        this.Table.appendChild(colGroup);
        return totalWidth;
    }

    private AssociateColumnsWithBrands() {
        const filterTemplate: FilterTemplate = this.ReportStructure.FilterTemplateLookup[this.ReportPage.FilterTemplateName];
        if (filterTemplate) {
            this.Columns.map(column => {
                const myFilter = filterTemplate.Filters.filter(y => column.FilterId == y.Id)[0];
                if (myFilter) {
                    const myBrandId = myFilter.DisplayName;
                    const myBrand = this.ReportStructure.BrandRecords.filter(b => b.BrandName == myBrandId)[0]
                    column.AssociatedBrandRecord = myBrand;
                    column.AssociatedBrandId = myBrandId;
                    if (myBrand == undefined) {
                        const parts = myFilter.DisplayName.split(":");
                        if (parts.length > 1) {
                            parts.pop(); //Remove the last element
                            const brandId = parts.join(":");
                            column.AssociatedBrandRecord = this.ReportStructure.BrandRecords.filter(b => b.BrandName == brandId)[0];
                        }
                    }
                }
            });
            if (this.Columns.find(x => x.AssociatedBrandRecord != undefined)) {
                const columnsWithFailedAssociation = this.Columns.filter(col => col.AssociatedBrandRecord == undefined && col.AssociatedBrandId);
                if (columnsWithFailedAssociation.length) {
                    console.log(`Failed to associate brands ${columnsWithFailedAssociation.map(x => x.AssociatedBrandId)}`)
                }
            }
        }
    }

    public UpdateScorecard() {
        const me: Scorecard = this;
        const isSectionNotBrandRelated = me.ReportPage.Section.IsUnrelatedToBrand;
        let hasFilterBeenApplied: boolean = false;
        const filterTemplate: FilterTemplate = this.ReportStructure.FilterTemplateLookup[me.ReportPage.FilterTemplateName];
        var currentArray = filterTemplate ? filterTemplate.Filters.map(f => f) : [];
        if (filterTemplate && filterTemplate.CustomFieldHeadings) {
            var nHeadings = filterTemplate.CustomFieldHeadings.length;
            const maxPanelValues: { [id: string]: string; } = {};
            for (var i = 0; i < nHeadings; i++) {
                let dropDown = me.DropDowns[i];
                let selectedItem = dropDown.SelectedItem();
                let reducedArray = ((selectedItem == this.FilterForAll) || selectedItem.startsWith(this.FilterToIgnore)) ? currentArray : currentArray.filter(f => f.CustomField(i) == selectedItem);
                if (selectedItem != this.FilterForAll) {
                    maxPanelValues["Filter "+filterTemplate.CustomFieldHeadings[i]] = selectedItem;
                }
                currentArray = reducedArray;
            }

            if (Object.keys(maxPanelValues).length > 0) {
                maxPanelValues["Total Cols Displayed"] = currentArray.length.toString();
                MixPanel.trackWithContext("dashboardFiltersApplied", "ScoreCard", maxPanelValues);
                hasFilterBeenApplied = true;
            }
        }
        
        var showFilters: Filter[] = [];
        var currentBrand = this.ReportStructure.BrandRecords.filter(b => b.Id == me.ReportStructure.ActiveBrandId)[0];
        if (isSectionNotBrandRelated) {
            currentArray.forEach(f => {
                if (this.Columns.filter(x => x.FilterId == f.Id)?.length > 0) {
                    showFilters.push(f);
                }
            });
        }
        else if (currentBrand) {
            var currentBrandFilter = filterTemplate.Filters.filter(f => f.DisplayName == currentBrand.BrandName)[0];
            if (currentBrandFilter) {
                showFilters.push(currentBrandFilter);
                currentArray.forEach(f => {
                    if (currentBrandFilter && f.Id != currentBrandFilter.Id) {
                        //Does this filter exist somewhere in the column
                        if (this.Columns.filter(x => x.FilterId == f.Id)?.length > 0) {
                            showFilters.push(f);
                        }
                    }
                });
            }
        }
        else {
            currentArray.forEach(f => {
                showFilters.push(f);
            });
        }
        me.RenderCustom(showFilters, hasFilterBeenApplied);
        me.StoreDropDownStates();
    }

    public AddNav() {
        let me = this;
        var page = this.ReportPage;
        var filterTemplate: FilterTemplate = this.ReportStructure.FilterTemplateLookup[page.FilterTemplateName];
        let i = 0;
        if (filterTemplate && filterTemplate.CustomFieldHeadings) {
            this.DropDowns = [];
            filterTemplate.CustomFieldHeadings.forEach(h => {
                me.AddNavForCustomField(filterTemplate, i, h)
                i++;
            })
            me.AddSortNav();
        }
    }
    private IsRowIncludedInSortDropDown(row: ScorecardRow, inHeaderBlock: boolean): boolean {
        if ((row.RowType == ScorecardRowTypes.Main) ||
            (row.RowType == ScorecardRowTypes.VariableTitle && !inHeaderBlock)) {
            const cellsWithContent = row.Cells.filter((cell, index) => index != 0 && cell.DisplayText != undefined && cell.DisplayText.trim().length > 0);
            return cellsWithContent.length > 0;
        }
        return false;
    }

    private AddSortNav() {
        const me = this;
        const rowEndOfheadBlock = me.Rows.findIndex(row => !row.IsTitle());
        let validRows = me.Rows.filter(( row, index) => me.IsRowIncludedInSortDropDown(row, index < rowEndOfheadBlock));
        let items = Array.from(new Set(validRows.map(r => r.Cells[0].DisplayText)));
        let rowLookup = validRows.reduce((acc, r) => ({ ...acc, [r.Cells[0].DisplayText]: r }), {});
        items.unshift("None");
        me.SortDropDown = new DropDownMenu("", undefined, "Sort row", items, me.GetInitialItemForDropDown("Sort", undefined), rowLookup);
        me.SortDropDown.Render(this.NavDiv);

        me.SortDropDown.Select.addEventListener("change", function () {
            me.UpdateScorecard();
        });
    }

    private SortDropDown: DropDownMenu;
    private DropDowns: DropDownMenu[];
    private FilterToIgnore: string = "***";
    private FilterForAll: string = "All";

    private CreateListOfFilters(allFilters: string[]): string[] {
        const filters = allFilters.filter(filter => filter && filter.length > 0);
        const blankFilters = allFilters.filter(filter => !filter || filter.length == 0);
        if (blankFilters.length > 0) {
            filters.unshift(`${this.FilterToIgnore} ${blankFilters.length} filter${blankFilters.length > 1?'s':''} with missing configuration`);
        }
        filters.unshift(this.FilterForAll);
        return filters;
    }
    private AddNavForCustomField(filterTemplate: FilterTemplate, index: number, title: string) {
        const me = this;
        const allFilters = Array.from(new Set(filterTemplate.Filters.map(f => f.CustomField(index))));

        const dropDown = new DropDownMenu("", undefined, title, this.CreateListOfFilters(allFilters), me.GetInitialItemForDropDown(title, undefined), null);
        dropDown.Render(this.NavDiv);
        dropDown.Select.addEventListener("change", function () {
            me.UpdateNavContent(filterTemplate);
            me.UpdateScorecard();
        });
        this.DropDowns.push(dropDown);
    }


    private UpdateNavContent(filterTemplate: FilterTemplate) {
        let me = this;
        if (filterTemplate) {
            var nHeadings = filterTemplate.CustomFieldHeadings.length;
            var currentArray = filterTemplate.Filters.map(f => f);

            for (var i = 0; i < nHeadings - 1; i++) {
                const dropDown = me.DropDowns[i];
                const selectedItem = dropDown.SelectedItem();
                const reducedArray = ( (selectedItem == this.FilterForAll) || (selectedItem.startsWith(this.FilterToIgnore)) ) ? currentArray : currentArray.filter(f => f.CustomField(i) == selectedItem);

                const nextDropDown = me.DropDowns[i + 1];
                const nextSelectedItem = nextDropDown.SelectedItem();
                const items = this.CreateListOfFilters(Array.from(new Set(reducedArray.map(f => f.CustomField(i + 1)))));

                nextDropDown.UpdateItems(items, nextSelectedItem);
                nextDropDown.Select.enabled = (items.length > 1);
                currentArray = reducedArray;
            }
        }
    }

    private StoreDropDownStates() {
        let me = this;
        if (!me.ReportStructure.ScorecardDropDownStates) {
            me.ReportStructure.ScorecardDropDownStates = {};
        }
        me.DropDowns?.forEach(d => {
            me.ReportStructure.ScorecardDropDownStates[d.Title!] = d.SelectedItem();
        });
        me.ReportStructure.ScorecardDropDownStates["Sort"] = me.SortDropDown?.SelectedItem();
    }

    private GetInitialItemForDropDown(dropDownTitle: string, defaultValue: string | undefined): string | undefined {
        let me = this;
        if (!me.ReportStructure.ScorecardDropDownStates)
            return defaultValue;

        return me.ReportStructure.ScorecardDropDownStates[dropDownTitle] ?? defaultValue;
    }
}