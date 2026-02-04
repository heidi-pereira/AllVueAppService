import { IProject } from "./CustomerPortalApi";

export function clone<T>(object: T): T {
    return JSON.parse(JSON.stringify(object));
}

export function isObjectEmpty(obj): boolean {
    for (var key in obj) {
        if (obj.hasOwnProperty(key))
            return false;
    }
    return true;
}

export function emptyForm(form: HTMLFormElement): void {
    var inputs = Array.from(form.querySelectorAll("input, select, textarea"));
    inputs.forEach(x => {
        var inputType = x.getAttribute("type");
        if (inputType === "checkbox" || inputType === "radio") {
            (x as any).checked = false;
        } else {
            (x as any).value = "";
        }
    });
}

export function getResearchPortalUrl(window: Window) {
    if(window.location.host.includes("localhost"))
    {
        return `http://localhost:44394`;
    }

    if(window.location.host.includes("test")) {
        return `https://researchportal.test.savanta.com`;
    }

    if(window.location.host.includes("beta") || window.location.host.includes("uat")) {
        return `https://researchportal.beta.savanta.com`;
    }

    return `https://researchportal.savanta.com`;
}

export const NoProjectAccessQueryParam = "NoProjectAccess";

export function isProjectShared(project: IProject) {
    return project.isSharedWithAllUsers || project.numberOfUsers > 0;
}

export const SAVANTA_SHORTCODE = 'savanta';