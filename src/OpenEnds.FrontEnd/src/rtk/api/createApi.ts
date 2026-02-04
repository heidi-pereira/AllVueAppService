import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export const api = createApi({
  reducerPath: 'openEndsApi',
    baseQuery: fetchBaseQuery({
        baseUrl: '/openends',
        prepareHeaders: (headers) => {
            // Set default headers - individual endpoints can override as needed
            headers.set('Content-Type', 'application/json');
            return headers;
        }

    }), // Use your backend root
  endpoints: () => ({}),
});