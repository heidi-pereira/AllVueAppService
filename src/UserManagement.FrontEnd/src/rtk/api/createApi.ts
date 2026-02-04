import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export const api = createApi({
  reducerPath: '', // Change this if needed
    baseQuery: fetchBaseQuery({
        baseUrl: '/usermanagement',
        prepareHeaders: (headers, { method }) => {
            if (['POST', 'PUT', 'PATCH'].includes(method?.toUpperCase())) {
                headers.set('Content-Type', 'application/json');
            }
            return headers;
        }

    }), // Use your backend root
  endpoints: () => ({}),
});