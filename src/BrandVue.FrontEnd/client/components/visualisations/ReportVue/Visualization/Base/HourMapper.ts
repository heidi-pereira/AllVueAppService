

export class HourMapper {

    public HourMap: number[];
    private PositionMap: any;
    private Total: number;
    constructor() {
        this.HourMap = [];
        for (var i = 0; i < 24; i++) {
            this.HourMap.push(1);
        }
    }

    public GetHourPercentage(date: Date): number {
        let hour = date.getHours();
        var hourPosition = this.PositionMap[hour];
        var nextPosition = hour < 23 ? this.PositionMap[hour + 1] : this.Total;
        return (hourPosition + (nextPosition - hourPosition) * date.getMinutes() / 60) / this.Total;
    }
    
    public UpdateMapping() {
        this.PositionMap = [];
        let cumulativePosition = 0;
        for (var i = 0; i < 24; i++) {
            this.PositionMap[i] = cumulativePosition;
            cumulativePosition += this.HourMap[i];
        }
        this.Total = cumulativePosition;
    }

    
}