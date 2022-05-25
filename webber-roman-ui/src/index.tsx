import React from 'react'
import ReactDOM from 'react-dom/client'
import { GlobalStyle } from './style'
import { App } from './App'
import { PingBlockProvider } from './blocks/PingBlock';
import { WeatherBlockProvider } from './blocks/WeatherBlock';
import { TimeUntilBlockProvider } from './blocks/TimeUntilBlock';
import { RemilkBlockProvider } from './blocks/RemilkBlock';

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <GlobalStyle />
        <PingBlockProvider><WeatherBlockProvider><TimeUntilBlockProvider><RemilkBlockProvider>
            <App />
        </RemilkBlockProvider></TimeUntilBlockProvider></WeatherBlockProvider></PingBlockProvider>
    </React.StrictMode>
);
