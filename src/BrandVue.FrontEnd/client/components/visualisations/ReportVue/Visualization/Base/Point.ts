export class Point {
   
    public X: number;
    public Y: number;
    constructor(x: number, y: number) {
        this.X = x;
        this.Y = y;
    }

    public Transform(point: any, matrix: any) {
        point.x = this.X;
        point.y = this.Y;
        const transformed = point.matrixTransform(matrix);
        return new Point(transformed.x, transformed.y);
    }

    public Translate(x: number, y: number): Point {
        return new Point(this.X + x, this.Y + y);
    }

}