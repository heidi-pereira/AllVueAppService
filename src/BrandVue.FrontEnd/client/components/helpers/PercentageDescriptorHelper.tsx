export const getPercentageDescriptor = (percentage: number): string => {
    if (percentage < 9) return "Fewer than one in ten";
    if (percentage < 12) return "One in ten";
    if (percentage < 15) return "More than one in ten";
    if (percentage < 18) return "One in six";
    if (percentage < 24) return "One in five";
    if (percentage < 28) return "A quarter of";
    if (percentage < 29) return "Just over a quarter of";
    if (percentage < 32) return "Three in ten";
    if (percentage < 35) return "A third of";
    if (percentage < 38) return "Just over a third of";
    if (percentage < 43) return "Two in five";
    if (percentage < 45) return "More than two in five";
    if (percentage < 49) return "Approaching half of";
    if (percentage < 52) return "Half of";
    if (percentage < 58) return "More than half of";
    if (percentage < 62) return "Three in five";
    if (percentage < 65) return "Just over three in five";
    if (percentage < 68) return "Two thirds of";
    if (percentage < 74) return "Seven in ten";
    if (percentage < 77) return "Three quarters of";
    if (percentage < 78) return "Just over three quarters of";
    if (percentage < 83) return "Four in five";
    if (percentage < 88) return "More than four in five";
    if (percentage < 94) return "Nine in ten";
    if (percentage < 100) return "Almost all";
    return "All";
}