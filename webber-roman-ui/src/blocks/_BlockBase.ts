import { HubConnectionBuilder } from "@microsoft/signalr";
import { useEffect, useState } from "react";
import { DateTime } from "luxon";

export type BlockConnectionStatus = 'disconnected' | 'connecting' | 'connected';

export interface BaseDto {
    localOffsetHours: number;
    sentUtc: DateTime;
    validUntilUtc: DateTime;
    errorMessage: string;
}

export interface BlockState {
    status: BlockConnectionStatus;
    updates: number;
}

export interface BlockStateDto<TDto> extends BlockState {
    dto: TDto | null;
}

function basePatcher(dto: BaseDto) {
    dto.sentUtc = DateTime.fromISO(dto.sentUtc as any);
    dto.validUntilUtc = DateTime.fromISO(dto.validUntilUtc as any);
}

export function useBlock<TDto extends BaseDto>(url: string, patcher: (dto: TDto) => void): BlockStateDto<TDto> {
    const [dto, setDto] = useState<TDto | null>(null);
    const [status, setStatus] = useState<BlockConnectionStatus>('disconnected');
    const [updates, setUpdates] = useState(0);
    console.log([url, status]);

    useEffect(() => {
        let exited = false;
        const instance = Math.random();
        function dbg(str: string) { console.log(`[wbbr ${instance}] ${str}`); }
        dbg('START ' + url);
        const conn = new HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (rc) => rc.previousRetryCount <= 2 ? 2_000 : 10_000 })
            .build();
        conn.on('Update', (dto: TDto) => {
            basePatcher(dto);
            patcher(dto);
            setDto(dto);
            setUpdates(u => u + 1);
            if (dto.errorMessage)
                console.error(dto.errorMessage);
        });
        conn.onreconnecting(() => { setStatus('connecting'); dbg('on reconnecting'); });
        conn.onreconnected(() => { setStatus('connected'); dbg('on reconnected'); });
        conn.onclose(() => { setStatus('disconnected'); dbg('on close'); });
        async function connect() {
            setStatus('connecting'); dbg('connecting');
            try {
                if (exited) return;
                await conn.start();
                setStatus('connected'); dbg('connected');
                if (exited) conn.stop();
            } catch {
                dbg('catch');
                conn.stop();
                setStatus('disconnected');
                await new Promise(r => setTimeout(r, 10_000));
                if (!exited)
                    connect();
            }
        }
        connect();
        return () => { exited = true; conn.stop(); dbg('STOP'); }
    }, []);

    return { dto, status, updates };
};
