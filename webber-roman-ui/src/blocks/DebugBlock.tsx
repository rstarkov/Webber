import { useState } from "react";
import { makeContext } from "../util/makeContext";

const ctx = makeContext(() => {
    const [logs, setLogs] = useState<string[]>([]);
    const pushLog = (str: string) => {
        setLogs(lgs => [str, ...lgs.slice(0, 9)]);
    };
    return { logs, pushLog };
});

export const useDebugBlock = ctx.useFunc;
export const DebugBlockProvider = ctx.provider;
