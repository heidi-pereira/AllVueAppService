import { Point } from "./Point";
import { Box } from "./Box";

export class HTMLHelper {

    
    public static AddTopLeftViewBoxAndPreserveAspectRatio(div: any, width: number, height: number) {
        HTMLHelper.AddViewBox(div, 0, 0, width, height);
        HTMLHelper.AddPreserveAspectRatio(div, "Min", "Min");
    }

    public static AddViewBox(div, x: number, y: number, width: number, height: number) {
        div.attr("viewBox", x + " " + y + " " + width + " " + height);
    }

    public static AddPreserveAspectRatio(div: any, xMinMidMax = "Mid", yMinMidMax = "Mid", meetOrSlice = "meet") {
        div.attr("preserveAspectRatio", "x" + xMinMidMax + "Y" + yMinMidMax + " " + meetOrSlice);
    }

    public static AddTextToPointXY(div: any, x: number, y: number, text: string, styleClass: string) {
        if (text) {
            return div.append("text")
                .attr("dominant-baseline", "middle")
                .attr("anchor", "middle")
                .attr("class", styleClass)
                .attr("x", x)
                .attr("y", y)
                .text(text);
        }
    }

    static AddOrUpdateLine(div: any, existing: any, point1: Point, point2: Point, styleClass: string)  {
        let result = existing ? existing : div.append("line");
        return result
            .attr("class", styleClass)
            .attr("x1", point1.X)
            .attr("y1", point1.Y)
            .attr("x2", point2.X)
            .attr("y2", point2.Y);
    }

    static AddOrUpdateRect(div: any, existing: any, box: Box, styleClass: string, text: string) {
        let result = existing ? existing : div.append("div");
        result
            .style("left", Math.round(box.X)+"px")
            .style("top", Math.round(box.Y) + "px")
            .style("width", Math.round(box.Width) + "px")
            .style("height", Math.round(box.Height) + "px");

        if (styleClass) {
            result.attr("class", styleClass);
        }

        if (text || text == "") {
            result.text(text);
        }
        return result;

    }

    static AddOrUpdateIcon(div: any, existing: any, imageUrl: string, box: Box, styleClass: string) {
        let result = existing ? existing : div.append("g");
        result.html("");
        result.attr("class", styleClass)
            .attr("transform", HTMLHelper.Translate(box.X, box.Y));
        result.append("image")
            .attr("xlink:href", imageUrl)
            .attr("width", box.Width)
            .attr("height", box.Height);
        return result;
    }

    static AddOrUpdateG(div: any, existing: null, box: Box, styleClass: string) {
        let result = existing ? existing : div.append("g");
        result.attr("class", styleClass)
            .attr("transform", HTMLHelper.Translate(box.X, box.Y));
        return result;
    }

    public static AddOrUpdateGStyleOnly(div: any, existing: null, styleClass: string) {
        let result = existing ? existing : div.append("g");
        if (styleClass) {
            result.attr("class", styleClass);
        }
        return result;
    }

    private static Translate(x: number, y: number): string {
        return "translate(" + x + "," + y + ")";
    }

}