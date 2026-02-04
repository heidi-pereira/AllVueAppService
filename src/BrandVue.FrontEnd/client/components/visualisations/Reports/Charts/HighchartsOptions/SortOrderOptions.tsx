import { ReportOrder } from '../../../../../BrandVueApi';
import { SeriesColumnOptions, PointOptionsObject, SeriesBarOptions } from 'highcharts';
import { CustomPointOptionsObject } from "./PointOptions";

const sortByResultOrder = (results: SeriesColumnOptions[] | SeriesBarOptions[]) => {
    results.sort((a,b) => { 
        const pointsA = a.data as PointOptionsObject[];
        const pointsB = b.data as PointOptionsObject[];

        const sumA = pointsA.reduce((counts, point) => {
            const pointObj = point as CustomPointOptionsObject;
            counts+= parseFloat(pointObj.formattedText);
            return counts;   
        }, 0);
        const sumB = pointsB.reduce((counts, point) => {
            const pointObj = point as CustomPointOptionsObject;
            counts+= parseFloat(pointObj.formattedText);
            return counts;   
        }, 0)

        const avgA =  sumA / pointsA.length;
        const avgB =  sumB / pointsB.length;
        
        return avgA - avgB
    })  
}

interface ISortData {
    results: SeriesColumnOptions[] | SeriesBarOptions[],
    order: ReportOrder
}

export const sortData = ({results, order}: ISortData) => {
    switch(order){
        case ReportOrder.ResultOrderDesc:
        case ReportOrder.ResultOrderAsc:
            sortByResultOrder(results);
            break;
    }

    switch (order) {
        case ReportOrder.ResultOrderAsc:
        case ReportOrder.ScriptOrderAsc:
            results.reverse();
            break;
    }
}