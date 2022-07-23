import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface RouterHistoryPoint {
    txRate: number;
    rxRate: number;
}

export interface RouterBlockDto extends BaseDto {
    rxLast: number;
    txLast: number;
    rxAverageRecent: number;
    txAverageRecent: number;
    historyRecent: (RouterHistoryPoint | null)[];
    historyHourly: (RouterHistoryPoint | null)[];
}

function dtoPatcher(dto: RouterBlockDto) {
}

const ctx = makeContext(() => {
    const block = useBlock<RouterBlockDto>(`${Config.ServerUrl}/hub/RouterBlock`, dtoPatcher);
    return block;
});

export const useRouterBlock = ctx.useFunc;
export const RouterBlockProvider = ctx.provider;
