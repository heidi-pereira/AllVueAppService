import '@testing-library/jest-dom';
import { getAllCompanyNames } from './helpers';

describe('getAllCompanyNames', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('returns an empty string when no company names are provided', () => {
    const response = getAllCompanyNames(undefined, undefined);
    expect(response).toBe('');
  });

  it('returns an empty string when no company name is provided', () => {
    const response = getAllCompanyNames(undefined, ['one', 'two']);
    expect(response).toBe('');
  });

  it('returns the company name when no ancestors are provided', () => {
    const companyName = 'Test Company';
    const response = getAllCompanyNames(companyName, undefined);
    expect(response).toBe(companyName);
  });

  it('returns the company and ancestor names supplied', () => {
    const companyName = 'Test Company';
    const ancestor1Name = 'Ancestor One';
    const ancestor2Name = 'Ancestor Two';
    const ancestorNames = [ancestor1Name, ancestor2Name];
    const response = getAllCompanyNames(companyName, ancestorNames);
    expect(response).toBe(`${companyName}, ${ancestor1Name} & ${ancestor2Name}`);
  });
});