import { DateTime } from "luxon";
import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface WeatherDotComBlockDto extends BaseDto {
    hours: WeatherDotComForecastHourDto[];
}

export interface WeatherDotComForecastHourDto {
    dateTime: DateTime;
    cloudCover: number;
    precipChance: number;
    precipMm: number;
}

function dtoPatcher(dto: WeatherDotComBlockDto) {
    for (let k = 0; k < dto.hours.length; k++) {
        dto.hours[k].dateTime = DateTime.fromISO(dto.hours[k].dateTime as any);
    }
}

const ctx = makeContext(() => {
    const block = useBlock<WeatherDotComBlockDto>(`${Config.ServerUrl}/hub/WeatherDotComBlock`, dtoPatcher);
    return block;
});

export const useWeatherDotComBlock = ctx.useFunc;
export const WeatherDotComBlockProvider = ctx.provider;
