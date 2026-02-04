import * as BrandVueApi from "../BrandVueApi";
import FileResponse = BrandVueApi.FileResponse;

export function saveFile(file: FileResponse, defaultFileName: string) {
    const fileName = file.fileName !== undefined ? file.fileName : defaultFileName;
    //TODO: Use a library for this code. See FileSaver.js
    if ((window.navigator as any).msSaveOrOpenBlob) {
        const blobObject = new Blob([file.data]);
        (window.navigator as any).msSaveOrOpenBlob(blobObject, fileName);
    } else {
        const link = document.createElement('a');
        const url = window.URL.createObjectURL(file.data);
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        window.setTimeout(() => {
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
        }, 100);
    }
}