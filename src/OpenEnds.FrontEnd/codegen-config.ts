import { ConfigFile } from '@rtk-query/codegen-openapi';
import path from 'path';

const config: ConfigFile = {
  schemaFile: process.env.OPENAPI_URL || 'http://localhost:7035/swagger/v1/swagger.json',
  apiFile: path.join(__dirname, 'src', 'rtk', 'api', 'createApi.ts'),
  apiImport: 'api',
  outputFile: path.join(__dirname, 'src', 'rtk', 'apiSlice.ts'),
  exportName: 'openEndsApi', 
  hooks: true, 
};

const generateConfig = () => {
    return config;
};

module.exports = generateConfig();
