import React, { useEffect } from "react";
import { Location, useLocation, useNavigate } from "react-router-dom";
import { useWriteVueQueryParams } from "../components/helpers/UrlHelper";

export const TestComponentHook: React.FC<{
    onHookReady: (setQueryParameter: any, location: Location<any>) => void
}> = ({onHookReady}) => {
    const navigate = useNavigate();
    const location = useLocation();
    const {setQueryParameter} = useWriteVueQueryParams(navigate, location);

    // Use useEffect to pass the hook's result to the test
    useEffect(() => {
        onHookReady(setQueryParameter, location);
    }, [onHookReady, setQueryParameter, location]);

    return null;
};