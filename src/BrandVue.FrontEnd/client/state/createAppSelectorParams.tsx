import { RootState } from "./store";

/**
 * Helper factory for concisely adding parameters onto createAppSelector calls
 * Usage example:
 * const selectSomething = createAppSelector(
 *   [state => state.something, ...Params.two<IEntityConfiguration, IEntitySetFactory>()],
 *   (somethingFromState, entityConfiguration, entitySetFactory) => {
 *     // return selector result to be cached
 *  });
 *
 **/
export class Params {
    private static first<T1>(_state: RootState, p1: T1): T1 { return p1; }
    private static second<T1, T2>(_state: RootState, _p1: T1, p2: T2): T2 { return p2; }
    private static third<T1, T2, T3>(_state: RootState, _p1: T1, _p2: T2, p3: T3): T3 { return p3; }

    public static one<T1>(): [typeof Params.first<T1>] {
        return [(Params.first<T1>)];
    }
    public static two<T1, T2>(): [typeof Params.first<T1>, typeof Params.second<T1, T2>] {
        return [(Params.first<T1>), (Params.second<T1, T2>)];
    }
    public static three<T1, T2, T3>(): [typeof Params.first<T1>, typeof Params.second<T1, T2>, typeof Params.third<T1, T2, T3>] {
        return [(Params.first<T1>), (Params.second<T1, T2>), (Params.third<T1, T2, T3>)];
    }
}
