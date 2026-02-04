export class ProjectNotFoundError extends Error {
    public static typeDiscriminator = "ProjectNotFound";
    public typeDiscriminator = ProjectNotFoundError.typeDiscriminator;

    constructor(msg?: string) {
        super(!!msg ? msg : "Project was not found.");
    }
}