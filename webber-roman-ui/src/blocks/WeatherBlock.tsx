import { Config } from "../config";
import { makeContext } from "../util/makeContext";
import { BaseDto, useBlock } from "./_BlockBase";

export interface WeatherBlockDto extends BaseDto {
    curTemperature: number;
    curTemperatureColor: string;
    minTemperature: number;
    minTemperatureColor: string;
    minTemperatureAtTime: string;
    minTemperatureAtDay: string;
    maxTemperature: number;
    maxTemperatureColor: string;
    maxTemperatureAtTime: string;
    maxTemperatureAtDay: string;
    sunriseTime: string;
    solarNoonTime: string;
    sunsetTime: string;
    sunsetDeltaTime: string;

    recentHighTempMean: number | null;
    recentHighTempStdev: number | null;
    recentLowTempMean: number | null;
    recentLowTempStdev: number | null;
}

function dtoPatcher(dto: WeatherBlockDto) {
}

const ctx = makeContext(() => {
    const block = useBlock<WeatherBlockDto>(`${Config.ServerUrl}/hub/WeatherBlock`, dtoPatcher);
    return block;
});

export const useWeatherBlock = ctx.useFunc;
export const WeatherBlockProvider = ctx.provider;
