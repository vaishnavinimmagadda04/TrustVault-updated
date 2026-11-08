import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';

const configuredUrl = process.env.TRUSTVAULT_API_URL;
if (!configuredUrl) {
  throw new Error('Set TRUSTVAULT_API_URL to the deployed API URL ending in /api.');
}

const apiUrl = new URL(configuredUrl);
const apiBaseUrl = apiUrl.toString().replace(/\/+$/, '');
if (apiUrl.protocol !== 'https:' || !apiUrl.pathname.replace(/\/+$/, '').endsWith('/api')) {
  throw new Error('TRUSTVAULT_API_URL must use HTTPS and end in /api.');
}

const outputPath = resolve('dist/trustvault-ui/browser/runtime-config.js');
await mkdir(dirname(outputPath), { recursive: true });
await writeFile(
  outputPath,
  `window.trustVaultConfig = ${JSON.stringify({ apiBaseUrl })};\n`,
  'utf8',
);
