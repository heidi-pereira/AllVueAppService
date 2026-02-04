import { useLocation } from 'react-router-dom';
import { useReadVueQueryParams } from '../helpers/UrlHelper';
import { getReportsPage, getUrlForPageName } from '../helpers/PagesHelper';

export const useReportsPageUrl = (): string | undefined => {
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();

    const rootReportsPage = getReportsPage();
    if (!rootReportsPage) return undefined;

    const reportsPageUrl = getUrlForPageName(rootReportsPage.name, location, readVueQueryParams);
    return reportsPageUrl;
}