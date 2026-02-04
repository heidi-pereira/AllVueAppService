import { MetricSet } from "../../../../metrics/metricSet";
import { PartType } from "../../../../components/panes/PartType";
import { Metric } from "../../../../metrics/metric";
import { CalculationType, MainQuestionType, ReportType } from "../../../../BrandVueApi";
import { getPartTypeForMetric } from "./ReportPageBuilder";
import { createEntities } from "../../../../helpers/ReactTestingLibraryHelpers";

const singleChoiceMetricName = "singleChoice";
const multiChoiceMetricName = "multiChoice";
const textMetricName = "textMetric";

describe("Check correct part types are set for reports", () => {

    const questionTypeLookup: {[metricName: string]: MainQuestionType} = {
        singleChoice: MainQuestionType.SingleChoice,
        multiChoice: MainQuestionType.MultipleChoice,
        textMetric: MainQuestionType.Text
    };

    it("Table report should return ReportsTable", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        const hasWaves = false;

        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Table, hasWaves);
        expect(part).toBe(PartType.ReportsTable);
    });

    it("Text question should return ReportsCardText", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        metric.name = textMetricName;
        metric.calcType = CalculationType.Text;
        const hasWaves = false;

        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Chart, hasWaves);
        expect(part).toBe(PartType.ReportsCardText);
    });

    it("When report has waves, should return ReportsLineCard", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        const hasWaves = true;

        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Chart, hasWaves);
        expect(part).toBe(PartType.ReportsCardLine);
    });

    it("Zero entity metric should return ReportsCardChart", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        metric.entityCombination = []
        const hasWaves = false;

        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Chart, hasWaves);
        expect(part).toBe(PartType.ReportsCardChart);
    });

    it("Single entity single choice metric should return ReportsCardChart", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        metric.name = singleChoiceMetricName;
        metric.entityCombination = createEntities(1);
        const hasWaves = false;

        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Chart, hasWaves);
        expect(part).toBe(PartType.ReportsCardChart);
    });

    it("Single entity multi choice metric should return ReportsCardChart", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        metric.name = multiChoiceMetricName;
        metric.entityCombination = createEntities(1);
        const hasWaves = false;

        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Chart, hasWaves);
        expect(part).toBe(PartType.ReportsCardChart);
    });

    it("Two entity single choice metric should return ReportsCardStackedMulti", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        metric.numFormat="0%";
        metric.name = singleChoiceMetricName;
        metric.entityCombination = createEntities(2);
        const hasWaves = false;

        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Chart, hasWaves);
        expect(part).toBe(PartType.ReportsCardStackedMulti);
    });

    it("Two entity multi choice metric should return ReportsCardMultiEntityMultipleChoice", async () => {
        const metricSet = new MetricSet();
        const metric = new Metric({metricSet});
        metric.numFormat="0%";
        metric.name = multiChoiceMetricName;
        metric.entityCombination = createEntities(2);
        const hasWaves = false;
        const part = getPartTypeForMetric(metric, questionTypeLookup, ReportType.Chart, hasWaves);
        expect(part).toBe(PartType.ReportsCardMultiEntityMultipleChoice);
    });
});