import { nearestFraction, getSubsetToMaxLength }  from './LlmContextHelper'

describe('nearestFraction', () => {
    it('gives 3/7 for 0.428', () => {
        expect(nearestFraction(0.428)).toEqual([3, 7]);
    });
    it('gives 1/4 for 0.25', () => {
        expect(nearestFraction(0.25)).toEqual([1, 4]);
    });
});

describe('getSubsetToMaxLength', () => {
    it('25% of input results in 1 in every 4 items', () => {
        const input = new Array(100).fill('');
        input.forEach((_, index) => {
            if (index % 4 === 0) {
                input[index] = 'a';
            }
            else
            {
                input[index] = 'b';
            }
        });
        const result = getSubsetToMaxLength(input, 25);
        expect(result).toHaveLength(25);
        expect(result).toEqual(new Array(25).fill('a'));
    });
});
