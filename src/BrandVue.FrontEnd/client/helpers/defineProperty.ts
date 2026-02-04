const defineProperty = <F, T>(on: F, returns: T, propertyName: string) => {
    Object.defineProperty(on, propertyName,
        {
            get: () => returns,
            configurable: true
        });
}

export default defineProperty;