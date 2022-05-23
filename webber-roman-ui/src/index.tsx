import React from 'react'
import ReactDOM from 'react-dom/client'
import { GlobalStyle } from './style'
import { App } from './App'
import { PingBlockProvider } from './blocks/PingBlock';

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <GlobalStyle />
        <PingBlockProvider>
            <App />
        </PingBlockProvider>
    </React.StrictMode>
);
