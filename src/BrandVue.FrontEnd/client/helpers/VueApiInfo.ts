/*
 * Info needed to opt out of caching when the server API version has changed. Also see AssemblyVersionHeaderFilter.cs.
 */
class VueApiInfo {
    readonly clientVersionHeaderName = "ClientVueApiVersion";
    readonly serverVersionHeaderName = "ServerVueApiVersion";

    get clientApiVersion(): string {
        return (window as any).vueApiVersion;
    }
}

export default new VueApiInfo();