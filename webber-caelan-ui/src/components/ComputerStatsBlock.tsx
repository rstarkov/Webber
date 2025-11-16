import * as React from 'react';
import { useRef, useEffect, useState } from 'react';
import styled from 'styled-components';
import { withSubscription, BaseDto } from './util';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faMicrochip, faBolt, faImage, faMemory } from '@fortawesome/free-solid-svg-icons';

interface CpuCoreInfo {
    load: number;
    temp: number;
    core: number;
}

interface GpuLayout {
    load: number;
    memory: number;
}

interface GpuInfo {
    layout: GpuLayout[];
}

interface ComputerStats {
    name: string;
    cpuCores: CpuCoreInfo[];
    gpu: GpuInfo;
    avgCpuUtilization: number;
    maxCoreUtilization: number;
    maxGpuUtilization: number;
    powerConsumptionWatts?: number;
    ramUtilization: number;
}

interface ComputerStatsBlockDto extends BaseDto {
    computers: ComputerStats[];
    sentUtc: string;
}

const Container = styled.div`
    width: 424px;
    height: 80px;
    display: flex;
    flex-direction: row;
    gap: 8px;
    overflow: hidden;
`;

const ComputerSection = styled.div`
    display: flex;
    flex-direction: column;
    height: 100%;
`;

const NameLabel = styled.div`
    width: 100%;
    height: 20px;
    display: flex;
    align-items: left;
    justify-content: left;
    font-size: 11px;
    font-weight: bold;
    margin-left: 4px;
    color: white;
`;

const InnerContainer = styled.div`
    display: flex;
    flex-direction: row;
    flex: 1;
    gap: 2px;
    margin-top: 2px;
`;

const BarsSection = styled.div`
    display: flex;
    flex-direction: column;
    width: 60px;
    height: 100%;
    gap: 2px;
`;

const StatBar = styled.div`
    width: 100%;
    height: 29px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: bold;
    color: white;
    position: relative;
    background-color: rgba(138, 180, 248, 0.15);
    transition: background-color 0.4s ease;
    border-radius: 2px;
`;

const CoreGrid = styled.div`
    width: 60px;
    height: 100%;
    position: relative;
`;

const CoreCell = styled.div`
    position: absolute;
    background-color: rgba(138, 180, 248, 0.15);
    transition: background-color 0.4s ease;
    border-radius: 2px;
`;

// Calculate color based on percentage: blue (0-85%), red (85-100%)
// Blue: #8AB4F8 with varying transparency, Red: #FF0000 with varying transparency
const getColorForPercentage = (percent: number): string => {
    if (percent < 0) percent = 0;
    if (percent > 100) percent = 100;

    if (percent <= 85) {
        // 0-85%: #8AB4F8 with transparency from 15% to 100%
        const intensity = percent / 85;
        const alpha = 0.15 + (intensity * 0.85); // 0.15 to 1.0
        return `rgba(138, 180, 248, ${alpha})`;
    } else {
        // 85-100%: #FF0000 with transparency from 30% to 100%
        const range = percent - 85;
        const intensity = range / 15;
        const alpha = 0.4 + (intensity * 0.6); // 0.4 to 1.0
        return `rgba(255, 0, 0, ${alpha})`;
    }
};

// Utilization history graph component
interface UtilizationGraphBarProps {
    utilization: number;
    timestamp: string; // Add timestamp to detect new data updates
    icon: any; // FontAwesome icon
    fillHeight?: boolean; // If true, fills 100% height instead of 29px
}

const UtilizationGraphBar: React.FC<UtilizationGraphBarProps> = ({ utilization, timestamp, icon, fillHeight = false }) => {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const historyRef = useRef<number[]>([]);
    const maxHistoryLength = 80; // One point per pixel width
    const barHeight = fillHeight ? 60 : 29;

    useEffect(() => {
        // Add current utilization to history
        historyRef.current.push(utilization);
        if (historyRef.current.length > maxHistoryLength) {
            historyRef.current.shift();
        }

        // Draw graph
        const canvas = canvasRef.current;
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        const width = canvas.width;
        const height = canvas.height;

        // Clear canvas
        ctx.clearRect(0, 0, width, height);

        // Draw utilization line graph
        const history = historyRef.current;
        if (history.length < 2) return;

        // Scale from 0-100%
        ctx.strokeStyle = 'rgba(255, 255, 255, 0.6)';
        ctx.lineWidth = 1.5;
        ctx.beginPath();

        history.forEach((value, index) => {
            const x = (index / (maxHistoryLength - 1)) * width;
            const y = height - (value / 100) * (height - 4) - 2; // 2px padding, scale 0-100%

            if (index === 0) {
                ctx.moveTo(x, y);
            } else {
                ctx.lineTo(x, y);
            }
        });

        ctx.stroke();
    }, [utilization, timestamp]); // Depend on timestamp to update even when value doesn't change

    return (
        <StatBar style={{
            height: fillHeight ? '100%' : '29px',
            backgroundColor: getColorForPercentage(utilization),
            position: 'relative',
            overflow: 'hidden',
            flexDirection: fillHeight ? 'column' : 'row',
            justifyContent: fillHeight ? 'center' : 'center',
            alignItems: fillHeight ? 'center' : 'center'
        }}>
            <canvas
                ref={canvasRef}
                width={80}
                height={barHeight}
                style={{
                    position: 'absolute',
                    left: 0,
                    top: 0,
                    width: '100%',
                    height: '100%',
                    pointerEvents: 'none'
                }}
            />
            {fillHeight ? (
                <>
                    <FontAwesomeIcon
                        icon={icon}
                        style={{
                            position: 'relative',
                            fontSize: 16,
                            zIndex: 1,
                            opacity: 0.6,
                            marginBottom: 4
                        }}
                    />
                    <span style={{ position: 'relative', zIndex: 1 }}>
                        {Math.round(utilization)}%
                    </span>
                </>
            ) : (
                <>
                    <FontAwesomeIcon
                        icon={icon}
                        style={{
                            position: 'absolute',
                            left: 4,
                            top: '50%',
                            transform: 'translateY(-50%)',
                            fontSize: 12,
                            zIndex: 1,
                            opacity: 0.6
                        }}
                    />
                    <span style={{ position: 'relative', zIndex: 1, width: '100%', textAlign: 'right', paddingRight: 4 }}>
                        {Math.round(utilization)}%
                    </span>
                </>
            )}
        </StatBar>
    );
};

// Power consumption graph component
interface PowerGraphBarProps {
    watts: number;
    timestamp: string;
}

const PowerCard = styled.div`
    width: 60px;
    height: 29px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: bold;
    color: white;
    position: relative;
    background-color: rgba(138, 180, 248, 0.15);
    transition: background-color 0.4s ease;
    border-radius: 2px;
`;

const PowerGraphBar: React.FC<PowerGraphBarProps> = ({ watts, timestamp }) => {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const historyRef = useRef<number[]>([]);
    const maxHistoryLength = 60; // One point per pixel width

    useEffect(() => {
        // Add current watts to history
        historyRef.current.push(watts);
        if (historyRef.current.length > maxHistoryLength) {
            historyRef.current.shift();
        }

        // Draw graph
        const canvas = canvasRef.current;
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        const width = canvas.width;
        const height = canvas.height;

        // Clear canvas
        ctx.clearRect(0, 0, width, height);

        // Draw watts line graph
        const history = historyRef.current;
        if (history.length < 2) return;

        // Find max value for scaling
        const maxWatts = Math.max(...history, 1); // At least 1 to avoid division by zero

        ctx.strokeStyle = 'rgba(255, 255, 255, 0.6)';
        ctx.lineWidth = 1.5;
        ctx.beginPath();

        history.forEach((value, index) => {
            const x = (index / (maxHistoryLength - 1)) * width;
            const y = height - (value / maxWatts) * (height - 4) - 2; // 2px padding, scale to max

            if (index === 0) {
                ctx.moveTo(x, y);
            } else {
                ctx.lineTo(x, y);
            }
        });

        ctx.stroke();
    }, [watts, timestamp]);

    // Calculate color based on watts (assuming typical range 0-500W)
    // Use similar color scheme as CPU/GPU but scale differently
    const percentage = Math.min((watts / 500) * 100, 100);

    return (
        <PowerCard style={{
            backgroundColor: getColorForPercentage(percentage),
            overflow: 'hidden'
        }}>
            <canvas
                ref={canvasRef}
                width={60}
                height={29}
                style={{
                    position: 'absolute',
                    left: 0,
                    top: 0,
                    width: '100%',
                    height: '100%',
                    pointerEvents: 'none'
                }}
            />
            <FontAwesomeIcon
                icon={faBolt}
                style={{
                    position: 'absolute',
                    left: 4,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    fontSize: 12,
                    zIndex: 1,
                    opacity: 0.6
                }}
            />
            <span style={{ position: 'relative', zIndex: 1, width: '100%', textAlign: 'right', paddingRight: 4 }}>
                {Math.round(watts)}W
            </span>
        </PowerCard>
    );
};

// Calculate grid layout for cores
const calculateGridLayout = (numCores: number): { cols: number, rows: number, doubleWidthSpan: number } => {
    if (numCores === 0) return { cols: 0, rows: 0, doubleWidthSpan: 1 };

    // Try to find a near-square layout
    const sqrt = Math.sqrt(numCores);
    let cols = Math.ceil(sqrt);
    let rows = Math.ceil(numCores / cols);

    // Check if we need a double-width core
    const totalCells = cols * rows;
    const extraSpots = totalCells - numCores;
    // Highest core spans (extraSpots + 1) cells horizontally
    const doubleWidthSpan = extraSpots > 0 ? extraSpots + 1 : 1;

    return { cols, rows, doubleWidthSpan };
};

// Calculate position and size for each core
interface CoreLayout {
    left: number;
    top: number;
    width: number;
    height: number;
}

const getCoreLayout = (
    coreIndex: number,
    cols: number,
    rows: number,
    doubleWidthSpan: number
): CoreLayout => {
    const gap = 2;
    const gridSize = 60;
    const cellWidth = (gridSize - (cols - 1) * gap) / cols;
    const cellHeight = (gridSize - (rows - 1) * gap) / rows;

    // First core (highest loaded) gets double width if needed
    if (doubleWidthSpan > 1 && coreIndex === 0) {
        return {
            left: 0,
            top: 0,
            width: cellWidth * doubleWidthSpan + gap * (doubleWidthSpan - 1),
            height: cellHeight
        };
    }

    // Calculate position for other cores
    let position = coreIndex;

    // If we have a double-width core, skip the cells it occupies
    if (doubleWidthSpan > 1 && coreIndex > 0) {
        // The first core takes up 'doubleWidthSpan' cells in the first row
        position = coreIndex + (doubleWidthSpan - 1);
    }

    const col = position % cols;
    const row = Math.floor(position / cols);

    return {
        left: col * (cellWidth + gap),
        top: row * (cellHeight + gap),
        width: cellWidth,
        height: cellHeight
    };
};

const ComputerStatsBlock: React.FunctionComponent<{ data: ComputerStatsBlockDto }> = ({ data }) => {
    if (!data || !data.computers || data.computers.length === 0) {
        return <Container />;
    }

    // data.computers[0].cpuCores.push({ load: 100, temp: 0, core: 12 }); // For debug testing many cores
    // data.computers[0].cpuCores.push({ load: 50, temp: 0, core: 13 }); // For debug testing many cores

    return (
        <Container>
            {data.computers.map((computer, idx) => {
                const { cols, rows, doubleWidthSpan } = calculateGridLayout(computer.cpuCores.length);

                // Sort cores by load (highest to lowest), with core number as tiebreaker for stability
                const sortedCores = [...computer.cpuCores].sort((a, b) => {
                    if (b.load !== a.load) return b.load - a.load;
                    return a.core - b.core; // Stable sort: same load -> sort by core number
                });

                return (
                    <ComputerSection key={idx}>
                        {/* Computer Name Label */}
                        <NameLabel>{computer.name}</NameLabel>

                        {/* Inner container with all bars and grids */}
                        <InnerContainer>
                            {/* CPU/RAM Bars Section */}
                            <BarsSection>
                                {/* Avg CPU Bar with Utilization Graph */}
                                <UtilizationGraphBar
                                    utilization={computer.avgCpuUtilization}
                                    timestamp={data.sentUtc}
                                    icon={faMicrochip}
                                />

                                {/* RAM Utilization Bar */}
                                <UtilizationGraphBar
                                    utilization={computer.ramUtilization}
                                    timestamp={data.sentUtc}
                                    icon={faMemory}
                                />
                            </BarsSection>

                            {/* CPU Core Grid */}
                            <CoreGrid>
                                {Array.from({ length: computer.cpuCores.length }).map((_, positionIndex) => {
                                    const layout = getCoreLayout(positionIndex, cols, rows, doubleWidthSpan);
                                    const coreData = sortedCores[positionIndex];
                                    return (
                                        <CoreCell
                                            key={positionIndex}
                                            style={{
                                                backgroundColor: getColorForPercentage(coreData.load),
                                                left: `${layout.left}px`,
                                                top: `${layout.top}px`,
                                                width: `${layout.width}px`,
                                                height: `${layout.height}px`
                                            }}
                                        />
                                    );
                                })}
                            </CoreGrid>

                            {/* GPU and Power Section */}
                            <BarsSection>
                                {/* Max GPU Bar with Utilization Graph */}
                                <UtilizationGraphBar
                                    utilization={computer.maxGpuUtilization}
                                    timestamp={data.sentUtc}
                                    icon={faImage}
                                    fillHeight={computer.powerConsumptionWatts == null}
                                />

                                {/* Power Consumption Bar */}
                                {computer.powerConsumptionWatts != null && (
                                    <PowerGraphBar
                                        watts={computer.powerConsumptionWatts}
                                        timestamp={data.sentUtc}
                                    />
                                )}
                            </BarsSection>
                        </InnerContainer>
                    </ComputerSection>
                );
            })}
        </Container>
    );
}

export default withSubscription(ComputerStatsBlock, "ComputerStatsBlock");
