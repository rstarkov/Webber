import * as React from 'react';
import "./App.css";
import HwInfoBlock from './HwInfoBlock';
import ClockBlock from './ClockBlock';
import WeatherBlock from './WeatherBlock';
import TimeUntilBlock from './TimeUntilBlock';

function App() {
    return (
        <div className="box">
            <div className="l1t1 w4h8">
                <HwInfoBlock />
            </div>
            <div className="l5t1 w4h4">
                <ClockBlock />
            </div>
            <div className="l5t5 w4h2" style={{ overflow: 'hidden' }}>
                <TimeUntilBlock />
            </div>
            <div className="l5t7 w4h2">
                <WeatherBlock />
            </div>
        </div>
    );
}

export default App;