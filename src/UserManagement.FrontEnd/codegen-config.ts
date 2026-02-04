import { ConfigFile } from '@rtk-query/codegen-openapi';
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
  dotenv.config();
}


const config: ConfigFile = {
  schemaFile: process.env.OPENAPI_URL || 'http://localhost:7036/usermanagement/openapi/v1.json',
  apiFile: path.join(__dirname, 'src', 'rtk', 'api', 'createApi.ts'),
  apiImport: 'api',
  outputFile: path.join(__dirname, 'src', 'rtk', 'apiSlice.ts'),
  exportName: 'userManagementApi', 
  hooks: true, 
};

const generateConfig = () => {
    return config;
};

module.exports = generateConfig();

