export class DropDownMenu {

    public Parent?: any;
    public Div: any;
    public Select: any;
    public Title: string | undefined;
    public Items: string[];
    private Id: string;
    private Options: any[] = [];
    private Initial?: string;
    private Lookup: any;
    public Index: number;

    constructor(id: string, parent: any, title: string | undefined, items: string[], initial?: string, lookup?: any) {
        this.Id = id;
        this.Parent = parent;
        this.Title = title;
        this.Items = items;
        this.Initial = initial;
        this.Lookup = lookup;
    }

    public HtmlName(): string {
        return this.Id;
    }

    public Render(fieldGroupDiv: HTMLElement) {
        this.Div = document.createElement("div");
        this.Div.className = "rv-field";
        fieldGroupDiv.appendChild(this.Div);

        let div = document.createElement("div");
        div.className = "rv-control";

        this.Div.appendChild(div);

        if (this.Title) {
            let label = document.createElement("label");
            label.className = "rv-label-select";
            label.textContent = this.Title;
            div.appendChild(label);
        }

        let selectDiv = document.createElement("span");
        selectDiv.className = "rv-select";
        div.appendChild(selectDiv);

        this.Select = document.createElement("select");
        this.Select.id = this.Id;
        selectDiv.appendChild(this.Select);

        this.AddArrayToSelect(this.Items);
    }

    public SetActive(item: string, update: boolean) {
        if (item) {
            let selectItem = (this.Items.indexOf(item) > -1) ? item : this.Items[0];
            this.Select.value = selectItem;
            if (update) {
                this.Update();
            }
        }
    }

    public SetActiveId(id: string, update: boolean) {
        if (id) {
            let me = this;
            let element: any = document.getElementById(this.Id);
            let selected: string = "";
            for (let key in this.Lookup) {
                let o = me.Lookup[key];
                if (o && o["Id"] == id) {
                    selected = key;
                }
            }
            if (selected) {
                element.value = selected;
                if (update) {
                    this.Update();
                }
            }
        }
    }


    public SelectedItem(): string {
        return this.Select.value;
    }
    public SelectedObject(): any {
        let result = this.Lookup ? this.Lookup[this.Select.value] : null;
        return result ? result : this.Select.value;
    }
    public SelectedId(): number {
        let object = this.Lookup ? this.Lookup[this.Select.value] : null;
        return object["Id"] ? +object["Id"] : -1;
    }

    public Update() {

    }

    public UpdateItems(items: string[], initial: string) {
        if (items.join("|") !== this.Items.join("|")) {
            if (initial) {
                this.Initial = initial;
            } else {
                this.Initial = this.SelectedItem();
            }
            this.Items = items;
            this.Select.innerHTML = "";
            this.AddArrayToSelect(this.Items);
        }
    }

    private AddArrayToSelect(array: string[]) {
        let me = this;
        me.Options = [];

        for (let text of array) {
            let option = document.createElement("option");
            option.value = text;
            option.textContent = text;
            this.Select.appendChild(option);
            me.Options.push(option);
        }

        if (me.Initial) {
            me.SetActive(me.Initial, false);
        } else {
            me.SetActive(array[0], false);
        }
    }
}