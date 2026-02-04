import { Point } from "./Point";

export class Box {
        
    public X: number;
    public Y: number;
    public Width: number;
    public Height: number;

    constructor(box?: any) {
        if (box) {
            this.X = box.x;
            this.Y = box.y;
            this.Width = box.width;
            this.Height = box.height;
        }
    }

    public static Get(object: any): Box {
        let box = new Box();
        box.X = object.X;
        box.Y = object.Y;
        box.Width = object.Width;
        box.Height = object.Height;
        return box;
    }

    public static GetFromDimensions(x: number, y: number, width: number, height: number): Box {
        let box = new Box();
        box.X = x??0;
        box.Y = y??0;
        box.Width = width??1000;
        box.Height = height??100;
        return box;
    }

    public Clone():Box {
        let box = new Box();
        box.X = this.X;
        box.Y = this.Y;
        box.Width = this.Width;
        box.Height = this.Height;
        return box;
    }

    public GetZeroXY():Box {
        let box = new Box();
        box.X = 0;
        box.Y = 0;
        box.Width = this.Width;
        box.Height = this.Height;
        return box;
    }

    public GetInnerBox(innerAspectRatio: number): Box {
        let containerAspectRatio = this.Width / this.Height;
        let heightConstrained = containerAspectRatio > innerAspectRatio;
        let innerBox = this.Clone();
        innerBox.X = 0;
        innerBox.Y = 0;
        if (heightConstrained) {
            innerBox.Width = this.Height * innerAspectRatio;
            innerBox.X = (this.Width - innerBox.Width) / 2;
        } else {
            innerBox.Height = this.Width / innerAspectRatio;
            innerBox.Y = (this.Height - innerBox.Height) / 2;
        }
        return innerBox;
    }

    public get Right(): number {
        return this.X + this.Width;
    }

    public get Bottom(): number {
        return this.Y + this.Height;
    }

    public get Center(): Point {
        return new Point(this.X + this.Width / 2, this.Y + this.Height / 2);
    }

    public get BottomLeft(): Point {
        return new Point(this.X, this.Bottom);
    }

    public get BottomRight(): Point {
        return new Point(this.Right, this.Bottom);
    }
    public get TopLeft(): Point {
        return new Point(this.X, this.Y);
    }
    public SetDimensions(x: number, y: number, width: number, height: number) {
        this.X = x;
        this.Y = y;
        this.Width = width;
        this.Height = height;
    }

    public ConnectionPoint(direction: string): Point {
        switch (direction) {
            case "n":
                return new Point(this.X + this.Width / 2, this.Y);
            case "e":
                let pointE = new Point(this.X + this.Width, this.Y + this.Width);
            case "s":
                let pointS = new Point(this.X - this.Width / 2, this.Y);
            case "w":
                return new Point(this.X - this.Width / 2, this.Y);
            default:
                return new Point(this.X - this.Width / 2, this.Y);
        }
    }    

}