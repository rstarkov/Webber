import { DateTime } from "luxon";
import { makeContext } from "./makeContext";
import { timeCorrectionMs } from "../blocks/_BlockBase";
import { useEffect, useState } from "react";

const ctx = makeContext(() => {
    const [updates, setUpdates] = useState(0);
    useEffect(() => {
        let timer = 0;
        function setTimer() { timer = setTimeout(() => { setUpdates(u => u + 1); setTimer(); }, 60000 - (Date.now() + timeCorrectionMs) % 60000); }
        setTimer();
        return () => {
            clearTimeout(timer);
        }
    }, []);
    let time = DateTime.utc().plus({ milliseconds: timeCorrectionMs });
    if (time.second >= 58) // we schedule the update as close as possible to the minute change; if it triggers slightly before then fast forward it to the next minute
        time = time.plus({ seconds: 60 - time.second });
    return { time, updates };
});

export const useTime = ctx.useFunc;
export const TimeProvider = ctx.provider;
