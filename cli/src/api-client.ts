import { getConfig } from './config.js';

const DEFAULT_BASE_URL = 'https://api.contentocms.com/v1';

export class ApiError extends Error {
  constructor(
    public readonly statusCode: number,
    public readonly code: string,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  toJSON() {
    return {
      error: true,
      code: this.code,
      statusCode: this.statusCode,
      message: this.message,
    };
  }
}

function getBaseUrl(overrideUrl?: string): string {
  if (overrideUrl) return overrideUrl;
  const config = getConfig();
  return config?.baseUrl ?? DEFAULT_BASE_URL;
}

function getApiKey(): string {
  const config = getConfig();
  if (!config?.apiKey) {
    throw new ApiError(401, 'NOT_AUTHENTICATED', 'Not logged in. Run `contento login` first.');
  }
  return config.apiKey;
}

function buildHeaders(apiKey?: string): Record<string, string> {
  const key = apiKey ?? getApiKey();
  return {
    'Authorization': `Bearer ${key}`,
    'Content-Type': 'application/json',
    'User-Agent': 'contento-cli/0.1.0',
  };
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let code = 'API_ERROR';
    let message = `HTTP ${response.status}: ${response.statusText}`;

    try {
      const body = await response.json() as Record<string, unknown>;
      if (body.code) code = String(body.code);
      if (body.message) message = String(body.message);
      if (body.error && typeof body.error === 'string') message = body.error;
    } catch {
      // use default message
    }

    throw new ApiError(response.status, code, message);
  }

  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('application/json')) {
    return (await response.json()) as T;
  }

  // For non-JSON responses, return the text as unknown
  return (await response.text()) as unknown as T;
}

export interface ApiClientOptions {
  baseUrl?: string;
  apiKey?: string;
}

export class ApiClient {
  private readonly baseUrl: string;
  private readonly apiKey?: string;

  constructor(options?: ApiClientOptions) {
    this.baseUrl = getBaseUrl(options?.baseUrl);
    this.apiKey = options?.apiKey;
  }

  async get<T>(path: string): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'GET',
      headers: buildHeaders(this.apiKey),
    });
    return handleResponse<T>(response);
  }

  async post<T>(path: string, body?: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'POST',
      headers: buildHeaders(this.apiKey),
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return handleResponse<T>(response);
  }

  async put<T>(path: string, body?: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'PUT',
      headers: buildHeaders(this.apiKey),
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return handleResponse<T>(response);
  }

  async delete<T>(path: string): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const response = await fetch(url, {
      method: 'DELETE',
      headers: buildHeaders(this.apiKey),
    });
    return handleResponse<T>(response);
  }

  /**
   * Returns a URL for SSE streaming (does not initiate the connection).
   */
  streamUrl(path: string): string {
    return `${this.baseUrl}${path}`;
  }

  getAuthHeaders(): Record<string, string> {
    return buildHeaders(this.apiKey);
  }
}

/**
 * Create an API client using current config + optional overrides from CLI flags.
 */
export function createClient(options?: ApiClientOptions): ApiClient {
  return new ApiClient(options);
}

/**
 * Validate an API key by making a test request.
 */
export async function validateApiKey(apiKey: string, baseUrl?: string): Promise<boolean> {
  const client = new ApiClient({ apiKey, baseUrl });
  try {
    await client.get('/pseo/projects');
    return true;
  } catch {
    return false;
  }
}
