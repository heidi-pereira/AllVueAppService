/*
 * Fulfill resolved promises until condition is met (or maxPromises is hit)
 * Ideally something like jest.runAllTicks would let us fulfill all resolved promises, but it doesn't seem to so we have this.
 */
export default class AssertAfterPromisesFulfilled {

    public static notContains(getHaystack: () => string, needle: string, maxPromises = 100) {

        return new Promise<void>(async (res, rej) => {
            for (let i = 0; i < maxPromises && getHaystack().indexOf(needle) > -1; i++) {
                await Promise.resolve();
            }
            expect(getHaystack()).not.toContain(needle);
            res();
        });
    }

    public static contains(getHaystack: () => string, needle: string, maxPromises = 100) {

        return new Promise<void>(async (res, rej) => {
            for (let i = 0; i < maxPromises && getHaystack().indexOf(needle) === -1; i++) {
                await Promise.resolve();
            }
            expect(getHaystack()).toContain(needle);
            res();
        });
    }

    public static toBeNull(getHaystack: () => any, maxPromises = 100) {

        return new Promise<void>(async (res, rej) => {
            for (let i = 0; i < maxPromises && getHaystack() !== null; i++) {
                await Promise.resolve();
            }
            expect(getHaystack()).toBeNull();
            res();
        });
    }
}