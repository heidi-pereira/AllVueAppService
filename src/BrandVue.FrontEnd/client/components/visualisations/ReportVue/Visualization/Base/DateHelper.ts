
export class DateHelper {
    public static GetHourFraction(date: Date) : number {
        if (date) {
            return date.getHours() + date.getMinutes() / 60;
        } else {
            return 0;
        }
    }

    public static DayOfWeek(date: Date): number {
        return (date.getDay() + 6) % 7;
    }

    public static LastDate(date1: Date, date2: Date): Date
    {
        return date1.getTime() < date2.getTime() ? date2 : date1;
    }

    public static FirstDate(date1: Date, date2: Date): Date {
        return date1.getTime() < date2.getTime() ? date1 : date2;
    }

    public static EndOfMonth(date: Date): Date {
        return new Date(date.getFullYear(), date.getMonth() + 1, 1);
    }

    public static DayName(dayOfWeek: number): string {
        switch (dayOfWeek) {
            case 0:
                return "M"
            case 1:
                return "T"
            case 2:
                return "W"
            case 3:
                return "T"
            case 4:
                return "F"
            case 5:
                return "S"
            default:
                return "S"
        }
    }

    public static LongDayName(dayOfWeek: number): string {
        switch (dayOfWeek) {
            case 0:
                return "Monday"
            case 1:
                return "Tuesday"
            case 2:
                return "Wednesday"
            case 3:
                return "Thurdsay"
            case 4:
                return "Friday"
            case 5:
                return "Saturday"
            default:
                return "Sunday"
        }
    }

    public static DayId(date: Date) {
        return `${date.getFullYear}-${date.getMonth}-${date.getDay}`;
    }
}