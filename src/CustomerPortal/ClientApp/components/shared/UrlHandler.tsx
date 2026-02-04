const UrlReplacer = {
    removeParameter(paramName: string) {
        const url = new URL(location.href);
        url.searchParams.delete(paramName);

        return url.toString();
    },

    addParameter(paramName: string) {
        const url = new URL(location.href);
        url.searchParams.append(paramName, '');

        return url.toString();
    },

    setParameter(paramName: string, value: string) {
        const url = new URL(location.href);
        url.searchParams.set(paramName, value);

        return url.toString();
    }
};

export default UrlReplacer;
