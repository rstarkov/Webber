import iWind from "../assets/weather/Wind.png";
import iAstroSun from "../assets/weather/AstroSun.png";
import iAstroMoon from "../assets/weather/AstroMoon.png";
import iAstroCloudSun from "../assets/weather/AstroCloudSun.png";
import iAstroCloudMoon from "../assets/weather/AstroCloudMoon.png";
import iCloudLight from "../assets/weather/CloudLight.png";
import iCloudThick from "../assets/weather/CloudThick.png";
import iPrecipDrizzle from "../assets/weather/PrecipDrizzle.png";
import iPrecipHail from "../assets/weather/PrecipHail.png";
import iPrecipRainHeavy from "../assets/weather/PrecipRainHeavy.png";
import iPrecipRainLight from "../assets/weather/PrecipRainLight.png";
import iPrecipSleet from "../assets/weather/PrecipSleet.png";
import iPrecipSnowHeavy from "../assets/weather/PrecipSnowHeavy.png";
import iPrecipSnowLight from "../assets/weather/PrecipSnowLight.png";
import iPrecipThunder from "../assets/weather/PrecipThunder.png";
import iMist from "../assets/weather/Mist.png"; // bad
import iFog from "../assets/weather/Fog.png"; // bad
import iHaze from "../assets/weather/Haze.png";
import iSandstorm from "../assets/weather/Sandstorm.png"; // bad
import iTropicalStorm from "../assets/weather/TropicalStorm.png";

import { WeatherForecastKindDte } from "../blocks/WeatherForecastBlock";
import styled from "styled-components";

function makeIconMap(day: boolean): { [k in WeatherForecastKindDte]: string[] } {
    // every single one of these exists in a night variant in the source data, but we don't have distinct images for all of those cases
    return {
        sun: [day ? iAstroSun : iAstroMoon],
        sunIntervals: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight],
        cloudLight: [iCloudLight],
        cloudThick: [iCloudThick],
        drizzle: [iCloudLight, iPrecipDrizzle],

        rainLight: [iCloudThick, iPrecipRainLight],
        rainHeavy: [iCloudThick, iPrecipRainHeavy],
        snowRain: [iCloudThick, iPrecipSleet],
        snowLight: [iCloudThick, iPrecipSnowLight],
        snowHeavy: [iCloudThick, iPrecipSnowHeavy],
        hail: [iCloudThick, iPrecipHail],
        thunderstorm: [iCloudThick, iPrecipThunder],

        rainLightSun: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight, iPrecipRainLight],
        rainHeavySun: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight, iPrecipRainHeavy],
        snowRainSun: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight, iPrecipSleet],
        snowLightSun: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight, iPrecipSnowLight],
        snowHeavySun: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight, iPrecipSnowHeavy],
        hailSun: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight, iPrecipHail],
        thunderstormSun: [day ? iAstroCloudSun : iAstroCloudMoon, iCloudLight, iPrecipThunder],

        mist: [iMist],
        fog: [iFog],
        haze: [iHaze],
        sandstorm: [iSandstorm],
        tropicalStorm: [iTropicalStorm],
    };
}
const _dayIconMap = makeIconMap(true);
const _nightIconMap = makeIconMap(false);

interface WeatherTypeProps extends React.HTMLAttributes<HTMLDivElement> {
    kind: WeatherForecastKindDte;
    night: boolean;
    wind: number;
}

const ContainerDiv = styled.div`
    position: relative;
`;
const IconDiv = styled.div<{ $img: string }>`
    background-image: url(${p => p.$img});
    background-size: contain;
    background-repeat: no-repeat;
    background-position: center;
    position: absolute;
    left: 0; top: 0; right: 0; bottom: 0;
`;

export function WeatherTypeIcon({ kind, night, wind, ...rest }: WeatherTypeProps): JSX.Element {
    const layers = night ? _nightIconMap[kind] : _dayIconMap[kind];
    return <ContainerDiv {...rest}>
        {layers.map(src => <IconDiv key={src} $img={src} />)}
        {wind > 0 && <IconDiv key={iWind} $img={iWind} style={{ opacity: wind }} />}
    </ContainerDiv>;
}
