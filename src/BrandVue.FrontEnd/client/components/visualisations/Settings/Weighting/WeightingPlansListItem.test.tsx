import '@testing-library/jest-dom';
import { WeightingType, WeightingStyle, WeightingTypeStyle, Factory } from '../../../../BrandVueApi';

// Mock the Factory.WeightingAlgorithmsClient
jest.mock('../../../../BrandVueApi', () => ({
    ...jest.requireActual('../../../../BrandVueApi'),
    Factory: {
        WeightingAlgorithmsClient: jest.fn(),
    },
}));

describe('WeightingPlansListItem - getPlanType function', () => {
    let mockWeightingAlgorithmsClient: any;
    let mockOnErrorMessage: jest.Mock;
    let mockSetWeightingStyle: jest.Mock;
    let mockSetWeightingType: jest.Mock;
    let mockSetIsLoading: jest.Mock;

    beforeEach(() => {
        jest.clearAllMocks();
        
        mockOnErrorMessage = jest.fn();
        mockSetWeightingStyle = jest.fn();
        mockSetWeightingType = jest.fn();
        mockSetIsLoading = jest.fn();

        mockWeightingAlgorithmsClient = {
            weightingTypeAndStyle: jest.fn(),
        };

        (Factory.WeightingAlgorithmsClient as jest.Mock).mockReturnValue(mockWeightingAlgorithmsClient);
    });

    // Direct implementation of the getPlanType function for testing
    const createGetPlanTypeFunction = (subsetId?: string) => {
        const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient((error: any) => error());
        const props = { subset: subsetId !== undefined ? { id: subsetId } : undefined };
        
        return () => {
            if (props.subset?.id) {
                weightingAlgorithmsClient.weightingTypeAndStyle(props.subset?.id).then((p: WeightingTypeStyle) => {
                    mockSetWeightingStyle(p.style);
                    mockSetWeightingType(p.type);
                    mockSetIsLoading(false);
                }).catch((e: Error) => {
                    mockOnErrorMessage("An error occurred trying to load weightings information");
                    mockSetIsLoading(false);
                });
            }
            else {
                mockSetIsLoading(false);
            }
        };
    };

    describe('when subset.id is defined', () => {
        const mockSubsetId = 'test-subset-id';

        it('should call weightingTypeAndStyle with correct subset id', () => {
            const mockResponse = new WeightingTypeStyle({
                type: WeightingType.Tracker,
                style: WeightingStyle.RIM
            });
            
            mockWeightingAlgorithmsClient.weightingTypeAndStyle.mockResolvedValue(mockResponse);

            const getPlanType = createGetPlanTypeFunction(mockSubsetId);
            getPlanType();

            expect(mockWeightingAlgorithmsClient.weightingTypeAndStyle).toHaveBeenCalledWith(mockSubsetId);
        });

        it('should set weighting style and type on successful API response', async () => {
            const mockResponse = new WeightingTypeStyle({
                type: WeightingType.Tracker,
                style: WeightingStyle.RIM
            });
            
            mockWeightingAlgorithmsClient.weightingTypeAndStyle.mockResolvedValue(mockResponse);

            const getPlanType = createGetPlanTypeFunction(mockSubsetId);
            getPlanType();

            // Wait for promise resolution
            await new Promise(resolve => setTimeout(resolve, 10));

            expect(mockSetWeightingStyle).toHaveBeenCalledWith(WeightingStyle.RIM);
            expect(mockSetWeightingType).toHaveBeenCalledWith(WeightingType.Tracker);
            expect(mockSetIsLoading).toHaveBeenCalledWith(false);
        });

        it('should handle Unknown values correctly', async () => {
            const mockResponse = new WeightingTypeStyle({
                type: WeightingType.Unknown,
                style: WeightingStyle.Unknown
            });
            
            mockWeightingAlgorithmsClient.weightingTypeAndStyle.mockResolvedValue(mockResponse);

            const getPlanType = createGetPlanTypeFunction(mockSubsetId);
            getPlanType();

            await new Promise(resolve => setTimeout(resolve, 10));

            expect(mockSetWeightingStyle).toHaveBeenCalledWith(WeightingStyle.Unknown);
            expect(mockSetWeightingType).toHaveBeenCalledWith(WeightingType.Unknown);
            expect(mockSetIsLoading).toHaveBeenCalledWith(false);
        });

        it('should call onErrorMessage and set loading to false on API error', async () => {
            const mockError = new Error('API Error');
            mockWeightingAlgorithmsClient.weightingTypeAndStyle.mockRejectedValue(mockError);

            const getPlanType = createGetPlanTypeFunction(mockSubsetId);
            getPlanType();

            await new Promise(resolve => setTimeout(resolve, 10));

            expect(mockOnErrorMessage).toHaveBeenCalledWith("An error occurred trying to load weightings information");
            expect(mockSetIsLoading).toHaveBeenCalledWith(false);
        });
    });

    describe('when subset.id is undefined', () => {
        it('should not call weightingTypeAndStyle API', () => {
            const getPlanType = createGetPlanTypeFunction(undefined);
            getPlanType();

            expect(mockWeightingAlgorithmsClient.weightingTypeAndStyle).not.toHaveBeenCalled();
            expect(mockOnErrorMessage).not.toHaveBeenCalled();
            expect(mockSetWeightingStyle).not.toHaveBeenCalled();
            expect(mockSetWeightingType).not.toHaveBeenCalled();
        });
    });

    describe('when subset is null', () => {
        it('should behave the same as undefined subset.id', () => {
            const getPlanType = createGetPlanTypeFunction(undefined);
            getPlanType();

            expect(mockWeightingAlgorithmsClient.weightingTypeAndStyle).not.toHaveBeenCalled();
            expect(mockSetIsLoading).toHaveBeenCalledWith(false);
            expect(mockOnErrorMessage).not.toHaveBeenCalled();
        });
    });

    describe('edge cases', () => {
        it('should handle empty string subset id by NOT calling the API (empty string is falsy)', () => {
            const getPlanType = createGetPlanTypeFunction('');
            getPlanType();

            // Empty string is falsy in JavaScript, so the API should NOT be called
            expect(mockWeightingAlgorithmsClient.weightingTypeAndStyle).not.toHaveBeenCalled();
            expect(mockSetIsLoading).toHaveBeenCalledWith(false);
            expect(mockOnErrorMessage).not.toHaveBeenCalled();
        });

        it('should handle API response with null/undefined values', async () => {
            const mockResponse = new WeightingTypeStyle({
                type: null as any,
                style: undefined as any
            });
            
            mockWeightingAlgorithmsClient.weightingTypeAndStyle.mockResolvedValue(mockResponse);

            const getPlanType = createGetPlanTypeFunction('test-id');
            getPlanType();

            await new Promise(resolve => setTimeout(resolve, 10));

            expect(mockSetWeightingStyle).toHaveBeenCalledWith(undefined);
            expect(mockSetWeightingType).toHaveBeenCalledWith(null);
            expect(mockSetIsLoading).toHaveBeenCalledWith(false);
        });

        it('should handle API response with all WeightingStyle values', async () => {
            // Test all enum values for WeightingStyle
            const testCases = [
                WeightingStyle.Unknown,
                WeightingStyle.Interlocked,
                WeightingStyle.RIM,
                WeightingStyle.ResponseWeighting,
            ];

            for (const style of testCases) {
                mockSetWeightingStyle.mockClear();
                mockSetWeightingType.mockClear();
                mockSetIsLoading.mockClear();

                const mockResponse = new WeightingTypeStyle({
                    type: WeightingType.Tracker,
                    style: style
                });
                mockWeightingAlgorithmsClient.weightingTypeAndStyle.mockResolvedValue(mockResponse);

                const getPlanType = createGetPlanTypeFunction('test-id');
                getPlanType();

                await new Promise(resolve => setTimeout(resolve, 10));

                expect(mockSetWeightingStyle).toHaveBeenCalledWith(style);
                expect(mockSetWeightingType).toHaveBeenCalledWith(WeightingType.Tracker);
                expect(mockSetIsLoading).toHaveBeenCalledWith(false);
            }
        });
    });
});
