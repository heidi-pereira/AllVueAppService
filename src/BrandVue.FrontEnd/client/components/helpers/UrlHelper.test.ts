import {constructQueryString} from './UrlHelper';

describe('UrlHelper', () => {

    it('constructQueryString should add new values to query string without removing existing ones', () => {
    const currentQueryString = '?name=test&age=20';
    const newParameters = [
        { name: 'location', value: 'USA' },
        { name: 'gender', value: 'male' }
    ];
    const updatedQueryString = constructQueryString(currentQueryString, newParameters);
        expect(updatedQueryString).toBe('?age=20&gender=male&location=USA&name=test');
    });

    it("constructQueryString should handle characters that are invalid in urls", () => {
    const currentQueryString = '';
        const newParameters = [
            { name: 'Whisky&Bourbon', value: '==US&A==' }
        ];
        const updatedQueryString = constructQueryString(currentQueryString, newParameters);
        expect(updatedQueryString).toBe('?Whisky%26Bourbon=%3D%3DUS%26A%3D%3D');
    });

    // This behaviour might be relied upon elsewhere, e.g. people bookmarking locations
    // that include +'s, so we shouldn't break it.
    it("constructQueryString encodes spaces as pluses", () => {
        const currentQueryString = '';
        const newParameters = [
            { name: 'Whisky & Bourbon', value: '-1.1' }
        ];

        const updatedQueryString = constructQueryString(currentQueryString, newParameters);
        expect(updatedQueryString).toBe('?Whisky+%26+Bourbon=-1.1');
    });
});
