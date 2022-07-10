import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface ReloadBlockDto extends BaseDto {
    serverHash: string;
}

const ctx = makeContext(() => {
    const block = useBlock<ReloadBlockDto>(`${Config.ServerUrl}/hub/ReloadBlock`, () => {});
    return block;
});

export const useReloadBlock = ctx.useFunc;
export const ReloadBlockProvider = ctx.provider;
