import { DateTime } from "luxon";
import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface RainCloudBlockDto extends BaseDto {
    rain: RainCloudPtDto[];
    cloud: RainCloudPtDto[];
}

export interface RainCloudPtDto {
    atUtc: DateTime;
    counts: number[];
}

function dtoPatcher(dto: RainCloudBlockDto) {
    for (let i = 0; i < dto.rain.length; i++) {
        dto.rain[i].atUtc = DateTime.fromISO(dto.rain[i].atUtc as any);
    }
    for (let i = 0; i < dto.cloud.length; i++) {
        dto.cloud[i].atUtc = DateTime.fromISO(dto.cloud[i].atUtc as any);
    }
}

const ctx = makeContext(() => {
    const block = useBlock<RainCloudBlockDto>(`${Config.ServerUrl}/hub/RainCloudBlock`, dtoPatcher);
    return block;
});

export const useRainCloudBlock = ctx.useFunc;
export const RainCloudBlockProvider = ctx.provider;
