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