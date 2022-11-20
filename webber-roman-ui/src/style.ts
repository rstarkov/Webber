import { createGlobalStyle } from "styled-components"

export const GlobalStyle = createGlobalStyle`
    * {
        padding: 0;
        margin: 0;
        white-space: nowrap;
        box-sizing: border-box;
    }

    html,
    body {
        background: black;
        width: 100vw;
        height: 100vh;
        overflow: hidden;
    }

    html, body, button {
        color: white;
        font-size: 2.8vw;
        font-family: 'Open Sans';
    }

    button {
        background: #0f0f23;
        padding: 1.5vw 3vw;
        border: 0.2vw solid #333379;
    }

    button:hover {
        background: #433881;
        border-color: #725dee;
    }

    div#app, div#page {
        width: 100vw;
        height: 100vh;
    }

`;
