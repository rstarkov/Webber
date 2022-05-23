import { createContext, useContext } from "react";

export function makeContext<T>(hook: () => T) {
    const context = createContext({} as T);

    const useFunc = (): T => useContext(context);

    const provider = ({ ...rest }) => <context.Provider value={hook()} {...rest} />;

    return { useFunc, provider };
}
