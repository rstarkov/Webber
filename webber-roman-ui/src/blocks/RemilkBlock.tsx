import { DateTime } from "luxon";
import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { endOfLocalDay } from "../util/util";
import { BaseDto, useBlock } from "./_BlockBase";

export interface RemilkBlockDto extends BaseDto {
    tasks: RemilkTask[];
}

export interface RemilkTask {
    id: string;
    dueUtc: DateTime;
    hasDueTime: boolean;
    priority: number;
    description: string;
    tags: string[];
}

function dtoPatcher(dto: RemilkBlockDto) {
    for (let i = 0; i < dto.tasks.length; i++) {
        if (dto.tasks[i].dueUtc) {
            dto.tasks[i].dueUtc = DateTime.fromISO(dto.tasks[i].dueUtc as any).setZone("Europe/London");
            if (!dto.tasks[i].hasDueTime)
                dto.tasks[i].dueUtc = endOfLocalDay(dto.tasks[i].dueUtc, false);
        } else {
            dto.tasks[i].dueUtc = endOfLocalDay(DateTime.now(), true);
        }
    }
}

const ctx = makeContext(() => {
    const block = useBlock<RemilkBlockDto>(`${Config.ServerUrl}/hub/RemilkBlock`, dtoPatcher);
    return block;
});

export const useRemilkBlock = ctx.useFunc;
export const RemilkBlockProvider = ctx.provider;
