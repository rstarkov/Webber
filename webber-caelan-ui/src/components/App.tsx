import * as React from 'react';
import "./App.css";
import HwInfoBlock from './HwInfoBlock';
import ClockBlock from './ClockBlock';
import PingBlock from './PingBlock';
import SynologyRouterBlock from './SynologyRouterBlock';
import WeatherBlock from './WeatherBlock';
import TimeUntilBlock from './TimeUntilBlock';
import WeatherForecastBlock from './WeatherForecastBlock';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSyncAlt } from '@fortawesome/free-solid-svg-icons'

function reload() {
    window.location.reload();
}

function App() {
    return (
        <div className="box">
            {/* <div className="l1t1 w4h1">
                <HwInfoBlock />
            </div>
            <div className="l1t2 w4h1">
                <PingBlock />
            </div>
            <div className="l1t3 w4h3">
                <SynologyRouterBlock />
            </div>
            <div className="l5t1 w4h3">
                <ClockBlock />
            </div>
            <div className="l5t4 w4h2">
                <WeatherBlock />
            </div>
            <div className="l1t6 w8h1">
                <WeatherForecastBlock />
            </div> */}
            <div className="l1t1 w8h8" style={{ overflow: 'hidden' }}>
                <TimeUntilBlock />
            </div>
            <div onClick={reload} style={{ position: "absolute", right: 0, top: 0, width: 60, height: 60, textAlign: "center", fontSize: 40, opacity: 0.6, lineHeight: "60px" }}>
                <FontAwesomeIcon icon={faSyncAlt} />
            </div>
        </div>
    );
}

export default App;