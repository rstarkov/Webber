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

function basePatcher(dto: BaseDto) {
    dto.sentUtc = DateTime.fromJSDate(dto.sentUtc as any);
    dto.validUntilUtc = DateTime.fromJSDate(dto.validUntilUtc as any);
}

export function useBlock<TDto extends BaseDto>(url: string, patcher: (dto: TDto) => void): [TDto | null, BlockConnectionStatus] {
    const [dto, setDto] = useState<TDto | null>(null);
    const [status, setStatus] = useState<BlockConnectionStatus>('disconnected');

    useEffect(() => {
        let exited = false;
        const instance = Math.random();
        function dbg(str: string) { console.log(`[wbbr ${instance}] ${str}`); }
        dbg('START ' + url);
        const conn = new HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (rc) => rc.previousRetryCount <= 2 ? 2_000 : 10_000 })
            .build();
        conn.on('Update', (dto: TDto) => { basePatcher(dto); patcher(dto); setDto(dto); if (dto.errorMessage) console.error(dto.errorMessage); });
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

    return [dto, status];
};
