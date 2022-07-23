import { useEffect, useState } from "react";
import { Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { useReloadBlock } from "./blocks/ReloadBlock";
import { ClassicPage } from "./classic/_page";
import { DashboardPage } from "./dashboard/_page";

export function App(): JSX.Element {
    const reload = useReloadBlock();
    const [prevHash, setPrevHash] = useState(reload.dto?.serverHash);
    useEffect(() => {
        if (prevHash && prevHash != reload.dto?.serverHash) {
            location.reload();
        }
        setPrevHash(reload.dto?.serverHash); // save the initial hash on first update
    }, [reload.dto?.serverHash]);

    return (
        <Routes>
            <Route path='/' element={<DashboardPage />} />
            <Route path='/classic' element={<ClassicPage />} />
        </Routes>
    );
}
