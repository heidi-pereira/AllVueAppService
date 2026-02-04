import fs from 'fs';
import path from 'path';
import dotenv from 'dotenv';

// Determine environment (default to 'development' if not set)
const NODE_ENV = process.env.NODE_ENV || 'development';

// Load the appropriate .env file
const envFile = `.env.${NODE_ENV}`;
const envPath = path.resolve(process.cwd(), envFile);

if (fs.existsSync(envPath)) {
  dotenv.config({ path: envPath });
} else {
  dotenv.config(); // fallback to default .env
}

export default {
  api: {
    input: process.env.OPENAPI_URL,
    output: {
      mode: 'single',
      target: './src/orval/api/generated.ts',
      schemas: './src/orval/api/models',
      client: 'axios', 
      mock: false,
    },
    hooks: true,
  },
};