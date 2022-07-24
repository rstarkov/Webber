import { DateTime } from "luxon";
import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface TimeUntilBlockDto extends BaseDto {
    events: CalendarEvent[];
}

interface CalendarEvent {
    id: string;
    displayName: string;
    startTimeUtc: DateTime;
    hasStarted: boolean;
    isNextUp: boolean;
    isRecurring: boolean;
}

function dtoPatcher(dto: TimeUntilBlockDto) {
    for (let i = 0; i < dto.events.length; i++) {
        dto.events[i].startTimeUtc = DateTime.fromISO(dto.events[i].startTimeUtc as any);
    }
}

const ctx = makeContext(() => {
    const block = useBlock<TimeUntilBlockDto>(`${Config.ServerUrl}/hub/TimeUntilBlock`, dtoPatcher);
    return block;
});

export const useTimeUntilBlock = ctx.useFunc;
export const TimeUntilBlockProvider = ctx.provider;
