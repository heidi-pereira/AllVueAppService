export class PageLoadIndication {
    public static instance = new PageLoadIndication(()=>{});
    private loadedIndicatorId = 'loaded';
    public postLoadEvent: () => void | null;

    constructor(postLoadEvent: () => void) {
        this.postLoadEvent = postLoadEvent;
        this.handleStart = this.handleStart.bind(this);
        this.handleEnd = this.handleEnd.bind(this);
    }

    private xhrInflight = 0;
    public xhrStart: number | null = null;
    private setLoadedIndicatorHandle = 0;

    public handleStart = () => {
        if (this.xhrInflight === 0) {
            const loadedIndicator = document.getElementById(this.loadedIndicatorId);
            loadedIndicator?.parentNode!.removeChild(loadedIndicator);
        }

        if (!this.xhrStart) {
            this.xhrStart = Date.now()
        }

        this.xhrInflight++;
        if (this.setLoadedIndicatorHandle !== 0) {
            clearTimeout(this.setLoadedIndicatorHandle);
            this.setLoadedIndicatorHandle = 0;
        }
    }

    public handleEnd = () => {
        this.xhrInflight--;
        if (this.xhrInflight === 0) {
            this.setLoadedIndicatorHandle = window.setTimeout(() => {
                    var body = document.getElementsByTagName("body")[0];
                    var loadedIndicator = document.createElement("span");
                    loadedIndicator.id = this.loadedIndicatorId;
                    body.appendChild(loadedIndicator);
                    if(this.postLoadEvent != null) { this.postLoadEvent(); }
                    this.xhrStart = null;
                },
                750); // Allow some time for other requests to begin before marking the page as loaded
        }
        
    }
}