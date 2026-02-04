import React from "react";
import { render } from '@testing-library/react';
import DateRangePicker from "./DateRangePicker";
import moment from "moment";
import { Provider } from "react-redux";
import { setupStore } from "../../state/store";
import { MockRouter } from "../../helpers/MockRouter";

describe("DateRangePicker", () => {
    it("should render", () => {
        const store = setupStore({ subset: { subsetId: 'all', subsetConfigurations: [] } });
        const { container } = render(
            <Provider store={store}>
                <MockRouter>
                    <DateRangePicker
                        dateOfFirstDataPoint={new Date(2021, 5, 7)}
                        dateOfLastDataPoint={new Date(2022, 5, 7)}
                        startDate={moment.utc(new Date(2022, 1, 7))}
                        endDate={moment.utc(new Date(2022, 5, 7))}
                        onDateChanged={() => { }}
                    />
                </MockRouter>
            </Provider>
        );

        expect(container).toBeDefined();
    });
});
