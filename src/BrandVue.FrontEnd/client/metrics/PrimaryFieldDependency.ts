interface IPrimaryFieldDependency {
    name: string;
    itemNumber: number;
}

export class PrimaryFieldDependency implements IPrimaryFieldDependency {
    public name: string;
    public itemNumber: number;

    constructor(data: IPrimaryFieldDependency) {
        this.name = data.name,
        this.itemNumber = data.itemNumber
    }
}