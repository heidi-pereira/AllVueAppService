import { PageHandler } from './PageHandler';
import { dsession } from '../dsession';
import { MetricSet } from '../metrics/metricSet';
import {IReadVueQueryParams} from "./helpers/UrlHelper";
import {mock} from "jest-mock-extended";

describe('PageHandler', () => {
    let session: dsession;
    let pageHandler: PageHandler;

    beforeEach(() => {
        session = new dsession();
        pageHandler = new PageHandler(session);
    });

    test('getPageQuery should return updated query string', () => {
        const enabledMetricSet = new MetricSet();
        const result = pageHandler.getPageQuery('mockUrl', '?param=value', enabledMetricSet, mock<IReadVueQueryParams>());

        expect(result).toContain('param=value');
    });
});