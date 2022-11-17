import React from 'react'
import ReactDOM from 'react-dom/client'
import { GlobalStyle } from './style'
import { App } from './App'
import { BrowserRouter } from "react-router-dom";
import { PingBlockProvider } from './blocks/PingBlock';
import { WeatherBlockProvider } from './blocks/WeatherBlock';
import { TimeUntilBlockProvider } from './blocks/TimeUntilBlock';
import { RemilkBlockProvider } from './blocks/RemilkBlock';
import { DebugBlockProvider } from './blocks/DebugBlock';
import { ReloadBlockProvider } from './blocks/ReloadBlock';
import { RouterBlockProvider } from './blocks/RouterBlock';
import { RainCloudBlockProvider } from './blocks/RainCloudBlock';

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <GlobalStyle />
        <DebugBlockProvider><ReloadBlockProvider><PingBlockProvider><RouterBlockProvider><WeatherBlockProvider><TimeUntilBlockProvider><RemilkBlockProvider><RainCloudBlockProvider>
            <BrowserRouter>
                <App />
            </BrowserRouter>
        </RainCloudBlockProvider></RemilkBlockProvider></TimeUntilBlockProvider></WeatherBlockProvider></RouterBlockProvider></PingBlockProvider></ReloadBlockProvider></DebugBlockProvider>
    </React.StrictMode>
);
