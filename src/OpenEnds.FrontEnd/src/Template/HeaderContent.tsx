import { Helmet } from "react-helmet";
import useGlobalDetailsStore from "@model/globalDetailsStore";

const HeaderContent = () => {

    const details = useGlobalDetailsStore((state) => state.details);

    return <Helmet>
        <link rel="icon" href={details.faviconUrl} />
        <link rel="stylesheet" href={details.stylesheetUrl} />
    </Helmet>
}

export default HeaderContent;