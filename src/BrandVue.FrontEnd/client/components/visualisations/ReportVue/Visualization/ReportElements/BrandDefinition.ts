export class EntityDefinition {
    constructor(private singular: string, private plural: string) { }
    public Singular(): string { return this.singular; }
    public Plural(): string { return this.plural; }
}

function ReadFromJson(json: any, defaultSingular: string, defaultPlural: string): EntityDefinition{
    let singular:string|null = null;
    let plural: string | null = null;
    if (json) {
        singular = json.SingularTitle;
        plural = json.PlurarlTitle;
    }
    if (!singular) {
        singular = defaultSingular;
    }
    if (!plural) {
        plural = defaultPlural
    }
    return new EntityDefinition(singular, plural);
}

export class BrandDefinition extends EntityDefinition{
    constructor(json: any) {
        const val = ReadFromJson(json, "Brand", "Brands");
        super(val.Singular(), val.Plural())
    }

}
export class SectionDefinition extends EntityDefinition {
    constructor(json: any) {
        const val = ReadFromJson(json, "Section", "Sections");
        super(val.Singular(), val.Plural())
    }

}