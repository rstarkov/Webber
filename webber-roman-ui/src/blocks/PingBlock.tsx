import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface PingBlockDto extends BaseDto {
    last: number | null;
    recent: (number | null)[];
}

function dtoPatcher(dto: PingBlockDto) {
}

const ctx = makeContext(() => {
    const block = useBlock<PingBlockDto>(`${Config.ServerUrl}/hub/PingBlock`, dtoPatcher);
    return block;
});

export const usePingBlock = ctx.useFunc;
export const PingBlockProvider = ctx.provider;
