import React from "react"
import ReactDOM from "react-dom/client"
import { GlobalStyle } from "./style"
import { App } from "./App"
import { BrowserRouter } from "react-router-dom";
import { PingBlockProvider } from "./blocks/PingBlock";
import { WeatherBlockProvider } from "./blocks/WeatherBlock";
import { TimeUntilBlockProvider } from "./blocks/TimeUntilBlock";
import { RemilkBlockProvider } from "./blocks/RemilkBlock";
import { DebugBlockProvider } from "./blocks/DebugBlock";
import { ReloadBlockProvider } from "./blocks/ReloadBlock";
import { RouterBlockProvider } from "./blocks/RouterBlock";
import { RainCloudBlockProvider } from "./blocks/RainCloudBlock";
import { TimeProvider } from "./util/useTime";
import { WeatherForecastBlockProvider } from "./blocks/WeatherForecastBlock";
import { WeatherDotComBlockProvider } from "./blocks/WeatherDotComBlock";

ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <GlobalStyle />
        <DebugBlockProvider><TimeProvider><ReloadBlockProvider><PingBlockProvider><RouterBlockProvider><WeatherBlockProvider><WeatherForecastBlockProvider><TimeUntilBlockProvider><RemilkBlockProvider><RainCloudBlockProvider><WeatherDotComBlockProvider>
            <BrowserRouter>
                <App />
            </BrowserRouter>
        </WeatherDotComBlockProvider></RainCloudBlockProvider></RemilkBlockProvider></TimeUntilBlockProvider></WeatherForecastBlockProvider></WeatherBlockProvider></RouterBlockProvider></PingBlockProvider></ReloadBlockProvider></TimeProvider></DebugBlockProvider>
    </React.StrictMode>
);
