import { MixPanel } from "../../../../../mixpanel/MixPanel";
import { DropDownMenu } from "../../Nav/DropDownMenu";
import { FormatTypes } from "../Format";
import { ReportStructure } from "../ReportStructure";
import { CommentColumn, DialPadAlignment } from "./CommentColumn";
import { CommentRow } from "./CommentRow";
import style from "./CommentTable.module.less";

export class CommentTable {

    private _resetButton: HTMLButtonElement;
    public Rows: CommentRow[];
    public Columns: CommentColumn[];
    public NavDiv: HTMLDivElement;
    public Table: HTMLTableElement;
    public ReportStructure: ReportStructure;
    private Filters: NodeListOf<HTMLInputElement>;
    private Div: HTMLElement;
    private FilterRow: HTMLTableRowElement;
    public static async LoadCommentTableFromJson(url: string): Promise<CommentTable | null> {
        var commentTable: CommentTable | null = null;
        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            commentTable = await new CommentTable(await response.json());
        } catch (error) {
            console.error("Error fetching JSON:", error);
        }
        return commentTable;
    }

    constructor(json: any) {
        this.Populate(json);
    }

    private Populate(json: any) {
        this.Columns = CommentColumn.GetCommentColumns(this, json.Columns);
        this.Rows = CommentRow.GetCommentRows(this, json.Rows);
    }

    public Render(div: HTMLElement) {

        var me = this;
        me.Div = div;
        me.NavDiv = document.createElement("div");
        div.appendChild(me.NavDiv);

        var innerDiv = document.createElement("div");
        innerDiv.style.width = "100%";
        innerDiv.style.height = "100%";
        innerDiv.style.overflow = "auto";
        div.appendChild(innerDiv);

        me.Table = document.createElement("table");
        me.Table.className = style.commentTable;
        me.Table.style.fontFamily = me.ReportStructure.Formats[FormatTypes.XAxisTitle].FontName;
        me.Table.style.fontSize = me.ReportStructure.Formats[FormatTypes.XAxisTitle].FontSize + "px";

        innerDiv.appendChild(me.Table);

        let totalWidth = me.AppendColumnSettingsAndReturnTotalWidth(me.Columns, me.Rows);
        me.Table.style.width = totalWidth + "px";
        let body = document.createElement("tbody");
        me.Table.appendChild(body);
        me.Rows.forEach(r => {
            var row = document.createElement("tr");
            row.className = style.row;
            r.Cells.forEach(c => {
                var cell = document.createElement("td");
                if (c.Text) {
                    cell.innerText = c.Text;
                }
                me.ApplyColumnFormat(c.Column, cell);
                cell.style.padding = "2px";
                row.appendChild(cell);
            });
            body.appendChild(row);
        });
        me.SupportSorting();
        me.SupportFiltering();
        me.AddCopyTableButton(me.NavDiv);
        me.AddResetButton(me.NavDiv);
    }

    private DropDowns: DropDownMenu[];

    private AddDropDownListBox(head: HTMLElement, name: string, items: string[], index: number): DropDownMenu {
        const me = this;
        items.unshift("All");
        const dropDown = new DropDownMenu(name, undefined, undefined, items, undefined, null);
        dropDown.Index = index;
        dropDown.Render(head);
        this.DropDowns.push(dropDown);

        dropDown.Select.addEventListener("change", function () {
            me.FilterRows();
        });
        return dropDown;
    }

    private listOfUniqueAnswers(commentRows: CommentRow[], columnId: number): string[] {
        var dictionaryOfItems: { [item: string]: number; } = {};
        commentRows.forEach(row => {
            const val = row.Cells[columnId].Text;
            dictionaryOfItems[val]++;
        });
        return Object.keys(dictionaryOfItems);
    }


    private AppendColumnSettingsAndReturnTotalWidth(commentColumns: CommentColumn[], commentRows: CommentRow[]): number {
        let me = this;
        let totalWidth = 0;
        let colGroup = document.createElement("colgroup");
        me.Table.appendChild(colGroup);

        var thead = document.createElement("thead");

        var titleRow = document.createElement("tr");
        me.FilterRow = document.createElement("tr");
        thead.appendChild(titleRow);
        thead.appendChild(me.FilterRow);
        me.Table.appendChild(thead);
        this.DropDowns = [];
        commentColumns.forEach((coloumn, columnIndex) => {
            var colElement = document.createElement("col");
            var useWidth = coloumn.Width;
            colElement.style.width = useWidth + "px";
            totalWidth += coloumn.Width;
            colGroup.appendChild(colElement);

            var col = document.createElement("th");
            me.ApplyColumnFormat(coloumn, col);
            col.dataset.sort = coloumn.Title;
            col.title = `Click on ${coloumn.Title} to sort`;
            col.dataset.direction = "none";
            col.innerText = coloumn.Title;
            col.style.cursor = "pointer";
            titleRow.appendChild(col);

            var filterHead = document.createElement("th");
            const uniqueItems = this.listOfUniqueAnswers(commentRows, columnIndex);
            const tooLongTextLength = 50;
            const tooManyItems = 15;
            const hasLongText = uniqueItems.find(text => text.length > tooLongTextLength);

            if (columnIndex == 0 || (hasLongText) || (uniqueItems.length > tooManyItems)) {
                const filter = document.createElement("input");
                filter.type = "text";
                filter.className = style.filter;
                filter.dataset.filter = coloumn.Title;
                filter.dataset.dataColumnIndex = columnIndex.toString();
                filter.placeholder = "Filter by " + coloumn.Title;
                const prevValue = this.GetInitialItem(coloumn.Title, undefined);
                if (prevValue) {
                    filter.value = prevValue;
                }
                filterHead.appendChild(filter);

            }
            else {
                const menu = me.AddDropDownListBox(filterHead, coloumn.Title, uniqueItems, columnIndex);
                const prevValue = this.GetInitialItem(menu.Index.toString(), undefined);
                if (prevValue) {
                    menu.SetActive(prevValue, true);
                }
            }
            me.FilterRow.appendChild(filterHead);

        })
        return totalWidth;
    }

    private ApplyColumnFormat(column: CommentColumn, div: any) {
        switch (column.DialPadAlignment) {
            case DialPadAlignment.TopLeft:
            case DialPadAlignment.MiddleLeft:
            case DialPadAlignment.BottomLeft:
                div.style.textAlign = "left";
                break;
            case DialPadAlignment.TopCenter:
            case DialPadAlignment.MiddleCenter:
            case DialPadAlignment.BottomCenter:
                div.style.textAlign = "center";
                break;
        }
    }

    private sortData(columnIndex: number, direction: string | undefined, target: HTMLElement) {
        let me = this;

        target.dataset.direction = direction;

        const rows = Array.from(me.Table.tBodies[0].rows);
        const sortedRows = rows.sort((a, b) => {
            const cellA = a.cells[columnIndex].textContent || '';
            const cellB = b.cells[columnIndex].textContent || '';
            const comparison = cellA.localeCompare(cellB);
            return direction === 'asc' ? comparison : -comparison;
        });
        target.setAttribute('data-direction', direction === 'asc' ? 'desc' : 'asc');

        me.Table.tBodies[0].append(...sortedRows);
    }

    private SupportSorting() {
        let me = this;
        const column = this.GetInitialItem("SortColumn", undefined);
        const direction = this.GetInitialItem("SortDirection", undefined);

        me.Table.querySelector('thead')!.addEventListener('click', (event) => {
            const target = event.target as HTMLElement;
            const sortAttribute = target.getAttribute('data-sort');
            const sortDirection = target.getAttribute('data-direction');
            if (!sortAttribute) return;
            const columnIndex = Array.from(target.parentElement!.children).indexOf(target);
            this.StoreSortOrder(columnIndex, sortDirection ?? "asc");

            this.sortData(columnIndex, sortDirection ?? undefined, target)
            me._resetButton.disabled = false;

            const headers = me.Table.querySelectorAll<HTMLTableHeaderCellElement>("th");
            headers.forEach(h => {
                if (h !== target) {
                    h.dataset.direction = "none";
                }
            });
            target.setAttribute('data-direction', sortDirection === 'asc' ? 'desc' : 'asc');
        });
        const headers = me.Table.querySelectorAll<HTMLTableHeaderCellElement>("th");
        headers.forEach(target => {
            const sortAttribute = target.getAttribute('data-sort');
            if (sortAttribute) {
                const columnIndex = Array.from(target.parentElement!.children).indexOf(target);
                if (columnIndex.toString() === column) {
                    this.sortData(columnIndex, direction, target)
                }
            }
        });
    }

    private SupportFiltering() {
        let me = this;
        me.Filters = me.Table.querySelectorAll(`.${style.filter}`);
        me.Filters.forEach((filter: HTMLInputElement) => {
            filter.addEventListener('input', function () {
                me.FilterRows();
            });
        });
        me.FilterRows();
    }

    private dataToTabDelimited(table: HTMLTableElement): string {
        const headersAsText = this.getHeadersAsText(table);
        const rowsAsData = Array.from(table.tBodies[0].rows).map(row => this.getRowAsTabDelimited(row));
    
        return headersAsText.join("\t") + "\r\n" + rowsAsData.join("\r\n");
    }
    
    private getHeadersAsText(table: HTMLTableElement): string[] {
        const headers = table.querySelectorAll<HTMLTableCellElement>("th");
        const headersAsText: string[] = [];
        headers.forEach((header, pos) => {
            if (pos < headers.length / 2 && header.textContent) {
                headersAsText.push(header.textContent);
            }
        });
        return headersAsText;
    }
    
    private getRowAsTabDelimited(row: HTMLTableRowElement): string {
        const items: string[] = [];
        row.childNodes.forEach(node => {
            if (node.textContent) {
                items.push(node.textContent);
            }
        });
        return items.join("\t");
    }


    private FilterRows() {
        let me = this;
        const rows = Array.from(me.Table.tBodies[0].rows);
        const maxPanelValues: { [id: string]: string; } = {};
        rows.forEach(row => {
            let showRow = true;
            me.Filters.forEach((filter: HTMLInputElement) => {
                const dataColumnIndex = filter.dataset.dataColumnIndex ? +filter.dataset.dataColumnIndex : 0;
                const filterValue = filter.value.toLowerCase();
                let cellValue = row.cells[dataColumnIndex].textContent || '';
                if (filterValue.length > 0) {
                    const key = "Filter " + filter.dataset.filter;
                    if (Object.keys(maxPanelValues).find(x => x == key) == undefined) {
                        maxPanelValues[key] = filter.value;
                    }
                }
                if (!cellValue.toLowerCase().includes(filterValue)) {
                    showRow = false;
                }
            });
            me.DropDowns.forEach(dropDown => {
                const dropDownValue = dropDown.SelectedItem();
                if (dropDownValue != "All") {
                    const key = "Filter " + dropDown.HtmlName();
                    if (Object.keys(maxPanelValues).find(x => x == key) == undefined) {
                        maxPanelValues[key] = dropDownValue;
                    }
                    let cellValue = row.cells[dropDown.Index].textContent || '';
                    if (cellValue != dropDownValue)
                        showRow = false;
                }
            })
            row.style.display = showRow ? '' : 'none';
        });
        if (Object.keys(maxPanelValues).length > 0) {
            if (me._resetButton) {
                me._resetButton.disabled = false;
            }
            const rowsDisplayed = rows.filter(x => x.style.display == '').length;
            maxPanelValues["Total Rows Displayed"] = rowsDisplayed.toString();
            MixPanel.trackWithContext("dashboardFiltersApplied", "CommentTable", maxPanelValues);
        }
        me.StoreDropDownStates();
    }

    private AddCopyTableButton(div: HTMLDivElement) {
        const copyButton = this.createCopyButton();
        div.appendChild(copyButton);

        copyButton.addEventListener("click", async () => {
            MixPanel.trackWithContext("dashboardCopyToClipboard", "CommentTable");
            const filterDisplay = this.FilterRow.style.display;
            this.FilterRow.style.display = "none";

            try {
                const cleanedTableHtml = this.getFilteredTableHtml();
                const cleanedTable = this.getFilteredTable();
                await this.copyToClipboard(cleanedTableHtml, cleanedTable);
            } catch (error) {
                alert("Error copying table to the clipboard");
            } finally {
                this.FilterRow.style.display = filterDisplay;
            }
        });
    }

    private createCopyButton(): HTMLButtonElement {
        const copyButton = document.createElement("button");
        copyButton.title = "Copy to Excel with preserved table format";
        copyButton.className = "rv-button";

        const html_i = document.createElement("i");
        html_i.className = "material-symbols-outlined";
        html_i.textContent = "content_copy";

        const text = document.createElement("span");
        text.textContent = "Copy table";

        copyButton.appendChild(html_i);
        copyButton.appendChild(text);

        return copyButton;
    }

    private async copyToClipboard(cleanedTableHtml: string, cleanedTable: HTMLTableElement) {
        const newLocal = 'text/html';
        const textHtmlBlob = new Blob([cleanedTableHtml], { type: newLocal });
        const textPlainBlob = new Blob([this.dataToTabDelimited(cleanedTable)], { type: 'text/plain' });

        const clipboardItem = new ClipboardItem({
            'text/html': textHtmlBlob,
            'text/plain': textPlainBlob,
        });

        await navigator.clipboard.write([clipboardItem]);
    }

    private getFilteredTableHtml(): string {
        const { rowsToExclude, tableClone } = this.cloneAndFilterRows();

        rowsToExclude.forEach(row => row.remove());

        // Return the cleaned HTML
        return tableClone.outerHTML
            .replace(/font-size: \d+px;/g, "")
            .replace(/<input [^>]*?>/g, "")
            .replace(/<\/input>/g, "")
            .replace(/<span.*?<\/span>/g, "");
    }

    private cloneAndFilterRows() {
        const tableClone = this.Table.cloneNode(true) as HTMLTableElement;

        // Remove rows matching specific criteria (e.g., FilterRow)
        const rowsToExclude = Array.from(tableClone.querySelectorAll("tr"))
            .filter(row => row.style.display === "none" || row === this.FilterRow);
        return { rowsToExclude, tableClone };
    }

    private getFilteredTable(): HTMLTableElement {
        const { rowsToExclude, tableClone } = this.cloneAndFilterRows();

        rowsToExclude.forEach(row => row.remove());

        return tableClone;
    }

    private AddResetButton(div: HTMLDivElement) {
        var me = this;
        let isFilterSet: boolean = false;
        me.Filters.forEach((filter: HTMLInputElement, index) => {
            const filterValue = filter.value.toLowerCase();
            if (filterValue.length) {
                isFilterSet = true;
            }
        });
        me.DropDowns.forEach(dropDown => {
            const dropDownValue = dropDown.SelectedItem();
            if (dropDownValue != "All") {
                isFilterSet = true;
            }
        });
        const columnText = this.GetInitialItem("SortColumn", undefined)
        me._resetButton = document.createElement("button");
        me._resetButton.className = "rv-button";
        me._resetButton.disabled = !isFilterSet && (columnText == undefined || columnText.length == 0);
        me._resetButton.title = "Reset the all filters and sorting of columns";
        const html_i = document.createElement("i");
        html_i.className = "material-symbols-outlined";
        html_i.textContent = "autorenew";
        me._resetButton.appendChild(html_i);
        div.appendChild(me._resetButton);
        me._resetButton.addEventListener("click", function () {
            MixPanel.trackWithContext("dashboardFiltersCleared", "CommentTable");
            me.Div.innerHTML = "";
            me.ResetState();
            me.Render(me.Div);
            me._resetButton.disabled = true;
        });
    }

    private StoreSortOrder(col: number, direction: string) {
        let me = this;
        if (!me.ReportStructure.ApplicationUIStates) {
            me.ReportStructure.ApplicationUIStates = {};
        }
        console.log("Store: Sort " + col + " " + direction);
        me.ReportStructure.ApplicationUIStates["ct_SortColumn"] = col.toString();
        me.ReportStructure.ApplicationUIStates["ct_SortDirection"] = direction;
    }

    private ResetState() {
        let me = this;
        if (!me.ReportStructure.ApplicationUIStates) {
            me.ReportStructure.ApplicationUIStates = {};
        }
        for (const key in me.ReportStructure.ApplicationUIStates) {
            if (key.startsWith("ct")) {
                me.ReportStructure.ApplicationUIStates[key] = "";
            }
        }
    }

    private StoreDropDownStates() {
        let me = this;
        if (!me.ReportStructure.ApplicationUIStates) {
            me.ReportStructure.ApplicationUIStates = {};
        }
        me.Filters?.forEach(filter => {
            const name = filter.dataset.filter;
            if (name != undefined) {
                me.ReportStructure.ApplicationUIStates[`ct_${name}`] = filter.value.toLowerCase();
            }
        });
        me.DropDowns?.forEach(dropDown => {
            const name = dropDown.Index.toString();
            if (name != undefined) {
                me.ReportStructure.ApplicationUIStates[`ct_${name}`] = dropDown.SelectedItem();
            }

        });
    }

    private GetInitialItem(dropDownTitle: string, defaultValue: string | undefined): string | undefined {
        let me = this;
        if (!me.ReportStructure.ApplicationUIStates)
            return defaultValue;

        return me.ReportStructure.ApplicationUIStates[`ct_${dropDownTitle}`] ?? defaultValue;
    }
}