// Public API exports for programmatic usage
export { getConfig, saveConfig, clearConfig, getConfigPath } from './config.js';
export type { ContentoConfig } from './config.js';

export { ApiClient, ApiError, createClient, validateApiKey } from './api-client.js';
export type { ApiClientOptions } from './api-client.js';

export { connectSSE } from './sse-client.js';
export type { SSEEvent, SSEProgress, SSEClientOptions } from './sse-client.js';

export { output, setJsonMode, setVerboseMode, isJsonMode, isVerboseMode } from './output.js';

export { createSpinner } from './ui/spinner.js';
export type { Spinner } from './ui/spinner.js';

export { createProgressBar } from './ui/progress.js';
export type { ProgressBar } from './ui/progress.js';

export { renderTable } from './ui/table.js';
export type { TableOptions } from './ui/table.js';

export { showBanner } from './ui/banner.js';
