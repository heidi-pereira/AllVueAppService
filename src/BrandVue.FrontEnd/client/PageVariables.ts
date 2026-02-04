class PageVariables {
    private pageWindow: any;
    private headTag: any;

    constructor() {
        this.pageWindow = <any>window;
        this.headTag = document.getElementsByTagName('head')[0];
    }

    populate() {
        this.pageWindow.appBasePath = this.headTag.dataset.appbasepath;
        this.pageWindow.productName = this.headTag.dataset.productname;
        this.pageWindow.productDisplayName = this.headTag.dataset.productdisplayname;
        this.pageWindow.vueApiVersion = this.headTag.dataset.vueapiversion;
    }
}

export default new PageVariables();