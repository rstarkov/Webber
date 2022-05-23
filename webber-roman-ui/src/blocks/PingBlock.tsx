import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface PingBlockDto extends BaseDto {
    last: number | null;
    recent: (number | null)[];
}

function dtoPatcher(dto: PingBlockDto) {
}

const ctx = makeContext(() => {
    const [dto, connection] = useBlock<PingBlockDto>("http://localhost/hub/PingBlock", dtoPatcher);

    return {
        dto,
        connection,
    };
});

export const usePingBlock = ctx.useFunc;
export const PingBlockProvider = ctx.provider;
